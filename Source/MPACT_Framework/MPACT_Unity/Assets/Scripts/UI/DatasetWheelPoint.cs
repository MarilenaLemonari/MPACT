using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DatasetWheelPoint : MonoBehaviour, IPointerClickHandler
{
    private float m_goalAngle = 330f;
    private float m_groupAngle = 210f;
    private float m_interactAngle = 90f;
    private Vector2 origin;
    private RectTransform rectTransform;
    private float scale = 1.6f;
    private BehaviorProfile m_profile;
    private Slider m_connectSlider;

    // Start is called before the first frame update
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetPosition(BehaviorProfile profile, Slider slider)
    {
        if(origin == Vector2.zero)
            origin = transform.parent.parent.Find("Circle").GetComponent<RectTransform>().position;
        if(rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        m_profile = profile;
        m_connectSlider = slider;
        
        float maxDistance = ((RectTransform)transform.parent).sizeDelta.x * scale;
        float minDistance = 25f * scale;
        float range = maxDistance - minDistance;
        
        float[] angles = { m_goalAngle, m_groupAngle, m_interactAngle };
        float[] values = { profile.goal, profile.group, profile.interaction };
        
        float[] radianAngles = Array.ConvertAll(angles, angle => angle * Mathf.Deg2Rad);
        
        Vector2 cartesianSum = Vector2.zero;
        float totalWeight = 0;

        for (int i = 0; i < 3; i++) {
            cartesianSum += new Vector2(Mathf.Cos(radianAngles[i]), Mathf.Sin(radianAngles[i])) * values[i];
            totalWeight += values[i];
        }
        
        cartesianSum /= totalWeight;
        
        float resultingAngle = Mathf.Atan2(cartesianSum.y, cartesianSum.x) * Mathf.Rad2Deg;
        if (resultingAngle < 0) resultingAngle += 360;  // Normalize angle to [0, 360]
        
        float maxDistanceContribution = values.Max() * range + minDistance;
        
        Vector2 direction = new Vector2(Mathf.Cos(resultingAngle * Mathf.Deg2Rad), Mathf.Sin(resultingAngle * Mathf.Deg2Rad));
        
        rectTransform.position = origin + direction * maxDistanceContribution;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PointClick();
    }
    
    private void PointClick()
    {
        // Your custom logic for when the point is clicked.
        m_connectSlider.value = m_profile.connection;
        ProfileManager.Instance.SetCurrentProfile(m_profile);
    }
    
    // Update is called once per frame
    void Update()
    {
    }
}
