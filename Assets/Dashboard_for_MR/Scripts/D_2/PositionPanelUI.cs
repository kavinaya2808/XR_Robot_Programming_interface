using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Updates the Position panel UI for a robot (position, velocity, heading, speed).
/// Expects RobotTelemetry.Registry to contain entries keyed by robotId.
/// Defensive: will not throw NRE if inspector refs are missing; logs clear messages instead.
/// </summary>
public class PositionPanelUI : MonoBehaviour
{
    [Tooltip("Robot ID to display (e.g., Robot_1)")]
    public string robotId = "Robot_1";

    [Header("Text fields (TextMeshProUGUI)")]
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI velocityText;
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI speedText;

    [Header("Optional UI (not required)")]
    public RectTransform needleRect;     // optional: if you want this script to control the needle
    public Image dialImage;              // optional

    [Header("Telemetry (optional override)")]
    public RobotTelemetry robotTelemetry; // optional: if set, used instead of Registry lookup

    [Header("Update settings")]
    public float updateHz = 10f; // 10 updates per second

    float nextUpdateTime = 0f;

    void Reset()
    {
        // Try to auto-wire common children if present (convenience, not required)
        if (positionText == null) positionText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void Update()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + 1f / Mathf.Max(0.1f, updateHz);
        Refresh();
    }

    public void Refresh()
    {
        RobotTelemetry t = null;

        if (robotTelemetry != null)
        {
            t = robotTelemetry;
        }
        else
        {
            // Try lookup in registry
            if (!RobotTelemetry.Registry.TryGetValue(robotId, out t))
            {
                SetMissing();
                return;
            }
        }

        if (t == null)
        {
            SetMissing();
            return;
        }

        // position
        var p = t.position;
        if (positionText != null)
            positionText.text = $"<b>{robotId}({p.x:F2}, {p.y:F2}, {p.z:F2})";
        else
            Debug.LogWarning($"[PositionPanelUI] positionText not assigned for {gameObject.name}");

        // velocity vector and speed
        var v = t.velocity;
        if (velocityText != null)
            velocityText.text = $"Vx: {v.x:F2}  Vy: {v.y:F2}  Vz: {v.z:F2}";
        else
            Debug.LogWarning($"[PositionPanelUI] velocityText not assigned for {gameObject.name}");

        // speed (m/s)
        float speed = t.GetSpeed(); // prefer external setpoint if present or computed speed
        if (speedText != null)
            speedText.text = $"{speed:F2} m/s";
        else
            Debug.LogWarning($"[PositionPanelUI] speedText not assigned for {gameObject.name}");

        // heading as degrees (yaw) from heading vector
        Vector3 forward = t.heading;
        // protect against zero vector
        if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg; // convention: x right, z forward
        if (headingText != null)
            headingText.text = $"{yaw:F0}°";
        else
            Debug.LogWarning($"[PositionPanelUI] headingText not assigned for {gameObject.name}");
    }

    void SetMissing()
    {
        if (positionText != null) positionText.text = "No robot";
        if (velocityText != null) velocityText.text = "-";
        if (headingText != null) headingText.text = "-";
        if (speedText != null) speedText.text = "-";
    }
}
