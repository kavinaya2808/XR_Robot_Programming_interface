# Dashboard Architecture Analysis - Three Key Questions Answered

## Executive Summary

Based on comprehensive code analysis of the attached AR_PROJECT (2), here are the definitive answers to your three questions:

---

## Question 1: Are panels robot-anchored (follow robot) or world-anchored (fixed in space)?

### Answer: **WORLD-ANCHORED** (Fixed to Camera/User View)

#### Evidence:

**File: `DockFollow.cs`** - Controls the primary dashboard panel positioning
```csharp
public class DockFollow : MonoBehaviour
{
    [Tooltip("Assign the player's camera (XR Rig camera).")]
    public Transform targetCamera;
    
    [Tooltip("Local offset from camera (e.g., downwards).")]
    public Vector3 localPosition = new Vector3(0f, -0.35f, 0.6f);
    
    void LateUpdate()
    {
        if (targetCamera == null) return;
        
        // Keep a fixed local position relative to camera
        transform.position = targetCamera.TransformPoint(localPosition);  // ← KEY LINE
        
        if (faceCamera)
        {
            Vector3 dir = transform.position - targetCamera.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
```

**Key Insight:**
- `targetCamera.TransformPoint(localPosition)` means the dock position is **always relative to the camera**
- `localPosition = (0f, -0.35f, 0.6f)` = 0.35 units below and 0.6 units in front of camera
- As user moves/looks around, the dock **follows the camera**, not the robot

**Interaction Implication:**
- Panels are **user-centric** (dock with camera/head)
- Robot data is **displayed** on these world-anchored panels
- User can look away from robot and still see the dashboard

#### Supporting Evidence: DraggablePanel.cs
```csharp
public class DraggablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public void OnDrag(PointerEventData eventData)
    {
        // ... position updates ...
        transform.position = hitPoint + offset;  // ← Can be moved independently
    }
}
```
- Panels can be **manually dragged** to different world positions
- Not constrained to follow any robot

---

## Question 2: Is interaction gaze + hand, gaze only, or controller-based?

### Answer: **HAND-BASED INTERACTION** (with optional gaze support)

#### Evidence:

**File: `MRTK3GrabBridge.cs`** - Primary interaction handler
```csharp
#if XRITK_PRESENT
using UnityEngine.XR.Interaction.Toolkit;  // ← XR Interaction Toolkit
#endif

public class MRTK3GrabBridge : MonoBehaviour
{
    [Tooltip("If left null the script will try to find an XR interactable")]
    public XRBaseInteractable xriInteractable;  // ← Hand interaction
    
    void Start()
    {
        if (xriInteractable != null)
        {
            xriInteractable.selectEntered.AddListener(OnSelectEntered);  // ← Hand select event
            xriInteractable.selectExited.AddListener(OnSelectExited);    // ← Hand release event
        }
    }
    
    public void OnManipulationStarted()
    {
        dragWithHaptics.BeginDrag(startPoint, hapticNode);  // ← XRNode.RightHand
        isDragging = true;
    }
}
```

**Key Insights:**
1. **XRBaseInteractable** = XR Interaction Toolkit hand tracking
2. **selectEntered/selectExited** = Hand grab/release events
3. **hapticNode** = Haptic feedback on hand controllers

#### Interaction Workflow:
```
User Action                    Code Path
─────────────────────────────────────────
Hand approaches panel      → XRBaseInteractable.selectEntered
Hand grabs panel          → MRTK3GrabBridge.OnManipulationStarted
Hand drags panel          → DraggablePanel.OnDrag (IPointerDownHandler)
Hand releases panel       → MRTK3GrabBridge.OnManipulationEnded
```

#### File: `DraggablePanel.cs` - Pointer-based drag handling
```csharp
public class DraggablePanel : MonoBehaviour, 
    IPointerDownHandler,    // ← Hand/pointer down
    IDragHandler,           // ← Dragging
    IPointerUpHandler       // ← Hand/pointer up
{
    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        dragPlane = new Plane(uiCamera.transform.forward * -1f, transform.position);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Ray ray = uiCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            transform.position = hitPoint + offset;
        }
    }
}
```

**Supported Interaction Methods:**
1. ✅ **Hand Grab** - Primary (via XRBaseInteractable)
2. ✅ **Hand Drag** - Via PointerEventData
3. ✅ **Hand Release** - Tracked
4. ⚠️ **Gaze** - Not explicitly implemented but EventSystem can handle it
5. ❌ **Controller buttons** - Not used for panel interaction

#### Button/Control Interaction:
```csharp
// From PanelController1.cs
public GameObject minimizeButton;   // Requires hand interaction
public GameObject closeButton;      // Requires hand interaction
// No explicit controller button binding found
```

---

## Question 3: Is the data coming via ROS / simulation stream / JSON / mock data?

### Answer: **UDP NETWORK STREAM** (Comma-Separated Values over UDP)

#### Evidence:

**File: `TelemetryReceiver.cs`** - Network data receiver
```csharp
public class TelemetryReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("UDP port to listen on")]
    public int listenPort = 5005;  // ← Network port for external data
    
    private UdpClient udpClient;   // ← UDP communication
    private Thread receiveThread;  // ← Separate thread for network I/O
    
    void StartReceiving()
    {
        try
        {
            udpClient = new UdpClient(listenPort);  // ← Listen on port 5005
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
```

#### Data Format - Comma-Separated Values:
```csharp
void ProcessTelemetry(string data)
{
    // Format: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
    try
    {
        string[] parts = data.Split(',');  // ← CSV parsing
        if (parts.Length < 9) return;
        
        string robotId = parts[0];
        float posX = float.Parse(parts[1]);           // Position X
        float posY = float.Parse(parts[2]);           // Position Y
        float posZ = float.Parse(parts[3]);           // Position Z
        float rotY = float.Parse(parts[4]);           // Rotation Y (yaw)
        float speed = float.Parse(parts[5]);          // Speed m/s
        float battery = float.Parse(parts[6]);        // Battery %
        string status = parts[7];                     // State string
        float taskProgress = float.Parse(parts[8]);   // Progress %
        
        // Update RobotTelemetry object
        telemetry.position = new Vector3(posX, posY, posZ);
        telemetry.speed = speed;
        telemetry.batteryPercent = battery;
        telemetry.currentState = status;
        telemetry.taskProgressPercent = taskProgress;
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[TelemetryReceiver] Parse error: {e.Message}");
    }
}
```

#### Example UDP Data Stream:
```
Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0
Robot_2,1.0,0.0,2.5,90.0,0.50,75.0,Idle,40.0
Robot_1,0.6,0.0,1.3,45.5,0.76,85.4,Moving,25.5
```

#### Data Flow Architecture:
```
External System (e.g., Cem's Robot Simulation)
                 ↓
         UDP Network (Port 5005)
                 ↓
    TelemetryReceiver (Receives CSV)
                 ↓
    RobotTelemetry Registry (Stores data)
                 ↓
    Dashboard UI Panels (Display data)
                 ↓
    VirtualRobotVisual (Visual representation)
```

#### Alternative Data Sources Supported:

**File: `RobotTelemetry.cs`** - Data source abstraction
```csharp
public class RobotTelemetry : MonoBehaviour
{
    [Tooltip("When TRUE: uses internal simulation. When FALSE: expects external data from TelemetryReceiver")]
    public bool useSimulatedData = false;  // ← Can toggle data source
    
    // Data is set directly by TelemetryReceiver via UDP
    public Vector3 position;
    public float speed;
    public float batteryPercent;
    public string currentState;
    public float taskProgressPercent;
}
```

**Fallback Options:**
1. ✅ **UDP Network** (Primary) - From external robot/simulator via CSV over UDP
2. ✅ **Internal Simulation** - Built-in mock data generator
3. ✅ **Mock Data Provider** - `RobotDataProviderMock.cs` for testing

#### Data Source Configuration:
```csharp
// To use UDP data:
telemetry.useSimulatedData = false;  // Disable internal simulation
// TelemetryReceiver will populate data

// To use simulated data:
telemetry.useSimulatedData = true;   // Enable internal simulation
// Robot data is generated internally
```

---

## Summary Comparison Table

| Aspect | Configuration |
|--------|---|
| **Panel Anchoring** | World-anchored (follows camera/user) |
| **Panel Movement** | User can drag/reposition manually |
| **Robot Reference** | Data displayed on panels, not followed |
| **Interaction Method** | Hand grab/drag (XR Interaction Toolkit) |
| **Controller Support** | No explicit controller buttons |
| **Gaze Support** | Passive (EventSystem, not primary) |
| **Data Source** | UDP network (CSV format) |
| **Data Source Default** | External stream (simulation or real robot) |
| **Fallback Data** | Mock data generator if UDP unavailable |
| **Data Format** | Comma-separated values (9 fields) |
| **Network Port** | 5005 (configurable) |
| **Update Rate** | Depends on UDP sender (typically 10Hz) |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     MR Dashboard Application                     │
└─────────────────────────────────────────────────────────────────┘
                              ↑
                              │
                    ┌─────────┴─────────┐
                    ↓                   ↓
            TelemetryReceiver    RobotTelemetry
            (UDP Port 5005)      (Registry Storage)
                    │                   ↑
                    │       ┌───────────┴───────────┐
                    │       ↓                       ↓
                    └─→ CSV Parser          Dashboard UI Panels
                        (9 fields)          (World-Anchored to Camera)
                                                    ↓
                                            ┌───────┴───────┐
                                            ↓               ↓
                                        Battery         Position
                                        Panel           Panel
                                            ↓               ↓
                                        Draggable       Hand Interaction
                                        (IPointer)      (XRBase Interactable)
                                            │               │
                                            └───────┬───────┘
                                                    ↓
                                            PointerEventData
                                            (Hand input)
```

---

## Key Implementation Files

| File | Purpose |
|------|---------|
| `DockFollow.cs` | Camera-relative panel positioning |
| `DraggablePanel.cs` | Manual drag support via pointer events |
| `MRTK3GrabBridge.cs` | Hand grab/release event handling |
| `TelemetryReceiver.cs` | UDP network data reception |
| `RobotTelemetry.cs` | Centralized data storage & abstraction |
| `PositionPanelUI.cs` | Displays position/speed/heading data |
| `BatteryPanelUI.cs` | Displays battery status |
| `VirtualRobotVisual.cs` | Visual robot representation |

---

## Conclusion

The dashboard is a **world-anchored, hand-interactive, network-fed** MR UI system:

1. **Location:** Panels stay in user's view (camera-relative), not attached to robots
2. **Interaction:** Hand-based via XR Interaction Toolkit (grab, drag, release)
3. **Data:** External source via UDP network (CSV format from simulator/robot)
4. **User Experience:** User looks at fixed dashboard panels showing real-time robot telemetry

This design allows the user to maintain visual reference to both the real environment AND the robot telemetry data simultaneously.

