using Unity.Entities;
using UnityEngine;

namespace Pick_ups
{
    public class PickUpAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PickUpAuthoring>
        {
            public override void Bake(PickUpAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PickupData());
            }
        }
    }

    public struct PickupData : IComponentData
    {
        public float rarity;
    }
    
}