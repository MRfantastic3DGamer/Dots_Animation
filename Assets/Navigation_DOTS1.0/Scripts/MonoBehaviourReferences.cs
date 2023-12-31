using System;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

public class MoveObstacles : MonoBehaviour
{
    public GameObject[] obstacles;
    public float minSpeed;
    public float maxSpeed;
    private float[] directions; // array to store individual obstacle directions

    // Start is called before the first frame update
    void Start()
    {
        obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        directions = new float[obstacles.Length];

        // loop through each obstacle and assign a random direction
        for (int i = 0; i < obstacles.Length; i++)
        {
            directions[i] = Random.Range(-1.0f, 1.0f) > 0 ? 1.0f : -1.0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // loop through each obstacle and move it with its assigned speed and direction
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].transform.Translate(Vector3.right * Random.Range(minSpeed, maxSpeed) * Time.deltaTime * directions[i]);

            // check if the obstacle has reached its left or right limit
            if (obstacles[i].transform.position.x <= -15)
            {
                directions[i] = 1.0f;
            }
            else if (obstacles[i].transform.position.x >= 15)
            {
                directions[i] = -1.0f;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // var properties =
        //     World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
        //         ComponentType.ReadOnly(typeof(NavigationGlobalProperties)));
        //GUI.Box(new Rect(10, 10, 500, 40), "Cell Size : " + 20);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(10, 10, 10));
        //GUI.Box(new Rect(10, 52, 500, 40), "Collisions Per Second: " + collisions / 2); //2 units colliding will be regarded as 1 collision
    }
}
