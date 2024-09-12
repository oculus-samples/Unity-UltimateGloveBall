#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class allows the avatar to move forward/backward and left/right based on input from a thumbstick (XR SDK) or keyboard (Unity Editor).
// Horizontal/Vertical movement can be inverted if needed.
public class SampleAvatarLocomotion : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Controls the speed of movement")]
    public float movementSpeed = 1.0f;

    [SerializeField]
    [Tooltip("Invert the horizontal movement direction. Useful for avatar mirroring")]
    public bool invertHorizontalMovement = false;

    [SerializeField]
    [Tooltip("Invert the vertical movement direction. Useful for avatar mirroring")]
    public bool invertVerticalMovement = false;

#if UNITY_EDITOR
    [SerializeField]
    [Tooltip("Use keyboard buttons in Editor/PCVR to move avatars.")]
    private bool _useKeyboardDebug = false;
#endif


    void Update()
    {
        Vector2 inputVector;
        Vector3 translationVector;
        float movementDelta = movementSpeed * Time.deltaTime;
#if USING_XR_SDK
        // Moves the avatar forward/back and left/right based on primary input
        inputVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        translationVector = new Vector3(invertHorizontalMovement ? -inputVector.x : inputVector.x, 0.0f, invertVerticalMovement? -inputVector.y : inputVector.y);
        transform.Translate(movementDelta * translationVector);
#endif
#if UNITY_EDITOR
        if(_useKeyboardDebug)
        {
            inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            translationVector = new Vector3(invertHorizontalMovement ? -inputVector.x : inputVector.x, 0.0f, invertVerticalMovement ? -inputVector.y : inputVector.y);
            transform.Translate(movementDelta * translationVector);
        }
#endif
    }
}
