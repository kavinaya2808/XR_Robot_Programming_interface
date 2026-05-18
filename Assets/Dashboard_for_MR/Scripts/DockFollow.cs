using UnityEngine;

[DisallowMultipleComponent]
public class DockFollow : MonoBehaviour
{
    [Tooltip("Assign the player's camera (XR Rig camera).")]
    public Transform targetCamera;

    [Tooltip("Local offset from camera (e.g., downwards).")]
    public Vector3 localPosition = new Vector3(0f, -0.35f, 0.6f);

    [Tooltip("Should the dock rotate to face the camera?")]
    public bool faceCamera = true;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Keep a fixed local position relative to camera
        transform.position = targetCamera.TransformPoint(localPosition);

        if (faceCamera)
        {
            // Make sure dock rotation faces the camera but keep its up vector aligned with world up
            Vector3 dir = transform.position - targetCamera.position;
            dir.y = 0; // optional: prevent tilting with head pitch
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
