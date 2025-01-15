using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MemorySensor : ISensor
{

    RayPerceptionSensorComponent3D rayPerceptionSensor;
    AgentSoccer agent;


    public Queue<float[]> memory;
    public int capacity = 3;


    public MemorySensor(RayPerceptionSensorComponent3D rayPerceptionSensor, AgentSoccer agent)
    {
        this.rayPerceptionSensor = rayPerceptionSensor;
        memory = new Queue<float[]>();
        this.agent = agent;
    }

    public void Update()
    {
   
        RayPerceptionInput input = rayPerceptionSensor.GetRayPerceptionInput();
        RayPerceptionOutput output = RayPerceptionSensor.Perceive(input, false);

        

        List<float> currentOutputs = new List<float>();
        for(int i = 0; i < output.RayOutputs.Length; i++)
        {
            
            currentOutputs.Add(output.RayOutputs[i].HitTagIndex);
            currentOutputs.Add(output.RayOutputs[i].HitFraction);
           

        }
       

        if (memory.Count >= capacity)
        {
            memory.Dequeue();
        }
        memory.Enqueue(currentOutputs.ToArray());
    }
    public int Write(ObservationWriter writer)
    {
        int index = 0;
        foreach (float[] memoryEntry in memory)
        {
            foreach (float f in memoryEntry)
            {
                writer[index] = f;
                index++;
            }
        }
        while(index<GetExpectedObservationSize())
        {
            writer[index] = 0;
            index++;
            writer[index] = 0;
            index++;
        }
        return memory.Count;
    }

    public float[] getObservations()
    {
        List<float> currentObservations = new List<float>();
        foreach (float[] stack in memory)
        {
            
            currentObservations.AddRange(stack);
        }
  
        while (currentObservations.Count < GetExpectedObservationSize())
        {
  
            currentObservations.Add(0f);
        }
        return currentObservations.ToArray();
    }

    private int GetExpectedObservationSize()
    {
  
        return capacity * 11 * 2;
    }


 
    public ObservationSpec GetObservationSpec()
    {
        return ObservationSpec.Vector(capacity * 11 * 2);
    }

    public byte[] GetCompressedObservation()
    {
        return null;
    }

    public void Reset()
    {
        memory.Clear();
    }

    public CompressionSpec GetCompressionSpec()
    {
        return CompressionSpec.Default();
    }

    public string GetName()
    {
        return "MemorySensor";
    }
}
