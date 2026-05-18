using System.Collections;
using UnityEngine;
using UnityEngine.XR;

#if XRITK_PRESENT
using UnityEngine.XR.Interaction.Toolkit;
#endif

[RequireComponent(typeof(DragWithHaptics))]
public class MRTK3GrabBridge : MonoBehaviour
{
    [Header("Optional (auto-found if empty)")]
    [Tooltip("If left null the script will try to find an XR interactable (XRBaseInteractable) or rely on ObjectManipulator inspector events.")]
#if XRITK_PRESENT
    public XRBaseInteractable xriInteractable;
#endif


    public XRNode hapticNode = XRNode.RightHand;

    DragWithHaptics dragWithHaptics;

    // runtime state
    bool isDragging = false;
    Transform currentAttachTransform = null;

#if XRITK_PRESENT
    // store for unsubscribing
    private SelectEnterEventArgs lastEnterArgs;
#endif

    void Awake()
    {
        dragWithHaptics = GetComponent<DragWithHaptics>();
    }

    void Start()
    {
#if XRITK_PRESENT
        // If inspector didn't assign, try to get one on same object
        if (xriInteractable == null)
            xriInteractable = GetComponent<XRBaseInteractable>();

        if (xriInteractable != null)
        {
            xriInteractable.selectEntered.AddListener(OnSelectEntered);
            xriInteractable.selectExited.AddListener(OnSelectExited);
        }
        else
        {
            Debug.Log("[MRTK3GrabBridge] XRBaseInteractable not found (or XRITK_PRESENT not defined). Use ObjectManipulator inspector events or add XRBaseInteractable.");
        }
#else
        Debug.Log("[MRTK3GrabBridge] XR Interaction Toolkit support not compiled in. Use ObjectManipulator inspector events or define XRITK_PRESENT.");
#endif
    }

    // -----------------------
    // Methods for ObjectManipulator inspector wiring (parameterless)
    // -----------------------
    // Use these in the ObjectManipulator UnityEvents dropdown:
    //   OnManipulationStarted -> MRTK3GrabBridge.OnManipulationStarted
    //   OnManipulationUpdated -> MRTK3GrabBridge.OnManipulationUpdated
    //   OnManipulationEnded -> MRTK3GrabBridge.OnManipulationEnded

    /// <summary>
    /// Call this from ObjectManipulator -> OnManipulationStarted UnityEvent (parameterless)
    /// </summary>
    public void OnManipulationStarted()
    {
        // disable any internal manipulator if it's present (we don't assume type here)
        // start dragging using the current transform position as sensible initial point
        Vector3 startPoint = transform.position;

        // Try to pass XRNode for haptics (best-effort)
        dragWithHaptics.BeginDrag(startPoint, hapticNode);
        isDragging = true;
        currentAttachTransform = transform; // follow transform position as source of target updates
        StartCoroutine(ManipulationUpdater());
    }

    /// <summary>
    /// Call this from ObjectManipulator -> OnManipulationUpdated UnityEvent (parameterless)
    /// </summary>
    public void OnManipulationUpdated()
    {
        // This method is intentionally simple — it runs every manipulator update via UnityEvent.
        // UpdateDrag will be fed by the ManipulationUpdater coroutine which uses transform.position
        // so we don't need to do anything here. We keep the method so it appears in the dropdown.
    }

    /// <summary>
    /// Call this from ObjectManipulator -> OnManipulationEnded UnityEvent (parameterless)
    /// </summary>
    public void OnManipulationEnded()
    {
        dragWithHaptics.EndDrag(true);
        isDragging = false;
        currentAttachTransform = null;
    }

    IEnumerator ManipulationUpdater()
    {
        // Runs while ObjectManipulator-initiated drag is active and forwards transform.position each frame
        while (isDragging && currentAttachTransform != null)
        {
            dragWithHaptics.UpdateDrag(currentAttachTransform.position);
            yield return null;
        }
    }

    // -----------------------
    // XR Interaction Toolkit handlers (if available at compile time)
    // -----------------------
#if XRITK_PRESENT
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Try to extract a useful attach transform / interactor transform
        currentAttachTransform = null;

        // args.interactorObject often has a transform; use that if available
        if (args.interactorObject?.transform != null)
        {
            currentAttachTransform = args.interactorObject.transform;
        }
        else
        {
            // XR Interactor may provide an attach transform for this interactable
            try
            {
                var attach = args.interactorObject?.GetAttachTransform(this.transform);
                if (attach != null)
                    currentAttachTransform = attach;
            }
            catch { /* defensive: GetAttachTransform may not exist on older IXR types */ }
        }

        // fallback use the object's position
        if (currentAttachTransform == null)
            currentAttachTransform = this.transform;

        // Begin drag using the attach transform position
        dragWithHaptics.BeginDrag(currentAttachTransform.position, DetectXRNodeFromInteractor(args.interactorObject));
        isDragging = true;
        StartCoroutine(XRDragUpdater());
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isDragging = false;
        dragWithHaptics.EndDrag(true);
        currentAttachTransform = null;
    }

    IEnumerator XRDragUpdater()
    {
        while (isDragging)
        {
            if (currentAttachTransform != null)
                dragWithHaptics.UpdateDrag(currentAttachTransform.position);
            yield return null;
        }
    }

    XRNode? DetectXRNodeFromInteractor(IXRSelectInteractor interactor)
    {
        // Best-effort: derive XRNode handedness from interactor's GameObject name/components
        if (interactor == null) return hapticNode;

        var go = (interactor as UnityEngine.Object)?.GetType() != null ? (interactor as UnityEngine.Object).gameObject : null;
        if (go != null)
        {
            string n = go.name.ToLowerInvariant();
            if (n.Contains("left")) return XRNode.LeftHand;
            if (n.Contains("right")) return XRNode.RightHand;
        }

        return hapticNode;
    }
#endif

    // -----------------------
    // Cleanup
    // -----------------------
    void OnDestroy()
    {
#if XRITK_PRESENT
        if (xriInteractable != null)
        {
            xriInteractable.selectEntered.RemoveListener(OnSelectEntered);
            xriInteractable.selectExited.RemoveListener(OnSelectExited);
        }
#endif
    }
}
