using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct NavigationGlobalProperties : IComponentData
{
    public int maxIteration;
    public int maxPathSize;
    public int maxPathNodePoolSize;
    public float3 extents;
    public bool dynamicPathFinding;
    public float minimumDistanceToWaypoint;
    public bool agentMovementEnabled;
    public float3 units;
    public bool setGlobalRelativeLocation;
    public float dynamicPathRecalculatingFrequency;
    public float unitsInForwardDirection;
    public float agentSpeed;
    public float rotationSpeed;
    public int maxCellCapacity;
    public float cellSize;
    public int3 cells;
    public float agentAvoidance;
    public float agentAvoidanceAngle;
    public bool agentAvoidanceAngleBigger;
    public float AgentToAgentInteractionRadios;

    public Entity CellBlockPrefab;
    
}

public class NavigationGlobalProperties_Authoring : MonoBehaviour
{
    [Header("Navigation Global Properties")]
    public int maxIteration;
    public int maxPathSize;
    public int maxPathNodePoolSize;
    public float3 extents;
    public bool dynamicPathFinding;
    public float dynamicPathRecalculatingFrequency;
    public float unitsInForwardDirection;
    public float cellSize;
    public int3 cells;
    public int maxCellCapacity;
    public GameObject CellBlockPrefab;

    public float AgentToAgentInteractionRadios;

    [Header("Agent")]
    public bool setGlobalRelativeLocation;
    public float3 units;

    [Header("Agent Movement")]
    public bool agentMovementEnabled;
    public float minimumDistanceToWaypoint;
    public float agentSpeed;
    public float rotationSpeed;
    public float agentAvoidance;
    public bool agentAvoidanceAngleBigger;
    [Range(-1f,1f)]
    public float agentAvoidanceAngle;
}

public class NavigationGlobalPropertiesBaker : Baker<NavigationGlobalProperties_Authoring>
{
    public override void Bake(NavigationGlobalProperties_Authoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new NavigationGlobalProperties
        {
            maxIteration = authoring.maxIteration,
            maxPathSize = authoring.maxPathSize,
            maxPathNodePoolSize = authoring.maxPathNodePoolSize,
            extents = authoring.extents,
            dynamicPathFinding = authoring.dynamicPathFinding,
            minimumDistanceToWaypoint = authoring.minimumDistanceToWaypoint,
            agentMovementEnabled = authoring.agentMovementEnabled,
            units = authoring.units,
            setGlobalRelativeLocation= authoring.setGlobalRelativeLocation,
            unitsInForwardDirection= authoring.unitsInForwardDirection,
            dynamicPathRecalculatingFrequency = authoring.dynamicPathRecalculatingFrequency,
            agentSpeed = authoring.agentSpeed,
            rotationSpeed = authoring.rotationSpeed,
            cellSize = authoring.cellSize,
            cells = authoring.cells,
            agentAvoidance = authoring.agentAvoidance,
            maxCellCapacity = authoring.maxCellCapacity,
            agentAvoidanceAngleBigger = authoring.agentAvoidanceAngleBigger,
            agentAvoidanceAngle = authoring.agentAvoidanceAngle,
            AgentToAgentInteractionRadios = authoring.AgentToAgentInteractionRadios,
            CellBlockPrefab = GetEntity(authoring.CellBlockPrefab, TransformUsageFlags.Dynamic),
        });
    }
}
