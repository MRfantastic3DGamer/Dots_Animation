using System;
using NPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Navigation_DOTS1._0.MyScripts
{

    #region Authoring

    public class FollowersSpawnerAuthoring : MonoBehaviour
    {
        public int2 spawnGrid;
        public float distance;
        public class FollowersSpawnerBaker : Baker<FollowersSpawnerAuthoring>
        {
            public override void Bake(FollowersSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FollowersSpawnerData()
                {
                    spawnGrid = authoring.spawnGrid,
                    distance = authoring.distance,
                });
            }
        }

        private void OnDrawGizmos()
        {
            for (int i = -spawnGrid.x/2; i < spawnGrid.x/2; i++)
            {
                for (int j = -spawnGrid.y/2; j < spawnGrid.y/2; j++)
                {
                    Gizmos.DrawSphere(new Vector3(transform.position.x + i * distance, transform.position.y,
                        transform.position.z + j * distance), 0.5f);
                }
            }
        }
    }

    #endregion

    public struct FollowersSpawnerData : IComponentData
    {
        public int2 spawnGrid;
        public float distance;
    }
    
    
}
