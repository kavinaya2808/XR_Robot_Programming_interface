using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Attach this to the radar icon prefab. When the Radar creates the icon for a locatable,
/// Radar.OnLocatableAdded will instantiate the prefab and we will assign `Locatable` to it.
/// The updater rotates the arrow based on the locatable's heading (relative to Player) and
/// updates a speed label if available.
/// 
/// This version attempts multiple ways to read "speed" from the target:
///  - a property named "Speed" (case sensitive)
///  - a field named "speed" (case insensitive tries)
///  - a method named "GetSpeed" returning float
///  - Rigidbody.velocity.magnitude fallback
/// </summary>
public class LocatableIconUpdater : MonoBehaviour
{
    [HideInInspector] public Ilumisoft.RadarSystem.LocatableComponent Locatable; // assigned by Radar after CreateIcon()
    [HideInInspector] public Transform Player; // assigned by Radar (optional)
    [Tooltip("Optional: assign a component that provides speed (if you have one). The script will also attempt to autodetect speed sources on the Locatable GameObject.")]
    public Component speedSource; // optional assignable; will be used first if set

    RectTransform rt;
    RectTransform arrowRect; // the image that should be rotated
#if TMP_PRESENT
    TextMeshProUGUI speedTMP;
#endif
    Text speedTextLegacy;
    Transform targetTransform;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        // try to find an "Arrow" child; otherwise use root for rotation
        var arrowTransform = transform.Find("Arrow");
        if (arrowTransform != null && arrowTransform is RectTransform)
            arrowRect = arrowTransform as RectTransform;
        else
            arrowRect = rt;

        var speedChild = transform.Find("SpeedText");
        if (speedChild != null)
        {
#if TMP_PRESENT
            speedTMP = speedChild.GetComponent<TextMeshProUGUI>();
            if (speedTMP == null)
#endif
                speedTextLegacy = speedChild.GetComponent<Text>();
        }
    }

    void Start()
    {
        if (Player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) Player = pgo.transform;
        }
    }

    void Update()
    {
        if (Locatable == null) return;

        if (targetTransform == null) targetTransform = Locatable.transform;

        UpdateRotation();
        UpdateSpeedText();
    }

    void UpdateRotation()
    {
        if (Player != null)
        {
            Vector3 forward = targetTransform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f) forward = targetTransform.forward;

            Vector3 localForward = Quaternion.Inverse(Player.rotation) * forward;

            float angle = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;

            // If arrow sprite points up, -angle works; tweak +90/-90 if your sprite faces right/left.
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
        else
        {
            Vector3 localForward = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up).normalized;
            float angle = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }

    void UpdateSpeedText()
    {
        float speed = 0f;
        bool gotSpeed = TryGetSpeed(out speed);

        if (gotSpeed)
        {
#if TMP_PRESENT
            if (speedTMP != null) speedTMP.text = $"{speed:F2} m/s";
            else
#endif
            if (speedTextLegacy != null) speedTextLegacy.text = $"{speed:F2} m/s";
        }
    }

    /// <summary>
    /// Attempts to obtain a float speed value from multiple sources.
    /// Returns true if successful and sets outSpeed.
    /// </summary>
    bool TryGetSpeed(out float outSpeed)
    {
        outSpeed = 0f;

        // 1) If user explicitly assigned a speedSource component, try it first (reflection)
        if (speedSource != null)
        {
            if (TryReadSpeedFromComponent(speedSource, out outSpeed))
                return true;
        }

        // 2) Try to find well-known components on the locatable (RobotTelemetry, etc.) by reflection:
        var components = Locatable.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            if (comp == speedSource) continue; // already tried

            if (TryReadSpeedFromComponent(comp, out outSpeed))
                return true;
        }

        // 3) Fallback to Rigidbody if available
        var rb = Locatable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            outSpeed = rb.velocity.magnitude;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries reflection-based reads:
    /// - property "Speed" (public)
    /// - field "speed" or case variations
    /// - method "GetSpeed()" returning float
    /// Returns true if a float speed was read.
    /// </summary>
    bool TryReadSpeedFromComponent(Component comp, out float speed)
    {
        speed = 0f;
        if (comp == null) return false;

        Type t = comp.GetType();

        // 1) property named "Speed"
        try
        {
            var prop = t.GetProperty("Speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(float))
            {
                var val = prop.GetValue(comp);
                if (val is float f)
                {
                    speed = f;
                    return true;
                }
            }
        }
        catch { /* ignore reflection errors */ }

        // 2) field named "speed" or "Speed"
        try
        {
            var field = t.GetField("speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? t.GetField("Speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(float))
            {
                var val = field.GetValue(comp);
                if (val is float f)
                {
                    speed = f;
                    return true;
                }
            }
        }
        catch { /* ignore reflection errors */ }

        // 3) method "GetSpeed()" returning float
        try
        {
            var method = t.GetMethod("GetSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(float))
            {
                var val = method.Invoke(comp, null);
                if (val is float f)
                {
                    speed = f;
                    return true;
                }
            }
        }
        catch { /* ignore reflection errors */ }

        return false;
    }
}
