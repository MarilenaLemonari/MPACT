using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfileWheel : MonoBehaviour
{
    private List<BehaviorProfile> m_datasetProfiles;
    [SerializeField] private Transform m_pointsParent;
    [SerializeField] private GameObject m_pointUI;
    [SerializeField] private Slider m_connectSlider;

    public void AddProfile(BehaviorProfile profile)
    {
        m_datasetProfiles.Add(profile);
        GameObject point = Instantiate(m_pointUI, Vector3.zero, Quaternion.identity, m_pointsParent);
        point.GetComponent<DatasetWheelPoint>().SetPosition(profile, m_connectSlider);
    }

    private void Awake()
    {
        m_datasetProfiles = new List<BehaviorProfile>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
