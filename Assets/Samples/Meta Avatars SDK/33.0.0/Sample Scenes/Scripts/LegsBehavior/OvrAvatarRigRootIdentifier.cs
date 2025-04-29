#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// A utility script that helps identify the root of an animation rig
    /// </summary>
    public class OvrAvatarRigRootIdentifier : MonoBehaviour
    {
        [Tooltip("The root of the animation rig")]
        public Transform? RigRoot;
    }
}
