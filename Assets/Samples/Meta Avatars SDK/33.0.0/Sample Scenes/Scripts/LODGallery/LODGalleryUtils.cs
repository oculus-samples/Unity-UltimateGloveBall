#nullable enable

using Oculus.Avatar2;

namespace Oculus.Avatar2
{
    public static class LODGalleryUtils
    {

        public enum LODGalleryAvatarType
        {
            UltraLight = 0,
            Light = 1,
            Standard = 2,
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoUltraLightQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Ultralight,
                    loadRigZipFromGlb = false,
                }
            };
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoLightQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Light,
                    loadRigZipFromGlb = false,
                }
            };
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoStandardQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Standard,
                    loadRigZipFromGlb = false,
                },

            };
        }
    }
}
