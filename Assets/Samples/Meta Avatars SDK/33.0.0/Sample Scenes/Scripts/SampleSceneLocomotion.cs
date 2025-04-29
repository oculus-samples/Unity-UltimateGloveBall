#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections.Generic;
using Oculus.Avatar2;
using UnityEngine;

public class SampleSceneLocomotion : MonoBehaviour, IUIControllerInterface
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

    [SerializeField]
    [Tooltip("Controls the speed of rotation")]
    private float rotationSpeed = 60f;

#if UNITY_EDITOR
    [SerializeField]
    [Tooltip("Use keyboard buttons in Editor/PCVR to move avatars.")]
    private bool _useKeyboardDebug = true;

    [SerializeField]
    [Tooltip("Requires you to click your mouse in the game window activate keyboard debug.")]
    private bool _clickForKeyboardDebug = true;

    private bool _keyboardDebugActivated = false;
#endif

    void Update()
    {
        if (UIManager.IsPaused)
        {
            return;
        }
        float xMove, yMove, zMove, yRotate, rotationAngleY;
        Vector3 movement;
#if USING_XR_SDK
        Vector2 inputVectorL = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 inputVectorR = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        xMove = inputVectorL.x;
        yMove = inputVectorR.y;
        zMove = inputVectorL.y;
        yRotate = inputVectorR.x;
        rotationAngleY = yRotate * rotationSpeed * Time.deltaTime;

        if(invertHorizontalMovement) {
            xMove = -xMove;
        }
        if (invertVerticalMovement) {
            zMove = -zMove;
        }

        movement = new Vector3(xMove, yMove, zMove) * movementSpeed * Time.deltaTime;
        transform.Translate(movement);
        transform.Rotate(0f, rotationAngleY, 0f);
#endif
#if UNITY_EDITOR
        if (_useKeyboardDebug)
        {
            if(_keyboardDebugActivated || !_clickForKeyboardDebug)
            {
                xMove = Input.GetAxis("Horizontal");
                yMove = Input.GetAxis("Mouse ScrollWheel") * 100f;
                zMove = Input.GetAxis("Vertical");
                yRotate = Input.GetAxis("Mouse X");
                rotationAngleY = yRotate * rotationSpeed * Time.deltaTime;
                if(invertHorizontalMovement) {
                    xMove = -xMove;
                }
                if (invertVerticalMovement) {
                    zMove = -zMove;
                }

                Vector3 inputDirection = new Vector3(xMove, yMove, zMove);

                UpdateRotationForEditor(ref inputDirection);

                movement = inputDirection * movementSpeed * Time.deltaTime;
                transform.Translate(movement);
                transform.Rotate(0f, rotationAngleY, 0f);

                if(Input.GetKey(KeyCode.Escape) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) {
                    _keyboardDebugActivated = false;
                }
            } else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) {
                _keyboardDebugActivated = true;
            }
        }
#endif
    }

#if UNITY_EDITOR
    // If SampleXplatformObjectPlacement is present and is applying a rotation offset to the Camera,
    // the movement of the locomotion script will then be out of sync with the Camera.
    // This function can be used to adjust that by applying the same rotation to the input vector.
    private void UpdateRotationForEditor(ref Vector3 inputDirection)
    {
        if (OvrAvatarUtility.IsHeadsetActive())
        {
            return;
        }
        SampleXplatformObjectPlacement placement = FindObjectOfType<SampleXplatformObjectPlacement>();
        if (placement)
        {
            if (placement.gameObject.GetComponent<Camera>())
            {
                Quaternion rotation = Quaternion.Euler(placement.GetXPlatformRotation());
                inputDirection = rotation * inputDirection;
            }
        }
    }
#endif

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var primaryAxis2D = new UIInputControllerButton
        {
            axis2d = OVRInput.Axis2D.PrimaryThumbstick,
            controller = OVRInput.Controller.All,
            description = "Move in XZ plane",
            scope = "SampleSceneLocomotion"
        };
        var secondaryAxis2D = new UIInputControllerButton
        {
            axis2d = OVRInput.Axis2D.SecondaryThumbstick,
            controller = OVRInput.Controller.All,
            description = "Move in and Rotate around Y axis",
            scope = "SampleSceneLocomotion"
        };
        var buttons = new List<UIInputControllerButton>
        {
            primaryAxis2D,
            secondaryAxis2D,
        };
        return buttons;
    }
#endif
}
