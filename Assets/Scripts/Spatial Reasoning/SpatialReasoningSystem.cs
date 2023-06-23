using Unity.Collections;
using Unity.Entities;

namespace Spatial_Reasoning
{
    public partial struct SpatialReasoningSystem : ISystem
    {
        NativeList<SpatialReasoningRequest> Requests;
        NativeList<SpatialReasoningResult> Results;
        
        public void OnCreate(ref SystemState state)
        {
            Requests = new NativeList<SpatialReasoningRequest>();
            Results = new NativeList<SpatialReasoningResult>();
            state.EntityManager.AddComponentData(state.SystemHandle, new SpatialReasoningSingleton
            {
                Requests = Requests,
                Results = Results,
            });

            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<SpatialReasoningRequest, SpatialReasoningResult>().Build());
        }
        public void OnUpdate(ref SystemState state)
        {
            
        }
    }

    public struct SpatialReasoningSingleton : IComponentData
    {
        internal NativeList<SpatialReasoningRequest> Requests;
        internal NativeList<SpatialReasoningResult> Results;
        
    }
}