// ============================================================================
// RealRoomMRManager.cs - Real Room Mixed Reality Scene Manager
// ============================================================================
//
// PURPOSE:
// Manages the Real Room MR scene where:
// - Passthrough shows the real world
// - MRUK/OVRSceneManager loads room geometry
// - Robots are spawned on the real floor
// - Colliders are added to walls/furniture for LiDAR
//
// This works WITH Quest3RoomSetup.cs - it coordinates the setup flow
// and applies GameSettings (control mode, etc.)
//
// SETUP:
// 1. Add to empty "RealRoomMRManager" GameObject in 20_RealRoomMR scene
// 2. Assign robot prefabs to spawn
// 3. Optionally link to existing Quest3RoomSetup
//
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class RealRoomMRManager : MonoBehaviour
{
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    [Header("Room Setup")]
    [Tooltip("Reference to Quest3RoomSetup (auto-finds if null)")]
    public Quest3RoomSetup roomSetup;
    
    [Tooltip("Wait for room to be ready before spawning robots")]
    public bool waitForRoom = true;
    
    [Tooltip("Maximum time to wait for room setup (seconds)")]
    public float roomSetupTimeout = 30f;
    
    [Header("Robot Spawning")]
    [Tooltip("Robot prefabs to spawn (uses Quest3RoomSetup's prefabs if empty)")]
    public GameObject[] robotPrefabs;
    
    [Tooltip("Default spawn positions if room detection fails (relative to origin)")]
    public Vector3[] fallbackSpawnPositions = new Vector3[]
    {
        new Vector3(0, 0.05f, 0),
        new Vector3(1, 0.05f, 0)
    };
    
    [Tooltip("Height above floor to spawn robots")]
    public float spawnHeight = 0.05f;
    
    [Header("Control")]
    [Tooltip("Apply control mode from GameSettings on spawn")]
    public bool applyControlModeOnSpawn = true;
    
    [Tooltip("Auto-start robots (or wait for user to press Start)")]
    public bool autoStartRobots = false;
    
    [Header("UI References")]
    [Tooltip("Loading/status UI to show during setup")]
    public GameObject loadingUI;
    
    [Tooltip("Status text component")]
    public TMPro.TextMeshProUGUI statusText;
    
    [Tooltip("Start button (shown when setup complete)")]
    public GameObject startButton;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // ========================================================================
    // RUNTIME STATE
    // ========================================================================
    
    private List<GameObject> spawnedRobots = new List<GameObject>();
    private bool setupComplete = false;
    private bool robotsStarted = false;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Start()
    {
        Log("RealRoomMRManager starting...");
        
        // Show loading UI
        SetLoadingUI(true, "Initializing...");
        
        // Find Quest3RoomSetup if not assigned
        if (roomSetup == null)
        {
            roomSetup = FindObjectOfType<Quest3RoomSetup>();
            if (roomSetup != null)
            {
                Log("Found Quest3RoomSetup in scene");
            }
        }
        
        // Start setup coroutine
        StartCoroutine(SetupSequence());
    }
    
    // ========================================================================
    // SETUP SEQUENCE
    // ========================================================================
    
    private IEnumerator SetupSequence()
    {
        // Step 1: Wait for room detection
        if (waitForRoom && roomSetup != null)
        {
            SetLoadingUI(true, "Detecting room...");
            Log("Waiting for room detection...");
            
            float startTime = Time.time;
            while (!roomSetup.IsRoomReady && (Time.time - startTime) < roomSetupTimeout)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            if (roomSetup.IsRoomReady)
            {
                Log("Room detection complete!");
            }
            else
            {
                Log("Room detection timed out, using fallback positions");
            }
        }
        
        // Step 2: Spawn robots
        SetLoadingUI(true, "Spawning robots...");
        yield return new WaitForSeconds(0.5f);
        SpawnRobots();
        
        // Step 3: Apply control mode from GameSettings
        if (applyControlModeOnSpawn)
        {
            ApplyControlMode();
        }
        
        // Step 4: Setup complete
        setupComplete = true;
        Log("Setup complete!");
        
        if (autoStartRobots)
        {
            StartRobots();
            SetLoadingUI(false, "");
        }
        else
        {
            SetLoadingUI(true, "Press START to begin");
            if (startButton != null)
                startButton.SetActive(true);
        }
    }
    
    // ========================================================================
    // ROBOT SPAWNING
    // ========================================================================
    
    private void SpawnRobots()
    {
        // Determine spawn positions
        Vector3[] positions = GetSpawnPositions();
        
        // Get prefabs (from this script or from Quest3RoomSetup)
        GameObject[] prefabsToSpawn = robotPrefabs;
        if ((prefabsToSpawn == null || prefabsToSpawn.Length == 0) && roomSetup != null)
        {
            prefabsToSpawn = roomSetup.robotPrefabs;
        }
        
        // If still no prefabs, try to find existing robots in scene
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
        {
            Log("No robot prefabs assigned, looking for existing robots...");
            MoveExistingRobots(positions);
            return;
        }
        
        // Spawn robots
        int numRobots = Mathf.Min(prefabsToSpawn.Length, positions.Length);
        int robotsToSpawn = GameSettings.Instance != null 
            ? Mathf.Min(GameSettings.Instance.NumberOfRobots, numRobots)
            : numRobots;
        
        for (int i = 0; i < robotsToSpawn; i++)
        {
            if (prefabsToSpawn[i] == null) continue;
            
            GameObject robot = Instantiate(prefabsToSpawn[i], positions[i], Quaternion.identity);
            robot.name = $"Robot_{i + 1}";
            spawnedRobots.Add(robot);
            
            Log($"Spawned {robot.name} at {positions[i]}");
        }
    }
    
    private void MoveExistingRobots(Vector3[] positions)
    {
        // Find robots by tag or name pattern
        var existingRobots = new List<GameObject>();
        
        // Try tag first
        var taggedRobots = GameObject.FindGameObjectsWithTag("Robot");
        if (taggedRobots != null && taggedRobots.Length > 0)
        {
            existingRobots.AddRange(taggedRobots);
        }
        
        // Also look for TurtlebotCoverageAgent components
        var agents = FindObjectsOfType<TurtlebotCoverageAgent>();
        foreach (var agent in agents)
        {
            if (!existingRobots.Contains(agent.gameObject))
                existingRobots.Add(agent.gameObject);
        }
        
        // Move to spawn positions
        for (int i = 0; i < existingRobots.Count && i < positions.Length; i++)
        {
            existingRobots[i].transform.position = positions[i];
            existingRobots[i].transform.rotation = Quaternion.identity;
            spawnedRobots.Add(existingRobots[i]);
            Log($"Moved {existingRobots[i].name} to {positions[i]}");
        }
    }
    
    private Vector3[] GetSpawnPositions()
    {
        // Try to get floor position from room setup
        Vector3 floorPos = Vector3.zero;
        
        if (roomSetup != null && roomSetup.IsRoomReady)
        {
            floorPos = roomSetup.FloorPosition;
        }
        
        // Create positions relative to floor/origin
        int numPositions = GameSettings.Instance?.NumberOfRobots ?? 2;
        Vector3[] positions = new Vector3[numPositions];
        
        // Spread robots in a line or circle
        float spacing = 1.0f;
        for (int i = 0; i < numPositions; i++)
        {
            if (i < fallbackSpawnPositions.Length)
            {
                positions[i] = floorPos + fallbackSpawnPositions[i];
            }
            else
            {
                // Generate additional positions in a circle
                float angle = (360f / numPositions) * i * Mathf.Deg2Rad;
                positions[i] = floorPos + new Vector3(
                    Mathf.Cos(angle) * spacing,
                    spawnHeight,
                    Mathf.Sin(angle) * spacing
                );
            }
        }
        
        return positions;
    }
    
    // ========================================================================
    // CONTROL MODE APPLICATION
    // ========================================================================
    
    private void ApplyControlMode()
    {
        if (GameSettings.Instance == null)
        {
            Log("GameSettings not found, using default control mode");
            return;
        }
        
        var controlMode = GameSettings.Instance.GetAGVControlMode();
        Log($"Applying control mode: {controlMode}");
        
        // Apply to all robots
        foreach (var robot in spawnedRobots)
        {
            // Find AGVController
            var agv = robot.GetComponent<RosSharp.Control.AGVController>();
            if (agv == null)
                agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            
            if (agv != null)
            {
                agv.mode = controlMode;
                Log($"Set {robot.name} to {controlMode} mode");
            }
            
            // Also configure ML agent behavior type based on control mode
            var agent = robot.GetComponent<TurtlebotCoverageAgent>();
            if (agent == null)
                agent = robot.GetComponentInChildren<TurtlebotCoverageAgent>();
            
            if (agent != null)
            {
                var behaviorParams = agent.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
                if (behaviorParams != null)
                {
                    if (controlMode == RosSharp.Control.ControlMode.ML)
                    {
                        // Use inference or default (for training)
                        behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.Default;
                    }
                    else
                    {
                        // Disable ML agent actions when in other modes
                        behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
                    }
                }
            }
        }
    }
    
    // ========================================================================
    // PUBLIC API
    // ========================================================================
    
    /// <summary>
    /// Start the robots (called by Start button or automatically)
    /// </summary>
    public void StartRobots()
    {
        if (robotsStarted)
        {
            Log("Robots already started");
            return;
        }
        
        Log("Starting robots...");
        robotsStarted = true;
        
        // Tell GameSettings
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.StartGame();
        }
        
        // Enable robot agents/controllers
        foreach (var robot in spawnedRobots)
        {
            // Enable agent
            var agent = robot.GetComponent<TurtlebotCoverageAgent>();
            if (agent == null)
                agent = robot.GetComponentInChildren<TurtlebotCoverageAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }
            
            // Enable controller
            var agv = robot.GetComponent<RosSharp.Control.AGVController>();
            if (agv == null)
                agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            if (agv != null)
            {
                agv.enabled = true;
            }
        }
        
        // Hide loading UI
        SetLoadingUI(false, "");
        
        Log("Robots started!");
    }
    
    /// <summary>
    /// Stop/pause the robots
    /// </summary>
    public void StopRobots()
    {
        if (!robotsStarted) return;
        
        Log("Stopping robots...");
        robotsStarted = false;
        
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.StopGame();
        }
        
        // Stop robot movement
        foreach (var robot in spawnedRobots)
        {
            var agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            if (agv != null)
            {
                agv.RobotInput(0, 0); // Stop movement
            }
        }
    }
    
    /// <summary>
    /// Return to menu
    /// </summary>
    public void ReturnToMenu()
    {
        StopRobots();
        SceneManager.LoadScene("00_BootstrapMenu");
    }
    
    // ========================================================================
    // UI HELPERS
    // ========================================================================
    
    private void SetLoadingUI(bool show, string message)
    {
        if (loadingUI != null)
            loadingUI.SetActive(show);
        
        if (statusText != null)
            statusText.text = message;
        
        if (startButton != null && show && message.Contains("START"))
            startButton.SetActive(true);
        else if (startButton != null && !show)
            startButton.SetActive(false);
    }
    
    // ========================================================================
    // DEBUG
    // ========================================================================
    
    private void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[RealRoomMRManager] {message}");
    }
}

