using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Sends robot telemetry data over UDP to Kavinaya's MR Dashboard.
/// Add this to each robot (robot1, robot2).
/// 
/// This script is INDEPENDENT of TurtlebotCoverageAgent - it works with any robot
/// by tracking its Transform and optionally reading from MapCoverageManager.
/// </summary>
public class TelemetrySender : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("IP address of the Quest 3 or dashboard PC")]
    public string targetIP = "127.0.0.1";
    
    [Tooltip("UDP port to send data on")]
    public int targetPort = 5005;
    
    [Header("Robot Identity")]
    public string robotId = "Robot_1";
    
    [Header("Data Source")]
    [Tooltip("Transform to track for position/rotation (auto-detects if empty)")]
    public Transform robotTransform;
    
    [Tooltip("Optional: Coverage manager for task progress (auto-finds singleton if empty)")]
    public MapCoverageManager coverageManager;
    
    [Header("Settings")]
    [Tooltip("How often to send updates (times per second)")]
    public float sendRate = 10f;
    
    [Tooltip("Enable/disable sending")]
    public bool enableSending = true;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Network
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    
    // Tracking
    private float lastSendTime;
    private Vector3 lastPosition;
    private float lastTime;
    
    // Telemetry data
    private float speed;
    private float batteryPercent = 100f;
    private string status = "Idle";
    private float taskProgress = 0f;
    
    void Start()
    {
        // Auto-find transform if not assigned
        if (robotTransform == null)
            robotTransform = transform;
        
        // Auto-find coverage manager singleton
        if (coverageManager == null)
            coverageManager = MapCoverageManager.Instance;
        
        // Setup UDP
        try
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            Debug.Log($"[TelemetrySender] {robotId} ready to send to {targetIP}:{targetPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TelemetrySender] Failed to setup UDP: {e.Message}");
            enabled = false;
            return;
        }
        
        lastPosition = robotTransform.position;
        lastTime = Time.time;
    }
    
    void Update()
    {
        if (!enableSending || udpClient == null || robotTransform == null) return;
        
        // Calculate speed from position change
        float dt = Time.time - lastTime;
        if (dt > 0.01f)
        {
            Vector3 velocity = (robotTransform.position - lastPosition) / dt;
            speed = velocity.magnitude;
            lastPosition = robotTransform.position;
            lastTime = Time.time;
        }
        
        // Update status based on speed
        if (speed > 0.1f)
            status = "Exploring";
        else if (speed > 0.01f)
            status = "Moving";
        else
            status = "Idle";
        
        // Get coverage progress from manager if available
        if (coverageManager != null)
        {
            taskProgress = coverageManager.GetCoverageFraction() * 100f;
        }
        
        // Simulate battery drain (very slow)
        batteryPercent = Mathf.Max(0f, batteryPercent - Time.deltaTime * 0.005f);
        
        // Send at specified rate
        if (Time.time - lastSendTime >= 1f / sendRate)
        {
            SendTelemetry();
            lastSendTime = Time.time;
        }
    }
    
    void SendTelemetry()
    {
        if (robotTransform == null) return;
        
        try
        {
            // Create CSV telemetry packet
            // Format: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
            string data = string.Format(
                "{0},{1:F2},{2:F2},{3:F2},{4:F1},{5:F2},{6:F1},{7},{8:F1}",
                robotId,
                robotTransform.position.x,
                robotTransform.position.y,
                robotTransform.position.z,
                robotTransform.eulerAngles.y,
                speed,
                batteryPercent,
                status,
                taskProgress
            );
            
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            udpClient.Send(bytes, bytes.Length, endPoint);
            
            if (showDebugLogs)
                Debug.Log($"[TelemetrySender] Sent: {data}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TelemetrySender] Send failed: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
    
    /// <summary>
    /// Call this to update the target IP at runtime (e.g., Quest 3's IP)
    /// </summary>
    public void SetTargetIP(string ip, int port = 5005)
    {
        targetIP = ip;
        targetPort = port;
        try
        {
            endPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            Debug.Log($"[TelemetrySender] Target updated to {targetIP}:{targetPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TelemetrySender] Invalid IP: {e.Message}");
        }
    }
    
    /// <summary>
    /// Manually set battery percentage (0-100)
    /// </summary>
    public void SetBattery(float percent)
    {
        batteryPercent = Mathf.Clamp(percent, 0f, 100f);
    }
    
    /// <summary>
    /// Manually set status string
    /// </summary>
    public void SetStatus(string newStatus)
    {
        status = newStatus;
    }
}
