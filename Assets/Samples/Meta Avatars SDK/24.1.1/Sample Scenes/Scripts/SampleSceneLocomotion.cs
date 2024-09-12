#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using UnityEngine;

public class SampleSceneLocomotion : MonoBehaviour
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
        float xMove, yMove, zMove, yrotate, rotationAngleY;
        Vector3 movement;
#if USING_XR_SDK
        Vector2 inputVectorL = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 inputVectorR = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        xMove = inputVectorL.x;
        yMove = inputVectorR.y;
        zMove = inputVectorL.y;
        yrotate = inputVectorR.x;
        rotationAngleY = yrotate * rotationSpeed * Time.deltaTime;

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
                yrotate = Input.GetAxis("Mouse X");
                rotationAngleY = yrotate * rotationSpeed * Time.deltaTime;

                if(invertHorizontalMovement) {
                    xMove = -xMove;
                }
                if (invertVerticalMovement) {
                    zMove = -zMove;
                }

                movement = new Vector3(xMove, yMove, zMove) * movementSpeed * Time.deltaTime;
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
}
