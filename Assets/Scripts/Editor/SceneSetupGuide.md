# 🎮 Multi-Scene Setup Guide

This guide explains how to set up the 3-scene architecture for your robot exploration app.

---

## 📁 Scene Structure

| Scene | Purpose |
|-------|---------|
| `00_BootstrapMenu` | Main menu - user selects mode & control |
| `10_WarehouseSim` | Virtual warehouse (your current working scene) |
| `20_RealRoomMR` | Real room with passthrough + MRUK |

---

## 🎬 Scene 1: 00_BootstrapMenu (Main Menu)

### Step 1: Setup the Scene
1. Open `Assets/Assets/Scenes/00_BootstrapMenu.unity`
2. Delete everything except the default camera

### Step 2: Add MRTK XR Rig
1. Add MRTK XR Rig prefab to scene
2. Or create: `GameObject → XR → XR Origin`

### Step 3: Create Menu Controller
1. Create empty: `GameObject → Create Empty`
2. Name it: `MenuController`
3. Add component: `BootstrapMenuController`

### Step 4: Create UI Canvas
1. Create: `GameObject → UI → Canvas`
2. Set Canvas to "World Space" mode
3. Position it in front of camera (Z = 2)

### Step 5: Create Buttons
Create these buttons using MRTK3 PressableButton or standard UI:

**Mode Selection:**
- "Warehouse" button → OnClick → `MenuController.SelectWarehouseMode()`
- "Real Room" button → OnClick → `MenuController.SelectRealRoomMode()`

**Control Selection:**
- "ML" button → OnClick → `MenuController.SelectMLControl()`
- "ROS" button → OnClick → `MenuController.SelectROSControl()`
- "Keyboard" button → OnClick → `MenuController.SelectKeyboardControl()`

**Start:**
- "START" button → OnClick → `MenuController.StartGame()`

### Step 6: Link References
In BootstrapMenuController inspector:
- Drag buttons to their slots (optional but enables visual feedback)
- Create TextMeshPro texts for mode/control display

---

## 🏭 Scene 2: 10_WarehouseSim (Warehouse)

### Step 1: Copy Your Current Scene
1. Open your current working warehouse scene (`MR_RobotExplorer.unity` or similar)
2. Save As: `Assets/Assets/Scenes/10_WarehouseSim.unity`

### Step 2: Add Scene Manager
1. Create empty: `WarehouseSceneManager`
2. Add component: `WarehouseSceneManager`
3. Configure:
   - `Auto Start Robots`: true
   - `Apply Control Mode On Start`: true

### Step 3: Add BatteryModel to Robots
For each robot in the scene:
1. Select the robot root (e.g., `Robot_1`)
2. Add Component: `BatteryModel`
3. Configure battery settings if needed

### Step 4: (Optional) Add Pause Menu
1. Create a UI panel for pause menu
2. Add "Resume" button → `WarehouseSceneManager.TogglePause()`
3. Add "Return to Menu" button → `WarehouseSceneManager.ReturnToMenu()`
4. Link to `WarehouseSceneManager.pauseMenu`

---

## 🏠 Scene 3: 20_RealRoomMR (Real Room)

### Step 1: Setup Base Scene
1. Open `Assets/Assets/Scenes/20_RealRoomMR.unity`
2. Delete everything except default camera

### Step 2: Add XR Components
1. Add OVRCameraRig (for passthrough)
   - Or MRTK XR Rig
2. Add OVRPassthroughLayer
   - Placement: Underlay
   - Opacity: 1.0

### Step 3: Add Room Detection
1. Create empty: `RoomSetup`
2. Add component: `Quest3RoomSetup`
3. Configure:
   - `Enable Passthrough`: true
   - `Auto Setup Room`: true
   - `Show Debug Visuals`: true (for testing)

### Step 4: Add Scene Manager
1. Create empty: `RealRoomMRManager`
2. Add component: `RealRoomMRManager`
3. Configure:
   - `Wait For Room`: true
   - `Auto Start Robots`: false
   - Link `Room Setup` reference

### Step 5: Add Robot Prefabs
Either:
- Assign robot prefabs to `RealRoomMRManager.robotPrefabs`
- Or assign to `Quest3RoomSetup.robotPrefabs`

### Step 6: Add Start UI
1. Create a simple Canvas with:
   - Loading text (link to `statusText`)
   - Start button (link to `startButton`)
2. Start button → OnClick → `RealRoomMRManager.StartRobots()`

---

## ⚙️ Build Settings

### Configure Scene Order
1. Open: `File → Build Settings`
2. Add scenes in order:
   1. `Assets/Assets/Scenes/00_BootstrapMenu.unity` (Index 0)
   2. `Assets/Assets/Scenes/10_WarehouseSim.unity` (Index 1)
   3. `Assets/Assets/Scenes/20_RealRoomMR.unity` (Index 2)

---

## 🔋 Battery Model Setup

Add `BatteryModel` component to each robot for realistic battery:

### Default Settings:
| Setting | Value | Description |
|---------|-------|-------------|
| `Capacity Wh` | 15 | Total battery capacity |
| `Idle Power W` | 2 | Power when stationary |
| `Moving Base Power W` | 5 | Base power when moving |
| `Power Per Linear Speed` | 3 | Extra W per m/s |
| `Power Per Angular Speed` | 1.5 | Extra W per rad/s |
| `Smoothing Window Seconds` | 30 | Time averaging window |

### Reading Battery in HUD:
```csharp
// In your dashboard/HUD script:
BatteryModel battery = robot.GetComponent<BatteryModel>();
float percent = battery.BatteryPercent;
string timeRemaining = battery.TimeRemainingFormatted;
```

---

## 🎯 Quick Test

### Test Menu Scene:
1. Open `00_BootstrapMenu`
2. Enter Play Mode
3. Click mode/control buttons (check console for logs)
4. Click START (should load warehouse scene)

### Test Warehouse Scene:
1. Open `10_WarehouseSim`
2. Enter Play Mode
3. Robots should start with selected control mode
4. Press Escape for pause menu

### Test Real Room Scene:
1. Build to Quest 3
2. Select "Real Room" in menu
3. Wait for room detection
4. Press START to begin

---

## 🚨 Common Issues

### "Scene not found" error
- Check Build Settings - all scenes must be added
- Scene names must match exactly: `10_WarehouseSim`, `20_RealRoomMR`

### Robots don't move
- Check `GameSettings.GameStarted` is true
- Check `AGVController.mode` matches selection
- For ML: ensure model is assigned to BehaviorParameters

### Battery shows wrong time
- Add `BatteryModel` component
- Ensure `smoothingWindowSeconds` > 10

### Passthrough not working
- Ensure OVRCameraRig is in scene
- Enable passthrough in Project Settings → XR Plug-in Management → OpenXR
- Check Quest3RoomSetup.enablePassthrough = true

---

## 📝 Files Created

| File | Purpose |
|------|---------|
| `GameSettings.cs` | Stores mode/control selection across scenes |
| `BootstrapMenuController.cs` | Menu scene logic |
| `WarehouseSceneManager.cs` | Warehouse scene manager |
| `RealRoomMRManager.cs` | Real room MR scene manager |
| `BatteryModel.cs` | Realistic battery with time remaining |

---

## 🎮 Control Modes Summary

| Mode | Description | Use Case |
|------|-------------|----------|
| **ML** | Neural network controls robots | Automated exploration |
| **ROS** | ROS2 navigation stack controls | SLAM integration |
| **Keyboard** | WASD keys control robot | Testing/demos |

