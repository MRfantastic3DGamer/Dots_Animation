using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AI_Objective
{
    public class AIObjectiveBrainUserAuthoring : MonoBehaviour
    {
        public class Baker : Baker<AIObjectiveBrainUserAuthoring>
        {
            public override void Bake(AIObjectiveBrainUserAuthoring authoring)
            {
                authoring.Bake(this, new AIBrainUser(), authoring.gameObject);
            }
        }

        public void Bake<T>(Baker<T> baker, AIBrainUser brainUser, GameObject gameObject) where T : MonoBehaviour
        {
            Entity entity = baker.GetEntity(gameObject, TransformUsageFlags.None);
            baker.AddComponent(entity, brainUser);
            baker.AddComponent<AIBrainUserTask>(entity);
            baker.AddComponent<AIInput>(entity);
            baker.AddComponent<AISpatialData>(entity);
            baker.AddComponent<AIInteractionSignalData>(entity);
        }
    }

    public struct AIInput : IComponentData
    {
        public float2 MoveInput;
        public float LookInput;
        public float moveSpeed;
        public float rotationSpeed;
    }
    
    public struct AIBrainUser : IComponentData
    {
        
    }

    public struct AISpatialData : IComponentData
    {
        public int selectAgentIndex;
        public int selectInteractableIndex;
    }
    
    public struct AIInteractionSignalData : IComponentData
    {
        public AIInteractionSignal signal;
    }
    
    public struct AIBrainUserTask : IComponentData
    {
        public TaskType_e taskTypeE;
        public float3 destination;
    }

    public enum TaskType_e
    {
        Stand,
        Walk,
        Fight,
        Defend,
    }
    
    public enum AIInteractionSignal
    {
        Help,
        Threat,
        Stop,
    }
}