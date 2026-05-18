# 🔧 PAUSE MENU - QUICK FIX GUIDE

**Problem:** Pause Menu doesn't work (Resume/Return to Menu buttons do nothing)

---

## ⚡ THE FIX (5 Minutes)

### Step 1: Open the correct scene

```
File → Open Scene → Assets/Robotics/Scenes/10_WarehouseSim.unity
```

---

### Step 2: Find these GameObjects in Hierarchy

Look for:
- `WarehouseManager` (or create empty GameObject, rename to this)
- `Pause_panel` (under Dashboard or similar)
- `TaskBar` → `TaskBar-1` → look for `PauseButton`

If `PauseButton` doesn't exist, duplicate `BatteryButton` and rename it.

---

### Step 3: Configure Pause_panel

1. **Select `Pause_panel`** in Hierarchy
2. In Inspector, check for `PanelController` component:
   - `Panel Id` = **`Pause`** ← Type this exactly!
   - `Panel Root` = drag `Pause_panel` itself here

3. Check for `PauseMenuPanelUI` component:
   - If missing, click **Add Component** → search "PauseMenuPanelUI"
   - `Menu Scene Name` = **`00_BootstrapMenu`**

---

### Step 4: Create/Fix the Buttons inside Pause_panel

Find or create buttons inside `Pause_panel > ... > Content`:

#### Resume Button:
1. Create a UI Button or use existing
2. Rename to `ResumeButton`
3. In `Button` component → `On Click ()`:
   - Click **+**
   - Drag `Pause_panel` to object slot
   - Select: **PauseMenuPanelUI → OnResumePressed**

#### Return to Menu Button:
1. Create a UI Button or use existing  
2. Rename to `ReturnToMenuButton`
3. In `Button` component → `On Click ()`:
   - Click **+**
   - Drag `Pause_panel` to object slot
   - Select: **PauseMenuPanelUI → OnReturnToMenuPressed**

---

### Step 5: Configure TaskBar Pause Button

1. Find `TaskBar > TaskBar-1 > PauseButton` (create if needed)
2. Add `TaskbarButton` component if missing
3. Set `Panel Id` = **`Pause`** ← Must match exactly!
4. Find the MRTK `PressableButton` component on this button
5. In `OnClicked ()` event:
   - Click **+**
   - Drag `PauseButton` (itself) to object slot
   - Select: **TaskbarButton → OnPressed**

---

### Step 6: Link WarehouseSceneManager

1. **Select `WarehouseManager`** in Hierarchy
2. Check for `WarehouseSceneManager` component (add if missing)
3. Set:
   - `Pause Menu` = drag `Pause_panel` here
   - `Robots` → expand, set Size = 2
   - Element 0 = drag `robot1`
   - Element 1 = drag `robot2`

---

### Step 7: Verify Build Settings

1. `File → Build Settings`
2. Click **Add Open Scenes** or ensure these scenes are listed:
   - `00_BootstrapMenu`
   - `10_WarehouseSim`
   - `20_RealRoomMR`

---

### Step 8: SAVE AND TEST

1. **Ctrl+S** to save scene
2. Press **Play**
3. Press **ESC** key → Pause panel should appear
4. Click **Resume** → Game should continue
5. Click **Return to Menu** → Should load Bootstrap menu

---

## 🐛 Common Issues

| Problem | Solution |
|---------|----------|
| Panel doesn't open | Check `panelId` matches between `PanelController` and `TaskbarButton` (both must be "Pause") |
| Resume does nothing | Check Button's OnClick has `PauseMenuPanelUI.OnResumePressed` |
| Return to Menu crashes | Add `00_BootstrapMenu` to Build Settings |
| ESC key doesn't work | Check `WarehouseSceneManager` exists and has `pauseMenu` assigned |
| Robots keep moving when paused | `Time.timeScale` should be 0 when paused (this is automatic) |

---

## 📋 Verification Checklist

- [ ] `Pause_panel` → `PanelController` → `panelId` = "Pause"
- [ ] `Pause_panel` → `PauseMenuPanelUI` → exists
- [ ] `ResumeButton` → `Button.OnClick` → calls `OnResumePressed`
- [ ] `ReturnToMenuButton` → `Button.OnClick` → calls `OnReturnToMenuPressed`  
- [ ] `TaskBar` → `PauseButton` → `TaskbarButton` → `panelId` = "Pause"
- [ ] `TaskBar` → `PauseButton` → `PressableButton.OnClicked` → calls `TaskbarButton.OnPressed`
- [ ] `WarehouseManager` → `WarehouseSceneManager` → `pauseMenu` = Pause_panel
- [ ] Build Settings includes `00_BootstrapMenu`

---

## 🎯 KEY CONCEPT

```
When ESC is pressed:
  WarehouseSceneManager.Update() detects it
    → WarehouseSceneManager.TogglePause() is called
      → GameSettings.TogglePause() sets _gamePaused = true
      → Time.timeScale = 0f (freezes game)
      → pauseMenu.SetActive(true) (shows panel)

When Resume is clicked:
  Button.OnClick event fires
    → PauseMenuPanelUI.OnResumePressed() is called
      → WarehouseSceneManager.TogglePause() 
        → Time.timeScale = 1f (unfreezes)
        → pauseMenu.SetActive(false) (hides panel)

When Return to Menu is clicked:
  Button.OnClick event fires
    → PauseMenuPanelUI.OnReturnToMenuPressed()
      → Time.timeScale = 1f (reset)
      → SceneManager.LoadScene("00_BootstrapMenu")
```

---

## 📁 Files Involved

| Script | Location |
|--------|----------|
| WarehouseSceneManager.cs | `Assets/Scripts/` |
| PauseMenuPanelUI.cs | `Assets/Dashboard_for_MR/Scripts/D_2/` |
| PanelController.cs | `Assets/Dashboard_for_MR/Scripts/D_2/` |
| TaskbarButton.cs | `Assets/Dashboard_for_MR/Scripts/D_2/` |
| GameSettings.cs | `Assets/Scripts/` |

---

*If all else fails, delete `Pause_panel` and run the automated setup:*
*`Tools → Dashboard → Complete Pause Menu Setup`*

