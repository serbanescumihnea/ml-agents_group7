using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SoccerAgent : Agent
{
    public LayerMask rayLayerMask; // Specify the layers to be detected by the rays
    private FrontRayCastSensor frontSensor;

    public override void Initialize()
    {
        // Register the custom sensor with 3 rays, 20 units long, only in front of the agent
        frontSensor = new FrontRayCastSensor("FrontRayCastSensor", transform, 3, 20f, rayLayerMask);
        AddSensor(frontSensor);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // add more observations?
        sensor.AddObservation(transform.localPosition);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Handle the agent's actions based on the received vectorAction
        // Example: Moving the agent
        var moveX = vectorAction[0];
        var moveZ = vectorAction[1];

        Vector3 move = new Vector3(moveX, 0, moveZ);
        transform.localPosition += move * Time.deltaTime;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position or environment at the beginning of each episode
        transform.localPosition = Vector3.zero;
    }
}

