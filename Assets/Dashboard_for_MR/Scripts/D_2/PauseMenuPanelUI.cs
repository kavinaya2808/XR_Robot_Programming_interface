// ============================================================================
// PauseMenuPanelUI.cs - Pause Menu Panel for MR Dashboard
// ============================================================================
// MRTK3 PressableButtons Only
//
// SETUP INSTRUCTIONS:
// 1. Add PressableButton components to your buttons in the hierarchy
// 2. Assign the three PressableButton components in Inspector
// 3. Supports: Resume (pause toggle), Return to Menu (bootstrap scene), 
//    Restart (reload current scene)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit.UX;   // MRTK3

public class PauseMenuPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI gameTimeText;
    public TextMeshProUGUI coverageText;

    [Header("MRTK PressableButtons")]
    public PressableButton resumePressableButton;
    public PressableButton returnMenuPressableButton;
    public PressableButton restartPressableButton;

    [Header("Scene Manager Reference")]
    public WarehouseSceneManager warehouseManager;

    [Header("Settings")]
    public string menuSceneName = "00_BootstrapMenu";
    public float updateHz = 2f;

    [Header("Coverage Manager")]
    public MapCoverageManager coverageManager;

    // Private
    private float nextUpdateTime = 0f;
    private float gameStartTime;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    void Start()
    {
        if (warehouseManager == null)
            warehouseManager = FindObjectOfType<WarehouseSceneManager>();

        if (coverageManager == null)
            coverageManager = MapCoverageManager.Instance;

        // ---- Wire MRTK PressableButtons ----
        if (resumePressableButton != null)
        {
            resumePressableButton.OnClicked.AddListener(OnResumePressed);
            Debug.Log($"[PauseMenuPanelUI] ✓ Wired Resume Button: {resumePressableButton.gameObject.name}");
        }
        else
        {
            Debug.LogError("[PauseMenuPanelUI] ✗ Resume PressableButton not assigned!");
        }

        if (returnMenuPressableButton != null)
        {
            returnMenuPressableButton.OnClicked.AddListener(OnReturnToMenuPressed);
            Debug.Log($"[PauseMenuPanelUI] ✓ Wired Return to Menu Button: {returnMenuPressableButton.gameObject.name}");
        }
        else
        {
            Debug.LogError("[PauseMenuPanelUI] ✗ Return Menu PressableButton not assigned!");
        }

        if (restartPressableButton != null)
        {
            restartPressableButton.OnClicked.AddListener(OnRestartPressed);
            Debug.Log($"[PauseMenuPanelUI] ✓ Wired Restart Button: {restartPressableButton.gameObject.name}");
        }
        else
        {
            Debug.LogError("[PauseMenuPanelUI] ✗ Restart PressableButton not assigned!");
        }

        gameStartTime = Time.time;

        if (titleText != null)
            titleText.text = "PAUSE MENU";

        Debug.Log("[PauseMenuPanelUI] ✓ Initialized with MRTK PressableButtons");
    }

    void Update()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + 1f / Mathf.Max(0.1f, updateHz);
        Refresh();
    }

    // ========================================================================
    // UI UPDATE
    // ========================================================================

    public void Refresh()
    {
        if (statusText != null)
        {
            if (GameSettings.Instance != null)
            {
                if (GameSettings.Instance.GamePaused)
                    statusText.text = "PAUSED";
                else if (GameSettings.Instance.GameStarted)
                    statusText.text = "Running";
                else
                    statusText.text = "Ready";
            }
            else
            {
                statusText.text = Time.timeScale < 0.1f ? "PAUSED" : "Running";
            }
        }

        if (gameTimeText != null)
        {
            float elapsed = Time.time - gameStartTime;
            int minutes = (int)(elapsed / 60);
            int seconds = (int)(elapsed % 60);
            gameTimeText.text = $"Time: {minutes:D2}:{seconds:D2}";
        }

        if (coverageText != null && coverageManager != null)
        {
            float coverage = coverageManager.GetCoverageFraction() * 100f;
            coverageText.text = $"Coverage: {coverage:F1}%";
        }
    }

    // ========================================================================
    // BUTTON HANDLERS
    // ========================================================================

    public void OnResumePressed()
    {
        Debug.Log("[PauseMenuPanelUI] ▶ Resume pressed - unpausing game");

        // Resume the game (set timeScale back to 1)
        Time.timeScale = 1f;

        // Also notify WarehouseSceneManager if available
        if (warehouseManager != null && GameSettings.Instance != null)
        {
            if (GameSettings.Instance.GamePaused)
                warehouseManager.TogglePause();
        }

        // Close the pause menu panel
        PanelController panel = GetComponent<PanelController>();
        if (panel != null)
        {
            panel.ClosePanel();
            Debug.Log("[PauseMenuPanelUI] Closed pause menu panel");
        }
    }

    public void OnReturnToMenuPressed()
    {
        Debug.Log("[PauseMenuPanelUI] 🏠 Return to Menu pressed - loading bootstrap scene");

        // Resume game time before loading menu
        Time.timeScale = 1f;

        // Load the bootstrap menu scene
        if (warehouseManager != null)
        {
            warehouseManager.ReturnToMenu();
        }
        else
        {
            Debug.Log($"[PauseMenuPanelUI] Loading scene: {menuSceneName}");
            SceneManager.LoadScene(menuSceneName);
        }
    }

    public void OnRestartPressed()
    {
        Debug.Log("[PauseMenuPanelUI] 🔄 Restart pressed - reloading current scene");

        // Resume game time before restarting
        Time.timeScale = 1f;

        // Reload the current scene
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[PauseMenuPanelUI] Reloading scene: {currentScene}");

        if (warehouseManager != null)
        {
            warehouseManager.RestartScene();
        }
        else
        {
            SceneManager.LoadScene(currentScene);
        }
    }

    public void TogglePause()
    {
        if (warehouseManager != null)
            warehouseManager.TogglePause();
        else
            Time.timeScale = Time.timeScale < 0.1f ? 1f : 0f;

        Refresh();
    }

    // ========================================================================
    // PANEL EVENTS
    // ========================================================================

    public void OnPanelOpened()
    {
        Refresh();
    }

    public void OnPanelClosed()
    {
    }
}