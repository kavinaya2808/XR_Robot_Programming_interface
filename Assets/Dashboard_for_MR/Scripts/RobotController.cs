using UnityEngine;
using System;

public class RobotController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float batteryDrainRate = 2f; // % per second when moving
    public event Action<TelemetryData> OnTelemetryUpdated;

    private float batteryLevel = 100f;
    private Vector3 lastPosition;
    private bool isActive = false;

    private void Start()
    {
        lastPosition = transform.position;
        InvokeRepeating(nameof(SendTelemetry), 0f, 0.1f); // send 10 times per second
    }

    private void Update()
    {
        float h = Input.GetAxis("Horizontal"); // left/right arrows or A/D
        float v = Input.GetAxis("Vertical");   // up/down arrows or W/S

        Vector3 movement = new Vector3(h, 0, v);
        isActive = movement.magnitude > 0.01f;

        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

        if (isActive)
            batteryLevel = Mathf.Max(0f, batteryLevel - batteryDrainRate * Time.deltaTime);
    }

    private void SendTelemetry()
    {
        var t = new TelemetryData
        {
            position = transform.position,
            battery = batteryLevel,
            isActive = isActive
        };

        OnTelemetryUpdated?.Invoke(t);
    }
}

[Serializable]
public struct TelemetryData
{
    public Vector3 position;
    public float battery;
    public bool isActive;
}
