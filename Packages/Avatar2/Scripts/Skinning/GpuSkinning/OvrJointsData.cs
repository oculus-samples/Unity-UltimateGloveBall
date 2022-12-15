// Check for differences in update vs current state and ignore if they match
//#define OVR_GPUSKINNING_DIFFERENCE_CHECK

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

using Oculus.Avatar2;

namespace Oculus.Skinning.GpuSkinning
{
    internal class OvrJointsData
    {
        public int JointsTexWidth => _jointsTex.Width;
        public int JointsTexHeight => _jointsTex.Height;

        public static string[] ShaderKeywordsForJoints(OvrSkinningTypes.SkinningQuality quality)
        {
            var qualityIndex = (uint)quality;
            var keywords = qualityIndex < _KeywordLookup.Length ? _KeywordLookup[qualityIndex] : null;
            if (keywords == null)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), quality, "Invalid SkinningQuality value");
            }
            return keywords;
        }

        public OvrJointsData(OvrExpandableTextureArray jointsTexture, Material skinningMaterial)
        {
            _jointsTex = jointsTexture;
            _skinningMaterial = skinningMaterial;

            _jointBuffer = null;
            _jointsBufferLayout = new OvrFreeListBufferTracker(MAX_JOINTS);

            _jointsTex.ArrayResized += JointsTexArrayResized;

            SetJointsTextureInMaterial(jointsTexture.GetTexArray());
            SetBuffersInMaterial();
        }

        public void Destroy()
        {
            if (_jointArray.IsCreated) { _jointArray.Dispose(); }

            _jointBuffer?.Release();
            _jointsTex.ArrayResized -= JointsTexArrayResized;
        }

        private void JointsTexArrayResized(object sender, Texture2DArray newArray)
        {
            SetJointsTextureInMaterial(newArray);
        }

        public OvrSkinningTypes.Handle AddJoints(int numJoints)
        {
            OvrSkinningTypes.Handle layoutHandle = _jointsBufferLayout.TrackBlock(numJoints);
            if (!layoutHandle.IsValid())
            {
                return layoutHandle;
            }

            OvrFreeListBufferTracker.LayoutResult layoutInBuffer = _jointsBufferLayout.GetLayoutInBufferForBlock(layoutHandle);

            // Expand compute buffers if needed
            int newNumJoints = layoutInBuffer.startIndex + layoutInBuffer.count;
            if (_jointBuffer == null)
            {
                Debug.Assert(!_jointArray.IsCreated);

                _jointBuffer = new ComputeBuffer(newNumJoints, JOINTS_BUFFER_STRIDE);
                _jointArray = new NativeArray<JointData>(newNumJoints, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                _jointBuffer.SetData(_jointArray, 0, 0, newNumJoints);
            }
            else if (newNumJoints > _jointBuffer.count)
            {
                Debug.Assert(_jointArray.IsCreated);
                var newJointArray = new NativeArray<JointData>(newNumJoints, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                NativeArray<JointData>.Copy(_jointArray, newJointArray);

                var oldJointArray = _jointArray;
                _jointArray = newJointArray;
                oldJointArray.Dispose();

                // Enlarge buffer and copy back contents
                _jointBuffer.Release();
                _jointBuffer = new ComputeBuffer(newNumJoints, JOINTS_BUFFER_STRIDE);

                _jointBuffer.SetData(_jointArray, 0, 0, newNumJoints);
            }

            SetBuffersInMaterial();

            return layoutHandle;
        }

        public OvrFreeListBufferTracker.LayoutResult GetLayoutForJoints(OvrSkinningTypes.Handle handle)
        {
            return _jointsBufferLayout.GetLayoutInBufferForBlock(handle);
        }

        public void RemoveJoints(OvrSkinningTypes.Handle handle)
        {
            _jointsBufferLayout.FreeBlock(handle);
        }

        public bool CanFitAdditionalJoints(int numJoints)
        {
            return _jointsBufferLayout.CanFit(numJoints);
        }

        public NativeSlice<JointData>? GetJointTransformMatricesArray(OvrSkinningTypes.Handle handle)
        {
            var layout = _jointsBufferLayout.GetLayoutInBufferForBlock(handle);

            if (!layout.IsValid || _jointBuffer == null)
            {
                return null;
            }

            return _jointArray.Slice(layout.startIndex, layout.count);
        }

        public bool UpdateJointTransformMatrices(OvrSkinningTypes.Handle handle)
        {
            bool didUpdate = false;

            var layout = _jointsBufferLayout.GetLayoutInBufferForBlock(handle);

            Debug.Assert(_jointBuffer != null);
            if (layout.IsValid && _jointBuffer != null)
            {
#if OVR_GPUSKINNING_DIFFERENCE_CHECK
                bool isDifferent = false;
                for (int updIdx = 0; updIdx < updLen; updIdx++)
                {
                    isDifferent = jointMatrices[updIdx] != _jointArray[offset + updIdx];
                    if (isDifferent) { break; }
                }

                didUpdate = isDifferent;
                if (isDifferent)
#else
                didUpdate = true;
#endif
                _jointBuffer.SetData(_jointArray, layout.startIndex, layout.startIndex, layout.count);
            }
            return didUpdate;
        }

        private void SetBuffersInMaterial()
        {
            _skinningMaterial.SetBuffer(JOINT_MATRICES_PROP, _jointBuffer);
        }

        private void SetJointsTextureInMaterial(Texture texture)
        {
            _skinningMaterial.SetTexture(JOINTS_TEX_PROP, texture);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JointData
        {
            public Matrix4x4 transform;
            public Matrix4x4 normalTransform;
        }
        internal const int JointDataSize = 2 * BYTES_PER_MATRIX;  // must match sizeof(JointData)

        private ComputeBuffer _jointBuffer;

        // CPU copy of _jointBuffer contents.
        // Due to Unity API restrictions, there is no copying between buffers, so the only way to enlarge a buffer
        // but not blow away contents is to read via GetData which causes a pipeline stall.
        // This isn't expected to happen very frequently (only when buffer increases in size).
        private NativeArray<JointData> _jointArray;

        private OvrExpandableTextureArray _jointsTex;
        private Material _skinningMaterial;

        private readonly OvrFreeListBufferTracker _jointsBufferLayout;

        private const int BYTES_PER_MATRIX = 16 * sizeof(float);
        private const int JOINTS_BUFFER_STRIDE = JointDataSize;
        private const int MAX_JOINTS = 100000; // TODO* Have this based on max buffer size

        private const string OVR_FOUR_BONES_KEYWORD = "OVR_SKINNING_QUALITY_4_BONES";
        private const string OVR_TWO_BONES_KEYWORD = "OVR_SKINNING_QUALITY_2_BONES";
        private const string OVR_ONE_BONE_KEYWORD = "OVR_SKINNING_QUALITY_1_BONE";

        private static readonly int JOINTS_TEX_PROP = Shader.PropertyToID("u_JointsTex");
        private static readonly int JOINT_MATRICES_PROP = Shader.PropertyToID("u_JointMatrices");

        private static readonly string[][] _KeywordLookup = {
                /* 0, INVALID */ null,
                /* 1, Bone1 */ new[] { OVR_ONE_BONE_KEYWORD },
                /* 2, Bone2 */ new[] { OVR_TWO_BONES_KEYWORD },
                /* 3, Unsupported */ null,
                /* 4, Bone4 */ new[] { OVR_FOUR_BONES_KEYWORD },
        };
    }
}
