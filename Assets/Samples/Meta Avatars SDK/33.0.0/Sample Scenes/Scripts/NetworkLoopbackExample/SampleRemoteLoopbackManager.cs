#nullable enable

using System;
using System.Collections.Generic;
using Oculus.Avatar2;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using StreamLOD = Oculus.Avatar2.OvrAvatarEntity.StreamLOD;

/// <summary>
/// This class is an example of how to use the Streaming functions of the avatar to send and receive data over the network.
/// It extends BasicSampleRemoteLoopbackManager to incorporate simulated network latency and dropped packets.
/// </summary>
public class SampleRemoteLoopbackManager : BasicSampleRemoteLoopbackManager
{
    // Public functions

    // Configure the local and loopback avatars programmatically instead of from serialized fields. Must be called
    // immediately after adding the component
    public void Configure(OvrAvatarEntity localAvatar, List<OvrAvatarEntity> loopbackAvatars, SimulatedLatencySettings? latencySettings = null)
    {
        base.Configure(localAvatar, loopbackAvatars);
        if (latencySettings != null)
        {
            _simulatedLatencySettings = latencySettings;
        }

        _enableSimulatedLatency = latencySettings != null;
    }

    protected class ExtendedPacketData : SamplePacketData
    {
        public float fakeLatency = 0.0f;
    };

    [System.Serializable]
    public class SimulatedLatencySettings
    {
        [Range(0.0f, 0.5f)]
        public float fakeLatencyMax = 0.25f; //250 ms max latency

        [Range(0.0f, 0.5f)]
        public float fakeLatencyMin = 0.02f; //20ms min latency

        [Range(0.0f, 1.0f)]
        public float latencyWeight = 0.25f; // How much the latest sample impacts the current latency

        [Range(0, 10)]
        public int maxSamples = 4; //How many samples in our window

        [Range(0.0f, 1.0f)]
        public float packetLoss = 0.0f; // Percentage of packet loss that is simulated

        internal float averageWindow = 0f;
        internal float latencySum = 0f;
        internal List<float> latencyValues = new List<float>();

        public float NextValue()
        {
            averageWindow = latencySum / (float)latencyValues.Count;
            float randomLatency = UnityEngine.Random.Range(fakeLatencyMin, fakeLatencyMax);
            float fakeLatency = averageWindow * (1f - latencyWeight) + latencyWeight * randomLatency;

            if (latencyValues.Count >= maxSamples)
            {
                var firstLatency = latencyValues.First();
                if (firstLatency != null)
                {
                    latencySum -= firstLatency.Value;
                }
                latencyValues.RemoveFirst();
            }

            latencySum += fakeLatency;
            latencyValues.AddLast(fakeLatency);

            return fakeLatency;
        }

        public bool SkipNextPacket()
        {
            return UnityEngine.Random.value < packetLoss;
        }
    };

    [Tooltip("Toggle this on to enable simulated network latency to see how network conditions " +
        "can impact playback on a remote Avatar. Toggling this off disables simulated latency, " +
        "resulting in immediate playback on the remote Avatar. If you don't want to use " +
        "simulated latency, consider using the BasicSampleRemoteLoopbackManager script instead.")]
    [SerializeField]
    private bool _enableSimulatedLatency = true;

    [SerializeField]
    private SimulatedLatencySettings _simulatedLatencySettings = new SimulatedLatencySettings();

    protected void Awake()
    {
        float firstValue = UnityEngine.Random.Range(_simulatedLatencySettings.fakeLatencyMin, _simulatedLatencySettings.fakeLatencyMax);
        _simulatedLatencySettings.latencyValues.Insert(0, firstValue);
        _simulatedLatencySettings.latencySum += firstValue;
    }

    protected override PacketData GeneratePacketData(OvrAvatarEntity entity, StreamLOD lod)
    {
        ExtendedPacketData packet = FetchPacketFromPool() as ExtendedPacketData ?? new ExtendedPacketData();
        packet.Retain();

        packet.dataByteCount = entity.RecordStreamData_AutoBuffer(lod, ref packet.data);
        packet.fakeLatency = _simulatedLatencySettings.NextValue();

        Debug.Assert(packet.dataByteCount > 0);

        return packet;
    }

    protected override void ProcessPacketData(OvrAvatarEntity entity, PacketData packet)
    {
        var samplePacket = packet as ExtendedPacketData;
        if (samplePacket != null)
        {
            var dataSlice = samplePacket.data.Slice(0, (int)samplePacket.dataByteCount);
            entity.ApplyStreamData(in dataSlice);
        }
        else
        {
            Debug.LogError("Invalid packet format");
        }
    }

    protected override bool ShouldSkipPacket()
    {
        // If simulated latency is not enabled, we never skip a packet.
        if (!_enableSimulatedLatency)
        {
            return false;
        }

        return _simulatedLatencySettings.SkipNextPacket();
    }

    protected override bool ShouldProcessPacket(PacketData packet)
    {
        // If simulated latency is not enabled, we always process the packet.
        if (!_enableSimulatedLatency)
        {
            return true;
        }

        var samplePacket = packet as ExtendedPacketData;
        if (samplePacket != null)
        {
            samplePacket.fakeLatency -= Time.deltaTime;

            if (samplePacket.fakeLatency <= 0f)
            {
                return true;
            }
        }
        else
        {
            Debug.LogError("Invalid packet format");
        }
        return false;
    }
}

#if UNITY_EDITOR
// Customize the inspector so that we can hide the
// simulated network latency settings when not in use.
[CustomEditor(typeof(SampleRemoteLoopbackManager))]
public class SampleRemoteLoopbackManagerInspector : Editor
{
    SerializedProperty? _captureLOD;
    SerializedProperty? _localAvatar;
    SerializedProperty? _loopbackAvatars;
    SerializedProperty? _enableSimulatedLatency;
    SerializedProperty? _simulatedLatencySettings;

    private void OnEnable()
    {
        _captureLOD = serializedObject.FindProperty("_captureLOD");
        _localAvatar = serializedObject.FindProperty("_localAvatar");
        _loopbackAvatars = serializedObject.FindProperty("_loopbackAvatars");
        _enableSimulatedLatency = serializedObject.FindProperty("_enableSimulatedLatency");
        _simulatedLatencySettings = serializedObject.FindProperty("_simulatedLatencySettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(_captureLOD);
        EditorGUILayout.PropertyField(_localAvatar);
        EditorGUILayout.PropertyField(_loopbackAvatars);
        EditorGUILayout.PropertyField(_enableSimulatedLatency);

        // If simulated latency is not enabled, we'll hide the
        // simulated latency settings to keep the inspector clean.
        if (_enableSimulatedLatency != null && _enableSimulatedLatency.boolValue)
        {
            EditorGUILayout.PropertyField(_simulatedLatencySettings);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif // UNITY_EDITOR
