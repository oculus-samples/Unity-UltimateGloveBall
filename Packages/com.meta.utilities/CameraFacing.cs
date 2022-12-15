// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class CameraFacing : MonoBehaviour
    {
        [SerializeField] private bool m_fixY = false;

        private void LateUpdate()
        {
            var cameraPosition = Camera.main.transform;
            var dir = transform.position - cameraPosition.position;
            if (m_fixY)
            {
                dir.y = 0;
            }
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
