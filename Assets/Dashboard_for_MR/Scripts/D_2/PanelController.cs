using UnityEngine;

/// <summary>
/// Controls a single slate/panel: open/close, facing camera, manipulation state.
/// </summary>
public class PanelController : MonoBehaviour
{
    public enum FrontFacing { Forward, Backward }

    [Tooltip("Unique ID for this panel (must match Taskbar button exactly).")]
    public string panelId = "Position";

    [Tooltip("Root GameObject for the panel (the Slate). We will SetActive on this.")]
    public GameObject panelRoot;

    [Tooltip("Reference to layout manager in scene (optional, will auto-find).")]
    public PanelLayoutManager layoutManager;

    [Tooltip("Width of this panel in meters (used by layout manager).")]
    public float panelWidthMeters = 0.55f;

    [Tooltip("Should panel auto-face camera while open?")]
    public bool autoFaceCamera = true;

    [Tooltip("Rotation smoothing speed when facing camera.")]
    public float faceLerp = 14f;

    [Tooltip("Which local axis of the panel is the visible 'front' (Forward = +Z, Backward = -Z).")]
    public FrontFacing frontFacing = FrontFacing.Forward;

    [HideInInspector]
    public bool IsBeingManipulated = false;

    private Camera mainCam;
    private bool isOpen = false;

    void Awake()
    {
        if (panelRoot == null) panelRoot = this.gameObject;
        if (layoutManager == null) layoutManager = FindObjectOfType<PanelLayoutManager>();
        mainCam = Camera.main;

        // Source of truth: panelRoot.activeSelf
        isOpen = (panelRoot != null) && panelRoot.activeSelf;

        // Ensure closed if not open
        if (panelRoot != null && !isOpen)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        // Make the panel face the camera smoothly while open
        if (!isOpen || panelRoot == null || !autoFaceCamera) return;

        if (mainCam != null)
        {
            Vector3 dirToCam = (mainCam.transform.position - panelRoot.transform.position).normalized;
            if (dirToCam.sqrMagnitude <= 0.001f) return;

            Quaternion look = Quaternion.LookRotation(dirToCam, Vector3.up);

            // if the visible front is actually backward, rotate 180 degrees around Y to flip
            if (frontFacing == FrontFacing.Backward)
                look *= Quaternion.Euler(0f, 180f, 0f);

            panelRoot.transform.rotation = Quaternion.Slerp(panelRoot.transform.rotation, look, Time.deltaTime * faceLerp);
        }
    }

    /// <summary>Toggle panel open/close using the actual active state as truth.</summary>
    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning($"TogglePanel: panelRoot null for id {panelId}");
            return;
        }

        bool currentlyActive = panelRoot.activeSelf;
        if (currentlyActive) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) return;

        panelRoot.SetActive(true);
        isOpen = true;

        // register with layout manager
        if (layoutManager == null) layoutManager = FindObjectOfType<PanelLayoutManager>();
        layoutManager?.RegisterOpenPanel(this);

        // immediately face camera correctly
        FaceCameraImmediate();
    }

    public void ClosePanel()
    {
        if (panelRoot == null) return;
        if (!panelRoot.activeSelf) return;

        panelRoot.SetActive(false);
        isOpen = false;
        layoutManager?.UnregisterPanel(this);
    }

    /// <summary>Hook to slate's own close button.</summary>
    public void CloseFromSlate()
    {
        ClosePanel();
    }

    public void OnManipulationStarted()
    {
        IsBeingManipulated = true;
    }

    public void OnManipulationEnded()
    {
        IsBeingManipulated = false;
        // let layout manager reflow other panels
        layoutManager?.ForceRelayout();
    }

    private void FaceCameraImmediate()
    {
        if (!autoFaceCamera || panelRoot == null || Camera.main == null) return;

        Vector3 dirToCam = (Camera.main.transform.position - panelRoot.transform.position).normalized;
        if (dirToCam.sqrMagnitude <= 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dirToCam, Vector3.up);
        if (frontFacing == FrontFacing.Backward)
            look *= Quaternion.Euler(0f, 180f, 0f);

        panelRoot.transform.rotation = look;
    }
    // --- inside your PanelController.cs ---
    // add these two methods inside the class (below existing methods)

    void OnEnable()
    {
        // If the GameObject is active because Unity enabled it (or the prefab started active),
        // ensure we register with the layout manager and set isOpen accordingly.
        if (panelRoot != null && panelRoot.activeSelf)
        {
            isOpen = true;
            if (layoutManager == null) layoutManager = FindObjectOfType<PanelLayoutManager>();
            layoutManager?.RegisterOpenPanel(this);
        }
    }

    void OnDisable()
    {
        // If the panel root gets disabled unexpectedly (for example by the slate close),
        // make sure we mark it closed and unregister so the taskbar can reopen it.
        if (isOpen)
        {
            isOpen = false;
            layoutManager?.UnregisterPanel(this);
        }
    }

    /// <summary>
    /// Force open — public API to open panel even if prefab's internal close toggles
    /// different child objects. This ensures the panelRoot is activated and registered.
    /// </summary>
    public void ForceOpen()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        isOpen = true;
        if (layoutManager == null) layoutManager = FindObjectOfType<PanelLayoutManager>();
        layoutManager?.RegisterOpenPanel(this);
        FaceCameraImmediate();
    }

}

