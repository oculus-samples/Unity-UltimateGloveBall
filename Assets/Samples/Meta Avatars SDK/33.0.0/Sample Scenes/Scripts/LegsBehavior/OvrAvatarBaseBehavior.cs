#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Avatar2.Experimental
{
    /// <summary>
    /// </summary>
    public abstract class OvrAvatarBaseBehavior : MonoBehaviour
    {
        protected abstract string MainBehavior { get; set; }
        protected abstract string FirstPersonOutputPose { get; set; }
        protected abstract string ThirdPersonOutputPose { get; set; }
        protected virtual string? CustomBehaviorZipFilePath { get; set; }

        protected OvrAvatarEntity? Entity { get; private set; }

        protected virtual void OnUserAvatarLoaded(OvrAvatarEntity _)
        {
            InitializeBehaviorSystem(MainBehavior, FirstPersonOutputPose, ThirdPersonOutputPose, CustomBehaviorZipFilePath);
        }

        protected virtual void OnEntityPreTeardown(OvrAvatarEntity _)
        {
        }

        protected virtual void InitializeBehaviorSystem(
            string mainBehavior,
            string firstPersonOutputPose,
            string thirdPersonOutputPose,
            string? customBehaviorZipPath = null)
        {
            if (Entity == null)
            {
                OvrAvatarLog.LogError("No valid entity found");
                return;
            }

            if (!string.IsNullOrEmpty(customBehaviorZipPath))
            {
                var behaviorZipPath = Path.Combine(Application.streamingAssetsPath, customBehaviorZipPath!);
                if (!Entity.LoadBehaviorZip(behaviorZipPath))
                {
                    OvrAvatarLog.LogError($"Failed to load custom behavior zip from path {behaviorZipPath}");
                    return;
                }
            }

            if (!Entity.EnableBehaviorSystem(true))
            {
                OvrAvatarLog.LogError("Failed to enable behavior system");
                return;
            }

            if (!Entity.SetMainBehavior(mainBehavior))
            {
                OvrAvatarLog.LogError($"Failed to set main behavior to {mainBehavior}");
                return;
            }

            if (!Entity.SetOutputPose(firstPersonOutputPose,
                    Avatar2.CAPI.ovrAvatar2EntityViewFlags.FirstPerson))
            {
                OvrAvatarLog.LogError($"Failed to set behavior output pose to {firstPersonOutputPose}");
                return;
            }

            if (!Entity.SetOutputPose(thirdPersonOutputPose,
                    Avatar2.CAPI.ovrAvatar2EntityViewFlags.ThirdPerson))
            {
                OvrAvatarLog.LogError($"Failed to set behavior output pose to {thirdPersonOutputPose}");
                return;
            }
        }

        private void Start()
        {
            Entity = GetComponentInParent<OvrAvatarEntity>();
            Assert.IsNotNull(Entity);

            if (!Entity.IsLocal)
            {
                return;
            }

            Entity.PreTeardownEvent.AddListener(OnEntityPreTeardown);

            if (Entity.CurrentState != OvrAvatarEntity.AvatarState.UserAvatar)
            {
                Entity.OnUserAvatarLoadedEvent.AddListener(OnUserAvatarLoaded);
            }
            else
            {
                OnUserAvatarLoaded(Entity);
            }
        }
    }
}
