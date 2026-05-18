# Dashboard Telemetry Technical Workflow - Complete Guide

## Table of Contents
1. [System Overview](#system-overview)
2. [Data Generation (Robot Side)](#data-generation-robot-side)
3. [Network Transmission](#network-transmission)
4. [Data Reception & Parsing](#data-reception--parsing)
5. [Display on UI Panels](#display-on-ui-panels)
6. [Complete Data Flow Diagram](#complete-data-flow-diagram)
7. [Battery Calculation Details](#battery-calculation-details)
8. [Speed Calculation Details](#speed-calculation-details)
9. [Position & Heading Calculation](#position--heading-calculation)

---

## System Overview

The dashboard telemetry system is a **complete pipeline** that takes raw robot state data and displays it on MR UI panels.

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPLETE TELEMETRY PIPELINE                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: DATA GENERATION (Robot/Simulation)                      │
│ Location: Assets/Scripts/BatteryModel.cs                        │
│           Assets/Robotics/Scripts/TelemetryPublisher.cs         │
│           Assets/Dashboard_for_MR/Scripts/D_2/RobotTelemetry.cs │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ STEP 2: NETWORK TRANSMISSION (UDP)                              │
│ Location: Assets/Robotics/Scripts/TelemetryPublisher.cs         │
│ Port: 5005 (default)                                            │
│ Format: CSV (9 comma-separated values)                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ STEP 3: NETWORK RECEPTION & PARSING                             │
│ Location: Assets/Dashboard_for_MR/Scripts/D_2/TelemetryReceiver │
│ Listens on UDP port 5005                                        │
│ Parses CSV into structured data                                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ STEP 4: DATA STORAGE                                            │
│ Location: Assets/Dashboard_for_MR/Scripts/D_2/RobotTelemetry.cs │
│ Registry: Dictionary<string, RobotTelemetry>                    │
│ Access: RobotTelemetry.Registry["Robot_1"]                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ STEP 5: DISPLAY ON UI                                           │
│ Location: Assets/Dashboard_for_MR/Scripts/D_2/                  │
│           - BatteryPanelUI.cs                                   │
│           - PositionPanelUI.cs                                  │
│           - TaskProgressPanelUI.cs                              │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Generation (Robot Side)

### Phase 1: Speed Calculation
**File:** `TelemetryPublisher.cs` (Lines 68-77)

#### Linear Speed Calculation:
```csharp
Vector3 dp = baseLink.position - lastPos;
LinSpeed = dp.magnitude / dt;
lastPos = baseLink.position;
```

**Formula:**
```
LinSpeed (m/s) = Distance Traveled (m) / Time Delta (s)
                = |CurrentPosition - LastPosition| / Δt
```

**Example:**
- Robot at frame N: position = (1.0, 0.0, 2.5)
- Robot at frame N+1: position = (1.2, 0.0, 2.7)
- Time delta: 0.016 seconds (60 FPS)

```
Distance = √((1.2-1.0)² + (0.0-0.0)² + (2.7-2.5)²)
         = √(0.04 + 0 + 0.04)
         = √0.08
         = 0.283 m

LinSpeed = 0.283 m / 0.016 s = 17.69 m/s
```

#### Angular Speed Calculation:
```csharp
float yawDeg = baseLink.eulerAngles.y;
float dYawDeg = Mathf.DeltaAngle(lastYawDeg, yawDeg);
AngSpeed = (dYawDeg * Mathf.Deg2Rad) / dt;
lastYawDeg = yawDeg;
```

**Formula:**
```
AngSpeed (rad/s) = Rotation Change (degrees) × (π/180) / Time Delta (s)
```

**Example:**
- Yaw at frame N: 0°
- Yaw at frame N+1: 45°
- Time delta: 0.016 seconds

```
AngSpeed = 45° × (π/180) / 0.016 s
         = 0.785 rad / 0.016 s
         = 49.06 rad/s
```

### Phase 2: Battery Calculation
**File:** `TelemetryPublisher.cs` (Lines 79-86)

The battery drain is calculated based on robot activity:

```csharp
bool moving = (LinSpeed > 0.02f) || (Mathf.Abs(AngSpeed) > 0.02f);
float drain = moving
    ? moveDrainBase + linDrainPerMs * LinSpeed + angDrainPerRad * Mathf.Abs(AngSpeed)
    : idleDrainPerSecond;

Battery = Mathf.Max(0f, Battery - drain * dt);
```

#### Battery Drain Formula (When Moving):
```
Power Consumption (W/s) = moveDrainBase + (linDrainPerMs × LinSpeed) + (angDrainPerRad × |AngSpeed|)
Battery Drop = Power Consumption × Time Delta (seconds)
```

**Default Parameters:**
```
idleDrainPerSecond = 0.001 (0.1% per second when idle)
moveDrainBase = 0.004 (base drain when moving)
linDrainPerMs = 0.010 (additional 0.01% per m/s of linear speed)
angDrainPerRad = 0.005 (additional 0.005% per rad/s of angular speed)
```

**Example (Moving Robot):**
- Current Battery: 85%
- LinSpeed: 1.5 m/s
- AngSpeed: 0.5 rad/s
- Time Delta: 0.016 seconds (60 FPS)

```
Power = 0.004 + (0.010 × 1.5) + (0.005 × 0.5)
      = 0.004 + 0.015 + 0.0025
      = 0.0215 %/sec

Battery Drop = 0.0215 × 0.016 = 0.000344%

New Battery = 85.0 - 0.000344 = 84.9997%
```

**Example (Idle Robot):**
- Current Battery: 85%
- LinSpeed: 0 m/s
- AngSpeed: 0 rad/s
- Time Delta: 0.016 seconds

```
Power = idleDrainPerSecond = 0.001%/sec

Battery Drop = 0.001 × 0.016 = 0.000016%

New Battery = 85.0 - 0.000016 = 84.99998%
```

### Phase 3: Position & Heading Extraction
**File:** `TelemetryPublisher.cs` (Lines 53-61)

Position comes directly from the robot's transform:
```csharp
public float x;     // Unity X
public float y;     // Unity Z (converted to world Y)
public float yaw;   // radians (from Unity yaw angle)
```

**Heading Formula:**
```
Heading (radians) = Robot.EulerAngles.y × (π / 180)
```

### Phase 4: Publish Data
**File:** `TelemetryPublisher.cs` (Lines 87-101)

Data is published at a fixed rate (default 10 Hz):

```csharp
float _accum;
_accum += dt;
if (_accum >= 1f / Mathf.Max(publishHz, 0.01f))
{
    _accum = 0f;
    Publish();  // Send CSV over network
}
```

**Publication Interval:**
```
Publish Interval = 1 / publishHz = 1 / 10 = 0.1 seconds
Update Rate: 10 times per second
```

---

## Network Transmission

### CSV Format
**File:** `TelemetryPublisher.cs` (Lines 45-50)

Data is sent as comma-separated values:

```
robotName,x,y,yaw,linSpeed,angSpeed,battery,mode
robot1,1.5,2.3,0.785,1.2,0.05,85.0,ros
```

### Data Fields:
| Index | Field | Type | Unit | Range | Example |
|-------|-------|------|------|-------|---------|
| 0 | robotId | string | N/A | Any | "robot1" |
| 1 | x | float | meters | -∞ to +∞ | 1.5 |
| 2 | y | float | meters | -∞ to +∞ | 2.3 |
| 3 | yaw | float | radians | 0 to 2π | 0.785 |
| 4 | linSpeed | float | m/s | 0 to max | 1.2 |
| 5 | angSpeed | float | rad/s | -∞ to +∞ | 0.05 |
| 6 | battery | float | % | 0 to 100 | 85.0 |
| 7 | mode | string | N/A | "ros" or "keyboard" | "ros" |

### Network Configuration:
```
Protocol: UDP (User Datagram Protocol)
Port: 5005 (configurable)
Frequency: 10 Hz (100 ms between packets)
Packet Size: ~50 bytes (extremely lightweight)
```

---

## Data Reception & Parsing

### Receiving Data
**File:** `TelemetryReceiver.cs` (Lines 43-90)

```csharp
void StartReceiving()
{
    udpClient = new UdpClient(listenPort);  // Listen on port 5005
    isRunning = true;
    
    receiveThread = new Thread(ReceiveData);
    receiveThread.IsBackground = true;
    receiveThread.Start();  // Non-blocking thread for network I/O
}

void ReceiveData()
{
    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
    
    while (isRunning)
    {
        byte[] data = udpClient.Receive(ref remoteEP);
        string message = Encoding.UTF8.GetString(data);
        
        lock (queueLock)
        {
            receivedDataQueue.Enqueue(message);  // Thread-safe queue
        }
    }
}
```

### Processing Data
**File:** `TelemetryReceiver.cs` (Lines 107-160)

```csharp
void ProcessTelemetry(string data)
{
    // Format: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
    string[] parts = data.Split(',');  // CSV parsing
    
    // Extract and parse each field
    string robotId = parts[0];
    float posX = float.Parse(parts[1]);
    float posY = float.Parse(parts[2]);
    float posZ = float.Parse(parts[3]);
    float rotY = float.Parse(parts[4]);
    float speed = float.Parse(parts[5]);
    float battery = float.Parse(parts[6]);
    string status = parts[7];
    float taskProgress = float.Parse(parts[8]);
    
    // Find the RobotTelemetry object
    RobotTelemetry telemetry = RobotTelemetry.Registry[robotId];
    
    // Update telemetry data
    telemetry.position = new Vector3(posX, posY, posZ);
    telemetry.speed = speed;
    telemetry.batteryPercent = battery;
    telemetry.currentState = status;
    telemetry.taskProgressPercent = taskProgress;
}
```

### Data Validation:
```csharp
if (parts.Length < 9)
{
    Debug.LogWarning($"Invalid data format: {data}");
    return;  // Skip malformed packets
}
```

---

## Display on UI Panels

### Battery Panel Display
**File:** `BatteryPanelUI.cs` (Lines 90-130)

```csharp
void Update()
{
    if (Time.time >= nextTime)
    {
        nextTime = Time.time + 1f / Mathf.Max(0.1f, updateHz);
        Refresh();  // Update UI every 0.5 seconds (updateHz = 2)
    }
}

void Refresh()
{
    // Get robot telemetry from registry
    RobotTelemetry telemetry = RobotTelemetry.Registry[robotId];
    if (telemetry == null) return;
    
    // Update battery percentage text
    chargePercentText.text = $"{telemetry.batteryPercent:F0}%";
    
    // Update battery icon based on percentage
    UpdateBatteryIcon(telemetry.batteryPercent);
    
    // Update fill animation
    targetFill = telemetry.batteryPercent / 100f;
    displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * fillSmoothSpeed);
    tubeFillImage.fillAmount = displayedFill;
}

void UpdateBatteryIcon(float percent)
{
    if (percent > 75f) ShowIcon(BatteryFull);
    else if (percent > 50f) ShowIcon(BatteryQuadFill);
    else if (percent > 25f) ShowIcon(BatteryHalfFill);
    else if (percent > 10f) ShowIcon(BatteryQuarterFill);
    else if (percent > 0f) ShowIcon(BatteryLess);
    else ShowIcon(BatteryEmpty);
}
```

### Position Panel Display
**File:** `PositionPanelUI.cs` (Lines 50-100)

```csharp
void Refresh()
{
    RobotTelemetry t = RobotTelemetry.Registry[robotId];
    if (t == null) return;
    
    // Display position (X, Z)
    if (positionText != null)
        positionText.text = $"Pos: ({t.position.x:F1}, {t.position.z:F1})";
    
    // Display velocity vector
    if (velocityText != null)
        velocityText.text = $"Vel: ({t.velocity.x:F2}, {t.velocity.z:F2}) m/s";
    
    // Display speed magnitude
    if (speedText != null)
        speedText.text = $"Speed: {t.speed:F2} m/s";
    
    // Display heading (yaw angle)
    float headingDegrees = Mathf.Atan2(t.heading.x, t.heading.z) * Mathf.Rad2Deg;
    if (headingText != null)
        headingText.text = $"Heading: {headingDegrees:F1}°";
}
```

---

## Complete Data Flow Diagram

```
┌───────────────────────────────────────────────────────────────────┐
│                         ROBOT SIMULATION                          │
├───────────────────────────────────────────────────────────────────┤
│                                                                    │
│  Robot Transform (Position, Rotation)                             │
│         ↓                                                          │
│  ┌──────────────────────────────────────────┐                    │
│  │ TelemetryPublisher.cs                     │                    │
│  │ ─────────────────────────────────────────  │                    │
│  │ 1. Calculate LinSpeed from position delta │                    │
│  │    LinSpeed = |Δposition| / Δt            │                    │
│  │                                           │                    │
│  │ 2. Calculate AngSpeed from yaw delta      │                    │
│  │    AngSpeed = Δyaw × π/180 / Δt          │                    │
│  │                                           │                    │
│  │ 3. Calculate Battery drain                │                    │
│  │    Power = Base + (LinDrain × Speed)      │                    │
│  │    Battery -= Power × Δt                  │                    │
│  │                                           │                    │
│  │ 4. Format CSV string                      │                    │
│  │    "robot1,x,y,yaw,speed,ang,batt,mode" │                    │
│  │                                           │                    │
│  │ 5. Publish at 10 Hz                       │                    │
│  │    UDP Port 5005                          │                    │
│  └──────────────────────────────────────────┘                    │
│         ↓ CSV over UDP                                            │
│         │ (100ms intervals)                                       │
│         │                                                         │
└─────────┼──────────────────────────────────────────────────────────┘
          │
          │
┌─────────┼──────────────────────────────────────────────────────────┐
│         ↓                                                          │
│    ┌────────────────────────────────────────┐                    │
│    │ TelemetryReceiver.cs (Dashboard Side)   │                    │
│    │ ────────────────────────────────────────  │                    │
│    │ 1. Listen on UDP Port 5005               │                    │
│    │    (Background thread, non-blocking)     │                    │
│    │                                          │                    │
│    │ 2. Receive byte array from network       │                    │
│    │    byte[] data = udpClient.Receive()     │                    │
│    │                                          │                    │
│    │ 3. Decode UTF-8 string                   │                    │
│    │    string msg = Encoding.UTF8.GetString() │                    │
│    │                                          │                    │
│    │ 4. Parse CSV (split by comma)            │                    │
│    │    parts = msg.Split(',')                │                    │
│    │                                          │                    │
│    │ 5. Validate format (≥9 fields)           │                    │
│    │    if (parts.Length < 9) return;         │                    │
│    │                                          │                    │
│    │ 6. Convert strings to numbers            │                    │
│    │    posX = float.Parse(parts[1])          │                    │
│    │    battery = float.Parse(parts[6])       │                    │
│    └────────────────────────────────────────┘                    │
│         ↓ Parsed data                                             │
│         │                                                         │
│    ┌────────────────────────────────────────┐                    │
│    │ RobotTelemetry.cs (Data Registry)       │                    │
│    │ ────────────────────────────────────────  │                    │
│    │ 1. Store in Registry                     │                    │
│    │    RobotTelemetry.Registry["robot1"]     │                    │
│    │                                          │                    │
│    │ 2. Update public properties              │                    │
│    │    telemetry.position = new Vector3()    │                    │
│    │    telemetry.batteryPercent = 85.5       │                    │
│    │    telemetry.speed = 1.2                 │                    │
│    │    telemetry.currentState = "Moving"     │                    │
│    │                                          │                    │
│    │ 3. Update visual representation          │                    │
│    │    visual.UpdateFromTelemetry()           │                    │
│    └────────────────────────────────────────┘                    │
│         ↓ Data ready for UI                                       │
│         │                                                         │
│    ┌────────────────────────────────────────┐                    │
│    │ UI Panels (Read from Registry)           │                    │
│    │ ────────────────────────────────────────  │                    │
│    │                                          │                    │
│    │ BatteryPanelUI:                          │                    │
│    │   → Reads batteryPercent                 │                    │
│    │   → Displays "85%"                       │                    │
│    │   → Updates fill bar                     │                    │
│    │   → Shows battery icon                   │                    │
│    │                                          │                    │
│    │ PositionPanelUI:                         │                    │
│    │   → Reads position, velocity, speed      │                    │
│    │   → Displays coordinates                 │                    │
│    │   → Shows velocity vector                │                    │
│    │   → Displays heading angle               │                    │
│    │                                          │                    │
│    │ TaskProgressPanelUI:                     │                    │
│    │   → Reads taskProgressPercent            │                    │
│    │   → Updates progress ring                │                    │
│    │   → Shows task status                    │                    │
│    └────────────────────────────────────────┘                    │
│         ↓ Rendered to screen                                      │
│    World-anchored MR panels in user's view                        │
│                                                                   │
│    [================== 85% ==================]  Battery            │
│    [Pos: (1.5, 2.3)] [Speed: 1.2 m/s]         Position            │
│    [████████████░░░░░░░░░░░░░] 60% Complete    Task Progress      │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

---

## Battery Calculation Details

### Complete Battery Model

The battery model uses a sophisticated simulation with multiple parameters:

**File:** `BatteryModel.cs` (Lines 25-75)

```csharp
[Header("Battery Capacity")]
public float capacityWh = 15f;           // Total capacity (Watt-hours)
private float currentEnergyWh;           // Current remaining energy

[Header("Power Consumption")]
public float idlePowerW = 2f;            // Idle draw (Watts)
public float movingBasePowerW = 5f;      // Base moving draw (Watts)
public float powerPerLinearSpeed = 3f;   // Additional per m/s (Watts)
public float powerPerAngularSpeed = 1.5f; // Additional per rad/s (Watts)

[Header("Time Estimation")]
public float smoothingWindowSeconds = 30f; // Moving average window
public float minPowerForEstimate = 0.5f;  // Minimum for safety
```

### Battery Drain Calculation (Advanced)

**File:** `BatteryModel.cs` (Lines 190-210)

```csharp
private void CalculatePower()
{
    if (IsMoving)
    {
        // Moving: base + linear + angular components
        _currentPowerW = movingBasePowerW 
            + (powerPerLinearSpeed * _linearSpeed)
            + (powerPerAngularSpeed * _angularSpeed);
    }
    else
    {
        // Idle: just base electronics
        _currentPowerW = idlePowerW;
    }
}

private void DrainBattery(float dt)
{
    // Energy = Power × Time
    // Wh = W × hours
    float energyUsedWh = _currentPowerW * (dt / 3600f);
    currentEnergyWh = Mathf.Max(0f, currentEnergyWh - energyUsedWh);
}

public float BatteryPercent => (currentEnergyWh / capacityWh) * 100f;
```

### Battery Formula (Complete):

**When Idle:**
```
Power (W) = idlePowerW = 2W
Consumption per second = 2W / 3600 sec/hour = 0.00556 Wh/sec
Energy per frame (60 FPS) = 0.00556 × 0.0167 = 0.0000926 Wh
Battery drop per frame = (0.0000926 Wh / 15 Wh) × 100% = 0.000617%
```

**When Moving (Example: LinSpeed=2 m/s, AngSpeed=0.1 rad/s):**
```
Power (W) = 5 + (3 × 2) + (1.5 × 0.1)
          = 5 + 6 + 0.15
          = 11.15 W

Consumption per second = 11.15 / 3600 = 0.003097 Wh/sec
Energy per frame (60 FPS) = 0.003097 × 0.0167 = 0.0000517 Wh
Battery drop per frame = (0.0000517 / 15) × 100% = 0.000344%
```

### Time Remaining Calculation

**File:** `BatteryModel.cs` (Lines 225-235)

```csharp
private void CalculateTimeRemaining()
{
    // Use smoothed average power (not instantaneous)
    float powerToUse = Mathf.Max(_averagePowerW, minPowerForEstimate);
    
    // Time (hours) = Energy (Wh) / Power (W)
    float hoursRemaining = currentEnergyWh / powerToUse;
    _timeRemainingSeconds = hoursRemaining * 3600f;  // Convert to seconds
}
```

**Formula:**
```
Time Remaining (seconds) = (Energy Remaining (Wh) / Average Power (W)) × 3600
```

**Example:**
```
Current Energy: 7.5 Wh (50% of 15 Wh)
Average Power (last 30 seconds): 8W
Time = (7.5 Wh / 8 W) × 3600 = 3375 seconds = 56 minutes
```

### Power Smoothing (Stability)

**File:** `BatteryModel.cs` (Lines 212-225)

```csharp
private void UpdatePowerHistory()
{
    // Add current sample with timestamp
    _powerHistory.Enqueue(new PowerSample { 
        time = Time.time, 
        powerW = _currentPowerW 
    });
    
    // Remove samples older than 30 seconds
    float cutoffTime = Time.time - smoothingWindowSeconds;
    while (_powerHistory.Count > 0 && _powerHistory.Peek().time < cutoffTime)
    {
        _powerHistory.Dequeue();
    }
    
    // Calculate average of remaining samples
    if (_powerHistory.Count > 0)
    {
        float sum = 0f;
        foreach (var sample in _powerHistory)
        {
            sum += sample.powerW;
        }
        _averagePowerW = sum / _powerHistory.Count;
    }
}
```

**Why Smoothing?**
- Raw instantaneous power jumps wildly when robot accelerates/decelerates
- 30-second moving average gives stable, predictable time estimate
- User doesn't see "14 minutes" → "5 minutes" → "25 minutes" every frame

---

## Speed Calculation Details

### Linear Speed

**File:** `BatteryModel.cs` (Lines 179-190)

```csharp
private void CalculateSpeeds(float dt)
{
    // Linear speed from position change
    Vector3 positionDelta = robotTransform.position - _lastPosition;
    _linearSpeed = positionDelta.magnitude / dt;
    _lastPosition = robotTransform.position;
}

public bool IsMoving => _linearSpeed > 0.05f || Mathf.Abs(_angularSpeed) > 0.05f;
```

**Calculation:**
```
LinearSpeed (m/s) = |Current Position - Previous Position| / Time Delta
```

**With Numerical Example:**
```
Frame 1: Position = (0.0, 0.0, 0.0), Time = 0.000s
Frame 2: Position = (0.1, 0.0, 0.0), Time = 0.016s (60 FPS)

Δposition = (0.1, 0.0, 0.0)
|Δposition| = 0.1 m
Δt = 0.016 s

LinearSpeed = 0.1 / 0.016 = 6.25 m/s
```

### Angular Speed

**File:** `BatteryModel.cs` (Lines 193-200)

```csharp
// Angular speed from yaw change
float currentYaw = robotTransform.eulerAngles.y;
float yawDelta = Mathf.DeltaAngle(_lastYaw, currentYaw);
_angularSpeed = Mathf.Abs(yawDelta * Mathf.Deg2Rad) / dt;
_lastYaw = currentYaw;
```

**Calculation:**
```
AngularSpeed (rad/s) = |Yaw Delta (degrees)| × (π/180) / Time Delta
```

**With Numerical Example:**
```
Frame 1: Yaw = 0°, Time = 0.000s
Frame 2: Yaw = 45°, Time = 0.016s

Δyaw = 45° - 0° = 45°
Δyaw (radians) = 45 × π/180 = 0.7854 rad
Δt = 0.016 s

AngularSpeed = 0.7854 / 0.016 = 49.08 rad/s
```

---

## Position & Heading Calculation

### Position Updates

**File:** `RobotTelemetry.cs` (Lines 105-130)

```csharp
// Position comes directly from transform
position = transform.position;

// Velocity from position change with smoothing
float dt = now - lastTime;
Vector3 rawVel = (position - lastPosition) / dt;

// Apply exponential smoothing
float alpha = Mathf.Clamp01(dt / Mathf.Max(velocitySmoothing, dt));
velocitySmoothed = Vector3.Lerp(velocitySmoothed, rawVel, alpha);

velocity = velocitySmoothed;
speed = velocity.magnitude;
```

**Smoothing Formula:**
```
Smoothed Velocity = Lerp(Previous Smoothed, Raw Velocity, Alpha)

Where Alpha = Time Delta / Smoothing Window
Alpha prevents sudden jumps, creates smooth velocity curve
```

### Heading Calculation

**File:** `RobotTelemetry.cs` (Lines 132-138)

```csharp
// Heading (normalized forward direction in horizontal plane)
var h = transform.forward;
h.y = 0f;  // Remove vertical component
heading = (h.sqrMagnitude > 0.0001f) ? h.normalized : Vector3.forward;
```

**Calculation:**
```
Heading = Normalize(Robot Forward Vector with Y = 0)
         = Unit vector pointing in robot's direction
```

**Conversion to Degrees:**
```csharp
float headingDegrees = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
```

**Compass Mapping:**
```
Heading = 0°   → Forward (+Z)
Heading = 90°  → Right (+X)
Heading = 180° → Backward (-Z)
Heading = 270° → Left (-X)
```

---

## Complete End-to-End Example

### Scenario: Robot Moving at 2 m/s, Spinning at 45°/sec, Battery at 80%

**Time: t = 1.000s (Frame update)**

#### Robot Telemetry Publisher (Sender)

```
Current State:
- Position: (5.0, 0.0, 10.0)
- Previous Position: (4.968, 0.0, 9.968)  [from 0.016s ago]
- Current Yaw: 45°
- Previous Yaw: 33.6°
- Delta Time: 0.016s

Step 1: Calculate Linear Speed
Δposition = (5.0-4.968, 0.0-0.0, 10.0-9.968) = (0.032, 0, 0.032)
|Δposition| = √(0.032² + 0.032²) = 0.0452 m
LinSpeed = 0.0452 / 0.016 = 2.83 m/s ≈ 2 m/s (after rounding)

Step 2: Calculate Angular Speed
Δyaw = 45° - 33.6° = 11.4°
Δyaw_rad = 11.4 × π/180 = 0.199 rad
AngSpeed = 0.199 / 0.016 = 12.44 rad/s
(User input was 45°/sec = 0.785 rad/s, but some frame variance)

Step 3: Calculate Battery Drain
Power = 5 + (3 × 2) + (1.5 × 0.785)
      = 5 + 6 + 1.18
      = 12.18 W

Energy drained = 12.18 W × (0.016 / 3600) hours
              = 12.18 × 0.00000444
              = 0.0000541 Wh

Battery% = 80.0 - (0.0000541 / 15 × 100) = 80.0 - 0.000361% ≈ 80.0%

Step 4: Format and Send CSV
"robot1,5.0,10.0,0.785,2.0,12.44,80.0,ros"
↑       ↑   ↑     ↑     ↑   ↑     ↑   ↑
robot   x   z     yaw   lin ang  batt mode
```

#### Dashboard Receiver (Dashboard Side)

```
Step 1: Receive UDP Packet
Port 5005: Received 48 bytes
Data: "robot1,5.0,10.0,0.785,2.0,12.44,80.0,ros"

Step 2: Parse CSV
parts[0] = "robot1"
parts[1] = "5.0"      → posX = 5.0
parts[2] = "10.0"     → posZ = 10.0
parts[3] = "0.785"    → rotY = 0.785 rad = 45°
parts[4] = "2.0"      → speed = 2.0 m/s
parts[5] = "12.44"    → angular = 12.44 rad/s
parts[6] = "80.0"     → battery = 80.0%
parts[7] = "ros"      → mode = ros

Step 3: Update RobotTelemetry Registry
RobotTelemetry.Registry["robot1"].position = (5.0, 0, 10.0)
RobotTelemetry.Registry["robot1"].speed = 2.0
RobotTelemetry.Registry["robot1"].batteryPercent = 80.0
RobotTelemetry.Registry["robot1"].currentState = "Moving"

Step 4: Visual Update
VirtualRobotVisual moves to (5.0, 0, 10.0) with yaw = 45°
```

#### Dashboard UI Display

```
Next frame when UI refreshes (0.5 second interval):

BatteryPanelUI reads: batteryPercent = 80.0%
  Display: "80%"
  Bar fill: 0.80 (80% full)
  Icon: BatteryQuadFill (50-75% range)

PositionPanelUI reads: position, speed, heading
  Display: "Pos: (5.0, 10.0)"
  Display: "Speed: 2.0 m/s"
  Display: "Heading: 45.0°"

User sees on their MR dashboard:
┌─────────────────────────────────┐
│ Battery: 80% [████████░░░░░░░░] │
├─────────────────────────────────┤
│ Pos: (5.0, 10.0)                │
│ Speed: 2.0 m/s                  │
│ Heading: 45.0°                  │
└─────────────────────────────────┘
```

---

## Summary

The telemetry system is a **complete, real-time pipeline**:

1. **Generation**: Robot calculates speeds from position/rotation changes
2. **Battery**: Drain depends on power consumption (idle + movement)
3. **Network**: CSV data sent via UDP at 10 Hz
4. **Reception**: Dashboard receives and parses CSV
5. **Storage**: Data stored in RobotTelemetry registry
6. **Display**: UI panels read from registry and update 2-4 times per second

**Key Formulas:**
- Speed = |ΔPosition| / Δt
- Battery Drain = Power × Time
- Power = Base + (LinearScale × Speed) + (AngularScale × AngularSpeed)
- TimeRemaining = Energy / AveragePower
