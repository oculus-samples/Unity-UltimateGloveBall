#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarManager;
using static Oculus.Avatar2.OvrAvatarManager.PuppeteerInfo;

namespace Oculus.Avatar2.Experimental
{
    /// <summary>
    /// A sample behavior scripts that enable appPoseNode on an avatar
    /// </summary>
    public class OvrAvatarAnimationBehavior : OvrAvatarBaseBehavior
    {
        private const string FIRST_PERSON_POSE = "pose";

        private const string BLENDED_ANIMATION_BEHAVIOR = "apn_prototype/main";
        private const string BLENDED_ANIMATION_THIRD_PERSON_POSE = "pose3P";
        private const string BLENDED_ANIMATION_BEHAVIOR_FILE_PATH = "BehaviorAssets/blendedAnimation.behavior.zip";

        private const string BASIC_ANIMATION_BEHAVIOR = "appPoseScene/sample_app_pose_node";
        private const string BASIC_ANIMATION_THIRD_PERSON_POSE = "pose3P";
        private const string BASIC_ANIMATION_BEHAVIOR_FILE_PATH = "BehaviorAssets/avatarAnimation.behavior.zip";

        private const string BLENDED_TRACKING_ANCHORED_ANIMATION_BEHAVIOR_FILE_PATH = "BehaviorAssets/blendedTrackingAnchoredAnimation.behavior.zip";

        private const string ROOT_ANGLE_EVENT_NAME = "avatarSDK_rootAngleCorrection";
        private static BehaviorSystem.EventController<float>? _rootAngleEvent;

        private const string ROOT_TRANSLATION_EVENT_NAME = "avatarSDK_rootTranslationCorrection";
        private static BehaviorSystem.EventController<Avatar2.CAPI.ovrAvatar2Vector3f>? _rootTranslationEvent;

        private const string ROOT_SCALE_EVENT_NAME = "avatarSDK_rootScaleCorrection";
        private static BehaviorSystem.EventController<Avatar2.CAPI.ovrAvatar2Vector3f>? _rootScaleEvent;

        private const string LEFT_ARM_BLEND_EVENT_NAME = "avatarSDK_leftArmBlendFactor";
        private static BehaviorSystem.EventController<float>? _leftArmBlendFactorEvent;

        private const string RIGHT_ARM_BLEND_EVENT_NAME = "avatarSDK_rightArmBlendFactor";
        private static BehaviorSystem.EventController<float>? _rightArmBlendFactorEvent;

        private string _defaultRtRigPrefab = "RTR00003";
#if UNITY_EDITOR
        private float _cachedLeftArmBlendFactor = float.MinValue;
        private float _cachedRightArmBlendFactor = float.MinValue;
        private float _cachedCustomAnimBlendFactor = float.MinValue;
#endif
        [SerializeField]
        private GameObject? _customRigPrefab;
        [SerializeField]
        private bool _customAnimationOnly = false;
        [SerializeField]
        [Tooltip("Enable performance optimization for avatar that runs static animation, or avatar that can potentially be paused and frozen")]
        private bool _enableStaticAnimationOptimization = false;

        private RigInfo? _defaultRigInfo;
        private RigInfo? _humanoidRigInfo;

        [Tooltip("Blend tracking input into animation")]
        [SerializeField]
        private bool _inputBlended = true;

        [Tooltip("Controller input blend factor for left arm")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _leftArmBlendFactor = 1f;

        [Tooltip("Controller input blend factor for the right arm")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _rightArmBlendFactor = 1f;

        [Tooltip("Blend factor between default and custom animation. 0 is full default animation and 1 is full custom animation")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _customAnimationBlendFactor = 0f;

        [SerializeField]
        private bool _enableTrackedUpperBodyRotation = true;

        [SerializeField]
        private bool _enableDebugRigVisual = false;

        [Tooltip("When enabled, this allows the upper body of an avatar to rotate independently of the lower body. When the difference " +
            "in angle between the upper body and lower body grows larger than the max upper body angle deviation, the lower body will " +
            "automatically rotate to face the same direction as the upper body. When disabled, the lower body of the avatar will always " +
            "remain in sync with the upper body.")]
        [SerializeField]
        private bool _enableIndependentUpperBodyHorizontalRotation = true;

        [Tooltip("When independent upper body horizontal rotation is enabled, this value determines the angle threshold between the upper " +
            "and lower body, past which the lower body will automatically rotate to align with the upper body.")]
        [Range(0.0f, 180.0f)]
        [SerializeField]
        private float _maxUpperBodyAngleDeviation = 45.0f;

        [Tooltip("When independent upper body horizontal rotation is enabled, this value determines how many degrees per second the lower " +
            "body can rotate in order to align with the upper body.")]
        [Range(1.0f, 1440.0f)]
        [SerializeField]
        private float _lowerBodyRotationDegreesPerSecond = 90.0f;

        [SerializeField]
        [Tooltip("Enabling this allows the scale of the corresponding remote avatar to be adjusted using the RemoteScaleFactor property. " +
            "This lets you adjust the height of the avatar on remote devices to better match the actual height of the user.")]
        private bool _enableRemoteScaling = false;

        [Range(OvrAvatarManager.AvatarRemoteDynamicScalingInputMin, OvrAvatarManager.AvatarRemoteDynamicScalingInputMax)]
        [SerializeField]
        [Tooltip("When EnableRemoteScaling is enabled, this property allows you to control the scale of the remote avatar. This can be " +
            "helpful for adjusting the size of the avatar to better match the height of the user.")]
        private float _remoteScaleFactor = 1.0f;

        [SerializeField]
        [Tooltip("This property contains options meant for use only with mixed reality applications.")]
        private MRAnimationOptions _mixedRealityAnimationOptions = new MRAnimationOptions(MRAnimationOptions.MRAnchoringState.AnchorToFloor);
        private Transform? _ballJointTransform;
        private float _dynamicCrouchFactor = 0.4f;

        [System.Serializable]
        protected struct MRAnimationOptions
        {
            public enum MRAnchoringState
            {
                AnchorToFloor = 0,

                AnchorToHeadset,
                AnchorToHeadsetDynamicCrouching,
            }

            public MRAnchoringState anchoringState;

            public MRAnimationOptions(MRAnchoringState _anchoringState)
            {
                anchoringState = _anchoringState;
            }

            public bool HeadsetAnchoringEnabled
            {
                get
                {
                    return anchoringState != MRAnchoringState.AnchorToFloor;
                }
            }

            public bool DynamicCrouchingEnabled
            {
                get
                {
                    return anchoringState == MRAnchoringState.AnchorToHeadsetDynamicCrouching;
                }
            }
        }

        // This property allows the root angle updating to be temporarily paused from code.
        // When true, the root angle will not be updated, even if all other conditions are met.
        public bool PauseUpdateRootAngle { get; set; }

        public float RootAngle
        {
            get
            {
                return _rootAngle;
            }
        }

        public Quaternion RootRotation
        {
            get
            {
                return _rootRotation;
            }
        }

        private Avatar2.CAPI.ovrAvatar2Vector3f RootScale
        {
            get
            {
                if (!EnableRemoteScaling)
                {
                    return new Avatar2.CAPI.ovrAvatar2Vector3f(1.0f, 1.0f, 1.0f);
                }

                float inverseScale = 1.0f / RemoteScaleFactor;
                float adjustedScale = AdjustedRootScale;
                return new Avatar2.CAPI.ovrAvatar2Vector3f(adjustedScale, inverseScale, 1.0f);
            }
        }

        private float _rootAngle = 0.0f;
        private float _rootAngleStart = 0.0f;
        private bool _interpolatingRootAngle = false;
        private float _rootAngleInterpTime = 0.0f;
        private float _rootAngleInterpTotalTime = 0.0f;
        private bool _initialRootAngleSet = false;

        private Vector3 _rootPosition = Vector3.zero;
        private Quaternion _rootRotation = Quaternion.identity;

        private float _upperBodyAngle = 0.0f;
        private float _upperBodyAngleDeviation = 0.0f;

        private OvrAvatarDefaultStateListener? _defaultStateListener;
        private Coroutine? _animationTransitionRoutine;
        private bool _isExitingDefaultState;

        public float LeftArmBlendFactor
        {
            get => _leftArmBlendFactor;
            set
            {
                if (Entity == null)
                {
                    OvrAvatarLog.LogError("LeftArmBlendFactor: Entity is null. Unable to send event.");
                    return;
                }

                if (_leftArmBlendFactorEvent == null)
                {
                    return;
                }

                _leftArmBlendFactor = Mathf.Clamp01(value);
                if (_inputBlended)
                {
                    Entity?.SendEvent(_leftArmBlendFactorEvent, _leftArmBlendFactor);
                }
#if UNITY_EDITOR
                _cachedLeftArmBlendFactor = _leftArmBlendFactor;
#endif
            }
        }

        public float RightArmBlendFactor
        {
            get => _rightArmBlendFactor;
            set
            {
                if (Entity == null)
                {
                    OvrAvatarLog.LogError("RightArmBlendFactor: Entity is null. Unable to send event.");
                    return;
                }

                if (_rightArmBlendFactorEvent == null)
                {
                    return;
                }

                _rightArmBlendFactor = Mathf.Clamp01(value);
                if (_inputBlended)
                {
                    Entity?.SendEvent(_rightArmBlendFactorEvent, _rightArmBlendFactor);
                }
#if UNITY_EDITOR
                _cachedRightArmBlendFactor = _rightArmBlendFactor;
#endif
            }
        }

        public bool EnableRemoteScaling => _enableRemoteScaling;

        public float RemoteScaleFactor
        {
            get
            {
                if (!EnableRemoteScaling)
                {
                    return 1.0f;
                }

                return _remoteScaleFactor;
            }
            set
            {
                if (value < OvrAvatarManager.AvatarRemoteDynamicScalingInputMin || value > OvrAvatarManager.AvatarRemoteDynamicScalingInputMax)
                {
                    OvrAvatarLog.LogWarning($"RemoteScaleFactor must be in the range OvrAvatarManager.AvatarRemoteDynamicScalingInputMin ({OvrAvatarManager.AvatarRemoteDynamicScalingInputMin}) to OvrAvatarManager.AvatarRemoteDynamicScalingInputMax ({OvrAvatarManager.AvatarRemoteDynamicScalingInputMax})");
                    _remoteScaleFactor = Mathf.Clamp(value, OvrAvatarManager.AvatarRemoteDynamicScalingInputMin, OvrAvatarManager.AvatarRemoteDynamicScalingInputMax);
                }
                else
                {
                    _remoteScaleFactor = value;
                }
            }
        }

        private float AdjustedRootScale
        {
            get
            {
                if (!EnableRemoteScaling)
                {
                    return 1.0f;
                }

                float minScale = OvrAvatarManager.AvatarRemoteDynamicScalingInputMin;
                float maxScale = OvrAvatarManager.AvatarRemoteDynamicScalingInputMax;

                float inputScaleRange = maxScale - minScale;

                float normalizedScale = (RemoteScaleFactor - minScale) / inputScaleRange;

                float minOutputScale = OvrAvatarManager.AvatarRemoteDynamicScalingOutputMin;
                float maxOutputScale = OvrAvatarManager.AvatarRemoteDynamicScalingOutputMax;

                float outputScaleRange = maxOutputScale - minOutputScale;

                return (normalizedScale * outputScaleRange) + minOutputScale;
            }
        }

        public float CustomAnimationBlendFactor
        {
            get => _customAnimationBlendFactor;
            set
            {
                _customAnimationBlendFactor = Mathf.Clamp01(value);
#if UNITY_EDITOR
                _cachedCustomAnimBlendFactor = _customAnimationBlendFactor;
#endif
                // Only update blend factor if we have both rigs
                if (_defaultRigInfo != null && _humanoidRigInfo != null)
                {
                    _defaultRigInfo!.Animator.enabled = _customAnimationBlendFactor < 1;

                    // Ensure that at least the avatar's skeleton has been loaded before we update the blend factor.
                    if (Entity?.CurrentState >= OvrAvatarEntity.AvatarState.Skeleton)
                    {
                        OvrAvatarManager.Instance.UpdateAnimatedAvatarBlendFactor(Entity, _customAnimationBlendFactor);
                    }
                }
            }
        }

        public Animator? CustomAnimator
        {
            get => _humanoidRigInfo?.Animator;
        }

        protected sealed override string MainBehavior
        {
            get => _inputBlended ? BLENDED_ANIMATION_BEHAVIOR : BASIC_ANIMATION_BEHAVIOR;
            set => throw new System.NotSupportedException();
        }

        protected sealed override string FirstPersonOutputPose
        {
            get => FIRST_PERSON_POSE;
            set => throw new System.NotImplementedException();
        }

        protected sealed override string ThirdPersonOutputPose
        {
            get => _inputBlended ? BLENDED_ANIMATION_THIRD_PERSON_POSE : BASIC_ANIMATION_THIRD_PERSON_POSE;
            set => throw new System.NotImplementedException();
        }

        protected sealed override string? CustomBehaviorZipFilePath
        {
            get => GetCustomBehaviorZipFilePath();
            set => throw new System.NotImplementedException();
        }

        protected string GetCustomBehaviorZipFilePath()
        {
            if (_inputBlended)
            {
                if (_mixedRealityAnimationOptions.HeadsetAnchoringEnabled)
                {
                    return BLENDED_TRACKING_ANCHORED_ANIMATION_BEHAVIOR_FILE_PATH;
                }
                return BLENDED_ANIMATION_BEHAVIOR_FILE_PATH;
            }
            else
            {
                return BASIC_ANIMATION_BEHAVIOR_FILE_PATH;
            }
        }

        // Use this property to check if the animation controller is
        // currently in the sitting state, and to toggle sitting on or off.
        public bool IsSitting
        {
            get
            {
                if (_defaultRigInfo != null && _defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                {
                    return controller.IsSitting;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (_defaultRigInfo != null && _defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                {
                    controller.IsSitting = value;
                }
            }
        }

        private void RegisterAnimatedAvatar()
        {
            OvrAvatarManager.RigInfo? defaultRigInfo = null;
            OvrAvatarManager.RigInfo? humanoidRigInfo = null;
            OvrAvatarManager.PuppeteerInfo? puppeteerInfo = null;

            if (_defaultRigInfo != null)
            {
                defaultRigInfo = new OvrAvatarManager.RigInfo
                {
                    Root = _defaultRigInfo.Root,
                    RigMap = _defaultRigInfo.RigMap,
                };
            }

            if (_humanoidRigInfo != null)
            {
                humanoidRigInfo = new OvrAvatarManager.RigInfo
                {
                    Root = _humanoidRigInfo.Root,
                    RigMap = _humanoidRigInfo.RigMap,
                };
            }

            // default rig only no blending
            if (defaultRigInfo != null && humanoidRigInfo == null)
            {
                puppeteerInfo = new OvrAvatarManager.PuppeteerInfo(defaultRigInfo!, null);
            }
            // humanoid rig only no blending
            else if (humanoidRigInfo != null && defaultRigInfo == null)
            {
                puppeteerInfo = new OvrAvatarManager.PuppeteerInfo(null, humanoidRigInfo!);
            }
            // default and humanoid rig with blending
            else if (humanoidRigInfo != null && defaultRigInfo != null)
            {
                puppeteerInfo = new OvrAvatarManager.PuppeteerInfo(defaultRigInfo, humanoidRigInfo, CustomAnimationBlendFactor);
            }

            if (puppeteerInfo != null)
            {
                puppeteerInfo.IsInputBlended = _inputBlended;
                puppeteerInfo.HeadsetAnchoring = _mixedRealityAnimationOptions.HeadsetAnchoringEnabled;
                puppeteerInfo.EnableStaticAnimationOptimization = _enableStaticAnimationOptimization;

                // Delegate method for getting the position and rotation of the root; we need this
                // so we can determine the world position of the avatar's foot when foot planting
                // is enabled.
                puppeteerInfo.GetRootPose = (out Vector3 rootPosition, out Quaternion rootRotation) =>
                {
                    rootPosition = _rootPosition;
                    rootRotation = _rootRotation;
                };

                // Delegate method for getting the pose of the entity; currently used for debug visualizations.
                puppeteerInfo.GetEntityPose = (out Vector3 entityPosition, out Quaternion entityRotation) =>
                {
                    if (Entity == null)
                    {
                        entityPosition = Vector3.zero;
                        entityRotation = Quaternion.identity;
                    }
                    else
                    {
                        entityPosition = Entity.transform.position;
                        entityRotation = Entity.transform.rotation;
                    }
                };

                // Delegate method for getting the foot plant status for both feet.
                puppeteerInfo.GetFootPlantStatus = (out float leftFootPlant, out float rightFootPlant) =>
                {
                    Animator? customAnimator = CustomAnimator;

                    // Foot planting will only be active when we're entirely using the default rig, or entirely
                    // using custom animations. If there's a blend of the two, foot planting will not be active.
                    if (Mathf.Approximately(1.0f, CustomAnimationBlendFactor) && customAnimator != null)
                    {
                        // Use custom animation foot planting parameters.
                        leftFootPlant = customAnimator.GetFloat("LeftFootPlanted");
                        rightFootPlant = customAnimator.GetFloat("RightFootPlanted");
                    }
                    else if (Mathf.Approximately(0.0f, CustomAnimationBlendFactor) && _defaultRigInfo != null &&
                        _defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                    {
                        // Use default rig foot planting parameters.
                        leftFootPlant = controller.GetLeftFootPlant();
                        rightFootPlant = controller.GetRightFootPlant();
                    }
                    else
                    {
                        // Disable foot planting.
                        leftFootPlant = 0.0f;
                        rightFootPlant = 0.0f;
                    }
                };

                // Delegate method for getting if the animation controller is currently in a state transition.
                puppeteerInfo.GetIsControllerInStateTransition = (int layerIndex) =>
                {
                    // Attempt to get the foot plant status for each foot from the MecanimLegsAnimationController.
                    if (_defaultRigInfo != null && _defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                    {
                        return controller.IsInTransition(layerIndex);
                    }
                    else
                    {
                        return false;
                    }
                };

                // Delegate method for getting the entity that's using the PuppeteerInfo.
                puppeteerInfo.GetEntityCallback = () =>
                {
                    return Entity;
                };

                // Delegate method to try fetch float channel values from animator
                puppeteerInfo.TryGetDataChannelValue = (PuppeteerInfo.RigType rigType, string channelName, out float channelValue) =>
                {
                    if (rigType == PuppeteerInfo.RigType.Default)
                    {
                        channelValue = _defaultRigInfo!.Animator.GetFloat(channelName);
                        return true;
                    }

                    if (rigType != RigType.Default)
                    {
                        OvrAvatarLog.LogError($"Only support fetching animation channel values from default animation for now.");
                    }

                    channelValue = 0;
                    return false;
                };


                // Delegate method to pull all the float value animation parameters from the animator
                puppeteerInfo.GetAvaiableDataChannels = (rigType) =>
                {
                    if (rigType == RigType.Both)
                    {
                        OvrAvatarLog.LogAssert("Function can only query data channels for one rig type at a time, not both");
                    }

                    var rig = rigType == RigType.Default ? _defaultRigInfo : _humanoidRigInfo;
                    return rig?.AnimatorFloatChannels ?? new HashSet<string>();
                };

                puppeteerInfo.CheckIsAnimating = (rigType) =>
                {
                    if (rigType == RigType.Default)
                    {
                        return _defaultRigInfo?.IsAnimating() == true;
                    }
                    else if (rigType == RigType.Humanoid)
                    {
                        return _humanoidRigInfo?.IsAnimating() == true;
                    }
                    else
                    {
                        if (_customAnimationBlendFactor is > 0 and < 1)
                        {
                            return _defaultRigInfo?.IsAnimating() == true && _humanoidRigInfo?.IsAnimating() == true;
                        }
                        else if (Mathf.Approximately(0, _customAnimationBlendFactor))
                        {
                            return _defaultRigInfo?.IsAnimating() == true;
                        }
                        else
                        {
                            return _humanoidRigInfo?.IsAnimating() == true;
                        }
                    }
                };
            }

            OvrAvatarManager.Instance.RegisterAnimatedAvatar(Entity, puppeteerInfo);
            OvrAvatarManager.Instance.OnAvatarFootFall.AddListener(ProcessAvatarFootFall);
        }

        private void UnRegisterAnimatedAvatar()
        {
            var avatarManager = OvrAvatarManager.Instance;
            if (avatarManager != null && Entity != null && Entity.IsCreated == true)
            {
                OvrAvatarManager.Instance.UnregisterAnimatedAvatar(Entity);
            }
        }

        protected override void OnUserAvatarLoaded(OvrAvatarEntity entity)
        {
            base.InitializeBehaviorSystem(MainBehavior, FirstPersonOutputPose, ThirdPersonOutputPose, CustomBehaviorZipFilePath);

            if (_inputBlended)
            {
                _rootAngleEvent ??= BehaviorSystem.EventController<float>.Register(ROOT_ANGLE_EVENT_NAME);
                _rootTranslationEvent ??= BehaviorSystem.EventController<Oculus.Avatar2.CAPI.ovrAvatar2Vector3f>.Register(ROOT_TRANSLATION_EVENT_NAME);
                _rootScaleEvent ??= BehaviorSystem.EventController<Oculus.Avatar2.CAPI.ovrAvatar2Vector3f>.Register(ROOT_SCALE_EVENT_NAME);
                _leftArmBlendFactorEvent ??= BehaviorSystem.EventController<float>.Register(LEFT_ARM_BLEND_EVENT_NAME);
                _rightArmBlendFactorEvent ??= BehaviorSystem.EventController<float>.Register(RIGHT_ARM_BLEND_EVENT_NAME);
            }

            if (!_customAnimationOnly)
            {
                var defaultRigPrefab = Resources.Load<GameObject>(_defaultRtRigPrefab);
                _defaultRigInfo = InstantiateRig(defaultRigPrefab, out var defaultRoot);

                if (_defaultRigInfo == null)
                {
                    OvrAvatarLog.LogError("Error in instantiating default rig prefab.");
                    return;
                }

                if (_defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                {
                    controller.AssignEntity(entity);
                }
                else
                {
                    OvrAvatarLog.LogError("Unable to find MecanimLegsAnimationController in default rt rig");
                }
            }

            if (_customRigPrefab != null)
            {
                _humanoidRigInfo = InstantiateRig(_customRigPrefab, out var humanoidRoot);
#if UNITY_EDITOR
                if (_enableDebugRigVisual && humanoidRoot != null)
                {
                    var visualizer = humanoidRoot.gameObject.AddComponent<RigVisualizerUtility>();
                    visualizer.Initialize(humanoidRoot, Color.green);
                }
#endif
            }

            if (_defaultRigInfo != null && _humanoidRigInfo != null)
            {
                _defaultStateListener = _humanoidRigInfo.Animator.GetBehaviour<OvrAvatarDefaultStateListener>();
                if (_defaultStateListener == null)
                {
                    OvrAvatarLog.LogError("Unable to find OvrAvatarAnimationTransitionBehavior inside custom animation graph. Add a default animation state with"
                        + "OvrAvatarAnimationTransitionBehavior attached to control transition between default/custom animation");
                }
                else
                {
                    _defaultStateListener.OnEnterState += OnDefaultStateEnter;
                    _defaultStateListener.OnUpdateState += OnDefaultStateUpdate;
                }
            }

            if (_mixedRealityAnimationOptions.HeadsetAnchoringEnabled)
            {
                // First, search for the LeftFootBall joint.
                Transform jointTransform = transform.Find("Joint LeftFootBall");
                if (jointTransform != null)
                {
                    _ballJointTransform = jointTransform;
                }
                else
                {
                    // If we couldn't find LeftFootBall, try searching for RightFootBall.
                    jointTransform = transform.Find("Joint RightFootBall");
                    if (jointTransform != null)
                    {
                        _ballJointTransform = jointTransform;
                    }
                    else
                    {
                        OvrAvatarLog.LogInfo("Could not find critical joint for LeftFootBall or RightFootBall; if you want to use dynamic crouching with headset anchoring, one of these joints must be defined for the avatar.");
                    }
                }
            }

            RegisterAnimatedAvatar();

            // Set initial blend factors
#if UNITY_EDITOR
            UpdateBlendFactors();
#else
            LeftArmBlendFactor = _leftArmBlendFactor;
            RightArmBlendFactor = _rightArmBlendFactor;
            CustomAnimationBlendFactor = _customAnimationBlendFactor;
#endif
        }

        protected override void OnEntityPreTeardown(OvrAvatarEntity entity)
        {
            base.OnEntityPreTeardown(entity);
            UnRegisterAnimatedAvatar();
        }

        private void OnDefaultStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animator.IsInTransition(layerIndex) || _isExitingDefaultState)
            {
                return;
            }

            var nextState = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (nextState.fullPathHash.Equals(stateInfo.fullPathHash))
            {
                _isExitingDefaultState = true;
                TriggerAnimationTransition(animator, layerIndex, startValue: 0, endValue: 1);
            }
        }

        private void OnDefaultStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _isExitingDefaultState = false;
            TriggerAnimationTransition(animator, layerIndex, startValue: 1, endValue: 0);
        }

        private void TriggerAnimationTransition(Animator animator, int layerIndex, float startValue, float endValue)
        {
            if (_animationTransitionRoutine != null)
            {
                StopCoroutine(_animationTransitionRoutine);
            }

            _animationTransitionRoutine = StartCoroutine(AnimateTransition(animator, layerIndex, startValue, endValue));
        }

        private IEnumerator AnimateTransition(Animator animator, int layerIndex, float startBlendValue, float endBlendValue)
        {
            if (_humanoidRigInfo == null || _defaultRigInfo == null)
            {
                OvrAvatarLog.LogAssert("Missing humanoid rig or default rig. Both rig must exist in order to animate transition between the two.");
            }

            if (!animator.IsInTransition(layerIndex))
            {
                yield return null;
            }

            AnimatorTransitionInfo transition = animator.GetAnimatorTransitionInfo(layerIndex);
            var duration = transition.duration;
            _defaultRigInfo!.Animator.enabled = true;

            if (!Mathf.Approximately(startBlendValue, endBlendValue))
            {
                float time = (Mathf.Abs(CustomAnimationBlendFactor - startBlendValue) / Mathf.Abs(endBlendValue - startBlendValue)) * duration;
                while (time < duration)
                {
                    CustomAnimationBlendFactor = Mathf.Lerp(startBlendValue, endBlendValue, time / duration);
                    time += Time.deltaTime;
                    yield return null;
                }
            }

            CustomAnimationBlendFactor = endBlendValue;
            _animationTransitionRoutine = null;

            _defaultRigInfo!.Animator.enabled = Mathf.Approximately(endBlendValue, 0);
            _isExitingDefaultState = false;
        }

        private Dictionary<string, Transform> GenerateRigMap(Transform rootTransform)
        {
            Transform[] transforms = rootTransform.GetComponentsInChildren<Transform>();
            Dictionary<string, Transform> rigMap = new Dictionary<string, Transform>();
            for (var i = 0; i < transforms.Length; i++)
            {
                var childTransform = transforms[i];
                rigMap.Add(childTransform.name, childTransform);
            }

            return rigMap;
        }

        private RigInfo? InstantiateRig(GameObject rigPrefab, out Transform? root)
        {
            var rig = Instantiate(rigPrefab, this.transform);
            rig.name = $"{gameObject.name}-{rigPrefab.name}-rig";

            if (!rig.TryGetComponent<OvrAvatarRigRootIdentifier>(out var rootIdentifier))
            {
                OvrAvatarLog.LogError($"Unable find OvrAvatarRigRootIdentifier in animation rig");
            }

            root = rootIdentifier!.RigRoot;
            if (!rig.TryGetComponent<Animator>(out var animator))
            {
                OvrAvatarLog.LogError($"Unable to find Animator in given rig {rig.name}");
            }

            if (root == null)
            {
                OvrAvatarLog.LogError("RigRoot has not been set");
            }

            Dictionary<string, Transform> rigMap = GenerateRigMap(root!);

            if (root == null)
            {
                OvrAvatarLog.LogError("RigRoot has not been set");
                return null;
            }

            return new RigInfo(root, rig, animator, rigMap);
        }

        private void OnDestroy()
        {
            _rootAngleEvent = null;
            _rootTranslationEvent = null;
            _rootScaleEvent = null;
            _leftArmBlendFactorEvent = null;
            _rightArmBlendFactorEvent = null;

            UnRegisterAnimatedAvatar();

            if (_defaultRigInfo != null)
            {
                Destroy(_defaultRigInfo.Rig);
            }

            if (_humanoidRigInfo != null)
            {
                if (_defaultStateListener != null)
                {
                    _defaultStateListener.OnEnterState -= OnDefaultStateEnter;
                    _defaultStateListener.OnUpdateState -= OnDefaultStateUpdate;
                }

                Destroy(_humanoidRigInfo.Rig);
            }
        }

        private void Update()
        {
            if (Entity == null || !Entity.BehaviorSystemEnabled || !OvrAvatarManager.hasInstance)
            {
                return;
            }
#if UNITY_EDITOR
            // Update blend factors based on inspector inputs
            UpdateBlendFactors();
#endif

            if (_mixedRealityAnimationOptions.HeadsetAnchoringEnabled && _ballJointTransform != null)
            {
                if (_defaultRigInfo != null && _defaultRigInfo.Rig.TryGetComponent<MecanimLegsAnimationController>(out var controller))
                {
                    // Is the ball joint transform below the entity?
                    float jointY = _ballJointTransform.position.y;
                    float entityY = Entity.transform.position.y;

                    if (_mixedRealityAnimationOptions.DynamicCrouchingEnabled && jointY < entityY)
                    {
                        float strength = Mathf.Max(0.0f, Mathf.Min(1.0f, (entityY - jointY) / _dynamicCrouchFactor));

                        controller.forceEnableCrouch = true;
                        controller.forceCrouchStrength = strength;
                    }
                    else
                    {
                        controller.forceEnableCrouch = false;
                    }
                }
            }

            if (Entity == null ||
                _rootAngleEvent == null ||
                !PollRootPose(out var rootEulerAngles, out var rootPosition))
            {
                return;
            }

            UpdateUpperBodyRotationAngle(rootEulerAngles);

            // Update avatar root angle every frame that PauseUpdateRootAngle isn't enabled.
            if (!PauseUpdateRootAngle)
            {
                SyncUpperAndLowerBodyRotation(rootEulerAngles);

                // For the root rotation quaternion, we're only concerned with the direction the avatar is
                // facing in the X-Z plane (i.e., the avatar's yaw), which is why we don't incorporate the
                // pitch or roll here.
                _rootRotation = Quaternion.AngleAxis(_rootAngle, Vector3.up);
                _rootPosition = rootPosition;
            }

            float eventRootAngle = -_rootAngle + 180.0f;
            Entity.SendEvent(_rootAngleEvent!, eventRootAngle);
            Entity.SendEventWithPositionPayload(_rootScaleEvent!, RootScale);

            if (_rootTranslationEvent != null)
            {
                Entity.SendEventWithPositionPayload(_rootTranslationEvent!, rootPosition);
            }
            else
            {
                OvrAvatarLog.LogWarning("Root translation event not found");
            }
        }

#if UNITY_EDITOR
        private void UpdateBlendFactors()
        {
            if (_inputBlended && (_defaultRigInfo != null || _humanoidRigInfo != null))
            {
                if (_cachedLeftArmBlendFactor != _leftArmBlendFactor)
                {
                    LeftArmBlendFactor = _leftArmBlendFactor;
                    _cachedLeftArmBlendFactor = _leftArmBlendFactor;
                }

                if (_cachedRightArmBlendFactor != _rightArmBlendFactor)
                {
                    RightArmBlendFactor = _rightArmBlendFactor;
                    _cachedRightArmBlendFactor = _rightArmBlendFactor;
                }
            }

            if (_cachedCustomAnimBlendFactor != _customAnimationBlendFactor)
            {
                CustomAnimationBlendFactor = _customAnimationBlendFactor;
                _cachedCustomAnimBlendFactor = _customAnimationBlendFactor;
            }
        }
#endif

        private bool PollRootPose(out Vector3 eulerAngles, out Vector3 position)
        {
            // Attempt to poll the current headset pose using the entity's body tracking context.
            if (Entity != null &&
                Entity.InputManager != null &&
                Entity.InputManager.InputTrackingProvider != null &&
                Entity.InputManager.InputTrackingProvider.GetInputTrackingState(out var state) &&
                state.headsetActive)
            {
                var pos = state.headset.position;
                var rot = state.headset.orientation;

                Quaternion headsetRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);

                Vector3 headsetUp = headsetRotation * Vector3.up;
                Vector3 headsetRight = headsetRotation * Vector3.right;

                // As the headset's right vector gets more closely aligned with
                // world up, we'll substitue it with the headset up vector.
                float rightWorldUpAngle = Vector3.Angle(Vector3.up, headsetRight);
                float rollAdjustment = 0.0f;

                if (rightWorldUpAngle < 45.0f)
                {
                    // Right approaching world up
                    headsetRight = -headsetUp;
                    rollAdjustment = 90.0f;
                }
                else if (rightWorldUpAngle > 135.0f)
                {
                    // Right approaching world down
                    headsetRight = headsetUp;
                    rollAdjustment = -90.0f;
                }

                // Flatten the headset right vector into the X-Z plane.
                Vector3 flatRight = Vector3.ProjectOnPlane(headsetRight, Vector3.up).normalized;

                // Use the flattend right vector to calculate the headset's forward vector in the X-Z plane.
                Vector3 flatForward = Vector3.Cross(flatRight, Vector3.up);

                // Pitch
                eulerAngles.x = headsetRotation.eulerAngles.x;

                // Yaw
                eulerAngles.y = Vector3.SignedAngle(Vector3.forward, flatForward, Vector3.up);

                // Roll
                eulerAngles.z = Vector3.SignedAngle(flatRight, headsetRight, flatForward) + rollAdjustment;

                position = Vector3.ProjectOnPlane(new Vector3(pos.x, pos.y, pos.z), Vector3.up);

                return true;
            }
            else
            {
                eulerAngles = Vector3.zero;
                position = Vector3.zero;
                return false;
            }
        }

        private void UpdateUpperBodyRotationAngle(Vector3 eulerAngles)
        {
            if (!_enableTrackedUpperBodyRotation || !OvrAvatarManager.hasInstance)
            {
                return;
            }

            // Using DeltaAngle keeps the angles in the range [-180,180], which is necessary
            // for us to properly normalize them into the correct range. (i.e., -2 degrees
            // and 358 degrees are equivalent rotations, but the second value would get
            // normalized incorrectly without additional checks in place)
            float pitch = Mathf.DeltaAngle(0.0f, eulerAngles.x); // Pitch controls front-back lean
            float roll = Mathf.DeltaAngle(0.0f, eulerAngles.z); // Roll controls side-to-side lean

            // Clamp the angles into the MaxUpperBodyRotationAngle range, and then normalize them into [-1,1].
            float maxAngle = OvrAvatarManager.Instance.MaxUpperBodyRotationAngle;

            float xAxisRotationFactor = Mathf.Clamp(pitch, -maxAngle, maxAngle) / maxAngle; // X-axis rotation
            float zAxisRotationFactor = Mathf.Clamp(roll, -maxAngle, maxAngle) / maxAngle; // Z-axis rotation
            float yAxisRotationFactor = 0.0f;

            // Special case for yaw, where we track it as the difference between upper and lower body rotation.
            // Disable yaw when PauseUpdateRootAngle is true.
            if (!PauseUpdateRootAngle)
            {
                float yaw = Mathf.DeltaAngle(_rootAngle, eulerAngles.y); // Yaw controls horizontal rotation
                _upperBodyAngle = eulerAngles.y;
                _upperBodyAngleDeviation = yaw;

                yAxisRotationFactor = Mathf.Clamp(yaw, -maxAngle, maxAngle) / maxAngle; // X-axis rotation
            }

            // Ensure that at least the avatar's skeleton has been loaded before we update the upper body rotation.
            if (Entity?.CurrentState >= OvrAvatarEntity.AvatarState.Skeleton)
            {
                OvrAvatarManager.Instance.UpdateAnimatedAvatarUpperBodyRotationFactors(Entity, new Vector3(xAxisRotationFactor, yAxisRotationFactor, zAxisRotationFactor));
            }
        }

        private void SyncUpperAndLowerBodyRotation(Vector3 eulerAngles)
        {
            // If independent upper body horizontal rotation is not enabled,
            // we'll keep the lower body in perfect sync with the upper body.
            // If this is the first update, and the initial root angle hasn't
            // been set yet, we'll go ahead and set it now; this ensures that
            // the upper body and lower body are in alignment when the avatar
            // first becomes active.
            if (!_enableIndependentUpperBodyHorizontalRotation || !_initialRootAngleSet)
            {
                _rootAngle = eulerAngles.y;
                _initialRootAngleSet = true;
                return;
            }

            if (_interpolatingRootAngle)
            {
                _rootAngleInterpTime -= Time.deltaTime;

                if (_rootAngleInterpTime <= 0.0f)
                {
                    _rootAngle = _upperBodyAngle;
                    _interpolatingRootAngle = false;
                }
                else
                {
                    float t = 1.0f - (_rootAngleInterpTime / _rootAngleInterpTotalTime);
                    _rootAngle = Mathf.LerpAngle(_rootAngleStart, _upperBodyAngle, t);
                }
            }
            // If independent upper body horizontal rotation is enabled, we only update
            // the root angle when the upper body rotation passes a certain threshold.
            else if (_upperBodyAngleDeviation < -_maxUpperBodyAngleDeviation || _upperBodyAngleDeviation > _maxUpperBodyAngleDeviation)
            {
                _interpolatingRootAngle = true;

                _rootAngleInterpTotalTime = Mathf.Abs(_upperBodyAngleDeviation) * (1.0f / _lowerBodyRotationDegreesPerSecond);

                _rootAngleInterpTime = _rootAngleInterpTotalTime;
                _rootAngleStart = _rootAngle;
            }
        }

        private void ProcessAvatarFootFall(OvrAvatarEntity entity, OvrAvatarManager.Side side)
        {
            // Is the foot fall event for the avatar this behavior is associated with?
            if (entity != Entity)
            {
                // If not, ignore it.
                return;
            }

            if (side == OvrAvatarManager.Side.Left)
            {
                // Left foot down
            }
            else
            {
                // Right foot down
            }
        }

        private class RigInfo
        {
            public Transform Root { get; }
            public GameObject Rig { get; }
            public Animator Animator { get; }
            public Dictionary<string, Transform> RigMap { get; }

            public HashSet<string> AnimatorFloatChannels { get; }

            public RigInfo(Transform root, GameObject rig, Animator animatior, Dictionary<string, Transform> rigMap)
            {
                Root = root;
                Rig = rig;
                Animator = animatior;
                RigMap = rigMap;
                AnimatorFloatChannels = new HashSet<string>();

                for (var i = 0; i < animatior.parameters.Length; i++)
                {
                    var param = animatior.parameters[i];
                    if (param.type == AnimatorControllerParameterType.Float)
                    {
                        AnimatorFloatChannels.Add(param.name);
                    }
                }
            }

            public bool IsAnimating()
            {
                return Animator != null && Animator.isActiveAndEnabled && !Mathf.Approximately(0, Animator.speed);
            }
        }
    }
}
