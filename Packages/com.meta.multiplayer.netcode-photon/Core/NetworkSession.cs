// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Keeps the state of the current network session, sharing the state from the server to the clients 
    /// Handles resolving the fallback host and voice room name.
    /// </summary>
    public class NetworkSession : NetworkBehaviour
    {
        public static ulong FallbackHostId { get; private set; } = ulong.MaxValue;
        public static string PhotonVoiceRoom { get; private set; } = "";

        private void Awake()
        {
            FallbackHostId = ulong.MaxValue;
        }

        public override void OnDestroy()
        {
            PhotonVoiceRoom = "";
            base.OnDestroy();
        }

        #region FallbackHost
        public void DetermineFallbackHost(ulong clientId)
        {
            // if the new client that joined has a smaller id, 
            // make them the new fallback host
            if (clientId < FallbackHostId)
            {
                // broadcast to all clients
                SetFallbackHostClientRpc(clientId);
            }
            // this new client that joined didn't change the fallback host information.
            // just send the current fallback host information to this new client.
            else
            {
                // only broadcast to new client
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };

                SetFallbackHostClientRpc(FallbackHostId, clientRpcParams);
            }
        }

        public void RedetermineFallbackHost(ulong clientId)
        {
            // if true, not the fallback host that left
            if (clientId != FallbackHostId) return;

            // reset fallback host id
            FallbackHostId = ulong.MaxValue;

            // if true, only original host is left
            if (NetworkManager.Singleton.ConnectedClients.Count < 2) return;

            // the fallbackhost left, pick another client to be the host
            foreach (var id in NetworkManager.Singleton.ConnectedClients.Keys)
            {
                // server or the disconnecting client can't be fallback hosts
                if (id == NetworkManager.ServerClientId || id == clientId)
                    continue;

                if (id < FallbackHostId)
                    FallbackHostId = id;
            }

            // broadcast new fallback host to all clients
            SetFallbackHostClientRpc(FallbackHostId);
        }

        [ClientRpc]
        private void SetFallbackHostClientRpc(ulong fallbackHostId, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.IsHost && fallbackHostId == NetworkManager.Singleton.LocalClientId)
            {
                // we don't fallback to current host
                return;
            }

            if (fallbackHostId == FallbackHostId)
            {
                return;
            }

            FallbackHostId = fallbackHostId;

            Debug.Log("------FALLBACK HOST STATE-------");
            Debug.Log("Client ID: " + fallbackHostId.ToString());

            if (fallbackHostId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("You are the new fallback host");
            }
            Debug.Log("--------------------------------");
        }
        #endregion // FallbackHost

        #region PhotonVoiceRoom

        public void SetPhotonVoiceRoom(string voiceRoomName)
        {
            PhotonVoiceRoom = voiceRoomName;
        }

        public void UpdatePhotonVoiceRoomToClient(ulong clientId)
        {
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            SetPhotonVoiceRoomClientRpc(PhotonVoiceRoom, clientRpcParams);
        }

        [ClientRpc]
        private void SetPhotonVoiceRoomClientRpc(string photonVoiceRoom, ClientRpcParams clientRpcParams)
        {
            Debug.Log("PHOTON VOICE ROOM TO JOIN: " + photonVoiceRoom);
            PhotonVoiceRoom = photonVoiceRoom;
        }

        #endregion
    }
}