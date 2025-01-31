using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityS.Transforms
{
    [BurstCompile]
    public partial struct ParentSystem : ISystem
    {
        EntityQuery m_NewParentsQuery;
        EntityQuery m_RemovedParentsQuery;
        EntityQuery m_ExistingParentsQuery;
        EntityQuery m_DeletedParentsQuery;

        static readonly ProfilerMarker k_ProfileDeletedParents = new ProfilerMarker("ParentSystem.DeletedParents");
        static readonly ProfilerMarker k_ProfileRemoveParents = new ProfilerMarker("ParentSystem.RemoveParents");
        static readonly ProfilerMarker k_ProfileChangeParents = new ProfilerMarker("ParentSystem.ChangeParents");
        static readonly ProfilerMarker k_ProfileNewParents = new ProfilerMarker("ParentSystem.NewParents");

        int FindChildIndex(DynamicBuffer<Child> children, Entity entity)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Value == entity)
                    return i;
            }

            throw new InvalidOperationException("Child entity not in parent");
        }

        void RemoveChildFromParent(ref SystemState state, Entity childEntity, Entity parentEntity)
        {
            if (!state.EntityManager.HasComponent<Child>(parentEntity))
                return;

            var children = state.EntityManager.GetBuffer<Child>(parentEntity);
            var childIndex = FindChildIndex(children, childEntity);
            children.RemoveAt(childIndex);
            if (children.Length == 0)
            {
                state.EntityManager.RemoveComponent(parentEntity, ComponentType.FromTypeIndex(
                    TypeManager.GetTypeIndex<Child>()));
            }
        }

        [BurstCompile]
        struct GatherChangedParents : IJobEntityBatch
        {
            public NativeParallelMultiHashMap<Entity, Entity>.ParallelWriter ParentChildrenToAdd;
            public NativeParallelMultiHashMap<Entity, Entity>.ParallelWriter ParentChildrenToRemove;
            public NativeParallelHashMap<Entity, int>.ParallelWriter UniqueParents;
            public ComponentTypeHandle<PreviousParent> PreviousParentTypeHandle;
            [ReadOnly] public BufferFromEntity<Child> ChildFromEntity;

            [ReadOnly] public ComponentTypeHandle<Parent> ParentTypeHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            public uint LastSystemVersion;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                if (batchInChunk.DidChange(ParentTypeHandle, LastSystemVersion) ||
                    batchInChunk.DidChange(PreviousParentTypeHandle, LastSystemVersion))
                {
                    var chunkPreviousParents = batchInChunk.GetNativeArray(PreviousParentTypeHandle);
                    var chunkParents = batchInChunk.GetNativeArray(ParentTypeHandle);
                    var chunkEntities = batchInChunk.GetNativeArray(EntityTypeHandle);

                    for (int j = 0; j < batchInChunk.Count; j++)
                    {
                        if (chunkParents[j].Value != chunkPreviousParents[j].Value)
                        {
                            var childEntity = chunkEntities[j];
                            var parentEntity = chunkParents[j].Value;
                            var previousParentEntity = chunkPreviousParents[j].Value;

                            ParentChildrenToAdd.Add(parentEntity, childEntity);
                            UniqueParents.TryAdd(parentEntity, 0);

                            if (ChildFromEntity.HasComponent(previousParentEntity))
                            {
                                ParentChildrenToRemove.Add(previousParentEntity, childEntity);
                                UniqueParents.TryAdd(previousParentEntity, 0);
                            }

                            chunkPreviousParents[j] = new PreviousParent
                            {
                                Value = parentEntity
                            };
                        }
                    }
                }
            }
        }

        [BurstCompile]
        struct FindMissingChild : IJob
        {
            [ReadOnly] public NativeParallelHashMap<Entity, int> UniqueParents;
            [ReadOnly] public BufferFromEntity<Child> ChildFromEntity;
            public NativeList<Entity> ParentsMissingChild;

            public void Execute()
            {
                var parents = UniqueParents.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < parents.Length; i++)
                {
                    var parent = parents[i];
                    if (!ChildFromEntity.HasComponent(parent))
                    {
                        ParentsMissingChild.Add(parent);
                    }
                }
            }
        }

        [BurstCompile]
        struct FixupChangedChildren : IJob
        {
            [ReadOnly] public NativeParallelMultiHashMap<Entity, Entity> ParentChildrenToAdd;
            [ReadOnly] public NativeParallelMultiHashMap<Entity, Entity> ParentChildrenToRemove;
            [ReadOnly] public NativeParallelHashMap<Entity, int> UniqueParents;

            public BufferFromEntity<Child> ChildFromEntity;

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void ThrowChildEntityNotInParent()
            {
                throw new InvalidOperationException("Child entity not in parent");
            }

            int FindChildIndex(DynamicBuffer<Child> children, Entity entity)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].Value == entity)
                        return i;
                }

                ThrowChildEntityNotInParent();
                return -1;
            }

            void RemoveChildrenFromParent(Entity parent, DynamicBuffer<Child> children)
            {
                if (ParentChildrenToRemove.TryGetFirstValue(parent, out var child, out var it))
                {
                    do
                    {
                        var childIndex = FindChildIndex(children, child);
                        children.RemoveAt(childIndex);
                    }
                    while (ParentChildrenToRemove.TryGetNextValue(out child, ref it));
                }
            }

            void AddChildrenToParent(Entity parent, DynamicBuffer<Child> children)
            {
                if (ParentChildrenToAdd.TryGetFirstValue(parent, out var child, out var it))
                {
                    do
                    {
                        children.Add(new Child() { Value = child });
                    }
                    while (ParentChildrenToAdd.TryGetNextValue(out child, ref it));
                }
            }

            public void Execute()
            {
                var parents = UniqueParents.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < parents.Length; i++)
                {
                    var parent = parents[i];
                    if (ChildFromEntity.TryGetBuffer(parent, out var children))
                    {
                        RemoveChildrenFromParent(parent, children);
                        AddChildrenToParent(parent, children);
                    }
                }
            }
        }
        
        //burst disabled pending burstable EntityQueryDesc
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_NewParentsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Parent>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<LocalToParent>()
                },
                None = new ComponentType[]
                {
                    typeof(PreviousParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_RemovedParentsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(PreviousParent)
                },
                None = new ComponentType[]
                {
                    typeof(Parent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_ExistingParentsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Parent>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<LocalToParent>(),
                    typeof(PreviousParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_ExistingParentsQuery.SetChangedVersionFilter(new ComponentType[] {typeof(Parent), typeof(PreviousParent)});
            m_DeletedParentsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Child)
                },
                None = new ComponentType[]
                {
                    typeof(LocalToWorld)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        void UpdateNewParents(ref SystemState state)
        {
            if (m_NewParentsQuery.IsEmptyIgnoreFilter)
                return;

            state.EntityManager.AddComponent(m_NewParentsQuery, ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<PreviousParent>()));
        }

        void UpdateRemoveParents(ref SystemState state)
        {
            if (m_RemovedParentsQuery.IsEmptyIgnoreFilter)
                return;

            var childEntities = m_RemovedParentsQuery.ToEntityArray(Allocator.TempJob);
            var previousParents = m_RemovedParentsQuery.ToComponentDataArray<PreviousParent>(Allocator.TempJob);

            for (int i = 0; i < childEntities.Length; i++)
            {
                var childEntity = childEntities[i];
                var previousParentEntity = previousParents[i].Value;

                RemoveChildFromParent(ref state, childEntity, previousParentEntity);
            }

            state.EntityManager.RemoveComponent(m_RemovedParentsQuery, ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<PreviousParent>()));
            childEntities.Dispose();
            previousParents.Dispose();
        }

        void UpdateChangeParents(ref SystemState state)
        {
            if (m_ExistingParentsQuery.IsEmptyIgnoreFilter)
                return;

            var count = m_ExistingParentsQuery.CalculateEntityCount() * 2; // Potentially 2x changed: current and previous
            if (count == 0)
                return;

            // 1. Get (Parent,Child) to remove
            // 2. Get (Parent,Child) to add
            // 3. Get unique Parent change list
            // 4. Set PreviousParent to new Parent
            var parentChildrenToAdd = new NativeParallelMultiHashMap<Entity, Entity>(count, Allocator.TempJob);
            var parentChildrenToRemove = new NativeParallelMultiHashMap<Entity, Entity>(count, Allocator.TempJob);
            var uniqueParents = new NativeParallelHashMap<Entity, int>(count, Allocator.TempJob);
            var gatherChangedParentsJob = new GatherChangedParents
            {
                ParentChildrenToAdd = parentChildrenToAdd.AsParallelWriter(),
                ParentChildrenToRemove = parentChildrenToRemove.AsParallelWriter(),
                UniqueParents = uniqueParents.AsParallelWriter(),
                PreviousParentTypeHandle = state.GetComponentTypeHandle<PreviousParent>(false),
                ChildFromEntity = state.GetBufferFromEntity<Child>(),
                ParentTypeHandle = state.GetComponentTypeHandle<Parent>(true),
                EntityTypeHandle = state.GetEntityTypeHandle(),
                LastSystemVersion = state.LastSystemVersion
            };
            var gatherChangedParentsJobHandle = gatherChangedParentsJob.ScheduleParallel(m_ExistingParentsQuery);
            gatherChangedParentsJobHandle.Complete();

            // 5. (Structural change) Add any missing Child to Parents
            var parentsMissingChild = new NativeList<Entity>(Allocator.TempJob);
            var findMissingChildJob = new FindMissingChild
            {
                UniqueParents = uniqueParents,
                ChildFromEntity = state.GetBufferFromEntity<Child>(true),
                ParentsMissingChild = parentsMissingChild
            };
            var findMissingChildJobHandle = findMissingChildJob.Schedule();
            findMissingChildJobHandle.Complete();

            state.EntityManager.AddComponent(parentsMissingChild.AsArray(), ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<Child>()));

            // 6. Get Child[] for each unique Parent
            // 7. Update Child[] for each unique Parent
            var fixupChangedChildrenJob = new FixupChangedChildren
            {
                ParentChildrenToAdd = parentChildrenToAdd,
                ParentChildrenToRemove = parentChildrenToRemove,
                UniqueParents = uniqueParents,
                ChildFromEntity = state.GetBufferFromEntity<Child>()
            };

            var fixupChangedChildrenJobHandle = fixupChangedChildrenJob.Schedule();
            fixupChangedChildrenJobHandle.Complete();

            parentChildrenToAdd.Dispose();
            parentChildrenToRemove.Dispose();
            uniqueParents.Dispose();
            parentsMissingChild.Dispose();
        }

        [BurstCompile]
        struct GatherChildEntities : IJob
        {
            [ReadOnly] public NativeArray<Entity> Parents;
            public NativeList<Entity> Children;
            [ReadOnly] public BufferFromEntity<Child> ChildFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;

            public void Execute()
            {
                for (int i = 0; i < Parents.Length; i++)
                {
                    var parentEntity = Parents[i];
                    var childEntitiesSource = ChildFromEntity[parentEntity].AsNativeArray();
                    for (int j = 0; j < childEntitiesSource.Length; j++)
                    {
                        var childEntity = childEntitiesSource[j].Value;
                        if (ParentFromEntity.HasComponent(childEntity) && ParentFromEntity[childEntity].Value == parentEntity)
                        {
                            Children.Add(childEntitiesSource[j].Value);
                        }
                    }
                }
            }
        }

        void UpdateDeletedParents(ref SystemState state)
        {
            if (m_DeletedParentsQuery.IsEmptyIgnoreFilter)
                return;

            var previousParents = m_DeletedParentsQuery.ToEntityArray(Allocator.TempJob);
            var childEntities = new NativeList<Entity>(Allocator.TempJob);
            var gatherChildEntitiesJob = new GatherChildEntities
            {
                Parents = previousParents,
                Children = childEntities,
                ChildFromEntity = state.GetBufferFromEntity<Child>(true),
                ParentFromEntity = state.GetComponentDataFromEntity<Parent>(true),
            };
            var gatherChildEntitiesJobHandle = gatherChildEntitiesJob.Schedule();
            gatherChildEntitiesJobHandle.Complete();

            state.EntityManager.RemoveComponent(
                childEntities,
                ComponentType.FromTypeIndex(
                    TypeManager.GetTypeIndex<Parent>()));
            state.EntityManager.RemoveComponent(childEntities, ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<PreviousParent>()));
            state.EntityManager.RemoveComponent(childEntities, ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<LocalToParent>()));
            state.EntityManager.RemoveComponent(m_DeletedParentsQuery, ComponentType.FromTypeIndex(
                TypeManager.GetTypeIndex<Child>()));

            childEntities.Dispose();
            previousParents.Dispose();
        }

        //burst disabled pending IJobBurstSchedulable not requiring hardcoded calls to init
        //for every distinct job
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)//JobHandle inputDeps)
        {
            //inputDeps.Complete(); // #todo
            state.Dependency.Complete();

            k_ProfileDeletedParents.Begin();
            UpdateDeletedParents(ref state);
            k_ProfileDeletedParents.End();

            k_ProfileRemoveParents.Begin();
            UpdateRemoveParents(ref state);
            k_ProfileRemoveParents.End();

            k_ProfileNewParents.Begin();
            UpdateNewParents(ref state);
            k_ProfileNewParents.End();

            k_ProfileChangeParents.Begin();
            UpdateChangeParents(ref state);
            k_ProfileChangeParents.End();

        }
    }
}
