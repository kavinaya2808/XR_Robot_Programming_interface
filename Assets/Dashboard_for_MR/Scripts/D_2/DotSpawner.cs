using UnityEngine;

public class DotSpawner : MonoBehaviour
{
    [Tooltip("Prefab with CollectibleDot script")]
    public GameObject dotPrefab;

    // Example: call SpawnForRobot(...) from inspector or another manager
    public void SpawnForRobot(string robotId, Vector3[] positions, Color dotColor = default, float timeLimitSeconds = 120f)
    {
        if (dotPrefab == null) { Debug.LogError("DotSpawner: dotPrefab not set."); return; }
        var robot = RobotTelemetry.Registry.ContainsKey(robotId) ? RobotTelemetry.Registry[robotId] : null;
        if (robot == null) Debug.LogWarning($"DotSpawner: robot {robotId} not found in Registry.");

        for (int i = 0; i < positions.Length; i++)
        {
            var go = Instantiate(dotPrefab, positions[i], Quaternion.identity, transform);
            var dot = go.GetComponent<CollectibleDot>();
            if (dot == null) dot = go.AddComponent<CollectibleDot>();
            dot.ownerRobotId = robotId;
            dot.value = 1;

            // set color if dot has renderer
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(rend.sharedMaterial);
                rend.material.color = dotColor;
            }
        }

        // after instantiating dots...
        if (robot != null)
        {
            // log positions length so you can verify correct spawn count
            Debug.Log($"DotSpawner: spawning {positions.Length} dots for robotId='{robotId}'");

            // Use forceRestart:true to ensure spawn intentionally resets the collect task to the number of spawned dots
            robot.StartCollectTask(totalDots: positions.Length, timeLimitSeconds: timeLimitSeconds, forceRestart: true);
        }

    }
}
