using AI_Movement;
using NPCs;
using Unity.Burst;
using Unity.Entities;

namespace Finite_State_Machine.BarbarianFSM
{
    [UpdateAfter(typeof(CharacterObjectiveBrainSystem))]
    public partial struct BarbarianFSMSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }
        
        public void OnDestroy(ref SystemState state)
        { }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float time = (float) SystemAPI.Time.ElapsedTime;


            var setInputJob = new SetInputJob
            {
                deltaTime = deltaTime,
                time = time,
            };
            var setInputHandle = setInputJob.ScheduleParallel(state.Dependency);
            setInputHandle.Complete();
        }
        
        public partial struct SetInputJob : IJobEntity
        {
            public float deltaTime;
            public float time;
            private void Execute(BarbarianAspect barbarian)
            {
                barbarian.Tick(deltaTime);
                barbarian.Fight(time, deltaTime);
            }
        }
    }
}