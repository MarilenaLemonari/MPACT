using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using RVO;
using Vector2 = UnityEngine.Vector2;

public class modCCPAgent : Unity.MLAgents.Agent
{
    public int m_rvoID = -1;
    [HideInInspector] public Manager m_manager;
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
    [SerializeField] private float m_episodeGoalDistance;
    private Vector3 m_goalVector;
    [SerializeField] private float m_currentGoalDistance;
    [SerializeField] private float m_currentGoalAngle;

    [Header("Group/Interact")] [SerializeField]
    private float m_distanceToClosestInteraction;

    [SerializeField] private float m_distanceToClosestAgent;
    [SerializeField] private int m_numberOfNeighbours;
    [Header("Interconnection")] public int groupId = -1;
    public bool deactivated = false;
    public float m_interconnDistance;
    [SerializeField] private float m_interconnAngle;
    [SerializeField] private float m_interconnSpeedVariance;

    [Header("CCP Weights")] 
    public float m_goalWeight;
    public float m_groupWeight;
    public float m_interactionWeight;
    public float m_interconnWeight;

    public override void Initialize()
    {
        m_rb = GetComponent<Rigidbody>();
        m_manager = transform.parent.parent.parent.GetComponent<Manager>();
    }

    private void Update()
    {
        m_goalWeight = m_manager.goalWeight;
        m_groupWeight = m_manager.groupWeight;
        m_interactionWeight = m_manager.interactWeight;
        m_interconnWeight = m_manager.interconnWeight;

        if (m_rvoID >= 0)
        {
            RVO.Vector2 pos = Simulator.Instance.getAgentPosition(m_rvoID);
            transform.position = new Vector3(pos.x(), transform.position.y, pos.y());

            Vector2 currentVelocity = Vector2.zero;
            RVO.Vector2 RVOVelocity = Simulator.Instance.getAgentVelocity(m_rvoID);
            if (Math.Abs(RVOVelocity.x()) > 0.015f || Math.Abs(RVOVelocity.y()) > 0.015f)
                transform.forward = new Vector3(RVOVelocity.x(), 0, RVOVelocity.y()).normalized;

            Vector3 temp = transform.InverseTransformDirection(new Vector3(RVOVelocity.x(), 0, RVOVelocity.y()));
            currentVelocity = new Vector2(temp.x, temp.z);
            m_currentVelocity = currentVelocity;
            m_currentSpeed = m_currentVelocity.magnitude;
        }
    }

    private void FixedUpdate()
    {
        if (StepCount >= (MaxStep - 10))
            EpisodeEnded();

        m_episodeStep = StepCount;
    }

    //Run every time a new episode starts
    public override void OnEpisodeBegin()
    {
        transform.position = m_startingPos;
        transform.LookAt(m_goalPos);
        float noiseAngle = UnityEngine.Random.Range(-60f, 60f);
        transform.Rotate(Vector3.up, noiseAngle);

        m_rb.velocity = Vector3.zero;
        m_goalDistance = Vector3.Distance(transform.position, m_goalPos);
        m_episodeGoalDistance = m_goalDistance;
        m_currentGoalDistance = m_goalDistance;
        m_currentGoalAngle = Vector3.SignedAngle(transform.forward, m_goalVector, Vector3.up);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if(m_goalDistance == 0)
            return;
        
        //Movement
        sensor.AddObservation(m_currentVelocity.normalized); // 2
        sensor.AddObservation(m_currentVelocity.magnitude / m_maxSpeed); // 1
        
        //Goal
        float goalDistanceNorm = Math.Clamp(
            m_manager.Normalize(m_currentGoalDistance, 0, m_episodeGoalDistance, 0, 1), 0, 1);
        float goalAngleNorm = m_currentGoalAngle / 180f;
        sensor.AddObservation(goalDistanceNorm); // 1
        sensor.AddObservation(goalAngleNorm); // 1

        // Interconnectivity
        float interconnDistance = Mathf.Clamp(m_manager.Normalize(m_interconnDistance, 1f, 2.5f, 0, 1), 0f, 1f);
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
        // Move the agent using the action.
        MoveAgent(actionBuffers.ContinuousActions);
        
        // Assign behavior rewards at each step
        AssignBehaviorsRewards();
    }

    private void MoveAgent(ActionSegment<float> act)
    {
        float distance = (m_manager.Normalize(act[0], -1f, 1f, -0.35f, 1f) * m_maxSpeed);
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

        //Goal Arrival Reward
        if (m_currentGoalDistance <= m_manager.goalDistanceThreshold)
        {
            float stepsVariable = (float)(MaxStep - StepCount) * 0.2f;
            AddReward(+0.01f * m_goalWeight * stepsVariable);
            EpisodeEnded();
        }

        //Moving towards goal reward
        if (m_currentGoalDistance <= m_goalDistance)
        {
            AddReward(+0.0025f * m_goalWeight);
            m_goalDistance = m_currentGoalDistance;
            if (Math.Abs(m_currentGoalAngle) <= 30f)
                AddReward(+0.0025f * m_goalWeight);
            float speedReward = +0.005f * m_goalWeight * (1f - ((m_maxSpeed - m_currentSpeed) / m_maxSpeed));
            AddReward(speedReward);
        }

        Vector3[] neighbourData = m_manager.GetCloserAgent(transform.position);
        Vector3 closerAgent = neighbourData[0];
        Vector3 groupCenter = neighbourData[1];
        //Debug.DrawLine(transform.position, groupCenter, Color.white);
        m_numberOfNeighbours = Mathf.RoundToInt(neighbourData[2].x);

        //Group reward
        if (m_groupWeight > 0)
        {
            m_distanceToClosestAgent = Vector3.Distance(transform.position, closerAgent);
            float dotGroup = Vector3.Dot(transform.forward, (groupCenter - transform.position).normalized);
            if (m_distanceToClosestAgent <= m_manager.groupDistanceThreshold && dotGroup >= 0.6f 
                && m_numberOfNeighbours <= m_manager.maxNeighbours && m_numberOfNeighbours > 0)
            {
                // Get a part of reward first. Get the another part based on speed, and the last one if stays stationary.
                AddReward(+0.0025f * m_groupWeight);
                float stationaryReward = +0.005f * m_groupWeight * ((m_maxSpeed - m_currentSpeed) / m_maxSpeed);
                AddReward(stationaryReward);
                if(m_currentSpeed <= m_manager.stationaryVelocityThreshold)
                    AddReward(+0.0025f * m_groupWeight);
            }
        }

        //Interacting with objects reward
        if (m_interactionWeight > 0)
        {
            Vector3 closerInteractionPoint = m_manager.interactionManager.GetCloserInteraction(transform.position);
            //Debug.DrawLine(transform.position, closerInteractionPoint, Color.green);
            m_distanceToClosestInteraction = Vector3.Distance(transform.position, closerInteractionPoint);
            float dotInteract =
                Vector3.Dot(transform.forward, (closerInteractionPoint - transform.position).normalized);
            if (m_distanceToClosestInteraction <= m_manager.interactionDistanceThreshold && dotInteract >= 0.5f
                && m_numberOfNeighbours <= m_manager.maxNeighbours)
            {
                // Get a part of reward first. Get the another part based on speed, and the last one if stays stationary.
                AddReward(+0.005f * m_interactionWeight);
                float stationaryReward = +0.005f * m_interactionWeight * ((m_maxSpeed - m_currentSpeed) / m_maxSpeed);
                AddReward(stationaryReward);
                float distanceReward = +0.0025f * m_interactionWeight *
                                       ((m_manager.interactionDistanceThreshold - m_distanceToClosestInteraction) /
                                        m_manager.interactionDistanceThreshold);
                AddReward(distanceReward);
                if(m_currentSpeed <= m_manager.stationaryVelocityThreshold)
                    AddReward(+0.0025f * m_interactionWeight);
            }
        }

        //Interconnection rewards
        //[1, 2.5]
        if (!deactivated)
        {
            float desiredDistance = m_manager.Normalize((1f - m_interconnWeight), 0f, 1f, 1f, 2.5f);
            if (m_manager.interconnectionData[groupId].agents.Count > 1)
            {
                m_interconnDistance = m_manager.interconnectionData[groupId].averageDistance;
                Vector3 interconnectionGroupCenter =
                    m_manager.interconnectionData[groupId].centerOfMass - transform.position;
                m_interconnAngle = Vector3.SignedAngle(transform.forward, interconnectionGroupCenter, Vector3.up);
                m_interconnSpeedVariance = m_manager.interconnectionData[groupId].speedVariance;

                float distanceToDesired = Math.Abs(m_interconnDistance - desiredDistance);
                if (distanceToDesired <= 0.5f && m_interconnSpeedVariance <= 0.05f)
                    AddReward(+0.02f);
                else
                    AddReward(-0.01f);
            }
            else
            {
                m_interconnDistance = 0f;
                m_interconnAngle = 0f;
                m_interconnSpeedVariance = 0f;
            }
        }

        //Add a negative reward to each step to make agent execute the appropriate behaviour
        float stepReward = -0.005f;
        AddReward(stepReward);
    }

    //Move agent using keyboard just for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }

    private void EpisodeEnded()
    {
        deactivated = true;
        m_manager.DeactivateAgent(this, groupId);
        EndEpisode();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Agent collide with obstacle
        if (m_episodeStep >= 30)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                //Debug.Log("Obstacle Collision");
                AddReward(-4.0f);
                EpisodeEnded();       
            }
            else if (other.gameObject.CompareTag("Interaction"))
            {
                //Debug.Log("Interaction Collision");
                AddReward(-2.0f);
                EpisodeEnded();    
            }
        }
    }

    private void OnDrawGizmos()
    {
        //Visualize lines for debugging
        Debug.DrawLine(transform.position, m_goalPos, Color.red);
        Debug.DrawLine(transform.position, new Vector3(m_targetPosition.x(), 0, m_targetPosition.y()), Color.yellow);
    }
}
