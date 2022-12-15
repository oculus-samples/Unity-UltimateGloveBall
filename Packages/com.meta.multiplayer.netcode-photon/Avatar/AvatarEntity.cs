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

        protected override void Awake()
        {
            // Don't call base.Awake() until after the network object is initialized
        }

        private async Task Start()
        {
            var isOwner = m_networkObject == null ? m_isLocalIfNotNetworked : m_networkObject.IsOwner;
            SetIsLocal(isOwner);
            if (isOwner)
            {
                var body = CameraRigRef.Instance.AvatarInputManager;
                SetBodyTracking(body);

                m_lipSync.gameObject.SetActive(true);
                SetLipSync(m_lipSync);
            }
            else
            {
                _creationInfo.features &= ~ovrAvatar2EntityFeatures.Animation;
                SetBodyTracking(null);
                SetLipSync(null);
            }

            base.Awake();
            if (!isOwner)
            {
                SetActiveView(ovrAvatar2EntityViewFlags.ThirdPerson);
            }
            else
            {
                SetActiveView(ovrAvatar2EntityViewFlags.FirstPerson);
            }

            var accessToken = await Users.GetAccessToken().Gen();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Data);

            if (m_networking)
            {
                m_networking.Init(this);
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
                    _ = StartCoroutine(TrackCamera());
                }
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
