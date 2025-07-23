using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Newtonsoft.Json;

public class RoomManager : Singleton<RoomManager>
{
    [SerializeField] private GameObject m_floorPrefab;
    [SerializeField] private float m_floorScale;
    [SerializeField] private int m_floorSize;
    private Vector3 m_startPos;
    private Vector3 m_endPos;
    private Vector3 m_deleteStartPos;
    private Vector3 m_deleteEndPos;
    private bool m_isCreating = false;
    private bool m_isDeleting = false;
    public bool interacting = true;
    public Vector3 gridCenter;
    [SerializeField] private Camera m_camera;
    private List<GameObject> m_addedFloors = new List<GameObject>();
    private Dictionary<Vector2Int, Room> m_rootRooms;
    [SerializeField] private Renderer m_visualFeedbackObject;
    [SerializeField] private Color m_addColor;
    [SerializeField] private Color m_deleteColor;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private EventSystem eventSystem;
    private bool m_isBuildMode = true;

    private void Awake()
    {
        base.Awake();
    }

    public void EnableDisableBuildMode(bool state)
    {
        m_isBuildMode = state;
    }

    public List<Room> GetRootRooms()
    {
        List<Room> rootRooms = new List<Room>();
        foreach (var room in m_addedFloors)
        {
            Room r = room.GetComponent<Room>();
            if(r.IsRoot)
                rootRooms.Add(r);
        }

        return rootRooms;
    }

    private void Update()
    {
        if (interacting && m_isBuildMode)
        {
            // Room creation
            if (Input.GetMouseButtonDown(0))
            {
                m_startPos = SnapToGrid(GetMousePosOnFloor());
                m_endPos = m_startPos;
                if (m_startPos != Vector3.zero)
                {
                    m_isCreating = true;
                }
            }

            // Room deletion
            if (Input.GetMouseButtonDown(1))
            {
                m_deleteStartPos = SnapToGrid(GetMousePosOnFloor());
                m_deleteEndPos = m_deleteStartPos;
                if (m_deleteStartPos != Vector3.zero)
                {
                    m_isDeleting = true;
                }
            }

            if (m_isCreating && Input.GetMouseButton(0))
            {
                m_isDeleting = false;
                m_endPos = SnapToGrid(GetMousePosOnFloor());
                UpdateVisualFeedback(m_startPos, m_endPos, m_addColor);
            }
            else if (m_isDeleting && Input.GetMouseButton(1))
            {
                m_isCreating = false;
                m_deleteEndPos = SnapToGrid(GetMousePosOnFloor());
                UpdateVisualFeedback(m_deleteStartPos, m_deleteEndPos, m_deleteColor);
            }
            else
                m_visualFeedbackObject.gameObject.SetActive(false);

            if (m_isCreating && Input.GetMouseButtonUp(0))
            {
                CreateFloorsWhileCreating();
                m_isCreating = false;
            }

            if (m_isDeleting && Input.GetMouseButtonUp(1))
            {
                DeleteFloorsWhileCreating();
                m_isDeleting = false;
            }

            if (Input.GetMouseButtonUp(0))
            {
                m_isCreating = false;
            }

            if (Input.GetMouseButtonUp(1))
            {
                m_isDeleting = false;
            }
        }

        if (!m_isBuildMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SelectRoomForProfile();
            }
        }
    }

    private void SelectRoomForProfile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        int layerMask = 1 << 7; // Floor Layer only
        
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            Room room = hitInfo.transform.gameObject.GetComponent<Room>();
            if (room != null)
            {
                room.IsSelected = true;
            }
        }
    }
    
    public void InitializeGrid(int width, int height)
    {
        m_rootRooms = new Dictionary<Vector2Int, Room>(width * height);
        
        Vector3 startPosition = Vector3.zero;

        // Create the rooms in a grid
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 position = startPosition + new Vector3(x * m_floorSize, 0, -z * m_floorSize);
                GameObject addedRoom = AddFloorAtPos(position, true, z, x);
                m_rootRooms.Add(new Vector2Int(z, x), addedRoom.GetComponent<Room>());
            }
        }

        gridCenter = new Vector3((width - 1) * (float)m_floorSize / 2f, 0f, -(height - 1) * (float)m_floorSize / 2f);

        // Find grid's center for positioning the camera.
        Vector3 cameraCenter = new Vector3(gridCenter.x, m_camera.transform.position.y, gridCenter.z);
        float uiWidthInWorldUnits = -(m_camera.orthographicSize * 2 * m_camera.aspect) * 0.65f;
        cameraCenter += new Vector3(uiWidthInWorldUnits / 2, 0, 0);

        m_camera.transform.position = cameraCenter;

        m_camera.orthographicSize = height * m_camera.orthographicSize * m_floorScale + 3;
    }

    public Vector3 SnapToGrid(Vector3 worldPosition)
    {
        float gridSize = m_floorSize;
    
        float x = Mathf.Round(worldPosition.x / gridSize) * gridSize;
        float z = Mathf.Round(worldPosition.z / gridSize) * gridSize;
    
        return new Vector3(x, 0, z);
    }

    private Vector3 GetMousePosOnFloor()
    {
        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            // m_floorSize: "CustomUI" layer
            if (result.gameObject.layer == m_floorSize)
                return Vector3.zero;
        }
        
        Vector2 mousePos = Input.mousePosition;
        Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, -m_camera.transform.position.y);
        Vector3 worldPos = m_camera.ScreenToWorldPoint(screenPos);
        return new Vector3(worldPos.x, 0, worldPos.z);
    }

    public GameObject AddFloorAtPos(Vector3 position, bool isRoot, int row, int column)
    {
        Vector3 roundedPos = new Vector3(Mathf.Round(position.x / m_floorSize) * m_floorSize, 0, Mathf.Round(position.z / m_floorSize) * m_floorSize);

        (bool hasFloor, Room _) = PositionHasFloor(roundedPos); 
        if (!hasFloor){
            GameObject newPlane = Instantiate(m_floorPrefab, roundedPos, Quaternion.identity, transform.GetChild(0));
            newPlane.transform.localScale = new Vector3(m_floorScale, 1f, m_floorScale);
            m_addedFloors.Add(newPlane);
            newPlane.GetComponent<Room>().IsRoot = isRoot;
            newPlane.GetComponent<Room>().Coordinates = new Vector2(row, column);
            
            // Update the walls of the newly added plane
            UpdatePlaneWalls(newPlane);

            // Also check and update the walls of its neighbors
            UpdateNeighborsWalls(roundedPos);
            return newPlane;
        }

        return null;
    }

    private void CreateFloorsWhileCreating()
    {
        int xStart = Mathf.FloorToInt(m_startPos.x / m_floorSize) * m_floorSize;
        int xEnd = Mathf.FloorToInt(m_endPos.x / m_floorSize) * m_floorSize;
        int zStart = Mathf.FloorToInt(m_startPos.z / m_floorSize) * m_floorSize;
        int zEnd = Mathf.FloorToInt(m_endPos.z / m_floorSize) * m_floorSize;

        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x += m_floorSize)
        {
            for (int z = Mathf.Min(zStart, zEnd); z <= Mathf.Max(zStart, zEnd); z += m_floorSize)
            {
                Vector3 newPos = new Vector3(x, 0, z);
                (bool hasFloor, Room _) = PositionHasFloor(newPos); 
                if (!hasFloor)
                {
                    AddFloorAtPos(newPos, false, -1, -1);
                }
            }
        }
    }

    private void DeleteFloorAtPos(Vector3 position)
    {
        GameObject floorToDelete = GetFloorAtPosition(position);
        if (floorToDelete != null)
        {
            Room room = floorToDelete.GetComponent<Room>();
            if (room.DeleteRoom())
            {
                m_addedFloors.Remove(floorToDelete);
                Destroy(floorToDelete);
                // Update neighboring walls since we removed a plane
                UpdateNeighborsWalls(position);
            }
        }
    }
    
    private void DeleteFloorsWhileCreating()
    {
        int xStart = Mathf.FloorToInt(m_deleteStartPos.x / m_floorSize) * m_floorSize;
        int xEnd = Mathf.FloorToInt(m_deleteEndPos.x / m_floorSize) * m_floorSize;
        int zStart = Mathf.FloorToInt(m_deleteStartPos.z / m_floorSize) * m_floorSize;
        int zEnd = Mathf.FloorToInt(m_deleteEndPos.z / m_floorSize) * m_floorSize;

        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x += m_floorSize)
        {
            for (int z = Mathf.Min(zStart, zEnd); z <= Mathf.Max(zStart, zEnd); z += m_floorSize)
            {
                Vector3 pos = new Vector3(x, 0, z);
                DeleteFloorAtPos(pos);
            }
        }
    }
    
    private (bool, Room) PositionHasFloor(Vector3 position)
    {
        foreach(GameObject floor in m_addedFloors)
        {
            if (floor.transform.position == position)
            {
                return (true, floor.GetComponent<Room>());
            }
        }

        return (false, null);
    }
    
    private (bool, Room) HasNeighbor(Vector3 position, Vector3 direction)
    {
        return PositionHasFloor(position + direction * m_floorSize);
    }
    
    private void UpdatePlaneWalls(GameObject plane)
    {
        Vector3 planePos = plane.transform.position;

        // Check each direction for a neighbor
        (bool hasNorthNeighbor, Room _) = HasNeighbor(planePos, Vector3.forward);
        (bool hasSouthNeighbor, Room _) = HasNeighbor(planePos, Vector3.back);
        (bool hasEastNeighbor, Room _) = HasNeighbor(planePos, Vector3.right);
        (bool hasWestNeighbor, Room _) = HasNeighbor(planePos, Vector3.left);

        // Activate walls if there's no neighbor in that direction
        plane.transform.Find("North").gameObject.SetActive(!hasNorthNeighbor);
        plane.transform.Find("South").gameObject.SetActive(!hasSouthNeighbor);
        plane.transform.Find("East").gameObject.SetActive(!hasEastNeighbor);
        plane.transform.Find("West").gameObject.SetActive(!hasWestNeighbor);
    }
    
    void UpdateNeighborsWalls(Vector3 position)
    {
        Vector3 north = position + Vector3.forward * m_floorSize;
        Vector3 south = position + Vector3.back * m_floorSize;
        Vector3 east = position + Vector3.right * m_floorSize;
        Vector3 west = position + Vector3.left * m_floorSize;
        
        (bool hasFloorNorth, Room _) = PositionHasFloor(north); 
        if (hasFloorNorth) 
            UpdatePlaneWalls(GetFloorAtPosition(north));
        
        (bool hasFloorSouth, Room _) = PositionHasFloor(south); 
        if (hasFloorSouth) 
            UpdatePlaneWalls(GetFloorAtPosition(south));
        
        (bool hasFloorEast, Room _) = PositionHasFloor(east); 
        if (hasFloorEast) 
            UpdatePlaneWalls(GetFloorAtPosition(east));
        
        (bool hasFloorWest, Room _) = PositionHasFloor(west); 
        if (hasFloorWest) 
            UpdatePlaneWalls(GetFloorAtPosition(west));
    }
    
    public List<BehaviorProfile> GetNeighborsProfiles(Vector3 position)
    {
        Vector3 north = position + Vector3.forward * m_floorSize;
        Vector3 south = position + Vector3.back * m_floorSize;
        Vector3 east = position + Vector3.right * m_floorSize;
        Vector3 west = position + Vector3.left * m_floorSize;

        List<BehaviorProfile> profiles = new List<BehaviorProfile>(4);
        
        (bool hasFloorNorth, Room roomNorth) = PositionHasFloor(north); 
        if (hasFloorNorth) 
            profiles.Add(roomNorth.Profile);
        else
            profiles.Add(null);
        
        (bool hasFloorSouth, Room roomSouth) = PositionHasFloor(south); 
        if (hasFloorSouth) 
            profiles.Add(roomSouth.Profile);
        else
            profiles.Add(null);
        
        (bool hasFloorEast, Room roomEast) = PositionHasFloor(east); 
        if (hasFloorEast) 
            profiles.Add(roomEast.Profile);
        else
            profiles.Add(null);
        
        (bool hasFloorWest, Room roomWest) = PositionHasFloor(west);
        if (hasFloorWest)
            profiles.Add(roomWest.Profile);
        else
            profiles.Add(null);

        return profiles;
    }
    
    private GameObject GetFloorAtPosition(Vector3 position)
    {
        return m_addedFloors.Find(p => p.transform.position == position);
    }
    
    private void UpdateVisualFeedback(Vector3 startPos, Vector3 endPos, Color feedbackColor)
    {
        m_visualFeedbackObject.gameObject.SetActive(true);
        m_visualFeedbackObject.material.color = feedbackColor;
        Vector3 centerPos = (startPos + endPos) / 2.0f;
        m_visualFeedbackObject.transform.position = new Vector3(centerPos.x, 1f, centerPos.z); // A bit above the ground to avoid z-fighting
        Vector3 scale = new Vector3(Mathf.Abs(endPos.x - startPos.x) + m_floorSize, Mathf.Abs(endPos.z - startPos.z) + m_floorSize, 0.01f);
        m_visualFeedbackObject.transform.localScale = scale;
    }

    public void ShowHideBehaviors(bool show)
    {
        foreach (var room in m_addedFloors)
        {
            room.GetComponent<Room>().ShowHideBehavior(show);
        }
    }
    
    public Room DetectCurrentRoom(Vector3 agentPosition)
    {
        GameObject closestRoom = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject room in m_addedFloors)
        {
            float distance = Vector3.Distance(agentPosition, room.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRoom = room;
            }
        }

        return closestRoom.GetComponent<Room>();
    }

    public List<Room> GetSelectedRooms()
    {
        List<Room> selectedRooms = new List<Room>();
        foreach (GameObject room in m_addedFloors)
        {
            Room r = room.GetComponent<Room>();
            if(r.IsSelected)
                selectedRooms.Add(r);
        }

        return selectedRooms;
    }

    private int m_frame = 0;
    private void FixedUpdate()
    {
        if(SimulationState.Instance.Running == false)
            return;
        m_frame += 1;
        int endFrame = Mathf.RoundToInt(EnvironmentSetup_modCCP.Instance.endSeconds / 0.04f);
        EnvironmentSetup_modCCP env = EnvironmentSetup_modCCP.Instance;
        if (env.saveRoutes)
        { 
            if((endFrame > 0 && m_frame >= endFrame) || (env.saveNow))
                SaveRoomsToJson();    
        }
    }

    public void SaveRoomsToJson()
    {
        string path = EnvironmentSetup_modCCP.Instance.savePath + "/room_profiles.json";
        if (File.Exists(path))
            return;

        // Prepare the data structure for JSON serialization
        var formattedDictionary = new Dictionary<string, List<List<float>>>();
        foreach (var roomEntry in m_rootRooms)
        {
            var key = $"{roomEntry.Key.x}_{roomEntry.Key.y}";
            var profileList = new List<List<float>>();
            
            foreach (var profile in roomEntry.Value.GetProfiles())
            {
                profileList.Add(new List<float> { profile.goal, profile.group, profile.interaction, profile.connection });
            }

            formattedDictionary.Add(key, profileList);
        }
        string json = JsonConvert.SerializeObject(formattedDictionary, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
