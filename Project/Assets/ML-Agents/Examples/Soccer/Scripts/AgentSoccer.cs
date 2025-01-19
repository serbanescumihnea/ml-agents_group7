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


    public Transform headTransform;


    MemorySensor memorySensor;

    public bool visionDecouple = true;
    public bool useMemory = true;
    public bool useSoundObservations = true;



 

    // Constants for normalization
    private const float maxAgentDistance = 15f;


    int maxNearbyAgents = 3;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        
        if(m_ResetParams.GetWithDefault("vision", visionDecouple ? 1:0) == 1)
        {
            visionDecouple = true;
        }
        else
        {
            visionDecouple = false;
        }
      
        if(m_ResetParams.GetWithDefault("memory", useMemory ? 1 : 0) == 1)
        {
            useMemory = true;
        }
        else
        {
            useMemory = false;
        }
       
        if(m_ResetParams.GetWithDefault("sound", useSoundObservations ? 1 : 0) == 1)
        {
            useSoundObservations = true;
        }
        else
        {
            useSoundObservations = false;
        }

        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        m_BehaviorParameters.BrainParameters.VectorObservationSize = 0;
        m_BehaviorParameters.BrainParameters.ActionSpec = new ActionSpec(0, new int[] { 3, 3, 3});

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
        nearbyAgents = new List<AgentSoccer>();

       
        if (visionDecouple)
        {
            m_BehaviorParameters.BrainParameters.ActionSpec = new ActionSpec(0, new int[]{ 3, 3, 3, 3 });
            m_BehaviorParameters.BrainParameters.VectorObservationSize += 3;
        }


        if (useMemory) {
            memorySensor = this.GetComponent<MemorySensorComponent>().createSensor();
            m_BehaviorParameters.BrainParameters.VectorObservationSize += 66; 
        }
        if (useSoundObservations)
        {
            SphereCollider hearingCollider = gameObject.AddComponent<SphereCollider>();
            hearingCollider.isTrigger = true;
            hearingCollider.radius = 6f;
            m_BehaviorParameters.BrainParameters.VectorObservationSize += 9;
        }
      
       

       

       
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
        if (agent != null && agent.name != this.name)
        {
            if (nearbyAgents.Contains(agent))
            {
                nearbyAgents.Remove(agent);
            }
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
        if (visionDecouple) {
            var headRotateAxis = act[3];
            switch (headRotateAxis)
            {
                case 1:
                    headRotateDir = Vector3.up * -1f;
                    break;
                case 2:
                    headRotateDir = Vector3.up * 1f;
                    break;
            }

            // Get the current Y-axis rotation
            float currentYRotation = headTransform.localEulerAngles.y;

            // Normalize the angle to [-180, 180] range
            if (currentYRotation > 180f)
                currentYRotation -= 360f;

            // Calculate the new potential rotation
            float rotationDelta = headRotateDir.y * Time.deltaTime * 100f;
            float newYRotation = currentYRotation + rotationDelta;

            newYRotation = Mathf.Clamp(newYRotation, -90f, 90f);

            headTransform.localEulerAngles = new Vector3(
       headTransform.localEulerAngles.x,
       newYRotation,
       headTransform.localEulerAngles.z
   );
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);

        
       

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
        if (visionDecouple) { 
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                discreteActionsOut[3] = 1;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                discreteActionsOut[3] = 2;
            }
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
        base.CollectObservations(sensor);
        if (visionDecouple)
        {
            sensor.AddObservation(headTransform.transform.rotation.eulerAngles / 180f);
           
        }

        if (useMemory)
        {
            memorySensor.Update();
            if (memorySensor != null)
            {
                sensor.AddObservation(memorySensor.getObservations());

            }
        }
        if (useSoundObservations)
        {

            List<float> currentSoundObservations = new List<float>();
            for (int i = 0; i < maxNearbyAgents; i++)
            {
                if (i < nearbyAgents.Count)
                {
                    AgentSoccer agent = nearbyAgents[i];
                    float relativeDistance =  Vector3.Distance(agent.transform.localPosition,transform.localPosition);
                    Vector3 directionVector = (agent.transform.localPosition - transform.localPosition).normalized;


                    float normalizedDistance = relativeDistance / maxAgentDistance;
                    
                    // Normalize distance
                   

                    currentSoundObservations.AddRange(new float[]
                    {
                        normalizedDistance,
                        directionVector.x,
                        directionVector.y,
                    });
                }
                else
                {
                    // Pad with zeroes
                    currentSoundObservations.AddRange(new float[]
                    {
                    0f, 0f, 0f // Position
                    });
                }
            }
       
        sensor.AddObservation(currentSoundObservations);
        }
    }




    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and velocity
        if(useMemory)
            memorySensor.Reset();
        

        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        transform.position = initialPos;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        headTransform.rotation = Quaternion.Euler(0, 0, 0);
      

       
    
    }

  
}
