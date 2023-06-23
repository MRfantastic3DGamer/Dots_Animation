using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Cell_Division
{
    public class CellDividerAuthoring : MonoBehaviour
    {
        public float cellSize = 10;
        public int agentCapacity = 50;
        public int pickupsCapacity = 50;
        public bool gizmos = true;
        
        public class Baker : Baker<CellDividerAuthoring>
        {
            public override void Bake(CellDividerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpatialPartitioningSettings
                {
                    AgentCapacity = authoring.agentCapacity,
                    CellSize = authoring.cellSize,
                    PickupsCapacity = authoring.pickupsCapacity,
                });
            }
        }

        private void OnDrawGizmos()
        {
            if(!gizmos) return;

            Gizmos.DrawWireCube(transform.position, Vector3.one * cellSize);
        }
    }

    public struct SpatialPartitioningSettings : IComponentData
    {
        public int AgentCapacity;
        public int PickupsCapacity;
        public float3 CellSize;
    }
}
