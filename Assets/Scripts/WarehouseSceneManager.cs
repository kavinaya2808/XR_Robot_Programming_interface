// ============================================================================
// WarehouseSceneManager.cs - Warehouse Simulation Scene Manager
// ============================================================================
//
// PURPOSE:
// Manages the Warehouse Simulation scene:
// - Applies control mode from GameSettings
// - Handles game start/stop
// - Connects to existing robots in the scene
//
// SETUP:
// 1. Add to empty "WarehouseSceneManager" GameObject in 10_WarehouseSim scene
// 2. Robots should already exist in the scene (from your working setup)
//
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WarehouseSceneManager : MonoBehaviour
{
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    [Header("Robot References")]
    [Tooltip("Existing robots in scene (auto-finds if empty)")]
    public List<GameObject> robots = new List<GameObject>();
    
    [Header("Control")]
    [Tooltip("Apply control mode from GameSettings on start")]
    public bool applyControlModeOnStart = true;
    
    [Tooltip("Auto-start robots (or wait for manual Start call)")]
    public bool autoStartRobots = true;
    
    [Tooltip("Delay before starting robots (gives time for scene to load)")]
    public float startDelay = 1f;
    
    [Header("UI References")]
    [Tooltip("In-game pause menu")]
    public GameObject pauseMenu;
    
    [Tooltip("Status text")]
    public TMPro.TextMeshProUGUI statusText;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // ========================================================================
    // RUNTIME STATE
    // ========================================================================
    
    private bool robotsStarted = false;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Start()
    {
        Log("WarehouseSceneManager starting...");
        
        // Find robots if not assigned
        FindRobots();
        
        // Apply control mode
        if (applyControlModeOnStart)
        {
            ApplyControlMode();
        }
        
        // Auto-start or wait
        if (autoStartRobots)
        {
            StartCoroutine(DelayedStart());
        }
    }
    
    void Update()
    {
        // Check for pause key (Escape or Menu button)
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }
    
    // ========================================================================
    // ROBOT MANAGEMENT
    // ========================================================================
    
    private void FindRobots()
    {
        if (robots.Count > 0) return; // Already have robots
        
        // Find by TurtlebotCoverageAgent
        var agents = FindObjectsOfType<TurtlebotCoverageAgent>();
        foreach (var agent in agents)
        {
            robots.Add(agent.gameObject);
            Log($"Found robot: {agent.gameObject.name}");
        }
        
        // Also try AGVController if no agents found
        if (robots.Count == 0)
        {
            var controllers = FindObjectsOfType<RosSharp.Control.AGVController>();
            foreach (var ctrl in controllers)
            {
                if (!robots.Contains(ctrl.gameObject))
                {
                    robots.Add(ctrl.gameObject);
                    Log($"Found robot (via AGVController): {ctrl.gameObject.name}");
                }
            }
        }
        
        Log($"Found {robots.Count} robots total");
    }
    
    private void ApplyControlMode()
    {
        if (GameSettings.Instance == null)
        {
            Log("GameSettings not found, using existing control modes");
            return;
        }
        
        var controlMode = GameSettings.Instance.GetAGVControlMode();
        Log($"Applying control mode: {controlMode}");
        
        foreach (var robot in robots)
        {
            if (robot == null) continue;
            
            // Find and set AGVController mode
            var agv = robot.GetComponent<RosSharp.Control.AGVController>();
            if (agv == null)
                agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            
            if (agv != null)
            {
                agv.mode = controlMode;
                Log($"Set {robot.name} to {controlMode} mode");
            }
            
            // Configure ML agent if present
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
                        behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.Default;
                    }
                    else if (controlMode == RosSharp.Control.ControlMode.Keyboard)
                    {
                        behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
                    }
                }
            }
            
            // Add BatteryModel if not present
            if (robot.GetComponent<BatteryModel>() == null)
            {
                robot.AddComponent<BatteryModel>();
                Log($"Added BatteryModel to {robot.name}");
            }
        }
    }
    
    // ========================================================================
    // PUBLIC API
    // ========================================================================
    
    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(startDelay);
        StartRobots();
    }
    
    /// <summary>
    /// Start robot exploration
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
        
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.StartGame();
        }
        
        // Enable all agents
        foreach (var robot in robots)
        {
            if (robot == null) continue;
            
            var agent = robot.GetComponentInChildren<TurtlebotCoverageAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }
            
            var agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            if (agv != null)
            {
                agv.enabled = true;
            }
        }
        
        // Update UI
        if (statusText != null)
            statusText.text = "Exploring...";
    }
    
    /// <summary>
    /// Stop robot exploration
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
        
        // Stop movement
        foreach (var robot in robots)
        {
            if (robot == null) continue;
            
            var agv = robot.GetComponentInChildren<RosSharp.Control.AGVController>();
            if (agv != null)
            {
                agv.RobotInput(0, 0);
            }
        }
        
        if (statusText != null)
            statusText.text = "Stopped";
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.TogglePause();
            
            // Toggle time scale
            Time.timeScale = GameSettings.Instance.GamePaused ? 0f : 1f;
            
            // Show/hide pause menu
            if (pauseMenu != null)
                pauseMenu.SetActive(GameSettings.Instance.GamePaused);
        }
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        StopRobots();
        SceneManager.LoadScene("00_BootstrapMenu");
    }
    
    /// <summary>
    /// Restart the current scene
    /// </summary>
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // ========================================================================
    // DEBUG
    // ========================================================================
    
    private void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[WarehouseSceneManager] {message}");
    }
}

