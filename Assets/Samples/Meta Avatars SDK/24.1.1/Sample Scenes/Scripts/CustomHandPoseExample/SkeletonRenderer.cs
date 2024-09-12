using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SkeletonRenderer : MonoBehaviour
{
    public Color color;

    public bool drawAxes;
    public float axisSize;

#if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += Draw;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= Draw;
    }
#endif

#if UNITY_EDITOR
    private void Draw(SceneView sceneView)
    {
        Handles.matrix = Matrix4x4.identity;

        foreach (var xform in GetComponentsInChildren<Transform>())
        {
            var parent = xform.parent;
            var position = xform.position;
            if (parent)
            {
                Handles.color = color;
                Handles.DrawLine(position, parent.position);
            }

            if (drawAxes)
            {
                var r = xform.rotation;
                var xAxis = position + r * Vector3.right * axisSize ;
                var yAxis = position + r * Vector3.up * axisSize ;
                var zAxis = position + r * Vector3.forward * axisSize;

                Handles.color = Color.blue;
                Handles.DrawLine(position, zAxis);

                Handles.color = Color.green;
                Handles.DrawLine(position, yAxis);

                Handles.color = Color.red;
                Handles.DrawLine(position, xAxis);
            }
        }
    }
#endif
}
