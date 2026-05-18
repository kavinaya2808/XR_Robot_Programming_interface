using UnityEngine;

/// <summary>
/// Rotates a UI needle (RectTransform) according to robot speed (m/s).
/// Attach to a controller object and assign the needle RectTransform.
/// </summary>
public class SpeedometerController : MonoBehaviour
{
    [Header("UI")]
    public RectTransform needleRect; // assign NeedleImage.RectTransform

    [Header("Dial mapping (m/s)")]
    public float maxSpeed = 60f;     // example max in m/s (set to match your robot)
    public float startAngle = -120f; // angle at 0 m/s
    public float endAngle = 120f;    // angle at maxSpeed

    [Header("Smoothing")]
    public float smoothSpeed = 360f; // degrees per second

    [Header("Robot source")]
    public Rigidbody robotRigidbody;    // optional (meters/second)
    public RobotTelemetry robotTelemetry; // optional: custom telemetry in m/s

    float currentAngle = 0f;

    void Start()
    {
        if (needleRect == null)
        {
            Debug.LogError("[SpeedometerController] needleRect not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        currentAngle = needleRect.localEulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
    }

    void Update()
    {
        float speed = GetRobotSpeed(); // expected in m/s
        float t = Mathf.Clamp01(speed / Mathf.Max(0.0001f, maxSpeed));
        float targetAngle = Mathf.Lerp(startAngle, endAngle, t);

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
        needleRect.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }

    float GetRobotSpeed()
    {
        if (robotTelemetry != null) return robotTelemetry.GetSpeed(); // should return m/s
        if (robotRigidbody != null) return robotRigidbody.velocity.magnitude; // m/s
        return 0f;
    }
}
