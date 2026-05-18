// ============================================================================
// BatteryModel.cs - Realistic Battery Simulation with Time Remaining
// ============================================================================
//
// PURPOSE:
// Provides a realistic battery model that:
// - Simulates actual battery capacity (Watt-hours)
// - Drains based on robot activity (idle vs moving)
// - Calculates STABLE time remaining using smoothed average power
//
// WHY SMOOTHED AVERAGE?
// If we calculate time remaining from instantaneous power, it jumps wildly:
// - Robot accelerates: "10 min remaining" 
// - Robot stops: "999 min remaining"
// 
// By using a moving average of power over 30 seconds, the estimate is stable.
//
// USAGE:
// Attach to each robot. The HUD/Dashboard reads public properties.
//
// ============================================================================

using UnityEngine;
using System.Collections.Generic;

public class BatteryModel : MonoBehaviour
{
    // ========================================================================
    // BATTERY SPECIFICATIONS
    // ========================================================================
    
    [Header("Battery Capacity")]
    [Tooltip("Total battery capacity in Watt-hours (Wh). Typical small robot: 10-20 Wh")]
    public float capacityWh = 15f;
    
    [Tooltip("Current energy remaining in Watt-hours")]
    [SerializeField] private float currentEnergyWh;
    
    [Header("Power Consumption")]
    [Tooltip("Base power draw when idle (Watts). Electronics, sensors always on.")]
    public float idlePowerW = 2f;
    
    [Tooltip("Base power when moving (Watts). Motors + electronics.")]
    public float movingBasePowerW = 5f;
    
    [Tooltip("Additional power per m/s of linear speed (Watts per m/s)")]
    public float powerPerLinearSpeed = 3f;
    
    [Tooltip("Additional power per rad/s of angular speed (Watts per rad/s)")]
    public float powerPerAngularSpeed = 1.5f;
    
    [Header("Time Estimation")]
    [Tooltip("Seconds of power history to average for time estimate")]
    public float smoothingWindowSeconds = 30f;
    
    [Tooltip("Minimum power for time calculation (prevents division by tiny numbers)")]
    public float minPowerForEstimate = 0.5f;
    
    [Header("Robot Reference")]
    [Tooltip("Transform to track for speed calculation (auto-detects if empty)")]
    public Transform robotTransform;
    
    [Tooltip("Optional: Reference to TelemetryPublisher to share data")]
    public TelemetryPublisher telemetryPublisher;
    
    // ========================================================================
    // PUBLIC PROPERTIES (for HUD/Dashboard to read)
    // ========================================================================
    
    /// <summary>Battery percentage (0-100)</summary>
    public float BatteryPercent => (currentEnergyWh / capacityWh) * 100f;
    
    /// <summary>Current power draw in Watts</summary>
    public float CurrentPowerW => _currentPowerW;
    
    /// <summary>Smoothed average power over the window</summary>
    public float AveragePowerW => _averagePowerW;
    
    /// <summary>Estimated time remaining in seconds</summary>
    public float TimeRemainingSeconds => _timeRemainingSeconds;
    
    /// <summary>Formatted time remaining string (e.g., "14m 32s")</summary>
    public string TimeRemainingFormatted => FormatTime(_timeRemainingSeconds);
    
    /// <summary>Current linear speed in m/s</summary>
    public float LinearSpeed => _linearSpeed;
    
    /// <summary>Current angular speed in rad/s</summary>
    public float AngularSpeed => _angularSpeed;
    
    /// <summary>Is the robot currently moving?</summary>
    public bool IsMoving => _linearSpeed > 0.05f || Mathf.Abs(_angularSpeed) > 0.05f;
    
    // ========================================================================
    // PRIVATE STATE
    // ========================================================================
    
    private float _currentPowerW;
    private float _averagePowerW;
    private float _timeRemainingSeconds;
    private float _linearSpeed;
    private float _angularSpeed;
    
    // For speed calculation
    private Vector3 _lastPosition;
    private float _lastYaw;
    
    // Power history for smoothing
    private Queue<PowerSample> _powerHistory = new Queue<PowerSample>();
    
    private struct PowerSample
    {
        public float time;
        public float powerW;
    }
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    void Start()
    {
        // Initialize battery to full
        currentEnergyWh = capacityWh;
        
        // Auto-find robot transform
        if (robotTransform == null)
            robotTransform = transform;
        
        // Initialize tracking
        _lastPosition = robotTransform.position;
        _lastYaw = robotTransform.eulerAngles.y;
        
        // Try to find telemetry publisher
        if (telemetryPublisher == null)
            telemetryPublisher = GetComponent<TelemetryPublisher>();
        
        Debug.Log($"[BatteryModel] Initialized: {capacityWh} Wh capacity");
    }
    
    void Update()
    {
        if (robotTransform == null) return;
        
        float dt = Time.deltaTime;
        if (dt < 0.001f) return; // Skip tiny frames
        
        // 1. Calculate speeds from position/rotation changes
        CalculateSpeeds(dt);
        
        // 2. Calculate current power draw
        CalculatePower();
        
        // 3. Drain battery
        DrainBattery(dt);
        
        // 4. Calculate smoothed average power
        UpdatePowerHistory();
        
        // 5. Estimate time remaining
        CalculateTimeRemaining();
        
        // 6. Share with TelemetryPublisher if available
        if (telemetryPublisher != null)
        {
            // TelemetryPublisher has its own battery field, but we can override
            // by accessing it directly or via a SetBattery method if it has one
        }
    }
    
    // ========================================================================
    // CALCULATION METHODS
    // ========================================================================
    
    private void CalculateSpeeds(float dt)
    {
        // Linear speed from position change
        Vector3 positionDelta = robotTransform.position - _lastPosition;
        _linearSpeed = positionDelta.magnitude / dt;
        
        // Angular speed from yaw change
        float currentYaw = robotTransform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(_lastYaw, currentYaw);
        _angularSpeed = Mathf.Abs(yawDelta * Mathf.Deg2Rad) / dt;
        
        // Update last values
        _lastPosition = robotTransform.position;
        _lastYaw = currentYaw;
    }
    
    private void CalculatePower()
    {
        if (IsMoving)
        {
            // Moving: base + linear contribution + angular contribution
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
        // So: Wh -= W × (seconds / 3600)
        float energyUsedWh = _currentPowerW * (dt / 3600f);
        currentEnergyWh = Mathf.Max(0f, currentEnergyWh - energyUsedWh);
    }
    
    private void UpdatePowerHistory()
    {
        // Add current sample
        _powerHistory.Enqueue(new PowerSample { time = Time.time, powerW = _currentPowerW });
        
        // Remove samples older than the window
        float cutoffTime = Time.time - smoothingWindowSeconds;
        while (_powerHistory.Count > 0 && _powerHistory.Peek().time < cutoffTime)
        {
            _powerHistory.Dequeue();
        }
        
        // Calculate average
        if (_powerHistory.Count > 0)
        {
            float sum = 0f;
            foreach (var sample in _powerHistory)
            {
                sum += sample.powerW;
            }
            _averagePowerW = sum / _powerHistory.Count;
        }
        else
        {
            _averagePowerW = _currentPowerW;
        }
    }
    
    private void CalculateTimeRemaining()
    {
        // Use smoothed average power for stable estimate
        float powerToUse = Mathf.Max(_averagePowerW, minPowerForEstimate);
        
        // Time (hours) = Energy (Wh) / Power (W)
        float hoursRemaining = currentEnergyWh / powerToUse;
        
        // Convert to seconds
        _timeRemainingSeconds = hoursRemaining * 3600f;
        
        // Clamp to reasonable range
        _timeRemainingSeconds = Mathf.Clamp(_timeRemainingSeconds, 0f, 360000f); // Max 100 hours
    }
    
    // ========================================================================
    // UTILITY METHODS
    // ========================================================================
    
    /// <summary>
    /// Format seconds into readable time string
    /// </summary>
    public static string FormatTime(float totalSeconds)
    {
        if (totalSeconds <= 0) return "0s";
        if (float.IsInfinity(totalSeconds) || float.IsNaN(totalSeconds)) return "--";
        
        int hours = (int)(totalSeconds / 3600);
        int minutes = (int)((totalSeconds % 3600) / 60);
        int seconds = (int)(totalSeconds % 60);
        
        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }
        else if (minutes > 0)
        {
            return $"{minutes}m {seconds}s";
        }
        else
        {
            return $"{seconds}s";
        }
    }
    
    /// <summary>
    /// Recharge battery to full (for testing/reset)
    /// </summary>
    public void Recharge()
    {
        currentEnergyWh = capacityWh;
        _powerHistory.Clear();
        Debug.Log("[BatteryModel] Battery recharged to full");
    }
    
    /// <summary>
    /// Set battery to specific percentage (for testing)
    /// </summary>
    public void SetBatteryPercent(float percent)
    {
        currentEnergyWh = capacityWh * Mathf.Clamp01(percent / 100f);
    }
    
    /// <summary>
    /// Get a detailed status string (for debugging)
    /// </summary>
    public string GetStatusString()
    {
        return $"Battery: {BatteryPercent:F1}% | Power: {_currentPowerW:F1}W (avg: {_averagePowerW:F1}W) | Time: {TimeRemainingFormatted}";
    }
    
    // ========================================================================
    // DEBUG VISUALIZATION
    // ========================================================================
    
    void OnGUI()
    {
        #if UNITY_EDITOR
        // Only show in editor for debugging
        if (!Application.isPlaying) return;
        
        // Optional: Draw debug info in corner
        // Uncomment if you want on-screen debug:
        /*
        GUI.Label(new Rect(10, 10, 300, 20), $"Battery: {BatteryPercent:F1}%");
        GUI.Label(new Rect(10, 30, 300, 20), $"Power: {_currentPowerW:F1}W (avg: {_averagePowerW:F1}W)");
        GUI.Label(new Rect(10, 50, 300, 20), $"Time Remaining: {TimeRemainingFormatted}");
        GUI.Label(new Rect(10, 70, 300, 20), $"Speed: {_linearSpeed:F2} m/s, {_angularSpeed:F2} rad/s");
        */
        #endif
    }
}

