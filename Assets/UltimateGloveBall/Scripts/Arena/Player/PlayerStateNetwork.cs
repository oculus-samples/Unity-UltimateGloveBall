// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Multiplayer.Core;
using Meta.Utilities;
using Meta.XR.Samples;
using UltimateGloveBall.App;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Keeps track of the players state and sync's it over the network to other client.
    /// It also handles updating the player name visual and the voip mute/unmute state. 
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class PlayerStateNetwork : NetworkBehaviour
    {
        [SerializeField] private PlayerNameVisual m_playerNameVisual;
        [SerializeField] private bool m_enableLocalPlayerName;
        [SerializeField] private VoipHandler m_voipHandler;
        [SerializeField, AutoSet] private CatOwner m_catOwner;

        private NetworkVariable<FixedString128Bytes> m_username = new(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<ulong> m_userId = new(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> m_isMasterClient = new(
            true,
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<FixedString128Bytes> m_userIconSku = new(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> m_hasACat = new(
            writePerm: NetworkVariableWritePermission.Owner);

        public string Username => m_username.Value.ToString();
        public ulong UserId => m_userId.Value;

        public VoipHandler VoipHandler => m_voipHandler;

        private LocalPlayerState LocalPlayerState => IsOwner ? LocalPlayerState.Instance : null;

        private void Start()
        {
            OnUsernameChanged(m_username.Value, m_username.Value);
            OnUserIdChanged(m_userId.Value, m_userId.Value);
            OnMasterClientChanged(m_isMasterClient.Value, m_isMasterClient.Value);
            OnUserIconChanged(m_userIconSku.Value, m_userIconSku.Value);
            OnUserCatOwnershipChanged(m_hasACat.Value, m_hasACat.Value);

            UserMutingManager.Instance.RegisterCallback(OnUserMuteStateChanged);

            if (!LocalPlayerState) return;

            // We snap local player rig to the spawned position of this player.
            PlayerMovement.Instance.SnapPositionToTransform(transform);
            LocalPlayerState.OnChange += UpdateData;
            LocalPlayerState.OnSpawnCatChange += OnSpawnCatChanged;

            UpdateData();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            UserMutingManager.Instance.UnregisterCallback(OnUserMuteStateChanged);

            if (m_catOwner)
            {
                m_catOwner.DeSpawnCat();
            }

            if (!LocalPlayerState) return;

            var thisTransform = transform;
            var playerTransform = LocalPlayerState.transform;
            playerTransform.position = thisTransform.position;
            playerTransform.rotation = thisTransform.rotation;

            LocalPlayerState.OnChange -= UpdateData;
            LocalPlayerState.OnSpawnCatChange -= OnSpawnCatChanged;
        }

        private void OnEnable()
        {
            m_username.OnValueChanged += OnUsernameChanged;
            m_userId.OnValueChanged += OnUserIdChanged;
            m_isMasterClient.OnValueChanged += OnMasterClientChanged;
            m_userIconSku.OnValueChanged += OnUserIconChanged;
            m_hasACat.OnValueChanged += OnUserCatOwnershipChanged;

            m_playerNameVisual?.SetEnableState(m_enableLocalPlayerName);
        }

        private void OnDisable()
        {
            m_username.OnValueChanged -= OnUsernameChanged;
            m_userId.OnValueChanged -= OnUserIdChanged;
            m_isMasterClient.OnValueChanged -= OnMasterClientChanged;
            m_userIconSku.OnValueChanged -= OnUserIconChanged;
            m_hasACat.OnValueChanged += OnUserCatOwnershipChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_playerNameVisual?.SetEnableState(m_enableLocalPlayerName || LocalPlayerState == null);

            if (LocalPlayerState)
            {
                // When object is spawned we snap local player rig to the spawned position of this player.
                PlayerMovement.Instance.SnapPositionToTransform(transform);

                SetState(LocalPlayerState.Username, LocalPlayerState.UserId, LocalPlayerState.UserIconSku,
                    LocalPlayerState.SpawnCatInNextGame);
                SetIsMaster(IsHost);
            }
        }

        private void UpdateData()
        {
            SetState(LocalPlayerState.Username, LocalPlayerState.UserId, LocalPlayerState.UserIconSku,
                LocalPlayerState.SpawnCatInNextGame);
        }

        private void OnSpawnCatChanged()
        {
            m_hasACat.Value = LocalPlayerState.SpawnCatInNextGame;
        }

        private void SetState(string username, ulong userId, string userIcon, bool ownsCat)
        {
            m_username.Value = username;
            m_userId.Value = userId;
            m_userIconSku.Value = userIcon;
            m_hasACat.Value = ownsCat;
        }

        private void SetIsMaster(bool isMasterClient)
        {
            m_isMasterClient.Value = isMasterClient;
        }

        private void OnUserIdChanged(ulong prevUserId, ulong newUserId)
        {
            if (newUserId != 0)
            {
                m_voipHandler.IsMuted = BlockUserManager.Instance.IsUserBlocked(newUserId) ||
                                        UserMutingManager.Instance.IsUserMuted(newUserId);
            }
        }

        private void OnUsernameChanged(FixedString128Bytes oldName, FixedString128Bytes newName)
        {
            m_playerNameVisual?.SetUsername(newName.ConvertToString());
        }

        private void OnUserIconChanged(FixedString128Bytes oldIcon, FixedString128Bytes newIcon)
        {
            if (m_playerNameVisual != null)
            {
                var iconSku = newIcon.ConvertToString();
                var icon = UserIconManager.Instance.GetIconForSku(iconSku);
                m_playerNameVisual.SetUserIcon(icon);
            }
        }

        private void OnUserCatOwnershipChanged(bool oldValue, bool newValue)
        {
            if (m_catOwner != null)
            {
                if (newValue)
                {
                    m_catOwner.SpawnCat();
                }
                else
                {
                    m_catOwner.DeSpawnCat();
                }
            }
        }

        private void OnMasterClientChanged(bool oldVal, bool newVal)
        {
            m_playerNameVisual?.ShowMasterIcon(newVal);
        }

        private void OnUserMuteStateChanged(ulong userId, bool isMuted)
        {
            if (userId == m_userId.Value)
            {
                m_voipHandler.IsMuted = isMuted || BlockUserManager.Instance.IsUserBlocked(userId);
            }
        }
    }
}