using UnityEngine;
using System.Collections.Generic;

public enum PanelType { Position, Battery, RobotStatus, TaskProgress }

public class DockManager : MonoBehaviour
{
    [Header("Panel Prefabs")]
    public GameObject positionPanelPrefab;
    public GameObject batteryPanelPrefab;
    public GameObject robotStatusPanelPrefab;
    public GameObject taskProgressPanelPrefab;

    [Header("Spawn settings")]
    public Transform defaultSpawnAnchor; // e.g. camera forward at 1.2m
    public float spawnDistance = 1.2f;

    // live instances
    private Dictionary<PanelType, GameObject> instances = new Dictionary<PanelType, GameObject>();

    void Start()
    {
        // ensure dictionary keys exist
        foreach (PanelType t in System.Enum.GetValues(typeof(PanelType)))
            instances[t] = null;
    }

    Vector3 GetSpawnPosition()
    {
        Transform cam = Camera.main.transform;
        return cam.position + cam.forward * spawnDistance;
    }

    public void TogglePanel(int panelTypeInt)
    {
        TogglePanel((PanelType)panelTypeInt);
    }

    public void TogglePanel(PanelType type)
    {
        if (instances[type] == null)
        {
            SpawnPanel(type);
        }
        else
        {
            bool isActive = instances[type].activeSelf;
            instances[type].SetActive(!isActive);
        }
    }

    public void SpawnPanel(PanelType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case PanelType.Position: prefab = positionPanelPrefab; break;
            case PanelType.Battery: prefab = batteryPanelPrefab; break;
            case PanelType.RobotStatus: prefab = robotStatusPanelPrefab; break;
            case PanelType.TaskProgress: prefab = taskProgressPanelPrefab; break;
        }
        if (prefab == null) { Debug.LogWarning("Prefab missing for " + type); return; }

        Vector3 pos = GetSpawnPosition();
        Quaternion rot = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
        GameObject instance = Instantiate(prefab, pos, rot);
        instances[type] = instance;

        // optionally set a starting offset so panels don't spawn exactly overlapping
        instance.transform.position += Vector3.right * (0.3f * (int)type);

        // set panel controller data (if needed)
        var pc = instance.GetComponent<PanelController1>();
        if (pc != null) pc.Initialize(type);

        // Add rigidbody/collider/XRGrabInteractable if not present (some prefabs might already include them)
    }

    public void ExpandAll()
    {
        // spawn or show all
        foreach (PanelType t in System.Enum.GetValues(typeof(PanelType)))
            TogglePanel(t);
    }

    public void CloseAll()
    {
        foreach (var kv in instances)
            if (kv.Value != null) kv.Value.SetActive(false);
    }
}
