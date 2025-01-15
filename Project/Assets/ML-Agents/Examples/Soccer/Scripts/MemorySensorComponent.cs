using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MemorySensorComponent : MonoBehaviour
{

    public RayPerceptionSensorComponent3D rayPerceptionSensor;
    public MemorySensor createSensor()
    {
        AgentSoccer agent = GetComponent<AgentSoccer>();
        MemorySensor sensor = new MemorySensor(rayPerceptionSensor, agent);
        return sensor;
    }

    private void OnEnable()
    {
        if(rayPerceptionSensor == null)
        {
            Debug.LogError("MemorySensorComponent requires a RayPerceptionSensorComponent3D component.");
        }
    }

}
