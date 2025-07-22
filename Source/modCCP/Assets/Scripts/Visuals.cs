using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Visuals : MonoBehaviour
{
    private Renderer m_body;
    private modCCPAgent m_agent;

    // Start is called before the first frame update
    void Start()
    {
        m_agent = GetComponent<modCCPAgent>();
        m_body = transform.Find("Body").GetComponent<Renderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Color color;
        float goal = m_agent.m_goalWeight;
        float group = m_agent.m_groupWeight;
        float interact = m_agent.m_interactionWeight;
        float interconn = m_agent.m_manager.Normalize(m_agent.m_interconnDistance, 0f, 1f, 0.1f, 1f);
        color = new Color(goal, interact, group, interconn);
        m_body.material.color = color;
    }
}
