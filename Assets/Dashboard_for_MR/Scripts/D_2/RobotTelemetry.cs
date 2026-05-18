using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RobotTelemetry : MonoBehaviour
{
    // Registry (lookup by robotId)
    public static readonly Dictionary<string, RobotTelemetry> Registry = new Dictionary<string, RobotTelemetry>();

    [Header("Identity")]
    [Tooltip("Unique ID (e.g., Robot_1)")]
    public string robotId = "Robot_1";

    [Header("Data Source")]
    [Tooltip("When TRUE: uses internal simulation. When FALSE: expects external data from TelemetryReceiver")]
    public bool useSimulatedData = true;
    
    [Tooltip("Timestamp of last external update (for timeout detection)")]
    [HideInInspector]
    public float lastExternalUpdateTime = 0f;

    [Header("Physical telemetry (read-only)")]
    [Tooltip("World position (meters)")]
    public Vector3 position;
    [Tooltip("Estimated velocity (m/s)")]
    public Vector3 velocity;
    [Tooltip("Speed magnitude (m/s) — computed from transform motion")]
    public float speed;
    [Tooltip("Heading direction (unit vector)")]
    public Vector3 heading;

    [Header("External/networked telemetry (optional)")]
    [Tooltip("If set by network, this value will be used by GetSpeed() instead of computed speed")]
    public float speed_mps = 0f;

    [Header("Battery simulation")]
    [Range(0f, 100f)]
    public float batteryPercent = 100f;           // 0..100
    [Tooltip("Watt consumption baseline when idle")]
    public float idlePowerW = 2f;
    [Tooltip("Watt per m/s additional when moving")]
    public float dynamicPowerPerSpeed = 4f;

    [Header("Task/status")]
    public string currentState = "Idle";          // Idle, Moving, Charging, Error, etc.
    public string activeTask = "None";

    [Header("Task progress (simulated)")]
    [Range(0f, 100f)]
    public float taskProgressPercent = 0f;       // 0..100
    public float taskEstimatedSecondsRemaining = 0f;

    [Header("Telemetry tuning")]
    [Tooltip("Time window for velocity smoothing (seconds)")]
    public float velocitySmoothing = 0.15f;

    [Header("Collect task")]
    public int collectTotalDots = 0;
    public int collectCollectedDots = 0;
    public float collectTimeLimitSeconds = 0f;
    public float collectTimeStarted = 0f; // Time.time when started

    // NEW: Task status for UI (keeps task name persistent)
    public enum TaskStatus { Incomplete, Completed, Expired }
    public TaskStatus taskStatus = TaskStatus.Incomplete;

    // internal
    private Vector3 lastPosition;
    private float lastTime;
    private Vector3 velocitySmoothed;

    void OnEnable()
    {
        if (string.IsNullOrEmpty(robotId)) robotId = gameObject.name;
        Registry[robotId] = this;

        lastPosition = transform.position;
        lastTime = Time.time;
        velocitySmoothed = Vector3.zero;
    }

    void OnDisable()
    {
        if (Registry.ContainsKey(robotId) && Registry[robotId] == this)
            Registry.Remove(robotId);
    }

    void Update()
    {
        // =====================================================
        // EXTERNAL DATA MODE: Skip internal simulation
        // Data is set directly by TelemetryReceiver via UDP
        // =====================================================
        if (!useSimulatedData)
        {
            // Just update heading from transform (if transform is being moved)
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            heading = (fwd.sqrMagnitude > 0.0001f) ? fwd.normalized : Vector3.forward;
            
            // Check for connection timeout (no data for 5 seconds)
            if (Time.time - lastExternalUpdateTime > 5f && lastExternalUpdateTime > 0f)
            {
                currentState = "Connection Lost";
            }
            
            // Skip all internal simulation - values are set externally
            lastPosition = position;
            lastTime = Time.time;
            return;
        }
        
        // =====================================================
        // SIMULATED DATA MODE: Internal calculations
        // =====================================================
        
        // --- POSITION & VELOCITY ----
        position = transform.position;

        float now = Time.time;
        float dt = Mathf.Max(1e-6f, now - lastTime);
        Vector3 rawVel = (position - lastPosition) / dt;

        float alpha = Mathf.Clamp01(dt / Mathf.Max(velocitySmoothing, dt));
        velocitySmoothed = Vector3.Lerp(velocitySmoothed, rawVel, alpha);

        velocity = velocitySmoothed;
        speed = velocity.magnitude;

        // heading (horizontal forward)
        var h = transform.forward;
        h.y = 0f;
        heading = (h.sqrMagnitude > 0.0001f) ? h.normalized : Vector3.forward;

        // --- STATE & BATTERY ---
        if (batteryPercent <= 10f)
            currentState = "Low Battery";
        else if (speed > 0.05f)
            currentState = "Moving";
        else
            currentState = "Idle";

        float batteryCapacityWs = 3600f * 1f;
        float powerW = idlePowerW + dynamicPowerPerSpeed * speed;
        float percentDrainedPerSec = (powerW / Mathf.Max(1f, batteryCapacityWs)) * 100f;
        batteryPercent = Mathf.Clamp(batteryPercent - percentDrainedPerSec * Time.deltaTime, 0f, 100f);

        // --- TASK / PROGRESS HANDLING ---
        // 1) If this is the special collect task, we only update ETA/timeouts here.
        if (!string.IsNullOrEmpty(activeTask) && activeTask == "Map Coverage")
        {
            // ETA countdown (does NOT modify progress percent)
            if (collectTimeLimitSeconds > 0f)
            {
                float elapsed = Mathf.Max(0f, Time.time - collectTimeStarted);
                taskEstimatedSecondsRemaining = Mathf.Max(0f, collectTimeLimitSeconds - elapsed);

                // If timer reached 0 and not all collected -> mark as expired
                if (taskEstimatedSecondsRemaining <= 0f && collectCollectedDots < collectTotalDots)
                {
                    // mark expired but KEEP the task name visible (do NOT clear activeTask)
                    //currentState = "Task Timeout";
                    taskStatus = TaskStatus.Expired;
                    activeTask = "None";
                    // keep activeTask as "Collect Dots" so UI shows the name
                }
            }

            // nothing else modifies taskProgressPercent for this task; it's updated on OnDotCollected()
        }
        // 2) Generic time-driven tasks (only apply when activeTask != Collect Dots)
        else if (!string.IsNullOrEmpty(activeTask) && activeTask != "None")
        {
            if (taskEstimatedSecondsRemaining > 0f)
            {
                float consumed = Time.deltaTime;
                taskEstimatedSecondsRemaining = Mathf.Max(0f, taskEstimatedSecondsRemaining - consumed);
                float initial = taskEstimatedSecondsRemaining + consumed;
                if (initial > 0f)
                {
                    float completed = (consumed / initial) * 100f;
                    taskProgressPercent = Mathf.Clamp(taskProgressPercent + completed, 0f, 100f);
                }
            }
            else
            {
                taskProgressPercent = 100f;
            }
        }

        // --- finalize housekeeping ---
        lastPosition = position;
        lastTime = now;
    }

    // Prefer network/external speed if supplied (speed_mps > tiny), otherwise return computed speed
    public float GetSpeed()
    {
        if (Mathf.Abs(speed_mps) > 1e-5f) return speed_mps;
        return speed;
    }

    // External API helpers
    public void SetSpeedMetersPerSecond(float s) => speed_mps = s;

    public float EstimatedBatterySecondsRemaining()
    {
        float powerW = idlePowerW + dynamicPowerPerSpeed * speed;
        float batteryCapacityWs = 3600f * 1f;
        if (powerW <= 0.0001f) return float.PositiveInfinity;
        float percentToZero = batteryPercent / 100f;
        float energyRemaining = batteryCapacityWs * percentToZero; // Ws
        float seconds = energyRemaining / powerW;
        return seconds;
    }

    // --- Collect task API ---
    public void StartCollectTask(int totalDots, float timeLimitSeconds, bool forceRestart = false)
    {
        // If a collect task is already active and we're not forcing a restart, ignore.
        if (!forceRestart && activeTask == "Collect Dots" && collectTotalDots > 0)
        {
            Debug.LogWarning($"RobotTelemetry[{robotId}] StartCollectTask ignored: collect task already active with total={collectTotalDots}. Use forceRestart=true to override.");
            return;
        }

        collectTotalDots = Mathf.Max(0, totalDots);
        collectCollectedDots = 0;
        collectTimeLimitSeconds = Mathf.Max(0f, timeLimitSeconds);
        collectTimeStarted = Time.time;

        activeTask = "Map Coverage";
        taskStatus = TaskStatus.Incomplete;
        taskProgressPercent = 0f;
        taskEstimatedSecondsRemaining = (collectTimeLimitSeconds > 0f) ? collectTimeLimitSeconds : -1f;
        currentState = "Collecting";

        Debug.Log($"RobotTelemetry[{robotId}] StartCollectTask total={collectTotalDots} timeLimit={collectTimeLimitSeconds}s (forceRestart={forceRestart})");
    }

    /// <summary>
    /// Called when a robot collects a dot. Progress is computed as (collected/total)*100.
    /// This is the ONLY place where taskProgressPercent is modified for the collect task.
    /// </summary>
    public void OnDotCollected(int value = 1)
    {
        // DEFENSIVE CHECK: if no collect task in place, ignore and warn
        if (collectTotalDots <= 0)
        {
            Debug.LogWarning($"RobotTelemetry[{robotId}].OnDotCollected called but collectTotalDots==0. " +
                             $"Did StartCollectTask run for this robotId?");
            return;
        }

        collectCollectedDots += Mathf.Max(0, value);
        collectCollectedDots = Mathf.Clamp(collectCollectedDots, 0, collectTotalDots);

        // update progress strictly by fraction of dots collected
        taskProgressPercent = (collectCollectedDots / (float)collectTotalDots) * 100f;

        // if completed set final state but KEEP activeTask name visible
        if (collectCollectedDots >= collectTotalDots)
        {
            taskEstimatedSecondsRemaining = 0f;
            //currentState = "Task Done";
            taskStatus = TaskStatus.Completed;
            activeTask = "None";
            // DO NOT clear activeTask — UI will keep showing the task name
            Debug.Log($"RobotTelemetry[{robotId}] collection completed ({collectCollectedDots}/{collectTotalDots}) -> {taskProgressPercent}%");
        }
        else
        {
            // recompute ETA (optional)
            if (collectTimeLimitSeconds > 0f)
            {
                float elapsed = Mathf.Max(0f, Time.time - collectTimeStarted);
                float remaining = Mathf.Max(0f, collectTimeLimitSeconds - elapsed);
                taskEstimatedSecondsRemaining = remaining;
            }

            Debug.Log($"RobotTelemetry[{robotId}] collected {collectCollectedDots}/{collectTotalDots} -> {taskProgressPercent}% (ETA {taskEstimatedSecondsRemaining}s)");
        }
    }
}
