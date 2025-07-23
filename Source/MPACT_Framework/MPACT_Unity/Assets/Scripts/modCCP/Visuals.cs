using UnityEngine;

public class Visuals : MonoBehaviour
{
    private Renderer m_body;
    private modCCPAgent_Framework m_agent;
    private TrailRenderer m_trail;
    [SerializeField] private bool m_randomColors;
    private Color m_color;

    // Start is called before the first frame update
    void Start()
    {
        m_agent = GetComponent<modCCPAgent_Framework>();
        m_body = transform.Find("Body").GetComponent<Renderer>();
        m_trail = GetComponentInChildren<TrailRenderer>();
        
        if (m_randomColors)
        {
            m_color = RandomColor();
            m_body.material.color = m_color;
            m_trail.startColor = m_color;
            m_trail.endColor = m_color;
        }
    }
    
    private Color RandomColor()
    {
        Color color;
        float H, S, V;

        do 
        {
            // Generate a fully random color
            color = Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f);

            // Convert the color to HSV
            Color.RGBToHSV(color, out H, out S, out V);
        
            // Repeat until the color is neither too white nor too gray
        } while (V > 0.8f || S < 0.5f);

        return color;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!m_randomColors)
        {

            float goal = m_agent.m_goalWeight;
            float interconn = EnvironmentSetup_modCCP.Instance.NormalizeValue(m_agent.m_interconnWeight, 0f, 1f, 0.3f, 0.7f);
            float group = m_agent.m_groupWeight;
            float interact = m_agent.m_interactionWeight;
            m_color = new Color(goal, interact, group, interconn);
            m_trail.startColor = m_color;
            m_trail.endColor = m_color;
            m_body.material.color = m_color;
        }
    }
}
