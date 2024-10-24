using Unity.MLAgents.Sensors;
using UnityEngine;

public class FrontRayCastSensor : ISensor
{
    private string sensorName;
    private int rayCount;
    private float rayLength;
    private LayerMask layerMask;
    private Transform agentTransform;

    // Constructor to initialize the sensor
    public FrontRayCastSensor(string name, Transform transform, int rayCount, float rayLength, LayerMask mask)
    {
        this.sensorName = name;
        this.rayCount = rayCount;
        this.rayLength = rayLength;
        this.layerMask = mask;
        this.agentTransform = transform;
    }

    public string GetName() => sensorName;

    public int[] GetObservationShape()
    {
        // Shape of the observations: rayCount * 2 (for each ray's distance and hit object tag)
        return new[] { rayCount * 2 };
    }

    public void Reset() { }

    public int Write(ObservationWriter writer)
    {
        float rayAngleStep = 180f / rayCount; // Rays only in front
        int index = 0;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -90f + rayAngleStep * i; // Adjusting to fire in the forward direction
            Vector3 direction = Quaternion.Euler(0, angle, 0) * agentTransform.forward;

            if (Physics.Raycast(agentTransform.position, direction, out RaycastHit hit, rayLength, layerMask))
            {
                // Write distance and object tag to the observation
                writer[index++] = hit.distance / rayLength; // Normalize the distance
                writer[index++] = hit.collider.gameObject.tag.GetHashCode(); // Store tag of hit object
            }
            else
            {
                // No hit, record max distance
                writer[index++] = 1f; // max distance is 1 when normalized
                writer[index++] = 0;  // No object hit
            }
        }

        return rayCount * 2; // Return total number of values written
    }

    public void Update() { }

    public SensorCompressionType GetCompressionType() => SensorCompressionType.None;

    public byte[] GetCompressedObservation() => null;

    public BuiltInSensorType GetBuiltInSensorType() => BuiltInSensorType.None;
}

