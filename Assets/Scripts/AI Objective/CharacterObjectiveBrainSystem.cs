using AI_Objective;
using Refference;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NPCs
{
    [BurstCompile]
    public partial struct CharacterObjectiveBrainSystem : ISystem
    {
        private EntityQuery brainUsers;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<AIBrainUser, AIBrainUserTask>().Build());
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<MirroredPlayerTransform>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            brainUsers = SystemAPI.QueryBuilder().WithAll<AIBrainUser, AIBrainUserTask>().Build();

            float3 playerPos = SystemAPI.GetSingleton<MirroredPlayerTransform>().Position;

            var getObjective = new GetObjectiveJob
            {
                playerPos = playerPos,
            };
            var getObjectiveHandle = getObjective.ScheduleParallel(brainUsers, state.Dependency);
            getObjectiveHandle.Complete();
        }

        [BurstCompile]
        partial struct GetObjectiveJob : IJobEntity
        {
            public float3 playerPos;
            public void Execute(ref AIBrainUserTask task, in AIBrainUser brainUser)
            {
                task.destination = playerPos;
                task.taskTypeE = TaskType_e.Fight;
            }
        }
    }
}