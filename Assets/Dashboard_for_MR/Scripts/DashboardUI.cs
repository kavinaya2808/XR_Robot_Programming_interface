using UnityEngine;
using TMPro;

public class DashboardUI : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI batteryText;

    public void UpdateTelemetry(TelemetryData data)
    {
        statusText.text = data.isActive ? "Status: Active" : "Status: Idle";
        positionText.text = $"Position:\nX: {data.position.x:F2}\nY: {data.position.y:F2}\nZ: {data.position.z:F2}";
        batteryText.text = $"Battery: {data.battery:F0}%";
    }
}
