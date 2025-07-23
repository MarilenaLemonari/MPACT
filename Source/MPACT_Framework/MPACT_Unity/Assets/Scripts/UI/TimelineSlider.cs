using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class TimelineSlider: Singleton<TimelineSlider>
{
    [SerializeField] private GameObject m_tickPrefab;
    [SerializeField] private RectTransform m_ticksParent;
    private Slider m_slider;
    [SerializeField] private Text m_timelineText;
    private List<string> m_timesteps; 
    private float m_lastAutoValue = 0;

    public int Value
    {
        get => (int) m_slider.value;
    }
    
    public int MaxValue
    {
        get => (int) m_slider.maxValue;
    }
    
    private void Awake()
    {
        m_slider = GetComponent<Slider>();
        m_timesteps = new List<string>();
    }

    private void Start()
    {
        m_slider.onValueChanged.AddListener(OnSliderValueChanged);
    }
    
    private void Update()
    {
        // Check if the right arrow key is pressed
        if (Input.GetKeyDown(KeyCode.RightArrow))
            m_slider.value = Mathf.Clamp(m_slider.value + 1, m_slider.minValue, m_slider.maxValue);
            // Check if the left arrow key is pressed
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            m_slider.value = Mathf.Clamp(m_slider.value - 1, m_slider.minValue, m_slider.maxValue);
    }
    
    public void AppendTimestep(string key, string start, string end)
    {
        string text = "Frames [" + key.Replace('_', '-') + "] Time [" + start + "-" + end + "m]";
        m_timesteps.Add(text);
        if(m_timelineText.text.Length > 1) return;
        m_timelineText.text = m_timesteps[0];
    }
    
    public void ChangeTimelineSliderState(bool state)
    {
        m_slider.interactable = state;
        m_slider.value = m_lastAutoValue;
    }

    public void SetSliderValue(float value)
    {
        m_slider.value = value;
    }

    public int IncreaseSliderValue()
    {
        float sliderValue = m_slider.value + 1;
        if(sliderValue <= m_slider.maxValue)
            m_slider.value = sliderValue;
        m_lastAutoValue = sliderValue;
        return (int)sliderValue;
    }

    public void SetTimeLineText(string text)
    {
        m_timelineText.text = text;
    }
    
    private void OnSliderValueChanged(float value)
    {
        if (m_slider.value < m_lastAutoValue)
            m_slider.value = m_lastAutoValue;
        m_timelineText.text = m_timesteps[Mathf.RoundToInt(m_slider.value)];
        
        List<Room> rootRooms = RoomManager.Instance.GetRootRooms();
        foreach (var room in rootRooms)
        {
            room.SetCurrentProfile((int)m_slider.value);
        }
    }

    public void InitializeRuler(int maxNumber)
    {
        maxNumber -= 1;
        m_slider.wholeNumbers = true;
        m_slider.maxValue = maxNumber;
        
        // Clear previous ticks
        foreach (Transform child in m_ticksParent)
        {
            Destroy(child.gameObject);
        }

        // Create ticks
        for (int i = 0; i <= maxNumber + 1; i++)
        {
            GameObject tick = Instantiate(m_tickPrefab, m_ticksParent);
            // Position tick appropriately along the slider
            float normalizedPosition = (float)i / maxNumber;
            RectTransform tickRectTransform = tick.GetComponent<RectTransform>();
    
            tickRectTransform.anchorMin = new Vector2(normalizedPosition, 0);
            tickRectTransform.anchorMax = new Vector2(normalizedPosition, 1);
            tickRectTransform.anchoredPosition = Vector2.zero;
            tickRectTransform.sizeDelta = new Vector2(2, tickRectTransform.sizeDelta.y);  // adjust the width as per your preference
    
            // Center the tick on its anchor point
            tickRectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }
    
    
}