#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// This class modifies the placement of the host game object in Non XR mode
/// </summary>
public class SampleXplatformObjectPlacement : MonoBehaviour
{
    public enum Mode
    {
        Replace, // Replace existing object position and rotation with new values
        Additive, // Position and rotation values are added to existing values
    }

#pragma warning disable 0414
    [Tooltip("The mode in which the position and rotation value is applied. Replace mode replaces existing values. Additive mode add to exisiting values.")]
    [SerializeField]
#pragma warning disable CS0414
    private Mode _mode = Mode.Replace;
#pragma warning restore CS0414

    [Tooltip("The coordinate space in which the object position and rotation should be applied.")]
    [SerializeField]
#pragma warning disable CS0414
    private Space _coordinateSpace = Space.Self;
#pragma warning restore CS0414

    [Tooltip("Object position")]
    [SerializeField]
    private Vector3 _position;

    [Tooltip("Object rotation in euler angles")]
    [SerializeField]
    private Vector3 _rotation;
#pragma warning restore 0414

#if !USING_XR_SDK || UNITY_EDITOR
    private void Awake()
    {
        // No correction if Headset is available through Link
        if (OvrAvatarUtility.IsHeadsetActive())
        {
            return;
        }

        var finalPosition = _position;
        var finalRotation = Quaternion.identity;

        if (_coordinateSpace == Space.World)
        {
            if (_mode == Mode.Additive)
            {
                finalPosition += transform.position;
                finalRotation = Quaternion.Euler(_rotation + transform.rotation.eulerAngles);
            }
            else
            {
                finalRotation = Quaternion.Euler(_rotation);
            }

            transform.SetPositionAndRotation(finalPosition, finalRotation);
        }
        else
        {
            if (_mode == Mode.Additive)
            {
                finalPosition += transform.localPosition;
                finalRotation = Quaternion.Euler(_rotation + transform.localRotation.eulerAngles);
            }
            else
            {
                finalRotation = Quaternion.Euler(_rotation);
            }

            transform.localPosition = finalPosition;
            transform.localRotation = finalRotation;
        }
    }
#endif
}
