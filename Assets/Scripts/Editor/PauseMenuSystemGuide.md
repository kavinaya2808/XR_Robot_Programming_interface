# 🎮 Pause Menu System - Complete Technical Guide

This document explains how the Pause Menu system works in the `10_WarehouseSim` scene, including all components, their connections, and how to fix common issues.

---

## 📋 Table of Contents

1. [System Overview](#system-overview)
2. [Component Architecture](#component-architecture)
3. [How Each Part Works](#how-each-part-works)
4. [Connection Diagram](#connection-diagram)
5. [Step-by-Step Configuration](#step-by-step-configuration)
6. [Common Issues & Fixes](#common-issues--fixes)
7. [Testing Checklist](#testing-checklist)

---

## 🔍 System Overview

### What the Pause Menu Should Do:

| Action | Expected Result |
|--------|-----------------|
| Press **ESC** key | Game pauses (Time.timeScale = 0), Pause_panel opens |
| Click **Pause** in TaskBar | Pause_panel opens/closes (toggle) |
| Click **▶ RESUME** button | Pause_panel closes, game resumes (Time.timeScale = 1) |
| Click **🏠 RETURN TO MENU** button | Loads `00_BootstrapMenu` scene |

### Key Scenes:

- **`00_BootstrapMenu`** - Main menu where user selects mode (Warehouse/Real Room) and control type (ML/ROS/Keyboard)
- **`10_WarehouseSim`** - The warehouse simulation scene (current scene with pause menu)
- **`20_RealRoomMR`** - Real room mixed reality scene

---

## 🏗️ Component Architecture

### The 5 Key Components:

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCENE HIERARCHY                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  WarehouseManager (GameObject)                                  │
│  └── WarehouseSceneManager.cs ←── Controls pause/resume logic   │
│      • pauseMenu = Pause_panel                                  │
│      • robots = [robot1, robot2]                                │
│                                                                 │
│  Pause_panel (GameObject)                                       │
│  ├── PanelController.cs ←── Opens/closes panel, panelId="Pause" │
│  ├── PauseMenuPanelUI.cs ←── Handles button clicks              │
│  │   • resumeButton → OnResumePressed()                         │
│  │   • returnToMenuButton → OnReturnToMenuPressed()             │
│  └── Content/                                                   │
│      ├── TitleText ("PAUSE MENU")                               │
│      ├── StatusText ("Press ESC to toggle")                     │
│      ├── ResumeButton (green)                                   │
│      └── ReturnToMenuButton (red)                               │
│                                                                 │
│  TaskBar (GameObject)                                           │
│  └── TaskBar-1/                                                 │
│      ├── PositionButton                                         │
│      ├── RobotStatusButton                                      │
│      ├── BatteryButton                                          │
│      ├── TaskProgressButton                                     │
│      └── PauseButton ←── TaskbarButton.cs (panelId="Pause")     │
│                                                                 │
│  GameSettings (Singleton - persists across scenes)              │
│  └── GameSettings.cs                                            │
│      • GamePaused (bool)                                        │
│      • GameStarted (bool)                                       │
│      • TogglePause()                                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ⚙️ How Each Part Works

### 1. WarehouseSceneManager.cs

**Location:** `Assets/Scripts/WarehouseSceneManager.cs`

**Purpose:** Main controller for the warehouse scene. Handles:
- Finding robots in scene
- Applying control mode (ML/ROS/Keyboard)
- **Pause/Resume logic**
- Scene transitions

**Key Code - TogglePause():**
```csharp
public void TogglePause()
{
    if (GameSettings.Instance != null)
    {
        GameSettings.Instance.TogglePause();
        
        // THIS IS THE KEY LINE - pauses/unpauses the game
        Time.timeScale = GameSettings.Instance.GamePaused ? 0f : 1f;
        
        // Show/hide the pause panel
        if (pauseMenu != null)
            pauseMenu.SetActive(GameSettings.Instance.GamePaused);
    }
}
```

**Key Code - ReturnToMenu():**
```csharp
public void ReturnToMenu()
{
    Time.timeScale = 1f;  // IMPORTANT: Reset time before loading
    StopRobots();
    SceneManager.LoadScene("00_BootstrapMenu");
}
```

**Required Inspector Setup:**
| Field | Value |
|-------|-------|
| `Pause Menu` | Drag `Pause_panel` here |
| `Robots` Element 0 | Drag `robot1` here |
| `Robots` Element 1 | Drag `robot2` here |

---

### 2. PauseMenuPanelUI.cs

**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/PauseMenuPanelUI.cs`

**Purpose:** Handles the UI elements and button clicks inside the Pause_panel.

**Key Methods:**

```csharp
// Called when Resume button is clicked
public void OnResumePressed()
{
    Debug.Log("[PauseMenuPanelUI] Resume pressed");
    
    if (warehouseManager != null)
    {
        // If paused, this will unpause
        if (GameSettings.Instance != null && GameSettings.Instance.GamePaused)
        {
            warehouseManager.TogglePause();
        }
    }
    else
    {
        // Fallback: just restore time scale
        Time.timeScale = 1f;
    }
    
    // Close this panel
    var panelController = GetComponent<PanelController>();
    if (panelController != null)
    {
        panelController.ClosePanel();
    }
}

// Called when Return to Menu button is clicked
public void OnReturnToMenuPressed()
{
    Debug.Log("[PauseMenuPanelUI] Return to Menu pressed");
    
    Time.timeScale = 1f;  // Reset time
    
    if (warehouseManager != null)
    {
        warehouseManager.ReturnToMenu();
    }
    else
    {
        // Fallback: direct scene load
        SceneManager.LoadScene("00_BootstrapMenu");
    }
}
```

**Required Inspector Setup:**
| Field | Value |
|-------|-------|
| `Resume Button` | Drag the Resume button (with Button component) |
| `Return To Menu Button` | Drag the Return to Menu button |
| `Warehouse Manager` | Leave empty (auto-finds) or drag WarehouseManager |
| `Menu Scene Name` | `00_BootstrapMenu` |

---

### 3. PanelController.cs

**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/PanelController.cs`

**Purpose:** Controls opening/closing of dashboard panels. Works with TaskBar buttons.

**Key Properties:**
```csharp
public string panelId = "Pause";  // MUST match TaskbarButton.panelId
public GameObject panelRoot;      // The panel GameObject itself
```

**How it works:**
- When `TogglePanel()` is called, it shows/hides the panel
- The TaskBar button calls `TogglePanel()` via `TaskbarButton.OnPressed()`

**Required Inspector Setup on Pause_panel:**
| Field | Value |
|-------|-------|
| `Panel Id` | `Pause` (MUST match exactly) |
| `Panel Root` | Drag `Pause_panel` itself |

---

### 4. TaskbarButton.cs

**Location:** `Assets/Dashboard_for_MR/Scripts/D_2/TaskbarButton.cs`

**Purpose:** Connects a TaskBar button to its corresponding panel.

**Key Code:**
```csharp
public string panelId;  // Must match PanelController.panelId
public PanelController panelController;  // Auto-finds if null

public void OnPressed()
{
    if (panelController == null)
    {
        // Auto-find by panelId
        var all = FindObjectsOfType<PanelController>();
        foreach (var p in all)
        {
            if (p.panelId == panelId)
            {
                panelController = p;
                break;
            }
        }
    }
    
    panelController?.TogglePanel();
}
```

**Required Inspector Setup on PauseButton:**
| Field | Value |
|-------|-------|
| `Panel Id` | `Pause` |
| `Panel Controller` | Leave empty (auto-finds) |

**IMPORTANT:** The button must have an MRTK `PressableButton` or Unity `Button` component that calls `OnPressed()` when clicked!

---

### 5. GameSettings.cs (Singleton)

**Location:** `Assets/Scripts/GameSettings.cs`

**Purpose:** Stores game state that persists across scenes.

**Key Properties:**
```csharp
public bool GamePaused => _gamePaused;
public bool GameStarted => _gameStarted;

public void TogglePause()
{
    _gamePaused = !_gamePaused;
    OnGamePausedChanged?.Invoke(_gamePaused);
}
```

**Note:** This is a singleton that uses `DontDestroyOnLoad()` so it persists when loading new scenes.

---

## 🔗 Connection Diagram

```
USER INPUT                    SCRIPT CALLS                     RESULT
──────────────────────────────────────────────────────────────────────

Press ESC Key
    │
    ▼
WarehouseSceneManager.Update()
    │
    ▼
WarehouseSceneManager.TogglePause()
    │
    ├──► GameSettings.TogglePause()     →  _gamePaused = true
    ├──► Time.timeScale = 0f            →  Game freezes
    └──► pauseMenu.SetActive(true)      →  Panel shows

──────────────────────────────────────────────────────────────────────

Click TaskBar "Pause" Button
    │
    ▼
TaskbarButton.OnPressed()
    │
    ▼
PanelController.TogglePanel()
    │
    └──► panelRoot.SetActive(true/false)  →  Panel shows/hides

NOTE: TaskBar button does NOT pause the game by itself!
      It only opens the panel. User must click Resume.

──────────────────────────────────────────────────────────────────────

Click "RESUME" Button
    │
    ▼
PauseMenuPanelUI.OnResumePressed()
    │
    ├──► warehouseManager.TogglePause()
    │       ├──► Time.timeScale = 1f    →  Game resumes
    │       └──► pauseMenu.SetActive(false)
    └──► panelController.ClosePanel()   →  Panel closes

──────────────────────────────────────────────────────────────────────

Click "RETURN TO MENU" Button
    │
    ▼
PauseMenuPanelUI.OnReturnToMenuPressed()
    │
    ├──► Time.timeScale = 1f            →  Reset time
    └──► SceneManager.LoadScene("00_BootstrapMenu")  →  Load menu
```

---

## 🔧 Step-by-Step Configuration

### Step 1: Configure Pause_panel

1. Select `Pause_panel` in Hierarchy
2. Check these components:

**PanelController:**
- `Panel Id` = `Pause`
- `Panel Root` = `Pause_panel` (itself)

**PauseMenuPanelUI:**
- `Resume Button` = drag the Resume button from Content
- `Return To Menu Button` = drag the Return to Menu button from Content
- `Menu Scene Name` = `00_BootstrapMenu`

### Step 2: Configure Resume Button

1. Find `Pause_panel > BatteryPanel > Content > ResumeButton`
2. It must have a `Button` component
3. In Button's `OnClick()` event:
   - Click `+` to add listener
   - Drag `Pause_panel` to the object slot
   - Select `PauseMenuPanelUI > OnResumePressed`

### Step 3: Configure Return To Menu Button

1. Find `Pause_panel > BatteryPanel > Content > ReturnToMenuButton`
2. Same as above but select `OnReturnToMenuPressed`

### Step 4: Configure TaskBar PauseButton

1. Find `TaskBar > TaskBar-1 > PauseButton`
2. Must have `TaskbarButton` component:
   - `Panel Id` = `Pause`
3. The MRTK button must call `TaskbarButton.OnPressed()` on click

### Step 5: Configure WarehouseSceneManager

1. Find `WarehouseManager` in Hierarchy
2. In `WarehouseSceneManager` component:
   - `Pause Menu` = drag `Pause_panel`
   - `Robots` Element 0 = drag `robot1`
   - `Robots` Element 1 = drag `robot2`

### Step 6: Verify Build Settings

1. Go to `File > Build Settings`
2. Ensure scenes are added:
   - `00_BootstrapMenu` (index 0)
   - `10_WarehouseSim` (index 1)
   - `20_RealRoomMR` (index 2)

---

## 🐛 Common Issues & Fixes

### Issue 1: "Pause button in TaskBar doesn't open panel"

**Cause:** TaskbarButton.panelId doesn't match PanelController.panelId

**Fix:**
- On PauseButton: `panelId` = `Pause`
- On Pause_panel's PanelController: `panelId` = `Pause`
- They must match EXACTLY (case-sensitive)

---

### Issue 2: "Resume button doesn't do anything"

**Cause:** Button's OnClick event not wired up

**Fix:**
1. Select ResumeButton
2. Find `Button` component
3. In `On Click()`:
   - Click `+`
   - Drag `Pause_panel` to object slot
   - Select `PauseMenuPanelUI > OnResumePressed`

**Alternative Fix (via code):**
```csharp
// In PauseMenuPanelUI.Start()
if (resumeButton != null)
{
    resumeButton.onClick.RemoveAllListeners();
    resumeButton.onClick.AddListener(OnResumePressed);
}
```

---

### Issue 3: "Return to Menu loads wrong scene or crashes"

**Cause:** Scene not in Build Settings or wrong name

**Fix:**
1. Open `File > Build Settings`
2. Click `Add Open Scenes` or drag scene files
3. Ensure `00_BootstrapMenu` is listed
4. In PauseMenuPanelUI: `menuSceneName` = `00_BootstrapMenu`

---

### Issue 4: "Game doesn't actually pause (robots still move)"

**Cause:** `Time.timeScale` not being set

**Fix:** Check `WarehouseSceneManager.TogglePause()`:
```csharp
public void TogglePause()
{
    if (GameSettings.Instance != null)
    {
        GameSettings.Instance.TogglePause();
        Time.timeScale = GameSettings.Instance.GamePaused ? 0f : 1f;  // THIS LINE
        
        if (pauseMenu != null)
            pauseMenu.SetActive(GameSettings.Instance.GamePaused);
    }
}
```

---

### Issue 5: "Pause_panel not linked to WarehouseSceneManager"

**Cause:** Inspector reference missing

**Fix:**
1. Select `WarehouseManager` in Hierarchy
2. Find `Warehouse Scene Manager (Script)` component
3. Drag `Pause_panel` to `Pause Menu` field

---

### Issue 6: "ESC key doesn't work"

**Cause:** Input System issue

**Check:** `WarehouseSceneManager.Update()`:
```csharp
void Update()
{
    if (UnityEngine.InputSystem.Keyboard.current != null)
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }
}
```

**Note:** This uses the new Input System. If using old Input, use:
```csharp
if (Input.GetKeyDown(KeyCode.Escape))
{
    TogglePause();
}
```

---

## ✅ Testing Checklist

Run through this checklist to verify everything works:

### In Unity Editor:

- [ ] Press Play
- [ ] Press ESC → Panel opens, game pauses (check if robots stop)
- [ ] Press ESC again → Panel closes, game resumes
- [ ] Click TaskBar "Pause" button → Panel opens
- [ ] Click "RESUME" button → Panel closes
- [ ] Click "RETURN TO MENU" → Loads 00_BootstrapMenu scene

### Inspector Verification:

- [ ] `Pause_panel` has `PanelController` with `panelId = "Pause"`
- [ ] `Pause_panel` has `PauseMenuPanelUI` attached
- [ ] `ResumeButton` has `Button.OnClick` → `PauseMenuPanelUI.OnResumePressed`
- [ ] `ReturnToMenuButton` has `Button.OnClick` → `PauseMenuPanelUI.OnReturnToMenuPressed`
- [ ] `TaskBar` has `PauseButton` with `TaskbarButton.panelId = "Pause"`
- [ ] `WarehouseManager` has `pauseMenu = Pause_panel`
- [ ] Build Settings includes `00_BootstrapMenu`

### Console Check:

When clicking buttons, you should see:
```
[PauseMenuPanelUI] Resume pressed
[PauseMenuPanelUI] Return to Menu pressed
[WarehouseSceneManager] ...
```

If you don't see these logs, the button click events are not wired up!

---

## 📁 File Locations Summary

| File | Path |
|------|------|
| WarehouseSceneManager.cs | `Assets/Scripts/WarehouseSceneManager.cs` |
| PauseMenuPanelUI.cs | `Assets/Dashboard_for_MR/Scripts/D_2/PauseMenuPanelUI.cs` |
| PanelController.cs | `Assets/Dashboard_for_MR/Scripts/D_2/PanelController.cs` |
| TaskbarButton.cs | `Assets/Dashboard_for_MR/Scripts/D_2/TaskbarButton.cs` |
| GameSettings.cs | `Assets/Scripts/GameSettings.cs` |

---

## 🎯 Quick Fix Commands

Run these from Unity menu `Tools > Dashboard`:

| Command | What it does |
|---------|--------------|
| `Complete Pause Menu Setup` | Does everything automatically |
| `Just Add TaskBar Button` | Only adds PauseButton to TaskBar |
| `Just Link to Manager` | Only links Pause_panel to WarehouseSceneManager |
| `Verify All Connections` | Shows what's connected and what's missing |

---

## 📞 Contact

If issues persist after following this guide:
1. Run `Tools > Dashboard > Verify All Connections`
2. Check Unity Console for error messages
3. Verify all components have their references assigned in Inspector

---

*Document created for the MR Robot Explorer project - December 2024*

