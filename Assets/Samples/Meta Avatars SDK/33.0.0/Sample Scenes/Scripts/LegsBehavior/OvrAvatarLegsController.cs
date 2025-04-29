#nullable enable

using System;
using System.Collections;
using UnityEngine;

namespace Oculus.Avatar2.Experimental
{
    /// <summary>
    /// Refactored and moved from Testbed:
    /// https://www.internalfb.com/code/fbsource/arvr/projects/avatartestbed/AvatarTestbed/Assets/AvatarTestbed/Avatars/Scripts/Controllers/BehaviorLegsController.cs
    /// </summary>
    ///
    public class OvrAvatarLegsController : MonoBehaviour
    {
        // joints information to support specific rigs
        private const string _ROOT_TRANSFORM_NAME = "Root_jnt";
        private const string _HIPS_TRANSFORM_NAME = "Hips_jnt";
        private const string _HEAD_TRANSFORM_NAME = "Head_jnt";

        private const float MIN_THUMBSTICK_INPUT_VALUE = 0.0f;
        private const float MAX_THUMBSTICK_INPUT_VALUE = 3.85f;
        private const float MIN_LOCOMOTION_ANIMATION_SPEED = 0.0f;
        private const float MAX_LOCOMOTION_ANIMATION_SPEED = 3.0f;

        // Turn in place logic.
        private const float FOOT_ROTATION_THRESHOLD = 30.0f;
        public const float MAX_SPEED = 50f;
        public const float SMOOTH_TIME = 0.1f;

        private float _angle;

        private OvrAvatarEntity? _entity;
        private bool _feetAreMoving;

        // Calculate Velocity without using the CharacterController, as CharacterController.velocity
        // returns the value between two CharacterController.move calls, which the mirrored avatar
        // won't have as it's missing the SimpleLocomotionScript
        private Vector3 _lastPosition;
        private float _startFootRotation;
        private float _currentFootRotation;
        private float _targetFootRotation;
        private float _turnLeftBlend;
        private float _turnPhase;
        private float _turnRightBlend;
        private Vector3 _velocity;
        private Vector3 _acceleration;

        private Transform? _rootTransform;
        private Transform? _hipsTransform;
        private Transform? _headTransform;

        public void Initialize(OvrAvatarEntity entity)
        {
            _entity = entity;
        }

        private static float RemapValueRange(float inValue, float oldMin, float oldMax, float newMin, float newMax)
        {
            return Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, inValue));
        }

        private static float CalculateDirection(Vector3 velocity, Transform? transform)
        {
            if (transform is null)
            {
                return 0.0f;
            }

            if (velocity == Vector3.zero)
            {
                return 0.0f;
            }

            float mirrorSign = (transform.lossyScale.x < 0) ? 1 : -1;

            // TODO: find a better way to do this instead of differentiating between mirrored avatar and local avatar
            Vector3 forwardVector = -transform.forward;
            Vector3 rightVector = transform.right * mirrorSign;
            Vector3 normalizedVelocity = velocity.normalized;

#if UNITY_EDITOR
            Debug.DrawLine(transform.position, transform.position + forwardVector, Color.blue);
            Debug.DrawLine(transform.position, transform.position + normalizedVelocity, Color.red);
#endif

            float forwardCosAngle = Vector3.Dot(forwardVector, normalizedVelocity);
            float forwardDeltaDegree = Mathf.Rad2Deg * Mathf.Acos(forwardCosAngle);

            float rightCosAngle = Vector3.Dot(rightVector, normalizedVelocity);
            if (rightCosAngle < 0)
            {
                forwardDeltaDegree *= -1;
            }

            return forwardDeltaDegree;
        }

        private void Start()
        {
            if (_entity == null)
            {
                OvrAvatarLog.LogError("OvrAvatarLegsController not initialized");
            }

            _lastPosition = transform.position;
            _startFootRotation = GetRelativeHeadTurn();
        }

        private void Update()
        {
            UpdateState();
            SendEvents();
            UpdateBaseAndTargetRotations();
        }

        private float GetRelativeHeadTurn()
        {
            // This will take the forward vector of both transform and the head joint, and find the rotation difference between the two.
            // TODO: Replace GetHeadTransform() with the Camera Transform.
            if (Camera.main is null)
            {
                OvrAvatarLog.LogError("SampleAvatarLegsController::GetRelativeHeadTurn() : Main Camera game object not found.");
                return 0.0f;
            }
            var headVector = transform.worldToLocalMatrix * Camera.main.transform.forward;
            var baseTurn = IsMirrored() ? Quaternion.identity : Quaternion.Inverse(Quaternion.identity);
            var turn = Quaternion.LookRotation(headVector);
            var turnCalc = baseTurn * turn;
            return (turnCalc.eulerAngles.y + 90.0f) * -1f;
        }

        private void UpdateBaseAndTargetRotations()
        {
            _targetFootRotation = GetRelativeHeadTurn();
            var distanceCheck =
              Math.Abs(_targetFootRotation - _startFootRotation) > FOOT_ROTATION_THRESHOLD;
            if (!distanceCheck || _feetAreMoving)
            {
                return;
            }

            if (_targetFootRotation < _startFootRotation)
            {
                _turnRightBlend = 1.0f;
            }
            else
            {
                _turnLeftBlend = 1.0f;
            }

            // TODO: why are we starting a new coroutine per frame? since _taretFootRotation
            // and _turnLeft/Right blend is updated every frame, we could probably get away with
            // running only one coroutine at a time
            StartCoroutine(LerpFootRotation());
        }

        private IEnumerator LerpFootRotation()
        {
            _feetAreMoving = true;
            _turnPhase = 0.0f;
            float timeElapsed = 0;
            while (timeElapsed < 1.0f)
            {
                _currentFootRotation =
                  Mathf.LerpAngle(_startFootRotation, _targetFootRotation, timeElapsed / 1.0f);
                _turnPhase += Time.deltaTime;
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            _feetAreMoving = false;
            _startFootRotation = _targetFootRotation;
            _currentFootRotation = _targetFootRotation;
            _turnLeftBlend = 0.0f;
            _turnRightBlend = 0.0f;
            _turnPhase = 0.0f;
        }

        private void UpdateState()
        {
            var position = transform.position;
            var targetVelocity = (position - _lastPosition) / Time.deltaTime;
            targetVelocity.y = 0f;
            _lastPosition = position;

            _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _acceleration, SMOOTH_TIME, MAX_SPEED);
            _angle = CalculateDirection(_velocity, GetRootTransform());
        }

        private void SendEvents()
        {
            var remappedSpeed = RemapValueRange(_velocity.magnitude,
              MIN_THUMBSTICK_INPUT_VALUE, MAX_THUMBSTICK_INPUT_VALUE,
              MIN_LOCOMOTION_ANIMATION_SPEED, MAX_LOCOMOTION_ANIMATION_SPEED);

            if (_entity == null)
            {
                OvrAvatarLog.LogError("SampleAvatarLegsController.SendEvents _entity is null.");
                return;
            }

            BehaviorLegsEvent.Send(
                _entity,
                new BehaviorLegsEvent.EventParam
                {
                    Velocity = remappedSpeed,
                    Angle = _angle,
                    TrackingBlendUpper = 1.0f,
                    RootRotationOffset = _currentFootRotation,
                    LeftFootAngle = FOOT_ROTATION_THRESHOLD,
                    RightFootAngle = FOOT_ROTATION_THRESHOLD,
                    LeftFootBlend = _turnLeftBlend,
                    RightFootBlend = _turnRightBlend,
                    TurningPhase = _turnPhase,
                });
        }

        private bool IsMirrored()
        {
            return Mathf.Sign(transform.lossyScale.x) < 0f;
        }

        #region Joint Lookup
        private Transform? GetRootTransform()
        {
            return GetJointTransform(ref _rootTransform, _ROOT_TRANSFORM_NAME);
        }

        private Transform? GetHipsTransform()
        {
            return GetJointTransform(ref _hipsTransform, _HIPS_TRANSFORM_NAME);
        }

        private Transform? GetHeadTransform()
        {
            return GetJointTransform(ref _headTransform, _HEAD_TRANSFORM_NAME);
        }

        private Transform? GetJointTransform(ref Transform? result, string jointName)
        {
            if (result == null)
            {
                result = transform.FindChildRecursive(jointName);
            }

            return result;
        }
        #endregion // Joint Lookup
    }
}
