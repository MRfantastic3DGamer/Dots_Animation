using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spatial_Reasoning
{
    public class SpatialReasoningUserAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SpatialReasoningUserAuthoring>
        {
            public override void Bake(SpatialReasoningUserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SpatialReasoningRequest>(entity);
                AddComponent<SpatialReasoningRequestIndex>(entity);
                AddComponent<SpatialReasoningResult>(entity);
            }
        }
    }
    
    public struct SpatialReasoningRequest : IComponentData
    {
        public SpatialReasoningRequestType_e type;
        
        public float3 targetPosition;
        public float3 targetRotation;
        public float3 targetVelocity;
        
        public float3 targetDestinationPosition;
        public float3 targetDestinationRotation;

        public float maintainedDistance;
    }

    public struct SpatialReasoningRequestIndex : IComponentData
    {
        public int index;
    }

    public struct SpatialReasoningResult : IComponentData
    {
        public float3 position;
        public float3 direction;
    }

    public enum SpatialReasoningRequestType_e
    {
        Surround,
        Push,
        Pull,
        Direct,
        Defend,
    }
}