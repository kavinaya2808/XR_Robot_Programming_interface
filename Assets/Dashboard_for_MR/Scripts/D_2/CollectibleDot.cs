using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleDot : MonoBehaviour
{
    [Tooltip("Robot ID that should collect this dot (e.g. R1_Red). If empty, any robot can collect.")]
    public string ownerRobotId = "";

    [Tooltip("Point value — usually 1 per dot.")]
    public int value = 1;

    [HideInInspector]
    public bool isCollected = false;

    void Reset()
    {
        // make sure collider is a trigger for simple overlap detection
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    /// <summary>
    /// Called when a robot collects this dot.
    /// Disables visual  collider and returns true if collected now.
    /// </summary>
    public bool CollectBy(RobotTelemetry collector)
    {
        if (isCollected) return false;

        // Owner check
        if (!string.IsNullOrEmpty(ownerRobotId) && collector != null && collector.robotId != ownerRobotId)
            return false;

        // Mark collected immediately
        isCollected = true;

        // Disable collider immediately to avoid further trigger callbacks
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Log collector + pos
        var byId = collector != null ? collector.robotId : "UNKNOWN";
        Debug.Log($"CollectibleDot: collected by {byId} id={GetInstanceID()} pos={transform.position}");

        // hide renderer so visible disappears in game, but object stays in Hierarchy for debugging
        var rend = GetComponent<Renderer>();
        if (rend != null)
            rend.enabled = false;

        gameObject.name = $"Dot_collected_by_{byId}_{GetInstanceID()}";

        return true;
    }

}
