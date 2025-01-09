using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class SoccerTool : EditorWindow
{
    // Field configuration
    private GameObject soccerFieldPrefab;
    private int rows = 3;
    private int columns = 3;
    private float spacing = 50f;

    private Vector3 startPosition = Vector3.zero;


    // Agent data
    private List<GameObject> agents = new List<GameObject>();

    // Agent configuration
    private bool visionDecouple = true;
    private bool useMemory = true;
    private bool useSoundObservations = true;

    


    private void OnEnable()
    {
       
        LoadAgentData();
        
    }
    private void LoadAgentData()
    {
        agents.Clear();
        agents.AddRange(GameObject.FindGameObjectsWithTag("purpleAgent"));
        agents.AddRange(GameObject.FindGameObjectsWithTag("blueAgent"));

        Debug.Log($"Found {agents.Count} agents.");
    }



    [MenuItem("Tools/Soccer Configuration")]
    public static void ShowWindow()
    {
        GetWindow<SoccerTool>("Soccer Configuration");
    }

    private void OnGUI()
    {
        GUILayout.Label("Soccer Field Configuration", EditorStyles.boldLabel);

        soccerFieldPrefab = (GameObject)EditorGUILayout.ObjectField("Soccer Field Prefab", soccerFieldPrefab, typeof(GameObject), false);
        rows = EditorGUILayout.IntField("Rows", rows);
        columns = EditorGUILayout.IntField("Columns", columns);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);

        if (GUILayout.Button("Generate Field"))
        {
            SpawnFields();
        }
        if(GUILayout.Button("Delete fields"))
        {
            DeleteFields();
        }


        createSensorConfigMenu();
        
    }

    private void createSensorConfigMenu()
    {
        bool previousVision = visionDecouple;
        bool previousMemory = useMemory;
        bool previousSound = useSoundObservations;

        visionDecouple = EditorGUILayout.Toggle("Vision Decouple", visionDecouple);
        if (visionDecouple)
        {
            EditorGUILayout.HelpBox("Vision Decoupling is enabled. The head of the agents will rotate independently from the body.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Vision Decoupling is disabled. The head of the agents will rotate with the body.", MessageType.Info);
        }
        if (previousVision != visionDecouple)
        {
            if (visionDecouple)
            {
                EditorGUILayout.HelpBox("Vision Decoupling is enabled. The head of the agents will rotate independently from the body.", MessageType.Warning);
                toggleVisionDecouple(true);
            }
            else
            {
                EditorGUILayout.HelpBox("Vision Decoupling is disabled. The head of the agents will rotate with the body.", MessageType.Info);
                toggleVisionDecouple(false);
            }
        }






        useMemory = EditorGUILayout.Toggle("Use Memory", useMemory);
        if (useMemory)
        {
            EditorGUILayout.HelpBox("Memory is enabled. The agents will have a memory sensor.", MessageType.Info);
           
        }
        else
        {
            EditorGUILayout.HelpBox("Memory is disabled. The agents will not have a memory sensor.", MessageType.Warning);
           
        }
        if (previousMemory != useMemory)
        {
            if (useMemory)
            {
                EditorGUILayout.HelpBox("Memory is enabled. The agents will have a memory sensor.", MessageType.Info);
                toggleMemory(true);
            }
            else
            {
                EditorGUILayout.HelpBox("Memory is disabled. The agents will not have a memory sensor.", MessageType.Warning);
                toggleMemory(false);
            }
        }

        useSoundObservations = EditorGUILayout.Toggle("Use Sound Observations", useSoundObservations);
        if (useSoundObservations)
        {
            EditorGUILayout.HelpBox("Sound Observations are enabled. The agents will have a sound sensor.", MessageType.Info);
           
        }
        else
        {
            EditorGUILayout.HelpBox("Sound Observations are disabled. The agents will not have a sound sensor.", MessageType.Warning);
            
        }
        if (previousSound!=useSoundObservations)
        {
            if (useSoundObservations)
            {
                EditorGUILayout.HelpBox("Sound Observations are enabled. The agents will have a sound sensor.", MessageType.Info);
                toggleSoundObservations(true);
            }
            else
            {
                EditorGUILayout.HelpBox("Sound Observations are disabled. The agents will not have a sound sensor.", MessageType.Warning);
                toggleSoundObservations(false);
            }
        }
    }

    private void SpawnFields(){
        if (soccerFieldPrefab == null)
        {
            Debug.LogError("Please assign a soccer field prefab");
            return;
        }
        if(rows <= 0 || columns <= 0)
        {
            Debug.LogError("Rows and columns must be greater than 0");
            return;
        }
        if(GameObject.Find("Spawned Objects") != null)
        {
            Debug.LogError("Please delete the existing fields before spawning new ones");
            return;
        }

        GameObject parentObject = new GameObject("Spawned Objects");


        // Spawn the fields in a grid-like style
        for(int i = 0; i < rows; i++)
        {
            for(int j=0; j < columns; j++)
            {
                Vector3 spawnPosition = startPosition + new Vector3(i * spacing, 0, j * spacing);
                GameObject spawnedField = Instantiate(soccerFieldPrefab, spawnPosition, Quaternion.identity);
                spawnedField.transform.position = spawnPosition;
                spawnedField.transform.parent = parentObject.transform;
            }
        }

        LoadAgentData();

        visionDecouple = true;
        toggleVisionDecouple(true);
        useMemory = true;
        toggleMemory(true);
        useSoundObservations = true;
        toggleSoundObservations(true);

        Debug.Log($"Successfully activated {rows * columns} soccer fields in the formation {rows} x {columns}.");
    }


    //Delete the spawned fields if there are any
    private void DeleteFields()
    {
        GameObject parentObject = GameObject.Find("Spawned Objects");
        if (parentObject != null)
        {
            agents.Clear();
            DestroyImmediate(parentObject);

            visionDecouple = true;
            useMemory = true;
            useSoundObservations = true;

            Debug.Log("Deleted all spawned fields.");
        }
        else
        {
            Debug.Log("No spawned fields to delete.");
        }   
    }

    private void toggleVisionDecouple(bool enabled)
    {
        foreach (GameObject item in agents)
        {
            AgentSoccer agentSoccerScript = item.GetComponent<AgentSoccer>();
            agentSoccerScript.visionDecouple = enabled;
        }
    }

    private void toggleMemory(bool enabled)
    {
        foreach (GameObject item in agents)
        {
            AgentSoccer agentSoccerScript = item.GetComponent<AgentSoccer>();
            agentSoccerScript.useMemory = enabled;
        }
    }

    private void toggleSoundObservations(bool enabled)
    {
        foreach (GameObject item in agents)
        {
            AgentSoccer agentSoccerScript = item.GetComponent<AgentSoccer>();
            agentSoccerScript.useSoundObservations = enabled;
        }
    }

   
}
