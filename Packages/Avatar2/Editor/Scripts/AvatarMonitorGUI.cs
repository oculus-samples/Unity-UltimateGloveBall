//#if UNITY_EDITOR

using System;
using System.Text;
using System.Linq;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEditor;

using Oculus.Avatar2;

namespace UnityEditor.AvatarMonitor
{
    public static class Helpers
    {
        public static string BytesToKB(Int64 bytes)
        {
            string result = String.Format("{0:0.00} KB", bytes / 1024.0);
            return result;
        }

        public static string BytesToMB(Int64 bytes)
        {
            string result = String.Format("{0:0.00} MB", bytes / (1024.0 * 1024.0));
            return result;
        }
        public static string BytesToGB(Int64 bytes)
        {
            string result = String.Format("{0:0.00} GB", bytes / (1024.0 * 1024.0 * 1024.0));
            return result;
        }

        public static string ReadableBytesString(Int64 bytes, bool bytesOnly)
        {
            if (bytesOnly)      // early out
            {
                return ReadableCount(bytes);
            }

            if (bytes < 1024)
            {
                return $"{bytes}";
            }
            else if (bytes < (1024 * 1024))
            {
                return BytesToKB(bytes);
            }
            else if (bytes < (1024 * 1024 * 1024))
            {
                return BytesToMB(bytes);
            }
            else
            {
                return BytesToGB(bytes);
            }
        }

        static public string ReadableCount(Int64 count)
        {
            return $"{count:n0}";
        }

        //< [0]  -> 0ns to (1<<0)1ns
        //< [1]  -> 1ns to (1<<1)2ns
        //< ...
        //< [30] -> ~0.5s to (1<<30)ns ~ 1s
        //< etc.
        public static string TaskTimeSpanString(Int32 index)
        {
            UInt64 bottom = ((UInt64)1 << index);
            //bottom <<= index;
            UInt64 top = ((UInt64)1 << (index + 1));
            //top <<= (index + 1);

            if (top > 100000000)        // use seconds
            {
                return String.Format("{0:0.00}-{1:0.00}s", bottom / 1000000000.0, top / 1000000000.0);
            }
            else if (top > 100000) // Use milliseconds
            {
                return String.Format("{0:0.00}-{1:0.00}ms", bottom / 1000000.0, top / 1000000.0);
            }
            else if (top > 100) // Use microseconds
            {
                return String.Format("{0:0.00}-{1:0.00}us", bottom / 1000.0, top / 1000.0);
            }
            else  // Use nanoseconds
            {
                return String.Format("{0:0.00}-{1:0.00}ns", bottom, top);
            }
        }
    }

    public class MessageLog
    {
        static MessageLog()
        {
            log_ = new ConcurrentQueue<string>();
        }

        static public void Clear()
        {
            while (log_.Count > 0)
            {
                if (!log_.TryDequeue(out var discard))
                {
                    Thread.Yield();
                }
            }
        }

        static public void AddMessage(string format, params object[] args)
        {
            string msg;
            if (args.Length == 0)
            {
                msg = format;
            }
            else
            {
                msg = string.Format(format, args);
            }

            log_.Enqueue(msg + "\n");

            while (log_.Count > cLogMaxSize)
            {
                while (!log_.TryDequeue(out var discard))
                {
                    Thread.Yield();
                }
            }
        }

        static public string Contents()
        {
            string result = "";
            foreach (string line in log_)
            {
                result += line;
            }

            return result;
        }

        static readonly private Int32 cLogMaxSize = 400;
        static private ConcurrentQueue<string> log_;
    }

    public class EventLog
    {
        static EventLog()
        {
            log_ = new ConcurrentQueue<string>();
        }
        static public void Clear()
        {
            while (log_.Count > 0)
            {
                string discard;
                log_.TryDequeue(out discard);
            }
        }

        static public void AddMessage(string format, params object[] args)
        {
            string msg;
            if (args.Length == 0)
            {
                msg = format;
            }
            else
            {
                msg = string.Format(format, args);
            }

            log_.Enqueue(msg + "\n");

            while (log_.Count > cLogMaxSize)
            {
                string discard;
                log_.TryDequeue(out discard);
            }
        }

        static public string Contents()
        {
            string result = "";
            foreach (string line in log_)
            {
                result += line;
            }

            return result;
        }

        static readonly private Int32 cLogMaxSize = 400;
        static private ConcurrentQueue<string> log_;
    }


    public class AvatarMonitorWindow : EditorWindow
    {
        static AvatarMonitorWindow()
        {
            // Unity and the lifetime of these objects are not very clear (to me at least)
            // and the constructor is called multiple times;
            // this static constructor is called when we might expect (window created)
            // So do some construction work here.
            if (server_ == null)
            {
                UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0}: Construct Arbiter server", ArbiterHelpers.TimeFormat());
                server_ = new ArbiterServer("127.0.0.1", 8080);
                server_.Start();
                server_.OnConnect += ClientConnect;
                server_.OnDisconnect += ClientDisconnect;
                eventLog_ = new ConcurrentQueue<string>();

                lastNetworkStats = null;
                lastMemoryStats = null;
                lastTaskStats = null;

                exponent_ = null;
                maxWeight_ = null;

                cts_ = new CancellationTokenSource();
                token_ = cts_.Token;

                propertyThread_ = new Thread(PropertyThread);
                propertyThread_.Start();

                lodUIState_ = new ConcurrentDictionary<Int32, LODUIState>();
            }
        }

        private static void PropertyThread()
        {
            while (!token_.IsCancellationRequested)
            {
                Thread.Sleep(1000);        // Sleep for a second
                try
                {
                    if (server_.Status == ArbiterServer.ConnectionStatus.Open)
                    {
                        server_.RequestRemotePropertyList();        // Valid return caught in OnPropertyList
                    }
                }
                catch (Exception)
                {
                    // Do nothing here
                }
            }
        }


        public AvatarMonitorWindow()
        {
        }

        ~AvatarMonitorWindow()
        {
            //MessageLog.AddMessage("{0}: Destroy Avatar Monitor Window instance.", ArbiterHelpers.TimeFormat());
            //cts_.Cancel();
            //propertyThread_.Join();
        }


        private static void ClientConnect(object sender, EventArgs args)
        {
            // Reset the stats so we stop displaying
            lastNetworkStats = null;
            lastMemoryStats = null;
            lastTaskStats = null;

            // Clear the log storage for messages and eventds
            MessageLog.Clear();
            while (!eventLog_.IsEmpty)
            {
                string discard;
                eventLog_.TryDequeue(out discard);
            }
        }

        private static void ClientDisconnect(object sender, EventArgs args)
        {
        }



        private void ClientPropertyUpdate(ArbiterStructs.PropertyUpdate update)
        {
            // I'd like to repaint here but you can't do it off the main thread.
            // We repaint on inspector update anyway.
        }

        private void ClientPropertyList(ArbiterStructs.PropertyList packet)
        {
            // Look for LOD records we don't have, and register for them
            foreach (ArbiterBaseProperty prop in packet.properties)
            {
                // LOD property handling
                if (prop.Tag().StartsWith("LOD::"))
                {
                    if (prop.IsFloat())
                    {
                        if (prop.Tag() == "LOD::exponent")
                        {
                            if (server_.RegisterRemoteProperty(prop.Tag(), prop))
                            {
                                exponent_ = (ArbiterFloatProperty)server_.GetRemoteProperty(prop.Tag());
                            }
                            else
                            {
                                // Error, most likely the register failed (network etc)
                            }
                        }
                    }

                    if (prop.IsInteger())
                    {
                        if (prop.Tag() == "LOD::maxWeight")
                        {
                            if (server_.RegisterRemoteProperty(prop.Tag(), prop))
                            {
                                maxWeight_ = (ArbiterIntProperty)server_.GetRemoteProperty(prop.Tag());
                            }
                            else
                            {
                                // Error, most likely the register failed (network etc)
                            }
                        }
                    }
                }

                if (prop.IsLODRecord())
                {
                    var record = ((ArbiterLODRecordProperty)prop).Value();
                    if (!server_.RemotePropertyRegistered(prop.Tag()))
                    {
                        MessageLog.AddMessage("{0} Session {1:X} Register for remote LOD record {2}.", ArbiterHelpers.TimeFormat(), server_.SessionId(), prop.Tag());
                        lodUIState_[record.id] = new LODUIState();
                        server_.RegisterRemoteProperty(prop.Tag(), prop);
                    }
                }

            }
        }


        private void ClientEvent(ArbiterStructs.Event ev)
        {
            DateTime utcTime = DateTime.SpecifyKind(ev.arrival, DateTimeKind.Utc);
            DateTime localTime = utcTime.ToLocalTime();
            string entry = localTime.ToString("hh:mm:ss.ff tt");
            entry += string.Format(":  {0}", ev.name);
            eventLog_.Enqueue(entry);
        }

        private void ClientNetworkStats(ArbiterStructs.NetworkStats stats)
        {
            lastNetworkStats = stats;
            lastNetworkStatsTime = DateTime.Now;
        }

        private void ClientMemoryStats(ArbiterStructs.MemoryStats stats)
        {
            lastMemoryStats = stats;
            lastMemoryStatsTime = DateTime.Now;
        }

        private void ClientTaskStats(ArbiterStructs.TaskStats stats)
        {
            lastTaskStats = stats;
            lastTaskStatsTime = DateTime.Now;
        }


        public void OnEnable()
        {
            server_.OnPropertyUpdate += ClientPropertyUpdate;
            server_.OnPropertyList += ClientPropertyList;
            server_.OnEvent += ClientEvent;
            server_.OnNetworkStats += ClientNetworkStats;
            server_.OnMemoryStats += ClientMemoryStats;
            server_.OnTaskStats += ClientTaskStats;
        }

        public void OnDisable()
        {
            server_.OnConnect += ClientConnect;
            server_.OnDisconnect += ClientDisconnect;
            server_.OnPropertyUpdate += ClientPropertyUpdate;
            server_.OnPropertyList += ClientPropertyList;
            server_.OnEvent += ClientEvent;
            server_.OnNetworkStats += ClientNetworkStats;
            server_.OnMemoryStats += ClientMemoryStats;
            server_.OnTaskStats += ClientTaskStats;

        }

        [MenuItem("AvatarSDK2/Avatar Monitor Window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(AvatarMonitorWindow));
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnStatsGUI()
        {
            Int32 labelWidth = 600;
            using (var statsScrollView = new EditorGUILayout.ScrollViewScope(statsScrollPos_, GUILayout.Width(750), GUILayout.Height(600)))
            {
                statsShowRawBytes = EditorGUILayout.Toggle("Show raw bytes:  ", statsShowRawBytes);
                EditorGUILayout.LabelField("");
                GUILayout.Label("Avatar Native Runtime Summary Stats", EditorStyles.boldLabel);
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("Memory", EditorStyles.boldLabel);
                if (lastMemoryStats != null)
                {
                    EditorGUILayout.LabelField("Current bytes used:  ", Helpers.ReadableBytesString(lastMemoryStats.currBytesUsed, statsShowRawBytes), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Current active allocations:  ", Helpers.ReadableCount(lastMemoryStats.currAllocationCount), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Peak bytes used:  ", Helpers.ReadableBytesString(lastMemoryStats.maxBytesUsed, statsShowRawBytes), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Peak allocation count:  ", Helpers.ReadableCount(lastMemoryStats.maxAllocationCount), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total bytes used:  ", Helpers.ReadableBytesString(lastMemoryStats.totalBytesUsed, statsShowRawBytes), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total allocation count:  ", Helpers.ReadableCount(lastMemoryStats.totalAllocationCount), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  ", lastMemoryStatsTime.ToString("hh:mm:ss.ff tt"));
                }
                else
                {
                    EditorGUILayout.LabelField("Current bytes used:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Current active allocations:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Peak bytes used:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Peak allocation count:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total bytes used:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total allocation count:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  Never", GUILayout.Width(labelWidth));
                }
                EditorGUILayout.LabelField("");
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
                if (lastNetworkStats != null)
                {
                    EditorGUILayout.LabelField("Total downloaded bytes:  ", Helpers.ReadableBytesString(lastNetworkStats.downloadTotalBytes, statsShowRawBytes), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Download speed:  ", Helpers.ReadableBytesString(lastNetworkStats.downloadSpeed, statsShowRawBytes), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total network requests:  ", Helpers.ReadableCount(lastNetworkStats.totalRequests), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Current active requests:  ", Helpers.ReadableCount(lastNetworkStats.activeRequests), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  ", lastNetworkStatsTime.ToString("hh:mm:ss.ff tt"), GUILayout.Width(labelWidth));
                }
                else
                {
                    EditorGUILayout.LabelField("Total downloaded bytes:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Download speed:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Total network requests:  ", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Current active requests:  ", GUILayout.Width(labelWidth));

                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  Never", GUILayout.Width(labelWidth));
                }
                EditorGUILayout.LabelField("");
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);

                if (lastTaskStats != null)
                {
                    EditorGUILayout.LabelField("Pending tasks:  ", Helpers.ReadableCount(lastTaskStats.pendingTasks));
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Task duration histogram");


                    for (Int32 index = 0; index < lastTaskStats.histogram.Length; index++)
                    {
                        string title = String.Format("{0:00}  {1}:  ", index, Helpers.TaskTimeSpanString(index), GUILayout.Width(labelWidth));
                        EditorGUILayout.LabelField(title, Helpers.ReadableCount(lastTaskStats.histogram[index]));
                    }

                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  ", lastMemoryStatsTime.ToString("hh:mm:ss.ff tt"), GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("");
                }
                else
                {
                    EditorGUILayout.LabelField("Pending tasks:  None");
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Task duration histogram");
                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("Updated:  Never");
                    EditorGUILayout.LabelField("");
                }
                GUILayout.EndVertical();
                statsScrollPos_ = statsScrollView.scrollPosition;
            }
        }

        private void OnEventsGUI()
        {
            GUILayout.Label("Avatar Native Runtime Event Log", EditorStyles.boldLabel);
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(eventScrollPos_, GUILayout.Width(800), GUILayout.Height(400)))
                {
                    string contents = "";
                    foreach (string ev in eventLog_)
                    {
                        contents += ev;
                        contents += "\n";
                    }

                    GUILayout.Label(contents);
                    eventScrollPos_ = scrollView.scrollPosition;
                }
            }

            if (GUILayout.Button("Clear event log"))
            {
                while (eventLog_.Count > 0)
                {
                    if (!eventLog_.TryDequeue(out var discard))
                    {
                        Thread.Yield();
                    }
                }
            }
        }

        bool player = false;
        private void IndividualLODGUI(ArbiterStructs.LODRecord lodRecord)
        {
            if (!lodUIState_.TryGetValue(lodRecord.id, out var state))
            {
                // This is an error; means we didn't make a UI state when thr LOD was registered
                return;
            }

            string title = String.Format("Avatar id {0}", lodRecord.id);

            state.structOpen = EditorGUILayout.Foldout(state.structOpen, title);
            if (state.structOpen)
            {
                GUILayout.BeginVertical();

                EditorGUI.BeginChangeCheck();
                Int32 importance = EditorGUILayout.DelayedIntField("Importance:  ", lodRecord.importanceScore);
                if (EditorGUI.EndChangeCheck())
                {
                    string tag = $"LOD::{lodRecord.id}::importance";
                    server_.SendPropertyUpdate(tag, importance);
                }

                EditorGUI.BeginChangeCheck();
                Int32 maxLODThreshold = EditorGUILayout.DelayedIntField("LOD threshold:  ", lodRecord.maxLODThreshold);
                if (EditorGUI.EndChangeCheck())
                {
                    string tag = $"LOD::{lodRecord.id}::maxLOD";
                    server_.SendPropertyUpdate(tag, maxLODThreshold);
                }

                GUI.enabled = false;
                EditorGUILayout.IntField("Assigned LOD;  ", lodRecord.assignedLOD);
                GUI.enabled = true;

                EditorGUI.BeginChangeCheck();
                bool isPlayer = EditorGUILayout.Toggle("Player:  ", lodRecord.isPlayer);
                if (EditorGUI.EndChangeCheck())
                {
                    string tag = $"LOD::{lodRecord.id}::isPlayer";
                    server_.SendPropertyUpdate(tag, isPlayer);
                }


                GUI.enabled = false;
                player = EditorGUILayout.Toggle("Culled:  ", lodRecord.isCulled);

                state.weightGroupOpen = EditorGUILayout.Foldout(state.weightGroupOpen, "LOD Weights");
                if (state.weightGroupOpen)
                {
                    for (Int32 index = 0; index < lodRecord.weights.Length; index++)
                    {
                        EditorGUILayout.IntField(String.Format("LOD {0}:  ", index), lodRecord.weights[index]);
                    }
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
        }

        private void OnLODGUI()
        {
            GUILayout.Label("Native Runtime LOD System", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("");

            var remoteProperties = server_.GetRemoteProperties();
            var lodRecords_ = remoteProperties.Values.OfType<ArbiterLODRecordProperty>().ToList();

            EditorGUILayout.IntField("Avatar LODs registered: ", lodRecords_.Count);
            if (exponent_ != null)
            {
                EditorGUI.BeginChangeCheck();
                float newExponent = EditorGUILayout.DelayedFloatField("LOD distribution exponent: ", exponent_.Value());
                if (EditorGUI.EndChangeCheck())
                {
                    server_.SendPropertyUpdate("LOD::exponent", newExponent);
                }
            }
            else
            {
                EditorGUILayout.LabelField("LOD distribution exponent:  Not set");
            }

            if (maxWeight_ != null)
            {
                EditorGUI.BeginChangeCheck();
                Int32 newMaxWeight = EditorGUILayout.DelayedIntField("LOD distribution max weight: ", maxWeight_.Value());
                if (EditorGUI.EndChangeCheck())
                {
                    server_.SendPropertyUpdate("LOD::maxWeight", newMaxWeight);
                }
            }
            else
            {
                EditorGUILayout.LabelField("LOD distribution max weight:  Not set");
            }
            EditorGUILayout.LabelField("");

            GUILayout.BeginVertical();

            if (lodRecords_.Count == 0)
            {
                return;
            }

            using (var lodScrollView = new EditorGUILayout.ScrollViewScope(lodScrollPos_, GUILayout.Width(800), GUILayout.Height(400)))
            {
                foreach (var record in lodRecords_)
                {
                    IndividualLODGUI(record.Value());
                }
                lodScrollPos_ = lodScrollView.scrollPosition;
            }
            GUILayout.EndVertical();
        }

        private void OnServerLogGUI()
        {
            GUILayout.Label("Arbiter client / server log", EditorStyles.boldLabel);
            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(logScrollPos_, GUILayout.Width(800), GUILayout.Height(400)))
                {
                    GUILayout.Label(MessageLog.Contents());
                    logScrollPos_ = scrollView.scrollPosition;
                }
            }

            if (GUILayout.Button("Clear log"))
            {
                MessageLog.Clear();
            }
        }

        // Draw some tabs that we'll fill with data in later diffs
        private void OnGUI()
        {
            tab_ = GUILayout.Toolbar(tab_, tabNames_);
            EditorGUILayout.LabelField("");

            switch (tab_)
            {
                case cStatsTab:
                    OnStatsGUI();
                    break;

                case cEventTab:
                    OnEventsGUI();
                    break;

                case cLODTab:
                    OnLODGUI();
                    break;

                case cServerLogTab:
                    OnServerLogGUI();
                    break;
            }
        }

        private static ArbiterServer server_;

        private const Int32 cStatsTab = 0;
        private const Int32 cEventTab = 1;
        private const Int32 cLODTab = 2;
        private const Int32 cServerLogTab = 3;
        private static readonly string[] tabNames_ = new string[] { "Stats", "Events", "LOD System", "Server Log" };

        // Local cached state for memory, tasks, network stats
        static private ArbiterStructs.NetworkStats lastNetworkStats;
        static private DateTime lastNetworkStatsTime;

        static private ArbiterStructs.MemoryStats lastMemoryStats;
        static private DateTime lastMemoryStatsTime;

        static private ArbiterStructs.TaskStats lastTaskStats;
        static private DateTime lastTaskStatsTime;    // GUI state

        // Event log storage
        private static ConcurrentQueue<string> eventLog_;

        // LOD tracking
        class LODUIState
        {
            public bool structOpen;
            public bool weightGroupOpen;
        };

        // LOD algorithm control
        private static ArbiterFloatProperty exponent_;
        private static ArbiterIntProperty maxWeight_;

        private static ConcurrentDictionary<Int32, LODUIState> lodUIState_;

        // Property update thread
        private static CancellationTokenSource cts_;
        private static CancellationToken token_;
        private static Thread propertyThread_;

        // UI state
        private Int32 tab_ = cServerLogTab;

        bool statsShowRawBytes = false;
        private Vector2 logScrollPos_;
        private Vector2 statsScrollPos_;
        private Vector2 eventScrollPos_;
        private Vector2 lodScrollPos_;
    }
}

//#endif
