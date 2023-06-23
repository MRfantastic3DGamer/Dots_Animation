using AI_Objective;
using Pick_ups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Scripting;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Cell_Division
{
    [BurstCompile]
    public partial struct SpatialPartitioningSystem : ISystem
    {
        const int InitialCapacity = 2000;


        NativeParallelMultiHashMap<int, int> characterMap;
        NativeList<Entity> characterEntities;
        NativeList<CharacterSpatialDataAspect> CharacterSpatialDatas;
        
        NativeParallelMultiHashMap<int, int> pickupMap;
        NativeList<Entity> pickupEntities;
        NativeList<PickupSpatialDataAspect> PickupSpatialDatas;

        int m_Capacity;
        float3 m_CellSize;

        internal JobHandle SpatialDataScheduleUpdate(ref SystemState state, JobHandle dependency)
        {
            dependency = new DataClearJob
            {
                CharMap = characterMap,
                CharEntities = characterEntities,
                CharDatas = CharacterSpatialDatas,
                
                PickupMap = pickupMap,
                PickupEntities = pickupEntities,
                PickupDatas = PickupSpatialDatas,
            }.Schedule(dependency);
            
            var charCopyHandle = new CharSpatialDataCopyJob
            {
                CharEntities = characterEntities,
                CharDatas = CharacterSpatialDatas,
            }.Schedule(dependency);

            var pickupCopyHandle = new PickupSpatialDataCopyJob
            {
                PickupEntities = pickupEntities,
                PickupDatas = PickupSpatialDatas,
            }.Schedule(dependency);
            
            var charHashHandle = new CharSpatialDataHashJob
            {
                Map = characterMap.AsParallelWriter(),
                CellSize = m_CellSize,
            }.Schedule(dependency);

            var pickupHashHandle = new PickupSpatialDataHashJob
            {
                Map = pickupMap.AsParallelWriter(),
                CellSize = m_CellSize,
            }.Schedule(dependency);

            return JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(charHashHandle, pickupHashHandle),
                JobHandle.CombineDependencies(charCopyHandle, pickupCopyHandle));
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = GetSingletonRW<SpatialPartitioningSingleton>();
            if (TryGetSingleton(out SpatialPartitioningSettings settings))
            {
                singleton.ValueRW.m_CellSize = settings.CellSize;
                m_CellSize = settings.CellSize;

                if (singleton.ValueRW.m_Capacity != settings.AgentCapacity)
                {
                    state.Dependency = new CharSpatialDataChangeCapacityJob
                    {
                        CharMap = characterMap,
                        CharEntities = characterEntities,
                        CharDatas = CharacterSpatialDatas,
                        
                        PickupMap = pickupMap,
                        PickupEntities = pickupEntities,
                        PickupSpatialDataAspects = PickupSpatialDatas,
                        
                        CharCapacity = settings.AgentCapacity,
                        PickUpCapacity = settings.PickupsCapacity,
                    }.Schedule(state.Dependency);
                    
                    singleton.ValueRW.m_Capacity = settings.AgentCapacity;
                }
            }
            
            state.Dependency = SpatialDataScheduleUpdate(ref state, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            characterMap = new NativeParallelMultiHashMap<int, int>(InitialCapacity, Allocator.Persistent);
            CharacterSpatialDatas = new NativeList<CharacterSpatialDataAspect>(InitialCapacity, Allocator.Persistent);
            characterEntities = new NativeList<Entity>(InitialCapacity, Allocator.Persistent);
            
            pickupMap = new NativeParallelMultiHashMap<int, int>(InitialCapacity, Allocator.Persistent);
            pickupEntities = new NativeList<Entity>(InitialCapacity, Allocator.Persistent);
            PickupSpatialDatas = new NativeList<PickupSpatialDataAspect>(InitialCapacity, Allocator.Persistent);
            
            m_Capacity = 2000;

            state.EntityManager.AddComponentData(state.SystemHandle, new SpatialPartitioningSingleton
            {
                charMap = characterMap,
                charDatas = CharacterSpatialDatas,
                charEntities = characterEntities,
                
                pickupMap = pickupMap,
                PickupSpatialDatas = PickupSpatialDatas,
                pickupEntities = pickupEntities,
                
                m_Capacity = m_Capacity,
                m_CellSize = m_CellSize,
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState systemState)
        {
            characterMap.Dispose();
            characterEntities.Dispose();
            CharacterSpatialDatas.Dispose();
        }
        
        
        public struct SpatialPartitioningSingleton : IComponentData
        {
            internal NativeParallelMultiHashMap<int, int> charMap;
            internal NativeList<Entity> charEntities;
            internal NativeList<CharacterSpatialDataAspect> charDatas;
            
            internal NativeParallelMultiHashMap<int, int> pickupMap;
            internal NativeList<Entity> pickupEntities;
            internal NativeList<PickupSpatialDataAspect> PickupSpatialDatas;
            
            internal int m_Capacity;
            internal float3 m_CellSize;
    
            /// <summary>
            /// Query agents that intersect with the line.
            /// </summary>
            public int CharQueryLine<T>(float3 from, float3 to, ref T action) where T : unmanaged, ISpatialQueryEntity
            {
                int count = 0;
                
                // Convert to unit voxel size
                from = from / m_CellSize;
                to = to / m_CellSize;
    
                // Convert to parametric line form: u + v * t, t >= 0
                float3 u = from;
                float3 v = to - from;
    
                // Find start and end voxel coordinates
                int3 point = (int3) round(from);
                int3 end = (int3) round(to);
    
                // Initialized to either 1 or - 1 indicating whether X and Y are incremented or decremented as the
                // ray crosses voxel boundaries(this is determined by the sign of the x and y components of → v).
                int3 step = (int3) sign(v);
    
                float3 boundaryDistance = select(-0.5f, 0.5f, step == 1);
    
                // Here we find distance to closest voxel boundary on each axis
                // Formula is actually quite simple we just equate parametric line to cloest voxel boundary
                // u + v * t = start + boundaryDistance, step = 1
                // u + v * t = start - boundaryDistance, step = -1
                float3 tMax = select((point + boundaryDistance - u) / v, float.MaxValue, step == 0);
    
                // TDelta indicates how far along the ray we must move
                // (in units of t) for the horizontal component of such a movement to equal the width of a voxel.
                // Similarly, we store in tDeltaY the amount of movement along the ray which has a vertical component equal to the height of a voxel.
                float3 tDelta = select(abs(1f / v), float.MaxValue, step == 0);
    
                // Loop through each voxel
                for (int i = 0; i < 100; ++i)
                {
                    int hash = GetCellHash(point.x, point.y, point.z);
    
                    // Find all entities in the bucket
                    if (charMap.TryGetFirstValue(hash, out int index, out var iterator))
                    {
                        do
                        {
                            ExecuteCharAction(action, index);
                            count++;
                        }
                        while (charMap.TryGetNextValue(out index, ref iterator));
                    }
    
                    // Stop if reached the end voxel
                    if (all(point == end))
                        break;
    
                    // Progress line towards the voxel that will be reached fastest
                    if (tMax.x < tMax.y)
                    {
                        if (tMax.x < tMax.z)
                        {
                            tMax.x = tMax.x + tDelta.x;
                            point.x = point.x + step.x;
                        }
                        else
                        {
                            tMax.z = tMax.z + tDelta.z;
                            point.z = point.z + step.z;
                        }
                    }
                    else
                    {
                        if (tMax.y < tMax.z)
                        {
                            tMax.y = tMax.y + tDelta.y;
                            point.y = point.y + step.y;
                        }
                        else
                        {
                            tMax.z = tMax.z + tDelta.z;
                            point.z = point.z + step.z;
                        }
                    }
                }
    
                return count;
            }
    
            /// <summary>
            /// Query agents that intersect with the line.
            /// </summary>
            // public int QueryLineRef<T>(EntityManager entityManager, float3 from, float3 to, ref T action) where T : unmanaged, ISpatialQueryEntity
            // {
            //     int count = 0;
            //     
            //     // Convert to unit voxel size
            //     from = from / m_CellSize;
            //     to = to / m_CellSize;
            //
            //     // Convert to parametric line form: u + v * t, t >= 0
            //     float3 u = from;
            //     float3 v = to - from;
            //
            //     // Find start and end voxel coordinates
            //     int3 point = (int3) round(from);
            //     int3 end = (int3) round(to);
            //
            //     // Initialized to either 1 or - 1 indicating whether X and Y are incremented or decremented as the
            //     // ray crosses voxel boundaries(this is determined by the sign of the x and y components of → v).
            //     int3 step = (int3) sign(v);
            //
            //     float3 boundaryDistance = select(-0.5f, 0.5f, step == 1);
            //
            //     // Here we find distance to closest voxel boundary on each axis
            //     // Formula is actually quite simple we just equate parametric line to cloest voxel boundary
            //     // u + v * t = start + boundaryDistance, step = 1
            //     // u + v * t = start - boundaryDistance, step = -1
            //     float3 tMax = select((point + boundaryDistance - u) / v, float.MaxValue, step == 0);
            //
            //     // TDelta indicates how far along the ray we must move
            //     // (in units of t) for the horizontal component of such a movement to equal the width of a voxel.
            //     // Similarly, we store in tDeltaY the amount of movement along the ray which has a vertical component equal to the height of a voxel.
            //     float3 tDelta = select(abs(1f / v), float.MaxValue, step == 0);
            //
            //     // Loop through each voxel
            //     for (int i = 0; i < 100; ++i)
            //     {
            //         int hash = GetCellHash(point.x, point.y, point.z);
            //
            //         // Find all entities in the bucket
            //         if (m_Map.TryGetFirstValue(hash, out int index, out var iterator))
            //         {
            //             do
            //             {
            //                 ExecuteRefAction(entityManager, action, index);
            //                 count++;
            //             }
            //             while (m_Map.TryGetNextValue(out index, ref iterator));
            //         }
            //
            //         // Stop if reached the end voxel
            //         if (all(point == end))
            //             break;
            //
            //         // Progress line towards the voxel that will be reached fastest
            //         if (tMax.x < tMax.y)
            //         {
            //             if (tMax.x < tMax.z)
            //             {
            //                 tMax.x = tMax.x + tDelta.x;
            //                 point.x = point.x + step.x;
            //             }
            //             else
            //             {
            //                 tMax.z = tMax.z + tDelta.z;
            //                 point.z = point.z + step.z;
            //             }
            //         }
            //         else
            //         {
            //             if (tMax.y < tMax.z)
            //             {
            //                 tMax.y = tMax.y + tDelta.y;
            //                 point.y = point.y + step.y;
            //             }
            //             else
            //             {
            //                 tMax.z = tMax.z + tDelta.z;
            //                 point.z = point.z + step.z;
            //             }
            //         }
            //     }
            //
            //     return count;
            // }

            /// <summary>
            /// Query agents that intersect with the sphere.
            /// </summary>
            public int QuerySphere<T>(float3 center, float radius, ref T action, SpatialQueryMode mode) where T : unmanaged, ISpatialQueryEntity
            {
                int count = 0;
    
                // Find min and max point in radius
                int3 min = (int3) math.round((center - radius) / m_CellSize);
                int3 max = (int3) math.round((center + radius) / m_CellSize);
    
                max++;
                int index;
                NativeParallelMultiHashMapIterator<int> iterator;
                for (int i = min.x; i < max.x; ++i)
                {
                    for (int j = min.y; j < max.y; ++j)
                    {
                        for (int k = min.z; k < max.z; ++k)
                        {
                            int hash = GetCellHash(i, j, k);
                            
                            if (mode == SpatialQueryMode.Character && charMap.TryGetFirstValue(hash, out index, out iterator))
                            {
                                do
                                {
                                    ExecuteCharAction(action, index);
                                    count++;
                                }
                                while (charMap.TryGetNextValue(out index, ref iterator));
                            }
                            else if (mode == SpatialQueryMode.Pickup && pickupMap.TryGetFirstValue(hash, out index, out iterator))
                            {
                                do
                                {
                                    ExecutePickupAction(action, index);
                                    count++;
                                }
                                while (pickupMap.TryGetNextValue(out index, ref iterator));
                            }
                        }
                    }
                }
    
                return count;
            }

            /// <summary>
            /// Query agents that intersect with the cylinder.
            /// </summary>
            public int CharQueryCylinder<T>(float3 center, float radius, float height, ref T action) where T : unmanaged, ISpatialQueryEntity
            {
                int count = 0;
    
                // Find min and max point in radius
                int3 min = (int3) math.round((center - new float3(radius, 0, radius)) / m_CellSize);
                int3 max = (int3) math.round((center + new float3(radius, height, radius)) / m_CellSize);
    
                max++;
    
                for (int i = min.x; i < max.x; ++i)
                {
                    for (int j = min.y; j < max.y; ++j)
                    {
                        for (int k = min.z; k < max.z; ++k)
                        {
                            int hash = GetCellHash(i, j, k);
    
                            // Find all entities in the bucket
                            if (charMap.TryGetFirstValue(hash, out int index, out var iterator))
                            {
                                do
                                {
                                    ExecuteCharAction(action, index);
                                    count++;
                                }
                                while (charMap.TryGetNextValue(out index, ref iterator));
                            }
                        }
                    }
                }
    
                return count;
            }
            
            /// <summary>
            /// Query partitions that intersect with the sphere.
            /// </summary>
            public int CharQuerySphereBoxes<T>(float3 center, float radius, T action) where T : unmanaged, ISpatialQueryVolume
            {
                int count = 0;
    
                // Find min and max point in radius
                int3 min = (int3) math.round((center - radius) / m_CellSize);
                int3 max = (int3) math.round((center + radius) / m_CellSize);
    
                max++;
    
                for (int i = min.x; i < max.x; ++i)
                {
                    for (int j = min.y; j < max.y; ++j)
                    {
                        for (int k = min.z; k < max.z; ++k)
                        {
                            action.Execute(new float3(i, j, k) * m_CellSize, m_CellSize);
                            count++;
                        }
                    }
                }
    
                return count;
            }
    
            /// <summary>
            /// Query partitions that intersect with the cylinder.
            /// </summary>
            public int CharQueryCylinderBoxes<T>(float3 center, float radius, float height, T action) where T : unmanaged, ISpatialQueryVolume
            {
                int count = 0;
    
                // Find min and max point in radius
                int3 min = (int3) math.round((center - new float3(radius, 0, radius)) / m_CellSize);
                int3 max = (int3) math.round((center + new float3(radius, height, radius)) / m_CellSize);
    
                max++;
    
                for (int i = min.x; i < max.x; ++i)
                {
                    for (int j = min.y; j < max.y; ++j)
                    {
                        for (int k = min.z; k < max.z; ++k)
                        {
                            action.Execute(new float3(i, j, k) * m_CellSize, m_CellSize);
                            count++;
                        }
                    }
                }
    
                return count;
            }
    
            static int GetCellHash(int x, int y, int z)
            {
                var hash = (int) math.hash(new int3(x, y, z));
                return hash;
            }

            void ExecuteCharAction<T>(T action, int index) where T : ISpatialQueryEntity
            {
                action.SpatialQueryAction(
                    otherEntity: charEntities[index],
                    characterSpatialDataAspect: charDatas[index]);
            }

            void ExecutePickupAction<T>(T action, int index) where T : ISpatialQueryEntity
            {
                action.SpatialQueryAction(
                    otherEntity: charEntities[index],
                    pickupSpatialDataAspect: PickupSpatialDatas[index]);
            }
        }
    }

    public interface ISpatialQueryEntity
    {
        [RequiredMember]
        SpatialQueryMode SetMode();

        [RequiredMember]
        void SpatialQueryAction(Entity otherEntity, CharacterSpatialDataAspect characterSpatialDataAspect);
        
        [RequiredMember]
        void SpatialQueryAction(Entity otherEntity, PickupSpatialDataAspect pickupSpatialDataAspect);
    }

    public interface ISpatialQueryVolume
    {
        void Execute(float3 position, float3 size);
    }
    
    public enum SpatialQueryMode
    {
        Character,
        Pickup,
    }
    
    [BurstCompile]
    partial struct CharSpatialDataCopyJob : IJobEntity
    {
        public NativeList<Entity> CharEntities;
        public NativeList<CharacterSpatialDataAspect> CharDatas;

        void Execute(Entity entity, CharacterSpatialDataAspect data)
        {
            CharEntities.Add(entity);
            CharDatas.Add(data);
        }
    }
    
    [BurstCompile]
    partial struct PickupSpatialDataCopyJob : IJobEntity
    {
        public NativeList<Entity> PickupEntities;
        public NativeList<PickupSpatialDataAspect> PickupDatas;

        void Execute(Entity entity, PickupSpatialDataAspect data)
        {
            PickupEntities.Add(entity);
            PickupDatas.Add(data);
        }
    }
    
    [BurstCompile]
    partial struct CharSpatialDataHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, int>.ParallelWriter Map;
        public float3 CellSize;
        void Execute([EntityIndexInQuery] int entityInQueryIndex, CharacterSpatialDataAspect Data)
        {
            var hash = GetCellHash(Data.Transforms.ValueRO.Position);
            Map.Add(hash, entityInQueryIndex);
        }

        int GetCellHash(float3 value)
        {
            var hash = (int) math.hash(new int3(math.round(value.xyz / CellSize)));
            return hash;
        }
    }

    [BurstCompile]
    partial struct PickupSpatialDataHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, int>.ParallelWriter Map;
        public float3 CellSize;

        void Execute([EntityIndexInQuery] int entityInQueryIndex, PickupSpatialDataAspect data)
        {
            var hash = GetCellHash(data.Transform.ValueRO.Position);
            Map.Add(hash, entityInQueryIndex);
        }
        
        int GetCellHash(float3 value)
        {
            var hash = (int) math.hash(new int3(math.round(value.xyz / CellSize)));
            return hash;
        }
    }

    [BurstCompile]
    struct DataClearJob : IJob
    {
        public NativeParallelMultiHashMap<int, int> CharMap;
        public NativeList<Entity> CharEntities;
        public NativeList<CharacterSpatialDataAspect> CharDatas;
        public NativeParallelMultiHashMap<int, int> PickupMap;
        public NativeList<Entity> PickupEntities;
        public NativeList<PickupSpatialDataAspect> PickupDatas;

        public void Execute()
        {
            CharMap.Clear();
            CharEntities.Clear();
            CharDatas.Clear();
            
            PickupMap.Clear();
            PickupEntities.Clear();
            PickupDatas.Clear();
        }
    }

    [BurstCompile]
    struct CharSpatialDataChangeCapacityJob : IJob
    {
        public NativeParallelMultiHashMap<int, int> CharMap;
        public NativeList<Entity> CharEntities;
        public NativeList<CharacterSpatialDataAspect> CharDatas;
        
        public NativeParallelMultiHashMap<int, int> PickupMap;
        public NativeList<Entity> PickupEntities;
        public NativeList<PickupSpatialDataAspect> PickupSpatialDataAspects;
        
        public int CharCapacity;
        public int PickUpCapacity;

        public void Execute()
        {
            CharMap.Capacity = CharCapacity;
            CharEntities.Capacity = CharCapacity;
            CharDatas.Capacity = CharCapacity;

            PickupMap.Capacity = PickUpCapacity;
            PickupEntities.Capacity = PickUpCapacity;
            PickupSpatialDataAspects.Capacity = PickUpCapacity;
        }
    }
    
    public readonly partial struct CharacterSpatialDataAspect : IAspect
    {
        public readonly RefRO<LocalTransform> Transforms;
        public readonly RefRO<AIInteractionSignalData> characterSignals;
    }
    
    public readonly partial struct PickupSpatialDataAspect : IAspect
    {
        public readonly RefRO<LocalTransform> Transform;
        public readonly RefRO<PickupData> pickup;
    }
}
