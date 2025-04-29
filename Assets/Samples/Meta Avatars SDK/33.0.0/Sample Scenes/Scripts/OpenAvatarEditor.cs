#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using UnityEngine;
using Oculus.Avatar2;
using Oculus.Platform;
using System;
using System.Collections.Generic;

public class OpenAvatarEditor : MonoBehaviour, IUIControllerInterface
{
    private const string logScope = "open_avatar_editor";

    void Update()
    {
#if USING_XR_SDK
        // Button Press
        // TODO: Should the user be able to run avatar editor when in UI?
        if (!UIManager.IsPaused && OVRInput.Get(OVRInput.Button.One) && OVRInput.Get(OVRInput.Button.Two))
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

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var openAvatarEditor = new UIInputControllerButton
        {
            combinationButtons = new List<OVRInput.Button>
            {
                OVRInput.Button.One,
                OVRInput.Button.Two
            },
            controller = OVRInput.Controller.Active,
            description = "Opens the Avatar Editor",
            scope = "OpenAvatarEditor"
        };
        var buttons = new List<UIInputControllerButton>
        {
            openAvatarEditor,
        };
        return buttons;
    }
#endif
}
