using UnityEngine;
using TMPro;

public class TelemetryDashboard : MonoBehaviour
{
    public Transform robot;             // assign your robot cube
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI batteryText;

    private float battery = 100f;
    private Vector3 lastPos;

    void Start()
    {
        lastPos = robot.position;
    }

    void Update()
    {
        Vector3 pos = robot.position;

        // Check if robot moved
        bool isMoving = pos != lastPos;
        statusText.text = "Status: " + (isMoving ? "Active" : "Idle");

        // Update position
        positionText.text = $"Position: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}";

        // Simulate battery drain
        if (isMoving)
            battery = Mathf.Max(0, battery - Time.deltaTime * 0.5f);

        batteryText.text = $"Battery: {battery:F1}%";

        lastPos = pos;
    }
}
