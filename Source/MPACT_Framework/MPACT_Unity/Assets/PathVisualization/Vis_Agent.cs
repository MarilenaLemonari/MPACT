using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vis_Agent : MonoBehaviour
{
    private List<Vector3> m_points;
    [SerializeField] private float m_start;
    [SerializeField] private float m_end;
    private int m_index = 1;
    private TrajectoryVisualizer m_manager;
    private Renderer m_renderer;
    private TrailRenderer m_trail;
    private Vector3 m_lastPoint;

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_trail = transform.GetChild(0).GetComponent<TrailRenderer>();
        m_renderer.enabled = false;
    }

    public void InitializeAgentData(float start, float end, List<Vector3> points, TrajectoryVisualizer manager)
    {
        m_points = points;
        m_start = start;
        m_end = end;
        m_manager = manager;
        m_lastPoint = m_points[0];
    }

    private void FixedUpdate()
    {
        if(m_manager.globalTimer < m_start)
            return;
        if (m_index >= m_points.Count || m_manager.globalTimer > m_end)
        {
            Destroy(this.gameObject);
            return;
        }

        transform.position = m_points[m_index];
        Vector3 direction = (m_points[m_index] - m_lastPoint);
        if(direction != Vector3.zero)
            transform.forward = direction;
        m_lastPoint = m_points[m_index];
        
        Color c = m_manager.CalculateDirectionColor(direction.normalized);
        m_renderer.material.color = c;
        m_trail.startColor = c;
        m_trail.endColor = c;
        if (!m_renderer.enabled)
            m_renderer.enabled = true;

        m_index += 1;
    }
}
