using System;
using AI_Movement;
using AI_Objective;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Finite_State_Machine.BarbarianFSM
{
    [BurstCompile]
    public readonly partial struct BarbarianAspect : IAspect
    {
        private readonly RefRW<BarbarianStateData> state;
        private readonly RefRO<AIBrainUserTask> task;
        private readonly RefRW<AIMovementStats> aiMovementStats;
        private readonly AIMovementAspect movement;
        
        // public readonly RefRW<AIInput> aiInput;

        [BurstCompile]
        public void Tick(float deltaTime)
        {
            state.ValueRW.timeSinceStateChange += deltaTime;
        }

        [BurstCompile]
        public void ResetStateTime()
        {
            state.ValueRW.timeSinceStateChange = 0;
        }

        [BurstCompile]
        public void ChangeState(BarbarianStates_e state)
        {
            this.state.ValueRW.currentState = state;
            ResetStateTime();
        }
        
        [BurstCompile]
        public void Fight(float time, float deltaTime)
        {
            var dest = task.ValueRO.destination;
            switch (state.ValueRO.currentState)
            {
                case BarbarianStates_e.Stand:
                {
                    movement.StandStill(deltaTime);
                    ChangeState(BarbarianStates_e.Run);
                    movement.Move(deltaTime, float2.zero, 0);
                    break;
                }
                case BarbarianStates_e.Run:
                {
                    if (math.distance(movement.CurrentPosition, dest) < 2.5f)
                    {
                        ChangeState(BarbarianStates_e.Attack);
                    }
                    movement.MoveToDestination(deltaTime, dest, aiMovementStats.ValueRO.runSpeed);
                    break;
                }
                case BarbarianStates_e.Attack:
                {
                    movement.StandStill(deltaTime);
                    if (state.ValueRO.timeSinceStateChange > 2f)
                    {
                        ChangeState(BarbarianStates_e.Run);
                    }
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        [BurstCompile]
        public void Defend(float time, float deltaTime, float3 defend, float3 from, float distance)
        {
            float3 defendToFrom = math.normalize(from - defend);
            float3 dest = defend + defendToFrom * distance;
            
            switch (state.ValueRO.currentState)
            {
                case BarbarianStates_e.Stand:
                {
                    movement.StandStill(deltaTime);
                    state.ValueRW.currentState = BarbarianStates_e.Run;
                    ResetStateTime();
                    movement.Move(deltaTime, float2.zero, 0);
                    break;
                }
                case BarbarianStates_e.Run:
                {
                    if (math.distance(movement.CurrentPosition, dest) < 2.5f)
                    {
                        state.ValueRW.currentState = BarbarianStates_e.Attack;
                        ResetStateTime();
                    }
                    movement.MoveToDestination(deltaTime, dest, aiMovementStats.ValueRO.runSpeed);
                    break;
                }
                case BarbarianStates_e.Attack:
                {
                    movement.StandStill(deltaTime);
                    if (state.ValueRO.timeSinceStateChange > 2f)
                    {
                        state.ValueRW.currentState = BarbarianStates_e.Run;
                        ResetStateTime();
                    }
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}