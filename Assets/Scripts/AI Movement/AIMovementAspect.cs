using AI_Objective;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace AI_Movement
{
    /// <summary>
    /// contains - PhysicsVelocity, transform
    /// </summary>
    [BurstCompile]
    public readonly partial struct AIMovementAspect : IAspect
    {
        private readonly RefRW<LocalTransform> transform;
        private readonly RefRW<PhysicsVelocity> velocity;

        #region Helper Variables

        public float3 CurrentPosition => transform.ValueRO.Position;
        public quaternion CurrentRotation => transform.ValueRO.Rotation;
        public float3 Up => new float3(0, 1, 0);
        public float3 CharUp => transform.ValueRO.Up();
        public float3 CharForward => transform.ValueRO.Forward();
        public float3 CurrentVelocity => velocity.ValueRO.Linear;

        #endregion
        
        
        [BurstCompile]
        public void StandStill(float deltaTime)
        {
            SetVelocity(float3.zero);
            SetRotation(quaternion.LookRotation(CharForward, Up));
        }
        [BurstCompile]
        public void StandStill(float deltaTime, float3 lookDirection)
        {
            SetVelocity(float3.zero);
            SetRotation(quaternion.LookRotation(lookDirection, Up));
        }
        
        [BurstCompile]
        public void MoveToDestination(float deltaTime, float3 destination, float moveSpeed)
        {
            float3 direction = math.normalize(destination - CurrentPosition);
            Move(deltaTime, new float2(direction.x, direction.z), moveSpeed);
        }
        
        [BurstCompile]
        public void Move(float deltaTime, float2 MoveInput, float moveSpeed)
        {
            if(MoveInput.x == 0 && MoveInput.y == 0) return;
            var movDir = math.normalize(new float3(MoveInput.x, 0, MoveInput.y));
            
            SetVelocity(movDir * moveSpeed);
            SetRotation(quaternion.LookRotation(movDir, Up));
        }

        [BurstCompile]
        public void SetVelocity(float3 velocity)
        {
            this.velocity.ValueRW.Linear = velocity;
        }

        [BurstCompile]
        public void SetRotation(quaternion rotation)
        {
            transform.ValueRW.Rotation = rotation;
        }
    }
}