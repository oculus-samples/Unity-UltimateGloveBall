#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

#if USING_XR_SDK
using UnityEngine;

namespace Oculus.Avatar2
{
    public class DelayPermissionRequest : MonoBehaviour
    {
        void Start()
        {
            OvrAvatarManager.Instance.automaticallyRequestPermissions = false;
        }

        void Update()
        {
            if (OVRInput.Get(OVRInput.Button.Two))
            {
                OvrAvatarManager.Instance.EnablePermissionRequests();
            }
        }
    }
}
#endif
