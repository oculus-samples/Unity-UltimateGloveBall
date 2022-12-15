using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Unity.Netcode;

namespace Netcode.Transports.PhotonRealtime
{
    public partial class PhotonRealtimeTransport : IMatchmakingCallbacks
    {
        /// <summary>
		/// Gets the current Master client of the current Room.
		/// </summary>
		/// <returns>The master client ID if the client in inside a Room, -1 otherwise.</returns>
		private int CurrentMasterId => this.m_Client != null && this.m_Client.CurrentRoom != null ? this.m_Client.CurrentRoom.MasterClientId : -1;

        /// <summary>Photon ActorNumber of the host/server.</summary>
        private int m_originalRoomMasterClient = -1;

        public int RetriesClient { get; set; }= 0;

        public void OnCreatedRoom()
        {
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Create Room Failed: {message}");
            InvokeTransportEvent(NetworkEvent.Disconnect);
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
        }

        public void OnJoinedRoom()
        {
            RetriesClient = 0; // reset retry count
            Debug.Log($"OnJoinedRoom {m_IsHostOrServer}");
            Debug.LogFormat("Caching Original Master Client: {0}", CurrentMasterId);
            m_originalRoomMasterClient = CurrentMasterId;

            // any client (except host/server) need to know about their own join event
            if (!m_IsHostOrServer)
            {
                NetworkEvent netEvent = NetworkEvent.Connect;
                InvokeTransportEvent(netEvent, GetMlapiClientId(m_originalRoomMasterClient, false));
            }

            m_RoomName = Client.CurrentRoom.Name;
            // we update the state of private room in case of host migration
            m_UsePrivateRoom = !Client.CurrentRoom.IsVisible;
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Join Room Failed: {message}");
            InvokeTransportEvent(NetworkEvent.Disconnect);
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Join Room Failed: {message}");
            if (RetriesClient > 0 && connectionIntent == ConnectionIntent.Client)
            {
                RetriesClient--;
                Debug.LogWarning($"Retry Client - remaining({RetriesClient})");

                IEnumerator RetryClientConnection()
                {
                    yield return new WaitForSeconds(1f);
                    HandleConnectionIntent();
                }
                StartCoroutine(RetryClientConnection());
            }
            else
            {
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }
        }

        public void OnLeftRoom()
        {
            // any client (except host/server) need to know about their own leave event
            if (!this.m_IsHostOrServer)
            {
                NetworkEvent netEvent = NetworkEvent.Connect;
                InvokeTransportEvent(netEvent, GetMlapiClientId(m_originalRoomMasterClient, false));
            }
        }
    }
}