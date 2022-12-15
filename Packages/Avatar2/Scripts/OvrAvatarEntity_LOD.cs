using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Oculus.Avatar2
{
    public partial class OvrAvatarEntity : MonoBehaviour
    {
        [Header("LOD")]
        protected AvatarLOD _avatarLOD;

        internal protected readonly LodData[] _visibleLodData = new LodData[CAPI.ovrAvatar2EntityLODFlagsCount];

        /// Number of LODs loaded for this avatar.
        protected int lodObjectCount { get; private set; } = 0;

        /// Index of lowest quality level of detail loaded for this avatar.
        public int LowestQualityLODIndex { get; private set; } = -1;

        /// Index of highest quality level of detail loaded for this avatar.
        public int HighestQualityLODIndex { get; private set; } = -1;

        /// Provides vertex and triangle counts for each level of detail.
        public IReadOnlyList<LodCostData> CopyVisibleLODCostData()
        {
            var lodCosts = new LodCostData[CAPI.ovrAvatar2EntityLODFlagsCount];
            var allLodCost = _visibleAllLodData.IsValid ? _visibleAllLodData.totalCost : default;
            for (int idx = 0; idx < _visibleLodData.Length; idx++)
            {
                ref readonly var lodObj = ref _visibleLodData[idx];
                lodCosts[idx] = LodCostData.Sum(in allLodCost, in lodObj.totalCost);
            }
            return lodCosts;
        }

        // TODO: This is a silly method to have - IReadOnlyDictionary maybe?
        private Dictionary<int, LodData> CopyVisibleLODData()
        {
            var lodDict = new Dictionary<int, LodData>(lodObjectCount);
            for (int idx = 0; idx < _visibleLodData.Length; idx++)
            {
                ref var lodObj = ref _visibleLodData[idx];
                if (lodObj.HasInstances)
                {
                    lodDict.Add(idx, lodObj);
                }
            }
            return lodDict;
        }

        /// Per-avatar level of detail information.
        public AvatarLOD AvatarLOD
        {
            get
            {
                if (_avatarLOD == null)
                {
                    _avatarLOD = gameObject.GetOrAddComponent<AvatarLOD>();
                    
                    // In some edge cases `GetOrAddComponent` can return null
                    if (_avatarLOD != null)
                    {
                        _avatarLOD.Entity = this;   
                    }
                }

                return _avatarLOD;
            }
        }

        // TODO: Setup LOD control via these properties, suppresing unused warnings for now
#pragma warning disable 0414
        // Intended LOD to render
        // TODO: Have LOD system drive this value
        private readonly uint _targetLodIndex = 0;
        // LOD currently being rendered
        // TODO: Drive this from render state + `ovrAvatar2Entity_[Get/Set]LodFlags`
        private readonly int _currentLodIndex = -1;
#pragma warning restore 0414

        private LodData _visibleAllLodData;

        // High level container for a given singular LOD, may combine multiple primitive instances
        public struct LodData
        {
            internal LodData(GameObject gob)
            {
                gameObject = gob;
                transform = gob.transform;

                instances = new HashSet<OvrAvatarRenderable>();
                totalCost = default;
            }

            public bool IsValid => gameObject != null;
            public bool HasInstances => instances != null && instances.Count > 0;

            // TODO: Refactor AvatarLOD and remove
            public int vertexCount => (int) totalCost.meshVertexCount;
            public int triangleCount => (int) totalCost.renderTriangleCount;

            // TODO: Remove gameObject and transform fields - manage these internally
            public readonly GameObject gameObject;
            public readonly Transform transform;

            // Discrete renderables, may be parented to various gameObjects
            private readonly HashSet<OvrAvatarRenderable> instances;

            public LodCostData totalCost;

            internal void AddInstance(OvrAvatarRenderable newInstance)
            {
                if (instances.Add(newInstance))
                {
                    totalCost = LodCostData.Sum(in totalCost, in newInstance.CostData);
                }
            }
            internal bool RemoveInstance(OvrAvatarRenderable oldInstance)
            {
                bool didRemove = instances.Remove(oldInstance);
                if (didRemove)
                {
                    totalCost = LodCostData.Subtract(in totalCost, in oldInstance.CostData);
                }
                OvrAvatarLog.Assert(didRemove, logScope);
                return didRemove;
            }
            internal void Clear()
            {
                instances.Clear();
                totalCost = default;
            }
        }

        // TODO: Move LodCostData out of OvrAvatarEntity, it is used by other classes too
        /**
         * Contains vertex and triangle counts for a single level of detail.
         * This is used by the avatar LOD system to select the proper
         * LOD based on application specified vertex limits.
         * @see OvrAvatarLODManager
         */
        public readonly struct LodCostData
        {
            /// Number of vertices in avatar mesh.
            public readonly uint meshVertexCount;
            // TODO: Deprecate, use triCount instead
            /// Number of vertices in the morph targets.
            public readonly uint morphVertexCount;
            /// Number of triangles in the avatar mesh.
            public readonly uint renderTriangleCount;
            // TODO: Include number of skinned bones + num morph targets

            private LodCostData(uint meshVertCount, uint morphVertCount, uint triCount)
            {
                meshVertexCount = meshVertCount;
                morphVertexCount = morphVertCount;
                renderTriangleCount = triCount;
            }
            internal LodCostData(OvrAvatarPrimitive prim)
                : this(prim.meshVertexCount, prim.morphVertexCount, prim.triCount) { }
            ///
            /// Add the second LOD cost to the first and return
            /// the combined cost of both LODs.
            ///
            /// @param total    first LodCostData to add.
            /// @param add      second LodCostData to add.
            /// @returns LodCostData with total cost of both LODs.
            // TODO: inplace Increment/Decrement would be useful
            public static LodCostData Sum(in LodCostData total, in LodCostData add)
            {
                return new LodCostData(
                    total.meshVertexCount + add.meshVertexCount,
                    total.morphVertexCount + add.morphVertexCount,
                    total.renderTriangleCount + add.renderTriangleCount
                );
            }

            ///
            /// Subtract the second LOD cost from the first and return
            /// the difference between the LODs.
            ///
            /// @param total    LodCostData to subtract from.
            /// @param sub      LodCostData to subtract.
            /// @returns LodCostData with different between LODs.
            public static LodCostData Subtract(in LodCostData total, in LodCostData sub)
            {
                Debug.Assert(total.meshVertexCount >= sub.meshVertexCount);
                return new LodCostData(
                    total.meshVertexCount - sub.meshVertexCount,
                    total.morphVertexCount - sub.morphVertexCount,
                    total.renderTriangleCount - sub.renderTriangleCount
                );
            }
        }

        protected void InitAvatarLOD()
        {
            AvatarLOD.CulledChangedEvent += OnCullChangedEvent;  // Access to the public AvatarLod causes the component to be GetOrAdded (see above)
        }

        internal void UpdateAvatarLODOverride()    // internal so it can be called from the LOD Manager, which runs at a slower framerate
        {
            _avatarLOD.UpdateOverride();
        }

        protected void ShutdownAvatarLOD()
        {
            if (_avatarLOD != null) // check the private instance to avoid creating a new one on the spot
            {
                _avatarLOD.CulledChangedEvent -= OnCullChangedEvent;

                Destroy(_avatarLOD);
                _avatarLOD = null;
            }
        }

        protected virtual void ComputeImportanceAndCost(out float importance, out UInt32 cost)
        {
            var avatarLod = AvatarLOD;
            var avatarLevel = avatarLod.Level;
            if (0 <= avatarLevel && avatarLevel < avatarLod.vertexCounts.Count)
            {
                importance = avatarLod.updateImportance;
                cost = avatarLod.UpdateCost;
            }
            else
            {
                importance = 0f;
                cost = 0;
            }
        }

        internal void SendImportanceAndCost()    // internal so it can be called from the LOD Manager, which runs at a slower framerate
        {
            ComputeImportanceAndCost(out float importance, out UInt32 cost);

            // set importance for next frame
            CAPI.ovrAvatar2Importance_SetImportanceAndCost(entityId, importance, cost);
        }

        [Conditional("UNITY_DEVELOPMENT")]
        [Conditional("UNITY_EDITOR")]
        internal void TrackUpdateAge()
        {
#if UNITY_EDITOR || UNITY_DEVELOPMENT
            var avatarLod = AvatarLOD;
            // Track of the last update time for debug tools
            if (EntityActive)
            {
                avatarLod.previousUpdateAgeWindowSeconds = avatarLod.lastUpdateAgeSeconds + Time.deltaTime;
                avatarLod.lastUpdateAgeSeconds = 0;
            }
            else
            {
                avatarLod.lastUpdateAgeSeconds += Time.deltaTime;
            }
#endif // UNITY_EDITOR || UNITY_DEVELOPMENT
        }

        public virtual void OnCullChangedEvent(bool culled)
        {
            OnCulled?.Invoke(culled);

#if AVATAR_CULLING_DEBUG
            // Use to easily debug but do not check in enabled, its way too verbose and allocates string:
            OvrAvatarLog.LogInfo("Caught culling event for Avatar " + AvatarLOD.name + (culled ? "":" NOT") + " CULLED", logScope, this);
#endif
        }

        protected void SetupLodGroups()
        {
            bool setupLodGroups = false;

            var avatarLod = AvatarLOD;
            if (lodObjectCount > 1)
            {
                // TODO: Update avatarLOD to take array, didn't want to change *too* many classes all at once
                var lodDict = CopyVisibleLODData();

                // Don't add if effective lodCount is <= 1
                // TODO: It seems like this should be handled by `AvatarLOD`?
                setupLodGroups = lodDict.Count > 1;
                if (setupLodGroups)
                {
                    avatarLod.AddLODGameObjectGroupBySdkRenderers(lodDict);
                }
            }

            if (!setupLodGroups)
            {
                avatarLod.ClearLODGameObjects();
            }
            avatarLod.AddLODActionGroup(gameObject, UpdateAvatarLodColor, 5);
        }

        protected void ResetLodCullingPoints()
        {
            if (_avatarLOD != null)
            {
                _avatarLOD.Reset();
            }
        }

        protected void SetupLodCullingPoints()
        {
            // TODO: This seems like mostly logic which should live in AvatarLODManager?
            // populate the centerXform and the extraXforms for culling
            if (HasJoints)
            {
                var avatarLod = AvatarLOD;
                var lodManager = AvatarLODManager.Instance;

                var skelJoint = GetSkeletonTransformByType(lodManager.JointTypeToCenterOn);

                OvrAvatarLog.Assert(skelJoint);

                avatarLod.centerXform = skelJoint ? skelJoint : _baseTransform;

                avatarLod.extraXforms.Clear();

                foreach (var jointType in lodManager.JointTypesToCullOn)
                {
                    var cullJoint = GetSkeletonTransformByType(jointType);
                    OvrAvatarLog.Assert(cullJoint);
                    if (cullJoint)
                    {
                        avatarLod.extraXforms.Add(cullJoint);
                    }
                }
            }
            else
            {
                // If there are no skeletal joints, reset AvatarLOD to default settings
                TeardownLodCullingPoints();
            }
        }

        protected void TeardownLodCullingPoints()
        {
            if (_avatarLOD)
            {
                // reset JointToCenterOn
                _avatarLOD.centerXform = _baseTransform;

                // reset extraXforms
                _avatarLOD.extraXforms.Clear();
            }
        }

        // Used for tracking Entity's valid LOD range
        private void ResetLODRange()
        {
            LowestQualityLODIndex = HighestQualityLODIndex = -1;
        }
        private void ExpandLODRange(uint lod)
        {
            // TODO: Initial values of -1/-1 aren't super clean
            if (LowestQualityLODIndex < lod) { LowestQualityLODIndex = (int) lod; }
            if (HighestQualityLODIndex < 0 || HighestQualityLODIndex > lod) { HighestQualityLODIndex = (int) lod; }
        }
        private void RefreshLODRange()
        {
            ResetLODRange();
            for (uint lodIdx = 0; lodIdx < _visibleLodData.Length; ++lodIdx)
            {
                if (_visibleLodData[lodIdx].HasInstances)
                {
                    ExpandLODRange(lodIdx);
                }
            }
        }
    }
}
