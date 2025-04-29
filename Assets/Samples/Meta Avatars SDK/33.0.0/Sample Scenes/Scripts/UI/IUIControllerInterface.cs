#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUIControllerInterface
{
#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema();
#endif
}

#if USING_XR_SDK
[System.Serializable]
public struct UIInputControllerButton
{
    public OVRInput.Button button;
    public OVRInput.Controller controller;
    public List<OVRInput.Button> combinationButtons;
    public string description;
    [HideInInspector] public string scope;
    [HideInInspector] public OVRInput.Axis2D axis2d;
}
#endif
