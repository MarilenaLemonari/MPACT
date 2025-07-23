using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

enum DatasetSelector{
    Zara,
    Students,
    Church
}

public class EnvironmentSetup_modCCP : Singleton<EnvironmentSetup_modCCP>
{
    [Header("Run Data")]
    [SerializeField] private DatasetSelector m_datasetName;
    [Header("Saving")]
    public float startSeconds = 0;
    public float endSeconds = 0;
    public float stepSeconds;
    public float currentStep = 0;
    private int m_frame = 0;
    public bool saveRoutes;
    public bool saveNow;
    [HideInInspector] public string savePath;
    public DataReader.SimulationRootObject simulationRootObject;
    public DataReader.SceneRootObject sceneRootObject;
    private DataReader m_dataReader;
    private int m_frameInterval;
    [SerializeField] private int m_nextEndFame;
    private int m_framerate;
    [HideInInspector] public int m_width;
    [HideInInspector] public int m_height;
    [Header("Environment Info")]
    [SerializeField] private bool m_transparentSceneObjects;
    [SerializeField] private float m_endFrameMultiplier;
    public Vector3 floorScale;
    public float scale;
    private Dictionary<int, List<float[]>> m_agentDictionary;
    private Dictionary<string, List<DataReader.RealData>> m_agentDictionaryReal;
    public int frame = 0;
    private Transform m_agentsParent;
    [SerializeField] private GameObject m_agentPrefab;
    public float goalDistance = 1.5f;
    public float groupingDistance = 3f;
    public float interactionDistance = 4f;
    public float neighbourDistance = 6;
    public int maxNeighbours = 4;
    [HideInInspector] public List<GameObject> activeAgents;
    private Transform m_interactionParent;
    private Transform m_obstacleParent;
    [SerializeField] private GameObject m_interactionPrefab;
    [SerializeField] private GameObject m_obstaclePrefab;
    [SerializeField] private Text m_datasetNameText;
    [SerializeField] private Toggle m_spawnToggle;
    [SerializeField] private ProfileWheel m_profileWheel;
    public List<Transform> interactionObjects;
    private int m_agentID = 0;
    public bool m_updateGridProfiles = true;
    private bool m_useDatasetAgentPositions = true;
    [SerializeField] private LayerMask m_agentSpawnLayerMask;
    private Collider[] m_spawnHitColliders;
    [HideInInspector] public bool startSimulation = false;
    public Dictionary<int, InterconnectionData> interconnectionData;
    // Behavior Scene
    private CustomSceneManager m_behaviorDemoManager;

    private string GetDatasetName()
    {
        switch (m_datasetName)
        {
            case DatasetSelector.Zara:
                return "Zara";
            case DatasetSelector.Students:
                return "Students";
            case DatasetSelector.Church:
                return "Church";
            default:
                return "";
        }
    }
    
    public void ContinueUpdateGridProfiles(bool state)
    {
        m_updateGridProfiles = state;
    }

    public void UseDatasetAgentPositions(bool state)
    {
        m_useDatasetAgentPositions = state;
    }
    
    public class InterconnectionData
    {
        public int id;
        public Vector3 centerOfMass;
        public float averageDistance;
        public float speedVariance;
        public List<modCCPAgent_Framework> agents;
        private Color color;

        public InterconnectionData(int idIn)
        {
            id = idIn;
            centerOfMass = Vector3.zero;
            averageDistance = 0;
            agents = new List<modCCPAgent_Framework>();
            color = new Color(Random.value, Random.value, Random.value, 1.0f);
        }

        public int RemoveAgentFromGroup(modCCPAgent_Framework agent)
        {
            agents.Remove(agent);
            return agents.Count;
        }
        
        public void AddAgentToGroup(modCCPAgent_Framework agent) { agents.Add(agent); }

        public bool IsAgentInMyGroup(modCCPAgent_Framework agent)
        {
            return agents.Contains(agent);
        }
        
        public void UpdateInterconnectionParameters()
        {
            // Center of mass
            Vector3 center = new Vector3(0, 0, 0);
            foreach (modCCPAgent_Framework agent in agents)
            {
                center += agent.transform.position;
            }
            centerOfMass = center / agents.Count;

            // Center Distace
            float dis = 0;
            foreach (modCCPAgent_Framework agent in agents)
            {
                Debug.DrawLine(agent.transform.position,centerOfMass, color);
                dis += Vector3.Distance(agent.transform.position, centerOfMass);
            }
            averageDistance = dis / agents.Count;

            CalculateSpeedVariance();
        }
        public void CalculateSpeedVariance()
        {
            float sum = 0;
            float mean = 0;
            float variance = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                sum += agents[i].m_currentSpeed;
            }
            mean = sum / agents.Count;
            for (int i = 0; i < agents.Count; i++)
            {
                variance += (agents[i].m_currentSpeed - mean)
                            * (agents[i].m_currentSpeed - mean);
            }
            variance /= agents.Count;
            speedVariance = variance;
        }
    }

    private void InitializeProfilesOvertime()
    {
        Dictionary<int, BehaviorProfile[]> videoProfilesOvertime = new Dictionary<int, BehaviorProfile[]>();
        List<string> rangeKeys = new List<string>(simulationRootObject.Classes.Keys);
        for (int i = 0; i < rangeKeys.Count; i++)
        {
            string rangeKey = rangeKeys[i];
            BehaviorProfile[] profiles = new BehaviorProfile[m_width * m_height];
            Dictionary<string, BehaviorProfile> rangeData = simulationRootObject.Classes[rangeKey];
            List<string> keyValueKeys = new List<string>(rangeData.Keys);
            for (int j = 0; j < keyValueKeys.Count; j++)
            {
                string keyValueKey = keyValueKeys[j];
                BehaviorProfile value = rangeData[keyValueKey];
                value = new BehaviorProfile(value.goal, value.group, value.interaction, value.connection);
                profiles[j] = value;
            }

            int frameIndex = int.Parse(rangeKey.Split('_')[1]) - m_frameInterval;
            videoProfilesOvertime.Add(frameIndex, profiles);

            TimeSpan startTimeSpan = TimeSpan.FromSeconds((frameIndex * (1f / m_framerate)));
            TimeSpan endTimeSpan = TimeSpan.FromSeconds(((frameIndex + m_frameInterval) * (1f / m_framerate)));
            string startTime =  string.Format("{0}:{1:D2}", startTimeSpan.Minutes, startTimeSpan.Seconds);
            string endTime = string.Format("{0}:{1:D2}", endTimeSpan.Minutes, endTimeSpan.Seconds);
            TimelineSlider.Instance.AppendTimestep(rangeKey, startTime, endTime);
        }
        ProfileManager.Instance.SetTimelineProfilesList(videoProfilesOvertime, m_width, m_height);
        ProfileManager.Instance.BuildTimeline();
        
        // Add cluster profile to profile wheel
        foreach (List<float> profile in simulationRootObject.Clusters)
        {
            BehaviorProfile p = new BehaviorProfile(profile[0], profile[1], profile[2], profile[3]);
            m_profileWheel.AddProfile(p);
        }
    }

    private void InitializeAgentsDictionary()
    {
        m_agentDictionary = new Dictionary<int, List<float[]>>();
        foreach (KeyValuePair<string, List<List<float>>> frame in simulationRootObject.Agents)
        {
            List<float[]> agents = new List<float[]>();
            foreach (List<float> values in frame.Value)
            {
                float[] agent = new float[]{values[1], values[2], values[4], values[5], values[3], values[6]};
                agents.Add(agent);
            }
            m_agentDictionary[int.Parse(frame.Key)] = agents;
        }
    }

    private void PlaceSceneObjects(DataReader.SceneRootObject obj)
    {
        if (obj == null || obj.SceneObjects.Count == 0)
        {
            GameObject interactionObj = Instantiate(m_interactionPrefab, Vector3.zero, Quaternion.identity, m_interactionParent.transform);
            interactionObj.transform.localScale = new Vector3(1f, 1f, 1f);
            interactionObj.transform.position = new Vector3(0f, -20f, 0f);
            return;
        }

        Vector3 gridCenter = RoomManager.Instance.gridCenter;
        
        int interactionCounter = 0;
        foreach (var so in obj.SceneObjects)
        {
            float x = NormalizeValue(so.pos_x, 0f, 1f, -1f, 1f) * scale * floorScale.x;
            float z = NormalizeValue(1f - so.pos_z, 0f, 1f, -1f, 1f) * scale * floorScale.z;
            Vector3 pos = new Vector3(gridCenter.x + x, 1.5f, gridCenter.z + z);
            float scale_x = so.scale_x * 2f * scale * floorScale.x;
            float scale_z = so.scale_z * 2f * scale * floorScale.z;
            
            // If object is obstacle
            if (so.type < 1f)
            {
                GameObject obstacleObj = Instantiate(m_obstaclePrefab, pos, Quaternion.identity, m_obstacleParent.transform);
                obstacleObj.transform.localScale = new Vector3(scale_x, 3f, scale_z);
                obstacleObj.GetComponent<SceneObject>().UpdateObstacleRVO();
                if (m_transparentSceneObjects)
                    obstacleObj.GetComponent<Renderer>().enabled = false;
            }
            // If object is interaction
            else
            {
                GameObject interactionObj = Instantiate(m_interactionPrefab, pos, Quaternion.identity, m_interactionParent.transform);
                interactionObj.transform.localScale = new Vector3(scale_x, 3f, scale_z);
                interactionObj.GetComponent<SceneObject>().UpdateObstacleRVO();
                if (m_transparentSceneObjects)
                    interactionObj.GetComponent<Renderer>().enabled = false;
                interactionCounter += 1;
            }
        }
        
        if (interactionCounter == 0)
        {
            GameObject interactionObj = Instantiate(m_interactionPrefab, Vector3.zero, Quaternion.identity, m_interactionParent.transform);
            interactionObj.transform.localScale = new Vector3(1f, 1f, 1f);
            interactionObj.transform.position = new Vector3(0f, -20f, 0f);
        }
    }

    private void Awake()
    {
        SceneManager.LoadScene("BehaviorDemo", LoadSceneMode.Additive);
        currentStep += stepSeconds;
    }

    // Start is called before the first frame update
    private void Start()
    {
        m_spawnHitColliders = new Collider[10];
        m_agentsParent = transform.Find("Agents");
        m_interactionParent = transform.Find("InteractionObjects");
        m_obstacleParent = transform.Find("ObstacleObjects");
        activeAgents = new List<GameObject>();
        interconnectionData = new Dictionary<int, InterconnectionData>();
        m_datasetNameText.text = GetDatasetName();

        if (saveRoutes)
        {
            String currentPath = Directory.GetCurrentDirectory();
            DirectoryInfo parent = Directory.GetParent(currentPath);
            int randomInt = UnityEngine.Random.Range(0, 1000000);
            savePath = parent.ToString() + "/MPACT_Model/SaveData" + GetDatasetName() + "_" + randomInt;
            Directory.CreateDirectory(savePath);
        }

        // Read data from json
        m_dataReader = GetComponent<DataReader>();
        simulationRootObject = m_dataReader.ReadSimulationJsonFile(GetDatasetName());
        gameObject.name += "_" + GetDatasetName();
        m_frameInterval = simulationRootObject.Environment.frame_interval;
        m_nextEndFame = m_frameInterval;
        m_framerate = simulationRootObject.Environment.framerate;
        Time.fixedDeltaTime = 1f / m_framerate;

        //Build scene from json
        m_width = simulationRootObject.Environment.width;
        m_height = simulationRootObject.Environment.height;
        floorScale = new Vector3(m_width, 1f, m_height);
        RoomManager.Instance.InitializeGrid(m_width, m_height);

        // Place scene objects
        sceneRootObject = m_dataReader.ReadSceneObjectsJsonFile(GetDatasetName());
        PlaceSceneObjects(sceneRootObject);
        
        RVO_Manager.Instance.RecreateObstacles(true);

        InitializeProfilesOvertime();
        InitializeAgentsDictionary();
        
        m_behaviorDemoManager = GameObject.Find("EnvDemo").GetComponent<CustomSceneManager>();
    }

    public void InitializeNewDemoSimulation()
    {
        m_behaviorDemoManager.StartNewDemo(ProfileManager.Instance.GetCurrentProfile());
    }

    private void SpawnAgentsInFrame(int frameIndex, int endFrameInterval)
    {
        int startFrame = Mathf.RoundToInt(startSeconds / 0.04f);
        if(frameIndex < startFrame)
            return;

        // int currentEndFrame = Mathf.RoundToInt(currentStep / 0.04f);
        // if (frameIndex > currentEndFrame)
        // {
        //     foreach (Transform agent in m_agentsParent)
        //     {
        //         agent.GetComponent<modCCPAgent_Framework>().DeactivateAgent();
        //     }
        //     currentStep += stepSeconds;
        // }        
        
        int endFrame = Mathf.RoundToInt(endSeconds / 0.04f);
        if (frameIndex > endFrame)
        {
            foreach (Transform agent in m_agentsParent)
            {
                agent.GetComponent<modCCPAgent_Framework>().DeactivateAgent();
            }
            UnityEditor.EditorApplication.isPlaying = false;
        }
        
        if(m_spawnToggle.isOn == false)
            return;
        
        Vector3 gridCenter = RoomManager.Instance.gridCenter;
        
        List<float[]> agents = m_agentDictionary[frameIndex];
        foreach (var agent in agents)
        {
            float x_spawn_pos = NormalizeValue(agent[0], 0f, 1f, -1f, 1f) * scale * floorScale.x;
            float z_spawn_pos = NormalizeValue(agent[1], 0f, 1f, -1f, 1f) * scale * floorScale.z;

            float x_goal_pos = NormalizeValue(agent[2], 0f, 1f, -1f, 1f) * scale * floorScale.x;
            float z_goal_pos = NormalizeValue(agent[3], 0f, 1f, -1f, 1f) * scale * floorScale.z;
            Vector3 spawnPosTemp = transform.TransformPoint(new Vector3(x_spawn_pos, 0f, z_spawn_pos)); 
            Vector3 goalPosTemp = transform.TransformPoint(new Vector3(x_goal_pos, 0f, z_goal_pos));
            Vector3 spawnPos = new Vector3(spawnPosTemp.x + gridCenter.x, 0f, spawnPosTemp.z + gridCenter.z);
            Vector3 goalPos = new Vector3(goalPosTemp.x + gridCenter.x, 0f, goalPosTemp.z + gridCenter.z);
            
            if (true)
            {
                GameObject agentGameobject = Instantiate(m_agentPrefab, spawnPos, Quaternion.identity, m_agentsParent);
                print(agentGameobject);
                agentGameobject.name = "Agent_" + m_agentID;
                agentGameobject.SetActive(false);
                modCCPAgent_Framework ccpAgent = agentGameobject.GetComponent<modCCPAgent_Framework>();
                ccpAgent.m_frame = frame;
                ccpAgent.SetGoalPosition(goalPos);
                ccpAgent.SetEndFrame(frame + endFrameInterval + Mathf.RoundToInt(m_frameInterval * m_endFrameMultiplier));
                int groupId = (int)agent[5];
                if (interconnectionData.ContainsKey(groupId))
                    interconnectionData[groupId].AddAgentToGroup(ccpAgent);
                else
                {
                    InterconnectionData interData = new InterconnectionData(groupId);
                    interData.AddAgentToGroup(ccpAgent);
                    interconnectionData.Add(groupId, interData);
                }
                int rvoID = Simulator.Instance.addAgent(new RVO.Vector2(spawnPos.x, spawnPos.z));
                ccpAgent.m_rvoID = rvoID;
                ccpAgent.RotateToGoal();
                
                ccpAgent.groupId = groupId;
                agentGameobject.SetActive(true);
                m_agentID += 1;
            }
        }
    }

    public modCCPAgent_Framework[] SpawnAgentsFromUI(Vector3 mainPos, int quantity)
    {
        Vector3[] spawnPoints = GenerateSpawnPointsAroundPoint(mainPos, quantity);
        modCCPAgent_Framework[] agentsToSpawn = new modCCPAgent_Framework[quantity];
        
        int groupId = -1;
        do
        {
            groupId = Mathf.Abs(Guid.NewGuid().GetHashCode());
        } while (interconnectionData.ContainsKey(groupId));
        
        for (int i = 0; i < quantity; i++)
        {
            int uniqueAgentId = Mathf.Abs(Guid.NewGuid().GetHashCode());
            GameObject agentGameobject = Instantiate(m_agentPrefab, spawnPoints[i], Quaternion.identity, m_agentsParent);
            agentGameobject.name = "Agent_" + groupId + "_" + uniqueAgentId;
            agentGameobject.SetActive(false);
            modCCPAgent_Framework ccpAgent = agentGameobject.GetComponent<modCCPAgent_Framework>();
            ccpAgent.m_frame = frame;
            ccpAgent.SetEndFrame(frame + 10000);
            
            if (interconnectionData.ContainsKey(groupId))
                interconnectionData[groupId].AddAgentToGroup(ccpAgent);
            else
            {
                InterconnectionData interData = new InterconnectionData(groupId);
                interData.AddAgentToGroup(ccpAgent);
                interconnectionData.Add(groupId, interData);
            }
            ccpAgent.groupId = groupId;
            agentsToSpawn[i] = ccpAgent;
        }

        return agentsToSpawn;
    }
    
    public Vector3[] GenerateSpawnPointsAroundPoint(Vector3 sourcePoint, int quantity)
    {
        Vector3[] retPoints = new Vector3[quantity];
        
        // If only one point need, return the center point
        if (quantity == 1){
            retPoints[0] = sourcePoint;
            return retPoints;
        }
        // Else return a list of points around the center point
        for (int i = 0; i < quantity; i++)
        {
            float angle = i * 360f / quantity;
            float angleRad = Mathf.Deg2Rad * angle;
            float x = 0.75f * Mathf.Cos(angleRad);
            float z = 0.75f * Mathf.Sin(angleRad);
            Vector3 pointPos = new Vector3(x, 0, z);
            retPoints[i] = sourcePoint + pointPos;
        }
        return retPoints;
    }
    
    private void UpdateInterconnectionData()
    {
        foreach (var data in interconnectionData)
            data.Value.UpdateInterconnectionParameters();
    }
    
    public void RemoveAgent(modCCPAgent_Framework agent, int groupId)
    {
        if (interconnectionData.Count > 0)
        {
            int groupCount = interconnectionData[groupId].RemoveAgentFromGroup(agent);
            if (agent.m_rvoID >= 0)
                Simulator.Instance.delAgent(agent.m_rvoID);
            if (groupCount == 0)
            {
                interconnectionData.Remove(groupId);
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (startSimulation && SimulationState.Instance.Running)
        {
            UpdateInterconnectionData();

            frame += 1;
            if (frame % m_frameInterval == 0)
            {
                m_nextEndFame += m_frameInterval;
                TimelineSlider.Instance.IncreaseSliderValue();
            }

            if (m_useDatasetAgentPositions && m_agentDictionary.ContainsKey(frame))
                SpawnAgentsInFrame(frame, m_nextEndFame);

            activeAgents.Clear();
            activeAgents.TrimExcess();
            activeAgents.AddRange(m_agentsParent.Cast<Transform>().Select(child => child.gameObject));
        }
    }

    private void Update()
    {
        interactionObjects = GameObject.FindGameObjectsWithTag("Interaction")
            .Select(go => go.transform)
            .ToList();
    }

    public float NormalizeValue(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        float normalizedValue = (((value - oldMin) * newRange) / oldRange) + newMin;
        return normalizedValue;
    }
    
    private Vector3 CalculateGroupCenter(List<Vector3> points)
    {
        Vector3 center = new Vector3(0, 0, 0);
        float count = 0;
        foreach (Vector3 p in points)
        {
            center += p;
            count++;
        }
        return center / count;
    }
    
    public Vector3[] GetCloserAgent(Vector3 currentPos)
    {
        if (activeAgents.Count < 2)
        {
            return new Vector3[]
            {
                currentPos,
                currentPos,
                new Vector3(0, 0, 0)
            };
        }
        
        List<Vector3> neighboursPoints = new List<Vector3>();
        
        int closeAgents = 0;
        Vector3 agentMin = currentPos;
        float minDist = Mathf.Infinity;

        foreach (GameObject otherAgent in activeAgents)
        {
            float dist = Vector3.Distance(otherAgent.transform.position, currentPos);
            if (dist <= neighbourDistance)
            {
                closeAgents++;
                neighboursPoints.Add(otherAgent.transform.position);
            }
            
            if (dist < minDist && dist > 0.1f)
            {
                agentMin = otherAgent.transform.position;
                minDist = dist;
            }
        }

        Vector3 groupCenterPoint;
        if (neighboursPoints.Count > 1)
            groupCenterPoint = CalculateGroupCenter(neighboursPoints);
        else
            groupCenterPoint = currentPos;

        return new Vector3[]
        {
            agentMin,
            groupCenterPoint,
            new Vector3(closeAgents, 0f, 0f)
        };
    }
    
    public Vector3 GetCloserInteraction(Vector3 currentPos)
    {
        Vector3 objectMin = currentPos;
        float minDist = Mathf.Infinity;
        foreach (Transform interPos in interactionObjects)
        {
            if (interPos.gameObject.CompareTag("Interaction"))
            {
                Collider objectCollider = interPos.GetComponent<Collider>();
                Vector3 closestPointOnObject = objectCollider.ClosestPoint(currentPos);
                float distanceToEdge = Vector3.Distance(currentPos, closestPointOnObject);

                if (distanceToEdge < minDist)
                {
                    objectMin = closestPointOnObject;
                    minDist = distanceToEdge;
                }
            }
        }

        return objectMin;
    }
    
    public bool PositionIsInBounds(Vector3 pos)
    {
        if (pos.x <= -1 || pos.x > 16 || pos.z <= -11 || pos.z >= 1f)
            return true;
        return false;
    }
}
