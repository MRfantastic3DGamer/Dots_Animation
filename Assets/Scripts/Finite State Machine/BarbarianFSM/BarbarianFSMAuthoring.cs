using Unity.Entities;
using UnityEngine;

namespace Finite_State_Machine.BarbarianFSM
{
    public class BarbarianFSMAuthoring : MonoBehaviour
    {
        public class Baker : Baker<BarbarianFSMAuthoring>
        {
            public override void Bake(BarbarianFSMAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BarbarianStateData
                {
                    timeSinceStateChange = 0,
                    currentState = BarbarianStates_e.Stand,
                });
            }
        }
    }
    
    public struct BarbarianStateData : IComponentData
    {
        public float timeSinceStateChange;
        public BarbarianStates_e currentState;
    }

    public enum BarbarianStates_e
    {
        Stand,
        Run,
        Attack,
    }
}