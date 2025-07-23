using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO_Manager : Singleton<RVO_Manager>
{
    [SerializeField] private Transform m_obstaclesParent;
    [SerializeField] private Transform m_interactionsParent;
    
    // Start is called before the first frame update
    void Awake()
    {
        // RVO Initialization
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(
            3.0f,
            8,
            3f,
            1f,
            0.35f,
            2.25f,
            new RVO.Vector2(0.0f, 0.0f));
        Simulator.Instance.SetNumWorkers(12);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Simulator.Instance.doStep();
    }

    public void RecreateObstacles(bool includeBounds)
    {
        MeshFilter[] boxes;
        if (m_obstaclesParent != null)
        {
            boxes = m_obstaclesParent.GetComponentsInChildren<MeshFilter>();
            foreach (var box in boxes)
            {
                // Get the local 3D vertices
                Vector3[] localVertices = box.mesh.vertices;

                // Convert the local 3D vertices to world 3D vertices
                Vector3[] worldVertices = new Vector3[localVertices.Length];
                for (int i = 0; i < localVertices.Length; i++)
                {
                    worldVertices[i] = box.transform.TransformPoint(localVertices[i]);
                }

                IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();
                // Convert the world 3D vertices to 2D vertices
                RVO.Vector2[] vertices2D = new RVO.Vector2[worldVertices.Length];
                for (int i = 0; i < worldVertices.Length; i++)
                {
                    vertices2D[i] = new RVO.Vector2(worldVertices[i].x, worldVertices[i].z);
                    obstacle.Add(vertices2D[i]);
                }

                Simulator.Instance.addObstacle(obstacle);
            }
        }

        if (m_interactionsParent != null)
        {
            boxes = m_interactionsParent.GetComponentsInChildren<MeshFilter>();
            foreach (var box in boxes)
            {
                if (box.transform.position.y < -10)
                    continue;

                // Get the local 3D vertices
                Vector3[] localVertices = box.mesh.vertices;

                // Convert the local 3D vertices to world 3D vertices
                Vector3[] worldVertices = new Vector3[localVertices.Length];
                for (int i = 0; i < localVertices.Length; i++)
                {
                    worldVertices[i] = box.transform.TransformPoint(localVertices[i]);
                }

                IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();
                // Convert the world 3D vertices to 2D vertices
                RVO.Vector2[] vertices2D = new RVO.Vector2[worldVertices.Length];
                for (int i = 0; i < worldVertices.Length; i++)
                {
                    vertices2D[i] = new RVO.Vector2(worldVertices[i].x, worldVertices[i].z);
                    obstacle.Add(vertices2D[i]);
                }

                Simulator.Instance.addObstacle(obstacle);
            }
        }

        if (includeBounds)
        {
            GameObject[] floors = GameObject.FindGameObjectsWithTag("Floor");
            foreach (GameObject floor in floors)
            {
                foreach (Transform obj in floor.transform)
                {
                    if (obj.gameObject.activeSelf && obj.GetComponent<BoxCollider>() != null)
                    {
                        MeshFilter box = obj.GetComponent<MeshFilter>();
                        // Get the local 3D vertices
                        Vector3[] localVertices = box.mesh.vertices;

                        // Convert the local 3D vertices to world 3D vertices
                        Vector3[] worldVertices = new Vector3[localVertices.Length];
                        for (int i = 0; i < localVertices.Length; i++)
                        {
                            worldVertices[i] = box.transform.TransformPoint(localVertices[i]);
                        }

                        IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();
                        // Convert the world 3D vertices to 2D vertices
                        RVO.Vector2[] vertices2D = new RVO.Vector2[worldVertices.Length];
                        for (int i = 0; i < worldVertices.Length; i++)
                        {
                            vertices2D[i] = new RVO.Vector2(worldVertices[i].x, worldVertices[i].z);
                            obstacle.Add(vertices2D[i]);
                        }

                        Simulator.Instance.addObstacle(obstacle);
                    }
                }
            }
        }
        
        Simulator.Instance.processObstacles();
    }
}
