using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents.Policies;
using Unity.Sentis;

public class SoccerTool : EditorWindow
{
    // Constants
    private const int PLAYING_FIELDS_ROWS = 8;
    private const int PLAYING_FIELDS_COLUMNS = 8;


    // Configuration Fields
    private GameObject soccerFieldPrefab;
    private int rows = 3;
    private int columns = 3;
    private float spacing = 50f;
    private Vector3 startPosition = Vector3.zero;

    // Agent and Team Data
    private List<GameObject> agents = new List<GameObject>();
    private List<GameObject> blueTeamAgents = new List<GameObject>();
    private List<GameObject> purpleTeamAgents = new List<GameObject>();

    // Agent Configuration
    private bool visionDecouple = true;
    private bool useMemory = true;
    private bool useSoundObservations = true;

    // Training Mode
    private bool isTraining = false;

    // ONNX Models
    public ModelAsset blueTeamModel;
    public ModelAsset purpleTeamModel;

    private bool blueVision = false, blueMemory = false, blueSound = false;
    private bool purpleVision = false, purpleMemory = false, purpleSound = false;

    [MenuItem("Tools/Soccer Configuration")]
    public static void ShowWindow()
    {
        GetWindow<SoccerTool>("Soccer Configuration");
    }

    private void OnEnable()
    {
        LoadAgentData();
    }

    private void OnGUI()
    {
        DrawHeader("Soccer Field Configuration");

        soccerFieldPrefab = (GameObject)EditorGUILayout.ObjectField("Soccer Field Prefab", soccerFieldPrefab, typeof(GameObject), false);

        isTraining = EditorGUILayout.Toggle("Training Mode", isTraining);
        if (isTraining)
        {
            DrawTrainingMenu();
        }
        else
        {
            DrawPlayingMenu();
        }
    }

    private void DrawTrainingMenu()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Table Configuration", EditorStyles.boldLabel);
        rows = EditorGUILayout.IntField("Rows", rows);
        columns = EditorGUILayout.IntField("Columns", columns);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Functionality", EditorStyles.boldLabel);
        DrawAgentSettings();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Field")) SpawnFields();
        if (GUILayout.Button("Delete Fields")) DeleteFields();

        
    }

    private void DrawPlayingMenu()
    {
        DrawHeader("ONNX Models", 15);

        DrawTeamModelSettings("Blue Team", ref blueTeamModel, ref blueVision, ref blueMemory, ref blueSound);
        DrawTeamModelSettings("Purple Team", ref purpleTeamModel, ref purpleVision, ref purpleMemory, ref purpleSound);

        if (GUILayout.Button("Generate 1v1 Environments")) GenerateBenchmarkEnvironments();

        if (GameObject.Find("Spawned Objects") != null && GUILayout.Button("Delete Fields")) DeleteFields();

        if (GameObject.Find("Logger")==null && GUILayout.Button("Use Logger"))
        {
            AddLogger();
        }
    }

    private void AddLogger()
    {
        GameObject parent = new GameObject("Logger");
        parent.AddComponent<ScoreLogger>();
    }

    private void DrawTeamModelSettings(string teamName, ref ModelAsset model, ref bool vision, ref bool memory, ref bool sound)
    {
        EditorGUILayout.LabelField(teamName, EditorStyles.boldLabel);
        model = (ModelAsset)EditorGUILayout.ObjectField("Model", model, typeof(ModelAsset), false);

        vision = EditorGUILayout.Toggle("Vision", vision);
        memory = EditorGUILayout.Toggle("Memory", memory);
        sound = EditorGUILayout.Toggle("Sound", sound);
        EditorGUILayout.Space();
    }

    private void DrawAgentSettings()
    {
        visionDecouple = EditorGUILayout.Toggle("Vision Decouple", visionDecouple);
        useMemory = EditorGUILayout.Toggle("Use Memory", useMemory);
        useSoundObservations = EditorGUILayout.Toggle("Use Sound Observations", useSoundObservations);
    }

    private void DrawHeader(string title, int spacing = 20)
    {
        EditorGUILayout.Space(spacing);
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUILayout.Space();
    }

    private void LoadAgentData()
    {
        agents.Clear();
        agents.AddRange(GameObject.FindGameObjectsWithTag("purpleAgent"));
        agents.AddRange(GameObject.FindGameObjectsWithTag("blueAgent"));
        Debug.Log($"Found {agents.Count} agents.");
    }

    private void LoadTeamData()
    {
        purpleTeamAgents.Clear();
        purpleTeamAgents.AddRange(GameObject.FindGameObjectsWithTag("purpleAgent"));

        blueTeamAgents.Clear();
        blueTeamAgents.AddRange(GameObject.FindGameObjectsWithTag("blueAgent"));
    }

    private void SpawnFields()
    {
        if (ValidateFieldInputs())
        {
            DestroyImmediate(GameObject.Find("Logger"));

            GameObject parentObject = new GameObject("Spawned Objects");

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Vector3 spawnPosition = startPosition + new Vector3(i * spacing, 0, j * spacing);
                    GameObject field = Instantiate(soccerFieldPrefab, spawnPosition, Quaternion.identity);
                    field.transform.parent = parentObject.transform;
                }
            }

            LoadAgentData();
            ApplyAgentSettings();

            Debug.Log($"Generated {rows * columns} fields ({rows}x{columns}).");
        }
    }

    private void DeleteFields()
    {
        GameObject.DestroyImmediate(GameObject.Find("Logger")); 
        GameObject parentObject = GameObject.Find("Spawned Objects");
        if (parentObject != null)
        {
            DestroyImmediate(parentObject);
            ResetSettings();
            Debug.Log("All fields deleted.");
        }
        else
        {
            Debug.LogWarning("No fields to delete.");
        }
    }

    private void GenerateBenchmarkEnvironments()
    {
        GameObject parentObject = new GameObject("Spawned Objects");

        for (int i = 0; i < PLAYING_FIELDS_ROWS; i++)
        {
            for (int j = 0; j < PLAYING_FIELDS_COLUMNS; j++)
            {
                Vector3 spawnPosition = startPosition + new Vector3(i * spacing, 0, j * spacing);
                GameObject field = Instantiate(soccerFieldPrefab, spawnPosition, Quaternion.identity);
                field.transform.parent = parentObject.transform;
            }
        }

        LoadTeamData();
        ApplyTeamSettings();
    }

    private void ApplyAgentSettings()
    {
        ToggleAgentFeature(visionDecouple, agent => agent.visionDecouple = visionDecouple);
        ToggleAgentFeature(useMemory, agent => agent.useMemory = useMemory);
        ToggleAgentFeature(useSoundObservations, agent => agent.useSoundObservations = useSoundObservations);
    }

    private void ApplyTeamSettings()
    {
        ApplyModelSettings(blueTeamAgents, blueTeamModel, blueVision, blueMemory, blueSound);
        ApplyModelSettings(purpleTeamAgents, purpleTeamModel, purpleVision, purpleMemory, purpleSound);
    }

    private void ApplyModelSettings(List<GameObject> team, ModelAsset model, bool vision, bool memory, bool sound)
    {
        foreach (GameObject agent in team)
        {
            var behavior = agent.GetComponent<BehaviorParameters>();
            if (behavior != null) behavior.Model = model;

            var agentScript = agent.GetComponent<AgentSoccer>();
            if (agentScript != null)
            {
                agentScript.visionDecouple = vision;
                agentScript.useMemory = memory;
                agentScript.useSoundObservations = sound;
            }
        }
    }

    private void ToggleAgentFeature(bool enabled, System.Action<AgentSoccer> action)
    {
        foreach (var agent in agents)
        {
            var agentScript = agent.GetComponent<AgentSoccer>();
            if (agentScript != null) action(agentScript);
        }
    }

    private bool ValidateFieldInputs()
    {
        if (soccerFieldPrefab == null)
        {
            Debug.LogError("Assign a Soccer Field Prefab.");
            return false;
        }

        if (rows <= 0 || columns <= 0)
        {
            Debug.LogError("Rows and columns must be greater than 0.");
            return false;
        }

        if (GameObject.Find("Spawned Objects") != null)
        {
            Debug.LogError("Delete existing fields before spawning new ones.");
            return false;
        }

        return true;
    }

    private void ResetSettings()
    {
        agents.Clear();
        visionDecouple = useMemory = useSoundObservations = true;
        blueVision = blueMemory = blueSound = false;
        purpleVision = purpleMemory = purpleSound = false;
    }
}
