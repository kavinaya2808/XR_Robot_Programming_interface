// ============================================================================
// BootstrapMenuController.cs - Main Menu Scene Controller
// ============================================================================
//
// PURPOSE:
// Controls the bootstrap menu scene where users select:
// - Scene Mode: Warehouse Simulation or Real Room MR
// - Control Mode: ML / ROS / Keyboard
// Then loads the appropriate scene
//
// SETUP IN SCENE:
// 1. Create empty GameObject "MenuController"
// 2. Attach this script
// 3. Create UI buttons and link them via UnityEvents or the public methods
//
// MRTK SETUP:
// - Use MRTK3 PressableButton prefabs
// - Each button calls the appropriate method on this script
//
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BootstrapMenuController : MonoBehaviour
{
    // ========================================================================
    // UI REFERENCES (Optional - can also use UnityEvents)
    // ========================================================================
    
    [Header("Mode Selection UI")]
    [Tooltip("Button for Warehouse mode")]
    public GameObject warehouseButton;
    
    [Tooltip("Button for Real Room MR mode")]
    public GameObject realRoomButton;
    
    [Tooltip("Visual indicator for selected mode")]
    public TextMeshProUGUI modeDisplayText;
    
    [Header("Control Selection UI")]
    [Tooltip("Button for ML control")]
    public GameObject mlButton;
    
    [Tooltip("Button for ROS control")]
    public GameObject rosButton;
    
    [Tooltip("Button for Keyboard control")]
    public GameObject keyboardButton;
    
    [Tooltip("Visual indicator for selected control")]
    public TextMeshProUGUI controlDisplayText;
    
    [Header("Start/Info UI")]
    [Tooltip("Main start button")]
    public GameObject startButton;
    
    [Tooltip("Status/instruction text")]
    public TextMeshProUGUI statusText;
    
    [Header("Selection Colors")]
    public Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Header("Scene Names")]
    [Tooltip("Name of warehouse simulation scene")]
    public string warehouseSceneName = "10_WarehouseSim";
    
    [Tooltip("Name of real room MR scene")]
    public string realRoomSceneName = "20_RealRoomMR";
    
    // ========================================================================
    // RUNTIME STATE
    // ========================================================================
    
    private SceneMode selectedSceneMode = SceneMode.WarehouseSim;
    private GameControlMode selectedControlMode = GameControlMode.ML;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Start()
    {
        // Ensure GameSettings exists
        var settings = GameSettings.Instance;
        
        // Load any previously selected settings
        selectedSceneMode = settings.SceneMode;
        selectedControlMode = settings.ControlMode;
        
        // Update UI to match
        UpdateUI();
        
        Debug.Log("[BootstrapMenu] Menu initialized");
        
        // Show instructions
        if (statusText != null)
        {
            statusText.text = "Select Mode and Control type, then press START";
        }
    }
    
    // ========================================================================
    // PUBLIC API - SCENE MODE SELECTION
    // ========================================================================
    
    /// <summary>
    /// Select Warehouse Simulation mode
    /// </summary>
    public void SelectWarehouseMode()
    {
        selectedSceneMode = SceneMode.WarehouseSim;
        GameSettings.Instance.SetSceneMode(SceneMode.WarehouseSim);
        UpdateUI();
        Debug.Log("[BootstrapMenu] Selected: Warehouse Simulation");
    }
    
    /// <summary>
    /// Select Real Room MR mode
    /// </summary>
    public void SelectRealRoomMode()
    {
        selectedSceneMode = SceneMode.RealRoomMR;
        GameSettings.Instance.SetSceneMode(SceneMode.RealRoomMR);
        UpdateUI();
        Debug.Log("[BootstrapMenu] Selected: Real Room MR");
    }
    
    // ========================================================================
    // PUBLIC API - CONTROL MODE SELECTION
    // ========================================================================
    
    /// <summary>
    /// Select ML (Machine Learning) control
    /// </summary>
    public void SelectMLControl()
    {
        selectedControlMode = GameControlMode.ML;
        GameSettings.Instance.SetControlMode(GameControlMode.ML);
        UpdateUI();
        Debug.Log("[BootstrapMenu] Selected: ML Control");
    }
    
    /// <summary>
    /// Select ROS control
    /// </summary>
    public void SelectROSControl()
    {
        selectedControlMode = GameControlMode.ROS;
        GameSettings.Instance.SetControlMode(GameControlMode.ROS);
        UpdateUI();
        Debug.Log("[BootstrapMenu] Selected: ROS Control");
    }
    
    /// <summary>
    /// Select Keyboard control
    /// </summary>
    public void SelectKeyboardControl()
    {
        selectedControlMode = GameControlMode.Keyboard;
        GameSettings.Instance.SetControlMode(GameControlMode.Keyboard);
        UpdateUI();
        Debug.Log("[BootstrapMenu] Selected: Keyboard Control");
    }
    
    // ========================================================================
    // PUBLIC API - START GAME
    // ========================================================================
    
    /// <summary>
    /// Start the game with selected settings
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"[BootstrapMenu] Starting game - Mode: {selectedSceneMode}, Control: {selectedControlMode}");
        
        // Update status
        if (statusText != null)
        {
            statusText.text = "Loading...";
        }
        
        // Disable start button to prevent double-clicks
        if (startButton != null)
        {
            startButton.SetActive(false);
        }
        
        // Store settings (already done in selection, but ensure)
        GameSettings.Instance.SetSceneMode(selectedSceneMode);
        GameSettings.Instance.SetControlMode(selectedControlMode);
        
        // Load the appropriate scene
        StartCoroutine(LoadSceneAsync());
    }
    
    private IEnumerator LoadSceneAsync()
    {
        string sceneName = selectedSceneMode == SceneMode.WarehouseSim 
            ? warehouseSceneName 
            : realRoomSceneName;
        
        Debug.Log($"[BootstrapMenu] Loading scene: {sceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            // Update loading progress if you have a progress bar
            float progress = asyncLoad.progress;
            if (statusText != null)
            {
                statusText.text = $"Loading... {(progress * 100):F0}%";
            }
            yield return null;
        }
    }
    
    // ========================================================================
    // UI UPDATE
    // ========================================================================
    
    private void UpdateUI()
    {
        // Update mode display text
        if (modeDisplayText != null)
        {
            modeDisplayText.text = selectedSceneMode == SceneMode.WarehouseSim 
                ? "Mode: WAREHOUSE" 
                : "Mode: REAL ROOM";
        }
        
        // Update control display text
        if (controlDisplayText != null)
        {
            controlDisplayText.text = selectedControlMode switch
            {
                GameControlMode.ML => "Control: ML",
                GameControlMode.ROS => "Control: ROS",
                GameControlMode.Keyboard => "Control: KEYBOARD",
                _ => "Control: ML"
            };
        }
        
        // Update button colors (if using standard UI buttons)
        UpdateButtonColors();
    }
    
    private void UpdateButtonColors()
    {
        // Scene mode buttons
        SetButtonSelected(warehouseButton, selectedSceneMode == SceneMode.WarehouseSim);
        SetButtonSelected(realRoomButton, selectedSceneMode == SceneMode.RealRoomMR);
        
        // Control mode buttons
        SetButtonSelected(mlButton, selectedControlMode == GameControlMode.ML);
        SetButtonSelected(rosButton, selectedControlMode == GameControlMode.ROS);
        SetButtonSelected(keyboardButton, selectedControlMode == GameControlMode.Keyboard);
    }
    
    private void SetButtonSelected(GameObject buttonObj, bool selected)
    {
        if (buttonObj == null) return;
        
        // Try to find and color the button's image/renderer
        var image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? selectedColor : unselectedColor;
        }
        
        // Also check for a child with "Background" or similar
        var bgTransform = buttonObj.transform.Find("Background");
        if (bgTransform != null)
        {
            var bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = selected ? selectedColor : unselectedColor;
            }
        }
        
        // For MRTK buttons, you might need to access the backplate renderer
        var renderers = buttonObj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name.ToLower().Contains("backplate") || 
                r.gameObject.name.ToLower().Contains("background"))
            {
                r.material.color = selected ? selectedColor : unselectedColor;
            }
        }
    }
    
    // ========================================================================
    // CONVENIENCE - DIRECT SCENE LOADING (for testing)
    // ========================================================================
    
    /// <summary>
    /// Quick load warehouse scene (for testing)
    /// </summary>
    public void QuickLoadWarehouse()
    {
        SelectWarehouseMode();
        SelectMLControl();
        StartGame();
    }
    
    /// <summary>
    /// Quick load real room scene (for testing)
    /// </summary>
    public void QuickLoadRealRoom()
    {
        SelectRealRoomMode();
        SelectMLControl();
        StartGame();
    }
}

