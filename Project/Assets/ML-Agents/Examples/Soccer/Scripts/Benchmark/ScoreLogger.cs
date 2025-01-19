using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents.Policies;

/// <summary>
/// Logger script to track scores over time and write to a file.
/// </summary>
public class ScoreLogger : MonoBehaviour
{
    [Tooltip("Reference to the ScoreTracker script.")]
    [SerializeField]
    private ScoreTracker scoreTracker;

    private const int MAX_LOGGING_GOALS = 1000;


    private List<ScoreLogEntry> logEntries = new List<ScoreLogEntry>();
    private float elapsedTime = 0f;
    private string logFilePath;

    private string modelAName;
    private string modelBName;

    /// <summary>
    /// Initialize the logger.
    /// </summary>
    private void Start()
    {
        scoreTracker = GameObject.Find("ScoreTracker").GetComponent<ScoreTracker>();

        modelAName = GameObject.FindGameObjectWithTag("blueAgent").GetComponent<BehaviorParameters>().Model.name; 
        modelBName = GameObject.FindGameObjectWithTag("purpleAgent").GetComponent<BehaviorParameters>().Model.name;

        logFilePath = Path.Combine(Application.persistentDataPath, $"{modelAName}vs{modelBName}.csv");
        File.WriteAllText(logFilePath, $"TimeStamp,{modelAName},{modelBName}\n"); // Initialize the file with headers.
    }

    /// <summary>
    /// FixedUpdate is used to log scores at regular time intervals.
    /// </summary>
    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        LogCurrentScores();

        if(scoreTracker.blueGoals >= MAX_LOGGING_GOALS || scoreTracker.purpleGoals >= MAX_LOGGING_GOALS)
        {
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the editor.
            #else
                    Application.Quit(); // Exits the application in standalone builds.
            #endif
        }
    }




    /// <summary>
    /// Logs the current scores with a timestamp and writes to a file.
    /// </summary>
    private void LogCurrentScores()
    {
        if (scoreTracker != null)
        {
            var entry = new ScoreLogEntry(elapsedTime, scoreTracker.blueGoals, scoreTracker.purpleGoals);
            logEntries.Add(entry);
            AppendLogToFile(entry);
        }
    }

    /// <summary>
    /// Appends a log entry to the file.
    /// </summary>
    /// <param name="entry">The log entry to append.</param>
    private void AppendLogToFile(ScoreLogEntry entry)
    {
        string logLine = $"{entry.TimeStamp:F2},{entry.BlueGoals},{entry.PurpleGoals}\n";
        File.AppendAllText(logFilePath, logLine);
    }

    /// <summary>
    /// Retrieves the logged data for external use.
    /// </summary>
    /// <returns>A list of logged score entries.</returns>
    public List<ScoreLogEntry> GetLogEntries()
    {
        return new List<ScoreLogEntry>(logEntries); // Return a copy for safety.
    }

    /// <summary>
    /// Score log entry structure.
    /// </summary>
    public struct ScoreLogEntry
    {
        public float TimeStamp { get; }
        public int BlueGoals { get; }
        public int PurpleGoals { get; }

        public ScoreLogEntry(float timeStamp, int blueGoals, int purpleGoals)
        {
            TimeStamp = timeStamp;
            BlueGoals = blueGoals;
            PurpleGoals = purpleGoals;
        }
    }
}


