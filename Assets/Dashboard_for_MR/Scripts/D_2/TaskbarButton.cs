using UnityEngine;

/// <summary>
/// Attach this to the same GameObject as each taskbar button (or to a helper object).
/// Wire the MRTK button OnClicked/OnPressed UnityEvent to call OnPressed() below.
/// </summary>
public class TaskbarButton : MonoBehaviour
{
    [Tooltip("Panel ID to toggle (must match PanelController.panelId).")]
    public string panelId;

    [Tooltip("Optional explicit reference to the panel's PanelController. If left null, it will be auto-found.")]
    public PanelController panelController;

    void Start()
    {
        if (panelController == null)
        {
            var all = FindObjectsOfType<PanelController>();
            foreach (var p in all)
            {
                if (p.panelId == panelId)
                {
                    panelController = p;
                    break;
                }
            }
        }
    }

    /// <summary>Hook this method to your MRTK button's OnClicked / OnPressed event.</summary>
    public void OnPressed()
    {
        if (panelController == null)
        {
            Debug.LogWarning($"TaskbarButton: PanelController not found for id {panelId}");
            return;
        }

        panelController.TogglePanel();
    }
}
