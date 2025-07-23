using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DraggablePoint : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private float m_value;
    private Vector2 origin;
    private RectTransform rectTransform;
    [SerializeField] private float angle = 0f;
    private float scale = 1.6f;
    [SerializeField] private TriangleUI triangleUI;
    private float minDistance;
    private float maxDistance;
    private Vector2 direction;
    [SerializeField] private BehaviorProfile.Type m_type;
    [SerializeField] private DraggablePoint m_pointA;
    [SerializeField] private DraggablePoint m_pointB;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        origin = rectTransform.position;
        minDistance = 25f * scale;
        maxDistance = ((RectTransform)transform.parent).sizeDelta.x * scale;
        direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        m_value = 0.333f;
        SetPosition();
    }

    public float Value
    {
        get
        {
            if (Application.isPlaying == false)
                return 1f;
            float currentDistance = Vector2.Distance(origin, rectTransform.position);
            return Math.Clamp((currentDistance - minDistance) / (maxDistance - minDistance), 0f, 1f);
        }
        set
        {
            m_value = value;
            SetPosition();
        }
    }
    
    public void SetActivty(bool value)
    {
        GetComponent<Image>().enabled = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        float distance = Vector2.Dot(eventData.position - origin, direction);
        distance = Mathf.Clamp(distance, minDistance - 0.01f, maxDistance + 0.01f); 
        rectTransform.position = origin + direction * distance;

        triangleUI.UpdateTriangle();
        m_value = this.Value;
        AdjustOtherPoints();
        ProfileManager.Instance.SetCurrentProfilePerType(m_type, m_value);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
    
    private void SetPosition()
    {
        float distance = minDistance;
        if (m_value > 0.01f)
        {
            float range = maxDistance - minDistance;
            float ratio = 1f / (m_value);
            distance = minDistance + (range / ratio);
        }

        rectTransform.position = origin + direction * distance;
        ProfileManager.Instance.SetCurrentProfilePerType(m_type, m_value);
    }
    
    private void AdjustOtherPoints()
    {
        float remainingValue = 1 - m_value;
        
        if (m_pointA.Value <= 0.01f && m_pointB.Value <= 0.01f)
        {
            m_pointA.Value = remainingValue / 2;
            m_pointB.Value = remainingValue / 2;
            return;
        }

        float totalValueOfOtherPoints = m_pointA.Value + m_pointB.Value;
        float weightA = m_pointA.Value / totalValueOfOtherPoints;
        float weightB = m_pointB.Value / totalValueOfOtherPoints;

        m_pointA.Value = weightA * remainingValue;
        m_pointB.Value = weightB * remainingValue;
    }
}
