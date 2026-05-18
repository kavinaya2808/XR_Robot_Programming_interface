# Dashboard Architecture - Quick Reference

## Three Questions - Three Answers

### 1️⃣ Panel Anchoring: **WORLD-ANCHORED** ✓
```
DockFollow.cs → transform.position = targetCamera.TransformPoint(localPosition)
                                      ↑
                                      Camera-relative positioning

Result: Panels FOLLOW camera/user, NOT robot
```

**Effect:** Dashboard stays in front of user, can look away from robot

---

### 2️⃣ Interaction: **HAND-BASED** ✓
```
MRTK3GrabBridge.cs → XRBaseInteractable.selectEntered/selectExited
DraggablePanel.cs  → IPointerDownHandler, IDragHandler, IPointerUpHandler

Result: Hand grab/drag for interaction
```

**Supported:**
- ✅ Hand grab
- ✅ Hand drag  
- ✅ Hand release
- ⚠️ Gaze (passive)
- ❌ Controller buttons (not used)

---

### 3️⃣ Data Source: **UDP NETWORK (CSV)** ✓
```
External System → UDP Port 5005 → TelemetryReceiver → RobotTelemetry → UI Panels
                                   CSV Parser (9 fields)

Format: robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress
```

**Example:**
```
Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0
```

---

## Key Code Locations

| Feature | File | Line |
|---------|------|------|
| Camera-relative panel | `DockFollow.cs` | 26 |
| Hand drag interaction | `DraggablePanel.cs` | 5 |
| Hand grab event | `MRTK3GrabBridge.cs` | 45-47 |
| UDP receiver | `TelemetryReceiver.cs` | 33-50 |
| CSV parser | `TelemetryReceiver.cs` | 110-145 |
| Data registry | `RobotTelemetry.cs` | Registry |

---

## Data Flow (Single Diagram)

```
┌──────────────────┐
│ Simulator/Robot  │
│  (Cem's system)  │
└────────┬─────────┘
         │ UDP
         ↓ Port 5005
┌──────────────────────┐
│  TelemetryReceiver   │
│   (Listen & Parse)   │
└────────┬─────────────┘
         │ CSV Parse
         ↓
┌──────────────────────┐
│  RobotTelemetry      │
│  (Store in Registry) │
└────────┬─────────────┘
         │
         ↓
┌──────────────────────┐
│  Dashboard UI        │
│  (Display on panels) │
└────────┬─────────────┘
         │
    ┌────┴────┐
    ↓         ↓
Battery   Position
Panel     Panel
(World-Anchored)
```

---

## Interaction Flow (Touch/Grab)

```
User's Hand
    ↓
XR Interactor (Hand tracking)
    ↓
XRBaseInteractable.selectEntered
    ↓
MRTK3GrabBridge.OnSelectEntered
    ↓
PointerEventData.OnPointerDown
    ↓
DraggablePanel.OnDrag
    ↓
Panel Follows Hand
    ↓
User Releases Hand
    ↓
XRBaseInteractable.selectExited
    ↓
Panel Stays Where Placed
```

---

## Panel Positioning (Under the Hood)

```csharp
// Every frame:
panel.position = camera.TransformPoint(new Vector3(0, -0.35f, 0.6f))

// Translation:
// X: 0 units right/left
// Y: -0.35 units down (below eye level)  
// Z: 0.6 units forward (in front of face)

// Result: Panel always 0.35m below and 0.6m in front of camera
```

---

## Alternative Scenarios Not Used

| Scenario | Status | Why |
|----------|--------|-----|
| Robot-anchored panels | ❌ Not used | Would be disorienting if robot moves far |
| Gaze-based interaction | ⚠️ Passive | Hand is more intuitive for MR |
| ROS direct | ❌ Not used | Uses UDP wrapper instead |
| JSON data format | ❌ Not used | CSV is simpler and faster |
| Mock data (default) | ✅ Fallback | Used when external source unavailable |

---

## Network Configuration

```
Port: 5005 (configurable)
Protocol: UDP (connectionless)
Format: CSV (comma-separated)
Thread: Background thread for I/O
Update Rate: Depends on sender (typically 10Hz from simulator)

// To change port in Inspector:
TelemetryReceiver.listenPort = 5005
```

---

## Data Fields (In Order)

| Index | Field | Type | Example | Unit |
|-------|-------|------|---------|------|
| 0 | robotId | string | Robot_1 | N/A |
| 1 | posX | float | 0.5 | meters |
| 2 | posY | float | 0.0 | meters |
| 3 | posZ | float | 1.2 | meters |
| 4 | rotY | float | 45.0 | degrees |
| 5 | speed | float | 0.75 | m/s |
| 6 | battery | float | 85.5 | % |
| 7 | status | string | Moving | text |
| 8 | taskProgress | float | 25.0 | % |

---

## Testing the System

### Test 1: Panel Positioning
```
Stand still, look at dashboard panel
Expected: Panel stays 0.35m below, 0.6m in front of face
Walk around
Expected: Panel follows, maintains offset
```

### Test 2: Hand Interaction
```
Raise hand toward panel
Expected: Panel becomes grabable
Close hand (pinch)
Expected: Panel enters grab state
Move hand
Expected: Panel follows hand
Open hand
Expected: Panel stays in final position
```

### Test 3: Data Reception
```
Inspector: Enable debug logs on TelemetryReceiver
Run external simulator on port 5005
Check Console
Expected: "[TelemetryReceiver] ✓ Robot_1: pos=(0.5,1.2) speed=0.75 battery=85%"
```

---

## Common Customization Points

### Change Panel Offset
```csharp
// In DockFollow.cs:
public Vector3 localPosition = new Vector3(0f, -0.35f, 0.6f);
// Change to: new Vector3(0.5f, -0.2f, 1.0f) for offset right, higher, farther
```

### Change Network Port
```csharp
// In TelemetryReceiver.cs:
public int listenPort = 5005;
// Change to: 9000 for different port
```

### Toggle Data Source
```csharp
// In scene, find RobotTelemetry:
useSimulatedData = false;  // Use external UDP
useSimulatedData = true;   // Use internal simulation
```

### Add Controller Button Support
```csharp
// Currently not implemented
// Would need to add:
// - Input.GetKey(KeyCode.A) checks
// - Or XRController button event wiring
```

---

## Conclusion

**Simple Answer:**
1. Panels stick to your view (world-anchored to camera)
2. You grab them with your hands (XR Interaction Toolkit)
3. Data comes from network via UDP (CSV format)

**User Experience:**
- You see dashboard in your viewport
- You grab and move panels where you want
- Real-time robot data updates on display
- Panels stay put once released
