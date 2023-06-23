using AI_Objective;
using Unity.Entities;
using Unity.Transforms;

namespace AI_Character
{
    public readonly partial struct AICharacterAspect : IAspect
    {
        public readonly RefRO<AIBrainUserTask> aiBrainUserTask;
        public readonly RefRO<AIBrainUser> aiBrainUser;
        public readonly RefRW<AIInput> aiInput;
        public readonly RefRO<LocalTransform> transform;
    }
}