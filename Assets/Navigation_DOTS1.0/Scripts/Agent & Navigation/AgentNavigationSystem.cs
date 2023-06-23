using Refference;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.PlayerLoop;

namespace Navigation_DOTS1._0.Scripts.Agent___Navigation
{
    public partial struct AgentNavigationSystem : ISystem, ISystemStartStop
    {
        public void OnStartRunning(ref SystemState state)
        {
            state.RequireForUpdate<Agent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<NavigationGlobalProperties> properties = SystemAPI.GetSingletonRW<NavigationGlobalProperties>();

            NativeParallelMultiHashMap<int, AgentInCell> cellVsEntityPositions = new NativeParallelMultiHashMap<int, AgentInCell>();
            NativeHashMap<int, int> cellHeld             = new NativeHashMap<int, int>();
            var agentQuery                               = SystemAPI.QueryBuilder().WithAll<Agent>().Build();
            float3 playerPos                             = SystemAPI.GetSingleton<MirroredPlayerTransform>().Position;
            NativeArray<JobHandle> pathFindingJobs       = new NativeArray<JobHandle>(agentQuery.CalculateEntityCount(), Allocator.Temp);
            NativeArray<NavMeshQuery> pathFindingQueries = new NativeArray<NavMeshQuery>(agentQuery.CalculateEntityCount(), Allocator.Temp);

            // Cells creation
            if (agentQuery.CalculateEntityCount() > cellVsEntityPositions.Capacity)
            {
                cellVsEntityPositions.Capacity = agentQuery.CalculateEntityCount();
            }
            JobHandle cellsCreationJob = new CellsCreationJob
            {
                cellVsEntityPositions = cellVsEntityPositions.AsParallelWriter(),
                cellHeld = cellHeld,
                cellSize = properties.ValueRO.cellSize,
                cells = properties.ValueRO.cells,
            }.Schedule(state.Dependency);
            cellsCreationJob.Complete();

            int i = 0;
            
            // Pathfinding
            foreach (AgentNavigationAspect ana in SystemAPI.Query<AgentNavigationAspect>())
            {
                if(!ana.agent.ValueRO.canFindPath) continue;
                if (properties.ValueRO.dynamicPathFinding && ana.agentPathValidityBuffer.Length > 0 && ana.agentPathValidityBuffer.ElementAt(0).isPathInvalid)
                {
                    ana.agentBuffer.Clear();
                    ana.agentMovement.ValueRW.currentBufferIndex = 0;
                    ana.agent.ValueRW.pathCalculated = false;
                    ana.agentPathValidityBuffer.Clear();
                }
                if (properties.ValueRO.agentMovementEnabled && ana.agent.ValueRW.usingGlobalRelativeLoction)
                {
                    ana.agent.ValueRW.toLocation = new float3(ana.agent.ValueRW.toLocation.x, ana.agent.ValueRW.toLocation.y, ana.agent.ValueRW.toLocation.z);
                    ana.agentBuffer.Clear();
                    ana.agentMovement.ValueRW.currentBufferIndex = 0;
                    ana.agent.ValueRW.pathCalculated = false;
                    ana.agentPathValidityBuffer.Clear();
                    ana.agentMovement.ValueRW.reached = false;
                }
                if (!ana.agent.ValueRO.pathCalculated || ana.agentBuffer.Length == 0)
                {
                    pathFindingQueries[i] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, properties.ValueRO.maxPathNodePoolSize);
                    ana.agent.ValueRW.pathFindingQueryIndex = i;
                    if (properties.ValueRO.setGlobalRelativeLocation && !ana.agent.ValueRO.usingGlobalRelativeLoction)
                    {
                        ana.agent.ValueRW.toLocation = ana.trans.ValueRO.Position + properties.ValueRO.units;
                        ana.agent.ValueRW.usingGlobalRelativeLoction = true;
                    }
                    pathFindingJobs[i] = new NavigateJob
                    {
                        query = pathFindingQueries[i],
                        ab = ana.agentBuffer,
                        fromLocation = ana.trans.ValueRO.Position,
                        toLocation = ana.agent.ValueRO.toLocation,
                        extents = properties.ValueRO.extents,
                        maxIteration = properties.ValueRO.maxIteration,
                        maxPathSize = properties.ValueRO.maxPathSize
                    }.Schedule(state.Dependency);
                    ana.agent.ValueRW.pathCalculated = true;
                    ana.agent.ValueRW.pathFindingQueryDisposed = false;
                }
                i++;
            }
            JobHandle.CompleteAll(pathFindingJobs);
            foreach (AgentNavigationAspect ana in SystemAPI.Query<AgentNavigationAspect>())
            {
                if (ana.agent.ValueRO.canFindPath && ana.agent.ValueRO.pathCalculated && !ana.agent.ValueRW.pathFindingQueryDisposed)
                {
                    pathFindingQueries[ana.agent.ValueRW.pathFindingQueryIndex].Dispose();
                    ana.agent.ValueRW.pathFindingQueryDisposed = true;
                }
            }
            pathFindingQueries.Dispose();
            
            // Agent Avoidance
            float deltaTime = SystemAPI.Time.DeltaTime;
            JobHandle agentAvoidanceHandler = new AgentAgentRelationJob
            {
                cellVsEntityPositions = cellVsEntityPositions,
                deltaTime = deltaTime,
                cellSize = properties.ValueRO.cellSize,
                cells = properties.ValueRO.cells,
                cameraPos = playerPos,
                AgentToAgentInteractionRadios = properties.ValueRO.AgentToAgentInteractionRadios,
            }.Schedule(state.Dependency);
            agentAvoidanceHandler.Complete();
            
            // move agent
            if (properties.ValueRO.agentMovementEnabled)
            {
                new MoveJob
                {
                    deltaTime = SystemAPI.Time.DeltaTime,
                    minDistance = properties.ValueRO.minimumDistanceToWaypoint,
                    agentSpeed = properties.ValueRO.agentSpeed,
                    agentRotationSpeed = properties.ValueRO.rotationSpeed,
                    avoidanceUrge = properties.ValueRO.agentAvoidance,
                    agentAvoidanceAngle = properties.ValueRO.agentAvoidanceAngle,
                    agentAvoidanceAngleBigger = properties.ValueRO.agentAvoidanceAngleBigger,
                }.ScheduleParallel();
            }
            
            
            
            // debug
            // foreach (KVPair<int,int> ch in cellHeld)
            // {
            //     int x = ((ch.Key) / 100) - 50, z = ((ch.Key) % 100) - 50;
            //     Color c = Color.Lerp(Color.green, Color.red, (float)ch.Value / properties.ValueRO.maxCellCapacity);
            //     Debug.DrawLine(new float3(x-0.25f,2,z-0.2f) * properties.ValueRO.cellSize, new float3(x+0.25f,2,z-0.2f) * properties.ValueRO.cellSize,c);
            //     Debug.DrawLine(new float3(x-0.25f,2,z-0.2f) * properties.ValueRO.cellSize, new float3(x-0.25f,2,z+0.2f) * properties.ValueRO.cellSize,c);
            //     Debug.DrawLine(new float3(x+0.25f,2,z+0.2f) * properties.ValueRO.cellSize, new float3(x+0.25f,2,z-0.2f) * properties.ValueRO.cellSize,c);
            //     Debug.DrawLine(new float3(x+0.25f,2,z+0.2f) * properties.ValueRO.cellSize, new float3(x-0.25f,2,z+0.2f) * properties.ValueRO.cellSize,c);
            // }
            
            cellVsEntityPositions.Clear();
            cellHeld.Clear();
        }

        public void OnStopRunning(ref SystemState state)
        {
            
        }

        public static int GetCellKey(float3 pos, float cellSize, int3 cells)
        {
            pos.x += cellSize * cells.x / 2;
            pos.y += cellSize * cells.y / 2;
            pos.z += cellSize * cells.z / 2;
            return (int) (math.floor(pos.z / cellSize) + cells.z * math.floor(pos.x / cellSize));
            //+ cells.z * cells.x * math.floor(p.y / cellSize));
        }

        public partial struct CellsCreationJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, AgentInCell>.ParallelWriter cellVsEntityPositions;
            public NativeHashMap<int, int> cellHeld;
            public float cellSize;
            public int3 cells;
            public void Execute(AgentNavigationAspect ana)
            {
                ana.agent.ValueRW.currentCellKey =
                    GetCellKey(ana.trans.ValueRO.Position, cellSize, cells);
                cellVsEntityPositions.Add(ana.agent.ValueRO.currentCellKey,new AgentInCell
                {
                    ID = ana.agent.ValueRO.ID,
                    pos = ana.trans.ValueRO.Position,
                    direction = ana.agentMovement.ValueRO.waypointDirection,
                    reached = ana.agentMovement.ValueRO.reached,
                    neighbourReached = ana.agentMovement.ValueRO.neighbourReached,
                    priority = ana.agent.ValueRO.priority,
                    timeSinceStanding = ana.agentMovement.ValueRO.timeSinceStanding,
                });
                if(cellHeld.ContainsKey(ana.agent.ValueRO.currentCellKey))
                    cellHeld[ana.agent.ValueRO.currentCellKey] += ana.agent.ValueRO.cellSpace;
                else cellHeld.Add(ana.agent.ValueRO.currentCellKey, ana.agent.ValueRO.cellSpace);
            }
        }

        [BurstCompile]
        public partial struct AgentAgentRelationJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, AgentInCell> cellVsEntityPositions;
            public float deltaTime;
            public float cellSize;
            public int3 cells;
            public float AgentToAgentInteractionRadios;
            public float3 cameraPos;
            
            [BurstCompile]
            public void Execute(AgentNavigationAspect ana)
            {
                // if(math.distance(cameraPos, ana.trans.ValueRO.Position) > AgentToAgentInteractionRadios) return;
                
                
                
                if (!ana.agent.ValueRO.CanAvoideAgents)
                {
                    ana.agentMovement.ValueRW.avoidanceForce = float3.zero;
                    return;
                }
                
                float3 pos = ana.trans.ValueRO.Position;
                NativeParallelMultiHashMapIterator<int> nmhKeyIterator;
                AgentInCell currentAgentToCheck;
                float currentDistance = 1.5f;
                int totalRepulsingAgents = 0;
                int totalAgentsOfSameID = 0;
                
                float3 totalAgentRepulsion = float3.zero;
                
                ana.agentMovement.ValueRW.avoidanceForce = float3.zero;
                
                int posKey = GetCellKey(pos, cellSize, cells);

                NativeArray<int> adjacentKeys = new NativeArray<int>(9, Allocator.Temp);
                adjacentKeys[0] = posKey;
                // adjacentKeys[1] = posKey + 1;
                // adjacentKeys[2] = posKey - 1;
                // adjacentKeys[3] = posKey + 100;
                // adjacentKeys[4] = posKey - 100;
                // adjacentKeys[5] = posKey + 101;
                // adjacentKeys[6] = posKey + 99;
                // adjacentKeys[7] = posKey - 101;
                // adjacentKeys[8] = posKey - 99;
                
                for (int d = 0; d < 1; d++)
                {
                    int key = adjacentKeys[d];
                    if(!cellVsEntityPositions.ContainsKey(key)) continue;
                    if (cellVsEntityPositions.TryGetFirstValue(key, out currentAgentToCheck, out nmhKeyIterator))
                    {
                        do
                        {
                            if (pos.Equals(currentAgentToCheck.pos)) continue;

                            float3 posToCheckPos = pos - currentAgentToCheck.pos;

                            if (math.dot(math.normalize(ana.agentMovement.ValueRO.waypointDirection),
                                    math.normalize(posToCheckPos)) > -0.1f)
                            {
                                continue;
                            }


                            if (currentAgentToCheck.reached || currentAgentToCheck.neighbourReached)
                            {
                                ana.agentMovement.ValueRW.avoidanceForce = float3.zero;
                                ana.agentMovement.ValueRW.neighbourReached = true;
                                Debug.DrawLine(ana.trans.ValueRO.Position +(float3) Vector3.up * 2 ,currentAgentToCheck.pos +(float3) Vector3.up * 2);
                                return;
                            }

                            if (currentDistance > math.sqrt(math.lengthsq(posToCheckPos)))
                            {
                                totalAgentRepulsion += posToCheckPos;
                                totalRepulsingAgents++;
                            }
                        } while (cellVsEntityPositions.TryGetNextValue(out currentAgentToCheck, ref nmhKeyIterator));
                        if (totalRepulsingAgents > 0)
                        {
                            ana.agentMovement.ValueRW.avoidanceForce = totalAgentRepulsion / totalRepulsingAgents;
                        }
                    }
                }

                if (math.distance(ana.trans.ValueRO.Position, ana.agentDestination.ValueRO.destination) >
                    ana.agent.ValueRO.stopDistance)
                {
                    ana.agentMovement.ValueRW.neighbourReached = true;
                }
                else
                {    
                    ana.agentMovement.ValueRW.reached = true;
                }
            }
        }
        
        [BurstCompile]
        public partial struct MoveJob : IJobEntity
        {
            public float deltaTime;
            public float minDistance;
            public float agentSpeed;
            public float agentRotationSpeed;
            public float avoidanceUrge;
            public float agentAvoidanceAngle;
            public bool agentAvoidanceAngleBigger;
            public void Execute(AgentNavigationAspect ana)
            {
                if(ana.agent.ValueRO.CanAvoideAgents)
                    ana.MoveAgent(deltaTime, minDistance, agentSpeed, agentRotationSpeed, agentAvoidanceAngle, agentAvoidanceAngleBigger);
            }
        }


        public struct AgentInCell
        {
            public int ID;
            public bool reached;
            public float3 pos;
            public float3 direction;
            public int priority;
            public float timeSinceStanding;
            public bool neighbourReached;
        }
    }
}