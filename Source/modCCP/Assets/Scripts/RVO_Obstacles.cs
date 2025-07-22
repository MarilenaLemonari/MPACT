using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO_Obstacles : MonoBehaviour
{
    private void Awake()
    {
        MeshFilter[] boxes = GetComponentsInChildren<MeshFilter>();
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
}
