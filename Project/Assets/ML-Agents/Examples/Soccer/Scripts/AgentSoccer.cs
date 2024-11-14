using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    public GameObject ball;
    public float currentSize = 3f;

    public Transform ballTransform;


    public Transform ownGoal;
    public Transform opponentGoal;

    private List<AgentSoccer> nearbyAgents;

    private int memorySize = 5;

    // Buffer to store past raycast observations
    private Queue<float[]> raycastObservationsBuffer;

    // Number of rays and observations per ray
    private int raysPerObservation = 5; // Adjust based on your raycast setup
    private int observationsPerRay = 2;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        base.Initialize();
        nearbyAgents = new List<AgentSoccer>();

        raycastObservationsBuffer = new Queue<float[]>(memorySize);

        // Add a Sphere Collider for "hearing range"
        SphereCollider hearingCollider = gameObject.AddComponent<SphereCollider>();
        hearingCollider.isTrigger = true;
        hearingCollider.radius = 6f; // Adjust radius based on how far agents should "hear"

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<AgentSoccer>();
        if (agent != null && agent != this)
        {
            nearbyAgents.Add(agent);
        }
    }

    // Trigger detection for agents exiting "hearing range"
    private void OnTriggerExit(Collider other)
    {
        var agent = other.GetComponent<AgentSoccer>();
        if (agent != null && agent != this)
        {
            nearbyAgents.Remove(agent);
        }
    }
    private float[] GetRaycastObservations()
    {
        List<float> observations = new List<float>();

        // Define raycast parameters
        int numRays = raysPerObservation;
        float rayLength = 20; // Adjust as needed
        float angleRange = 135; // Total angle range for rays
        float angleIncrement = angleRange / (numRays - 1);

        // Start angle
        float startAngle = -angleRange / 2;

        for (int i = 0; i < numRays; i++)
        {
            float angle = startAngle + i * angleIncrement;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, rayLength))
            {
                // Normalize distance
                float normalizedDistance = hit.distance / rayLength;

                // Encode hit tag as a number
                float tagEncoding = EncodeTag(hit.collider.tag);

                observations.Add(normalizedDistance);
                observations.Add(tagEncoding);
            }
            else
            {
                // No hit
                observations.Add(1f); // Max normalized distance
                observations.Add(0f); // No tag
            }

            // Optional: Visualize rays in the editor
            Debug.DrawRay(transform.position, direction * rayLength, Color.red, 0.01f, false);
        }
        // Ensure the observations array matches raysPerObservation * observationsPerRay
        if (observations.Count != raysPerObservation * observationsPerRay)
        {
            Debug.LogWarning("Mismatch in raycast observations count.");
        }
        return observations.ToArray();
    }

    private float EncodeTag(string tag)
    {
        switch (tag)
        {
            case "ball":
                return 1f;
            case "agent":
                return 2f;
            case "wall":
                return 3f;
            // Add other tags as needed
            default:
                return 0f;
        }
    }
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var headRotateDir = Vector3.zero; // New head rotation direction

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];
        var headRotateAxis = act[3]; // New action for head rotation
        var kickAxis = act[4];

        switch(kickAxis)
        {
            case 1:
                m_KickPower = 1.5f;
                Debug.Log("Kick");
                break;
            case 2:
                m_KickPower = 2f;
                Debug.Log("Hard Kick");
                break;
        }

        // Moving forward and backward
        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        // Moving right and left
        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        // Body rotation (left and right)
        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        // Head rotation (independent of movement direction)
        switch (headRotateAxis)
        {
            case 1:
                headRotateDir = Vector3.up * -1f;
                break;
            case 2:
                headRotateDir = Vector3.up * 1f;
                break;
        }

        // Apply body and head rotations
        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        transform.Rotate(headRotateDir, Time.deltaTime * 50f, Space.Self); // Slower rotation for the head

        // Apply movement
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        // Forward and backward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        // Rotate body
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        // Move right and left
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
        // Rotate head independently
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[3] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[3] = 2;
        }

        if(Input.GetKey(KeyCode.LeftShift))
        {
            discreteActionsOut[4] = 1;
        }
        if(Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[4] = 2;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }

        MoveAgent(actionBuffers.DiscreteActions);

        
    }




   
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
       
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Get current raycast observations
        float[] currentRaycastObservations = GetRaycastObservations();

        // Add current observations to the buffer
        raycastObservationsBuffer.Enqueue(currentRaycastObservations);

        // Ensure the buffer does not exceed the memory size
        if (raycastObservationsBuffer.Count > memorySize)
        {
            raycastObservationsBuffer.Dequeue();
        }

        // Collect observations from the buffer
        foreach (var observation in raycastObservationsBuffer)
        {
            sensor.AddObservation(observation);
        }


        int maxNearbyAgents = 3; // Maximum number of agents to consider
        // Add observations for nearby agents
        for (int i = 0; i < maxNearbyAgents; i++)
        {
            if (i < nearbyAgents.Count)
            {
                AgentSoccer agent = nearbyAgents[i];
                Vector3 relativePositionToAgent = agent.transform.position - transform.position;
                sensor.AddObservation(relativePositionToAgent.normalized); // Relative direction
                sensor.AddObservation(relativePositionToAgent.magnitude); // Distance
                sensor.AddObservation(agent.agentRb.velocity); // Velocity
            }
            else
            {
                // Pad with zeroes if fewer agents are in hearing range
                sensor.AddObservation(Vector3.zero); // Placeholder for direction
                sensor.AddObservation(0f); // Placeholder for distance
                sensor.AddObservation(Vector3.zero); // Placeholder for velocity
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 1.0f); // Set default to 1.0f


    }
}
