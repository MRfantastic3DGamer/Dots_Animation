using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AI_Movement
{
    public class AIMovementUserAuthoring : MonoBehaviour
    {
        public AIMovementStats movementStats;
        public class Baker : Baker<AIMovementUserAuthoring>
        {
            public override void Bake(AIMovementUserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AIMovementStats>(entity, authoring.movementStats);
            }
        }
    }

    [Serializable]
    public struct AIMovementStats : IComponentData
    {
        public float walkSpeed;
        public float turnSpeed;

        public float runSpeed;
    }
}