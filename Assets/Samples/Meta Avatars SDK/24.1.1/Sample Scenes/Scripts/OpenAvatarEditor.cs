#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using UnityEngine;
using Oculus.Avatar2;
using Oculus.Platform;
using System;

public class OpenAvatarEditor : MonoBehaviour
{
    private const string logScope = "open_avatar_editor";

    void Update()
    {
#if USING_XR_SDK
        // Button Press
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch | OVRInput.Controller.LHand))
        {
            if (OvrPlatformInit.status != OvrPlatformInitStatus.Succeeded)
            {
                OvrAvatarLog.LogError("OvrPlatform not initialized.", logScope);
                return;
            }

            AvatarEditorOptions options = new AvatarEditorOptions();
            options.SetSourceOverride("avatar_2_sdk");
            var result = new Request<Oculus.Platform.Models.AvatarEditorResult>(Oculus.Platform.CAPI.ovr_Avatar_LaunchAvatarEditor((IntPtr)options));
        }
#endif
    }
}
