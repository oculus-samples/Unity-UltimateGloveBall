// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode.Components;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host.
    /// Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public bool IgnoreUpdates { get; set; }

        protected void UpdateCanCommit() => CanCommitToTransform = NetworkObject.IsOwner;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateCanCommit();

            if (CanCommitToTransform)
            {
                // workaround for issue
                // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1560#issuecomment-1013217835
                var cache = ScaleThreshold;
                ScaleThreshold = -1;

                var position = InLocalSpace ? transform.localPosition : transform.position;
                var rotation = InLocalSpace ? transform.localRotation : transform.rotation;
                var scale = transform.localScale;
                Teleport(position, rotation, scale);

                ScaleThreshold = cache;
            }
        }

        protected override void Update()
        {
            UpdateCanCommit();

            var thisTransform = transform;

            var cacheLocalPosition = thisTransform.localPosition;
            var cacheLocalRotation = thisTransform.localRotation;
            base.Update();

            if (IgnoreUpdates)
            {
                thisTransform.localPosition = cacheLocalPosition;
                thisTransform.localRotation = cacheLocalRotation;
            }

            if (NetworkManager != null && (NetworkManager.IsConnectedClient || NetworkManager.IsListening))
            {
                if (CanCommitToTransform)
                {
                    TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                }
            }
        }

        public override void OnGainedOwnership()
        {
            UpdateCanCommit();
            base.OnGainedOwnership();
        }

        public override void OnLostOwnership()
        {
            UpdateCanCommit();
            base.OnLostOwnership();
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
