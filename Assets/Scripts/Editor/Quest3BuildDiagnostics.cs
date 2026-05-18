// ============================================================================
// Quest3BuildDiagnostics.cs - Pre-Build Verification Tool
// ============================================================================
// 
// This Editor script checks if your project is correctly configured
// for Quest 3 deployment. Run it before building!
//
// HOW TO USE:
// In Unity menu: Tools → Quest 3 → Run Build Diagnostics
//
// ============================================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;
using System.Linq;

public class Quest3BuildDiagnostics : EditorWindow
{
    private Vector2 scrollPosition;
    private List<DiagnosticResult> results = new List<DiagnosticResult>();
    private int passCount = 0;
    private int warnCount = 0;
    private int failCount = 0;

    private enum ResultType { Pass, Warning, Fail }
    
    private class DiagnosticResult
    {
        public string category;
        public string check;
        public ResultType type;
        public string message;
        public string fix;
    }

    [MenuItem("Tools/Quest 3/Run Build Diagnostics")]
    public static void ShowWindow()
    {
        var window = GetWindow<Quest3BuildDiagnostics>("Quest 3 Diagnostics");
        window.minSize = new Vector2(600, 500);
        window.RunAllDiagnostics();
    }

    [MenuItem("Tools/Quest 3/Quick Check (Console)")]
    public static void QuickCheck()
    {
        var diagnostics = new Quest3BuildDiagnostics();
        diagnostics.RunAllDiagnostics();
        
        Debug.Log("=== QUEST 3 BUILD DIAGNOSTICS ===");
        Debug.Log($"✓ PASS: {diagnostics.passCount} | ⚠ WARN: {diagnostics.warnCount} | ✗ FAIL: {diagnostics.failCount}");
        
        foreach (var result in diagnostics.results)
        {
            string icon = result.type == ResultType.Pass ? "✓" : (result.type == ResultType.Warning ? "⚠" : "✗");
            if (result.type != ResultType.Pass)
            {
                Debug.LogWarning($"{icon} [{result.category}] {result.check}: {result.message}");
                if (!string.IsNullOrEmpty(result.fix))
                    Debug.Log($"   FIX: {result.fix}");
            }
        }
        
        if (diagnostics.failCount == 0 && diagnostics.warnCount == 0)
            Debug.Log("✓ ALL CHECKS PASSED! Ready to build for Quest 3.");
        else if (diagnostics.failCount == 0)
            Debug.Log("⚠ Build may work but has warnings. Review above.");
        else
            Debug.LogError("✗ BUILD WILL LIKELY FAIL! Fix the errors above.");
    }

    void RunAllDiagnostics()
    {
        results.Clear();
        passCount = warnCount = failCount = 0;

        // Run all checks
        CheckPlatform();
        CheckXRSettings();
        CheckPlayerSettings();
        CheckSceneSetup();
        CheckRobotSetup();
        CheckMRTKSetup();
        CheckROSSettings();

        // Count results
        foreach (var r in results)
        {
            if (r.type == ResultType.Pass) passCount++;
            else if (r.type == ResultType.Warning) warnCount++;
            else failCount++;
        }
    }

    // ========================================================================
    // PLATFORM CHECKS
    // ========================================================================
    
    void CheckPlatform()
    {
        // Check if Android is selected
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            AddResult("Platform", "Build Target", ResultType.Pass, "Android platform selected");
        }
        else
        {
            AddResult("Platform", "Build Target", ResultType.Fail, 
                $"Current: {EditorUserBuildSettings.activeBuildTarget}", 
                "File → Build Settings → Android → Switch Platform");
        }
    }

    // ========================================================================
    // XR SETTINGS CHECKS
    // ========================================================================
    
    void CheckXRSettings()
    {
        // Check XR Plug-in Management
        var xrSettings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        
        if (xrSettings != null && xrSettings.Manager != null)
        {
            var loaders = xrSettings.Manager.activeLoaders;
            bool hasOpenXR = loaders.Any(l => l.GetType().Name.Contains("OpenXR"));
            
            if (hasOpenXR)
            {
                AddResult("XR", "OpenXR Loader", ResultType.Pass, "OpenXR is enabled for Android");
            }
            else
            {
                AddResult("XR", "OpenXR Loader", ResultType.Fail, 
                    "OpenXR not enabled", 
                    "Project Settings → XR Plug-in Management → Android → Check OpenXR");
            }
        }
        else
        {
            AddResult("XR", "XR Management", ResultType.Fail, 
                "XR Management not configured", 
                "Project Settings → XR Plug-in Management → Android → Enable OpenXR");
        }

        // Check for Meta Quest feature (via OpenXR settings)
        // This is a simplified check - full check would need OpenXR package API
        AddResult("XR", "Meta Quest Support", ResultType.Warning, 
            "Manually verify Meta Quest Support is enabled",
            "Project Settings → XR Plug-in Management → OpenXR → Meta Quest Support");
    }

    // ========================================================================
    // PLAYER SETTINGS CHECKS
    // ========================================================================
    
    void CheckPlayerSettings()
    {
        // Check scripting backend
        var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        if (backend == ScriptingImplementation.IL2CPP)
        {
            AddResult("Player", "Scripting Backend", ResultType.Pass, "IL2CPP selected");
        }
        else
        {
            AddResult("Player", "Scripting Backend", ResultType.Fail, 
                $"Current: {backend}", 
                "Project Settings → Player → Android → Scripting Backend → IL2CPP");
        }

        // Check target architecture
        var arch = PlayerSettings.Android.targetArchitectures;
        if ((arch & AndroidArchitecture.ARM64) != 0)
        {
            AddResult("Player", "Target Architecture", ResultType.Pass, "ARM64 enabled");
        }
        else
        {
            AddResult("Player", "Target Architecture", ResultType.Fail, 
                "ARM64 not enabled", 
                "Project Settings → Player → Android → Target Architectures → ARM64");
        }

        // Check minimum API level
        var minApi = PlayerSettings.Android.minSdkVersion;
        if ((int)minApi >= 32) // Android 12L = API 32
        {
            AddResult("Player", "Minimum API Level", ResultType.Pass, $"API {(int)minApi}");
        }
        else
        {
            AddResult("Player", "Minimum API Level", ResultType.Warning, 
                $"Current: API {(int)minApi}, Recommended: 32+", 
                "Project Settings → Player → Android → Minimum API Level → 32 (Android 12L)");
        }
    }

    // ========================================================================
    // SCENE SETUP CHECKS
    // ========================================================================
    
    void CheckSceneSetup()
    {
        // Check for Quest3RoomSetup
        var roomSetup = Object.FindObjectOfType<Quest3RoomSetup>();
        if (roomSetup != null)
        {
            AddResult("Scene", "Quest3RoomSetup", ResultType.Pass, 
                $"Found on '{roomSetup.gameObject.name}'");
            
            // Check passthrough settings
            if (roomSetup.enablePassthrough)
            {
                AddResult("Scene", "Passthrough Enabled", ResultType.Pass, "Passthrough is enabled");
            }
            else
            {
                AddResult("Scene", "Passthrough Enabled", ResultType.Warning, 
                    "Passthrough is disabled", 
                    "Enable 'Enable Passthrough' on Quest3RoomSetup");
            }
        }
        else
        {
            AddResult("Scene", "Quest3RoomSetup", ResultType.Fail, 
                "Quest3RoomSetup not found in scene", 
                "Add Quest3RoomSetup component to an empty GameObject");
        }

        // Check for OVRCameraRig or MRTK XR Rig
        var ovrRig = Object.FindObjectOfType<OVRCameraRig>();
        var xrRig = GameObject.Find("MRTK XR Rig");
        
        if (ovrRig != null || xrRig != null)
        {
            AddResult("Scene", "XR Camera Rig", ResultType.Pass, 
                ovrRig != null ? "OVRCameraRig found" : "MRTK XR Rig found");
        }
        else
        {
            AddResult("Scene", "XR Camera Rig", ResultType.Fail, 
                "No XR camera rig found", 
                "Add MRTK XR Rig or OVRCameraRig to scene");
        }

        // Check for MapCoverageManager
        var coverage = Object.FindObjectOfType<MapCoverageManager>();
        if (coverage != null)
        {
            AddResult("Scene", "MapCoverageManager", ResultType.Pass, "Found");
        }
        else
        {
            AddResult("Scene", "MapCoverageManager", ResultType.Warning, 
                "MapCoverageManager not found", 
                "Add MapCoverageManager if you want coverage tracking");
        }
    }

    // ========================================================================
    // ROBOT SETUP CHECKS
    // ========================================================================
    
    void CheckRobotSetup()
    {
        // Find all TurtlebotCoverageAgents
        var agents = Object.FindObjectsOfType<TurtlebotCoverageAgent>();
        
        if (agents.Length == 0)
        {
            AddResult("Robots", "Agents Found", ResultType.Fail, 
                "No TurtlebotCoverageAgent found", 
                "Add TurtlebotCoverageAgent to your robot GameObjects");
            return;
        }
        
        AddResult("Robots", "Agents Found", ResultType.Pass, $"Found {agents.Length} robot(s)");

        foreach (var agent in agents)
        {
            string robotName = agent.gameObject.name;
            
            // Check AGVController
            var agv = agent.GetComponent<RosSharp.Control.AGVController>();
            if (agv == null)
                agv = agent.GetComponentInChildren<RosSharp.Control.AGVController>();
            
            if (agv != null)
            {
                if (agv.mode == RosSharp.Control.ControlMode.ML)
                {
                    AddResult("Robots", $"{robotName} - Control Mode", ResultType.Pass, "Mode = ML");
                }
                else
                {
                    AddResult("Robots", $"{robotName} - Control Mode", ResultType.Warning, 
                        $"Mode = {agv.mode}", 
                        $"Set AGVController.mode to 'ML' on {robotName}");
                }
            }
            else
            {
                AddResult("Robots", $"{robotName} - AGVController", ResultType.Fail, 
                    "AGVController not found", 
                    $"Add AGVController to {robotName}");
            }

            // Check for LaserScanSensor
            var lidar = agent.GetComponentInChildren<LaserScanSensor>();
            if (lidar != null)
            {
                AddResult("Robots", $"{robotName} - LiDAR", ResultType.Pass, $"LaserScanSensor on {lidar.gameObject.name}");
                
                // Check for Quest3LiDARAdapter
                var adapter = lidar.GetComponent<Quest3LiDARAdapter>();
                if (adapter != null)
                {
                    AddResult("Robots", $"{robotName} - LiDAR Adapter", ResultType.Pass, "Quest3LiDARAdapter attached");
                }
                else
                {
                    AddResult("Robots", $"{robotName} - LiDAR Adapter", ResultType.Warning, 
                        "Quest3LiDARAdapter not found on LiDAR", 
                        $"Add Quest3LiDARAdapter to {lidar.gameObject.name}");
                }
            }
            else
            {
                AddResult("Robots", $"{robotName} - LiDAR", ResultType.Warning, 
                    "No LaserScanSensor found", 
                    "LiDAR is optional but needed for ROS/SLAM");
            }

            // Check boundary limit
            if (agent.boundaryLimit < 5f)
            {
                AddResult("Robots", $"{robotName} - Boundary", ResultType.Warning, 
                    $"Boundary limit is small ({agent.boundaryLimit}m)", 
                    "Increase boundaryLimit to match your room size");
            }
            else
            {
                AddResult("Robots", $"{robotName} - Boundary", ResultType.Pass, 
                    $"Boundary limit: {agent.boundaryLimit}m");
            }
        }
    }

    // ========================================================================
    // MRTK SETUP CHECKS
    // ========================================================================
    
    void CheckMRTKSetup()
    {
        // Check for MRTK XR Rig by name (safer than type check)
        var mrtkRig = GameObject.Find("MRTK XR Rig");
        if (mrtkRig != null)
        {
            AddResult("MRTK", "XR Rig", ResultType.Pass, "MRTK XR Rig found in scene");
        }
        else
        {
            AddResult("MRTK", "XR Rig", ResultType.Warning, 
                "No 'MRTK XR Rig' found", 
                "Ensure MRTK XR Rig is in scene for hand tracking");
        }
        
        // Check for Input Simulator by name
        var inputSim = GameObject.Find("MRTKInputSimulator");
        if (inputSim != null)
        {
            AddResult("MRTK", "Input Simulator", ResultType.Pass, "MRTKInputSimulator found");
        }
        
        // Check for Pointable Canvas Module by name
        var pointable = GameObject.Find("PointableCanvasModule");
        if (pointable != null)
        {
            AddResult("MRTK", "Pointable Canvas", ResultType.Pass, "PointableCanvasModule found");
        }
    }

    // ========================================================================
    // ROS SETTINGS CHECKS (Optional)
    // ========================================================================
    
    void CheckROSSettings()
    {
        // Check for ROS Connection
        var rosConnection = Object.FindObjectOfType<Unity.Robotics.ROSTCPConnector.ROSConnection>();
        
        if (rosConnection != null)
        {
            AddResult("ROS", "ROSConnection", ResultType.Pass, "ROSConnection found");
            
            // Check if IP is configured (not localhost)
            string ip = rosConnection.RosIPAddress;
            if (ip == "127.0.0.1" || ip == "localhost")
            {
                AddResult("ROS", "ROS IP Address", ResultType.Warning, 
                    $"IP is '{ip}' - won't work on Quest", 
                    "Set ROSConnection IP to your PC's WiFi IP address");
            }
            else
            {
                AddResult("ROS", "ROS IP Address", ResultType.Pass, $"IP: {ip}");
            }
        }
        else
        {
            AddResult("ROS", "ROSConnection", ResultType.Warning, 
                "No ROSConnection in scene", 
                "ROS is optional - add ROSConnection if you want SLAM");
        }
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================
    
    void AddResult(string category, string check, ResultType type, string message, string fix = "")
    {
        results.Add(new DiagnosticResult
        {
            category = category,
            check = check,
            type = type,
            message = message,
            fix = fix
        });
    }

    // ========================================================================
    // GUI
    // ========================================================================
    
    void OnGUI()
    {
        GUILayout.Space(10);
        
        // Header
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Quest 3 Build Diagnostics", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Summary
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.FlexibleSpace();
        
        GUI.color = Color.green;
        GUILayout.Label($"✓ PASS: {passCount}", EditorStyles.boldLabel);
        GUI.color = Color.yellow;
        GUILayout.Label($"  ⚠ WARN: {warnCount}", EditorStyles.boldLabel);
        GUI.color = Color.red;
        GUILayout.Label($"  ✗ FAIL: {failCount}", EditorStyles.boldLabel);
        GUI.color = Color.white;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Refresh button
        if (GUILayout.Button("🔄 Run Diagnostics Again", GUILayout.Height(30)))
        {
            RunAllDiagnostics();
        }
        
        GUILayout.Space(10);
        
        // Results
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        string currentCategory = "";
        foreach (var result in results)
        {
            // Category header
            if (result.category != currentCategory)
            {
                currentCategory = result.category;
                GUILayout.Space(5);
                EditorGUILayout.LabelField(currentCategory, EditorStyles.boldLabel);
            }
            
            // Result row
            EditorGUILayout.BeginHorizontal("box");
            
            // Icon
            if (result.type == ResultType.Pass)
            {
                GUI.color = Color.green;
                GUILayout.Label("✓", GUILayout.Width(20));
            }
            else if (result.type == ResultType.Warning)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("⚠", GUILayout.Width(20));
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("✗", GUILayout.Width(20));
            }
            GUI.color = Color.white;
            
            // Check name and message
            GUILayout.Label(result.check, GUILayout.Width(180));
            GUILayout.Label(result.message, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndHorizontal();
            
            // Fix suggestion
            if (!string.IsNullOrEmpty(result.fix) && result.type != ResultType.Pass)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                EditorGUILayout.HelpBox("FIX: " + result.fix, MessageType.Info);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        // Overall status
        if (failCount == 0 && warnCount == 0)
        {
            EditorGUILayout.HelpBox("✓ ALL CHECKS PASSED! You're ready to build for Quest 3.", MessageType.Info);
        }
        else if (failCount == 0)
        {
            EditorGUILayout.HelpBox("⚠ Build should work but review warnings above.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("✗ FIX THE ERRORS ABOVE before building!", MessageType.Error);
        }
    }
}