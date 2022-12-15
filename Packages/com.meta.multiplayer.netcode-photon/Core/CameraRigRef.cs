// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities;
using Oculus.Avatar2;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Singleton to easily access the camera rig and components related to the rig.
    /// </summary>
    [RequireComponent(typeof(OVRCameraRig))]
    public class CameraRigRef : Singleton<CameraRigRef>
    {
        [AutoSet] public OVRCameraRig CameraRig;
        public OvrAvatarInputManager AvatarInputManager;
    }
}