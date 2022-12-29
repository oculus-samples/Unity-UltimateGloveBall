// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.Utilities;
using Meta.Multiplayer.Core;
using Oculus.Avatar2;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Oculus.Avatar2.CAPI;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// The AvatarEntity handles the setup of the Avatar, adds functionalities to the base class OvrAvatarEntity.
    /// On Joint loaded callbacks
    /// Set up lipsync and body tracking.
    /// Paired with the AvatarNetworking it initializes it to support networking in a multiplayer project.
    /// In a not networked setup, it will track the camera rig to keep the position in sync.
    /// </summary>
    [DefaultExecutionOrder(50)] // after GpuSkinningConfiguration initializes
    public class AvatarEntity : OvrAvatarEntity
    {
        [Serializable]
        public struct OnJointLoadedPair
        {
            public ovrAvatar2JointType Joint;
            public Transform TargetToSetAsChild;
            public UnityEvent<Transform> OnLoaded;
        }

        [SerializeField, AutoSet] private NetworkObject m_networkObject;
        [SerializeField, AutoSet] private AvatarNetworking m_networking;

        [SerializeField, AutoSetFromChildren(IncludeInactive = true)]
        private OvrAvatarLipSyncBehavior m_lipSync;

        [SerializeField] private bool m_isLocalIfNotNetworked;

        public List<OnJointLoadedPair> OnJointLoadedEvents = new();

        public Transform GetJointTransform(ovrAvatar2JointType jointType) => GetSkeletonTransformByType(jointType);

        [Header("Face Pose Input")]
        [SerializeField, AutoSet]
        private OvrAvatarFacePoseBehavior m_facePoseProvider;
        [SerializeField, AutoSet]
        private OvrAvatarEyePoseBehavior m_eyePoseProvider;

        private Task m_initializationTask;
        public Task m_setUpAccessTokenTask;

        protected override void Awake()
        {
            m_setUpAccessTokenTask = SetUpAccessTokenAsync();
            base.Awake();
            OVRPlugin.StartFaceTracking();
            OVRPlugin.StartEyeTracking();
        }

        private void Start()
        {
            if ((m_networkObject == null || m_networkObject.NetworkManager == null || !m_networkObject.NetworkManager.IsListening) && m_isLocalIfNotNetworked)
            {
                Initialize();
            }
        }

        private async Task SetUpAccessTokenAsync()
        {
            var accessToken = await Users.GetAccessToken().Gen();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Data);
        }

        public void Initialize()
        {
            var prevInit = m_initializationTask;
            m_initializationTask = Impl();

            async Task Impl()
            {
                if (prevInit != null)
                    await prevInit;
                await InitializeImpl();
            }
        }

        private async Task InitializeImpl()
        {
            Teardown();

            var isOwner = m_networkObject == null || (m_networkObject != null && !m_networkObject.NetworkManager.IsClient) ? m_isLocalIfNotNetworked : m_networkObject.IsOwner;

            SetIsLocal(isOwner);
            if (isOwner)
            {
                _creationInfo.features |= Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Animation;

                var body = CameraRigRef.Instance.AvatarInputManager;
                SetBodyTracking(body);

                m_lipSync.gameObject.SetActive(true);
                SetLipSync(m_lipSync);

                SetFacePoseProvider(m_facePoseProvider);
                SetEyePoseProvider(m_eyePoseProvider);

                AvatarLODManager.Instance.firstPersonAvatarLod = AvatarLOD;
            }
            else
            {
                _creationInfo.features &= ~ovrAvatar2EntityFeatures.Animation;

                SetBodyTracking(null);
                SetFacePoseProvider(null);
                SetEyePoseProvider(null);
                SetLipSync(null);
            }

            _creationInfo.renderFilters.viewFlags = isOwner ? Oculus.Avatar2.CAPI.ovrAvatar2EntityViewFlags.FirstPerson : Oculus.Avatar2.CAPI.ovrAvatar2EntityViewFlags.ThirdPerson;

            CreateEntity();

            SetActiveView(_creationInfo.renderFilters.viewFlags);

            await m_setUpAccessTokenTask;

            if (m_networking != null)
            {
                m_networking.Init();
            }

            if (IsLocal)
            {
                var user = await Users.GetLoggedInUser().Gen();
                _userId = user.Data.ID;
                if (m_networking)
                {
                    m_networking.UserId = _userId;
                }

                LoadUser();

                if (!m_networking)
                {
                    UpdatePositionToCamera();
                    StartCoroutine(TrackCamera());
                }
            }
            else if (_userId != 0)
            {
                LoadUser();
            }
        }

        public void LoadUser(ulong userId)
        {
            if (_userId != userId)
            {
                _userId = userId;
                LoadUser();
            }
        }

        public void Show()
        {
            SetActiveView(!m_networkObject.IsOwner
                ? ovrAvatar2EntityViewFlags.ThirdPerson
                : ovrAvatar2EntityViewFlags.FirstPerson);
        }

        public void Hide()
        {
            SetActiveView(ovrAvatar2EntityViewFlags.None);
        }

        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();

            foreach (var evt in OnJointLoadedEvents)
            {
                var jointTransform = GetJointTransform(evt.Joint);
                if (evt.TargetToSetAsChild != null)
                {
                    evt.TargetToSetAsChild.SetParent(jointTransform, false);
                }

                evt.OnLoaded?.Invoke(jointTransform);
            }
        }

        private IEnumerator TrackCamera()
        {
            while (true)
            {
                UpdatePositionToCamera();
                yield return null;
            }
        }

        private void UpdatePositionToCamera()
        {
            var cameraTransform = CameraRigRef.Instance.transform;
            transform.SetPositionAndRotation(
                cameraTransform.position,
                cameraTransform.rotation);
        }
    }
}
