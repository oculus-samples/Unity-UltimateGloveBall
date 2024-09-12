using System;
using System.Collections.Generic;
using Oculus.Avatar2;
using Oculus.Platform;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using StreamLOD = Oculus.Avatar2.OvrAvatarEntity.StreamLOD;


/// <summary>
/// This class is an abstract class that simulates network packet transfer via loopback.
/// Data isn't sent over a real network, but simply added to a queue and then "received" by a second, "remote" avatar.
/// For a real network, much of the logic of preparing snapshots and receiving based on the desired fidelity is the same.
/// Refer to concrete subclass for avatar streaming functions examples on how to serialize/deseriaze packet data
/// </summary>
public abstract class RemoteLoopbackManagerBase : MonoBehaviour
{
    private const string logScope = "RemoteLoopbackManagerBase";

    // Const & Static Variables
    private const int MAX_PACKETS_PER_FRAME = 3;

    // Frequency of how often to send network data
    private static readonly float[] StreamLodSnapshotIntervalSeconds = new float[OvrAvatarEntity.StreamLODCount] { 1f / 72, 2f / 72, 3f / 72, 4f / 72 };
    private readonly float[] _streamLodSnapshotElapsedTime = new float[OvrAvatarEntity.StreamLODCount];

    [SerializeField]
    [Tooltip("LOD level to capture avatar data with")]
    private StreamLOD _captureLOD = StreamLOD.Low;

    // Public functions

    // Configure the local and loopback avatars programmatically instead of from serialized fields. Must be called
    // immediately after adding the component
    public void Configure(OvrAvatarEntity localAvatar, List<OvrAvatarEntity> loopbackAvatars, SimulatedLatencySettings latencySettings = null)
    {
        _localAvatar = localAvatar;
        _loopbackAvatars = loopbackAvatars;
        if (latencySettings != null)
        {
            _simulatedLatencySettings = latencySettings;
        }
    }

    #region Internal Classes
    protected abstract class PacketData
    {
        public float fakeLatency;
        private uint refCount = 0;

        public PacketData() { }

        public bool Unretained => refCount == 0;
        public PacketData Retain() { ++refCount; return this; }
        public bool Release()
        {
            return --refCount == 0;
        }
    };

    protected class LoopbackState
    {
        public List<PacketData> packetQueue = new List<PacketData>(64);
        public StreamLOD requestedLod = StreamLOD.Low;
        public float smoothedPlaybackDelay = 0f;
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
                latencySum -= latencyValues.First().Value;
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

    #endregion

    // Serialized Variables
    [SerializeField]
    private OvrAvatarEntity _localAvatar = null;
    [SerializeField]
    private List<OvrAvatarEntity> _loopbackAvatars = null;
    [SerializeField]
    private SimulatedLatencySettings _simulatedLatencySettings = new SimulatedLatencySettings();

    // Private Variables
    private Dictionary<OvrAvatarEntity, LoopbackState> _loopbackStates =
        new Dictionary<OvrAvatarEntity, LoopbackState>();


    private readonly List<PacketData> _packetPool = new List<PacketData>(32);
    private readonly List<PacketData> _deadList = new List<PacketData>(16);

    protected void ReturnPacket(PacketData packet)
    {
        Debug.Assert(packet.Unretained);
        _packetPool.Add(packet);
    }

    protected PacketData FetchPacketFromPool()
    {
        PacketData result = null;
        if (_packetPool.Count > 0)
        {
            var lastIndex = _packetPool.Count - 1;
            result = _packetPool[lastIndex];
            _packetPool.RemoveAt(lastIndex);
        }

        return result;
    }

    public List<OvrAvatarEntity> LoopbackAvatars
    {
        get
        {
            return _loopbackAvatars;
        }

        set
        {
            _loopbackAvatars = value;
            CreateStates();
        }
    }

    #region Core Unity Functions

    protected void Start()
    {
        // Check for other LoopbackManagers in the current scene
        var loopbackManagers = FindObjectsOfType<RemoteLoopbackManagerBase>();
        if (loopbackManagers.Length > 1)
        {
            foreach (var loopbackManager in loopbackManagers)
            {
                if (loopbackManager == this || !loopbackManager.isActiveAndEnabled) { continue; }

                OvrAvatarLog.LogError($"Multiple active LoopbackManagers detected! Please update the scene."
                    , logScope, this);
                break;
            }
        }

        AvatarLODManager.Instance.firstPersonAvatarLod = _localAvatar.AvatarLOD;
        AvatarLODManager.Instance.enableDynamicStreaming = true;

        float firstValue = UnityEngine.Random.Range(_simulatedLatencySettings.fakeLatencyMin, _simulatedLatencySettings.fakeLatencyMax);
        _simulatedLatencySettings.latencyValues.Insert(0, firstValue);
        _simulatedLatencySettings.latencySum += firstValue;

        CreateStates();
    }

    private void CreateStates()
    {
        foreach (var item in _loopbackStates)
        {
            foreach (var packet in item.Value.packetQueue)
            {
                if (packet.Release())
                {
                    ReturnPacket(packet);
                }
            }
        }
        _loopbackStates.Clear();

        foreach (var loopbackAvatar in _loopbackAvatars)
        {
            _loopbackStates.Add(loopbackAvatar, new LoopbackState { requestedLod = _captureLOD });
        }
    }

    private void OnDestroy()
    {
        foreach (var item in _loopbackStates)
        {
            foreach (var packet in item.Value.packetQueue)
            {
                if (packet.Release())
                {
                    ReturnPacket(packet);
                }
            }
        }

        foreach (var packet in _packetPool)
        {
            if (packet is IDisposable disposablePacket)
            {
                disposablePacket.Dispose();
            }
        }
        _packetPool.Clear();
    }

    private void Update()
    {
        for (int i = 0; i < OvrAvatarEntity.StreamLODCount; ++i)
        {
            // Assume remote Avatar StreamLOD sizes are the same
            float streamBytesPerSecond = _localAvatar.GetLastByteSizeForLodIndex(i) / StreamLodSnapshotIntervalSeconds[i];
            AvatarLODManager.Instance.dynamicStreamLodBitsPerSecond[i] = (long)(streamBytesPerSecond * 8);
        }

        foreach (var item in _loopbackStates)
        {
            var loopbackAvatar = item.Key;
            var loopbackState = item.Value;

            if (!loopbackAvatar.IsCreated)
            {
                continue;
            }

            // "Remote" avatar receives incoming data and applies if it is the correct lod
            if (loopbackState.packetQueue.Count > 0)
            {
                foreach (var packet in loopbackState.packetQueue)
                {
                    packet.fakeLatency -= Time.deltaTime;

                    if (packet.fakeLatency <= 0f)
                    {
                        ProcessPacketData(loopbackAvatar, packet);
                        _deadList.Add(packet);
                    }
                }

                foreach (var packet in _deadList)
                {
                    loopbackState.packetQueue.Remove(packet);
                    if (packet.Release())
                    {
                        ReturnPacket(packet);
                    }
                }
                _deadList.Clear();
            }

            // "Send" the lod that "remote" avatar wants to use back over the network
            // TODO delay this reception for an accurate test
            loopbackState.requestedLod = loopbackAvatar.activeStreamLod;
        }
    }

    private void LateUpdate()
    {
        // Local avatar has fully updated this frame and can send data to the network
        SendSnapshot();
    }

    protected abstract PacketData GeneratePacketData(OvrAvatarEntity entity, StreamLOD lod);
    protected abstract void ProcessPacketData(OvrAvatarEntity entity, PacketData data);
    #endregion

    #region Local Avatar

    private void SendSnapshot()
    {
        if (!_localAvatar.HasJoints) { return; }

        for (int streamLod = (int)StreamLOD.Full; streamLod <= (int)StreamLOD.Low; ++streamLod)
        {
            int packetsSentThisFrame = 0;
            _streamLodSnapshotElapsedTime[streamLod] += Time.unscaledDeltaTime;
            while (_streamLodSnapshotElapsedTime[streamLod] > StreamLodSnapshotIntervalSeconds[streamLod])
            {
                SendPacket((StreamLOD)streamLod);
                _streamLodSnapshotElapsedTime[streamLod] -= StreamLodSnapshotIntervalSeconds[streamLod];
                if (++packetsSentThisFrame >= MAX_PACKETS_PER_FRAME)
                {
                    _streamLodSnapshotElapsedTime[streamLod] = 0;
                    break;
                }
            }
        }
    }

    private void SendPacket(StreamLOD lod)
    {
        var packet = GeneratePacketData(_localAvatar, lod);

        foreach (var loopbackState in _loopbackStates.Values)
        {
            if (loopbackState.requestedLod == lod)
            {
                if (!_simulatedLatencySettings.SkipNextPacket())
                {
                    packet.fakeLatency = _simulatedLatencySettings.NextValue();
                    loopbackState.packetQueue.Add(packet.Retain());
                }
            }
        }

        if (packet.Release())
        {
            ReturnPacket(packet);
        }
    }

    #endregion
}
