using UnityEngine;
using RosSharp.Control;

/// <summary>
/// Bridges telemetry data from the simulated robot (TelemetryPublisher) 
/// to the MR Dashboard (RobotTelemetry).
/// 
/// Since both projects are merged into one Unity instance, we can 
/// directly read values from the robot and write to the dashboard - 
/// no network needed!
/// 
/// SETUP:
/// 1. Add this script to robot1 and robot2
/// 2. Drag the robot's TelemetryPublisher to "Source Robot"
/// 3. Find the dashboard's RobotTelemetry object and drag to "Target Dashboard"
///    (Or leave empty and set targetRobotId to auto-find by ID)
/// </summary>
public class DirectTelemetryBridge : MonoBehaviour
{
    [Header("Source (The Simulated Robot)")]
    [Tooltip("The TelemetryPublisher on this robot that generates telemetry data")]
    public TelemetryPublisher sourceRobot;

    [Header("Target (The Dashboard Display)")]
    [Tooltip("The RobotTelemetry component that displays data. If empty, will auto-find by robotId")]
    public RobotTelemetry targetDashboard;
    
    [Tooltip("If targetDashboard is not set, search for RobotTelemetry with this ID")]
    public string targetRobotId = "Robot_1";

    [Header("Options")]
    [Tooltip("Also sync the transform position/rotation to dashboard object")]
    public bool syncTransform = false;
    
    [Tooltip("Show debug messages in console")]
    public bool debugLog = false;

    private bool initialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Auto-find source if not assigned
        if (sourceRobot == null)
        {
            sourceRobot = GetComponent<TelemetryPublisher>();
            if (sourceRobot == null)
                sourceRobot = GetComponentInChildren<TelemetryPublisher>();
        }

        if (sourceRobot == null)
        {
            Debug.LogError($"[DirectTelemetryBridge] No TelemetryPublisher found on {gameObject.name}!");
            return;
        }

        // Auto-find target by ID if not assigned
        if (targetDashboard == null && !string.IsNullOrEmpty(targetRobotId))
        {
            if (RobotTelemetry.Registry.TryGetValue(targetRobotId, out RobotTelemetry found))
            {
                targetDashboard = found;
                Debug.Log($"[DirectTelemetryBridge] Auto-found dashboard for '{targetRobotId}'");
            }
        }

        if (targetDashboard == null)
        {
            Debug.LogWarning($"[DirectTelemetryBridge] No target dashboard found for '{targetRobotId}'. " +
                           "Assign manually or ensure RobotTelemetry with matching ID exists.");
            return;
        }

        // Disable internal simulation on dashboard - we'll feed it real data
        targetDashboard.useSimulatedData = false;

        initialized = true;
        Debug.Log($"[DirectTelemetryBridge] Connected: {sourceRobot.robotName} → {targetDashboard.robotId}");
    }

    void Update()
    {
        // Try to initialize if not yet done (dashboard might load later)
        if (!initialized)
        {
            Initialize();
            if (!initialized) return;
        }

        if (sourceRobot == null || targetDashboard == null) return;

        // ============================================
        // SYNC TELEMETRY DATA: Robot → Dashboard
        // ============================================

        // 1. Position (from robot's base_link)
        if (sourceRobot.baseLink != null)
        {
            targetDashboard.position = sourceRobot.baseLink.position;
            
            // Optional: move the dashboard object to robot position
            if (syncTransform)
            {
                targetDashboard.transform.position = sourceRobot.baseLink.position;
                targetDashboard.transform.rotation = sourceRobot.baseLink.rotation;
            }
        }

        // 2. Speed (linear speed in m/s)
        targetDashboard.speed = sourceRobot.LinSpeed;
        targetDashboard.speed_mps = sourceRobot.LinSpeed * 100f;
        targetDashboard.taskEstimatedSecondsRemaining = targetDashboard.taskEstimatedSecondsRemaining;

        // 3. Battery percentage
        targetDashboard.batteryPercent = sourceRobot.Battery;

        // 4. Status/Mode
        // Map control mode to state string
        string mode = sourceRobot.Mode;
        if (sourceRobot.LinSpeed > 0.05f)
        {
            targetDashboard.currentState = $"Moving ({mode})";
        }
        else if (targetDashboard.batteryPercent <= 10f)
        {
            targetDashboard.currentState = "Low Battery";
        }
        else
        {
            targetDashboard.currentState = $"Idle ({mode})";
        }

        // 5. Heading (forward direction)
        if (sourceRobot.baseLink != null)
        {
            Vector3 fwd = sourceRobot.baseLink.forward;
            fwd.y = 0f;
            targetDashboard.heading = fwd.normalized;
        }

        // 6. Update timestamp so dashboard knows we're connected
        targetDashboard.lastExternalUpdateTime = Time.time;

        // Debug logging
        if (debugLog && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            Debug.Log($"[Bridge] {sourceRobot.robotName}: " +
                     $"pos={targetDashboard.position:F2}, " +
                     $"speed={targetDashboard.speed:F2}m/s, " +
                     $"battery={targetDashboard.batteryPercent:F1}%, " +
                     $"state={targetDashboard.currentState}");
        }
    }

    /// <summary>
    /// Call this if the dashboard objects are created at runtime
    /// </summary>
    public void RefreshConnection()
    {
        initialized = false;
        Initialize();
    }
}



