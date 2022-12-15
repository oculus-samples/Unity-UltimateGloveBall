using UnityEngine;

namespace Oculus.Skinning.GpuSkinning
{
    internal class OvrMorphTargetsData
    {
        public static string[] ShaderKeywordsForMorphTargets(bool useIndirectionTexture = true)
        {
            return useIndirectionTexture ? _shaderIndirectionKeywords : _shaderKeywords;
        }

        public int IndirectionTexWidth => _indirectionTex.Width;
        public int IndirectionTexHeight => _indirectionTex.Height;

        public OvrMorphTargetsData(
            OvrGpuMorphTargetsCombiner morphTargetsCombiner,
            OvrExpandableTextureArray indirectionTexture,
            Material skinningMaterial)
        {
            _combiner = morphTargetsCombiner;
            _indirectionTex = indirectionTexture;
            _skinningMaterial = skinningMaterial;

            _combiner.ArrayResized += CombinerArrayResized;
            _indirectionTex.ArrayResized += IndirectionTexArrayResized;

            SetIndirectionTextureInMaterial(_indirectionTex.GetTexArray());
            SetCombinedMorphTargetsTextureInMaterial(morphTargetsCombiner.GetCombinedShapesTexArray());
        }

        public void Destroy()
        {
            _combiner.ArrayResized -= CombinerArrayResized;
            _indirectionTex.ArrayResized -= IndirectionTexArrayResized;
        }

        private void CombinerArrayResized(object sender, Texture newArray)
        {
            SetCombinedMorphTargetsTextureInMaterial(newArray);
        }

        private void IndirectionTexArrayResized(object sender, Texture2DArray newArray)
        {
            SetIndirectionTextureInMaterial(newArray);
        }

        private void SetIndirectionTextureInMaterial(Texture indirectionTex)
        {
            _skinningMaterial.SetTexture(INDIRECTION_TEX_PROP, indirectionTex);
        }

        private void SetCombinedMorphTargetsTextureInMaterial(Texture combinedMorphTargetsTex)
        {
            _skinningMaterial.SetTexture(COMBINED_MORPH_TARGETS_TEX_PROP, combinedMorphTargetsTex);
        }

        private const string OVR_MORPH_TARGET_KEYWORD = "OVR_HAS_MORPH_TARGETS";
        private const string OVR_MORPH_TARGET_INDIRECTION_KEYWORD = "OVR_HAS_MORPH_TARGETS_INDIRECTION_TEXTURE";

        private static readonly string[] _shaderKeywords = { OVR_MORPH_TARGET_KEYWORD };
        private static readonly string[] _shaderIndirectionKeywords = { OVR_MORPH_TARGET_INDIRECTION_KEYWORD };

        private static readonly int COMBINED_MORPH_TARGETS_TEX_PROP = Shader.PropertyToID("u_CombinedMorphTargetsTex");
        private static readonly int INDIRECTION_TEX_PROP = Shader.PropertyToID("u_IndirectionTex");

        private Material _skinningMaterial;
        private OvrGpuMorphTargetsCombiner _combiner;
        private OvrExpandableTextureArray _indirectionTex;
    }
}
