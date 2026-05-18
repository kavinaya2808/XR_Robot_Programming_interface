using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Receives robot telemetry data over UDP from Cem's robot simulation.
/// Add this to a GameObject in the dashboard scene.
/// </summary>
public class TelemetryReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("UDP port to listen on")]
    public int listenPort = 5005;
    
    [Header("Robot References")]
    [Tooltip("Optional: Direct references to RobotTelemetry components")]
    public RobotTelemetry robot1Telemetry;
    public RobotTelemetry robot2Telemetry;
    
    [Header("Virtual Robot Visuals (MR)")]
    [Tooltip("Optional: Visual robot representations for MR view")]
    public VirtualRobotVisual robot1Visual;
    public VirtualRobotVisual robot2Visual;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Network
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    
    // Thread-safe data queue
    private readonly Queue<string> receivedDataQueue = new Queue<string>();
    private readonly object queueLock = new object();
    
    void Start()
    {
        StartReceiving();
    }
    
    void StartReceiving()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            isRunning = true;
            
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            Debug.Log($"[TelemetryReceiver] ✓ Listening on UDP port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TelemetryReceiver] Failed to start: {e.Message}");
        }
    }
    
    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
        
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                
                lock (queueLock)
                {
                    receivedDataQueue.Enqueue(message);
                }
            }
            catch (SocketException)
            {
                // Expected when closing
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogWarning($"[TelemetryReceiver] Error: {e.Message}");
            }
        }
    }
    
    void Update()
    {
        // Process received data on main thread
        lock (queueLock)
        {
            while (receivedDataQueue.Count > 0)
            {
                string data = receivedDataQueue.Dequeue();
                ProcessTelemetry(data);
            }
        }
    }
    
    void ProcessTelemetry(string data)
    {
        // Format: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
        try
        {
            string[] parts = data.Split(',');
            if (parts.Length < 9)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[TelemetryReceiver] Invalid data format: {data}");
                return;
            }
            
            string robotId = parts[0];
            float posX = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            float posY = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            float posZ = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
            float rotY = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
            float speed = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);
            float battery = float.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture);
            string status = parts[7];
            float taskProgress = float.Parse(parts[8], System.Globalization.CultureInfo.InvariantCulture);
            
            // Find the appropriate telemetry component
            RobotTelemetry telemetry = null;
            
            if (robotId == "Robot_1" && robot1Telemetry != null)
                telemetry = robot1Telemetry;
            else if (robotId == "Robot_2" && robot2Telemetry != null)
                telemetry = robot2Telemetry;
            else if (RobotTelemetry.Registry.ContainsKey(robotId))
                telemetry = RobotTelemetry.Registry[robotId];
            
            if (telemetry != null)
            {
                // IMPORTANT: Disable internal simulation when receiving external data
                telemetry.useSimulatedData = false;
                telemetry.lastExternalUpdateTime = Time.time;
                
                // Update telemetry data from Cem's simulation
                telemetry.position = new Vector3(posX, posY, posZ);
                telemetry.speed = speed;
                telemetry.speed_mps = speed;
                telemetry.batteryPercent = battery;
                telemetry.currentState = status;
                telemetry.taskProgressPercent = taskProgress;
                telemetry.activeTask = "Map Coverage";
                
                // Also update the transform position so UI elements can track it
                telemetry.transform.position = new Vector3(posX, posY, posZ);
                telemetry.transform.rotation = Quaternion.Euler(0, rotY, 0);
                
                if (showDebugLogs)
                    Debug.Log($"[TelemetryReceiver] ✓ {robotId}: pos=({posX:F1},{posZ:F1}) speed={speed:F2} battery={battery:F0}%");
            }
            
            // Update virtual robot visual for MR display
            VirtualRobotVisual visual = null;
            if (robotId == "Robot_1" && robot1Visual != null)
                visual = robot1Visual;
            else if (robotId == "Robot_2" && robot2Visual != null)
                visual = robot2Visual;
            
            if (visual != null)
            {
                visual.UpdateFromTelemetry(new Vector3(posX, posY, posZ), rotY, status);
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[TelemetryReceiver] No telemetry component found for {robotId}");
            }
        }
        catch (Exception e)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[TelemetryReceiver] Parse error: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        isRunning = false;
        
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            try { receiveThread.Abort(); } catch { }
        }
    }
    
    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
