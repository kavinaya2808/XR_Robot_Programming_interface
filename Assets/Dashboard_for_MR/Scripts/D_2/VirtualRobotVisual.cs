using UnityEngine;

/// <summary>
/// Visual representation of a robot in MR space.
/// Position/rotation is updated from TelemetryReceiver.
/// This is just a visual - no physics, no ML-Agents.
/// </summary>
public class VirtualRobotVisual : MonoBehaviour
{
    [Header("Identity")]
    public string robotId = "Robot_1";
    
    [Header("Visual Settings")]
    [Tooltip("Speed of position smoothing (higher = faster)")]
    public float positionSmoothSpeed = 10f;
    
    [Tooltip("Speed of rotation smoothing (higher = faster)")]
    public float rotationSmoothSpeed = 10f;
    
    [Header("MR Placement")]
    [Tooltip("Offset from received position (for MR alignment)")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Scale of the robot visual")]
    public float visualScale = 1f;
    
    [Header("Status Indicators")]
    [Tooltip("Optional: Material to change color based on status")]
    public Renderer statusRenderer;
    
    [Tooltip("Color when idle")]
    public Color idleColor = Color.gray;
    
    [Tooltip("Color when moving")]
    public Color movingColor = Color.green;
    
    [Tooltip("Color when exploring")]
    public Color exploringColor = Color.cyan;
    
    // Target position/rotation (set by TelemetryReceiver)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private string currentStatus = "Idle";
    private bool hasReceivedData = false;
    
    void Start()
    {
        transform.localScale = Vector3.one * visualScale;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }
    
    void Update()
    {
        if (!hasReceivedData) return;
        
        // Smooth movement to target
        transform.position = Vector3.Lerp(transform.position, targetPosition + positionOffset, 
            Time.deltaTime * positionSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            Time.deltaTime * rotationSmoothSpeed);
        
        // Update status color
        if (statusRenderer != null)
        {
            Color targetColor = idleColor;
            if (currentStatus == "Moving") targetColor = movingColor;
            else if (currentStatus == "Exploring") targetColor = exploringColor;
            
            statusRenderer.material.color = Color.Lerp(
                statusRenderer.material.color, targetColor, Time.deltaTime * 5f);
        }
    }
    
    /// <summary>
    /// Called by TelemetryReceiver to update this robot's position
    /// </summary>
    public void UpdateFromTelemetry(Vector3 position, float rotationY, string status)
    {
        targetPosition = position;
        targetRotation = Quaternion.Euler(0, rotationY, 0);
        currentStatus = status;
        hasReceivedData = true;
    }
    
    /// <summary>
    /// Set MR alignment offset (for matching real-world position)
    /// </summary>
    public void SetMROffset(Vector3 offset)
    {
        positionOffset = offset;
    }
}

