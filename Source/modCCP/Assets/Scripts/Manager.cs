using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using RVO;

public class Manager : MonoBehaviour
{
    private float m_timer;
    private const float INTERVAL = 180.0f; // 3 minutes(every beh), 3 minutes(full blend)
    
    [Header("Environment Parameters")]
    [SerializeField] private int m_numberOfAgents;
    [SerializeField] private GameObject m_agentPrefab;
    
    [Header("CCP Weights")]
    [Tooltip("[0, 1]")] public float goalWeight;
    [Tooltip("[0, 1]")] public float groupWeight;
    [Tooltip("[0, 1]")] public float interactWeight;
    [Tooltip("[0, 1]")] public float interconnWeight;

    [Header("Agent Parameters")]
    public float maxEnvironmentDistance;
    public float goalDistanceThreshold = 2f;
    [SerializeField] private float m_maxNeighbourDistance = 3f;
    public float groupDistanceThreshold = 3f;
    public int maxNeighbours = 6;
    public float interactionDistanceThreshold = 4;
    public float stationaryVelocityThreshold = 0.1f;

    [HideInInspector] public InteractionManager interactionManager;
    private List<Transform> m_agents;
    private Transform m_activeAgentsParent;
    private Transform m_inactiveAgentsParent;
    private List<Collider> m_spawnAreas;
    [SerializeField] private LayerMask m_agentsLayer;
    public Dictionary<int, InterconnectionData> interconnectionData;
    private List<int> m_availableKeys;
    private int m_nextGroupCount = 0;

    public class InterconnectionData
    {
        public int id;
        public Vector3 centerOfMass;
        public float averageDistance;
        public float speedVariance;
        public List<modCCPAgent> agents;
        private Color color;

        public InterconnectionData(int idIn)
        {
            id = idIn;
            centerOfMass = Vector3.zero;
            averageDistance = 0;
            agents = new List<modCCPAgent>();
            color = new Color(Random.value, Random.value, Random.value, 1.0f);
        }

        public int RemoveAgentFromGroup(modCCPAgent agent)
        {
            agents.Remove(agent);
            return agents.Count;
        }
        public void AddAgentToGroup(modCCPAgent agent) { agents.Add(agent); }
        
        public void UpdateInterconnectionParameters()
        {
            Vector3 averageVelocity = Vector3.zero;
            // Center of mass
            Vector3 center = new Vector3(0, 0, 0);
            foreach (modCCPAgent agent in agents)
            {
                center += agent.transform.position;
                averageVelocity += new Vector3(agent.m_currentVelocity.x, 0f, agent.m_currentVelocity.y);
            }
            centerOfMass = center / agents.Count;

            // Center Distace
            float dis = 0;
            foreach (modCCPAgent agent in agents)
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
    
    private void Awake()
    {
        m_activeAgentsParent = transform.Find("Agents").Find("Active");
        m_inactiveAgentsParent = transform.Find("Agents").Find("Inactive");
        m_agents = new List<Transform>();
        interactionManager = transform.Find("InteractionObjects").GetComponent<InteractionManager>();
        
        m_spawnAreas = new List<Collider>();
        foreach (Transform area in transform.Find("SpawnAreas"))
            m_spawnAreas.Add(area.GetComponent<Collider>());
        
        interconnectionData = new Dictionary<int, InterconnectionData>();
        m_availableKeys = Enumerable.Range(0, m_numberOfAgents + 1).ToList();
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeAgents();
        ChangEnvironmentWeights();
    }

    private Vector3 GetDominantVector(int dominantIndex)
    {
        float dominantValue = Random.Range(0.75f, 1.0f);
        float firstNonDominantValue = Random.Range(0.0f, 1f - dominantValue);
        float secondNonDominantValue = 1.0f - dominantValue - firstNonDominantValue;

        if (dominantIndex == 0)
        {
            return new Vector3(dominantValue, firstNonDominantValue, secondNonDominantValue);
        }
        if (dominantIndex == 1)
        {
            return new Vector3(firstNonDominantValue, dominantValue, secondNonDominantValue);
        }
        if (dominantIndex == 2)
        {
            return new Vector3(firstNonDominantValue, secondNonDominantValue, dominantValue);
        }

        return new Vector3(0.33f, 0.33f, 0.33f);
    }
    
    private void ChangEnvironmentWeights()
    {
        /*Vector3 biasedOutput = new Vector3(0.33f, 0.33f, 0.33f);
        int r = UnityEngine.Random.Range(0, 3);
        if (r == 0)
        {
            biasedOutput = GetDominantVector(0);
        }else if (r == 1)
        {
            biasedOutput = GetDominantVector(1);
        }
        else
        {
            biasedOutput = GetDominantVector(2);
        }
        goalWeight = biasedOutput.x;
        groupWeight = biasedOutput.y;
        interactWeight = biasedOutput.z;
        interconnWeight = UnityEngine.Random.Range(0f, 1f);*/

        //int intR1 = UnityEngine.Random.Range(0, 21);
        float r1 = UnityEngine.Random.Range(0f, 1f);//intR1 * 0.05f;
        //int intR2 = UnityEngine.Random.Range(0, 21);
        float r2 = UnityEngine.Random.Range(0f, 1f);//intR2 * 0.05f;
        if (r1 > r2)
        {
            float temp = r1;
            r1 = r2;
            r2 = temp;
        }
        goalWeight = r1;
        groupWeight = r2 - r1;
        interactWeight = 1 - r2;
        //int intR3 = UnityEngine.Random.Range(0, 21);
        float r3 = UnityEngine.Random.Range(0f, 1f);//intR3 * 0.05f;
        interconnWeight = r3;
    }
    
    // Update is called once per frame
    void Update()
    {
        FillAgentsList();

        m_timer += Time.deltaTime;

        if (m_timer >= INTERVAL)
        {
            m_timer -= INTERVAL;
            ChangEnvironmentWeights();
        }
    }

    private void FixedUpdate()
    {
        UpdateInterconnectionData();
        ArrangeInterconnectionGroups();
    }

    private void InitializeAgents()
    {
        for (int i = 0; i < m_numberOfAgents; i++)
        {
            GameObject agent = Instantiate(m_agentPrefab, Vector3.zero, Quaternion.identity, m_inactiveAgentsParent);
            agent.name = "Agent_" + i;
            agent.gameObject.SetActive(false);
        }
    }

    private void ArrangeInterconnectionGroups()
    {
        if (m_nextGroupCount < 2)
            m_nextGroupCount = UnityEngine.Random.Range(2, 5);
        
        if(m_inactiveAgentsParent.childCount <= 1)
            return;
        if(m_inactiveAgentsParent.childCount < m_nextGroupCount)
            return;

        Vector3[] spawnPoints = GenerateRandomSpawnPositions(m_nextGroupCount);
        Vector3 goalPoint = GenerateGoalPos(spawnPoints[0]);

        int availableId = m_availableKeys[0];
        m_availableKeys.Remove(availableId);
        InterconnectionData interData = new InterconnectionData(availableId);
        interconnectionData.Add(availableId, interData);
        for (int i = m_nextGroupCount - 1; i >= 0; i--)
        {
            modCCPAgent agent = m_inactiveAgentsParent.GetChild(i).GetComponent<modCCPAgent>();
            interData.AddAgentToGroup(agent);
            agent.groupId = availableId;
            agent.m_startingPos = spawnPoints[i];
            agent.transform.position = spawnPoints[i];
            Vector3 noise = new Vector3(
                UnityEngine.Random.Range(-0.25f, 0.25f), 
                0f,
                UnityEngine.Random.Range(-0.25f, 0.25f)
            );
            agent.m_goalPos = goalPoint + noise;
            agent.transform.parent = m_activeAgentsParent;
            agent.deactivated = false;
            int rvoID = Simulator.Instance.addAgent(new RVO.Vector2(spawnPoints[i].x, spawnPoints[i].z));
            agent.m_rvoID = rvoID;
            Simulator.Instance.setAgentPrefVelocity(rvoID, new RVO.Vector2(0, 0));
            agent.gameObject.SetActive(true);
        }
        
        m_nextGroupCount = 0;
    }

    public void DeactivateAgent(modCCPAgent agent, int groupId)
    {
        if (interconnectionData.Count > 0)
        {
            int groupCount = interconnectionData[groupId].RemoveAgentFromGroup(agent);
            if (groupCount == 0)
            {
                interconnectionData.Remove(groupId);
                m_availableKeys.Add(groupId);
            }
        }
        agent.transform.parent = m_inactiveAgentsParent;
        if (agent.m_rvoID >= 0)
            Simulator.Instance.delAgent(agent.m_rvoID);
        agent.m_rvoID = -1;
        agent.gameObject.SetActive(false);
    }

    private void FillAgentsList()
    {
        m_agents.Clear();
        foreach (Transform agent in m_activeAgentsParent)
        {
            m_agents.Add(agent);
        }
    }

    private void UpdateInterconnectionData()
    {
        foreach (var data in interconnectionData)
            data.Value.UpdateInterconnectionParameters();
    }
    
    public Vector3[] GenerateRandomSpawnPositions(int quantity)
    {
        Vector3[] retPoints = new Vector3[quantity];

        bool positionsFound = false;
        do
        {
            int randAreaIndex = UnityEngine.Random.Range(0, m_spawnAreas.Count);
            Bounds area = m_spawnAreas[randAreaIndex].bounds;
            Vector3 point = new Vector3(
                UnityEngine.Random.Range(area.min.x, area.max.x),
                0f,
                UnityEngine.Random.Range(area.min.z, area.max.z)
            );
            // Check if the area around the point is free.
            Collider[] hitColliders = Physics.OverlapSphere(point, 1f * ((float)quantity / 2), m_agentsLayer);
            if (hitColliders.Length == 0)
            {
                // If only one point need, return the center point
                if (quantity == 1){
                    retPoints[0] = point;
                    return retPoints;
                }
                // Else return a list of points around the center point
                for (int i = 0; i < quantity; i++)
                {
                    float angle = i * 360f / quantity;
                    float angleRad = Mathf.Deg2Rad * angle;
                    float x = 1f * Mathf.Cos(angleRad);
                    float z = 1f * Mathf.Sin(angleRad);
                    Vector3 pointPos = new Vector3(x, 0, z);
                    retPoints[i] = point + pointPos;
                }
                positionsFound = true;
            }
        } while (positionsFound == false);

        return retPoints;
    }
    
    public Vector3 GenerateGoalPos(Vector3 spawnPos)
    {
        Vector3 goalPos = spawnPos;
        do 
        {
            goalPos = GenerateRandomSpawnPositions(1)[0];
        }while(Vector3.Distance(spawnPos, goalPos) < 16f);

        return goalPos;
    }
    
    public float Normalize(float value, float originalMin, float originalMax, float targetMin, float targetMax)
    {
        // Normalize to 0-1 range
        float normalizedValue = (value - originalMin) / (originalMax - originalMin);
        // Scale and shift to new range
        float rescaledValue = normalizedValue * (targetMax - targetMin) + targetMin;
        return rescaledValue;
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
        if (m_activeAgentsParent.childCount < 2)
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

        foreach (Transform otherAgent in m_activeAgentsParent)
        {
            float dist = Vector3.Distance(otherAgent.position, currentPos);
            if (dist <= m_maxNeighbourDistance)
            {
                closeAgents++;
                neighboursPoints.Add(otherAgent.position);
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
}
