// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using Meta.XR.Samples;
using UltimateGloveBall.Arena.Player.Respawning;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// You own a cat? You can spawn or despawn the cat as an owner. This also is an interface for the Cat to get
    /// instructions on how to behave.
    /// </summary>
    [MetaCodeSample("UltimateGloveBall")]
    public class CatOwner : MonoBehaviour
    {
        [SerializeField] private CatAI m_catPrefab;
        [SerializeField] private float m_spawnCatDistance = 2;
        [SerializeField, AutoSet] private RespawnController m_respawnController;
        [SerializeField, AutoSet] private PlayerControllerNetwork m_playerControllerNetwork;
        private CatAI m_cat;

        private void Start()
        {
            if (m_respawnController)
            {
                m_respawnController.OnKnockedOutEvent += OnOwnerKnockedOut;
                m_respawnController.OnRespawnCompleteEvent += OnOwnerRespawn;
            }

            if (m_playerControllerNetwork)
            {
                m_playerControllerNetwork.OnInvulnerabilityStateUpdatedEvent += OnInvulnerableStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (m_respawnController)
            {
                m_respawnController.OnKnockedOutEvent -= OnOwnerKnockedOut;
                m_respawnController.OnRespawnCompleteEvent -= OnOwnerRespawn;
            }

            if (m_playerControllerNetwork)
            {
                m_playerControllerNetwork.OnInvulnerabilityStateUpdatedEvent -= OnInvulnerableStateChanged;
            }
        }

        public void SpawnCat()
        {
            if (m_cat != null)
            {
                // spawn maximum 1 cat
                return;
            }

            var thisTransform = transform;
            var currentPos = thisTransform.position;
            // spawn in front of player
            var spawnPos = currentPos + thisTransform.forward * m_spawnCatDistance;
            m_cat = Instantiate(m_catPrefab, spawnPos, Quaternion.FromToRotation(Vector3.forward, currentPos - spawnPos));
            m_cat.SetOwner(this);

            if (m_respawnController.IsKnockedOut)
            {
                OnOwnerKnockedOut();
            }

            if (m_playerControllerNetwork.IsInvulnerable.Value)
            {
                OnInvulnerableStateChanged(true);
            }
        }

        public void DeSpawnCat()
        {
            if (m_cat != null)
            {
                Destroy(m_cat.gameObject);
            }
        }

        private void OnOwnerKnockedOut()
        {
            m_cat?.UnfollowOwner();
        }

        private void OnOwnerRespawn()
        {
            m_cat?.FollowOwner();
        }

        private void OnInvulnerableStateChanged(bool invulnerable)
        {
            m_cat?.ChangeInvulnerabilityState(invulnerable);
        }
    }
}