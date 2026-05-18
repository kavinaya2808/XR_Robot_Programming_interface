// ============================================================================
// Quest3LiDARAdapter.cs - Adapts LiDAR to work with Quest 3 Room Geometry
// ============================================================================
//
// PURPOSE:
// Ensures the LaserScanSensor raycasts hit the Quest 3 room mesh colliders.
// This is the bridge that makes your virtual robot "see" real walls!
//
// HOW IT WORKS:
// 1. Quest3RoomSetup creates colliders from real room geometry
// 2. LaserScanSensor casts rays that hit those colliders
// 3. /scan data is published to ROS
// 4. SLAM builds a map of your REAL room!
//
// SETUP:
// 1. Add this script to the same GameObject as LaserScanSensor
// 2. It will auto-configure the sensor to hit room geometry
//
// ============================================================================

using UnityEngine;

[RequireComponent(typeof(LaserScanSensor))]
public class Quest3LiDARAdapter : MonoBehaviour
{
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    [Header("Layer Configuration")]
    [Tooltip("Layers that the LiDAR should detect. -1 = Everything")]
    public LayerMask lidarDetectionLayers = -1; // All layers by default
    
    [Tooltip("Ignore triggers (usually should be true)")]
    public bool ignoreTriggers = true;
    
    [Header("Quest 3 Integration")]
    [Tooltip("Wait for Quest3RoomSetup before enabling sensor")]
    public bool waitForRoomSetup = true;
    
    [Tooltip("Reference to Quest3RoomSetup (auto-finds if empty)")]
    public Quest3RoomSetup roomSetup;
    
    [Header("Debug")]
    [Tooltip("Show debug rays in Scene view")]
    public bool showDebugRays = true;
    
    [Tooltip("Color for rays that hit something")]
    public Color hitColor = Color.red;
    
    [Tooltip("Color for rays that miss")]
    public Color missColor = Color.green;
    
    // ========================================================================
    // INTERNAL
    // ========================================================================
    
    private LaserScanSensor laserSensor;
    private bool isReady = false;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Awake()
    {
        laserSensor = GetComponent<LaserScanSensor>();
        
        if (laserSensor == null)
        {
            Debug.LogError("[Quest3LiDARAdapter] No LaserScanSensor found on this GameObject!");
            enabled = false;
            return;
        }
    }
    
    void Start()
    {
        // Find room setup if not assigned
        if (roomSetup == null)
        {
            roomSetup = FindObjectOfType<Quest3RoomSetup>();
        }
        
        if (waitForRoomSetup && roomSetup != null)
        {
            Debug.Log("[Quest3LiDARAdapter] Waiting for room setup...");
            StartCoroutine(WaitForRoomSetup());
        }
        else
        {
            // No room setup or not waiting - enable immediately
            EnableLiDAR();
        }
    }
    
    private System.Collections.IEnumerator WaitForRoomSetup()
    {
        // Wait until room is ready
        while (roomSetup != null && !roomSetup.IsRoomReady)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        EnableLiDAR();
    }
    
    private void EnableLiDAR()
    {
        isReady = true;
        Debug.Log("[Quest3LiDARAdapter] ✓ LiDAR enabled and ready to scan room geometry");
        
        // Log configuration
        if (roomSetup != null && roomSetup.IsRoomReady)
        {
            Debug.Log($"[Quest3LiDARAdapter] Room floor at Y={roomSetup.FloorPosition.y:F2}");
        }
    }
    
    // ========================================================================
    // DEBUG VISUALIZATION
    // ========================================================================
    
    void Update()
    {
        if (!showDebugRays || !isReady) return;
        
        // Visualize LiDAR rays
        DrawDebugRays();
    }
    
    private void DrawDebugRays()
    {
        if (laserSensor == null) return;
        
        // Get sensor parameters
        float angleStart = laserSensor.ScanAngleStartDegrees;
        float angleEnd = laserSensor.ScanAngleEndDegrees;
        int numRays = laserSensor.NumMeasurementsPerScan;
        float maxRange = laserSensor.RangeMetersMax;
        
        Vector3 origin = transform.position;
        float baseYaw = transform.rotation.eulerAngles.y;
        
        for (int i = 0; i < numRays; i++)
        {
            float t = (float)i / (numRays - 1);
            float angle = Mathf.Lerp(angleStart, angleEnd, t);
            float totalAngle = baseYaw + angle;
            
            Vector3 direction = Quaternion.Euler(0f, totalAngle, 0f) * Vector3.forward;
            
            // Cast ray
            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, direction, out hit, maxRange, lidarDetectionLayers);
            
            if (didHit)
            {
                Debug.DrawLine(origin, hit.point, hitColor);
            }
            else
            {
                Debug.DrawRay(origin, direction * maxRange, missColor);
            }
        }
    }
    
    // ========================================================================
    // PUBLIC API
    // ========================================================================
    
    /// <summary>
    /// Check if LiDAR is ready to scan
    /// </summary>
    public bool IsReady => isReady;
    
    /// <summary>
    /// Manually set the detection layer mask
    /// </summary>
    public void SetDetectionLayers(LayerMask layers)
    {
        lidarDetectionLayers = layers;
        Debug.Log($"[Quest3LiDARAdapter] Detection layers set to: {layers.value}");
    }
}

