// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Utilities;
using Netcode.Transports.PhotonRealtime;
using Photon.Realtime;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Handles network connection using the Unity Netcode API as well as the Photon connection.
    /// It handles the connection, reconnection and disconnection of the user.
    /// Register to the callbacks to handle different events through the connection flow. 
    /// </summary>
    [RequireComponent(typeof(PhotonRealtimeTransport))]
    public class NetworkLayer : MonoBehaviour, IConnectionCallbacks, IInRoomCallbacks
    {
        public enum ClientState
        {
            Disconnected,
            Disconnecting,
            StartingLobby,
            StartingHost,
            StartingClient,
            MigratingHost,
            MigratingClient,
            RestoringHost,
            RestoringClient,
            SwitchingPhotonRealtimeRoom,
            SwitchingLobby,
            Connected,
            ConnectedToLobby,
        }

        // We register this function so that we can call it internally when we receive the message
        // in ReceiveServerToClientSetDisconnectReason_CustomMessage
        private static Action<int> s_onDisconnectReasonReceivedStatic;

        [SerializeField, AutoSet] private PhotonRealtimeTransport m_photonRealtime;
        [SerializeField] private int m_retryToRestoreClientCount = 1;

        private int m_restoreClientRetries = 0;

        public Action<ulong> OnClientConnectedCallback;
        public Action<ulong> OnClientDisconnectedCallback;
        public Func<ulong> OnMasterClientSwitchedCallback;
        public Action OnHostLeftAndStartingMigration;
        public Action StartHostCallback;
        public Action StartLobbyCallback;
        public Action StartClientCallback;
        public Action RestoreHostCallback;
        public Action RestoreClientCallback;
        public Action<int> OnRestoreFailedCallback;
        public Func<string> GetOnClientConnectingPayloadFunc;
        public Func<bool> CanMigrateAsHostFunc;

        public ClientState CurrentClientState { get; private set; } = ClientState.Disconnected;

        public string CurrentRoom => m_photonRealtime?.RoomName ?? m_photonRealtime?.Client?.CurrentRoom?.Name;

        public List<Region> EnabledRegions { get; private set; }

        private bool m_callFailureOnDisconnect = false;
        private int m_failureCode = 0;

        private void OnEnable()
        {
            DontDestroyOnLoad(this);
            // Assign the OnDisconnectReasonReceived to keep track of it when
            // ReceiveServerToClientSetDisconnectReason_CustomMessage is called
            s_onDisconnectReasonReceivedStatic = OnDisconnectReasonReceived;
        }

        public string GetRegion()
        {
            return m_photonRealtime.Client?.CloudRegion;
        }

        public void SetRegion(string region)
        {
            m_photonRealtime.RegionOverride = region;
            if (CurrentClientState == ClientState.ConnectedToLobby)
            {
                CurrentClientState = ClientState.SwitchingLobby;
                m_photonRealtime.Shutdown();
            }
        }

        public void SetRoomProperty(ExitGames.Client.Photon.Hashtable properties)
        {
            _ = m_photonRealtime.Client.CurrentRoom.SetCustomProperties(properties);
        }

        public void Init(string room, string region)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            m_photonRealtime.RoomName = room;
            if (string.IsNullOrEmpty(room))
            {
                _ = StartCoroutine(StartLobby());
            }
            else
            {
                // On init if we fail to load client we want to host a private game
                // this failure will mostly come from in Editor when AutoJoining a specific room as teh first player.
                m_photonRealtime.UsePrivateRoom = true;
                m_photonRealtime.RegionOverride = region;
                _ = StartCoroutine(StartClient());
            }
        }

        public void GoToLobby()
        {
            _ = StartCoroutine(StartLobby());
        }

        private IEnumerator StartLobby()
        {
            CurrentClientState = ClientState.StartingLobby;

            _ = m_photonRealtime.StartLobby();
            m_photonRealtime.Client.AddCallbackTarget(this);

            yield return new WaitUntil(() => m_photonRealtime.Client.InLobby);

            CurrentClientState = ClientState.ConnectedToLobby;

            StartLobbyCallback?.Invoke();

            Debug.LogWarning("You are in the Lobby.");
        }

        private IEnumerator StartHost()
        {
            CurrentClientState = ClientState.StartingHost;

            _ = NetworkManager.Singleton.StartHost();
            m_photonRealtime.Client.AddCallbackTarget(this);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            yield return new WaitUntil(() => m_photonRealtime.Client.InRoom);
            if (CurrentClientState != ClientState.StartingHost)
                yield break;

            CurrentClientState = ClientState.Connected;

            StartHostCallback.Invoke();

            Debug.LogWarning("You are the host.");
            yield break;
        }

        private IEnumerator StartClient()
        {
            CurrentClientState = ClientState.StartingClient;

            var networkManager = NetworkManager.Singleton;
            if (GetOnClientConnectingPayloadFunc != null)
            {
                var payload = GetOnClientConnectingPayloadFunc();
                var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

                networkManager.NetworkConfig.ConnectionData = payloadBytes;
            }

            _ = networkManager.StartClient();
            m_photonRealtime.Client.AddCallbackTarget(this);

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage),
                ReceiveServerToClientSetDisconnectReason_CustomMessage);

            yield return WaitForLocalPlayerObject();
            if (CurrentClientState != ClientState.StartingClient)
                yield break;

            CurrentClientState = ClientState.Connected;

            StartClientCallback.Invoke();

            Debug.LogWarning("You are a client.");
        }

        private static WaitUntil WaitForLocalPlayerObject()
        {
            return new WaitUntil(() => NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);
        }

        private IEnumerator RestoreHost()
        {
            Debug.LogWarning("Restore Host.");
            _ = NetworkManager.Singleton.StartHost();
            m_photonRealtime.Client.AddCallbackTarget(this);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            yield return new WaitUntil(() => m_photonRealtime.Client.InRoom);
            if (CurrentClientState != ClientState.RestoringHost)
                yield break;
            Debug.LogWarning("Restore Host Connected.");
            CurrentClientState = ClientState.Connected;

            RestoreHostCallback.Invoke();

            Debug.LogWarning("You are the host.");
        }

        private IEnumerator RestoreClient()
        {
            Debug.LogWarning("Restore Client.");
            // we need to delay the restore so that the host can be restored first and create the room
            yield return new WaitForSeconds(1f);
            m_photonRealtime.RetriesClient = 3; // We also want to add retries in case the room is not ready
            _ = NetworkManager.Singleton.StartClient();
            m_photonRealtime.Client.AddCallbackTarget(this);

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage),
                ReceiveServerToClientSetDisconnectReason_CustomMessage);

            yield return WaitForLocalPlayerObject();
            if (CurrentClientState != ClientState.RestoringClient)
                yield break;
            m_photonRealtime.RetriesClient = 0;
            Debug.LogWarning("Restore Client Connected.");
            CurrentClientState = ClientState.Connected;

            RestoreClientCallback.Invoke();

            Debug.LogWarning("You are a client.");
        }

        private IEnumerator RestoreClientFailed()
        {
            yield return null;
            OnRestoreFailedCallback?.Invoke(m_failureCode);
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"OnDisconnected: {cause}");

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            // For some reason, sometimes another shutdown is coming,
            // but won't progress unless you start the client.
            if (cause is DisconnectCause.DisconnectByClientLogic && NetworkManager.Singleton.ShutdownInProgress)
            {
                _ = NetworkManager.Singleton.StartClient();
                m_photonRealtime.Client.AddCallbackTarget(this);
                return;
            }

            if (m_callFailureOnDisconnect)
            {
                m_callFailureOnDisconnect = false;
                CurrentClientState = ClientState.Disconnected;
                _ = StartCoroutine(RestoreClientFailed());
                return;
            }

            switch (CurrentClientState)
            {
                case ClientState.StartingHost:
                    // happened because of room name conflict, meaning this 
                    // photon room already exist, so join as a client instead
                    Debug.LogWarning("HOSTING FAILED. ATTEMPTING TO JOIN AS CLIENT INSTEAD.");
                    CurrentClientState = ClientState.StartingClient;
                    StopCoroutine(StartHost());
                    _ = StartCoroutine(StartClient());
                    break;

                case ClientState.StartingClient:
                    // happened because room may have stopped being hosted while joining it
                    if (!CanMigrateAsHost())
                    {
                        Debug.LogWarning("JOINING AS CLIENT FAILED AS NON MIGRATING TO HOST, CALL RESTORE CLIENT FAILED.");
                        _ = StartCoroutine(RestoreClientFailed());
                    }
                    else
                    {
                        Debug.LogWarning("JOINING AS CLIENT FAILED. ATTEMPTING TO HOST INSTEAD.");
                        CurrentClientState = ClientState.StartingHost;
                        StopCoroutine(StartClient());
                        _ = StartCoroutine(StartHost());
                    }

                    break;

                case ClientState.MigratingHost:
                    Debug.LogWarning("MIGRATING AS HOST.");
                    CurrentClientState = ClientState.RestoringHost;
                    _ = StartCoroutine(RestoreHost());
                    break;

                case ClientState.MigratingClient:
                    // there is a possibility that client migration fails while the fallback host
                    // is taking over as host, meaning this codepath might be taken more than once
                    Debug.LogWarning("MIGRATING AS CLIENT.");
                    m_restoreClientRetries = 0;
                    CurrentClientState = ClientState.RestoringClient;
                    _ = StartCoroutine(RestoreClient());
                    break;

                case ClientState.RestoringHost:
                    Debug.LogWarning("RESTORING HOST FAILED. RESTORING AS CLIENT INSTEAD.");
                    CurrentClientState = ClientState.RestoringClient;
                    StopCoroutine(RestoreHost());
                    _ = StartCoroutine(RestoreClient());
                    break;

                case ClientState.RestoringClient:
                    StopCoroutine(RestoreClient());
                    // if we don't restore as host try as client again
                    if (!CanMigrateAsHost())
                    {
                        m_restoreClientRetries++;
                        if (m_restoreClientRetries <= m_retryToRestoreClientCount)
                        {
                            Debug.LogWarning("RESTORING CLIENT FAILED AS NON MIGRATING TO HOST. TRYING AGAIN.");
                            _ = StartCoroutine(RestoreClient());
                        }
                        else
                        {
                            Debug.LogWarning($"RESTORING CLIENT FAILED AS NON MIGRATING TO HOST. FAILED AFTER {m_restoreClientRetries} tries.");
                            _ = StartCoroutine(RestoreClientFailed());
                        }
                    }
                    else
                    {
                        Debug.LogWarning("RESTORING CLIENT FAILED. RESTORING AS HOST INSTEAD.");
                        CurrentClientState = ClientState.RestoringHost;
                        _ = StartCoroutine(RestoreHost());
                    }

                    break;

                case ClientState.SwitchingPhotonRealtimeRoom:
                    Debug.LogWarning("SWITCHING ROOM.");
                    if (CurrentRoom == null)
                    {
                        CurrentClientState = ClientState.StartingHost;
                        _ = StartCoroutine(StartHost());
                    }
                    else
                    {
                        CurrentClientState = ClientState.StartingClient;
                        _ = StartCoroutine(StartClient());
                    }
                    break;

                case ClientState.Connected:
                    if (cause == DisconnectCause.ServerTimeout)
                    {
                        Debug.LogWarning("SERVER TIMEOUT. RESTORING AS CLIENT.");
                        CurrentClientState = ClientState.RestoringClient;
                        _ = StartCoroutine(RestoreClient());
                    }
                    break;
                case ClientState.StartingLobby:
                    Debug.LogWarning("DISCONNECTED FROM LOBBY.");
                    break;
                case ClientState.ConnectedToLobby:
                    if (cause == DisconnectCause.ServerTimeout)
                    {
                        Debug.LogWarning("SERVER TIMEOUT IN LOBBY. RESTORING IN LOBBY.");
                        CurrentClientState = ClientState.StartingLobby;
                        _ = StartCoroutine(StartLobby());
                    }
                    break;
                case ClientState.Disconnecting:
                    CurrentClientState = ClientState.Disconnected;
                    break;

                case ClientState.SwitchingLobby:
                    Debug.LogWarning("SWITCHING TO NEW LOBBY REGION.");
                    CurrentClientState = ClientState.StartingLobby;
                    _ = StartCoroutine(StartLobby());
                    break;
                case ClientState.Disconnected:
                    break;
                default:
                    break;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            m_photonRealtime.Client.LoadBalancingPeer.SendInCreationOrder = false;

            IEnumerator Routine()
            {
                // For some reason, OnClientConnectedCallback is called before OnServerStarted, so wait
                if (NetworkManager.Singleton.IsHost)
                    yield return new WaitUntil(() => CurrentClientState == ClientState.Connected);
                OnClientConnectedCallback.Invoke(clientId);
            }
            _ = StartCoroutine(Routine());
        }

        private void OnClientDisconnected(ulong clientId)
        {
            OnClientDisconnectedCallback.Invoke(clientId);
        }

        public void Leave()
        {
            if (CurrentClientState == ClientState.Connected)
            {
                CurrentClientState = ClientState.Disconnecting;
                NetworkManager.Singleton.Shutdown();
            }
            else
            {
                CurrentClientState = ClientState.Disconnected;
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.LogWarning("HOST LEFT, MIGRATING...");

            CurrentClientState = OnMasterClientSwitchedCallback() == NetworkManager.Singleton.LocalClientId && CanMigrateAsHost()
                ? ClientState.MigratingHost
                : ClientState.MigratingClient;
            OnHostLeftAndStartingMigration?.Invoke();
        }

        private bool CanMigrateAsHost()
        {
            // If no function provided we can migrate as an Host
            return CanMigrateAsHostFunc == null || CanMigrateAsHostFunc();
        }

        public void SwitchPhotonRealtimeRoom(string room, bool isHosting, string region)
        {
            m_photonRealtime.RoomName = room;
            m_photonRealtime.RegionOverride = region;
            if (m_photonRealtime.Client.InRoom)
            {
                CurrentClientState = ClientState.SwitchingPhotonRealtimeRoom;

                NetworkManager.Singleton.Shutdown();
            }
            else
            {
                if (isHosting)
                {
                    // When starting has host we want to use private rooms
                    m_photonRealtime.UsePrivateRoom = true;
                    _ = StartCoroutine(StartHost());
                }
                else
                {
                    m_photonRealtime.UsePrivateRoom = false;
                    _ = StartCoroutine(StartClient());
                }
            }
        }

        private void OnDisconnectReasonReceived(int failureCode)
        {
            m_failureCode = failureCode;
            m_callFailureOnDisconnect = true;
        }

        public static void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int status);
            s_onDisconnectReasonReceivedStatic(status);
        }

        // no need to implement them at the moment
        public void OnConnected() { }
        public void OnConnectedToMaster() { }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            EnabledRegions = regionHandler.EnabledRegions;
        }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }
        public void OnPlayerEnteredRoom(Player newPlayer) { }
        public void OnPlayerLeftRoom(Player otherPlayer) { }
        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    }
}