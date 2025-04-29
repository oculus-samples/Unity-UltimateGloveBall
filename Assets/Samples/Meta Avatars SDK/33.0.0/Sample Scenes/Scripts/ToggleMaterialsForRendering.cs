#nullable enable

using UnityEngine;
using UnityEngine.Rendering;

public class ToggleMaterialsForRendering : MonoBehaviour
{
    [SerializeField] private Material? builtInMaterial;
    [SerializeField] private Material? urpMaterial;

    private void Start()
    {
        // Get the current rendering pipeline
        bool isBuiltInPipeline = GraphicsSettings.renderPipelineAsset == null;
        bool isUrpPipeline = GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset.GetType().Name == "UniversalRenderPipelineAsset";

        if (isBuiltInPipeline && builtInMaterial != null)
        {
            GetComponent<Renderer>().material = builtInMaterial;
        }
        else if (isUrpPipeline && urpMaterial != null)
        {
            GetComponent<Renderer>().material = urpMaterial;
        }
        else
        {
            Debug.LogWarning($"Skipping setting materials for {gameObject.name} based on rendering pipeline, material is not set..");
        }
    }
}
