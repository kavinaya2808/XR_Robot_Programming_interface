using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DraggablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Camera uiCamera;
    private Vector3 offset;
    private bool dragging = false;
    private Plane dragPlane;

    void Start()
    {
        // find the main camera (XR camera)
        uiCamera = Camera.main;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        // build a plane where the drag will happen
        dragPlane = new Plane(uiCamera.transform.forward * -1f, transform.position);
        Ray ray = uiCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            offset = transform.position - hitPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        Ray ray = uiCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.position = hitPoint + offset;
            // keep facing camera
            transform.rotation = Quaternion.LookRotation(transform.position - uiCamera.transform.position, Vector3.up);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
    }
}
