using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RobotTelemetry))]
public class DotCollector : MonoBehaviour
{
    RobotTelemetry telemetry;

    // defensive: remember dot instanceIDs we've already counted (cleans up automatically)
    HashSet<int> countedDotInstanceIds = new HashSet<int>();

    void Awake()
    {
        telemetry = GetComponent<RobotTelemetry>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        var dot = other.GetComponent<CollectibleDot>();
        if (dot == null) return;

        int id = dot.GetInstanceID();

        // If we already counted this dot, ignore
        if (countedDotInstanceIds.Contains(id))
        {
            Debug.Log($"DotCollector: already counted dot id={id} on {telemetry.robotId}, ignoring duplicate trigger.");
            return;
        }

        // attempt collect (this marks the dot as collected and disables its collider/renderer)
        if (dot.CollectBy(telemetry))
        {
            // Mark it as counted right away so even if another trigger fires we won't double-count
            countedDotInstanceIds.Add(id);

            // only notify telemetry if the robot is on the collect task
            if (telemetry.activeTask == "Collect Dots" && telemetry.collectTotalDots > 0)
            {
                telemetry.OnDotCollected(dot.value);
            }
            else
            {
                Debug.LogWarning($"DotCollector: dot touched by {telemetry.robotId} but no active collect task; ignored.");
            }
        }
    }
}
