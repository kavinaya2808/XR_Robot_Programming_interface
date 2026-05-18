using UnityEngine;
using TMPro;

public class TaskProgressPanelUI : MonoBehaviour
{
    public string robotId = "Robot_1";

    public TextMeshProUGUI taskNameText;
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI etaText;
    public TextMeshProUGUI taskStatusText; // NEW

    public float updateHz = 4f;
    float nextTime = 0f;

    void Update()
    {
        if (Time.time < nextTime) return;
        nextTime = Time.time + 1f / Mathf.Max(0.1f, updateHz);
        Refresh();
    }

    public void Refresh()
    {
        if (!RobotTelemetry.Registry.TryGetValue(robotId, out var t))
        {
            taskNameText.text = "No robot";
            progressPercentText.text = "-";
            etaText.text = "-";
            if (taskStatusText) taskStatusText.text = "-";
            Debug.LogWarning($"[TaskProgressPanelUI] Robot {robotId} NOT found in Registry!");
            return;
        }

        // Show the task name even after completion/expiry (we keep activeTask)
        taskNameText.text = "Map Coverage";

        // Progress strictly from telemetry
        progressPercentText.text = $"{t.taskProgressPercent:F0}%";
        
        // Debug: Log every 4 updates (every 1 second at 4Hz)
        if (Time.frameCount % 4 == 0)
        {
            Debug.Log($"[TaskProgressPanelUI] {robotId}: Coverage = {t.taskProgressPercent:F1}% " +
                $"(text: '{progressPercentText.text}')");
        }

        // ETA: show seconds while active/incomplete, "Done" on completion, "Expired" if expired
        if (t.taskStatus == RobotTelemetry.TaskStatus.Completed)
        {
            etaText.text = "Done";
            if (taskStatusText) taskStatusText.text = "Completed";
        }
        else if (t.taskStatus == RobotTelemetry.TaskStatus.Expired)
        {
            etaText.text = "Timeout";
            if (taskStatusText) taskStatusText.text = "Expired";
        }
        else
        {
            // Incomplete (show countdown if available)
            if (t.taskEstimatedSecondsRemaining > 0f)
                etaText.text = $"{Mathf.CeilToInt(t.taskEstimatedSecondsRemaining)}s";
            else
                etaText.text = "-";

            if (taskStatusText) taskStatusText.text = "Incomplete";
        }
    }
}
