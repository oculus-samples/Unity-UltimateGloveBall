#nullable disable

using System;
using Oculus.Avatar2;
using Oculus.Avatar2.Experimental;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using CAPI = Oculus.Avatar2.CAPI;

/*
 * The AttachablesAuthoring Scene offers a test bed to edit and preview different Attachable Socket configurations.
 *
 * The default sample setup displays a "Hat" prefab attached to a created "Head" socket.
 * The sliders change the various properties of the Socket, changing the Socket and thus the Hat with it.
 * The text panel displays the equivalent avatarEntity.CreateSocket() that generates the previewed Socket configuration.
 * The text can be copied and pasted into your code.
 *
 * To test different prefabs and sockets:
 *  - Change the prefab in AttachablesAuthoringSceneManager.SocketCreationInfo.AttachedObject
 *  - Change the AttachablesAuthoringSceneManager.SocketCreationInfo.Parent to a different joint
 *       - Note: The same Joint must be added to the Avatars' "Critical Joint Types" for each Avatar
 */
public class AttachablesAuthoringSceneManager : MonoBehaviour
{
    private static readonly int s_isJumping = Animator.StringToHash("IsJumping");

    // public Slider slider;
    [FormerlySerializedAs("entity")] public OvrAvatarEntity entity1;
    public OvrAvatarEntity entity2;
    public OvrAvatarEntity entity3;
    public SocketCreationInfo socketCreationInfo;
    public Slider xPosSlider;
    public Slider yPosSlider;
    public Slider zPosSlider;
    public Slider xRotSlider;
    public Slider yRotSlider;
    public Slider zRotSlider;
    public Slider depthSlider;
    public Slider widthSlider;
    public Slider heightSlider;
    public Slider xBaseScaleSlider;
    public Slider yBaseScaleSlider;
    public Slider zBaseScaleSlider;
    public Button resetButton;
    public InputField createSocketText;
    public Toggle jumpingAnimationToggle;

    private OvrAvatarAnimationBehavior _animationBehavior1;
    private OvrAvatarAnimationBehavior _animationBehavior2;
    private OvrAvatarAnimationBehavior _animationBehavior3;

    private OvrAvatarSocketDefinition _socket1;
    private OvrAvatarSocketDefinition _socket2;
    private OvrAvatarSocketDefinition _socket3;

    private const float DISABLE_SCALE_CUTOFF = 0.0001f;

    [Serializable]
    public struct SocketCreationInfo
    {
        public string name;

        public CAPI.ovrAvatar2JointType parent;

        // Canonical position and rotation
        public Vector3 position;
        public Vector3 eulerAngles;

        // Base Scale
        public bool hasBaseScaleMod;
        public Vector3 baseScale;

        // Canonical Sizes (for calculating scaling)
        public float width;
        public float depth;
        public float height;

        // Configuration
        public bool createGameObject;
        public bool scaleGameObject;
        public GameObject attachedObject;
    }

    private void Awake()
    {
        SetSliderBounds();

        // Position
        xPosSlider.onValueChanged.AddListener(delegate { OnPositionSliderChange(); });
        yPosSlider.onValueChanged.AddListener(delegate { OnPositionSliderChange(); });
        zPosSlider.onValueChanged.AddListener(delegate { OnPositionSliderChange(); });

        // Rotation
        xRotSlider.onValueChanged.AddListener(delegate { OnRotationSliderChange(); });
        yRotSlider.onValueChanged.AddListener(delegate { OnRotationSliderChange(); });
        zRotSlider.onValueChanged.AddListener(delegate { OnRotationSliderChange(); });

        // Canonical size: Height -> x --- Depth -> y --- Width -> z
        heightSlider.onValueChanged.AddListener(delegate { OnCanonicalScaleSliderChange(); });
        depthSlider.onValueChanged.AddListener(delegate { OnCanonicalScaleSliderChange(); });
        widthSlider.onValueChanged.AddListener(delegate { OnCanonicalScaleSliderChange(); });

        // Base Scale
        xBaseScaleSlider.onValueChanged.AddListener(delegate { OnBaseScaleSliderChange(); });
        yBaseScaleSlider.onValueChanged.AddListener(delegate { OnBaseScaleSliderChange(); });
        zBaseScaleSlider.onValueChanged.AddListener(delegate { OnBaseScaleSliderChange(); });

        // Reset
        resetButton.onClick.AddListener(ResetToInitialValues);

        // Jumping Animation Toggle
        jumpingAnimationToggle.onValueChanged.AddListener(delegate { OnJumpingAnimationToggle(); });
    }

    private void Start()
    {
        _socket1 = CreateSocketFromCreationInfo(entity1, socketCreationInfo);
        _socket2 = CreateSocketFromCreationInfo(entity2, socketCreationInfo);
        _socket3 = CreateSocketFromCreationInfo(entity3, socketCreationInfo);

        _animationBehavior1 = entity1.GetComponent<OvrAvatarAnimationBehavior>();
        _animationBehavior2 = entity2.GetComponent<OvrAvatarAnimationBehavior>();
        _animationBehavior3 = entity3.GetComponent<OvrAvatarAnimationBehavior>();
        SetInitialSliderValues();
    }

    private void SetSliderBounds()
    {
        xRotSlider.minValue = -180f;
        xRotSlider.maxValue = 180f;
        yRotSlider.minValue = -180f;
        yRotSlider.maxValue = 180f;
        zRotSlider.minValue = -180f;
        zRotSlider.maxValue = 180f;

        heightSlider.minValue = 0f;
        heightSlider.maxValue = 1f;
        widthSlider.minValue = 0f;
        widthSlider.maxValue = 1f;
        depthSlider.minValue = 0f;
        depthSlider.maxValue = 1f;

        xBaseScaleSlider.minValue = 0f;
        xBaseScaleSlider.maxValue = 2f;
        yBaseScaleSlider.minValue = 0f;
        yBaseScaleSlider.maxValue = 2f;
        zBaseScaleSlider.minValue = 0f;
        zBaseScaleSlider.maxValue = 2f;
    }

    private void SetInitialSliderValues()
    {
        xPosSlider.value = socketCreationInfo.position.x;
        yPosSlider.value = socketCreationInfo.position.y;
        zPosSlider.value = socketCreationInfo.position.z;
        xRotSlider.value = socketCreationInfo.eulerAngles.x;
        yRotSlider.value = socketCreationInfo.eulerAngles.y;
        zRotSlider.value = socketCreationInfo.eulerAngles.z;

        widthSlider.value = ScaleSlider(socketCreationInfo.width);
        depthSlider.value = ScaleSlider(socketCreationInfo.depth);
        heightSlider.value = ScaleSlider(socketCreationInfo.height);

        if (socketCreationInfo.hasBaseScaleMod)
        {
            xBaseScaleSlider.value = socketCreationInfo.baseScale.x;
            yBaseScaleSlider.value = socketCreationInfo.baseScale.y;
            zBaseScaleSlider.value = socketCreationInfo.baseScale.z;
        }
        else
        {
            xBaseScaleSlider.value = 1f;
            yBaseScaleSlider.value = 1f;
            zBaseScaleSlider.value = 1f;
        }

        UpdateText();
    }
    private void OnPositionSliderChange()
    {
        var socketPos = new Vector3(xPosSlider.value, yPosSlider.value, zPosSlider.value);
        _socket1.SetPosition(socketPos);
        _socket2.SetPosition(socketPos);
        _socket3.SetPosition(socketPos);
        UpdateText();
    }

    private void OnRotationSliderChange()
    {
        var socketRotation = new Vector3(xRotSlider.value, yRotSlider.value, zRotSlider.value);
        _socket1.SetRotation(socketRotation);
        _socket2.SetRotation(socketRotation);
        _socket3.SetRotation(socketRotation);
        UpdateText();
    }

    private void OnCanonicalScaleSliderChange()
    {
        var socketScale = new Vector3(ScaleSlider(heightSlider.value), ScaleSlider(depthSlider.value),
            ScaleSlider(widthSlider.value));
        _socket1.SetCanonicalSize(socketScale);
        _socket2.SetCanonicalSize(socketScale);
        _socket3.SetCanonicalSize(socketScale);
        UpdateText();
    }

    private void CheckBaseScale()
    {
        if (xBaseScaleSlider.value < DISABLE_SCALE_CUTOFF && yBaseScaleSlider.value < DISABLE_SCALE_CUTOFF &&
            zBaseScaleSlider.value < DISABLE_SCALE_CUTOFF)
        {
            socketCreationInfo.hasBaseScaleMod = false;
        }
        else
        {
            socketCreationInfo.hasBaseScaleMod = true;
        }
    }

    private void OnBaseScaleSliderChange()
    {
        CheckBaseScale();
        var baseScale = socketCreationInfo.hasBaseScaleMod ?
            new Vector3(xBaseScaleSlider.value, yBaseScaleSlider.value, zBaseScaleSlider.value) :
            Vector3.one;
        _socket1.SetBaseScale(baseScale);
        _socket2.SetBaseScale(baseScale);
        _socket3.SetBaseScale(baseScale);
        UpdateText();
    }

    private float ScaleSlider(float val)
    {
        if (val > DISABLE_SCALE_CUTOFF)
        {
            return val;
        }

        return 1f;
    }

    private void UpdateText()
    {
        createSocketText.text = $"CreateSocket(\n" +
                                $"\tname: \"{socketCreationInfo.name}\", \n" +
                                $"\tparent: CAPI.ovrAvatar2JointType.{socketCreationInfo.parent}, \n" +
                                $"\tposition: new Vector3({xPosSlider.value}f, {yPosSlider.value}f, {zPosSlider.value}f), \n" +
                                $"\teulerAngles: new Vector3({xRotSlider.value}f, {yRotSlider.value}f, {zRotSlider.value}f), \n" +
                                $"\tbaseScale: " + (socketCreationInfo.hasBaseScaleMod ? $"new Vector3({xBaseScaleSlider.value}f, {yBaseScaleSlider.value}f, {zBaseScaleSlider.value}f), \n" : "null, \n") +
                                $"\twidth: " + (widthSlider.value > DISABLE_SCALE_CUTOFF ? $"{widthSlider.value}f, \n" : "null, \n") +
                                $"\tdepth: " + (depthSlider.value > DISABLE_SCALE_CUTOFF ? $"{depthSlider.value}f, \n" : "null, \n") +
                                $"\theight: " + (heightSlider.value > DISABLE_SCALE_CUTOFF ? $"{heightSlider.value}f, \n" : "null, \n") +
                                $")";
    }

    private void OnJumpingAnimationToggle()
    {
        bool isJumping = jumpingAnimationToggle.isOn;
        _animationBehavior1?.CustomAnimator?.SetBool(s_isJumping, isJumping);
        _animationBehavior2?.CustomAnimator?.SetBool(s_isJumping, isJumping);
        _animationBehavior3?.CustomAnimator?.SetBool(s_isJumping, isJumping);
    }

    private void ResetToInitialValues()
    {
        SetInitialSliderValues();
    }

    private void OnDestroy()
    {
        RemoveAllSliderListeners();
    }

    private void OnDisable()
    {
        RemoveAllSliderListeners();
    }

    private void RemoveAllSliderListeners()
    {
        xPosSlider.onValueChanged.RemoveAllListeners();
        yPosSlider.onValueChanged.RemoveAllListeners();
        zPosSlider.onValueChanged.RemoveAllListeners();

        xRotSlider.onValueChanged.RemoveAllListeners();
        yRotSlider.onValueChanged.RemoveAllListeners();
        zRotSlider.onValueChanged.RemoveAllListeners();

        heightSlider.onValueChanged.RemoveAllListeners();
        depthSlider.onValueChanged.RemoveAllListeners();
        widthSlider.onValueChanged.RemoveAllListeners();

        xBaseScaleSlider.onValueChanged.RemoveAllListeners();
        yBaseScaleSlider.onValueChanged.RemoveAllListeners();
        zBaseScaleSlider.onValueChanged.RemoveAllListeners();

        resetButton.onClick.RemoveAllListeners();
    }

    private OvrAvatarSocketDefinition CreateSocketFromCreationInfo(OvrAvatarEntity avatarEntity, SocketCreationInfo creationInfo)
    {
        return avatarEntity.CreateSocket(
            creationInfo.name,
            creationInfo.parent,
            creationInfo.position,
            creationInfo.eulerAngles,
            creationInfo.hasBaseScaleMod ? creationInfo.baseScale : null,
            creationInfo.width > DISABLE_SCALE_CUTOFF ? creationInfo.width : null,
            creationInfo.depth > DISABLE_SCALE_CUTOFF ? creationInfo.depth : null,
            creationInfo.height > DISABLE_SCALE_CUTOFF ? creationInfo.height : null,
            creationInfo.createGameObject,
            creationInfo.scaleGameObject
        );
    }

    private void Update()
    {
        if (_socket1 != null && _socket1.IsReady() && _socket1.IsEmpty())
        {
            _socket1.Attach(Instantiate(socketCreationInfo.attachedObject));
        }
        if (_socket2 != null && _socket2.IsReady() && _socket2.IsEmpty())
        {
            _socket2.Attach(Instantiate(socketCreationInfo.attachedObject));
        }
        if (_socket3 != null && _socket3.IsReady() && _socket3.IsEmpty())
        {
            _socket3.Attach(Instantiate(socketCreationInfo.attachedObject));
        }
    }
}
