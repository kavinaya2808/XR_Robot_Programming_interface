# Telemetry Dashboard Technical Report
## Comprehensive Analysis of Robot Telemetry Visualization System

**Project:** AR_PROJECT (2) - Mixed Reality Dashboard for Robot Telemetry  
**Date:** Generated Report  
**Robots:** Robot_1, Robot_2

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Telemetry Data Generation](#telemetry-data-generation)
3. [Data Transmission Mechanisms](#data-transmission-mechanisms)
4. [Dashboard Visualization Architecture](#dashboard-visualization-architecture)
5. [UI Layer Architecture](#ui-layer-architecture)
6. [Technical Implementation Details](#technical-implementation-details)
7. [Data Flow Diagrams](#data-flow-diagrams)
8. [Component Reference](#component-reference)

---

## Executive Summary

This project implements a **Mixed Reality (MR) dashboard system** that visualizes real-time telemetry data from two simulated robots (Robot_1 and Robot_2). The system supports multiple data transmission methods (UDP network, ROS, and direct Unity bridge) and displays telemetry through world-anchored UI panels that follow the user's camera in MR space.

**Key Features:**
- Real-time telemetry visualization for two robots
- Multiple data transmission pathways (UDP, ROS, Direct Bridge)
- World-anchored UI panels (camera-relative positioning)
- Hand-based interaction for panel manipulation
- Modular architecture with registry-based data storage

---

## Telemetry Data Generation

### 1. Robot Simulation Components

#### A. TelemetryPublisher (ROS-based)
**Location:** `Assets/Robotics/Scripts/TelemetryPublisher.cs`

**Purpose:** Publishes robot telemetry data via ROS (Robot Operating System) topics.

**Data Generation Process:**

```csharp
// Position tracking from base_link transform
Vector3 dp = baseLink.position - lastPos;
LinSpeed = dp.magnitude / dt;  // Linear speed (m/s)

// Angular velocity calculation
float dYawDeg = Mathf.DeltaAngle(lastYawDeg, yawDeg);
AngSpeed = (dYawDeg * Mathf.Deg2Rad) / dt;  // Angular speed (rad/s)

// Battery model (physics-based)
bool moving = (LinSpeed > 0.02f) || (Mathf.Abs(AngSpeed) > 0.02f);
float drain = moving
    ? moveDrainBase + linDrainPerMs * LinSpeed + angDrainPerRad * Mathf.Abs(AngSpeed)
    : idleDrainPerSecond;
Battery = Mathf.Max(0f, Battery - drain * dt);
```

**Generated Telemetry Fields:**
- **Position:** `baseLink.position` (Unity world coordinates)
- **Linear Speed:** Calculated from position delta over time (m/s)
- **Angular Speed:** Calculated from yaw rotation delta (rad/s)
- **Battery:** Physics-based drain model (0-100%)
- **Mode:** Control mode (keyboard/ROS)
- **Timestamp:** Unix timestamp in seconds

**Publishing Format:**
- **Topic:** `/{robotName}/telemetry` (e.g., `/robot1/telemetry`)
- **Message Type:** `StringMsg` (JSON serialized)
- **Rate:** Configurable (default: 10 Hz)

**JSON Structure:**
```json
{
  "stamp": 1234567890.123,
  "x": 0.5,
  "y": 1.2,
  "yaw": 0.785,
  "lin": 0.75,
  "ang": 0.1,
  "battery": 85.5,
  "mode": "ros"
}
```

#### B. TelemetrySender (UDP-based)
**Location:** `Assets/Robotics/Scripts/TelemetrySender.cs`

**Purpose:** Sends telemetry data over UDP network to dashboard.

**Data Generation Process:**

```csharp
// Speed calculation from transform movement
Vector3 velocity = (robotTransform.position - lastPosition) / dt;
speed = velocity.magnitude;

// Status determination
if (speed > 0.1f) status = "Exploring";
else if (speed > 0.01f) status = "Moving";
else status = "Idle";

// Task progress from coverage manager
if (coverageManager != null)
    taskProgress = coverageManager.GetCoverageFraction() * 100f;

// Battery simulation (slow drain)
batteryPercent = Mathf.Max(0f, batteryPercent - Time.deltaTime * 0.005f);
```

**Generated Telemetry Fields:**
- **Position:** `robotTransform.position` (X, Y, Z)
- **Rotation:** `robotTransform.eulerAngles.y` (Yaw angle)
- **Speed:** Magnitude of velocity vector (m/s)
- **Battery:** Simulated percentage (0-100%)
- **Status:** State string ("Idle", "Moving", "Exploring")
- **Task Progress:** Coverage percentage from MapCoverageManager

**Transmission Format:**
- **Protocol:** UDP
- **Port:** Configurable (default: 5005)
- **Format:** CSV (Comma-Separated Values)
- **Rate:** Configurable (default: 10 Hz)

**CSV Format:**
```
robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
```

**Example:**
```
Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0
Robot_2,1.0,0.0,2.5,90.0,0.50,75.0,Idle,40.0
```

#### C. DirectTelemetryBridge (Unity Direct)
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/DirectTelemetryBridge.cs`

**Purpose:** Directly bridges telemetry from robot simulation to dashboard within the same Unity instance (no network required).

**Data Synchronization Process:**

```csharp
// Position sync from robot's base_link
targetDashboard.position = sourceRobot.baseLink.position;

// Speed sync
targetDashboard.speed = sourceRobot.LinSpeed;
targetDashboard.speed_mps = sourceRobot.LinSpeed;

// Battery sync
targetDashboard.batteryPercent = sourceRobot.Battery;

// Status mapping
if (sourceRobot.LinSpeed > 0.05f)
    targetDashboard.currentState = $"Moving ({mode})";
else if (targetDashboard.batteryPercent <= 10f)
    targetDashboard.currentState = "Low Battery";
else
    targetDashboard.currentState = $"Idle ({mode})";
```

**Update Frequency:** Every frame (60+ Hz)

---

## Data Transmission Mechanisms

### 1. UDP Network Transmission

#### Architecture:
```
Robot Simulation (TelemetrySender)
    ↓
UDP Socket (System.Net.Sockets.UdpClient)
    ↓
Network Layer (Ethernet/WiFi)
    ↓
Dashboard (TelemetryReceiver)
```

#### Implementation Details:

**Sender Side (`TelemetrySender.cs`):**
- **Socket Type:** `UdpClient` (connectionless)
- **Target:** Configurable IP address and port
- **Threading:** Main Unity thread (Update loop)
- **Error Handling:** Try-catch with warning logs

**Receiver Side (`TelemetryReceiver.cs`):**
- **Socket Type:** `UdpClient` (listening on port)
- **Port:** 5005 (default, configurable)
- **Threading:** Background thread for receiving, main thread for processing
- **Thread Safety:** Queue-based with lock mechanism

**Thread-Safe Data Queue:**
```csharp
private readonly Queue<string> receivedDataQueue = new Queue<string>();
private readonly object queueLock = new object();

// Background thread: Receive and enqueue
void ReceiveData() {
    byte[] data = udpClient.Receive(ref remoteEP);
    string message = Encoding.UTF8.GetString(data);
    lock (queueLock) {
        receivedDataQueue.Enqueue(message);
    }
}

// Main thread: Process dequeued data
void Update() {
    lock (queueLock) {
        while (receivedDataQueue.Count > 0) {
            string data = receivedDataQueue.Dequeue();
            ProcessTelemetry(data);
        }
    }
}
```

**Data Parsing:**
```csharp
// CSV parsing with culture-invariant float parsing
string[] parts = data.Split(',');
float posX = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
float posY = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
// ... etc
```

### 2. ROS Transmission

#### Architecture:
```
Robot Simulation (TelemetryPublisher)
    ↓
ROS Connection (Unity Robotics ROS-TCP-Connector)
    ↓
ROS Topic: /{robotName}/telemetry
    ↓
External ROS System (optional)
```

#### Implementation Details:

**Publisher (`TelemetryPublisher.cs`):**
- **ROS Connection:** `ROSConnection.GetOrCreateInstance()`
- **Topic Registration:** `ros.RegisterPublisher<StringMsg>($"/{robotName}/telemetry")`
- **Message Serialization:** JSON via `JsonUtility.ToJson()`
- **Publishing Rate:** Configurable Hz (default: 10 Hz)

**Message Format:**
- **Type:** `StringMsg` (ROS standard string message)
- **Content:** JSON serialized telemetry object
- **Timestamp:** Unix timestamp (seconds since epoch)

### 3. Direct Unity Bridge

#### Architecture:
```
Robot GameObject (TelemetryPublisher component)
    ↓
DirectTelemetryBridge (Update loop)
    ↓
Dashboard GameObject (RobotTelemetry component)
```

**Advantages:**
- No network overhead
- Lowest latency
- No serialization/deserialization
- Same Unity instance

**Use Case:** When robot simulation and dashboard run in the same Unity scene.

---

## Dashboard Visualization Architecture

### 1. Data Reception and Storage

#### TelemetryReceiver Component
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/TelemetryReceiver.cs`

**Responsibilities:**
1. Listen for UDP packets on port 5005
2. Parse CSV telemetry data
3. Update RobotTelemetry registry
4. Update VirtualRobotVisual components

**Processing Flow:**
```csharp
void ProcessTelemetry(string data) {
    // 1. Parse CSV: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
    string[] parts = data.Split(',');
    
    // 2. Find target RobotTelemetry by ID
    RobotTelemetry telemetry = RobotTelemetry.Registry[robotId];
    
    // 3. Disable internal simulation
    telemetry.useSimulatedData = false;
    
    // 4. Update telemetry fields
    telemetry.position = new Vector3(posX, posY, posZ);
    telemetry.speed = speed;
    telemetry.batteryPercent = battery;
    telemetry.currentState = status;
    telemetry.taskProgressPercent = taskProgress;
    
    // 5. Update transform for UI tracking
    telemetry.transform.position = new Vector3(posX, posY, posZ);
    telemetry.transform.rotation = Quaternion.Euler(0, rotY, 0);
    
    // 6. Update visual representation
    visual.UpdateFromTelemetry(position, rotY, status);
}
```

#### RobotTelemetry Registry
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/RobotTelemetry.cs`

**Registry Pattern:**
```csharp
public static readonly Dictionary<string, RobotTelemetry> Registry = 
    new Dictionary<string, RobotTelemetry>();

void OnEnable() {
    Registry[robotId] = this;
}

void OnDisable() {
    Registry.Remove(robotId);
}
```

**Data Storage:**
- **Position:** `Vector3` (world coordinates)
- **Velocity:** `Vector3` (smoothed)
- **Speed:** `float` (m/s)
- **Heading:** `Vector3` (normalized direction)
- **Battery:** `float` (0-100%)
- **State:** `string` ("Idle", "Moving", "Exploring", etc.)
- **Task Progress:** `float` (0-100%)
- **Active Task:** `string` ("Map Coverage", "None", etc.)

**Data Source Modes:**
1. **Simulated Mode (`useSimulatedData = true`):**
   - Calculates telemetry from transform movement
   - Simulates battery drain
   - Computes velocity from position deltas

2. **External Mode (`useSimulatedData = false`):**
   - Expects data from TelemetryReceiver or DirectTelemetryBridge
   - Skips internal calculations
   - Monitors connection timeout (5 seconds)

### 2. Virtual Robot Visualization

#### VirtualRobotVisual Component
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/VirtualRobotVisual.cs`

**Purpose:** Visual representation of robot in MR space.

**Features:**
- Smooth position interpolation
- Status-based color changes
- MR alignment offset support
- Configurable visual scale

**Update Process:**
```csharp
void Update() {
    // Smooth movement to target
    transform.position = Vector3.Lerp(
        transform.position, 
        targetPosition + positionOffset, 
        Time.deltaTime * positionSmoothSpeed
    );
    
    // Smooth rotation
    transform.rotation = Quaternion.Slerp(
        transform.rotation, 
        targetRotation, 
        Time.deltaTime * rotationSmoothSpeed
    );
    
    // Status color update
    if (statusRenderer != null) {
        Color targetColor = GetColorForStatus(currentStatus);
        statusRenderer.material.color = Color.Lerp(
            statusRenderer.material.color, 
            targetColor, 
            Time.deltaTime * 5f
        );
    }
}
```

---

## UI Layer Architecture

### 1. Panel Positioning System

#### DockFollow Component
**Location:** `Assets/Dashboard_for_MR/Scripts/DockFollow.cs`

**Architecture:** **World-Anchored (Camera-Relative)**

**Key Implementation:**
```csharp
void LateUpdate() {
    // Transform local offset to world space relative to camera
    transform.position = targetCamera.TransformPoint(localPosition);
    
    // Face camera (optional)
    if (faceCamera) {
        Vector3 dir = transform.position - targetCamera.position;
        dir.y = 0; // Prevent tilting with head pitch
        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
```

**Positioning Parameters:**
- **Offset X:** 0 units (centered horizontally)
- **Offset Y:** -0.35 units (35cm below eye level)
- **Offset Z:** 0.6 units (60cm in front of face)
- **Update Frequency:** Every frame (LateUpdate)

**Behavior:**
- Panels follow user's camera/viewpoint
- Panels do NOT follow robots
- User can look away from robots and still see dashboard
- Panels maintain fixed position relative to user's head

### 2. Panel Interaction System

#### MRTK3GrabBridge Component
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/MRTK3GrabBridge.cs`

**Interaction Method:** **Hand-Based (Primary)**

**Implementation:**
```csharp
// XR Interaction Toolkit integration
xriInteractable.selectEntered.AddListener(OnSelectEntered);
xriInteractable.selectExited.AddListener(OnSelectExited);

private void OnSelectEntered(SelectEnterEventArgs args) {
    // Begin drag on hand grab
    dragWithHaptics.BeginDrag(attachTransform.position, hapticNode);
    isDragging = true;
    StartCoroutine(XRDragUpdater());
}

private void OnSelectExited(SelectExitEventArgs args) {
    // End drag on hand release
    dragWithHaptics.EndDrag(true);
    isDragging = false;
}
```

**Supported Interactions:**
- ✅ **Hand Grab:** Primary interaction via XRBaseInteractable
- ✅ **Hand Drag:** Move panels in 3D space
- ✅ **Hand Release:** Drop panels in new position
- ✅ **Haptic Feedback:** Optional vibration on grab/release
- ⚠️ **Gaze:** Passive (not primary interaction)
- ❌ **Controller Buttons:** Not implemented

### 3. UI Panel Components

#### A. BatteryPanelUI
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/BatteryPanelUI.cs`

**Displayed Data:**
- Battery percentage (0-100%)
- Estimated time remaining
- Power consumption (Watts)
- Battery icon (6 states: Full, QuadFill, HalfFill, QuarterFill, Less, Empty)
- Power consumption sparkline graph

**Update Process:**
```csharp
public void Refresh() {
    RobotTelemetry t = RobotTelemetry.Registry[robotId];
    
    // Update text fields
    chargePercentText.text = $"{t.batteryPercent:F0}%";
    estimatedTimeText.text = FormatSeconds(t.EstimatedBatterySecondsRemaining());
    powerConsumptionText.text = $"{powerW:F2} W";
    
    // Update visual battery icon
    UpdateBatteryVisual(t.batteryPercent);
    
    // Update fill tube with color thresholds
    targetFill = t.batteryPercent / 100f;
    if (t.batteryPercent > 75f) tubeFillImage.color = colorHigh;
    else if (t.batteryPercent > 50f) tubeFillImage.color = colorMedium;
    // ... etc
}
```

**Visual Features:**
- Smooth fill animation (Lerp interpolation)
- Color-coded thresholds (High/Medium/Low/Critical)
- Power consumption sparkline (time-series graph)
- Battery icon state machine

**Update Rate:** Configurable (default: 2 Hz)

#### B. PositionPanelUI
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/PositionPanelUI.cs`

**Displayed Data:**
- Position (X, Y, Z coordinates)
- Velocity vector (Vx, Vy, Vz)
- Speed magnitude (m/s)
- Heading angle (degrees)

**Update Process:**
```csharp
public void Refresh() {
    RobotTelemetry t = RobotTelemetry.Registry[robotId];
    
    // Position display
    positionText.text = $"<b>{robotId}({p.x:F2}, {p.y:F2}, {p.z:F2})";
    
    // Velocity display
    velocityText.text = $"Vx: {v.x:F2}  Vy: {v.y:F2}  Vz: {v.z:F2}";
    
    // Speed display
    speedText.text = $"{t.GetSpeed():F2} m/s";
    
    // Heading display (yaw angle)
    float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    headingText.text = $"{yaw:F0}°";
}
```

**Update Rate:** Configurable (default: 10 Hz)

#### C. RobotStatusPanelUI
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/RobotStatusPanelUI.cs`

**Displayed Data:**
- Robot ID
- Current state ("Idle", "Moving", "Exploring", etc.)
- Active task name

**Update Process:**
```csharp
public void Refresh() {
    RobotTelemetry t = RobotTelemetry.Registry[robotId];
    
    robotIdText.text = t.robotId;
    stateText.text = t.currentState;
    activeTaskText.text = t.activeTask;
}
```

**Update Rate:** Configurable (default: 5 Hz)

#### D. TaskProgressPanelUI
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/TaskProgressPanelUI.cs`

**Displayed Data:**
- Task name ("Map Coverage")
- Progress percentage (0-100%)
- Estimated time remaining (ETA)
- Task status (Incomplete/Completed/Expired)

**Update Process:**
```csharp
public void Refresh() {
    RobotTelemetry t = RobotTelemetry.Registry[robotId];
    
    taskNameText.text = "Map Coverage";
    progressPercentText.text = $"{t.taskProgressPercent:F0}%";
    
    // ETA display based on task status
    if (t.taskStatus == TaskStatus.Completed)
        etaText.text = "Done";
    else if (t.taskStatus == TaskStatus.Expired)
        etaText.text = "Timeout";
    else
        etaText.text = $"{Mathf.CeilToInt(t.taskEstimatedSecondsRemaining)}s";
}
```

**Update Rate:** Configurable (default: 4 Hz)

### 4. Panel Layout Management

#### PanelLayoutManager
**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/PanelLayoutManager.cs`

**Purpose:** Manages spatial arrangement of multiple panels.

**Features:**
- Grid-based layout
- Spacing configuration
- Panel grouping by robot ID
- Dynamic panel addition/removal

---

## Technical Implementation Details

### 1. Thread Safety

**Challenge:** UDP receiving happens on background thread, but Unity API calls must be on main thread.

**Solution:** Queue-based thread-safe data transfer.

```csharp
// Background thread (ReceiveData)
lock (queueLock) {
    receivedDataQueue.Enqueue(message);
}

// Main thread (Update)
lock (queueLock) {
    while (receivedDataQueue.Count > 0) {
        ProcessTelemetry(receivedDataQueue.Dequeue());
    }
}
```

### 2. Data Source Abstraction

**Pattern:** Registry-based lookup with fallback to direct reference.

```csharp
// Try direct reference first
if (robotTelemetry != null) {
    t = robotTelemetry;
}
// Fallback to registry lookup
else if (RobotTelemetry.Registry.TryGetValue(robotId, out t)) {
    // Use registry entry
}
```

### 3. Update Rate Optimization

**Strategy:** Throttled updates per panel type.

```csharp
// Throttle updates based on Hz setting
if (Time.time < nextUpdateTime) return;
nextUpdateTime = Time.time + 1f / updateHz;
Refresh();
```

**Rationale:**
- Battery panel: 2 Hz (slow-changing data)
- Position panel: 10 Hz (fast-changing data)
- Status panel: 5 Hz (moderate changes)
- Task progress: 4 Hz (moderate changes)

### 4. Smooth Animation

**Technique:** Lerp interpolation for visual updates.

```csharp
// Smooth fill animation
displayedFill = Mathf.Lerp(
    displayedFill, 
    targetFill, 
    Time.deltaTime * fillSmoothSpeed
);
```

**Benefits:**
- Reduces visual jitter
- Smooth transitions
- Configurable smoothness

### 5. Error Handling

**Defensive Programming:**
- Null checks before UI updates
- Try-catch for network operations
- Graceful degradation on missing data
- Connection timeout detection

```csharp
// Defensive null check
if (!RobotTelemetry.Registry.TryGetValue(robotId, out var t)) {
    chargePercentText.text = "No robot";
    return;
}
```

---

## Data Flow Diagrams

### Complete System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    ROBOT SIMULATION LAYER                        │
│                                                                 │
│  ┌──────────────┐         ┌──────────────┐                     │
│  │   Robot_1    │         │   Robot_2    │                     │
│  │              │         │              │                     │
│  │ • Transform  │         │ • Transform  │                     │
│  │ • Movement   │         │ • Movement   │                     │
│  │ • Coverage   │         │ • Coverage   │                     │
│  └──────┬───────┘         └──────┬───────┘                     │
│         │                        │                              │
│         │ TelemetryPublisher     │ TelemetryPublisher          │
│         │ (ROS)                  │ (ROS)                       │
│         │                        │                              │
│         │ TelemetrySender        │ TelemetrySender             │
│         │ (UDP)                  │ (UDP)                       │
│         │                        │                              │
│         └────────┬───────────────┘                              │
│                  │                                              │
└──────────────────┼──────────────────────────────────────────────┘
                   │
                   │ Data Transmission
                   │
┌──────────────────┼──────────────────────────────────────────────┐
│    TRANSMISSION LAYER                                          │
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐                 │
│  │   UDP Network     │  │   ROS Topics      │                 │
│  │   Port 5005       │  │   /robot1/        │                 │
│  │   CSV Format      │  │   /robot2/        │                 │
│  └────────┬──────────┘  └────────┬──────────┘                 │
│           │                      │                              │
│           │                      │                              │
│  ┌────────┴──────────┐  ┌──────┴──────────┐                 │
│  │ DirectUnityBridge  │  │  (External ROS)  │                 │
│  │ (Same Instance)    │  │  (Optional)      │                 │
│  └────────────────────┘  └──────────────────┘                 │
│                                                                 │
└──────────────────┼──────────────────────────────────────────────┘
                   │
                   │ Received Data
                   │
┌──────────────────┼──────────────────────────────────────────────┐
│    DASHBOARD RECEPTION LAYER                                   │
│                                                                 │
│  ┌──────────────────────────────────────┐                     │
│  │      TelemetryReceiver                │                     │
│  │  • UDP Socket (Port 5005)            │                     │
│  │  • CSV Parser                         │                     │
│  │  • Thread-Safe Queue                  │                     │
│  └──────────────┬───────────────────────┘                     │
│                 │                                              │
│                 │ Update Registry                               │
│                 │                                              │
│  ┌──────────────┴───────────────────────┐                     │
│  │   RobotTelemetry Registry             │                     │
│  │   Dictionary<string, RobotTelemetry>  │                     │
│  │                                        │                     │
│  │   • Robot_1 → RobotTelemetry          │                     │
│  │   • Robot_2 → RobotTelemetry          │                     │
│  └──────────────┬───────────────────────┘                     │
│                 │                                              │
└──────────────────┼──────────────────────────────────────────────┘
                   │
                   │ Data Lookup
                   │
┌──────────────────┼──────────────────────────────────────────────┐
│    UI VISUALIZATION LAYER                                       │
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐     │
│  │         DockFollow (Camera-Relative Positioning)     │     │
│  │         Offset: (0, -0.35, 0.6)                      │     │
│  └────────────────────┬──────────────────────────────────┘     │
│                       │                                        │
│         ┌─────────────┴─────────────┐                         │
│         │                            │                         │
│  ┌──────▼──────┐          ┌─────────▼─────────┐             │
│  │ Robot_1     │          │ Robot_2            │             │
│  │ Panels      │          │ Panels             │             │
│  │             │          │                   │             │
│  │ • Battery   │          │ • Battery         │             │
│  │ • Position  │          │ • Position        │             │
│  │ • Status    │          │ • Status          │             │
│  │ • Task      │          │ • Task            │             │
│  └─────────────┘          └───────────────────┘             │
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐     │
│  │    VirtualRobotVisual (MR Space Representation)      │     │
│  │    • Smooth Position Interpolation                  │     │
│  │    • Status-Based Color Changes                    │     │
│  └─────────────────────────────────────────────────────┘     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow: UDP Transmission Path

```
Robot_1 (TelemetrySender)
    │
    │ Update() loop (10 Hz)
    │
    ├─ Calculate speed from transform
    ├─ Get battery percentage
    ├─ Get status string
    ├─ Get task progress from MapCoverageManager
    │
    │ Format CSV: "Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0"
    │
    │ UDP Send (UdpClient.Send)
    │
    ▼
Network (UDP Port 5005)
    │
    ▼
TelemetryReceiver (Background Thread)
    │
    ├─ Receive UDP packet
    ├─ Enqueue to thread-safe queue
    │
    ▼
TelemetryReceiver (Main Thread - Update)
    │
    ├─ Dequeue from queue
    ├─ Parse CSV string
    ├─ Extract 9 fields
    │
    ▼
RobotTelemetry Registry
    │
    ├─ Lookup by robotId ("Robot_1")
    ├─ Set useSimulatedData = false
    ├─ Update position, speed, battery, status, taskProgress
    ├─ Update transform.position and transform.rotation
    │
    ▼
UI Panels (BatteryPanelUI, PositionPanelUI, etc.)
    │
    ├─ Refresh() called at configured Hz
    ├─ Lookup RobotTelemetry from Registry
    ├─ Update TextMeshProUGUI fields
    ├─ Update visual elements (fill bars, icons, etc.)
    │
    ▼
User sees updated dashboard in MR space
```

### Data Flow: Direct Unity Bridge Path

```
Robot_1 GameObject
    │
    ├─ TelemetryPublisher component
    │  ├─ baseLink.transform (position/rotation)
    │  ├─ LinSpeed (calculated)
    │  ├─ Battery (simulated)
    │  └─ Mode (keyboard/ROS)
    │
    ▼
DirectTelemetryBridge (Update loop - 60+ Hz)
    │
    ├─ Read from sourceRobot (TelemetryPublisher)
    ├─ Write to targetDashboard (RobotTelemetry)
    │
    ├─ Sync position: targetDashboard.position = sourceRobot.baseLink.position
    ├─ Sync speed: targetDashboard.speed = sourceRobot.LinSpeed
    ├─ Sync battery: targetDashboard.batteryPercent = sourceRobot.Battery
    ├─ Map status: targetDashboard.currentState = MapStatus(sourceRobot)
    │
    ▼
RobotTelemetry Registry
    │
    ├─ Robot_1 entry updated
    │
    ▼
UI Panels
    │
    ├─ Refresh() reads from Registry
    ├─ Updates displayed values
    │
    ▼
User sees updated dashboard
```

---

## Component Reference

### Core Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **TelemetryReceiver** | `Assets/Dashboard_for_MR/Scripts/D_2/TelemetryReceiver.cs` | Receives UDP telemetry, parses CSV, updates registry |
| **RobotTelemetry** | `Assets/Dashboard_for_MR/Scripts/D_2/RobotTelemetry.cs` | Central data storage, registry management |
| **TelemetrySender** | `Assets/Robotics/Scripts/TelemetrySender.cs` | Sends robot telemetry over UDP |
| **TelemetryPublisher** | `Assets/Robotics/Scripts/TelemetryPublisher.cs` | Publishes robot telemetry via ROS |
| **DirectTelemetryBridge** | `Assets/Dashboard_for_MR/Scripts/D_2/DirectTelemetryBridge.cs` | Direct Unity-to-Unity telemetry bridge |

### UI Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **DockFollow** | `Assets/Dashboard_for_MR/Scripts/DockFollow.cs` | Camera-relative panel positioning |
| **BatteryPanelUI** | `Assets/Dashboard_for_MR/Scripts/D_2/BatteryPanelUI.cs` | Battery status display |
| **PositionPanelUI** | `Assets/Dashboard_for_MR/Scripts/D_2/PositionPanelUI.cs` | Position/velocity/speed display |
| **RobotStatusPanelUI** | `Assets/Dashboard_for_MR/Scripts/D_2/RobotStatusPanelUI.cs` | Robot status display |
| **TaskProgressPanelUI** | `Assets/Dashboard_for_MR/Scripts/D_2/TaskProgressPanelUI.cs` | Task progress display |
| **VirtualRobotVisual** | `Assets/Dashboard_for_MR/Scripts/D_2/VirtualRobotVisual.cs` | Visual robot representation in MR |

### Interaction Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **MRTK3GrabBridge** | `Assets/Dashboard_for_MR/Scripts/D_2/MRTK3GrabBridge.cs` | Hand grab/drag interaction bridge |
| **DraggablePanel** | `Assets/Dashboard_for_MR/Scripts/DraggablePanel.cs` | Panel drag handler |

### Manager Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **MRSceneManager** | `Assets/Scripts/MRSceneManager.cs` | Manages combined MR scene, connects robots to dashboard |
| **PanelLayoutManager** | `Assets/Dashboard_for_MR/Scripts/D_2/PanelLayoutManager.cs` | Manages panel spatial layout |

---

## Technical Specifications

### Network Configuration

| Parameter | Value | Description |
|-----------|-------|-------------|
| **Protocol** | UDP | Connectionless datagram protocol |
| **Port** | 5005 | Default listening port (configurable) |
| **Format** | CSV | Comma-separated values |
| **Encoding** | UTF-8 | Character encoding |
| **Rate** | 10 Hz | Default transmission rate (configurable) |

### Data Format Specifications

#### UDP CSV Format
```
robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
```

**Field Descriptions:**
- `robotId`: String identifier ("Robot_1", "Robot_2")
- `posX`: Position X coordinate (float, meters)
- `posY`: Position Y coordinate (float, meters)
- `posZ`: Position Z coordinate (float, meters)
- `rotY`: Rotation Y (yaw angle, float, degrees)
- `speed`: Speed magnitude (float, m/s)
- `battery`: Battery percentage (float, 0-100)
- `status`: State string ("Idle", "Moving", "Exploring", etc.)
- `taskProgress`: Task progress percentage (float, 0-100)

#### ROS JSON Format
```json
{
  "stamp": 1234567890.123,
  "x": 0.5,
  "y": 1.2,
  "yaw": 0.785,
  "lin": 0.75,
  "ang": 0.1,
  "battery": 85.5,
  "mode": "ros"
}
```

### Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| **UDP Latency** | < 10ms | Local network |
| **Update Rate (UI)** | 2-10 Hz | Panel-dependent |
| **Registry Lookup** | O(1) | Dictionary-based |
| **Thread Safety** | Queue-based | Lock-protected |
| **Memory Usage** | Low | Minimal allocations |

### Supported Platforms

- **Unity Version:** 2021.3+ (MRTK3 compatible)
- **XR Platforms:** Meta Quest 3, Windows Mixed Reality
- **Networking:** Windows, macOS, Linux (UDP support)
- **ROS:** Unity Robotics ROS-TCP-Connector

---

## Conclusion

This telemetry dashboard system provides a comprehensive solution for visualizing robot data in Mixed Reality. The architecture supports multiple data transmission methods, ensuring flexibility for different deployment scenarios. The world-anchored UI design provides an intuitive user experience, while the modular component structure allows for easy extension and customization.

**Key Strengths:**
- Multiple transmission pathways (UDP, ROS, Direct)
- Thread-safe network reception
- Efficient registry-based data storage
- Smooth visual animations
- Hand-based interaction support
- Modular, extensible architecture

**Potential Enhancements:**
- WebSocket support for web-based dashboards
- Historical data logging and playback
- Customizable panel layouts
- Multi-robot aggregation views
- Alert/notification system

---

**Report Generated:** Comprehensive analysis of telemetry visualization system  
**Codebase Version:** AR_PROJECT (2)  
**Analysis Date:** Current

