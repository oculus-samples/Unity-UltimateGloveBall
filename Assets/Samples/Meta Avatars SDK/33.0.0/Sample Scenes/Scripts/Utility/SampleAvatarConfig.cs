#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;


public class SampleAvatarConfig
{
    [Serializable]
    public struct AssetData
    {
        public OvrAvatarEntity.AssetSource source;
        public string path;
    }

    // OvrAvatarEntity
    public CAPI.ovrAvatar2EntityCreateInfo CreationInfo;
    public CAPI.ovrAvatar2EntityViewFlags ActiveView;
    public CAPI.ovrAvatar2EntityManifestationFlags ActiveManifestation;
    // Sample Avatar Entity
    public bool LoadUserFromCdn = true;
    public List<AssetData>? Assets;

    public override string ToString()
    {
        return $"\tCreationInfo:\n" +
               $"\t\tFeatures: {CreationInfo.features.ToString()}\n" +
               $"\t\trenderFilters: {CreationInfo.renderFilters.ToString()}\n" +
               $"\t\tRender Filters:\n" +
               $"\t\t\tLOD Flags: {CreationInfo.renderFilters.lodFlags}\n" +
               $"\t\t\tManifestation Flags: {CreationInfo.renderFilters.manifestationFlags}\n" +
               $"\t\t\tView Flags: {CreationInfo.renderFilters.viewFlags}\n" +
               $"\t\t\tSub Mesh Inclusion Flags: {CreationInfo.renderFilters.subMeshInclusionFlags}\n" +
               $"\t\t\tQuality: {CreationInfo.renderFilters.quality}\n" +
               $"\t\t\tLoad Rig Zip From GLB: {CreationInfo.renderFilters.loadRigZipFromGlb}\n" +
               $"\t\tlodFlags: {CreationInfo.lodFlags.ToString()}\n" +
               $"\t\tIsValid: {CreationInfo.IsValid.ToString()}\n" +
               $"\tActiveView: {ActiveView.ToString()}\n" +
               $"\tActiveManifestation: {ActiveManifestation.ToString()}\n" +
               $"\tLoadUserFromCdn: {LoadUserFromCdn}\n";
    }
}
