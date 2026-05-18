using System.Collections.Generic;
using UnityEngine;

public class PanelLayoutManager : MonoBehaviour
{
    [Tooltip("Anchor transform (e.g., Taskbar).")]
    public Transform anchorTransform;

    [Tooltip("Local direction from anchor to place next panel.")]
    public Vector3 rowDirection = Vector3.right;

    [Tooltip("Spacing between panels in meters.")]
    public float panelSpacing = 0.06f;

    [Tooltip("Forward offset from anchor.")]
    public float forwardOffset = 0.0f;

    private readonly List<PanelController> openPanels = new List<PanelController>();

    public void RegisterOpenPanel(PanelController pc)
    {
        if (pc == null) return;
        if (!openPanels.Contains(pc)) openPanels.Add(pc);
        LayoutOpenPanels();
    }

    public void UnregisterPanel(PanelController pc)
    {
        if (pc == null) return;
        if (openPanels.Remove(pc)) LayoutOpenPanels();
    }

    public void LayoutOpenPanels()
    {
        if (anchorTransform == null) return;

        Vector3 basePos = anchorTransform.position + anchorTransform.forward * forwardOffset;
        Quaternion baseRot = anchorTransform.rotation;

        float offsetMeters = 0f;
        for (int i = 0; i < openPanels.Count; i++)
        {
            var p = openPanels[i];
            if (p == null) continue;

            // If user is manipulating, don't relocate; still advance offset by its width so others don't overlap.
            if (p.IsBeingManipulated)
            {
                offsetMeters += p.panelWidthMeters + panelSpacing;
                continue;
            }

            Vector3 localOffset = rowDirection.normalized * offsetMeters;
            Vector3 worldOffset = baseRot * localOffset;
            Vector3 targetPos = basePos + worldOffset;

            p.transform.position = targetPos;

            // rotate to face camera with same facing rule as PanelController
            if (Camera.main != null)
            {
                Vector3 dir = (Camera.main.transform.position - p.transform.position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                    if (p.frontFacing == PanelController.FrontFacing.Backward)
                        look *= Quaternion.Euler(0f, 180f, 0f);
                    p.transform.rotation = look;
                }
            }

            offsetMeters += p.panelWidthMeters + panelSpacing;
        }
    }

    public void ForceRelayout()
    {
        LayoutOpenPanels();
    }
}
