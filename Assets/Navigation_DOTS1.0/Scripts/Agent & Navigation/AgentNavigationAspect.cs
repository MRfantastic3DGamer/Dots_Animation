using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Experimental.AI;

namespace Navigation_DOTS1._0.Scripts.Agent___Navigation
{
    public readonly partial struct AgentNavigationAspect : IAspect
    {
        public readonly RefRW<Agent> agent;
        public readonly RefRW<AgentMovement> agentMovement;
        public readonly RefRW<AgentDestination> agentDestination;
        public readonly DynamicBuffer<WaypointBuffer> agentBuffer;
        public readonly DynamicBuffer<AgentPathValidityBuffer> agentPathValidityBuffer;
        public readonly RefRW<LocalTransform> trans;

        [BurstDiscard]
        public void MoveAgent(float deltaTime, float minDistanceReached, float agentSpeed, float agentRotationSpeed, float agentAvoidanceAngle, bool agentAvoidanceAngleBigger)
        {
            if(!agent.ValueRO.canFindPath || agentBuffer.Length <= 0 || agentMovement.ValueRO.reached || agentMovement.ValueRO.neighbourReached)
            {
                agentMovement.ValueRW.timeSinceStanding += deltaTime;
                return;
            }
            
            agentMovement.ValueRW.timeSinceStanding = 0;
            
            agentMovement.ValueRW.waypointDirection =
                math.normalize(agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint - trans.ValueRO.Position);


            float3 movDir = math.normalize(agentMovement.ValueRO.waypointDirection +
                                           agentMovement.ValueRO.avoidanceForce * agent.ValueRO.avoidanceUrge);
            agentMovement.ValueRW.currentMoveDirection =
                math.lerp(agentMovement.ValueRO.currentMoveDirection, movDir, 0.35f);
            if (!float.IsNaN(movDir.x))
            {
                if (math.dot(agentMovement.ValueRW.waypointDirection, agentMovement.ValueRO.avoidanceForce) > agentAvoidanceAngle
                    * (agentAvoidanceAngleBigger ? 1 : -1))
                {
                    trans.ValueRW.Position += movDir * agentSpeed * deltaTime;
                }
                trans.ValueRW.Rotation = math.slerp(
                    trans.ValueRW.Rotation, 
                    quaternion.LookRotation(agentMovement.ValueRW.currentMoveDirection, math.up()), 
                    deltaTime * agentRotationSpeed);
                if (math.distance(trans.ValueRO.Position, agentDestination.ValueRO.destination) <= minDistanceReached)
                {
                    agentMovement.ValueRW.reached = true;
                }
                else if (agentMovement.ValueRO.currentBufferIndex >= 0 && math.distance(trans.ValueRO.Position, agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint) <= minDistanceReached)
                {
                    agentMovement.ValueRW.currentBufferIndex += 1;
                }
            }
            else if (!agentMovement.ValueRO.reached)
            {
                agentMovement.ValueRW.currentBufferIndex += 1;
            }
        }
    }

    [BurstCompile]
    public struct PathValidityJob : IJob
    {
        public NavMeshQuery query;
        public float3 extents;
        public int currentBufferIndex;
        public LocalTransform trans;
        public float unitsInDirection;
        [NativeDisableContainerSafetyRestriction] public DynamicBuffer<WaypointBuffer> wps;
        [NativeDisableContainerSafetyRestriction] public DynamicBuffer<AgentPathValidityBuffer> apvb;
    
    
        NavMeshLocation startLocation;
        UnityEngine.AI.NavMeshHit navMeshHit;
        PathQueryStatus status;
    
    
        public void Execute()
        {
            if (currentBufferIndex < wps.Length)
            {
                if (!query.IsValid(query.MapLocation(wps.ElementAt(currentBufferIndex).wayPoint, extents, 0)))
                {
                    apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
                }
                else
                {
                    startLocation = query.MapLocation(trans.Position + (trans.Forward() * unitsInDirection), extents, 0);
                    status = query.Raycast(out navMeshHit, startLocation, wps.ElementAt(currentBufferIndex).wayPoint);
                
                    if (status == PathQueryStatus.Success)
                    {
                        if ((math.ceil(navMeshHit.position).x != math.ceil(wps.ElementAt(currentBufferIndex).wayPoint.x)) &&
                            (math.ceil(navMeshHit.position).z != math.ceil(wps.ElementAt(currentBufferIndex).wayPoint.z)))
                        {
                            apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
                        }
                    }
                    else
                    {
                        apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct NavigateJob : IJob
    {
        public NavMeshQuery query;
        [NativeDisableContainerSafetyRestriction] public DynamicBuffer<WaypointBuffer> ab;
        public float3 fromLocation;
        public float3 toLocation;
        public float3 extents;
        public int maxIteration;
        public int maxPathSize;
        NavMeshLocation nml_FromLocation;
        NavMeshLocation nml_ToLocation;
        PathQueryStatus status;
        PathQueryStatus returningStatus;

        public void Execute()
        {
            nml_FromLocation = query.MapLocation(fromLocation, extents, 0);
            nml_ToLocation = query.MapLocation(toLocation, extents, 0);
        
            if (!query.IsValid(nml_FromLocation) || !query.IsValid(nml_ToLocation)) return;
        
            status = query.BeginFindPath(nml_FromLocation, nml_ToLocation, -1);
            if (status != PathQueryStatus.InProgress) return;
        
            status = query.UpdateFindPath(maxIteration, out int iterationPerformed);
            if (status != PathQueryStatus.Success) return;
        
            status = query.EndFindPath(out int pathSize);
            NativeArray<NavMeshLocation> straightPath = new NativeArray<NavMeshLocation>(pathSize, Allocator.Temp);
            NativeArray<StraightPathFlags> straightPathFlags = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
            NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
            NativeArray<PolygonId> path = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
            int straightPathCount = 0;
            int a = query.GetPathResult(path);
            returningStatus = PathUtils.FindStraightPath(
                query,
                fromLocation,
                toLocation,
                path,
                pathSize,
                ref straightPath,
                ref straightPathFlags,
                ref vertexSide,
                ref straightPathCount,
                maxPathSize
            );
            if (returningStatus == PathQueryStatus.Success)
            {
                for (int i = 0; i < straightPathCount; i++)
                {
                    if (!(math.distance(fromLocation, straightPath[i].position) < 1) && query.IsValid(query.MapLocation(straightPath[i].position, extents, 0)))
                    {
                        ab.Add(new WaypointBuffer { wayPoint = new float3(straightPath[i].position.x, fromLocation.y, straightPath[i].position.z) });
                    }
                }
            }
            straightPath.Dispose();
            straightPathFlags.Dispose();
            path.Dispose();
            vertexSide.Dispose();
        }
    }
}