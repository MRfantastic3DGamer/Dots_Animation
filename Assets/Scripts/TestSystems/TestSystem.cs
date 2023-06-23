using Cell_Division;
using Refference;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TestSystems
{
    public partial struct TestSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            TestAction t = new TestAction();
            var spatialPartitioningSingleton = SystemAPI.GetSingleton<SpatialPartitioningSystem.SpatialPartitioningSingleton>();
            var playerPos = SystemAPI.GetSingleton<MirroredPlayerTransform>().Position;
            // var jobHandle = new DrawRayJob {ss = spatialPartitioningSingleton, playerPos = playerPos}.ScheduleParallel(state.Dependency);
            // jobHandle.Complete();
        }
        
        // partial struct DrawRayJob : IJobEntity
        // {
        //     [ReadOnly] public SpatialPartitioningSystem.SpatialPartitioningSingleton ss;
        //     [ReadOnly] public float3 playerPos;
        //     public void Execute(Entity entity, LocalTransform transform)
        //     {
        //         NativeList<float3> refs = new NativeList<float3>(2, Allocator.Temp);
        //         refs.Add(float3.zero);
        //         refs.Add(new float3(10000, 0, 0));
        //         TestAction t = new TestAction
        //         {
        //             refs = refs,
        //             cp = transform.Position,
        //         };
        //         ss.QuerySphere(transform.Position, 10, ref t, SpatialQueryMode.Pickup);
        //         Debug.DrawLine(transform.Position + new float3(0, 2, 0), refs[0] + new float3(0, 2, 0), Color.red);
        //         refs.Dispose();
        //     }
        // }
        
        public struct TestAction : ISpatialQueryEntity 
        {
            public NativeArray<float3> refs;
            public float3 cp;

            public SpatialQueryMode SetMode() => 0;
            public void SpatialQueryAction(Entity otherEntity, CharacterSpatialDataAspect characterSpatialDataAspect)
            {
                float3 otherPos = characterSpatialDataAspect.Transforms.ValueRO.Position;
                if (refs[1].x > math.distance(otherPos, cp) &&
                    math.distance(otherPos, cp) > 0.01f)
                {
                    refs[0] = otherPos;
                    var @ref = refs[1];
                    @ref.x = math.distance(otherPos, cp);
                    refs[1] = @ref;
                }
            }

            public void SpatialQueryAction(Entity otherEntity, PickupSpatialDataAspect pickupSpatialDataAspect)
            {
                float3 otherPos = pickupSpatialDataAspect.Transform.ValueRO.Position;
                if (refs[1].x > math.distance(otherPos, cp) &&
                    math.distance(otherPos, cp) > 0.01f)
                {
                    refs[0] = otherPos;
                    var @ref = refs[1];
                    @ref.x = math.distance(otherPos, cp);
                    refs[1] = @ref;
                }
            }
        }
    }
}