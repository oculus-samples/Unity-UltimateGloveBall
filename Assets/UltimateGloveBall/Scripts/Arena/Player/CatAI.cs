// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using Meta.XR.Samples;
using UltimateGloveBall.Arena.Balls;
using UnityEngine;
using UnityEngine.AI;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// This Cat AI component integrate logic for the cat to follow it's owner on a navigation mesh.
    /// The cat will also react to being hit by a ball.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CatAI : MonoBehaviour
    {
        private const float IDLE_TIME = 6f;
        private const float JUMP_FORCE = 3f;
        private static readonly int s_state = Animator.StringToHash("State");
        private static readonly int s_ghostProperty = Shader.PropertyToID("ENABLE_GHOST_EFFECT");

        [SerializeField, AutoSet] private NavMeshAgent m_navMeshAgent;
        [SerializeField, AutoSetFromChildren] private Animator m_animator;
        [SerializeField] private Transform m_visualTransform;
        [SerializeField] private Renderer m_renderer;
        [SerializeField] private Collider m_collider;
        [SerializeField] private float m_randomPositionRadius = 2;
        // Distance the owner needs to move for the Cat to repath
        [SerializeField] private float m_minMovementSqrDistance = 2;
        [SerializeField] private float m_idleRotationSpeed = 1f;
        [SerializeField] private float m_moveRotationSpeed = 10f;

        [Header("Sounds")]
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_idleSound;
        [SerializeField] private AudioClip[] m_angrySounds;


        private CatOwner m_owner;

        private Vector3 m_ownerLastPosition;

        private bool m_stateIsIdle = true;
        private float m_idleTimer = 0;

        private bool m_isJumping;
        private float m_currentJumpVelocity;

        private bool m_followOwner = true;

        private bool m_isInvulnerable;
        private MaterialPropertyBlock m_materialPropertyBlock;

        public void SetOwner(CatOwner owner)
        {
            m_owner = owner;
            m_followOwner = true;
        }

        public void UnfollowOwner()
        {
            m_followOwner = false;
            PlayAngrySound();
        }

        public void FollowOwner()
        {
            m_followOwner = true;
            PlayIdleSound();
        }

        public void ChangeInvulnerabilityState(bool invulnerable)
        {
            if (m_isInvulnerable == invulnerable)
            {
                return;
            }
            m_isInvulnerable = invulnerable;
            m_collider.enabled = !invulnerable;

            m_materialPropertyBlock ??= new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetFloat(s_ghostProperty, invulnerable ? 1 : 0);
            m_renderer.SetPropertyBlock(m_materialPropertyBlock);
        }

        private void Update()
        {
            if (m_owner == null)
            {
                return;
            }

            var dt = Time.deltaTime;

            if (m_isJumping)
            {
                var pos = m_visualTransform.position;
                pos.y += m_currentJumpVelocity * dt;
                m_currentJumpVelocity += Physics.gravity.y * dt;
                if (pos.y <= 0)
                {
                    pos.y = 0;
                    m_isJumping = false;
                }

                m_visualTransform.position = pos;
            }

            var rotating = false;
            if (m_navMeshAgent.remainingDistance < 0.01f)
            {
                m_stateIsIdle = true;
                var dir = GetTrackingPosition() - transform.position;
                dir.y = 0;
                var rot = Quaternion.LookRotation(dir);
                var thisTrans = transform;
                thisTrans.rotation = Quaternion.Slerp(thisTrans.rotation, rot, m_idleRotationSpeed * dt);
                rotating = Vector3.Angle(thisTrans.forward, dir) > 10;
                if (!rotating)
                {
                    m_animator.SetInteger(s_state, 0);
                }
            }
            else
            {
                // To avoid some sliding we make the cat look in the direction of movement
                // the slerp will show some sliding but reduces the snaping
                var direction = m_navMeshAgent.desiredVelocity;
                var rot = Quaternion.LookRotation(direction);
                var thisTrans = transform;
                thisTrans.rotation = Quaternion.Slerp(thisTrans.rotation, rot, m_moveRotationSpeed * dt);
            }

            var velocity = m_navMeshAgent.velocity.magnitude;
            m_animator.SetFloat("walkspeed", rotating ? 2 : velocity * 3);

            var findNewSpot = false;
            if (m_stateIsIdle)
            {
                m_idleTimer += dt;
                if (m_idleTimer >= IDLE_TIME)
                {
                    PlayIdleSound();
                    findNewSpot = true;
                    m_idleTimer = 0;
                }
            }
            var currentTrackingPos = GetTrackingPosition();
            if (!findNewSpot && (m_ownerLastPosition - currentTrackingPos).sqrMagnitude < m_minMovementSqrDistance)
            {
                return;
            }

            _ = m_navMeshAgent.SetDestination(GetRandomPositionAroundOwner());
            m_ownerLastPosition = currentTrackingPos;
            m_animator.SetInteger(s_state, 1);
            m_stateIsIdle = false;
            m_idleTimer = 0;
        }

        private Vector3 GetTrackingPosition()
        {
            return m_followOwner ? m_owner.transform.position : m_ownerLastPosition;
        }

        private Vector3 GetRandomPositionAroundOwner()
        {
            return GetTrackingPosition() + new Vector3(
                Random.Range(-m_randomPositionRadius, m_randomPositionRadius),
                0,
                Random.Range(-m_randomPositionRadius, m_randomPositionRadius));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!m_isJumping && collision.gameObject.GetComponent<BallNetworking>())
            {
                m_isJumping = true;
                m_currentJumpVelocity = JUMP_FORCE;
                PlayAngrySound();
            }
        }

        private void PlayAngrySound()
        {
            m_audioSource.PlayOneShot(m_angrySounds[Random.Range(0, m_angrySounds.Length)]);
        }

        private void PlayIdleSound()
        {
            m_audioSource.PlayOneShot(m_idleSound);
        }
    }
}