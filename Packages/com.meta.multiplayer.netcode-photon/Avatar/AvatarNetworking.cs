// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Utilities;
using Meta.Multiplayer.Core;
using Unity.Netcode;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// Handles the networking of the Avatar.
    /// Local avatars will send their state to other users through rpcs.
    /// For remote avatars we receive the state rpc and apply it to the avatar entity.
    /// </summary>
    public class AvatarNetworking : NetworkBehaviour
    {
        private const float PLAYBACK_SMOOTH_FACTOR = 0.25f;

        [Serializable]
        private struct LodFrequency
        {
            public StreamLOD LOD;
            public float UpdateFrequency;
        }

        [SerializeField] private List<LodFrequency> m_updateFrequenySecondsByLodList;
        [SerializeField] private float m_streamDelayMultiplier = 0.5f;

        private NetworkVariable<ulong> m_userId = new(
            ulong.MaxValue,
            writePerm: NetworkVariableWritePermission.Owner);

        private Stopwatch m_streamDelayWatch = new();
        private float m_currentStreamDelay;

        private Dictionary<StreamLOD, float> m_updateFrequencySecondsByLod;
        private Dictionary<StreamLOD, double> m_lastUpdateTime = new();
        [SerializeField, AutoSet] private AvatarEntity m_entity;

        public ulong UserId
        {
            get => m_userId.Value;
            set => m_userId.Value = value;
        }

        public void Init()
        {
            m_updateFrequencySecondsByLod = new Dictionary<StreamLOD, float>();
            foreach (var val in m_updateFrequenySecondsByLodList)
            {
                m_updateFrequencySecondsByLod[val.LOD] = val.UpdateFrequency;
                m_lastUpdateTime[val.LOD] = 0;
            }
            if (!m_entity.IsLocal)
            {
                m_userId.OnValueChanged += OnUserIdChanged;

                if (m_userId.Value != ulong.MaxValue)
                    OnUserIdChanged(ulong.MaxValue, m_userId.Value);
            }
        }

        private void OnUserIdChanged(ulong previousValue, ulong newValue)
        {
            m_entity.LoadUser(newValue);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_userId.OnValueChanged?.Invoke(ulong.MaxValue, m_userId.Value);
            m_entity.Initialize();
        }

        private void Update()
        {
            if (m_entity && m_entity.IsLocal)
            {
                var rigTransform = CameraRigRef.Instance.transform;
                transform.SetPositionAndRotation(
                    rigTransform.position,
                    rigTransform.rotation);

                UpdateDataStream();
            }
        }

        private void UpdateDataStream()
        {
            if (isActiveAndEnabled)
            {
                if (m_entity.IsCreated && m_entity.HasJoints && NetworkObject?.IsSpawned is true)
                {
                    var now = Time.unscaledTimeAsDouble;
                    var lod = StreamLOD.Low;
                    double timeSinceLastUpdate = default;
                    foreach (var lastUpdateKvp in m_lastUpdateTime)
                    {
                        var lastLod = lastUpdateKvp.Key;
                        var time = now - lastUpdateKvp.Value;
                        var frequency = m_updateFrequencySecondsByLod[lastLod];
                        if (time >= frequency)
                        {
                            if (time > timeSinceLastUpdate)
                            {
                                timeSinceLastUpdate = time;
                                lod = lastLod;
                            }
                        }
                    }

                    if (timeSinceLastUpdate != default)
                    {
                        // act like every lower frequency lod got updated too
                        var lodFrequency = m_updateFrequencySecondsByLod[lod];
                        foreach (var lodFreqKvp in m_updateFrequencySecondsByLod)
                        {
                            if (lodFreqKvp.Value <= lodFrequency)
                            {
                                m_lastUpdateTime[lodFreqKvp.Key] = now;
                            }
                        }

                        SendAvatarData(lod);
                    }
                }
            }
        }

        private void SendAvatarData(StreamLOD lod)
        {
            var bytes = m_entity.RecordStreamData(lod);
            SendAvatarData_ServerRpc(bytes);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ServerRpc(byte[] data, ServerRpcParams args = default)
        {
            var allClients = NetworkManager.Singleton.ConnectedClientsIds;
            var targetClients = allClients.Except(args.Receive.SenderClientId).ToTempArray(allClients.Count - 1);
            SendAvatarData_ClientRpc(data, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIdsNativeArray = targetClients } });
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ClientRpc(byte[] data, ClientRpcParams args)
        {
            ReceiveAvatarData(data);
        }

        private void ReceiveAvatarData(byte[] data)
        {
            if (!m_entity)
            {
                return;
            }

            var latency = (float)m_streamDelayWatch.Elapsed.TotalSeconds;

            m_entity.ApplyStreamData(data);

            var delay = Mathf.Clamp01(latency * m_streamDelayMultiplier);
            m_currentStreamDelay = Mathf.LerpUnclamped(m_currentStreamDelay, delay, PLAYBACK_SMOOTH_FACTOR);
            m_entity.SetPlaybackTimeDelay(m_currentStreamDelay);
            m_streamDelayWatch.Restart();
        }
    }
}
