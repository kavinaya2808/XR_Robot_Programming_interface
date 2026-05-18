// ============================================================================
// Quest3RoomSetup.cs - Quest 3 Passthrough + Scene API Integration
// ============================================================================
//
// PURPOSE:
// This script enables Quest 3 passthrough and uses Meta's Scene API to:
// 1. Detect real room geometry (walls, floor, furniture)
// 2. Convert room mesh to Unity colliders (so robot LiDAR/raycasts work)
// 3. Spawn virtual robots on the real floor
//
// SETUP REQUIRED ON QUEST 3:
// 1. Go to Settings → Physical Space → Space Setup → Set up Room
// 2. Define your room boundaries (walls, floor, ceiling)
// 3. Optionally define furniture (tables, couches, etc.)
//
// HOW IT WORKS:
// - Quest 3 scans your room and creates "Scene Anchors" for each surface
// - This script reads those anchors and creates invisible colliders
// - Your robot's LiDAR rays hit these colliders (just like virtual walls)
// - SLAM builds a map of your REAL room!
//
// NOTE: This uses deprecated OVRSceneManager API. It still works but
// Meta recommends using MRUK for new projects.
//
// ============================================================================

// Suppress deprecation warnings for OVRSceneManager (still works, just deprecated)
#pragma warning disable CS0618

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest3RoomSetup : MonoBehaviour
{
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    [Header("Passthrough Settings")]
    [Tooltip("Enable camera passthrough (see real world)")]
    public bool enablePassthrough = true;
    
    [Tooltip("Passthrough opacity (1 = fully opaque real world)")]
    [Range(0f, 1f)]
    public float passthroughOpacity = 1f;
    
    [Header("Room Detection")]
    [Tooltip("Automatically setup room when scene loads")]
    public bool autoSetupRoom = true;
    
    [Tooltip("Show debug visualization of detected surfaces")]
    public bool showDebugVisuals = true;
    
    [Tooltip("Color for wall debug visualization")]
    public Color wallDebugColor = new Color(1f, 0f, 0f, 0.3f);
    
    [Tooltip("Color for floor debug visualization")]
    public Color floorDebugColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Tooltip("Color for furniture debug visualization")]
    public Color furnitureDebugColor = new Color(0f, 0f, 1f, 0.3f);
    
    [Header("Robot Spawning")]
    [Tooltip("Robot prefabs to spawn (leave empty to use existing robots in scene)")]
    public GameObject[] robotPrefabs;
    
    [Tooltip("Spawn robots at this height above detected floor")]
    public float spawnHeightAboveFloor = 0.05f;
    
    [Tooltip("If true, move existing robots to floor. If false, spawn new ones.")]
    public bool moveExistingRobots = true;
    
    [Header("Layer Settings")]
    [Tooltip("Layer to assign to room geometry (for raycasts)")]
    public string obstacleLayerName = "Default";
    
    [Tooltip("Tag to assign to walls/furniture")]
    public string obstacleTag = "Obstacle";
    
    // ========================================================================
    // RUNTIME STATE
    // ========================================================================
    
    private OVRCameraRig cameraRig;
    private GameObject xrOrigin;
    private OVRPassthroughLayer passthroughLayer;
    private OVRSceneManager sceneManager;
    
    private List<GameObject> createdColliders = new List<GameObject>();
    private Vector3 detectedFloorPosition = Vector3.zero;
    private bool roomSetupComplete = false;
    
    // ========================================================================
    // PUBLIC API
    // ========================================================================
    
    /// <summary>
    /// Returns true when room setup is complete and robots can be spawned
    /// </summary>
    public bool IsRoomReady => roomSetupComplete;
    
    /// <summary>
    /// Get the detected floor position (Y coordinate)
    /// </summary>
    public Vector3 FloorPosition => detectedFloorPosition;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Start()
    {
        Debug.Log("[Quest3RoomSetup] Initializing...");
        
        // Find rig components (OVR rig or XR Origin/MRTK XR Rig)
        cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig == null)
        {
            // Try XR Origin (MRTK XR Rig uses XR Origin)
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                xrOrigin = origin.gameObject;
            }
        }
        if (cameraRig == null && xrOrigin == null)
        {
            Debug.LogError("[Quest3RoomSetup] No OVRCameraRig or XR Origin found! Make sure you have MRTK XR Rig or OVR Camera Rig in scene.");
        }
        
        // Setup passthrough
        if (enablePassthrough)
        {
            SetupPassthrough();
        }
        
        // Setup room detection
        if (autoSetupRoom)
        {
            StartCoroutine(SetupRoomCoroutine());
        }
    }
    
    // ========================================================================
    // PASSTHROUGH SETUP
    // ========================================================================
    
    private void SetupPassthrough()
    {
        Debug.Log("[Quest3RoomSetup] Setting up passthrough...");
        
        // Find or create passthrough layer
        passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
        
        if (passthroughLayer == null)
        {
            GameObject target = cameraRig != null ? cameraRig.gameObject : xrOrigin;
            if (target != null)
            {
                passthroughLayer = target.AddComponent<OVRPassthroughLayer>();
                Debug.Log("[Quest3RoomSetup] Created OVRPassthroughLayer");
            }
        }
        
        if (passthroughLayer != null)
        {
            passthroughLayer.textureOpacity = passthroughOpacity;
            passthroughLayer.hidden = false;
            
            // Set passthrough as underlay (renders behind virtual objects)
            passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
            
            Debug.Log($"[Quest3RoomSetup] Passthrough enabled (opacity: {passthroughOpacity})");
        }
        else
        {
            Debug.LogWarning("[Quest3RoomSetup] Could not setup passthrough - no OVRPassthroughLayer");
        }
        
        // Configure camera for passthrough
        ConfigureCameraForPassthrough();
    }
    
    private void ConfigureCameraForPassthrough()
    {
        // Find main camera
        Camera mainCam = Camera.main;
        if (mainCam == null && cameraRig != null)
        {
            mainCam = cameraRig.centerEyeAnchor?.GetComponent<Camera>();
        }
        if (mainCam == null && xrOrigin != null)
        {
            mainCam = xrOrigin.GetComponentInChildren<Camera>();
        }
        
        if (mainCam != null)
        {
            // Set clear flags to show passthrough
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.clear;
            
            Debug.Log("[Quest3RoomSetup] Camera configured for passthrough");
        }
    }
    
    // ========================================================================
    // ROOM SETUP (Scene API)
    // ========================================================================
    
    private IEnumerator SetupRoomCoroutine()
    {
        Debug.Log("[Quest3RoomSetup] Waiting for Scene API...");
        
        // Find or create scene manager
        sceneManager = FindObjectOfType<OVRSceneManager>();
        
        if (sceneManager == null)
        {
            // Create scene manager
            GameObject sceneManagerObj = new GameObject("OVRSceneManager");
            sceneManager = sceneManagerObj.AddComponent<OVRSceneManager>();
            Debug.Log("[Quest3RoomSetup] Created OVRSceneManager");
        }
        
        // Wait for scene to load
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!IsSceneLoaded() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            Debug.Log($"[Quest3RoomSetup] Waiting for scene data... ({elapsed:F1}s)");
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogWarning("[Quest3RoomSetup] Timeout waiting for Scene API. Room setup may not be complete on this device.");
            Debug.LogWarning("[Quest3RoomSetup] Make sure you've set up your room in Quest 3 Settings → Physical Space → Space Setup");
            
            // Fallback: create a simple floor plane
            CreateFallbackFloor();
            roomSetupComplete = true;
            yield break;
        }
        
        // Process detected room geometry
        yield return StartCoroutine(ProcessRoomGeometry());
        
        // Move/spawn robots
        if (moveExistingRobots)
        {
            MoveExistingRobotsToFloor();
        }
        else if (robotPrefabs != null && robotPrefabs.Length > 0)
        {
            SpawnRobotsOnFloor();
        }
        
        roomSetupComplete = true;
        Debug.Log("[Quest3RoomSetup] ✓ Room setup complete!");
    }
    
    private bool IsSceneLoaded()
    {
        // Check if any scene anchors exist
        OVRSceneAnchor[] anchors = FindObjectsOfType<OVRSceneAnchor>();
        return anchors != null && anchors.Length > 0;
    }
    
    private IEnumerator ProcessRoomGeometry()
    {
        Debug.Log("[Quest3RoomSetup] Processing room geometry...");
        
        // Find all scene anchors
        OVRSceneAnchor[] anchors = FindObjectsOfType<OVRSceneAnchor>();
        
        int wallCount = 0;
        int floorCount = 0;
        int furnitureCount = 0;
        
        foreach (OVRSceneAnchor anchor in anchors)
        {
            // Get semantic classification
            OVRSemanticClassification classification = anchor.GetComponent<OVRSemanticClassification>();
            if (classification == null) continue;
            
            // Process based on type
            if (classification.Contains(OVRSceneManager.Classification.WallFace))
            {
                ProcessWall(anchor);
                wallCount++;
            }
            else if (classification.Contains(OVRSceneManager.Classification.Floor))
            {
                ProcessFloor(anchor);
                floorCount++;
            }
            else if (classification.Contains(OVRSceneManager.Classification.Ceiling))
            {
                // Skip ceiling - robots don't need to detect it
            }
            else if (classification.Contains(OVRSceneManager.Classification.Table) ||
                     classification.Contains(OVRSceneManager.Classification.Couch) ||
                     classification.Contains(OVRSceneManager.Classification.Other))
            {
                ProcessFurniture(anchor);
                furnitureCount++;
            }
            
            yield return null; // Process one per frame to avoid hitches
        }
        
        Debug.Log($"[Quest3RoomSetup] Processed: {wallCount} walls, {floorCount} floors, {furnitureCount} furniture pieces");
    }
    
    private void ProcessWall(OVRSceneAnchor anchor)
    {
        GameObject wallObj = anchor.gameObject;
        
        // Add collider if not present
        EnsureCollider(wallObj, true);
        
        // Set layer and tag
        SetLayerAndTag(wallObj, obstacleLayerName, obstacleTag);
        
        // Debug visualization
        if (showDebugVisuals)
        {
            SetDebugMaterial(wallObj, wallDebugColor);
        }
        else
        {
            HideVisual(wallObj);
        }
        
        createdColliders.Add(wallObj);
    }
    
    private void ProcessFloor(OVRSceneAnchor anchor)
    {
        GameObject floorObj = anchor.gameObject;
        
        // Record floor position
        detectedFloorPosition = floorObj.transform.position;
        
        // Add collider
        EnsureCollider(floorObj, true);
        
        // Floor is NOT an obstacle (robots drive on it)
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer >= 0)
        {
            floorObj.layer = defaultLayer;
        }
        
        // Debug visualization
        if (showDebugVisuals)
        {
            SetDebugMaterial(floorObj, floorDebugColor);
        }
        else
        {
            HideVisual(floorObj);
        }
        
        createdColliders.Add(floorObj);
        
        Debug.Log($"[Quest3RoomSetup] Floor detected at Y={detectedFloorPosition.y:F2}");
    }
    
    private void ProcessFurniture(OVRSceneAnchor anchor)
    {
        GameObject furnitureObj = anchor.gameObject;
        
        // Add collider
        EnsureCollider(furnitureObj, true);
        
        // Set as obstacle
        SetLayerAndTag(furnitureObj, obstacleLayerName, obstacleTag);
        
        // Debug visualization
        if (showDebugVisuals)
        {
            SetDebugMaterial(furnitureObj, furnitureDebugColor);
        }
        else
        {
            HideVisual(furnitureObj);
        }
        
        createdColliders.Add(furnitureObj);
    }
    
    // ========================================================================
    // HELPER METHODS
    // ========================================================================
    
    private void EnsureCollider(GameObject obj, bool useExistingMesh)
    {
        // Check if collider already exists
        Collider existingCollider = obj.GetComponent<Collider>();
        if (existingCollider != null) return;
        
        // Try to use mesh collider if mesh exists
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (useExistingMesh && meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false; // Non-convex for accurate wall detection
            return;
        }
        
        // Fallback: use box collider based on renderer bounds
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            // Box collider will auto-size to renderer bounds
            return;
        }
        
        // Last resort: add a small box collider
        obj.AddComponent<BoxCollider>();
    }
    
    private void SetLayerAndTag(GameObject obj, string layerName, string tag)
    {
        // Set layer
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            obj.layer = layer;
        }
        
        // Set tag (only if tag exists)
        try
        {
            obj.tag = tag;
        }
        catch
        {
            // Tag doesn't exist - that's OK
        }
    }
    
    private void SetDebugMaterial(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create semi-transparent material
            Material debugMat = new Material(Shader.Find("Standard"));
            debugMat.color = color;
            debugMat.SetFloat("_Mode", 3); // Transparent
            debugMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            debugMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            debugMat.SetInt("_ZWrite", 0);
            debugMat.DisableKeyword("_ALPHATEST_ON");
            debugMat.EnableKeyword("_ALPHABLEND_ON");
            debugMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            debugMat.renderQueue = 3000;
            
            renderer.material = debugMat;
            renderer.enabled = true;
        }
    }
    
    private void HideVisual(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }
    
    private void CreateFallbackFloor()
    {
        Debug.Log("[Quest3RoomSetup] Creating fallback floor plane...");
        
        // Create a large floor plane at Y=0
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "FallbackFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10f, 1f, 10f); // 100m x 100m
        
        // Hide visual but keep collider
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (showDebugVisuals)
            {
                SetDebugMaterial(floor, floorDebugColor);
            }
            else
            {
                renderer.enabled = false;
            }
        }
        
        detectedFloorPosition = Vector3.zero;
        createdColliders.Add(floor);
    }
    
    // ========================================================================
    // ROBOT SPAWNING / MOVING
    // ========================================================================
    
    private void MoveExistingRobotsToFloor()
    {
        Debug.Log("[Quest3RoomSetup] Moving existing robots to floor...");
        
        // Find robots by agent script
        TurtlebotCoverageAgent[] agents = FindObjectsOfType<TurtlebotCoverageAgent>();
        
        foreach (var agent in agents)
        {
            Transform robotRoot = agent.robotRoot ?? agent.transform;
            
            // Get current position
            Vector3 currentPos = robotRoot.position;
            
            // Move to floor height
            Vector3 newPos = new Vector3(
                currentPos.x,
                detectedFloorPosition.y + spawnHeightAboveFloor,
                currentPos.z
            );
            
            // Use ArticulationBody teleport if available
            ArticulationBody artBody = robotRoot.GetComponentInParent<ArticulationBody>();
            if (artBody != null && artBody.isRoot)
            {
                artBody.TeleportRoot(newPos, robotRoot.rotation);
            }
            else
            {
                robotRoot.position = newPos;
            }
            
            Debug.Log($"[Quest3RoomSetup] Moved {agent.gameObject.name} to floor at Y={newPos.y:F2}");
        }
    }
    
    private void SpawnRobotsOnFloor()
    {
        if (robotPrefabs == null || robotPrefabs.Length == 0) return;
        
        Debug.Log("[Quest3RoomSetup] Spawning robots on floor...");
        
        // Get spawn position (center of room, on floor)
        Vector3 spawnPos = new Vector3(
            0f,
            detectedFloorPosition.y + spawnHeightAboveFloor,
            0f
        );
        
        // If we have camera rig, spawn near user
        if (cameraRig != null)
        {
            Vector3 userPos = cameraRig.centerEyeAnchor.position;
            Vector3 userForward = cameraRig.centerEyeAnchor.forward;
            userForward.y = 0;
            userForward.Normalize();
            
            // Spawn 1.5m in front of user
            spawnPos = new Vector3(
                userPos.x + userForward.x * 1.5f,
                detectedFloorPosition.y + spawnHeightAboveFloor,
                userPos.z + userForward.z * 1.5f
            );
        }
        
        // Spawn robots with offset
        for (int i = 0; i < robotPrefabs.Length; i++)
        {
            if (robotPrefabs[i] == null) continue;
            
            Vector3 offset = new Vector3(i * 0.5f, 0, 0); // Offset each robot
            GameObject robot = Instantiate(robotPrefabs[i], spawnPos + offset, Quaternion.identity);
            Debug.Log($"[Quest3RoomSetup] Spawned {robot.name} at {spawnPos + offset}");
        }
    }
    
    // ========================================================================
    // PUBLIC METHODS
    // ========================================================================
    
    /// <summary>
    /// Manually trigger room setup (call if autoSetupRoom is false)
    /// </summary>
    [ContextMenu("Setup Room")]
    public void ManualSetupRoom()
    {
        StartCoroutine(SetupRoomCoroutine());
    }
    
    /// <summary>
    /// Clear all created colliders (for resetting)
    /// </summary>
    [ContextMenu("Clear Room Colliders")]
    public void ClearRoomColliders()
    {
        foreach (var obj in createdColliders)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        createdColliders.Clear();
        roomSetupComplete = false;
        Debug.Log("[Quest3RoomSetup] Room colliders cleared");
    }
    
    /// <summary>
    /// Toggle passthrough on/off at runtime
    /// </summary>
    public void SetPassthroughEnabled(bool enabled)
    {
        if (passthroughLayer != null)
        {
            passthroughLayer.hidden = !enabled;
        }
    }
    
    /// <summary>
    /// Set passthrough opacity at runtime (0-1)
    /// </summary>
    public void SetPassthroughOpacity(float opacity)
    {
        if (passthroughLayer != null)
        {
            passthroughLayer.textureOpacity = Mathf.Clamp01(opacity);
        }
    }
}

// Re-enable deprecation warnings
#pragma warning restore CS0618

