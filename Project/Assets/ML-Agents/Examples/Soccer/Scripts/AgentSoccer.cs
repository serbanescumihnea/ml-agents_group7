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
    public Transform ballTransform;

    public Transform ownGoal;
    public Transform opponentGoal;

    private List<AgentSoccer> nearbyAgents;

    private int numObservationStacks = 3;
    private Queue<float[]> observationStack;

    public Transform headTransform;

    // Number of rays and observations per ray
    private int raysPerObservation = 5; // Adjust based on your raycast setup
    private int observationsPerRay = 8; // Corrected to match actual observations per ray

    private int maxNearbyAgents = 3; // Maximum number of agents to consider

    private float prevDistanceToBall;

    // Constants for normalization
    private const float maxAgentDistance = 25f;
    private const float maxAgentVelocity = 10f;
    private const float maxObjectVelocity = 15f;
    private const float maxRaycastDistance = 25f;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();

        observationStack = new Queue<float[]>(numObservationStacks);

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
            initialPos = new Vector3(transform.position.x - 5f, 0.5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, 0.5f, transform.position.z);
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

        // Add a Sphere Collider for "hearing range"
        SphereCollider hearingCollider = gameObject.AddComponent<SphereCollider>();
        hearingCollider.isTrigger = true;
        hearingCollider.radius = 6f;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // Initialize previous distance to ball
        prevDistanceToBall = Vector3.Distance(transform.position, ballTransform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<AgentSoccer>();
        if (agent != null && agent.name != this.name)
        {
            if (!nearbyAgents.Contains(agent))
            {
                nearbyAgents.Add(agent);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var agent = other.GetComponent<AgentSoccer>();
        if (agent != null && agent != this)
        {
            if (nearbyAgents.Contains(agent))
            {
                nearbyAgents.Remove(agent);
            }
        }
    }

    private float[] GetRaycastObservations()
    {
        List<float> observations = new List<float>();

        // Define raycast parameters
        int numRays = raysPerObservation;
        float rayLength = 25f; // Adjust as needed
        float angleRange = 135f; // Total angle range for rays
        float angleIncrement = angleRange / (numRays - 1);

        // Start angle
        float startAngle = -angleRange / 2;

        for (int i = 0; i < numRays; i++)
        {
            float angle = startAngle + i * angleIncrement;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * headTransform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, rayLength))
            {
                // Normalize distance
                float normalizedDistance = hit.distance / rayLength;

                // Encode hit tag as a number
                float tagEncoding = EncodeTag(hit.collider.tag) / 6f; // Normalize tag encoding between 0 and 1

                observations.Add(normalizedDistance);
                observations.Add(tagEncoding);

                // Add normalized position of the observed object
                Vector3 positionDifference = hit.collider.transform.position - transform.position;
                Vector3 normalizedPosition = positionDifference / maxRaycastDistance;
                observations.Add(Mathf.Clamp(normalizedPosition.x, -1f, 1f));
                observations.Add(Mathf.Clamp(normalizedPosition.y, -1f, 1f));
                observations.Add(Mathf.Clamp(normalizedPosition.z, -1f, 1f));

                // Add normalized velocity of the observed object (if it has a Rigidbody)
                Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
                if (hitRigidbody != null)
                {
                    Vector3 normalizedVelocity = hitRigidbody.velocity / maxObjectVelocity;
                    observations.Add(Mathf.Clamp(normalizedVelocity.x, -1f, 1f));
                    observations.Add(Mathf.Clamp(normalizedVelocity.y, -1f, 1f));
                    observations.Add(Mathf.Clamp(normalizedVelocity.z, -1f, 1f));
                }
                else
                {
                    // Add zero velocity if no Rigidbody is present
                    observations.Add(0f);
                    observations.Add(0f);
                    observations.Add(0f);
                }
            }
            else
            {
                // No hit
                observations.Add(1f); // Max normalized distance
                observations.Add(0f); // No tag

                // Add zero position and velocity for no hit
                observations.Add(0f);
                observations.Add(0f);
                observations.Add(0f);
                observations.Add(0f);
                observations.Add(0f);
                observations.Add(0f);
            }
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
            case "blueAgent":
                return 2f;
            case "purpleAgent":
                return 3f;
            case "wall":
                return 4f;
            case "blueGoal":
                return 5f;
            case "purpleGoal":
                return 6f;
            default:
                return 0f;
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var headRotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];
        var headRotateAxis = act[3];
        var kickAxis = act[4];

        switch (kickAxis)
        {
            case 1:
                m_KickPower = 1.5f;
                break;
            case 2:
                m_KickPower = 2f;
                break;
        }

        // Moving forward and backward
        switch (forwardAxis)
        {
            case 1:
                dirToGo += transform.forward * m_ForwardSpeed;
                break;
            case 2:
                dirToGo += transform.forward * -m_ForwardSpeed;
                break;
        }

        // Moving right and left
        switch (rightAxis)
        {
            case 1:
                dirToGo += transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo += transform.right * -m_LateralSpeed;
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

        transform.Rotate(rotateDir, Time.deltaTime * 100f);

        // Apply head rotation to headTransform
        headTransform.Rotate(headRotateDir, Time.deltaTime * 50f, Space.Self);

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

        if (Input.GetKey(KeyCode.LeftShift))
        {
            discreteActionsOut[4] = 1;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[4] = 2;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
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
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        List<float> currentObservations = new List<float>();

        // Get normalized raycast observations
        float[] raycastObservations = GetRaycastObservations();
        currentObservations.AddRange(raycastObservations);

        // Normalize observations for nearby agents
        for (int i = 0; i < maxNearbyAgents; i++)
        {
            if (i < nearbyAgents.Count)
            {
                AgentSoccer agent = nearbyAgents[i];
                Vector3 relativePositionToAgent = agent.transform.position - transform.position;

                // Normalize distance
                float normalizedDistance = relativePositionToAgent.magnitude / maxAgentDistance;
                normalizedDistance = Mathf.Clamp(normalizedDistance, 0f, 1f);

                // Normalize relative position
                Vector3 normalizedDirection = relativePositionToAgent.normalized;

                // Normalize agent velocity
                Vector3 normalizedVelocity = agent.agentRb.velocity / maxAgentVelocity;
                normalizedVelocity.x = Mathf.Clamp(normalizedVelocity.x, -1f, 1f);
                normalizedVelocity.y = Mathf.Clamp(normalizedVelocity.y, -1f, 1f);
                normalizedVelocity.z = Mathf.Clamp(normalizedVelocity.z, -1f, 1f);

                currentObservations.AddRange(new float[]
                {
                    normalizedDirection.x,
                    normalizedDirection.y,
                    normalizedDirection.z,
                    normalizedDistance,
                    normalizedVelocity.x,
                    normalizedVelocity.y,
                    normalizedVelocity.z
                });
            }
            else
            {
                // Pad with zeroes
                currentObservations.AddRange(new float[]
                {
                    0f, 0f, 0f, // Direction
                    0f,         // Distance
                    0f, 0f, 0f  // Velocity
                });
            }
        }

        observationStack.Enqueue(currentObservations.ToArray());

        // Maintain observation stack size
        if (observationStack.Count > numObservationStacks)
        {
            observationStack.Dequeue();
        }

        // Add observations to the sensor
        foreach (var observation in observationStack)
        {
            sensor.AddObservation(observation);
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and velocity
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        transform.position = initialPos;
        transform.rotation = Quaternion.Euler(0, 0, 0);

        // Reset the previous distance to the ball
        prevDistanceToBall = Vector3.Distance(transform.position, ballTransform.position);
    }
}
