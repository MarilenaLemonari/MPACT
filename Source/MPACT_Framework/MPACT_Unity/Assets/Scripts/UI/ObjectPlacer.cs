using System;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private GameObject m_obstaclePrefab;
    [SerializeField] private GameObject m_interactionPrefab;
    [SerializeField] private Transform m_obstacleParent;
    [SerializeField] private Transform m_interactionParent;
    [SerializeField] private Image m_obstacleImage;
    [SerializeField] private Image m_interactionImage;
    [SerializeField] private Image m_obstacleImageSelected;
    [SerializeField] private Image m_interactionImageSelected;
    [SerializeField] private Color m_selectedColor;
    private GameObject m_prefabToPlace; // Assign the prefab in the inspector
    private Transform m_parent;
    private Image m_cursorImage;
    public LayerMask placementLayerMask; // Assign the layer of the floor in the inspector
    [SerializeField] private Camera m_camera;
    private Collider[] m_spawnHitColliders;
    [SerializeField] private LayerMask m_objectSpawnLayerMask;
    private bool isPlacing = false;
    private SceneObject m_currentCube;
    private Vector3 m_initialMousePosition;
    private Vector3 m_minScale = new Vector3(1.5f, 3f, 1.5f);
    private Vector3 m_maxScale = new Vector3(5f, 3f, 5f);

    private void Awake()
    {
        m_cursorImage = m_obstacleImage;
        m_cursorImage.enabled = false;
        m_spawnHitColliders = new Collider[10];
    }

    public void StartPlacingObstacleObject()
    {
        StopPlacing();
        if (m_prefabToPlace == m_obstaclePrefab)
        {
            m_prefabToPlace = null;
            return;
        }
        GetComponent<AgentPlacer>().StopPlacing();
        RoomManager.Instance.interacting = false;
        isPlacing = true;
        m_prefabToPlace = m_obstaclePrefab;
        m_parent = m_obstacleParent;
        m_cursorImage = m_obstacleImage;
        m_cursorImage.enabled = true;
        m_obstacleImageSelected.color = m_selectedColor;
    }
    
    public void StartPlacingInteractionObject()
    {
        StopPlacing();
        if (m_prefabToPlace == m_interactionPrefab)
        {
            m_prefabToPlace = null;
            return;
        }
        GetComponent<AgentPlacer>().StopPlacing();
        RoomManager.Instance.interacting = false;
        isPlacing = true;
        m_prefabToPlace = m_interactionPrefab;
        m_parent = m_interactionParent;
        m_cursorImage = m_interactionImage;
        m_cursorImage.enabled = true;
        m_interactionImageSelected.color = m_selectedColor;
    }

    public void StopPlacing()
    {
        RoomManager.Instance.interacting = true;
        m_cursorImage.enabled = false;
        isPlacing = false;
        m_obstacleImageSelected.color = Color.white;
        m_interactionImageSelected.color = Color.white;
    }

    private bool CheckSpawnAreaIsEmpty(Vector3 center, float radius)
    {
        var numColliders = Physics.OverlapSphereNonAlloc(center, radius, m_spawnHitColliders, m_objectSpawnLayerMask);
        return numColliders == 0;
    }
    
    private void Update()
    {
        if (isPlacing)
        {
            // Get the mouse position in world coordinates
            Vector3 mousePos = Input.mousePosition;
            // Set the position of the image to the mouse position
            m_cursorImage.transform.position = mousePos;
            
            if (Input.GetMouseButtonDown(0))
            {
                PlaceObject(mousePos);
            }
            if (Input.GetMouseButton(0) && m_currentCube != null)
            {
                RescaleObject();
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (m_currentCube != null)
                {
                    m_currentCube.UpdateObstacleRVO();
                    m_currentCube = null;
                }
            }
            
            // Delete Object
            if (Input.GetMouseButtonDown(1))
            {
                DeleteObject();
            }
        }
    }

    private (bool, Vector3) CanPlace(Vector3 mousePos)
    {
        Ray ray = m_camera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayerMask))
        {
            if (hit.transform.gameObject.layer == 7)
            {
                if (CheckSpawnAreaIsEmpty(hit.point, 1f))
                {
                    return (true, hit.point);
                }
            }
        }

        return (false, Vector3.zero);
    }

    private void PlaceObject(Vector3 mousePos)
    {
        Vector3 pos;
        bool canPlace;
        (canPlace, pos) = CanPlace(mousePos);
        pos = new Vector3(pos.x, 1.5f, pos.z);
        if(canPlace){
            GameObject cube = Instantiate(m_prefabToPlace, pos, Quaternion.identity, m_parent);
            m_currentCube = cube.GetComponent<SceneObject>();
            m_currentCube.Scale = new Vector3(1.5f, 3f, 1.5f);
            m_initialMousePosition = mousePos;
        }
    }

    private void RescaleObject()
    {
        Vector3 mouseDelta = Input.mousePosition - m_initialMousePosition;
        float moveScale = (Time.deltaTime / Time.timeScale) * 0.025f;
        Vector3 newScale = new Vector3(m_currentCube.transform.localScale.x + mouseDelta.x * moveScale, 
            m_currentCube.transform.localScale.y, m_currentCube.transform.localScale.z + mouseDelta.y * moveScale);
        // Clamp the scales to be within the defined minimum and maximum
        newScale.x = Mathf.Clamp(newScale.x, m_minScale.x, m_maxScale.x);
        newScale.z = Mathf.Clamp(newScale.z, m_minScale.z, m_maxScale.z);
        m_currentCube.Scale = newScale;
    }

    private void DeleteObject()
    {
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayerMask))
        {
            if (hit.transform.gameObject.layer == 6)
            {
                if(hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("Interaction"))
                    Destroy(hit.transform.gameObject);
                Simulator.Instance.ClearObstacles();
                RVO_Manager.Instance.RecreateObstacles(false);
            }
        }
    }
}