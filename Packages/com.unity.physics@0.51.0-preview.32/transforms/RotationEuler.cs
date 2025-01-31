using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityS.Mathematics;

/* **************
   COPY AND PASTE
   **************
 * PostRotationEuler.cs and RotationEuler.cs are copy-and-paste.
 * Any changes to one must be copied to the other.
 * The only differences are:
 *   s/PostRotation/Rotation/g
*/

namespace UnityS.Transforms
{
    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerXYZ : IComponentData
    {
        public float3 Value;
    }

    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerXZY : IComponentData
    {
        public float3 Value;
    }

    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerYXZ : IComponentData
    {
        public float3 Value;
    }

    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerYZX : IComponentData
    {
        public float3 Value;
    }

    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerZXY : IComponentData
    {
        public float3 Value;
    }

    [Serializable]
    [WriteGroup(typeof(Rotation))]
    public struct RotationEulerZYX : IComponentData
    {
        public float3 Value;
    }

    // Rotation = RotationEulerXYZ
    // (or) Rotation = RotationEulerXZY
    // (or) Rotation = RotationEulerYXZ
    // (or) Rotation = RotationEulerYZX
    // (or) Rotation = RotationEulerZXY
    // (or) Rotation = RotationEulerZYX
    [BurstCompile]
    public partial struct RotationEulerSystem : ISystem
    {
        private EntityQuery m_Query;

        //burst disabled pending burstable entityquerydesc
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_Query = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Rotation)
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<RotationEulerXYZ>(),
                    ComponentType.ReadOnly<RotationEulerXZY>(),
                    ComponentType.ReadOnly<RotationEulerYXZ>(),
                    ComponentType.ReadOnly<RotationEulerYZX>(),
                    ComponentType.ReadOnly<RotationEulerZXY>(),
                    ComponentType.ReadOnly<RotationEulerZYX>()
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }

        [BurstCompile]
        struct RotationEulerToRotation : IJobEntityBatch
        {
            public ComponentTypeHandle<Rotation> RotationTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerXYZ> RotationEulerXyzTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerXZY> RotationEulerXzyTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerYXZ> RotationEulerYxzTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerYZX> RotationEulerYzxTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerZXY> RotationEulerZxyTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationEulerZYX> RotationEulerZyxTypeHandle;
            public uint LastSystemVersion;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                if (batchInChunk.Has(RotationEulerXyzTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerXyzTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerXYZs = batchInChunk.GetNativeArray(RotationEulerXyzTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerXYZ(chunkRotationEulerXYZs[i].Value)
                        };
                    }
                }
                else if (batchInChunk.Has(RotationEulerXzyTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerXzyTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerXZYs = batchInChunk.GetNativeArray(RotationEulerXzyTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerXZY(chunkRotationEulerXZYs[i].Value)
                        };
                    }
                }
                else if (batchInChunk.Has(RotationEulerYxzTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerYxzTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerYXZs = batchInChunk.GetNativeArray(RotationEulerYxzTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerYXZ(chunkRotationEulerYXZs[i].Value)
                        };
                    }
                }
                else if (batchInChunk.Has(RotationEulerYzxTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerYzxTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerYZXs = batchInChunk.GetNativeArray(RotationEulerYzxTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerYZX(chunkRotationEulerYZXs[i].Value)
                        };
                    }
                }
                else if (batchInChunk.Has(RotationEulerZxyTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerZxyTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerZXYs = batchInChunk.GetNativeArray(RotationEulerZxyTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerZXY(chunkRotationEulerZXYs[i].Value)
                        };
                    }
                }
                else if (batchInChunk.Has(RotationEulerZyxTypeHandle))
                {
                    if (!batchInChunk.DidChange(RotationEulerZyxTypeHandle, LastSystemVersion))
                        return;

                    var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                    var chunkRotationEulerZYXs = batchInChunk.GetNativeArray(RotationEulerZyxTypeHandle);
                    for (var i = 0; i < batchInChunk.Count; i++)
                    {
                        chunkRotations[i] = new Rotation
                        {
                            Value = quaternion.EulerZYX(chunkRotationEulerZYXs[i].Value)
                        };
                    }
                }
            }
        }

        //disabling burst in dotsrt until burstable scheduling works
#if !UNITY_DOTSRUNTIME
        [BurstCompile]
#endif
        public void OnUpdate(ref SystemState state)
        {
            var job = new RotationEulerToRotation()
            {
                RotationTypeHandle = state.GetComponentTypeHandle<Rotation>(false),
                RotationEulerXyzTypeHandle = state.GetComponentTypeHandle<RotationEulerXYZ>(true),
                RotationEulerXzyTypeHandle = state.GetComponentTypeHandle<RotationEulerXZY>(true),
                RotationEulerYxzTypeHandle = state.GetComponentTypeHandle<RotationEulerYXZ>(true),
                RotationEulerYzxTypeHandle = state.GetComponentTypeHandle<RotationEulerYZX>(true),
                RotationEulerZxyTypeHandle = state.GetComponentTypeHandle<RotationEulerZXY>(true),
                RotationEulerZyxTypeHandle = state.GetComponentTypeHandle<RotationEulerZYX>(true),
                LastSystemVersion = state.LastSystemVersion
            };
            state.Dependency = job.ScheduleParallel(m_Query, state.Dependency);
        }
    }
}
