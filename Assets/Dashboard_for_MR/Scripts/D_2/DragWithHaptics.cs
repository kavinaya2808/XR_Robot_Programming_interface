using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class DragWithHaptics : MonoBehaviour
{
    [Header("Movement (if you want the script to move the object instead of ObjectManipulator)")]
    public bool scriptControlsMovement = false; // leave false if you use ObjectManipulator to move the object
    public float followSmoothing = 10f;
    public bool constrainToPlaneY = true;
    public bool rememberPosition = false;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticPulseDuration = 0.02f;
    public float hapticPulseInterval = 0.03f;
    public bool hapticsWhileDragging = true;

    // runtime
    Rigidbody rb;
    bool isDragging = false;
    Vector3 targetPoint;
    float lastHapticTime = 0f;

    List<Behaviour> solverLikeComponents = new List<Behaviour>();
    InputDevice? activeDevice = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // cache solver-like components so we can disable them while dragging (heuristic)
        var comps = GetComponents<Behaviour>();
        foreach (var c in comps)
        {
            if (c == this) continue;
            string name = c.GetType().Name;
            if (name.Contains("Solver") || name.Contains("SolverHandler") || name.Contains("Follow") || name.Contains("Radial") || name.Contains("SurfaceMagnetism"))
                solverLikeComponents.Add(c);
        }
    }

    void Start()
    {
        if (rememberPosition && PlayerPrefs.HasKey($"{gameObject.name}_x"))
        {
            float x = PlayerPrefs.GetFloat($"{gameObject.name}_x");
            float y = PlayerPrefs.GetFloat($"{gameObject.name}_y");
            float z = PlayerPrefs.GetFloat($"{gameObject.name}_z");
            if (rb != null)
            {
                rb.position = new Vector3(x, y, z);
                rb.MovePosition(rb.position);
            }
            else transform.position = new Vector3(x, y, z);
        }
    }

    void FixedUpdate()
    {
        if (!isDragging) return;

        // if you want the script to control movement instead of ObjectManipulator:
        if (scriptControlsMovement)
        {
            Vector3 desired = targetPoint;
            if (constrainToPlaneY) desired.y = transform.position.y;
            Vector3 next = Vector3.Lerp(rb != null ? rb.position : transform.position, desired, Mathf.Clamp01(followSmoothing * Time.fixedDeltaTime));
            if (rb != null) rb.MovePosition(next);
            else transform.position = next;
        }

        // haptic pulses while dragging
        if (hapticsWhileDragging && Time.time - lastHapticTime >= hapticPulseInterval)
        {
            lastHapticTime = Time.time;
            TrySendHapticPulse(hapticAmplitude, hapticPulseDuration);
        }
    }

    // ---------- Inspector-callable wrappers (visible in ObjectManipulator events) ----------
    // Hook these to ObjectManipulator -> Manipulation Started / Manipulation Ended
    public void OnManipulatorStarted()
    {
        // If you would like an initial point, use transform.position
        BeginDrag(transform.position, null);
    }

    public void OnManipulatorEnded()
    {
        EndDrag(false);
    }

    // ---------- Original API (keeps backward compatibility) ----------
    public void BeginDrag(Vector3 hitPoint, XRNode? xrNode = null)
    {
        foreach (var s in solverLikeComponents) if (s != null) s.enabled = false;

        isDragging = true;
        targetPoint = hitPoint;
        lastHapticTime = 0f;

        if (xrNode.HasValue)
        {
            var node = xrNode.Value;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid) activeDevice = device;
            else activeDevice = null;
        }
        else activeDevice = null;

        TrySendHapticPulse(hapticAmplitude * 0.8f, 0.03f);
    }

    public void UpdateDrag(Vector3 hitPoint)
    {
        if (!isDragging) return;
        targetPoint = hitPoint;
    }

    public void EndDrag(bool savePosition = false)
    {
        isDragging = false;
        foreach (var s in solverLikeComponents) if (s != null) s.enabled = true;
        TrySendHapticPulse(hapticAmplitude * 0.6f, 0.05f);

        if (savePosition && rememberPosition)
        {
            Vector3 p = rb != null ? rb.position : transform.position;
            PlayerPrefs.SetFloat($"{gameObject.name}_x", p.x);
            PlayerPrefs.SetFloat($"{gameObject.name}_y", p.y);
            PlayerPrefs.SetFloat($"{gameObject.name}_z", p.z);
            PlayerPrefs.Save();
        }

        activeDevice = null;
    }

    // ---------- Haptics ----------
    void TrySendHapticPulse(float amplitude, float duration)
    {
        // if a specific device is available, use it
        if (activeDevice.HasValue)
        {
            var dev = activeDevice.Value;
            if (dev.isValid)
            {
                HapticCapabilities caps;
                if (dev.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
                {
                    dev.SendHapticImpulse(0u, amplitude, duration);
                    return;
                }
            }
        }

        // else send to all held controllers that support impulses
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (!d.isValid) continue;
            HapticCapabilities caps;
            if (d.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
                d.SendHapticImpulse(0, amplitude, duration);
        }
    }
}
