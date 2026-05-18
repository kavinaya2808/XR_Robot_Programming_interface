# 🎯 Quest 3 Build & Deployment Guide
## Mixed Reality Robot Fleet Monitoring System

---

## 📋 PRE-BUILD CHECKLIST

### On Your Quest 3 Headset (DO THIS FIRST!)

1. **Set Up Room Boundaries:**
   - Put on Quest 3
   - Go to **Settings → Physical Space → Space Setup**
   - Select **Set up Room**
   - Walk around your room tracing the walls
   - Confirm floor and ceiling
   - Optionally mark furniture (tables, chairs)

2. **Enable Developer Mode:**
   - Open Meta Quest app on phone
   - Go to **Devices → [Your Quest 3] → Developer Mode**
   - Toggle **ON**

3. **Connect Quest to PC:**
   - Use USB-C cable (data cable, not just charging)
   - When prompted on Quest, click **Allow** for USB debugging

---

## 🔧 UNITY PROJECT SETUP

### Step 1: Switch to Android Platform

1. Open Unity
2. **File → Build Settings**
3. Select **Android** in platform list
4. Click **Switch Platform** (this takes a few minutes)
5. When prompted about **Input Handling**, click **Yes** (ignore warning)

### Step 2: Configure Player Settings

1. **Edit → Project Settings → Player**
2. Click **Android tab** (robot icon)

**Company & Product:**
- Company Name: `YourName`
- Product Name: `MRRobotExplorer`

**Other Settings:**
- **Color Space:** Linear (for better visuals)
- **Auto Graphics API:** Uncheck
- **Graphics APIs:** Keep only **OpenGLES3** (remove Vulkan if present)
- **Minimum API Level:** Android 12.0 (API level 32)
- **Target API Level:** Automatic (highest installed)
- **Scripting Backend:** IL2CPP
- **Target Architectures:** Check only **ARM64** (uncheck ARMv7)

### Step 3: Configure XR Settings

1. **Edit → Project Settings → XR Plug-in Management**
2. Click **Android tab**
3. Check **OpenXR**
4. Under OpenXR settings:
   - Check **Meta Quest Support** feature
   - Set **Render Mode:** Single Pass Instanced

### Step 4: Configure MRTK3 Profiles

1. **Edit → Project Settings → MRTK3**
2. For each tab (Standalone, Editor, Android):
   - Click **"Assign MRTK Default"** button
3. This ensures hand tracking and interactions work

---

## 🎮 SCENE SETUP FOR QUEST 3

### Add Quest3RoomSetup to Your Scene

1. Open your scene: `Assets/Assets/Scenes/MR_RobotExplorer.unity`

2. Create empty GameObject:
   - **Right-click in Hierarchy → Create Empty**
   - Name it: `Quest3RoomSetup`

3. Add the script:
   - Select the GameObject
   - **Add Component → Quest3RoomSetup**

4. Configure settings:
   - **Enable Passthrough:** ✓ (checked)
   - **Passthrough Opacity:** 1.0
   - **Auto Setup Room:** ✓ (checked)
   - **Show Debug Visuals:** ✓ (for testing, uncheck later)
   - **Move Existing Robots:** ✓ (checked)

### Add Quest3LiDARAdapter to Robots

1. Find each robot's LiDAR sensor:
   - `robot1 → base_footprint → base_link → base_scan`
   - `robot2 → base_footprint → base_link → base_scan`

2. Select the `base_scan` GameObject

3. **Add Component → Quest3LiDARAdapter**

4. Configure:
   - **Wait For Room Setup:** ✓ (checked)
   - **Show Debug Rays:** ✓ (for testing)

### Verify Robot Settings

For each robot (robot1, robot2):

1. Select the robot in Hierarchy
2. Find **TurtlebotCoverageAgent** component
3. Verify:
   - **Boundary Limit:** Set to match your room size (e.g., 5-10 for typical room)
   - **Max Obstacle Collisions:** 50 (lenient for real-world)
4. Find **AGVController** component
5. Verify:
   - **Mode:** ML (for autonomous exploration)

---

## 🏗️ BUILD THE APK

### Step 1: Build Settings

1. **File → Build Settings**
2. Make sure your scene is in the list:
   - If not, click **Add Open Scenes**
3. Ensure **Android** is selected

### Step 2: Build

**Option A: Build and Run (Recommended)**
1. Connect Quest 3 via USB
2. Click **Build and Run**
3. Choose save location for APK
4. Wait for build (5-15 minutes first time)
5. App launches automatically on Quest

**Option B: Build Only**
1. Click **Build**
2. Choose save location
3. Name file: `MRRobotExplorer.apk`
4. Manually install using SideQuest or adb

### Step 3: Install via ADB (if needed)

```bash
# Check Quest is connected
adb devices

# Install the APK
adb install -r "path/to/MRRobotExplorer.apk"
```

---

## 🚀 RUNNING ON QUEST 3

### First Launch

1. Put on Quest 3
2. Go to **App Library → Unknown Sources**
3. Find and launch **MRRobotExplorer**

### What You Should See

1. **Passthrough activates** - you see your real room
2. **Room geometry loads** - walls get invisible colliders (debug shows colored overlays)
3. **Robots appear** - on your real floor
4. **Robots start exploring** - using ML-Agents
5. **Dashboard panels** - showing telemetry (if configured)

### Connect to ROS for SLAM (Optional)

1. Make sure Quest and PC are on **same WiFi network**
2. Find your PC's IP address (e.g., `192.168.1.100`)
3. In Unity, before building:
   - Find **ROSConnectionPrefab** in scene
   - Set **ROS IP Address** to your PC's IP
4. On PC, run Docker with ROS:
   ```bash
   ros2 launch unity_slam_example unity_slam_example.py
   ```
5. Run app on Quest - SLAM will map your real room!

---

## 🐛 TROUBLESHOOTING

### Build Errors

| Error | Solution |
|-------|----------|
| "No Android SDK" | Install Android SDK via Unity Hub → Installs → Add Modules |
| "Minimum API level" | Set to Android 12.0 (API 32) in Player Settings |
| "ARM64 not found" | Install Android Build Support module in Unity Hub |
| "MRTK profile error" | Project Settings → MRTK3 → Assign MRTK Default |

### Runtime Issues

| Issue | Solution |
|-------|----------|
| No passthrough | Enable passthrough in Quest Settings |
| Robots don't see walls | Room setup incomplete - redo Space Setup on Quest |
| App crashes on launch | Check logcat: `adb logcat -s Unity` |
| Robots fall through floor | Quest3RoomSetup not in scene or room not scanned |
| SLAM not working | Check PC IP is correct, WiFi connected |

### Debug Commands

```bash
# View Unity logs from Quest
adb logcat -s Unity

# Take screenshot from Quest
adb shell screencap /sdcard/screenshot.png
adb pull /sdcard/screenshot.png

# Record video
adb shell screenrecord /sdcard/demo.mp4
# (Press Ctrl+C to stop)
adb pull /sdcard/demo.mp4
```

---

## 📱 QUICK REFERENCE

### Key Files

| File | Purpose |
|------|---------|
| `Quest3RoomSetup.cs` | Passthrough + room detection |
| `Quest3LiDARAdapter.cs` | Makes LiDAR hit room geometry |
| `TurtlebotCoverageAgent.cs` | ML-Agents robot brain |
| `AGVController.cs` | Robot wheel control |
| `LaserScanSensor.cs` | LiDAR → ROS /scan |

### Key Settings

| Setting | Value | Where |
|---------|-------|-------|
| Platform | Android | Build Settings |
| Graphics API | OpenGLES3 | Player Settings |
| XR Plugin | OpenXR + Meta Quest | XR Plug-in Management |
| MRTK Profile | MRTK Default | Project Settings → MRTK3 |
| AGVController.mode | ML | Inspector on robots |
| Passthrough | Enabled | Quest3RoomSetup |

---

## ✅ SUCCESS CHECKLIST

- [ ] Quest 3 room setup complete (Space Setup)
- [ ] Unity switched to Android platform
- [ ] XR Plug-in: OpenXR + Meta Quest enabled
- [ ] MRTK3 profiles assigned
- [ ] Quest3RoomSetup added to scene
- [ ] Quest3LiDARAdapter added to robot LiDAR
- [ ] AGVController mode = ML
- [ ] Build successful
- [ ] App runs with passthrough
- [ ] Robots move on real floor
- [ ] (Optional) SLAM builds map of real room

