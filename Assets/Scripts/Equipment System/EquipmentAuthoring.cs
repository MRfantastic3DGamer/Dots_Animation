using Unity.Entities;
using UnityEngine;

namespace Weapons
{
    public class EquipmentAuthoring : MonoBehaviour
    {
        public class Baker : Baker<EquipmentAuthoring>
        {
            public override void Bake(EquipmentAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EquipmentStats());
                AddComponent(entity, new EquipmentEquippedStatus());
            }
        }
    }
    
    
    public struct EquipmentStats : IComponentData
    {
        public float power;
        
        // TODO : list of stackable powers ( fire, water, magic, etc )
    }

    public struct EquipmentEquippedStatus : IComponentData
    {
        public bool equipped;
        /// <summary>
        /// high if high rank char is holding it
        /// </summary>
        public float equippedStat;
    }
    
}