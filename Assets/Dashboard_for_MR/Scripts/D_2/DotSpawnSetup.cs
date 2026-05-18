using UnityEngine;

/// <summary>
/// Small helper to spawn two teams of 4 dots each for Robot_1 (red) and Robot_2 (green).
/// Drop on an empty GameObject and assign the DotSpawner in the Inspector (or it will FindOne).
/// Use the context menu "SpawnDotsNow" in the component or set autoSpawnOnStart = true.
/// </summary>
public class DotSpawnSetup : MonoBehaviour
{
    public DotSpawner spawner;             // assign in inspector or leave blank to auto-find
    public bool autoSpawnOnStart = true;

    [Tooltip("Y position used for all dots (match your cube's height)")]
    public float dotY = 0.05f;

    // sample positions relative to world origin; adjust to fit your scene
    Vector3[] redPositions => new Vector3[] {
        new Vector3( 1.0f, dotY,  1.0f),
        new Vector3( 1.5f, dotY,  1.8f),
        new Vector3( 0.6f, dotY,  2.0f),
        new Vector3( 0.9f, dotY,  1.3f)
    };

    Vector3[] greenPositions => new Vector3[] {
        new Vector3(-1.0f, dotY, -1.0f),
        new Vector3(-1.5f, dotY, -1.8f),
        new Vector3(-0.6f, dotY, -2.0f),
        new Vector3(-0.9f, dotY, -1.3f)
    };

    void Start()
    {
        if (autoSpawnOnStart)
            SpawnDotsNow();
    }

    [ContextMenu("SpawnDotsNow")]
    public void SpawnDotsNow()
    {
        if (spawner == null)
        {
            spawner = FindObjectOfType<DotSpawner>();
            if (spawner == null)
            {
                Debug.LogError("DotSpawnSetup: No DotSpawner found in the scene. Add DotSpawner or assign it here.");
                return;
            }
        }

        // Robot IDs used in your project:
        const string redRobotId = "Robot_1";
        const string greenRobotId = "Robot_2";

        // time limit 120s (2 minutes)
        float timeLimit = 120f;

        // spawn red team
        spawner.SpawnForRobot(redRobotId, redPositions, Color.red, timeLimit);

        // spawn green team
        spawner.SpawnForRobot(greenRobotId, greenPositions, Color.green, timeLimit);

        Debug.Log("Spawned red and green dots for " + redRobotId + " and " + greenRobotId);
    }
}
