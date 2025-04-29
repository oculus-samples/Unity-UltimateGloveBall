#nullable enable

// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// A debug visualizer to highlight the bones and joints in a character rig
    /// </summary>
    public class RigVisualizerUtility : MonoBehaviour
    {
        private Transform? _root;
        private Color _color = Color.red;

        public void Initialize(Transform root, Color visualColor)
        {
            _color = visualColor;
            _root = root;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_root == null)
            {
                return;
            }

            DrawTransform(_root, _color);
        }

        private void DrawTransform(Transform? transform, Color color)
        {
            if (transform == null)
            {
                return;
            }

            if (transform != _root && transform.parent != null)
            {
                Handles.color = color;
                Handles.DrawLine(transform.position, transform.parent.position);
            }

            transform.DebugDrawInEditor(0.03f);

            for (int i = 0; i < transform.childCount; ++i)
            {
                DrawTransform(transform.GetChild(i), color);
            }
        }
    }
}
#endif
