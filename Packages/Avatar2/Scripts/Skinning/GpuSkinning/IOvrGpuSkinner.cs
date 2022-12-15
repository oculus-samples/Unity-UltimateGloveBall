using Oculus.Avatar2;

using Unity.Collections;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Oculus.Skinning.GpuSkinning
{
    internal abstract class IOvrGpuSkinner
    {
        public abstract GraphicsFormat GetOutputTexGraphicFormat();
        public abstract Texture GetOutputTex();
        public abstract CAPI.ovrTextureLayoutResult GetLayoutInOutputTex(OvrSkinningTypes.Handle handle);
        public abstract void EnableBlockToRender(OvrSkinningTypes.Handle handle, SkinningOutputFrame outputFrame);
        public abstract void UpdateOutputTexture();
        public abstract bool HasJoints { get; }
        public abstract OvrAvatarGpuSkinningController ParentController { get; set; }
        public abstract NativeSlice<OvrJointsData.JointData>? GetJointTransformMatricesArray(OvrSkinningTypes.Handle handle);
        public abstract void UpdateJointTransformMatrices(OvrSkinningTypes.Handle handle);
        public abstract void Destroy();
    }
}
