// ============================================================================
// GameSettings.cs - Global Game Configuration Singleton
// ============================================================================
//
// PURPOSE:
// Stores user-selected settings that persist across scene loads:
// - Scene Mode (Warehouse Simulation vs Real Room MR)
// - Control Mode (ML / ROS / Keyboard)
// - Game state (started/paused)
//
// USAGE:
// Access anywhere via: GameSettings.Instance.ControlMode
// Set values via: GameSettings.Instance.SetControlMode(ControlMode.ML)
//
// ============================================================================

using UnityEngine;

/// <summary>
/// Scene/Environment mode selection
/// </summary>
public enum SceneMode
{
    WarehouseSim,   // Virtual warehouse environment
    RealRoomMR      // Real room with passthrough + MRUK
}

/// <summary>
/// Robot control mode selection (mirrors AGVController.ControlMode)
/// </summary>
public enum GameControlMode
{
    ML,        // ML-Agents neural network control
    ROS,       // ROS2 navigation stack control
    Keyboard   // Manual keyboard/controller input
}

/// <summary>
/// Singleton that stores game settings across scenes.
/// DontDestroyOnLoad ensures it persists through scene transitions.
/// </summary>
public class GameSettings : MonoBehaviour
{
    // ========================================================================
    // SINGLETON PATTERN
    // ========================================================================
    
    private static GameSettings _instance;
    
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindObjectOfType<GameSettings>();
                
                // Create new if not found
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameSettings");
                    _instance = go.AddComponent<GameSettings>();
                }
            }
            return _instance;
        }
    }
    
    // ========================================================================
    // SETTINGS
    // ========================================================================
    
    [Header("Scene Settings")]
    [SerializeField] private SceneMode _sceneMode = SceneMode.WarehouseSim;
    
    [Header("Control Settings")]
    [SerializeField] private GameControlMode _controlMode = GameControlMode.ML;
    
    [Header("Game State")]
    [SerializeField] private bool _gameStarted = false;
    [SerializeField] private bool _gamePaused = false;
    
    [Header("Robot Settings")]
    [SerializeField] private int _numberOfRobots = 2;
    
    // ========================================================================
    // PUBLIC PROPERTIES
    // ========================================================================
    
    public SceneMode SceneMode => _sceneMode;
    public GameControlMode ControlMode => _controlMode;
    public bool GameStarted => _gameStarted;
    public bool GamePaused => _gamePaused;
    public int NumberOfRobots => _numberOfRobots;
    
    // ========================================================================
    // EVENTS (for UI updates)
    // ========================================================================
    
    public event System.Action<SceneMode> OnSceneModeChanged;
    public event System.Action<GameControlMode> OnControlModeChanged;
    public event System.Action<bool> OnGameStartedChanged;
    public event System.Action<bool> OnGamePausedChanged;
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Awake()
    {
        // Singleton enforcement
        if (_instance != null && _instance != this)
        {
            Debug.Log("[GameSettings] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameSettings] Initialized");
    }
    
    // ========================================================================
    // PUBLIC API - SETTERS
    // ========================================================================
    
    /// <summary>
    /// Set the scene mode (Warehouse or Real Room)
    /// </summary>
    public void SetSceneMode(SceneMode mode)
    {
        if (_sceneMode != mode)
        {
            _sceneMode = mode;
            Debug.Log($"[GameSettings] Scene mode set to: {mode}");
            OnSceneModeChanged?.Invoke(mode);
        }
    }
    
    /// <summary>
    /// Set the control mode (ML, ROS, or Keyboard)
    /// </summary>
    public void SetControlMode(GameControlMode mode)
    {
        if (_controlMode != mode)
        {
            _controlMode = mode;
            Debug.Log($"[GameSettings] Control mode set to: {mode}");
            OnControlModeChanged?.Invoke(mode);
        }
    }
    
    /// <summary>
    /// Set number of robots to spawn
    /// </summary>
    public void SetNumberOfRobots(int count)
    {
        _numberOfRobots = Mathf.Clamp(count, 1, 4);
        Debug.Log($"[GameSettings] Number of robots set to: {_numberOfRobots}");
    }
    
    /// <summary>
    /// Start the game (enables robot movement)
    /// </summary>
    public void StartGame()
    {
        if (!_gameStarted)
        {
            _gameStarted = true;
            _gamePaused = false;
            Debug.Log("[GameSettings] Game STARTED");
            OnGameStartedChanged?.Invoke(true);
        }
    }
    
    /// <summary>
    /// Stop/Reset the game
    /// </summary>
    public void StopGame()
    {
        if (_gameStarted)
        {
            _gameStarted = false;
            _gamePaused = false;
            Debug.Log("[GameSettings] Game STOPPED");
            OnGameStartedChanged?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        _gamePaused = !_gamePaused;
        Debug.Log($"[GameSettings] Game {(_gamePaused ? "PAUSED" : "RESUMED")}");
        OnGamePausedChanged?.Invoke(_gamePaused);
    }
    
    // ========================================================================
    // HELPER METHODS
    // ========================================================================
    
    /// <summary>
    /// Convert GameControlMode to AGVController.ControlMode
    /// </summary>
    public RosSharp.Control.ControlMode GetAGVControlMode()
    {
        switch (_controlMode)
        {
            case GameControlMode.ML:
                return RosSharp.Control.ControlMode.ML;
            case GameControlMode.ROS:
                return RosSharp.Control.ControlMode.ROS;
            case GameControlMode.Keyboard:
            default:
                return RosSharp.Control.ControlMode.Keyboard;
        }
    }
    
    /// <summary>
    /// Get scene name to load based on current mode
    /// </summary>
    public string GetSceneToLoad()
    {
        return _sceneMode switch
        {
            SceneMode.WarehouseSim => "10_WarehouseSim",
            SceneMode.RealRoomMR => "20_RealRoomMR",
            _ => "10_WarehouseSim"
        };
    }
    
    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        _sceneMode = SceneMode.WarehouseSim;
        _controlMode = GameControlMode.ML;
        _gameStarted = false;
        _gamePaused = false;
        _numberOfRobots = 2;
        Debug.Log("[GameSettings] Reset to defaults");
    }
}

