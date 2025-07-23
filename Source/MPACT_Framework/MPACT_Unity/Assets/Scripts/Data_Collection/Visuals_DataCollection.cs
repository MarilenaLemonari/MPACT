using UnityEngine;

public class Visuals_DataCollection : MonoBehaviour
{
    private Renderer m_body;
    private modCCPAgent m_agent;
    private Color m_color;
    [SerializeField] private bool m_isDataCollection;

    // Start is called before the first frame update
    void Start()
    {
        m_agent = GetComponent<modCCPAgent>();
        m_body = transform.Find("Body").GetComponent<Renderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_isDataCollection)
        {
            float goal = m_agent.m_goalWeight;
            float interconn = m_agent.m_interconnWeight * 0.5f + 0.5f;
            float group = m_agent.m_groupWeight;
            float interact = m_agent.m_interactionWeight;
            m_color = new Color(goal, interact, group, interconn);
        }
        else
        {
            float goal = m_agent.m_manager.goalWeight;
            float interconn = m_agent.m_manager.interconnWeight * 0.5f + 0.5f;
            float group = m_agent.m_manager.groupWeight;
            float interact = m_agent.m_manager.interWeight;   
            m_color = new Color(goal, interact, group, interconn);
        }
        m_body.material.color = m_color;
    }
}
