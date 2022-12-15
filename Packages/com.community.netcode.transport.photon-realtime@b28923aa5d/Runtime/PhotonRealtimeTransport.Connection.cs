using System.Collections.Generic;
using Photon.Realtime;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Netcode.Transports.PhotonRealtime
{
    public partial class PhotonRealtimeTransport : IConnectionCallbacks
    {
        public Func<bool, byte, RoomOptions> GetHostRoomOptionsFunc;
        public Func<byte, OpJoinRandomRoomParams> GetRandomRoomParamsFunc;

        public void OnConnected()
        {
        }

        public void OnConnectedToMaster()
        {
            HandleConnectionIntent();
        }

        private bool HandleConnectionIntent()
        {
            if (Client.InLobby)
            {
                Client.OpLeaveLobby();
            }

            if (Client.InRoom)
            {
                Client.OpLeaveRoom(false);
            }
            
            switch (connectionIntent)
            {
                case ConnectionIntent.Lobby:
                    return ConnectToLobby();
                
                case ConnectionIntent.HostOrServer:
                case ConnectionIntent.Client:
                    return ConnectToRoom();
                
                default:
                case ConnectionIntent.None:
                    // nothing to be done
                    break;
            }

            return false;
        }

        private bool ConnectToLobby()
        {
            var success = m_Client.OpJoinLobby(null);

            if (!success)
            {
                Debug.LogWarning("Unable to connect to Photon Lobby.");
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }

            return success;
        }
        
        private bool ConnectToRoom()
        {
            var randomRoom = string.IsNullOrEmpty(m_RoomName);

            // Once the client does connect to the master immediately redirect to its room.
            bool success = false;
            if (m_IsHostOrServer)
            {
                RoomOptions roomOptions;
                if (GetHostRoomOptionsFunc != null)
                {
                    roomOptions = GetHostRoomOptionsFunc(m_UsePrivateRoom, m_MaxPlayers);
                }
                else
                {
                    roomOptions = new RoomOptions();
                    roomOptions.MaxPlayers = m_MaxPlayers;
                }
                
                var enterRoomParams = new EnterRoomParams()
                {
                    RoomName = m_RoomName,
                    RoomOptions = roomOptions,
                };
                success = m_Client.OpCreateRoom(enterRoomParams);
            }
            else if (randomRoom)
            {
                OpJoinRandomRoomParams opJoinRandomRoomParams;
                if (GetRandomRoomParamsFunc != null)
                {
                    opJoinRandomRoomParams = GetRandomRoomParamsFunc(m_MaxPlayers);
                }
                else
                {
                    opJoinRandomRoomParams = new OpJoinRandomRoomParams();
                    opJoinRandomRoomParams.ExpectedMaxPlayers = m_MaxPlayers;
                }
                success = m_Client.OpJoinRandomRoom(opJoinRandomRoomParams);
            }
            else
            {
                // join room by name
                var enterRoomParams = new EnterRoomParams()
                {
                    RoomName = m_RoomName,
                    RoomOptions = new RoomOptions() 
                    {
                        MaxPlayers = m_MaxPlayers,
                    },
                };
                success = m_Client.OpJoinRoom(enterRoomParams);
            }

            if (!success)
            {
                Debug.LogWarning("Unable to create or join room.");
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }
            return success;
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            InvokeTransportEvent(NetworkEvent.Disconnect);
            this.DeInitialize();
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
        }
    }
}
