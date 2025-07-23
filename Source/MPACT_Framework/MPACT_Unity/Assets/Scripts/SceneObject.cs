using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RVO;

public class SceneObject : MonoBehaviour
{
    private Vector3 m_scale;
    private bool m_colliding;
    private BoxCollider m_box;

    private void Awake()
    {
        m_box = GetComponent<BoxCollider>();
    }

    public Vector3 Scale
    {
        get { return m_scale; }
        set
        {
            m_scale = value;
            transform.localScale = m_scale;
            CheckCollisionsAfterScale();
            if(IsColliding)
                transform.localScale = new Vector3(1.5f, 3f, 1.5f);
        }
    }

    public bool IsColliding
    {
        get { return m_colliding; }
        private set { m_colliding = value; }
    }

    private void CheckCollisionsAfterScale()
    {
        Collider[] hitColliders = Physics.OverlapBox(transform.position, m_box.bounds.extents * 1.2f, transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("ObstacleCollider"))
            {
                IsColliding = true;
                return;
            }
        }
        IsColliding = false;
    }
    
    public void UpdateObstacleRVO()
    {
        MeshFilter box = GetComponent<MeshFilter>();
        if(box.transform.position.y < -10)
            return;
            
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
        Simulator.Instance.processObstacles();
    }
}
