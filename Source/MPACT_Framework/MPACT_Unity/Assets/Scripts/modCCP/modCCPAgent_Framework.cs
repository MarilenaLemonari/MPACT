using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using RVO;
using Vector2 = UnityEngine.Vector2;

public class modCCPAgent_Framework : Unity.MLAgents.Agent
{
    // Framework Params
    public int m_frame;
    [SerializeField] private int m_endFrame;
    public List<float> timesteps;
    public List<Vector3> trajectorylist;
    public List<float> rotationList;
    public List<float[]> paramsList;
    private bool m_hasInitialized;
    private EnvironmentSetup_modCCP m_environmentSetup;
    
    public int m_rvoID = -1;
    private DecisionRequester m_decisionRequester;
    public Rigidbody m_rb;
    [Header("Episode")] [SerializeField] private int m_episodeStep;
    [Header("Movement")] [SerializeField] private float m_maxSpeed = 2.25f;
    public float m_currentSpeed;
    public Vector2 m_currentVelocity;
    private RVO.Vector2 m_targetPosition;
    private RVO.Vector2 m_preferredVelocity;
    [Header("Goal")] public Vector3 m_startingPos;
    public Vector3 m_goalPos;
    [SerializeField] private float m_goalDistance;
    private float m_initialGoalDistance;
    private Vector3 m_goalVector;
    [SerializeField] private float m_currentGoalDistance;
    [SerializeField] private float m_currentGoalAngle;

    [Header("Group/Interact")] [SerializeField]
    private float m_distanceToClosestInteraction;

    [SerializeField] private float m_distanceToClosestAgent;
    [SerializeField] private int m_numberOfNeighbours;
    [Header("Interconnection")] public int groupId = -1;
    public float m_interconnDistance;
    [SerializeField] private float m_interconnAngle;
    [SerializeField] private float m_interconnSpeedVariance;
    private bool m_canMove = true;
    
    [Header("CCP Weights")] 
    public float m_goalWeight;
    public float m_groupWeight;
    public float m_interactionWeight;
    public float m_interconnWeight;

    public void SetGoalPosition(Vector3 pos) { 
        m_goalPos = pos;
    }

    public void RotateToGoal()
    {
        Vector3 initialGoalVector = (m_goalPos - transform.position).normalized * 0.01f;
        Simulator.Instance.setAgentVelocity(m_rvoID, new RVO.Vector2(initialGoalVector.x, initialGoalVector.z));
        transform.forward = new Vector3(initialGoalVector.x, 0, initialGoalVector.z).normalized;
    }

    public void SetEndFrame(int frame) { m_endFrame = frame;}
    
    private void SetProfile()
    {
        Room currentRoom = RoomManager.Instance.DetectCurrentRoom(transform.position);
        BehaviorProfile profile = currentRoom.GetCurrentRoomProfile(transform.position);
        m_goalWeight = profile.goal;
        m_groupWeight = profile.group;
        m_interactionWeight = profile.interaction;
        m_interconnWeight = profile.connection;
    }
    
    public override void Initialize()
    {
        m_environmentSetup = EnvironmentSetup_modCCP.Instance;
        m_rb = GetComponent<Rigidbody>();
        timesteps = new List<float>();
        trajectorylist = new List<Vector3>();
        rotationList = new List<float>();
        paramsList = new List<float[]>();
    }

    private void Update()
    {
        if (m_rvoID >= 0)
        {
            RVO.Vector2 pos = Simulator.Instance.getAgentPosition(m_rvoID);
            transform.position = new Vector3(pos.x(), transform.position.y, pos.y());

            Vector2 currentVelocity = Vector2.zero;
            RVO.Vector2 RVOVelocity = Simulator.Instance.getAgentVelocity(m_rvoID);
            if (Math.Abs(RVOVelocity.x()) > 0.01f || Math.Abs(RVOVelocity.y()) > 0.01f)
                transform.forward = new Vector3(RVOVelocity.x(), 0, RVOVelocity.y()).normalized;

            Vector3 temp = transform.InverseTransformDirection(new Vector3(RVOVelocity.x(), 0, RVOVelocity.y()));
            currentVelocity = new Vector2(temp.x, temp.z);
            m_currentVelocity = currentVelocity;
            m_currentSpeed = m_currentVelocity.magnitude;
        }
    }

    private void FixedUpdate()
    {
        if(SimulationState.Instance.Running == false)
            return;
        
        SetProfile();
        m_frame += 1;

        if (m_environmentSetup.saveRoutes && m_frame >= 1)
        {
            int startFrame = Mathf.RoundToInt(m_environmentSetup.startSeconds / 0.04f);
            if (m_frame >= startFrame)
            {
                timesteps.Add(m_frame * 0.04f);
                trajectorylist.Add(transform.localPosition);
                rotationList.Add(transform.localEulerAngles.y);
            }

            float[] frameParams = {
                m_goalWeight,
                m_groupWeight,
                m_interactionWeight,
                m_interconnWeight
            };
            paramsList.Add(frameParams);
        }

        int endFrame = Mathf.RoundToInt(m_environmentSetup.endSeconds / 0.04f);
        if((endFrame > 0 && m_frame >= endFrame) || (m_environmentSetup.saveRoutes && m_environmentSetup.saveNow))
            DeactivateAgent();
        if(m_frame >= m_endFrame && m_currentGoalDistance < 1f)
            DeactivateAgent();
        
        m_episodeStep = StepCount;
    }

    //Run every time a new episode starts
    public override void OnEpisodeBegin()
    {
        if (!m_hasInitialized) {
            m_startingPos = transform.position;
            transform.position = m_startingPos;
            transform.LookAt(new Vector3(m_goalPos.x, transform.position.y, m_goalPos.z));
            m_goalDistance = Vector3.Distance(transform.position, m_goalPos);
            m_hasInitialized = true;
        }
        
        m_rb.velocity = Vector3.zero;
        m_rb.centerOfMass = new Vector3(0f, 1f, 0f);

        m_goalDistance = Vector3.Distance(transform.position, m_goalPos);
        m_initialGoalDistance = m_goalDistance;
        m_currentGoalDistance = Vector3.Distance(transform.position, m_goalPos);
        m_currentGoalAngle = Vector3.SignedAngle(transform.forward, m_goalVector, Vector3.up);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        //Movement
        sensor.AddObservation(m_currentVelocity.normalized); // 2
        sensor.AddObservation(m_currentVelocity.magnitude / m_maxSpeed); // 1

        //Goal
        float goalDistanceNorm = 1;
        if(m_initialGoalDistance > 0)
            goalDistanceNorm = Mathf.Clamp01(m_environmentSetup.NormalizeValue(m_currentGoalDistance, 0, 
            m_initialGoalDistance, 0, 1));
        float goalAngleNorm = m_currentGoalAngle / 180f;
        sensor.AddObservation(goalDistanceNorm); // 1
        sensor.AddObservation(goalAngleNorm); // 1

        // Interconnectivity
        float interconnDistance = Mathf.Clamp(m_environmentSetup.NormalizeValue(m_interconnDistance, 1f, 2.5f, 0, 1), 0f, 1f);
        sensor.AddObservation(interconnDistance); // 1
        float interconnAngle = m_interconnAngle / 180f;
        sensor.AddObservation(interconnAngle); // 1
        float interconnSpeedVariance = Mathf.Clamp(m_interconnSpeedVariance, 0f, 1f);
        sensor.AddObservation(interconnSpeedVariance); // 1

        //Weights
        sensor.AddObservation(m_goalWeight); // 1
        sensor.AddObservation(m_groupWeight); // 1
        sensor.AddObservation(m_interactionWeight); // 1
        sensor.AddObservation(m_interconnWeight); // 1
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (SimulationState.Instance.Running == false)
        {
            Simulator.Instance.setAgentPrefVelocity(m_rvoID, new RVO.Vector2(0,0));
            return;   
        }

        // Move the agent using the action.
        MoveAgent(actionBuffers.ContinuousActions);
        // Assign behavior rewards at each step
        AssignBehaviorsRewards();
    }
    
    private void MoveAgent(ActionSegment<float> act)
    {
        if (!m_canMove)
        {
            Simulator.Instance.setAgentPrefVelocity(m_rvoID, new RVO.Vector2(0, 0));
            return;
        }

        float distance = (m_environmentSetup.NormalizeValue(act[0], -1f, 1f, -0.3f, 1f) * m_maxSpeed);
        if (distance <= 0f) distance = 0.01f;
        float angle = act[1] * 45f;

        // Calculate target velocity based on actions
        RVO.Vector2 currentPosition = new RVO.Vector2(transform.position.x, transform.position.z);
        Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
        Vector3 offset = rotatedDirection * distance;
        m_targetPosition = currentPosition + new RVO.Vector2(offset.x, offset.z);
        m_preferredVelocity = m_targetPosition - currentPosition;

        if (RVOMath.abs(m_preferredVelocity) > m_maxSpeed)
            m_preferredVelocity = RVOMath.normalize(m_preferredVelocity) * m_maxSpeed;

        // Assign reward for smooth movement
        AssignSmoothReward();
        
        Simulator.Instance.setAgentPrefVelocity(m_rvoID, m_preferredVelocity);
    }
    
    private void AssignSmoothReward()
    {
        RVO.Vector2 deltaV = Simulator.Instance.getAgentVelocity(m_rvoID) - m_preferredVelocity;
        float smoothReward = -0.015f * (RVOMath.abs(deltaV) / m_maxSpeed);
        AddReward(smoothReward);
    }

    private void AssignBehaviorsRewards()
    {
        m_goalVector = m_goalPos - transform.position;
        m_currentGoalDistance = Vector3.Distance(transform.position, m_goalPos);
        m_currentGoalAngle = Vector3.SignedAngle(transform.forward, m_goalVector, Vector3.up);

        if (m_currentGoalDistance <= 1f){
            m_maxSpeed = 0.25f;
        }

        //Goal Arrival Reward
        if (m_currentGoalDistance <= m_environmentSetup.goalDistance)// && m_frame >= deactivateFrame)
        {
            float stepsVariable = Mathf.Clamp((float)(MaxStep - StepCount) / (float)MaxStep, 0f, 1f);
            AddReward(+5.0f * m_goalWeight * stepsVariable);
            if(m_environmentSetup.PositionIsInBounds(m_goalPos))
                print("Goal");//EpisodeEnded();
            else
            {
                m_canMove = false;
            }
        }

        //Moving towards goal reward
        if (m_currentGoalDistance <= m_goalDistance)
        {
            AddReward(+0.0025f * m_goalWeight);
            m_goalDistance = m_currentGoalDistance;
            if (Math.Abs(m_currentGoalAngle) <= 30f)
                AddReward(+0.0025f * m_goalWeight);
            float speedReward = +0.0025f * m_goalWeight * (1f - ((m_maxSpeed - m_currentSpeed) / m_maxSpeed));
            AddReward(speedReward);
        }

        Vector3[] neighbourData = m_environmentSetup.GetCloserAgent(transform.position);
        Vector3 closerAgent = neighbourData[0];
        Vector3 groupCenter = neighbourData[1];
        //Debug.DrawLine(transform.position, groupCenter, Color.white);
        m_numberOfNeighbours = Mathf.RoundToInt(neighbourData[2].x);

        //Group reward
        if (m_groupWeight > 0)
        {
            m_distanceToClosestAgent = Vector3.Distance(transform.position, closerAgent);
            float dotGroup = Vector3.Dot(transform.forward, (groupCenter - transform.position).normalized);
            if (m_distanceToClosestAgent <= m_environmentSetup.groupingDistance && dotGroup >= 0.5f 
                && m_numberOfNeighbours <= m_environmentSetup.maxNeighbours && m_numberOfNeighbours > 0)
            {
                // Get a part of reward first. Get the another part based on speed, and the last one if stays stationary.
                AddReward(+0.0025f * m_groupWeight);
                float stationaryReward = +0.005f * m_groupWeight * ((m_maxSpeed - m_currentSpeed) / m_maxSpeed);
                AddReward(stationaryReward);
                if(m_currentSpeed <= 0.1f)
                    AddReward(+0.0025f * m_groupWeight);
            }
        }

        //Interacting with objects reward
        if (m_interactionWeight > 0)
        {
            Vector3 closerInteractionPoint = m_environmentSetup.GetCloserInteraction(transform.position);
            //Debug.DrawLine(transform.position, closerInteractionPoint, Color.green);
            m_distanceToClosestInteraction = Vector3.Distance(transform.position, closerInteractionPoint);
            float dotInteract =
                Vector3.Dot(transform.forward, (closerInteractionPoint - transform.position).normalized);
            if (m_distanceToClosestInteraction <= m_environmentSetup.interactionDistance && dotInteract >= 0.5f
                && m_numberOfNeighbours <= m_environmentSetup.maxNeighbours)
            {
                // Get a part of reward first. Get the another part based on speed, and the last one if stays stationary.
                AddReward(+0.01f * m_interactionWeight);
                float stationaryReward = +0.005f * m_interactionWeight * ((m_maxSpeed - m_currentSpeed) / m_maxSpeed);
                AddReward(stationaryReward);
                if(m_currentSpeed <= 0.1f)
                    AddReward(+0.0025f * m_interactionWeight);
            }
        }

        //Interconnection rewards
        //[1, 2.5]
        float desiredDistance = m_environmentSetup.NormalizeValue((1f - m_interconnWeight), 0f, 1f, 1f, 2.5f);
        if (m_environmentSetup.interconnectionData[groupId].agents.Count > 1)
        {
            m_interconnDistance = m_environmentSetup.interconnectionData[groupId].averageDistance;
            Vector3 interconnectionGroupCenter =
                m_environmentSetup.interconnectionData[groupId].centerOfMass - transform.position;
            m_interconnAngle = Vector3.SignedAngle(transform.forward, interconnectionGroupCenter, Vector3.up);
            m_interconnSpeedVariance = m_environmentSetup.interconnectionData[groupId].speedVariance;

            float distanceToDesired = Math.Abs(m_interconnDistance - desiredDistance);
            if (distanceToDesired <= 0.5f && m_interconnSpeedVariance <= 0.05f)
                AddReward(+0.01f);
            else
                AddReward(-0.005f);
        }
        else
        {
            m_interconnDistance = 0;
            m_interconnAngle = 0;
            m_interconnSpeedVariance = 0;
        }

            //Add a negative reward to each step to make agent execute the appropriate behaviour
        float stepReward = -0.005f - (m_goalWeight * 0.0025f);
        AddReward(stepReward);
    }

    //Move agent using keyboard just for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("Interaction"))
        {
            //EpisodeEnded();       
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Agent collide with obstacle
        if (m_episodeStep >= 30)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                //Debug.Log("Obstacle Collision");
                AddReward(-5.0f);
                //EpisodeEnded();       
            }
            else if (other.gameObject.CompareTag("Interaction"))
            {
                //Debug.Log("Interaction Collision");
                AddReward(-3.0f);
                //EpisodeEnded();    
            }
        }
    }

    public void DeactivateAgent()
    {
        print(this);
        if (m_environmentSetup.saveRoutes)
        {
            saveRoute();
        }
        m_environmentSetup.RemoveAgent(this, groupId);
        Destroy(this.gameObject);
    }
    
    public void saveRoute()
    {
        string filePath = m_environmentSetup.savePath + "/" + gameObject.name + ".csv";
        StreamWriter writer = new StreamWriter(filePath);

        float offSetX = 0f;
        float offSetZ = 0f;
        float scaler = 1;
        if (m_environmentSetup.gameObject.name.Contains("Study"))
        {
            offSetX = -15f;
            offSetZ = +10f;
            scaler = 2;
        }

        float timestepAlign = m_environmentSetup.currentStep - m_environmentSetup.stepSeconds;
        
        for (int i = 4; i < timesteps.Count; i += 1)
        {
            string row = Math.Round((timesteps[i] - timestepAlign),2) + ";" + (trajectorylist[i].x + offSetX)/scaler + ";" + (trajectorylist[i].z + offSetZ)/scaler + ";" +
                         rotationList[i] + ";" + paramsList[i][0] + ";" + paramsList[i][1] + ";" + paramsList[i][2] + ";" +
                         paramsList[i][3];
            writer.WriteLine(row);
        }
        writer.Flush();
        writer.Close();
    }
    
    private void OnDrawGizmosSelected()
    {
        //Visualize lines for debugging
        Debug.DrawLine(transform.position, m_goalPos, Color.red);
        Debug.DrawLine(transform.position, new Vector3(m_targetPosition.x(), 0, m_targetPosition.y()), Color.yellow);
    }
}