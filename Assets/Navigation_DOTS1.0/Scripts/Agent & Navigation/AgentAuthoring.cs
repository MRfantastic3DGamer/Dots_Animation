using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Navigation_DOTS1._0.Scripts.Agent___Navigation
{
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 wayPoint;
    }

    public struct AgentPathValidityBuffer : IBufferElementData
    {
        public bool isPathInvalid;
    }

    [Serializable]
    public struct Agent : IComponentData
    {
        public int ID;
        public float3 toLocation;
        public bool usingGlobalRelativeLoction;
        public float elapsedSinceLastPathCalculation;
        public int pathFindingQueryIndex;
        public bool pathFindingQueryDisposed;
        public float timeStamp;
        public bool collided;
        public bool canFindPath;
        public bool CanAvoideAgents;
        public bool pathCalculated;
        public int cellSpace;
        public int currentCellKey;
        public int priority;
        public double stopDistance;
        public float avoidanceUrge;
        public float followUrge;
    }

    public struct AgentMovement : IComponentData
    {
        public int currentBufferIndex;
        public bool reached;
        public float timeSinceStanding;
        public float3 waypointDirection;
        public float3 avoidanceForce;
        public float3 currentMoveDirection;
        public bool neighbourReached;
    }

    public struct AgentDestination : IComponentData
    {
        public float3 destination;
    }
    
    public class AgentAuthoring : MonoBehaviour { }

    public class AgentBaker : Baker<AgentAuthoring>
    {
        public override void Bake(AgentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Agent
            {
                canFindPath = true,
                CanAvoideAgents = true,
                stopDistance = 5f,
            });
            AddComponent(entity, new AgentMovement
            {
                currentBufferIndex = 0
            });
            AddComponent(entity, new AgentDestination());
            AddBuffer<WaypointBuffer>(entity);
            AddBuffer<AgentPathValidityBuffer>(entity);
        }
    }
}