#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif
using System.Collections.Generic;
#if USING_XR_SDK
using UnityEngine;

namespace Oculus.Avatar2
{
    public class DelayPermissionRequest : MonoBehaviour, IUIControllerInterface
    {
        void Start()
        {
            OvrAvatarManager.Instance.automaticallyRequestPermissions = false;
        }

        void Update()
        {
            if (!UIManager.IsPaused && OVRInput.Get(OVRInput.Button.Two))
            {
                OvrAvatarManager.Instance.EnablePermissionRequests();
            }
        }

        public List<UIInputControllerButton> GetControlSchema()
        {
            var delayPermissionReq = new UIInputControllerButton
            {
                button = OVRInput.Button.Two,
                controller = OVRInput.Controller.Active,
                description = "Enables permission requests in OvrAvatarManager",
                scope = "DelayPermissionRequest"
            };
            var buttons = new List<UIInputControllerButton>
            {
                delayPermissionReq,
            };
            return buttons;
        }
    }
}
#endif
