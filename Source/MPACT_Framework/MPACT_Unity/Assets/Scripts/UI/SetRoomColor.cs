using UnityEngine;
using UnityEngine.UI;

public class SetEdgeColors : MonoBehaviour
{
    private Image m_image;
    private Material m_material;

    [SerializeField] private Color topColor;
    [SerializeField] private Color bottomColor;
    [SerializeField] private Color rightColor;
    [SerializeField] private Color leftColor;
    [SerializeField] private Color centerColor;

    private void Awake()
    {
        m_image = GetComponentInChildren<Image>();
        m_material = new Material(m_image.material);
        m_image.material = m_material;
    }

    public void UpdateEdgeColors( BehaviorProfile center_or, BehaviorProfile top_or, BehaviorProfile bottom_or,  BehaviorProfile right_or, BehaviorProfile left_or)
    {
        if (m_material == null) return;

        BehaviorProfile top = new BehaviorProfile(top_or);
        BehaviorProfile bottom = new BehaviorProfile(bottom_or);
        BehaviorProfile right = new BehaviorProfile(right_or);
        BehaviorProfile left = new BehaviorProfile(left_or);
        BehaviorProfile center = new BehaviorProfile(center_or);
        top.connection = EnvironmentSetup_modCCP.Instance.NormalizeValue(top_or.connection, 0f, 1f, 0.3f, 0.7f);
        bottom.connection = EnvironmentSetup_modCCP.Instance.NormalizeValue(bottom_or.connection, 0f, 1f, 0.3f, 0.7f);
        right.connection = EnvironmentSetup_modCCP.Instance.NormalizeValue(right_or.connection, 0f, 1f, 0.3f, 0.7f);
        left.connection = EnvironmentSetup_modCCP.Instance.NormalizeValue(left_or.connection, 0f, 1f, 0.3f, 0.7f);
        center.connection = EnvironmentSetup_modCCP.Instance.NormalizeValue(center_or.connection, 0f, 1f, 0.3f, 0.7f);
        
        topColor = new Color(top.goal, top.interaction, top.group, top.connection);
        bottomColor = new Color(bottom.goal, bottom.interaction, bottom.group, bottom.connection);
        rightColor = new Color(right.goal, right.interaction, right.group, right.connection);
        leftColor = new Color(left.goal, left.interaction, left.group, left.connection);
        centerColor = new Color(center.goal, center.interaction, center.group, center.connection);

        m_material.SetColor("_ColorTop", topColor);
        m_material.SetColor("_ColorBottom", bottomColor);
        m_material.SetColor("_ColorRight", rightColor);
        m_material.SetColor("_ColorLeft", leftColor);
        m_material.SetColor("_ColorCenter", centerColor);
    }
}