using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimulationState : Singleton<SimulationState>
{
    private Button m_button;
    private EnvironmentSetup_modCCP m_environmentSetup;
    [SerializeField] private Color m_startColor;
    [SerializeField] private Color m_pauseColor;
    private bool m_running = false;
    
    public bool Running
    {
        get
        {
            return m_running;
        }
    }
    
    // Start is called before the first frame update
    void Awake()
    {
        m_button = GetComponent<Button>();
        m_environmentSetup = EnvironmentSetup_modCCP.Instance;
        base.Awake();
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public void TriggerStartSimulation(Button startButton)
    {
        if (m_running == false)
        {
            m_running = true;
            TimelineSlider.Instance.ChangeTimelineSliderState(false);

            m_button.GetComponent<Image>().color = m_pauseColor;
            m_button.transform.Find("Text").GetComponent<Text>().text = "Pause";
            m_environmentSetup.startSimulation = true;
        }
        else
        {
            m_running = false;
            TimelineSlider.Instance.ChangeTimelineSliderState(true);
            m_button.GetComponent<Image>().color = m_startColor;
            m_button.transform.Find("Text").GetComponent<Text>().text = "Resume";
        }
    }
}
