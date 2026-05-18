# Dashboard Architecture Analysis - Final Summary

## Direct Answers to Your Three Questions

### ❓ Question 1: Are panels robot-anchored (follow robot) or world-anchored (fixed in space)?

**✅ Answer: WORLD-ANCHORED (Fixed to User's Camera)**

The panels are **anchored to the user's camera**, not to the robot. They maintain a **fixed position relative to the user's viewpoint** (0.35m below, 0.6m in front of the camera). As the user moves around and looks in different directions, the dashboard follows them. The robot can be anywhere in the world - the dashboard doesn't move with it.

**Proof:**
- `DockFollow.cs` line 26: `transform.position = targetCamera.TransformPoint(localPosition);`
- This line executes every frame, keeping the panel in a fixed position relative to the camera
- The robot's position is only **displayed as data** on the panel, not tracked spatially

**User Experience:** You look down and see your dashboard; it follows your view, not the robot.

---

### ❓ Question 2: Is interaction gaze + hand, gaze only, or controller-based?

**✅ Answer: HAND-BASED INTERACTION (Primary)**

The dashboard uses **hand grab and drag interaction via XR Interaction Toolkit**. You physically grab the panel with your hand, drag it to reposition it, and release it. Gaze is not a primary interaction method; controller buttons are not used for dashboard interaction.

**Proof:**
- `MRTK3GrabBridge.cs` lines 45-47: Hand grab/release events via `XRBaseInteractable`
- `DraggablePanel.cs` lines 5-6: Implements `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`
- These interfaces listen for hand-based pointer events (grab, drag, release)

**Supported Interaction Methods:**
1. ✅ **Hand Grab** - Primary interaction
2. ✅ **Hand Drag** - Move panels around
3. ✅ **Hand Release** - Drop panels in new position
4. ⚠️ **Gaze** - Passive (not primary)
5. ❌ **Controller Buttons** - Not implemented

**User Experience:** You reach out, pinch/grab the panel, drag it, then release. Feels like manipulating physical objects in your space.

---

### ❓ Question 3: Is the data coming via ROS / simulation stream / JSON / mock data?

**✅ Answer: UDP NETWORK STREAM (Comma-Separated CSV Format)**

The dashboard receives **real-time robot telemetry data from an external system via UDP network** in a simple comma-separated value (CSV) format. The data arrives on port 5005 (configurable) and is parsed into structured telemetry objects that update the UI.

**Proof:**
- `TelemetryReceiver.cs` line 33: `private UdpClient udpClient;`
- `TelemetryReceiver.cs` lines 50-57: Listens on UDP port 5005
- `TelemetryReceiver.cs` lines 110-145: Parses CSV data with 9 comma-separated fields
- Format: `robotId,posX,posY,posZ,rotY,speed,battery,status,taskProgress`

**Data Source Options:**
1. ✅ **External Network** - From Cem's robot simulator or real robots (primary)
2. ✅ **Internal Simulation** - Built-in mock data if external unavailable (fallback)

**Example Data Stream:**
```
Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0
Robot_2,1.0,0.0,2.5,90.0,0.50,75.0,Idle,40.0
Robot_1,0.6,0.0,1.3,45.5,0.76,85.4,Moving,25.5
```

**User Experience:** You see real-time robot position, battery, speed, and status updating on the dashboard as it receives data from external systems.

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────────────────────┐
│                    DASHBOARD ARCHITECTURE                     │
└──────────────────────────────────────────────────────────────┘

DATA LAYER (Question 3)
───────────────────────
External System (Robot/Simulator)
         ↓ UDP Port 5005
    TelemetryReceiver (CSV Parser)
         ↓
    RobotTelemetry (Registry Storage)


UI LAYER (Question 1)
─────────────────────
DockFollow.cs
    transform.position = camera.TransformPoint(offset)
    ↓
World-Anchored Panels (0.35m below, 0.6m in front of camera)
    ↓
    • Battery Panel
    • Position Panel
    • Status Panel
    ↓
User Views Panels (they follow camera)


INTERACTION LAYER (Question 2)
──────────────────────────────
User's Hand
    ↓
XR Hand Tracking
    ↓
XRBaseInteractable (Hand Grab Detection)
    ↓
MRTK3GrabBridge (Hand Events)
    ↓
DraggablePanel (Drag Handler)
    ↓
Panel Repositioned
```

---

## Key Technical Details

### Panel Anchoring (Question 1)
| Property | Value | Effect |
|----------|-------|--------|
| **Anchor Type** | Camera-Relative | Panels follow user view |
| **Offset X** | 0 units | Centered horizontally |
| **Offset Y** | -0.35 units | 35cm below eye level |
| **Offset Z** | 0.6 units | 60cm in front of face |
| **Rotation** | Faces toward camera | Always readable |
| **Update Frequency** | Every frame (LateUpdate) | Smooth following |

**Code Location:** `Assets/Dashboard_for_MR/Scripts/DockFollow.cs`

---

### Interaction Method (Question 2)
| Feature | Implementation | Status |
|---------|---|---|
| **Framework** | XR Interaction Toolkit | ✅ Active |
| **Input Device** | Hand Tracking | ✅ Primary |
| **Grab Event** | XRBaseInteractable.selectEntered | ✅ Implemented |
| **Release Event** | XRBaseInteractable.selectExited | ✅ Implemented |
| **Drag Support** | IPointerDownHandler/IDragHandler | ✅ Implemented |
| **Haptic Feedback** | XRNode.RightHand | ✅ Optional |
| **Gaze Support** | EventSystem (passive) | ⚠️ Not primary |
| **Controller Buttons** | Not bound | ❌ Not used |

**Code Location:** 
- `Assets/Dashboard_for_MR/Scripts/D_2/MRTK3GrabBridge.cs` (hand events)
- `Assets/Dashboard_for_MR/Scripts/DraggablePanel.cs` (drag logic)

---

### Data Source (Question 3)
| Aspect | Configuration | Example |
|--------|---|---|
| **Protocol** | UDP (connectionless) | Network streaming |
| **Port** | 5005 (configurable) | Can be changed |
| **Format** | CSV (9 fields) | Simple text parsing |
| **Fields** | Position, rotation, speed, battery, status, progress | See table below |
| **Update Rate** | Depends on sender | Typically 10Hz |
| **Threading** | Background thread | Non-blocking I/O |
| **Fallback** | Internal simulation | If external unavailable |

**Data Fields:**
```
Index 0: robotId (string)      - "Robot_1" or "Robot_2"
Index 1: posX (float)          - X position in meters
Index 2: posY (float)          - Y position in meters
Index 3: posZ (float)          - Z position in meters
Index 4: rotY (float)          - Yaw rotation in degrees
Index 5: speed (float)         - Speed in m/s
Index 6: battery (float)       - Battery percentage (0-100)
Index 7: status (string)       - State text ("Moving", "Idle", etc.)
Index 8: taskProgress (float)  - Task completion percentage (0-100)
```

**Code Location:** `Assets/Dashboard_for_MR/Scripts/D_2/TelemetryReceiver.cs`

---

## How They Work Together

### Complete User Flow:

```
1. USER LAUNCHES APPLICATION
   └─ TelemetryReceiver starts listening on UDP port 5005

2. EXTERNAL SYSTEM SENDS DATA
   └─ Simulator/Robot sends CSV data: "Robot_1,0.5,0.0,1.2,45.0,0.75,85.5,Moving,25.0"

3. DASHBOARD RECEIVES DATA
   └─ TelemetryReceiver parses CSV
   └─ Updates RobotTelemetry registry
   └─ UI panels read from registry

4. USER SEES UPDATED DISPLAY
   └─ Battery panel shows: 85.5%
   └─ Position panel shows: (0.5, 0.0, 1.2)
   └─ Status shows: Moving
   └─ Data updates in real-time

5. USER INTERACTS WITH PANEL
   └─ Reaches toward panel with hand
   └─ XR system detects hand approaching
   └─ Panel becomes "grabable" (visual highlight, haptic feedback)
   └─ User pinches/closes hand
   └─ XRBaseInteractable fires selectEntered event
   └─ DraggablePanel enables drag mode

6. USER DRAGS PANEL
   └─ Hand position tracked in real-time
   └─ DraggablePanel.OnDrag() called every frame
   └─ Panel follows hand position in world space
   └─ Panel rotates to always face user

7. USER RELEASES PANEL
   └─ User opens hand
   └─ XRBaseInteractable fires selectExited event
   └─ DraggablePanel disables drag mode
   └─ Panel stays in final position
   └─ DockFollow still keeps it camera-relative (0.35m down, 0.6m forward)

8. DATA CONTINUES UPDATING
   └─ Even though panel moved, TelemetryReceiver keeps getting data
   └─ Panel displays latest robot telemetry
   └─ Panel moved by user, but data updates happen on it
```

---

## Design Philosophy

### Why World-Anchored?
- **Pro:** Dashboard always visible in user's viewport
- **Pro:** User can look away from robot but still see data
- **Pro:** Multiple robots can be displayed on same panel
- **Con:** Not attached to specific robot spatially

### Why Hand-Based?
- **Pro:** Intuitive in VR/MR (familiar from real world)
- **Pro:** Precise 1:1 tracking of hand movement
- **Pro:** Haptic feedback feels natural
- **Con:** Requires hand-tracking hardware

### Why UDP/CSV?
- **Pro:** Lightweight (low bandwidth)
- **Pro:** Fast (no parsing overhead)
- **Pro:** Simple (easy to debug)
- **Pro:** Works with any external system (simulator, real robot, etc.)
- **Con:** No error checking (relies on sender correctness)
- **Con:** No acknowledgment (fire-and-forget)

---

## Comparison to Alternatives

| Design Choice | Alternative | Why Not Used |
|---|---|---|
| World-anchored | Robot-anchored | Would be disorienting if robot moves far |
| Hand grab/drag | Gaze pointer | Less precise, less intuitive in MR |
| Gaze + click | Controller buttons | Hand tracking is more natural |
| UDP/CSV | ROS | No ROS infrastructure needed |
| UDP/CSV | JSON | CSV is faster, simpler |
| Network stream | Hardcoded data | Need to support multiple sources |

---

## Files You Need to Know

| File | Lines | Purpose |
|------|-------|---------|
| `DockFollow.cs` | 26 | Camera-relative positioning (Q1) |
| `DraggablePanel.cs` | 5-45 | Hand drag interface (Q2) |
| `MRTK3GrabBridge.cs` | 45-47 | Hand grab/release events (Q2) |
| `TelemetryReceiver.cs` | 50-145 | UDP network, CSV parsing (Q3) |
| `RobotTelemetry.cs` | Registry | Data storage and access |

---

## Conclusion

The dashboard is a **modern MR interface** that:

1. **Stays with you** (world-anchored to camera) - Always in view
2. **Responds to your hands** (XR hand tracking) - Natural interaction
3. **Shows real robot data** (UDP network stream) - Live telemetry

**This design pattern is ideal for:**
- ✅ Real-time monitoring
- ✅ Multiple robot systems
- ✅ Hands-free primary interaction
- ✅ Flexible workspace layout
- ✅ Integration with external systems

It's the modern equivalent of a clipboard you carry with you while commanding robots on the floor.
