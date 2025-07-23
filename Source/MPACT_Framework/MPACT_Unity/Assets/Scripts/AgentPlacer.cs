using System;
using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

public class AgentPlacer : MonoBehaviour
{
    [SerializeField] private GameObject m_agentPrefab;
    [SerializeField] private Transform m_agentParent;
    [SerializeField] private Image m_cursorImage;
    [SerializeField] private Image m_goalImage;
    [SerializeField] private Slider m_quantitySlider;
    [SerializeField] private Text m_quantityText;

    public LayerMask placementLayerMask; // Assign the layer of the floor in the inspector
    private Camera m_camera;
    private Collider[] m_spawnHitColliders;
    [SerializeField] private LayerMask m_agentSpawnLayerMask;
    private EnvironmentSetup_modCCP m_environmentSetup;
    private LineRenderer m_lineRenderer;
    private bool m_goalSet;
    private bool isPlacing;
    private List<modCCPAgent_Framework> m_agentsToSpawn;

    private void Awake()
    {
        m_camera = Camera.main;
        m_lineRenderer = GetComponent<LineRenderer>();
        m_lineRenderer.positionCount = 2;
        m_cursorImage.enabled = false;
        m_goalImage.enabled = false;
        m_lineRenderer.enabled = false;
        m_spawnHitColliders = new Collider[10];
        m_environmentSetup = EnvironmentSetup_modCCP.Instance;
        m_agentsToSpawn = new List<modCCPAgent_Framework>();
        m_quantitySlider.onValueChanged.AddListener(UpdateText);
        UpdateText(m_quantitySlider.value);
    }
    
    void UpdateText(float value) { m_quantityText.text = value.ToString(); }

    public void StartPlacingAgentObject()
    {
        GetComponent<ObjectPlacer>().StopPlacing();
        RoomManager.Instance.interacting = false;
        isPlacing = true;
        m_cursorImage.enabled = true;
        m_goalImage.enabled = false;
        m_lineRenderer.enabled = false;
        if (m_agentsToSpawn.Count > 0)
        {
            foreach (var agent in m_agentsToSpawn)
            {
                Destroy(agent.gameObject);
            }
            m_agentsToSpawn.Clear();
        }
        m_agentsToSpawn = new List<modCCPAgent_Framework>(5);
        m_goalSet = false;
    }
    
    public void StopPlacing()
    {
        RoomManager.Instance.interacting = true;
        m_cursorImage.enabled = false;
        m_goalImage.enabled = false;
        m_lineRenderer.enabled = false;
        isPlacing = false;
        if (m_agentsToSpawn.Count > 0 && m_goalSet == false)
        {
            foreach (var agent in m_agentsToSpawn)
            {
                Destroy(agent.gameObject);
            }
            m_agentsToSpawn.Clear();
        }
    }
    
    private bool CheckSpawnAreaIsEmpty(Vector3 center, float radius)
    {
        var numColliders = Physics.OverlapSphereNonAlloc(center, radius, m_spawnHitColliders, m_agentSpawnLayerMask);
        return numColliders == 0;
    }
    
    private void Update()
    {
        if (isPlacing)
        {
            Vector3 mousePos = Input.mousePosition;
            
            if (m_agentsToSpawn.Count > 0 && m_goalSet == false)
            {
                m_cursorImage.transform.position = m_camera.WorldToScreenPoint(m_lineRenderer.GetPosition(0));
                Vector3 mousePosScreen = Input.mousePosition;
                mousePosScreen.z = m_camera.transform.position.z / 20f;
                Vector3 mousePosWorld = m_camera.ScreenToWorldPoint(mousePosScreen);
                m_lineRenderer.SetPosition(1, mousePosWorld);
                m_goalImage.enabled = true;
                m_goalImage.transform.position = mousePos;
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayerMask))
                    {
                        //Layer 7: Floor
                        if (hit.transform.gameObject.layer == 7)
                        {
                            if (Vector3.Distance(hit.point, m_lineRenderer.GetPosition(0)) > 5f)
                            {
                                m_lineRenderer.enabled = false;
                                m_goalImage.enabled = false;
                                m_goalSet = true;
                                Vector3 goalPos = hit.point;
                                foreach (var agent in m_agentsToSpawn)
                                {
                                    agent.SetGoalPosition(goalPos);
                                    int rvoID = Simulator.Instance.addAgent(
                                        new RVO.Vector2(agent.transform.position.x, agent.transform.position.z));
                                    agent.m_rvoID = rvoID;
                                    agent.gameObject.SetActive(true);
                                }

                                m_agentsToSpawn.Clear();
                                StopPlacing();
                            }
                        }
                    }
                }
            }
            else
            {
                m_cursorImage.transform.position = mousePos;
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayerMask))
                    {
                        if (hit.transform.gameObject.layer == 7)
                        {
                            int quantity = (int)m_quantitySlider.value;
                            float radius = (quantity / 2f) * 1f;
                            if (CheckSpawnAreaIsEmpty(hit.point, radius))
                            {
                                m_agentsToSpawn = m_environmentSetup.SpawnAgentsFromUI(hit.point, quantity).ToList();
                                m_lineRenderer.enabled = true;
                                m_lineRenderer.SetPosition(0, hit.point);
                                m_goalSet = false;
                            }
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (isPlacing)
                    {
                        StopPlacing();
                    }
                }
            }
        }
    }
}