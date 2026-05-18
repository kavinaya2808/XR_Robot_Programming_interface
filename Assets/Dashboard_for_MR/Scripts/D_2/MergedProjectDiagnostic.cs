// ============================================================================
// MergedProjectDiagnostic.cs - Diagnostic Tool for MR_RobotExplorer Scene
// ============================================================================
//
// PURPOSE: Helps diagnose issues with the merged Robotics + MRTK3 project
// 
// HOW TO USE:
// 1. Add this script to any GameObject in your scene (e.g., create empty "Diagnostics")
// 2. Press Play
// 3. Check the Console for diagnostic messages (marked with [DIAGNOSTIC])
// 4. In Game view, press 'P' to print current status
// 5. Check the Inspector for live status
//
// ============================================================================

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosSharp.Control;
using System.Collections.Generic;
using System.Text;

public class MergedProjectDiagnostic : MonoBehaviour
{
    [Header("Auto-Found References")]
    public List<AGVController> controllers = new List<AGVController>();
    public List<TelemetryPublisher> telemetryPublishers = new List<TelemetryPublisher>();
    public List<LaserScanSensor> lidarSensors = new List<LaserScanSensor>();
    public MapCoverageManager coverageManager;
    public ROSConnection rosConnection;
    
    [Header("Dashboard References")]
    public List<RobotTelemetry> dashboardTelemetry = new List<RobotTelemetry>();
    public List<DirectTelemetryBridge> bridges = new List<DirectTelemetryBridge>();
    
    [Header("Live Status")]
    public bool rosConnected = false;
    public string rosStatus = "Unknown";
    public float fixedDeltaTime;
    public int solverIterations;
    public int solverVelocityIterations;
    
    [Header("Settings")]
    public bool autoLogOnStart = true;
    public bool periodicLog = false;
    public float periodicLogInterval = 5f;
    
    private float lastPeriodicLog = 0f;
    
    void Start()
    {
        FindAllComponents();
        
        // Get physics settings
        fixedDeltaTime = Time.fixedDeltaTime;
        solverIterations = Physics.defaultSolverIterations;
        solverVelocityIterations = Physics.defaultSolverVelocityIterations;
        
        if (autoLogOnStart)
        {
            LogFullDiagnostic();
        }
    }
    
    void FindAllComponents()
    {
        // Find all robotics components
        controllers.AddRange(FindObjectsOfType<AGVController>());
        telemetryPublishers.AddRange(FindObjectsOfType<TelemetryPublisher>());
        lidarSensors.AddRange(FindObjectsOfType<LaserScanSensor>());
        coverageManager = FindObjectOfType<MapCoverageManager>();
        rosConnection = ROSConnection.GetOrCreateInstance();
        
        // Find dashboard components
        dashboardTelemetry.AddRange(FindObjectsOfType<RobotTelemetry>());
        bridges.AddRange(FindObjectsOfType<DirectTelemetryBridge>());
    }
    
    void Update()
    {
        // Check ROS connection status
        if (rosConnection != null)
        {
            rosStatus = rosConnection.enabled ? "Active" : "Disabled";
        }
        
        // Press P for diagnostic
        /*if (Input.GetKeyDown(KeyCode.P))
        {
            LogFullDiagnostic();
        }*/
        
        // Periodic logging
        if (periodicLog && Time.time - lastPeriodicLog > periodicLogInterval)
        {
            LogQuickStatus();
            lastPeriodicLog = Time.time;
        }
    }
    
    void LogFullDiagnostic()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("[DIAGNOSTIC] MERGED PROJECT STATUS REPORT");
        sb.AppendLine(new string('=', 80));
        
        // ====================================================================
        // PHYSICS SETTINGS
        // ====================================================================
        sb.AppendLine("\n[PHYSICS SETTINGS]");
        sb.AppendLine($"  Fixed Timestep: {Time.fixedDeltaTime:F4} seconds ({1f/Time.fixedDeltaTime:F0} Hz)");
        sb.AppendLine($"  Solver Iterations: {Physics.defaultSolverIterations}");
        sb.AppendLine($"  Solver Velocity Iterations: {Physics.defaultSolverVelocityIterations}");
        
        if (Time.fixedDeltaTime > 0.02f)
        {
            sb.AppendLine($"  ⚠️ WARNING: Fixed Timestep too high! Should be 0.01-0.02 for robots.");
        }
        if (Physics.defaultSolverIterations < 10)
        {
            sb.AppendLine($"  ⚠️ WARNING: Solver iterations too low for ArticulationBody robots!");
        }
        
        // ====================================================================
        // ROS CONNECTION
        // ====================================================================
        sb.AppendLine("\n[ROS CONNECTION]");
        if (rosConnection != null)
        {
            sb.AppendLine($"  Status: {rosStatus}");
            sb.AppendLine($"  Host: 127.0.0.1:10000 (check ROSConnectionPrefab)");
            sb.AppendLine("  To connect to ROS:");
            sb.AppendLine("    1. Start Docker container: docker start ros2_unity");
            sb.AppendLine("    2. Use noVNC at localhost:6080");
            sb.AppendLine("    3. Run: source /opt/ros/galactic/setup.bash");
            sb.AppendLine("    4. Run: source ~/colcon_ws/install/setup.bash");
            sb.AppendLine("    5. Run: ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0");
        }
        else
        {
            sb.AppendLine("  ❌ ROSConnection not found!");
        }
        
        // ====================================================================
        // ROBOTS
        // ====================================================================
        sb.AppendLine("\n[ROBOTS]");
        sb.AppendLine($"  Found {controllers.Count} AGVController(s)");
        
        foreach (var ctrl in controllers)
        {
            if (ctrl == null) continue;
            sb.AppendLine($"\n  Robot: {ctrl.gameObject.name}");
            sb.AppendLine($"    Mode: {ctrl.mode}");
            sb.AppendLine($"    Max Linear Speed: {ctrl.maxLinearSpeed} m/s");
            sb.AppendLine($"    Max Rotational Speed: {ctrl.maxRotationalSpeed} rad/s");
            
            if (ctrl.mode == ControlMode.ROS)
            {
                sb.AppendLine($"    ⚠️ Mode is ROS - make sure Docker TCP endpoint is running!");
                sb.AppendLine($"       TIP: Change to 'ML' for ML-Agents or 'Keyboard' for WASD control");
            }
        }
        
        // ====================================================================
        // LIDAR SENSORS
        // ====================================================================
        sb.AppendLine("\n[LIDAR SENSORS]");
        sb.AppendLine($"  Found {lidarSensors.Count} LaserScanSensor(s)");
        
        foreach (var lidar in lidarSensors)
        {
            if (lidar == null) continue;
            sb.AppendLine($"\n  LiDAR on: {lidar.gameObject.name}");
            sb.AppendLine($"    Topic: {lidar.topic}");
            sb.AppendLine($"    Measurements/Scan: {lidar.NumMeasurementsPerScan}");
            sb.AppendLine($"    Range: {lidar.RangeMetersMin}-{lidar.RangeMetersMax}m");
            sb.AppendLine($"    Frame ID: {lidar.FrameId}");
            
            if (string.IsNullOrEmpty(lidar.topic))
            {
                sb.AppendLine($"    ❌ WARNING: Topic is empty! Set to '/scan' or '/robot1/scan'");
            }
        }
        
        // ====================================================================
        // COVERAGE MANAGER
        // ====================================================================
        sb.AppendLine("\n[COVERAGE MANAGER]");
        if (coverageManager != null)
        {
            sb.AppendLine($"  Status: Found");
            sb.AppendLine($"  Coverage: {coverageManager.GetCoverageFraction() * 100:F1}%");
        }
        else
        {
            sb.AppendLine($"  ❌ NOT FOUND! Add MapCoverageManager to scene.");
        }
        
        // ====================================================================
        // TELEMETRY PUBLISHERS (Robot side - for ROS publishing)
        // ====================================================================
        sb.AppendLine("\n[TELEMETRY PUBLISHERS (Robot → ROS)]");
        sb.AppendLine($"  Found {telemetryPublishers.Count} TelemetryPublisher(s)");
        
        foreach (var pub in telemetryPublishers)
        {
            if (pub == null) continue;
            sb.AppendLine($"\n  Publisher: {pub.gameObject.name}");
            sb.AppendLine($"    Robot Name: {pub.robotName}");
            sb.AppendLine($"    Battery: {pub.Battery:F1}%");
            sb.AppendLine($"    Speed: {pub.LinSpeed:F2} m/s");
            sb.AppendLine($"    Mode: {pub.Mode}");
        }
        
        // ====================================================================
        // DASHBOARD (MR side)
        // ====================================================================
        sb.AppendLine("\n[DASHBOARD TELEMETRY (MR Display)]");
        sb.AppendLine($"  Found {dashboardTelemetry.Count} RobotTelemetry component(s)");
        
        foreach (var dash in dashboardTelemetry)
        {
            if (dash == null) continue;
            sb.AppendLine($"\n  Dashboard: {dash.gameObject.name}");
            sb.AppendLine($"    Robot ID: {dash.robotId}");
            sb.AppendLine($"    Use Simulated Data: {dash.useSimulatedData}");
            sb.AppendLine($"    Position: {dash.position}");
            sb.AppendLine($"    Speed: {dash.speed:F2} m/s");
            sb.AppendLine($"    Battery: {dash.batteryPercent:F1}%");
            sb.AppendLine($"    State: {dash.currentState}");
        }
        
        // ====================================================================
        // DIRECT BRIDGES
        // ====================================================================
        sb.AppendLine("\n[DIRECT TELEMETRY BRIDGES (Robot → Dashboard)]");
        sb.AppendLine($"  Found {bridges.Count} DirectTelemetryBridge(s)");
        
        if (bridges.Count == 0)
        {
            sb.AppendLine("  ⚠️ No bridges found! Dashboard won't receive robot data.");
            sb.AppendLine("     TIP: Add DirectTelemetryBridge to each robot");
        }
        
        foreach (var bridge in bridges)
        {
            if (bridge == null) continue;
            sb.AppendLine($"\n  Bridge on: {bridge.gameObject.name}");
            sb.AppendLine($"    Source Robot: {(bridge.sourceRobot != null ? bridge.sourceRobot.robotName : "NOT SET")}");
            sb.AppendLine($"    Target ID: {bridge.targetRobotId}");
            sb.AppendLine($"    Target Dashboard: {(bridge.targetDashboard != null ? bridge.targetDashboard.robotId : "NOT SET")}");
        }
        
        // ====================================================================
        // UNNECESSARY SCRIPTS CHECK
        // ====================================================================
        sb.AppendLine("\n[SCRIPTS TO DISABLE/REMOVE (not needed in merged project)]");
        
        var telemetrySenders = FindObjectsOfType<TelemetrySender>();
        if (telemetrySenders.Length > 0)
        {
            sb.AppendLine($"  ⚠️ Found {telemetrySenders.Length} TelemetrySender(s) - NOT NEEDED (was for UDP to separate project)");
            foreach (var sender in telemetrySenders)
            {
                sb.AppendLine($"     → Disable or remove: {sender.gameObject.name}");
            }
        }
        
        var telemetryReceivers = FindObjectsOfType<TelemetryReceiver>();
        if (telemetryReceivers.Length > 0)
        {
            sb.AppendLine($"  ⚠️ Found {telemetryReceivers.Length} TelemetryReceiver(s) - NOT NEEDED (was for UDP from separate project)");
            foreach (var receiver in telemetryReceivers)
            {
                sb.AppendLine($"     → Disable or remove: {receiver.gameObject.name}");
            }
        }
        
        // ====================================================================
        // RECOMMENDATIONS
        // ====================================================================
        sb.AppendLine("\n[RECOMMENDATIONS]");
        
        bool hasIssues = false;
        
        if (controllers.Count > 0 && controllers[0].mode == ControlMode.ROS)
        {
            sb.AppendLine("  1. AGVController Mode is 'ROS' - ensure Docker is running, OR change to 'ML' or 'Keyboard'");
            hasIssues = true;
        }
        
        if (bridges.Count == 0 && dashboardTelemetry.Count > 0)
        {
            sb.AppendLine("  2. Add DirectTelemetryBridge to robots to feed dashboard");
            hasIssues = true;
        }
        
        if (telemetrySenders.Length > 0 || telemetryReceivers.Length > 0)
        {
            sb.AppendLine("  3. Disable TelemetrySender and TelemetryReceiver (not needed anymore)");
            hasIssues = true;
        }
        
        if (!hasIssues)
        {
            sb.AppendLine("  ✓ No critical issues found!");
        }
        
        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("Press 'P' during play mode to print this diagnostic again");
        sb.AppendLine(new string('=', 80));
        
        Debug.Log(sb.ToString());
    }
    
    void LogQuickStatus()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n[DIAGNOSTIC] Quick Status:");
        
        foreach (var ctrl in controllers)
        {
            if (ctrl == null) continue;
            sb.AppendLine($"  {ctrl.gameObject.name}: mode={ctrl.mode}");
        }
        
        if (coverageManager != null)
        {
            sb.AppendLine($"  Coverage: {coverageManager.GetCoverageFraction() * 100:F1}%");
        }
        
        Debug.Log(sb.ToString());
    }
    
    void OnGUI()
    {
        // Show help text
        GUILayout.BeginArea(new Rect(10, 10, 350, 120));
        GUILayout.Label("=== DIAGNOSTIC ===");
        GUILayout.Label("Press 'P' for full diagnostic");
        GUILayout.Label($"Coverage: {(coverageManager != null ? coverageManager.GetCoverageFraction() * 100 : 0):F1}%");
        GUILayout.Label($"Physics: {1f/Time.fixedDeltaTime:F0}Hz, Solver: {Physics.defaultSolverIterations}");
        GUILayout.Label($"ROS: {rosStatus}");
        GUILayout.EndArea();
    }
}

