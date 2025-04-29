#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections.Generic;
using System.Reflection;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Node = UnityEngine.XR.XRNode;


public abstract class BaseHeadsetInputSimulator : MonoBehaviour
{
    protected OvrAvatarInputTrackingDelegate? _inputTrackingDelegate;
    public virtual OvrAvatarInputTrackingDelegate? InputTrackingDelegate => _inputTrackingDelegate;
}

/* This is an example class for how to send input and IK transforms to the sdk from any source
 * InputTrackingDelegate and InputControlDelegate are set on BodyTracking.
 */
public class SampleInputManager : OvrAvatarInputManager
{
    private const string logScope = "sampleInput";

    [SerializeField]
    [Tooltip("Optional. If added, it will use input directly from OVRCameraRig instead of doing its own calculations.")]
#if USING_XR_SDK
    private OVRCameraRig? _ovrCameraRig = null;
#endif
    private bool _useOvrCameraRig;

    // Only used in editor, produces warnings when packaging
#pragma warning disable CS0414 // is assigned but its value is never used
    [SerializeField]
    private bool _debugDrawTrackingLocations = false;
#pragma warning restore CS0414 // is assigned but its value is never used

#if UNITY_EDITOR
    [CollapsibleSectionStart("Sample Headset Input Simulator", true, 0f, 0.5f, 0.5f)] [SerializeField]
    public BaseHeadsetInputSimulator? HeadsetInputSimulator;
    [CollapsibleSectionEnd]
    [SerializeField]
    [Tooltip("Optional. If set to true, when played in Editor, all instances of SampleSceneLocomotion are disabled, " +
             "so that the movements don't override or interact in unwanted ways if applied to the same avatar.")]
    private bool disableSampleSceneLocomotion = false;
#endif

    public OvrAvatarBodyTrackingMode BodyTrackingMode
    {
        get => _bodyTrackingMode;
        set
        {
            _bodyTrackingMode = value;
            InitializeBodyTracking();
        }
    }

    protected void Awake()
    {
#if USING_XR_SDK
        _useOvrCameraRig = _ovrCameraRig != null;
#endif

        // Debug Drawing
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnSceneGUI;
#endif
    }

    private void Start()
    {
#if USING_XR_SDK
        // If OVRCameraRig doesn't exist, we should set tracking origin ourselves
        if (!_useOvrCameraRig)
        {

            if (OVRManager.instance == null)
            {
                OvrAvatarLog.LogDebug("Creating OVRManager, as one doesn't exist yet.", logScope, this);
                var go = new GameObject("OVRManager");
                var manager = go.AddComponent<OVRManager>();
                manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }
            else
            {
                OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }

            OvrAvatarLog.LogInfo("Setting Tracking Origin to FloorLevel", logScope, this);

            var instances = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(instances);
            foreach (var instance in instances)
            {
                instance.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }
        }
#endif
    }

    protected override void OnTrackingInitialized()
    {
#if USING_XR_SDK
        // On Oculus SDK version >= v46 Eye tracking and Face tracking need to be explicitly started by the application
        // after permission has been requested.
        OvrPluginInvoke("StartFaceTracking");
        OvrPluginInvoke("StartEyeTracking");
#endif

        IOvrAvatarInputTrackingDelegate? inputTrackingDelegate = null;
#if USING_XR_SDK
#if UNITY_EDITOR
        // Use SampleAvatarHeadsetInputSimulator if no headset connected via link
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            if (disableSampleSceneLocomotion)
            {
                DisableSampleSceneLocomotion();
            }

            if (HeadsetInputSimulator is not null)
            {
                inputTrackingDelegate = HeadsetInputSimulator.InputTrackingDelegate;
            }
            else
            {
                inputTrackingDelegate = new SampleAvatarHeadsetInputSimulator();
            }
        }
        else
        {
            inputTrackingDelegate = new SampleInputTrackingDelegate(_ovrCameraRig);
        }
#else // !UNITY_EDITOR
        inputTrackingDelegate = new SampleInputTrackingDelegate(_ovrCameraRig);
#endif // !UNITY_EDITOR
#endif // USING_XR_SDK
        var inputControlDelegate = new SampleInputControlDelegate();

        _inputTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
        _inputControlProvider = new OvrAvatarInputControlDelegatedProvider(inputControlDelegate);
    }

#if UNITY_EDITOR
    // Disable SampleSceneLomcomotion scripts if they are present.
    private void DisableSampleSceneLocomotion()
    {
        var locomotionScripts = FindObjectsOfType<SampleSceneLocomotion>();
        if (locomotionScripts != null)
        {
            foreach (var locomotionScript in locomotionScripts)
            {
                locomotionScript.enabled = false;
                OvrAvatarLog.LogWarning($"Found and disabled SampleSceneLocomotion on object: {locomotionScript.gameObject.name}.", logScope);
            }
        }
    }
#endif


#if USING_XR_SDK
    // We use reflection here so that there are not compiler errors when using Oculus SDK v45 or below.
    private static void OvrPluginInvoke(string method, params object[] args)
    {
        typeof(OVRPlugin).GetMethod(method, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, args);
    }
#endif

    protected override void OnDestroyCalled()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
#endif

        base.OnDestroyCalled();
    }

#if UNITY_EDITOR
    #region Debug Drawing

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_debugDrawTrackingLocations)
        {
            DrawTrackingLocations();
        }
    }

    private void DrawTrackingLocations()
    {
        if (InputTrackingProvider == null)
        {
            return;
        }

        var inputTrackingState = InputTrackingProvider.State;

        float radius = 0.2f;
        Quaternion orientation;
        float outerRadius() => radius + 0.25f;
        Vector3 forward() => orientation * Vector3.forward;

        Handles.color = Color.blue;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.headset.position, radius);

        orientation = inputTrackingState.headset.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.headset.position + forward() * radius,
            (Vector3)inputTrackingState.headset.position + forward() * outerRadius());

        radius = 0.1f;
        Handles.color = Color.yellow;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.leftController.position, radius);

        orientation = inputTrackingState.leftController.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.leftController.position + forward() * radius,
            (Vector3)inputTrackingState.leftController.position + forward() * outerRadius());

        Handles.color = Color.yellow;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.rightController.position, radius);

        orientation = inputTrackingState.rightController.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.rightController.position + forward() * radius,
            (Vector3)inputTrackingState.rightController.position + forward() * outerRadius());
    }

    #endregion
#endif // UNITY_EDITOR
}
