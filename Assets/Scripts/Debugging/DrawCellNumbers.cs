using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Debugging
{
    public class DrawCellNumbers : MonoBehaviour
    {
        public bool draw = true;
        private void OnDrawGizmos()
        {
            if(!draw) return;

            for (int i = -50; i < 50; i++)
            {
                for (int j = -50; j < 50; j++)
                {
                    Handles.Label(new Vector3(i*5, 2, j*5),GetCellKey(new float3(i*5, 2, j*5),5, new int3(100,100,100)).ToString());
                }
            }
            
        }
        public static int GetCellKey(float3 pos, float cellSize, int3 cells)
        {
            pos.x += cellSize * cells.x / 2;
            pos.y += cellSize * cells.y / 2;
            pos.z += cellSize * cells.z / 2;
            return (int) (math.floor(pos.z / cellSize) + cells.z * math.floor(pos.x / cellSize));
            //+ cells.z * cells.x * math.floor(p.y / cellSize));
        }
    }
}