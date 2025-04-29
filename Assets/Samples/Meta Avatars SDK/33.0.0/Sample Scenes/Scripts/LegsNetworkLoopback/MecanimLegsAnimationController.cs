#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Oculus.Avatar2.Experimental;
using System.Collections.Generic;

namespace Oculus.Avatar2
{
    public class MecanimLegsAnimationController : MonoBehaviour, IUIControllerInterface
    {
        public enum MovementParameter
        {
            None,

            MoveForward,
            MoveBackward,
            MoveLeft,
            MoveRight,

            MoveForwardLeft,
            MoveForwardRight,
            MoveBackwardLeft,
            MoveBackwardRight,

            TurnLeft,
            TurnRight,

            Crouch,

            Sitting,
        }

        // Stuff that would go in a Walk-in-place controller
        private const float MaxSpeed = 10.0f;
        private const float SmoothTime = 0.1f;

        private const float ForwardLateralAngleThreshold = 45.0f;
        private const float LateralAngleThreshold = 67.5f;
        private const float BackwardLateralAngleThreshold = 112.5f;
        private const float BackwardAngleThreshold = 135.0f;

        private const float SmallLeftTurnThreshold = 10.0f;
        private const float LargeLeftTurnThreshold = 360.0f;

        private const float SmallRightTurnThreshold = -10.0f;
        private const float LargeRightTurnThreshold = -360.0f;

        private const float WalkSpeedThreshold = 0.1f;
        private const float RunSpeedThreshold = 10.0f;
        private const float MoveSpeedRange = RunSpeedThreshold - WalkSpeedThreshold;

        private const float SittingTurnMinAngle = 60.0f;
        private const float SittingTurnMaxAngle = -60.0f;
        private const float SittingTurnAngleRange = SittingTurnMaxAngle - SittingTurnMinAngle;

        // Velocity vector of the avatar
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _acceleration = Vector3.zero;

        private Vector3 _previousHeadPosition = Vector3.zero;

        // The velocity threshold is used to determine if an avatar is staying relatively still. With headset
        // tracking the actual velocity, even when trying to be perfectly still, will rarely reach zero.
        // Using a low value for the threshold here allows us to determine when the avatar is effectively
        // not moving for the purposes of the animation controller.
        private const float _velocityThreshold = 0.1f;
        private const float _velocityThresholdSq = _velocityThreshold * _velocityThreshold;

        private Vector3 _lastMovePosition = Vector3.zero;

        private bool _isMoving = false;

        // The initial head height is used to determine when the Avatar is crouching.
        // It's set from the head position when tracking first becomes active on the entity.
        // TODO: we will need the means reset/repoll this value if tracking is reset/recentered.
        private float _initialHeadHeight = 0.0f;
        bool _initialPositionPolled = false;

        // The current height of the head, after it's been damped. This is used for calculating if the avatar
        // is crouching and how deep/shallow the crouch is.
        private float _currentHeadHeight = 0.0f;
        private float _headHeightVelocity = 0.0f;
        private float _crouchSmoothTime = 0.2f;

        private Vector3 _previousBodyDirection = Vector3.forward;
        private Vector3 _lowerBodyDirection = Vector3.forward;
        private Quaternion _lowerBodyRotation = Quaternion.identity;

        private float _angularVelocity = 0.0f;
        private float _angularAcceleration = 0.0f;

        // The length, in seconds, of the walk cycle (i.e., the time between the same foot making contact with the ground).
        public float _animationCyclePeriod = 1.0f;

        private float _moveSpeed = 0.0f;

        private bool _movingForward = false;
        private bool _movingBackward = false;
        private bool _movingLeft = false;
        private bool _movingRight = false;

        private bool _turnLeft = false;
        private bool _turnRight = false;
        private float _turnStrength = 0.0f;

        private bool _crouching = false;
        private float _crouchStrength = 0.0f; // 0 == fully standing, 1 == fully crouched

        private bool _wasSitting = false;
        private float _sittingTurnStrength = 0.5f;
        private Vector3 _startSittingDirection = Vector3.forward;

        private const string _turnInPlaceLayerName = "TurnInPlaceLayer";
        private int _turnInPlaceLayerIndex = -1;

        private bool _thumbstickWasInDeadzone = true;
        private const float _thumbstickDeadzone = 0.2f;

        // These are the weights assigned to the Turn In Place layer in the animation
        // controller during normal operation, and when crouching is active. We use a
        // lower weight when crouching so that the turn animation will blend with the
        // crouch animation without fully overwriting it.
        private const float _turnLayerNormalWeight = 1.0f;
        private const float _turnLayerCrouchWeight = 0.5f;

        // Stuff that could probably go in a base class
        private Animator? _animator;

        [SerializeField]
        protected OvrAvatarEntity? _entity;

        [SerializeField]
        [Range(0.0f, 0.5f)]
        [Tooltip("This value controls how many units the avatar must move before the linear motion animations will start to play. This allows the avatar to shift in place without lifting their feet.")]
        private float _movementThreshold = 0.05f;

        [SerializeField]
        [Tooltip("When true, the avatar will be able to enter the crouching state when the tracked headset position goes below the required height threshold. When false, crouching is disabled.")]
        private bool _enableCrouching = true;

        // This is how far below the initial head position the head must go to start triggering the crouch.
        [SerializeField]
        [Range(-1.0f, 0.0f)]
        [Tooltip("This value controls how far below the initial headset position the tracked pose must go before the avatar enters the crouching state. This assumes that the user starts in the standing position.")]
        private float _crouchStartThreshold = -0.3f;

        // This is how far the Avatar must move vertically to go from
        // standing upright at the crouch start threshold to fully crouched.
        [SerializeField]
        [Range(0.0f, 2.0f)]
        [Tooltip("This value controls how far the tracked pose must move vertically for the avatar to go from fully upright at the crouch start threshold to being fully crouched.")]
        private float _crouchRange = 0.3f;

        [SerializeField]
        [Tooltip("Enable this have the avatar start sitting in their current position.")]
        protected bool _isSitting = false;
        public bool IsSitting
        {
            get => _isSitting;
            set => _isSitting = value;
        }


        [SerializeField]
        [Tooltip("Enable this to be able to override the animation controller parameters using the properties below.")]
        public bool _useOverrideParameters = false;

        [SerializeField]
        public MovementParameter _movementParameterOverride = MovementParameter.None;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float _turnStrengthOverride = 0.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float _sittingTurnStrengthOverride = 0.5f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float _moveSpeedOverride = 0.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float _crouchStrengthOverride = 0.0f;

        [SerializeField]
        [Tooltip("Enable this to have the avatar automatically return to a standing position if " +
            "there is no motion after the Crouch Timeout Duration expires.")]
        public bool enableCrouchTimeout = true;

        [SerializeField]
        [Tooltip("When the Crouch Timeout is enabled, this value determines how long the avatar will remain in " +
            "the crouching position before automatically returning to the standing position when not moving.")]
        public float crouchTimeoutDuration = 5.0f;

        // Timer that controls when a crouch timeout expires.
        private float _crouchTimer = 0.0f;

        // When set to true, this denotes that there's a crouch timeout that's actively counting down.
        private bool _crouchTimeoutActive = false;

        public bool forceEnableCrouch = false;
        public float forceCrouchStrength = 0.0f;

        public void AssignEntity(OvrAvatarEntity entity)
        {
            if (_entity == null)
            {
                _entity = entity;

                // Verify that the entity has an OvrAvatarAnimationBehavior component; if not, log an error.
                if (GetTargetEntityAnimationBehavior() == null)
                {
                    OvrAvatarLog.LogError("Entity does not have required OvrAvatarAnimationBehavior component");
                }
            }
            else
            {
                OvrAvatarLog.LogError("Entity already exists. Attempting to assign entity a second time in MecanimLegsAnimationController");
            }
        }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            UpdateInput();
            UpdateState();
            SendEvents();
        }

        void UpdateInput()
        {
#if USING_XR_SDK
            if (!UIManager.IsPaused)
            {
                if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                {
                    // Toggle sitting on and off
                    IsSitting = !IsSitting;
                }
                else if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                {
                    RecalibrateStandingHeight();
                }

                Vector2 thumbstickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                if (Mathf.Abs(thumbstickInput.x) >= _thumbstickDeadzone)
                {
                    if (_thumbstickWasInDeadzone && _entity != null)
                    {
                        // Rotate the avatar's GameObject 45 degrees to the left or right, depending on the sign of the thumbstick x-axis input.
                        _entity.transform.rotation *= Quaternion.AngleAxis(45.0f * Mathf.Sign(thumbstickInput.x), Vector3.up);
                    }

                    // We require that the thumbstick must return to the deadzone before allowing the rotation to be
                    // applied again. This prevents accidentally over-rotating if you hold the thumbstick down.
                    _thumbstickWasInDeadzone = false;
                }
                else
                {
                    _thumbstickWasInDeadzone = true;
                }
            }
#endif
        }

        void UpdateState()
        {
            if (!PollInitialPosition())
            {
                return;
            }

            UpdateSitting();

            _previousBodyDirection = _lowerBodyDirection;
            _lowerBodyRotation = GetLowerBodyRotation();
            _lowerBodyDirection = _lowerBodyRotation * Vector3.forward;

            // Get the current position of the head.
            var headPositionVector = GetHeadPosition();
            if (headPositionVector == null)
            {
#if USING_XR_SDK
                // Only show error when running on device, or in editor if
                // the necessary input tracker provider is set up.
                if (OvrAvatarUtility.IsHeadsetActive())
                {
                    OvrAvatarLog.LogError("Failed to update state, no head position found");
                }
#endif
                return;
            }

            Vector3 headPosition = headPositionVector.Value;

            // Calculate the delta between the current position of the head, and it's position at the last update.
            var deltaHeadPosition = headPosition - _previousHeadPosition;
            deltaHeadPosition = Vector3.ProjectOnPlane(deltaHeadPosition, Vector3.up);

            // Use damping to gradually accelerate the walk animation; helps to reduce
            // high frequency noise (i.e., rapid twitching) during small movements.
            var targetVelocity = deltaHeadPosition / Time.deltaTime;
            _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _acceleration, SmoothTime);

            // To prevent animations from triggering at the slightest movement and introducing a lot of noise/jitter,
            // we don't allow the linear motion animations to trigger until the avatar as moved past a certain threshold.
            if (!_isMoving)
            {
                // Determine if the avatar has moved enough to be considering moving.
                if (Vector3.SqrMagnitude(_lastMovePosition - headPosition) > _movementThreshold * _movementThreshold)
                {
                    // Start moving
                    _isMoving = true;
                    _lastMovePosition = headPosition;
                }
            }
            else
            {
                // If the velocity is above the threshold, we're still moving.
                if (_velocity.sqrMagnitude > _velocityThresholdSq)
                {
                    // Continue moving
                    _lastMovePosition = headPosition;
                }
                else
                {
                    // Stop moving
                    _isMoving = false;
                }
            }

            // Apply the inverse of the lower body rotation to get the relative velocity vector (i.e., a positive value
            // on the z-axis means the avatar is moving forward, a negative value on the x-axis means it's moving left, etc.)
            Vector3 relativeVelocity = Quaternion.Inverse(_lowerBodyRotation) * _velocity;

            if (_crouchTimeoutActive && _crouchTimer > 0.0f)
            {
                _crouchTimer -= Time.deltaTime;
            }

            _movingForward = false;
            _movingBackward = false;
            _movingLeft = false;
            _movingRight = false;

            // We'll use the unsigned angle between the relative velocity vector
            // and the forward vector to determine how the avatar is moving.
            float movementAngle = Vector3.Angle(relativeVelocity, Vector3.forward);

            // Only process movement if we're not sitting.
            if (!_isSitting && _isMoving)
            {
                bool lateralMovement = false;

                if (movementAngle <= ForwardLateralAngleThreshold)
                {
                    // Forward
                    _movingForward = true;
                }
                else if (movementAngle <= LateralAngleThreshold)
                {
                    // Forward lateral
                    _movingForward = true;
                    lateralMovement = true;
                }
                else if (movementAngle <= BackwardLateralAngleThreshold)
                {
                    // Lateral
                    lateralMovement = true;
                }
                else if (movementAngle <= BackwardAngleThreshold)
                {
                    // Backward lateral
                    _movingBackward = true;
                    lateralMovement = true;
                }
                else
                {
                    // Backward
                    _movingBackward = true;
                }

                // If we have lateral movement, determine if its to the left or right.
                if (lateralMovement)
                {
                    if (relativeVelocity.x < 0.0f)
                    {
                        _movingLeft = true;
                    }
                    else
                    {
                        _movingRight = true;
                    }
                }
            }

            _moveSpeed = Mathf.Clamp((relativeVelocity.magnitude - WalkSpeedThreshold) / MoveSpeedRange, 0.0f, 1.0f);

            _previousHeadPosition = headPosition;

            // Calculate the change in angle from the last update.
            float deltaAngle = Vector3.SignedAngle(_lowerBodyDirection, _previousBodyDirection, Vector3.up);

            // The target angular velocity is the amount we angle has changed over the delta time since the previous update.
            float targetAngularVelocity = deltaAngle / Time.deltaTime;

            // Use the target angular velocity to calculate the current angular velocity using a smooth damp to
            // help mitigate high frequency noise.
            _angularVelocity = Mathf.SmoothDamp(_angularVelocity, targetAngularVelocity, ref _angularAcceleration, SmoothTime);

            _turnLeft = false;
            _turnRight = false;
            _turnStrength = 0.0f;
            _sittingTurnStrength = 0.5f;

            _crouching = false;
            _crouchStrength = 0.0f;

            // Sitting
            if (_isSitting)
            {
                Vector3 currentHeadVector = GetHeadVector();

                float angle = Vector3.SignedAngle(currentHeadVector, _startSittingDirection, Vector3.up);
                _sittingTurnStrength = Mathf.Clamp((angle - SittingTurnMinAngle) / SittingTurnAngleRange, 0.0f, 1.0f);
            }
            else
            {
                // Only process turning in place if we're not moving and not sitting.
                if (!_movingForward && !_movingBackward && !_movingLeft && !_movingRight)
                {
                    // Turn left
                    if (_angularVelocity > SmallLeftTurnThreshold)
                    {
                        _turnLeft = true;
                        _turnStrength = Mathf.Min(_angularVelocity / LargeLeftTurnThreshold, 1.0f);
                    }
                    // Turn right
                    else if (_angularVelocity < SmallRightTurnThreshold)
                    {
                        _turnRight = true;
                        _turnStrength = Mathf.Min(_angularVelocity / LargeRightTurnThreshold, 1.0f);
                    }
                }

                // We damp the head height for calculating crouching so that small fluctuations
                // in the value won't cause the animation to rapidly switch direction.
                _currentHeadHeight = Mathf.SmoothDamp(_currentHeadHeight, headPosition.y, ref _headHeightVelocity, _crouchSmoothTime);

                // Update crouching if we're not sitting.
                float crouchStartThreshold = _initialHeadHeight + _crouchStartThreshold;
                if (_enableCrouching)
                {
                    if (forceEnableCrouch)
                    {
                        _crouching = true;
                        _crouchStrength = forceCrouchStrength;
                    }
                    else if (_currentHeadHeight < crouchStartThreshold)
                    {
                        // If the current position of the head is below the crouch start threshold, we'll start crouching.

                        // If the crouch timeout isn't enabled, we can simply set crouching to true.
                        if (!enableCrouchTimeout)
                        {
                            _crouching = true;
                        }
                        // Otherwise, if crouch timeout is enabled...
                        else
                        {
                            if (_isMoving || _turnLeft || _turnRight)
                            {
                                // If the avatar is currently moving, we'll continue crouching normally,
                                // and if there's an active crouch timeout counting down, we'll cancel it.
                                _crouching = true;
                                _crouchTimeoutActive = false;
                            }
                            else
                            {
                                if (!_crouchTimeoutActive)
                                {
                                    // If we're crouching, not moving, and there isn't an active crouch timeout counting
                                    // down, we'll start the countdown now. If the avatar moves, exceeding the velocity
                                    // threshold, or stands back up, the countdown will get canceled.
                                    _crouchTimer = crouchTimeoutDuration;
                                    _crouchTimeoutActive = true;
                                    _crouching = true;
                                }
                                else if (_crouchTimer > 0.0f)
                                {
                                    // If there's an active crouch timeout counting down, but it hasn't
                                    // reached zero yet, we'll continue to crouch until it does.
                                    _crouching = true;
                                }
                            }
                        }

                        _crouchStrength = Mathf.Clamp((crouchStartThreshold - _currentHeadHeight) / _crouchRange, 0.0f, 1.0f);
                    }
                    else if (_currentHeadHeight >= crouchStartThreshold)
                    {
                        // If the head position is above the crouch start threshold, we'll deactivate the crouch timeout.
                        _crouchTimeoutActive = false;
                    }
                }
            }
        }

        private void SendEvents()
        {
            if (_animator is null)
            {
                Debug.LogError("Failed to send events, no animator found");
                return;
            }
            // Overriding the animation controller parameters is helpful for debugging purposes,
            // and allows us to easily test specific situations.
            if (_useOverrideParameters)
            {
                bool moveForward = _movementParameterOverride == MovementParameter.MoveForward || _movementParameterOverride == MovementParameter.MoveForwardLeft || _movementParameterOverride == MovementParameter.MoveForwardRight;
                bool moveBackward = _movementParameterOverride == MovementParameter.MoveBackward || _movementParameterOverride == MovementParameter.MoveBackwardLeft || _movementParameterOverride == MovementParameter.MoveBackwardRight;
                bool moveLeft = _movementParameterOverride == MovementParameter.MoveLeft || _movementParameterOverride == MovementParameter.MoveForwardLeft || _movementParameterOverride == MovementParameter.MoveBackwardLeft;
                bool moveRight = _movementParameterOverride == MovementParameter.MoveRight || _movementParameterOverride == MovementParameter.MoveForwardRight || _movementParameterOverride == MovementParameter.MoveBackwardRight;

                bool turnLeft = _movementParameterOverride == MovementParameter.TurnLeft;
                bool turnRight = _movementParameterOverride == MovementParameter.TurnRight;

                bool crouch = _movementParameterOverride == MovementParameter.Crouch;
                bool sitting = _movementParameterOverride == MovementParameter.Sitting;

                _animator.SetBool("IsMoving", moveForward || moveBackward || moveLeft || moveRight);

                _animator.SetBool("MoveForward", moveForward);
                _animator.SetBool("MoveBackward", moveBackward);
                _animator.SetBool("MoveLeft", moveLeft);
                _animator.SetBool("MoveRight", moveRight);

                _animator.SetBool("TurnLeft", turnLeft);
                _animator.SetBool("TurnRight", turnRight);

                _animator.SetBool("Crouch", crouch);
                _animator.SetBool("IsSitting", sitting);
                _animator.SetFloat("SittingTurnStrength", _sittingTurnStrengthOverride);

                _animator.SetFloat("MoveSpeed", _moveSpeedOverride);
                _animator.SetFloat("TurnStrength", _turnStrengthOverride);
                _animator.SetFloat("CrouchStrength", _crouchStrengthOverride);
            }
            else
            {
                _animator.SetBool("IsMoving", _movingForward || _movingBackward || _movingLeft || _movingRight);

                _animator.SetBool("MoveForward", _movingForward);
                _animator.SetBool("MoveBackward", _movingBackward);
                _animator.SetBool("MoveLeft", _movingLeft);
                _animator.SetBool("MoveRight", _movingRight);

                _animator.SetBool("TurnLeft", _turnLeft);
                _animator.SetBool("TurnRight", _turnRight);

                _animator.SetFloat("MoveSpeed", _moveSpeed);
                _animator.SetFloat("TurnStrength", _turnStrength);

                _animator.SetBool("Crouch", _crouching);
                _animator.SetFloat("CrouchStrength", _crouchStrength);

                _animator.SetBool("IsSitting", _isSitting);
                _animator.SetFloat("SittingTurnStrength", _sittingTurnStrength);

                // When the avatar is crouching, we use a different, lower weight with the TurnInPlaceLayer. This
                // allows the turning animation to blend with the crouching animation without fully overwriting it.
                if (_crouching)
                {
                    _animator.SetLayerWeight(_turnInPlaceLayerIndex, _turnLayerCrouchWeight);
                }
                else
                {
                    _animator.SetLayerWeight(_turnInPlaceLayerIndex, _turnLayerNormalWeight);
                }
            }
        }

        protected void Initialize()
        {
            _animator = transform.GetComponent<Animator>();

            if (_animator != null)
            {
                _turnInPlaceLayerIndex = _animator.GetLayerIndex(_turnInPlaceLayerName);
            }
        }

        // When the animation controller initialized, it assumes the user is in a standing position, and it uses
        // that initial height to determine when to enter the crouching state.  Call this method to recalibrate
        // the avatar's standing height to the current tracked position of the headset.
        public bool RecalibrateStandingHeight()
        {
            _initialPositionPolled = false;
            return PollInitialPosition();
        }

        protected bool PollInitialPosition()
        {
            if (_initialPositionPolled)
            {
                return true;
            }

            var headPositionVector = GetHeadPosition();
            if (headPositionVector == null)
            {
                Debug.LogError("Failed to poll initial positon, no head position found");
                return false;
            }

            _initialHeadHeight = headPositionVector.Value.y;
            _lastMovePosition = headPositionVector.Value;

            _initialPositionPolled = true;
            return true;
        }

        protected Quaternion GetLowerBodyRotation()
        {
            OvrAvatarAnimationBehavior? animationBehavior = GetTargetEntityAnimationBehavior();

            if (animationBehavior == null)
            {
                return Quaternion.identity;
            }

            return animationBehavior.RootRotation;
        }

        protected Vector3 GetHeadVector()
        {
            if (_entity == null)
            {
                Debug.LogError("No entity found");
                return Vector3.forward;
            }

            var cameraMain = Camera.main;
            if (cameraMain == null)
            {
                Debug.LogError("Failed to get main camera");
                return Vector3.forward;
            }
            else
            {
                var headVector = _entity.transform.InverseTransformDirection(cameraMain.transform.forward);

                // Flatten vector to remove nodding movements.
                headVector = Vector3.ProjectOnPlane(headVector, Vector3.up);

                // Handle singularity where the head is pointing straight up or down.
                if (headVector.sqrMagnitude < 1.0e-6f)
                {
                    headVector = Vector3.right;
                }

                return headVector.normalized;
            }
        }

        protected Vector3? GetHeadPosition()
        {
            if (_entity == null ||
                _entity.InputManager == null ||
                _entity.InputManager.InputTrackingProvider == null ||
                !_entity.InputManager.InputTrackingProvider.GetInputTrackingState(out var state) ||
                !state.headsetActive)
            {
                return null;
            }

            return state.headset.position;
        }

        protected OvrAvatarAnimationBehavior? GetTargetEntityAnimationBehavior()
        {
            if (_entity is null)
            {
                return null;
            }

            return _entity.GetComponent<OvrAvatarAnimationBehavior>();
        }

        protected void UpdateSitting()
        {
            if (_wasSitting == _isSitting)
            {
                return;
            }

            if (_isSitting)
            {
                StartSitting();
            }
            else
            {
                StopSitting();
            }

            _wasSitting = _isSitting;
        }

        protected void StartSitting()
        {
            // Get the facing from when we first enter the sitting state; we'll use this
            // to determine if the user is turning to look left or right while seated.
            _startSittingDirection = GetLowerBodyRotation() * Vector3.forward;

            OvrAvatarAnimationBehavior? animationBehavior = GetTargetEntityAnimationBehavior();
            if (animationBehavior == null)
            {
                return;
            }

            // When the avatar is sitting we want to temporarily stop updating the root angle,
            // as turning while sitting is driven entirely though animation.
            animationBehavior.PauseUpdateRootAngle = true;
        }

        protected void StopSitting()
        {
            OvrAvatarAnimationBehavior? animationBehavior = GetTargetEntityAnimationBehavior();

            if (animationBehavior is null)
            {
                return;
            }

            // When the avatar is no longer sitting, we can once again allow the root angle to be updated.
            animationBehavior.PauseUpdateRootAngle = false;
        }

        public float GetLeftFootPlant()
        {
            if (_animator == null)
            {
                return 0.0f;
            }

            return _animator.GetFloat("LeftFootPlanted");
        }

        public float GetRightFootPlant()
        {
            if (_animator == null)
            {
                return 0.0f;
            }

            return _animator.GetFloat("RightFootPlanted");
        }

        public bool IsInTransition(int layerIndex)
        {
            if (_animator == null)
            {
                return false;
            }

            return _animator.IsInTransition(layerIndex);
        }

#if USING_XR_SDK
        public List<UIInputControllerButton> GetControlSchema()
        {

            var sitToggle = new UIInputControllerButton
            {
                button = OVRInput.Button.Two,
                controller = OVRInput.Controller.RTouch,
                description = "Toggle Sitting on and off",
                scope = "MecanimLegsAnimationController"
            };

            var buttons = new List<UIInputControllerButton>
            {
                sitToggle,
            };
            return buttons;
        }
#endif
    }
}
