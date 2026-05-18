using UnityEngine;

/// <summary>
/// Manages the combined MR scene - connects robots to dashboard.
/// Add this to an empty GameObject in your combined scene.
/// </summary>
public class MRSceneManager : MonoBehaviour
{
    [Header("Robot References")]
    [Tooltip("The robot1 GameObject with TurtlebotCoverageAgent")]
    public Transform robot1Transform;
    
    [Tooltip("The robot2 GameObject with TurtlebotCoverageAgent")]
    public Transform robot2Transform;
    
    [Header("Dashboard References")]
    [Tooltip("Robot1's RobotTelemetry component from dashboard")]
    public RobotTelemetry robot1Telemetry;
    
    [Tooltip("Robot2's RobotTelemetry component from dashboard")]
    public RobotTelemetry robot2Telemetry;
    
    [Header("Coverage System")]
    [Tooltip("The shared MapCoverageManager (singleton) - BOTH robots contribute to this")]
    public MapCoverageManager coverageManager;
    
    [Header("MR Settings")]
    [Tooltip("Offset to align warehouse with real world")]
    public Vector3 worldOffset = Vector3.zero;
    
    [Tooltip("Scale factor for the warehouse (1 = real size)")]
    public float worldScale = 1f;
    
    [Header("Update Settings")]
    [Tooltip("How often to update dashboard (seconds)")]
    public float updateInterval = 0.1f;
    
    private float lastUpdateTime;
    
    // For calculating speed from position change
    private Vector3 robot1LastPos;
    private Vector3 robot2LastPos;
    private float robot1Speed;
    private float robot2Speed;
    
    void Start()
    {
        // Initialize last positions
        if (robot1Transform != null)
            robot1LastPos = robot1Transform.position;
        if (robot2Transform != null)
            robot2LastPos = robot2Transform.position;
        
        // Auto-find shared coverage manager (singleton)
        if (coverageManager == null)
            coverageManager = MapCoverageManager.Instance;
        
        // Disable simulated data on telemetry components (we'll feed real data)
        if (robot1Telemetry != null)
            robot1Telemetry.useSimulatedData = false;
        if (robot2Telemetry != null)
            robot2Telemetry.useSimulatedData = false;
        
        Debug.Log("[MRSceneManager] ✓ Initialized - Connecting robots to dashboard with shared coverage");
    }
    
    void Update()
    {
        // Throttle updates
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        lastUpdateTime = Time.time;
        
        // Update Robot 1 telemetry
        UpdateRobotTelemetry(robot1Transform, robot1Telemetry, "Robot_1", ref robot1LastPos, ref robot1Speed);
        
        // Update Robot 2 telemetry
        UpdateRobotTelemetry(robot2Transform, robot2Telemetry, "Robot_2", ref robot2LastPos, ref robot2Speed);
    }
    
    void UpdateRobotTelemetry(Transform robotTransform, RobotTelemetry telemetry, string robotId, ref Vector3 lastPos, ref float speed)
    {
        if (robotTransform == null || telemetry == null)
            return;
        
        // Calculate speed from position change
        Vector3 currentPos = robotTransform.position;
        float distance = Vector3.Distance(currentPos, lastPos);
        speed = distance / updateInterval;
        lastPos = currentPos;
        
        // Position
        telemetry.position = currentPos;
        telemetry.transform.position = currentPos;
        telemetry.transform.rotation = robotTransform.rotation;
        
        // Speed
        telemetry.speed = speed;
        telemetry.speed_mps = speed;
        
        // Heading
        Vector3 forward = robotTransform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f)
            telemetry.heading = forward.normalized;
        
        // Coverage/Task Progress - use shared coverage manager
        // Both robots contribute to the SAME coverage grid and display the SAME percentage
        if (coverageManager != null)
        {
            float coverageFraction = coverageManager.GetCoverageFraction();
            telemetry.taskProgressPercent = coverageFraction * 100f;
            telemetry.activeTask = "Map Coverage";
            
            // Debug: Log coverage updates every 10 updates (every 1 second at 10Hz)
            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"[MRSceneManager] {robotId}: Coverage = {telemetry.taskProgressPercent:F1}% " +
                    $"(fraction: {coverageFraction:F3})");
            }
        }
        else
        {
            Debug.LogWarning($"[MRSceneManager] {robotId}: coverageManager is NULL!");
        }
        
        // Status
        if (speed > 0.05f)
            telemetry.currentState = "Exploring";
        else
            telemetry.currentState = "Idle";
        
        // Mark as receiving external data
        telemetry.lastExternalUpdateTime = Time.time;
    }
    
    /// <summary>
    /// Call this to apply world offset (for MR alignment)
    /// </summary>
    public void ApplyWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
        
        // Find warehouse root and offset it
        var warehouse = GameObject.Find("GeneratedWarehouse");
        if (warehouse != null)
        {
            warehouse.transform.position = offset;
        }
    }
    
    /// <summary>
    /// Helper to auto-find and connect everything
    /// </summary>
    [ContextMenu("Auto-Connect Everything")]
    public void AutoConnect()
    {
        // Find robots
        var robots = FindObjectsOfType<TurtlebotCoverageAgent>();
        foreach (var robot in robots)
        {
            var rootTransform = robot.transform.Find("base_footprint");
            if (rootTransform == null) rootTransform = robot.transform;
            
            if (robot.name.Contains("1") || robot.name == "robot1")
                robot1Transform = rootTransform;
            else if (robot.name.Contains("2") || robot.name == "robot2")
                robot2Transform = rootTransform;
        }
        
        // Find telemetry components
        var telemetries = FindObjectsOfType<RobotTelemetry>();
        foreach (var t in telemetries)
        {
            if (t.robotId == "Robot_1" || t.name.Contains("1"))
                robot1Telemetry = t;
            else if (t.robotId == "Robot_2" || t.name.Contains("2"))
                robot2Telemetry = t;
        }
        
        // Find shared coverage manager (singleton)
        coverageManager = MapCoverageManager.Instance;
        
        Debug.Log($"[MRSceneManager] Auto-connected: Robot1={robot1Transform != null}, Robot2={robot2Transform != null}, " +
                  $"Telemetry1={robot1Telemetry != null}, Telemetry2={robot2Telemetry != null}, Coverage={coverageManager != null}");
    }
}

