#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Avatar2.Experimental
{
    /// <summary>
    /// Initializes avatar behavior with user defined behavior parameters
    /// </summary>
    public class SampleAvatarBehavior : OvrAvatarBaseBehavior
    {
        [Tooltip("The main behavior to use inside the behavior.zip file")]
        public string MainBehaviorName = "apn_prototype/main";

        [Tooltip("The name for 1st person output pose")]
        public string FirstPersonOutputPoseName = "pose";

        [Tooltip("The name for 3rd person output pose")]
        public string ThirdPersonOutputPoseName = "pose3P";

        [SerializeField]
        [Tooltip("Behavior zip file path inside the streaming asset folder")]
        private string _customBehaviorZipFilePath = string.Empty;

        protected override string MainBehavior { get => MainBehaviorName; set => MainBehaviorName = value; }
        protected override string FirstPersonOutputPose { get => FirstPersonOutputPoseName; set => FirstPersonOutputPoseName = value; }
        protected override string ThirdPersonOutputPose { get => ThirdPersonOutputPoseName; set => ThirdPersonOutputPoseName = value; }
        protected override string? CustomBehaviorZipFilePath { get => _customBehaviorZipFilePath; set => _customBehaviorZipFilePath = value ?? string.Empty; }
    }
}
