using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Collections.Generic;
using System.Collections.Concurrent;

using UnityEngine;
using UnityEditor;

using FlatBuffers;

namespace Oculus.Avatar2
{
    // Record of a client connecting
    readonly struct ConnectionRecord
    {
        public ConnectionRecord(UInt32 newSessionId, ovrAvatar.Arbiter.SessionInitialize sessionInitializePacket)
        {
            sessionId = newSessionId;
            clientName = sessionInitializePacket.ClientName;
            clientVersionMajor = sessionInitializePacket.ClientVersionMajor;
            clientVersionMinor = sessionInitializePacket.ClientVersionMinor;
            protocolVersionMajor = sessionInitializePacket.ProtocolVersionMajor;
            protocolVersionMinor = sessionInitializePacket.ProtocolVersionMinor;
        }

        public override string ToString()
        {
            return String.Format("Session {0:X} initialized; client \'{1}\' v {2}.{4} protococol v {3}.{4}",
                        sessionId,
                        clientName,
                        clientVersionMajor,
                        clientVersionMinor,
                        protocolVersionMajor,
                        protocolVersionMinor);
        }

        public readonly UInt32 sessionId;
        public readonly string clientName;
        public readonly Int32 clientVersionMajor;
        public readonly Int32 clientVersionMinor;
        public readonly Int32 protocolVersionMajor;
        public readonly Int32 protocolVersionMinor;
    }

    public class ArbiterServer
    {
        private const Int32 kDefaultFlatBufferSize = 512;

        public enum ConnectionStatus { Unconnected, Connecting, Initializing, Open, ServerNotFound, Failed, Closed };


        public delegate void ArbiterPropertyListEventHandler(ArbiterStructs.PropertyList list);
        public event ArbiterPropertyListEventHandler OnPropertyList;

        public delegate void ArbiterEventHandler(ArbiterStructs.Event ev);
        public event ArbiterEventHandler OnEvent;

        public delegate void ArbiterNetworkStatsEventHandler(ArbiterStructs.NetworkStats stats);
        public event ArbiterNetworkStatsEventHandler OnNetworkStats;
        public delegate void ArbiterMemoryStatsEventHandler(ArbiterStructs.MemoryStats stats);
        public event ArbiterMemoryStatsEventHandler OnMemoryStats;
        public delegate void ArbiterTaskStatsEventHandler(ArbiterStructs.TaskStats stats);
        public event ArbiterTaskStatsEventHandler OnTaskStats;


        public event EventHandler OnConnect;
        public event EventHandler OnDisconnect;

        public delegate void ArbiterPropertyUpdateEventHandler(ArbiterStructs.PropertyUpdate update);
        public event ArbiterPropertyUpdateEventHandler OnPropertyUpdate;

        static ArbiterServer()
        {
            clientRegistry_ = new Dictionary<Guid, ConnectionRecord>();
        }

        public ArbiterServer(IPAddress address, Int32 port)
        {
            address_ = address;
            port_ = port;
            serverThread_ = new Thread(ListenThread);
            status_ = ConnectionStatus.Unconnected;
            sessionId_ = 0;

            client_ = null;
            stream_ = null;

            remoteProperties_ = new ConcurrentDictionary<string, ArbiterBaseProperty>();
            localProperties_ = new ConcurrentDictionary<string, ArbiterBaseProperty>();
        }

        public ArbiterServer(string address, Int32 port) : this(IPAddress.Parse(address), port)
        {
        }

        public ConnectionStatus Status { get { return status_; } }

        public bool Start()
        {
            try
            {
                server_ = new TcpListener(address_, port_);
                server_.Start();
            }
            catch (Exception e)
            {
                UnityEditor.AvatarMonitor.MessageLog.AddMessage($"{ e.Message}");
                return false;
            }

            cts_ = new CancellationTokenSource();
            token_ = cts_.Token;

            serverThread_.Start();
            return true;
        }

        public void Stop()
        {
            cts_.Cancel();
            serverThread_.Join();
            status_ = ConnectionStatus.Closed;
        }

        protected bool HandlePacket(NetworkStream stream, ovrAvatar.Arbiter.ArbiterPacket arbiterPacket)
        {
            try
            {
                UInt32 id = arbiterPacket.SessionId;

                switch (arbiterPacket.DataType)
                {
                    case ovrAvatar.Arbiter.Packet.SessionPacket:
                        return ProcessArbiterSessionPacket(stream, id, arbiterPacket.DataAsSessionPacket());

                    case ovrAvatar.Arbiter.Packet.PropertyPacket:
                        return ProcessPropertyPacket(stream, id, arbiterPacket.DataAsPropertyPacket());

                    case ovrAvatar.Arbiter.Packet.TestPacket:
                        var testPacket = arbiterPacket.DataAsTestPacket();
                        UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Session {1:X}, test packet, value: {2}", ArbiterHelpers.TimeFormat(), arbiterPacket.SessionId, testPacket.TestValue);
                        return true;

                    case ovrAvatar.Arbiter.Packet.AvatarSDKEventPacket:
                        var ts = arbiterPacket.Timestamp;
                        var er = arbiterPacket.DataAsAvatarSDKEventPacket();
                        return ProcessEventPacket(stream, id, ts, er);

                    case ovrAvatar.Arbiter.Packet.NetworkStatsPacket:
                        return ProcessNetworkStatsPacket(stream, id, arbiterPacket.DataAsNetworkStatsPacket());

                    case ovrAvatar.Arbiter.Packet.MemoryStatsPacket:
                        return ProcessMemoryStatsPacket(stream, id, arbiterPacket.DataAsMemoryStatsPacket());

                    case ovrAvatar.Arbiter.Packet.TaskStatsPacket:
                        return ProcessTaskStatsPacket(stream, id, arbiterPacket.DataAsTaskStatsPacket());

                    default:
                        UnityEditor.AvatarMonitor.MessageLog.AddMessage("Bad / unknown packet type: {0}", arbiterPacket.DataType);
                        return true; // we did handle the packet, so don't return false and stop the server
                }
            }
            catch (Exception e)
            {
                UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0}", e.Message);
                return true;        // We did handle the packet, so don't return false and stop the server
            }
        }


        private void ListenThread()
        {
            while (!token_.IsCancellationRequested)
            {
                try
                {
                    status_ = ConnectionStatus.Connecting;
                    client_ = server_.AcceptTcpClient();
                }
                catch (Exception e)
                {
                    UnityEditor.AvatarMonitor.MessageLog.AddMessage($"{e.Message}");
                    return;
                }


                UnityEditor.AvatarMonitor.MessageLog.AddMessage("Client connected.");
                status_ = ConnectionStatus.Initializing;
                stream_ = client_.GetStream();

                // TODO: After client connected, loop reading packets until 'done', ie
                // handle a whole connected session here.
                // Done can be:
                // - an error, in which case fall out of this loop and accept a new connection
                // - a disconnect from the client
                // - an unknown packet type

                bool handsShaken = false;
                bool done = false;

                while (!done && !token_.IsCancellationRequested)
                {
                    if (!handsShaken)
                    {
                        if (WSHelpers.Handshake(client_, stream_))
                        {
                            currentConnectionUUID_ = Guid.NewGuid();
                            handsShaken = true;
                            status_ = ConnectionStatus.Open;
                            OnConnect?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            // non valid handshake / WS connect; break and accept new connection
                            done = true;
                        }
                    }
                    else
                    {
                        WSHelpers.Opcode opcode;
                        byte[] payload;
                        if (!WSHelpers.ReadPacket(client_, stream_, token_, out payload, out opcode))
                        {
                            done = true;
                        }
                        else
                        {
                            // TODO: Handle all possibilities more convincingly
                            switch (opcode)
                            {
                                case WSHelpers.Opcode.Binary:
                                    ByteBuffer bb = new FlatBuffers.ByteBuffer(payload);
                                    //UnityEditor.AvatarMonitor.MessageLog.AddMessage("Read data packet, length {0}", payload.Length);
                                    var arbiterPacket = ovrAvatar.Arbiter.ArbiterPacket.GetRootAsArbiterPacket(bb);
                                    if (!HandlePacket(stream_, arbiterPacket))
                                    {
                                        done = true;
                                    }
                                    break;
                                case WSHelpers.Opcode.Text:     // Arbiter protocol  is flatbuffer binary thanks
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Unexpected text packet.");
                                    break;
                                case WSHelpers.Opcode.ClosedConnection:
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Read close connection control frame, length {0}", payload.Length);
                                    done = true;
                                    break;
                                case WSHelpers.Opcode.Fragment:
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Unexpected WS fragment packet.");
                                    break;
                                case WSHelpers.Opcode.Ping:
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Unexpected WS ping packet.");
                                    break;
                                case WSHelpers.Opcode.Pong:
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Unexpected WS pong packet.");
                                    break;
                                default: // All reserved opcodeas
                                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("WS packet with reserved bits; extension in use?");
                                    break;
                            }
                        }
                    }
                }

                // Mark state as no longer connected
                status_ = ConnectionStatus.Connecting;

                // Close down and wait for a reconnect
                stream_.Flush();
                stream_.Close();
                stream_ = null;

                client_.Close();
                client_ = null;

                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }
        }


        private static void SessionClose(Guid uuid)
        {
            if (clientRegistry_.TryGetValue(uuid, out var record))
            {
                UInt32 sessionId = record.sessionId;

                UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Websocket for session {1:X} from \'{2}\' closed.", ArbiterHelpers.TimeFormat(), sessionId, record.clientName);

                // Remove the client info (maybe we need to do more here)
                clientRegistry_.Remove(uuid);
            }
        }

        private bool SendInitializeAck()
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} session {1:X} Send initialize ack.", ArbiterHelpers.TimeFormat(), sessionId_);
            var fbb = new FlatBufferBuilder(kDefaultFlatBufferSize);
            var sia = ovrAvatar.Arbiter.SessionInitializeAck.CreateSessionInitializeAck(fbb, sessionId_).Value;
            var sp = ovrAvatar.Arbiter.SessionPacket.CreateSessionPacket(fbb, ovrAvatar.Arbiter.Session.SessionInitializeAck, sia).Value;
            var apb = ovrAvatar.Arbiter.ArbiterPacket.CreateArbiterPacket(fbb, sessionId_, ArbiterHelpers.MicrosecondsSinceEpochUTC(), ovrAvatar.Arbiter.Packet.SessionPacket, sp).Value;
            fbb.Finish(apb);

            var buffer = fbb.SizedByteArray();
            return WSHelpers.SendDataPacket(stream_, buffer);
        }


        private bool SendTestPacket(NetworkStream stream, UInt32 sessionId)
        {
            var fbb = new FlatBufferBuilder(kDefaultFlatBufferSize);

            var test = ovrAvatar.Arbiter.TestPacket.CreateTestPacket(fbb, 42).Value;
            var apb = ovrAvatar.Arbiter.ArbiterPacket.CreateArbiterPacket(fbb, sessionId_, ArbiterHelpers.MicrosecondsSinceEpochUTC(), ovrAvatar.Arbiter.Packet.TestPacket, test).Value;
            fbb.Finish(apb);

            var buffer = fbb.SizedByteArray();
            return WSHelpers.SendDataPacket(stream, buffer);
        }

        // Process arbiter session control packets arriving from a client
        //
        public bool ProcessArbiterSessionPacket(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.SessionPacket sessionPacket)
        {
            switch (sessionPacket.DataType)
            {
                case ovrAvatar.Arbiter.Session.SessionInitialize:
                    {
                        if (sessionId != 0xffffffff)
                        {
                            // Session id should not be anything other than this marker value on initialize
                            UnityEditor.AvatarMonitor.MessageLog.AddMessage("Initialize packet with non marker session id received.");
                            return false;
                        }
                        // Check compatibility on the protocol here
                        var sessionInitializePacket = sessionPacket.DataAsSessionInitialize();

                        sessionId_ = (UInt32)currentConnectionUUID_.GetHashCode();

                        // Our respomse is to send back an initialize ack with a session id
                        clientRegistry_[currentConnectionUUID_] = new ConnectionRecord(sessionId_, sessionInitializePacket);

                        UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} {1}", ArbiterHelpers.TimeFormat(), clientRegistry_[currentConnectionUUID_].ToString());

                        // Send an ack back
                        SendInitializeAck();
                    }
                    break;

                case ovrAvatar.Arbiter.Session.SessionInitializeAck:
                    // This is a bug. Clients don't send us this, we send clients this.
                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("Session ack received from client.");
                    return false;

                case ovrAvatar.Arbiter.Session.SessionClose:
                    var sessionClosePacket = sessionPacket.DataAsSessionClose();
                    UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Client closes session {1:X}, reason: {2} ", ArbiterHelpers.TimeFormat(), sessionId, sessionClosePacket.CloseReason);
                    break;
            }

            return true;
        }

        // Get and register properties
        public bool RequestRemotePropertyList()
        {
            if (status_ != ConnectionStatus.Open)
            {
                return false;
            }

            FlatBufferBuilder fbb = new FlatBufferBuilder(256);

            var rqp = ovrAvatar.Arbiter.ListProperties.CreateListProperties(fbb).Value;
            var pp = ovrAvatar.Arbiter.PropertyPacket.CreatePropertyPacket(fbb, ovrAvatar.Arbiter.Property.ListProperties, rqp).Value;
            var apb = ovrAvatar.Arbiter.ArbiterPacket.CreateArbiterPacket(fbb, sessionId_, ArbiterHelpers.MicrosecondsSinceEpochUTC(), ovrAvatar.Arbiter.Packet.PropertyPacket, pp).Value;
            fbb.Finish(apb);

            var buffer = fbb.SizedByteArray();
            return WSHelpers.SendDataPacket(stream_, buffer);
        }

        public bool UnregisterRemoteProperty(string id)
        {
            if (remoteProperties_.ContainsKey(id))
            {
                while (!remoteProperties_.TryRemove(id, out var entry))
                {
                    // umm... a very short sleep would be good? Yield?
                    System.Threading.Thread.Yield();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        // Local properties
        public bool AddLocalProperty(string id, ArbiterBaseProperty newProperty)
        {
            if (localProperties_.ContainsKey(id))
            {
                return false;
            }

            localProperties_[id] = newProperty;
            return true;
        }

        public bool removeLocalProperty(string id)
        {
            if (localProperties_.ContainsKey(id))
            {
                while (!localProperties_.TryRemove(id, out var entry))
                {
                    // umm... a very short sleep would be good? Yield?
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public ArbiterBaseProperty GetLocalProperty(string id)
        {
            ArbiterBaseProperty property = null;
            localProperties_.TryGetValue(id, out property);
            return property;
        }

        public bool UpdateLocalProperty<T>(string id, T newValue)
        {
            ArbiterBaseProperty property;
            if (!localProperties_.TryGetValue(id, out property))
            {
                return false;
            }
            if (property.TypeHash() != newValue.GetHashCode())
            {
                return false; // type mismatch
            }

            var localProperty = (ArbiterProperty<T>)property;
            localProperty.Update(newValue);

            // send it (if registered)
            if (!localProperty.Registered())
            {
                return false;
            }

            // Good to send?
            if (status_ != ConnectionStatus.Open)
            {
                return false;
            }

            FlatBufferBuilder fbb = new FlatBufferBuilder(1024);

            var idloc = fbb.CreateString(id);

            return FinishSendPropertyUpdate(fbb, idloc, localProperty.AddValue(fbb), localProperty.ValueType());
        }


        public bool UpdateLocalProperty(string id, ArbiterBaseProperty newValue)
        {
            ArbiterBaseProperty property;
            if (!localProperties_.TryGetValue(id, out property))
            {
                return false;
            }

            // Don't permit swapping in a different type :-O
            if (property.ValueType() != newValue.ValueType())
            {
                return false;
            }

            // Store the value
            localProperties_[id].Update(newValue);

            // send it (if registered)
            if (!localProperties_[id].Registered())
            {
                return false;
            }

            if (status_ != ConnectionStatus.Open)
            {
                return false;
            }

            FlatBufferBuilder fbb = new FlatBufferBuilder(1024);

            var idloc = fbb.CreateString(id);
            return FinishSendPropertyUpdate(fbb, idloc, newValue.AddValue(fbb), newValue.ValueType());

        }

        // Synchronous poll, we may want a callback model for changes received; we could provide a
        // callback at the register above
        public ArbiterBaseProperty GetRemoteProperty(string id)
        {
            ArbiterBaseProperty property = null;
            remoteProperties_.TryGetValue(id, out property);
            return property;
        }

        public bool RemotePropertyRegistered(string id)
        {
            remoteProperties_.TryGetValue(id, out var property);
            return property != null;
        }

        public ConcurrentDictionary<string, ArbiterBaseProperty> GetLocalProperties()
        {
            return localProperties_;
        }

        public ConcurrentDictionary<string, ArbiterBaseProperty> GetRemoteProperties()
        {
            return remoteProperties_;
        }

        private bool FinishSendPropertyUpdate(
            FlatBufferBuilder fbb,
            StringOffset idLoc,
            Int32 valueLoc,
            ovrAvatar.Arbiter.PropertyValue valueType)
        {
            var puloc = ovrAvatar.Arbiter.PropertyUpdate.CreatePropertyUpdate(fbb, idLoc, valueType, valueLoc).Value;
            var pploc = ovrAvatar.Arbiter.PropertyPacket.CreatePropertyPacket(fbb, ovrAvatar.Arbiter.Property.PropertyUpdate, puloc).Value;
            var apb = ovrAvatar.Arbiter.ArbiterPacket.CreateArbiterPacket(fbb, sessionId_, ArbiterHelpers.MicrosecondsSinceEpochUTC(), ovrAvatar.Arbiter.Packet.PropertyPacket, pploc).Value;
            fbb.Finish(apb);

            var buffer = fbb.SizedByteArray();
            return WSHelpers.SendDataPacket(stream_, buffer);
        }
        public UInt32 SessionId() { return sessionId_; }

        public bool RegisterRemoteProperty(string id, ArbiterBaseProperty regProperty)
        {
            if (status_ != ConnectionStatus.Open)
            {
                return false;
            }

            if (remoteProperties_.ContainsKey(id))
            {
                return false;       // We already registered for it
            }

            remoteProperties_[id] = regProperty;
            remoteProperties_[id].MarkRegistered();

            FlatBufferBuilder fbb = new FlatBufferBuilder(256);
            var idOffset = fbb.CreateString(id);
            var nameOffset = fbb.CreateString(regProperty.Tag());

            var pr = ovrAvatar.Arbiter.PropertyRegister.CreatePropertyRegister(fbb, idOffset, nameOffset).Value;
            var pp = ovrAvatar.Arbiter.PropertyPacket.CreatePropertyPacket(fbb, ovrAvatar.Arbiter.Property.PropertyRegister, pr).Value;
            var apb = ovrAvatar.Arbiter.ArbiterPacket.CreateArbiterPacket(fbb, sessionId_, ArbiterHelpers.MicrosecondsSinceEpochUTC(), ovrAvatar.Arbiter.Packet.PropertyPacket, pp).Value;
            fbb.Finish(apb);

            var buffer = fbb.SizedByteArray();
            return WSHelpers.SendDataPacket(stream_, buffer);
        }


        public bool SendPropertyUpdate<T>(string tag, T value)
        {
            ArbiterBaseProperty property = ArbiterHelpers.MakePropertyFromValue(tag, value);
            // Good to send?
            if (status_ != ConnectionStatus.Open)
            {
                return false;
            }

            FlatBufferBuilder fbb = new FlatBufferBuilder(1024);

            var idloc = fbb.CreateString(tag);

            return FinishSendPropertyUpdate(fbb, idloc, property.AddValue(fbb), property.ValueType());

        }

        private bool SendPropertyList(NetworkStream stream, UInt32 sessionId)
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Session {1:X} requests property list.", ArbiterHelpers.TimeFormat(), sessionId);

            // Placeholder ids
            List<string> examplePropertyNames = new List<string> { "PropertyA", "PropertyB", "PropertyC" };

            var builder = new FlatBufferBuilder(kDefaultFlatBufferSize);

            // build property list; build strings first.
            List<StringOffset> fbbStrings = new List<StringOffset>();
            foreach (string str in examplePropertyNames)
            {
                fbbStrings.Add(builder.CreateString(str));
            }

            // Replace with sending the values from the local properties array
            var propertyUpdates = new List<Offset<ovrAvatar.Arbiter.PropertyUpdate>>();
            for (Int32 index = 0; index < examplePropertyNames.Count; index++)
            {
                // Send placeholder integer data

                ovrAvatar.Arbiter.IntegerValue.StartIntegerValue(builder);
                ovrAvatar.Arbiter.IntegerValue.AddI(builder, 52); // test value
                var value = ovrAvatar.Arbiter.IntegerValue.EndIntegerValue(builder);

                ovrAvatar.Arbiter.PropertyUpdate.StartPropertyUpdate(builder);
                ovrAvatar.Arbiter.PropertyUpdate.AddId(builder, fbbStrings[index]);
                ovrAvatar.Arbiter.PropertyUpdate.AddValueType(builder, ovrAvatar.Arbiter.PropertyValue.IntegerValue);
                ovrAvatar.Arbiter.PropertyUpdate.AddValue(builder, value.Value);
                propertyUpdates.Add(ovrAvatar.Arbiter.PropertyUpdate.EndPropertyUpdate(builder));
            }

            var properties = ovrAvatar.Arbiter.AvailableProperties.CreatePropertiesVector(builder, propertyUpdates.ToArray());

            ovrAvatar.Arbiter.AvailableProperties.StartAvailableProperties(builder);
            ovrAvatar.Arbiter.AvailableProperties.AddProperties(builder, properties);
            var ap = ovrAvatar.Arbiter.AvailableProperties.EndAvailableProperties(builder);

            ovrAvatar.Arbiter.PropertyPacket.StartPropertyPacket(builder);
            ovrAvatar.Arbiter.PropertyPacket.AddDataType(builder, ovrAvatar.Arbiter.Property.AvailableProperties);
            ovrAvatar.Arbiter.PropertyPacket.AddData(builder, ap.Value);
            var pp = ovrAvatar.Arbiter.PropertyPacket.EndPropertyPacket(builder);

            ovrAvatar.Arbiter.ArbiterPacket.StartArbiterPacket(builder);
            ovrAvatar.Arbiter.ArbiterPacket.AddSessionId(builder, sessionId);
            ovrAvatar.Arbiter.ArbiterPacket.AddTimestamp(builder, ArbiterHelpers.MicrosecondsSinceEpochUTC());
            ovrAvatar.Arbiter.ArbiterPacket.AddDataType(builder, ovrAvatar.Arbiter.Packet.PropertyPacket);
            ovrAvatar.Arbiter.ArbiterPacket.AddData(builder, pp.Value);
            var apb = ovrAvatar.Arbiter.ArbiterPacket.EndArbiterPacket(builder);

            builder.Finish(apb.Value);

            var buffer = builder.SizedByteArray();
            return WSHelpers.SendDataPacket(stream, buffer);
        }

        private bool PropertyRegister(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.PropertyRegister propertyRegisterPacket)
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Session {1:X} registers property {2}, {3}",
            ArbiterHelpers.TimeFormat(), sessionId, propertyRegisterPacket.Id, propertyRegisterPacket.PropertyName);
            return true;
        }

        private bool ReceivePropertyList(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.AvailableProperties propertyPacket)
        {
            string msg = String.Format("{0} Session {1:X} sends property list.", ArbiterHelpers.TimeFormat(), sessionId);
            UnityEditor.AvatarMonitor.MessageLog.AddMessage(msg);
            ArbiterStructs.PropertyList propertyList = new ArbiterStructs.PropertyList(propertyPacket);
            OnPropertyList?.Invoke(propertyList);
            return true;
        }


        private bool PropertyUpdate(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.PropertyUpdate propertyUpdatePacket)
        {
            ArbiterStructs.PropertyUpdate propertyUpdate = new ArbiterStructs.PropertyUpdate(propertyUpdatePacket);
            var id = propertyUpdatePacket.Id;
            string valueStr = ArbiterHelpers.ToString(propertyUpdatePacket);
            UnityEditor.AvatarMonitor.MessageLog.AddMessage("{0} Session {1:X} updates property {2}  with value {3}", ArbiterHelpers.TimeFormat(), sessionId, propertyUpdatePacket.Id, valueStr);
            if (remoteProperties_.TryGetValue(id, out var entry))
            {
                var property = ArbiterHelpers.MakePropertyFromPacket(propertyUpdatePacket);
                remoteProperties_[id].Update(property);
                OnPropertyUpdate?.Invoke(propertyUpdate);
            }
            else
            {
                // An update for an unregistered property, ignore?
            }
            return true;
        }

        private bool ProcessPropertyPacket(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.PropertyPacket propertyPacket)
        {
            switch (propertyPacket.DataType)
            {
                case ovrAvatar.Arbiter.Property.ListProperties:
                    return SendPropertyList(stream, sessionId);

                case ovrAvatar.Arbiter.Property.AvailableProperties:
                    return ReceivePropertyList(stream, sessionId, propertyPacket.DataAsAvailableProperties());

                case ovrAvatar.Arbiter.Property.PropertyRegister:
                    return PropertyRegister(stream, sessionId, propertyPacket.DataAsPropertyRegister());

                case ovrAvatar.Arbiter.Property.PropertyUpdate:
                    return PropertyUpdate(stream, sessionId, propertyPacket.DataAsPropertyUpdate());
            }

            return false;
        }

        private bool ProcessEventPacket(NetworkStream stream, UInt32 sessionId, UInt64 timestamp, ovrAvatar.Arbiter.AvatarSDKEventPacket packet)
        {
            ArbiterStructs.Event ev = new ArbiterStructs.Event(timestamp, packet);
            OnEvent?.Invoke(ev);
            return true;
        }

        private bool ProcessNetworkStatsPacket(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.NetworkStatsPacket networkStatsPacket)
        {
            ArbiterStructs.NetworkStats stats = new ArbiterStructs.NetworkStats(networkStatsPacket);
            float period = networkStatsPacket.Period;

            Int64 downloadTotalBytes = networkStatsPacket.DownloadTotalBytes;
            Int64 downloadSpeed = networkStatsPacket.DownloadSpeed;
            Int64 totalRequests = networkStatsPacket.TotalRequests;
            Int64 activeRequests = networkStatsPacket.ActiveRequests;
            OnNetworkStats?.Invoke(stats);
            return true;
        }

        private bool ProcessMemoryStatsPacket(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.MemoryStatsPacket memoryStatsPacket)
        {
            ArbiterStructs.MemoryStats stats = new ArbiterStructs.MemoryStats(memoryStatsPacket);
            float period = memoryStatsPacket.Period;
            Int64 currBytesUsed = memoryStatsPacket.CurrBytesUsed;
            Int64 currAllocationCount = memoryStatsPacket.CurrAllocationCount;
            Int64 maxBytesUsed = memoryStatsPacket.MaxBytesUsed;
            Int64 maxAllocationCount = memoryStatsPacket.MaxAllocationCount;
            Int64 totalBytesUsed = memoryStatsPacket.TotalBytesUsed;
            Int64 totalAllocationCount = memoryStatsPacket.TotalAllocationCount;
            OnMemoryStats?.Invoke(stats);
            return true;
        }

        private bool ProcessTaskStatsPacket(NetworkStream stream, UInt32 sessionId, ovrAvatar.Arbiter.TaskStatsPacket taskStatsPacket)
        {
            ArbiterStructs.TaskStats stats = new ArbiterStructs.TaskStats(taskStatsPacket);
            OnTaskStats?.Invoke(stats);
            return true;
        }

        // Process an Arbiter protocol message, delivered as a flatbuffer.
        // For now we don't insist on the packet being valid to leave other comms
        // protocols (eg existing JSON mechanism) to work, but as we advance we'll
        // make inability to recognize the packet an error.
        //

        private CancellationTokenSource cts_;
        private CancellationToken token_;
        private Thread serverThread_;

        private ConnectionStatus status_;


        private IPAddress address_;
        private Int32 port_;
        private TcpListener server_;
        TcpClient client_;

        NetworkStream stream_;

        private Guid currentConnectionUUID_;
        private UInt32 sessionId_;

        // Remote properties we have registered for (and their last received value
        private ConcurrentDictionary<string, ArbiterBaseProperty> remoteProperties_;

        // Local properties we know about (and whether the arbiter server has registered for updates)
        private ConcurrentDictionary<string, ArbiterBaseProperty> localProperties_;

        // Registry of client connections
        private static Dictionary<Guid, ConnectionRecord> clientRegistry_;
    }
}
