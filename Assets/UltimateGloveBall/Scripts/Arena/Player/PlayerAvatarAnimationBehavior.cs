// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Multiplayer.Core;
using Meta.XR.Samples;
using Oculus.Avatar2;
using Oculus.Avatar2.Experimental;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// This script is a complement to MecanimLegsAnimationController.cs as we didn't want to rewrite the script
    /// in it's entirety to avoid complex changes when we update the Avatar SDK and the samples.
    /// We make the execution order to be after so that we can overwrite the movement when the rig is moving around.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    [DefaultExecutionOrder(1)]
    public class PlayerAvatarAnimationBehavior : OvrAvatarBaseBehavior
    {
        // Const taken from MecanimLegsAnimationController.cs 
        private const float WALK_SPEED_THRESHOLD = 0.1f;
        private const float RUN_SPEED_THRESHOLD = 10.0f;
        private const float MOVE_SPEED_RANGE = RUN_SPEED_THRESHOLD - WALK_SPEED_THRESHOLD;

        private const float FORWARD_LATERAL_ANGLE_THRESHOLD = 45.0f;
        private const float LATERAL_ANGLE_THRESHOLD = 67.5f;
        private const float BACKWARD_LATERAL_ANGLE_THRESHOLD = 112.5f;
        private const float BACKWARD_ANGLE_THRESHOLD = 135.0f;

        private const float MOVE_THRESHOLD_SQR = 0.01f * 0.01f;

        private static readonly int s_isMoving = Animator.StringToHash("IsMoving");
        private static readonly int s_moveForward = Animator.StringToHash("MoveForward");
        private static readonly int s_moveBackward = Animator.StringToHash("MoveBackward");
        private static readonly int s_moveLeft = Animator.StringToHash("MoveLeft");
        private static readonly int s_moveRight = Animator.StringToHash("MoveRight");
        private static readonly int s_moveSpeed = Animator.StringToHash("MoveSpeed");

        [SerializeField] private OvrAvatarAnimationBehavior m_avatarAnimationBehavior;
        protected override string MainBehavior { get; set; }
        protected override string FirstPersonOutputPose { get; set; }
        protected override string ThirdPersonOutputPose { get; set; }

        private MecanimLegsAnimationController m_legAnimController;
        private Animator m_animator;
        private Transform m_cameraRigTransform;
        private Vector3 m_previousRigPosition;

        private struct MovementData
        {
            public bool IsMoving;
            public float Speed;
            public bool MoveForward;
            public bool MoveBackward;
            public bool MoveRight;
            public bool MoveLeft;
        }

        private void Awake()
        {
            m_cameraRigTransform = CameraRigRef.Instance.CameraRig.transform;
            if (m_avatarAnimationBehavior == null)
            {
                m_avatarAnimationBehavior = GetComponentInChildren<OvrAvatarAnimationBehavior>();
            }
        }

        protected override void OnUserAvatarLoaded(OvrAvatarEntity entity)
        {
            m_legAnimController = GetComponentInChildren<MecanimLegsAnimationController>();
            m_legAnimController.enableCrouchTimeout = false; // disable crouch timeout
            m_animator = m_legAnimController.GetComponent<Animator>();
        }

        private void Update()
        {
            if (!Entity.IsLocal)
            {
                enabled = false;
                return;
            }
            if (m_legAnimController && m_animator)
            {
                var moveData = CalculateMovement();
                if (moveData.IsMoving)
                {
                    m_animator.SetBool(s_isMoving, true);
                    m_animator.SetBool(s_moveForward, moveData.MoveForward);
                    m_animator.SetBool(s_moveBackward, moveData.MoveBackward);
                    m_animator.SetBool(s_moveLeft, moveData.MoveLeft);
                    m_animator.SetBool(s_moveRight, moveData.MoveRight);
                    m_animator.SetFloat(s_moveSpeed, moveData.Speed);
                }
            }
        }

        private MovementData CalculateMovement()
        {
            // Calculate the delta between the current position and it's position at the last update.
            var rigPosition = m_cameraRigTransform.position;
            var deltaRigPosition = rigPosition - m_previousRigPosition;
            deltaRigPosition = Vector3.ProjectOnPlane(deltaRigPosition, Vector3.up);

            var velocity = deltaRigPosition / Time.deltaTime;

            var lowerBodyRotation = m_avatarAnimationBehavior.RootRotation;
            var relativeVelocity = Quaternion.Inverse(lowerBodyRotation) * velocity;
            var movementAngle = Vector3.Angle(relativeVelocity, Vector3.forward);


            var moveSpeed = Mathf.Clamp(
                (velocity.magnitude - WALK_SPEED_THRESHOLD) / MOVE_SPEED_RANGE,
                0.0f,
                1.0f);
            m_previousRigPosition = rigPosition;
            // check that we move considerably
            var isMoving = velocity.sqrMagnitude > MOVE_THRESHOLD_SQR;
            return BuildMovementData(isMoving, moveSpeed, relativeVelocity, movementAngle);
        }

        private MovementData BuildMovementData(bool isMoving, float moveSpeed, Vector3 relativeVelocity, float movementAngle)
        {
            var moveData = new MovementData() { IsMoving = isMoving, Speed = moveSpeed };
            if (!isMoving)
            {
                return moveData;
            }

            var lateralMovement = false;

            if (movementAngle <= FORWARD_LATERAL_ANGLE_THRESHOLD)
            {
                // Forward
                moveData.MoveForward = true;
            }
            else if (movementAngle <= LATERAL_ANGLE_THRESHOLD)
            {
                // Forward lateral
                moveData.MoveForward = true;
                lateralMovement = true;
            }
            else if (movementAngle <= BACKWARD_LATERAL_ANGLE_THRESHOLD)
            {
                // Lateral
                lateralMovement = true;
            }
            else if (movementAngle <= BACKWARD_ANGLE_THRESHOLD)
            {
                // Backward lateral
                moveData.MoveBackward = true;
                lateralMovement = true;
            }
            else
            {
                // Backward
                moveData.MoveBackward = true;
            }

            // If we have lateral movement, determine if its to the left or right.
            if (!lateralMovement)
            {
                return moveData;
            }

            if (relativeVelocity.x < 0.0f)
            {
                moveData.MoveLeft = true;
            }
            else
            {
                moveData.MoveRight = true;
            }

            return moveData;
        }
    }
}