// ============================================================================
// XRDiagnosticReporter.cs - Runtime rig/passthrough/anchor diagnostics
// ============================================================================
// Drop this on any GameObject in a scene. It will log a concise health report
// about rigs, OVR managers, passthrough layers, and detected anchors so you can
// spot duplicate rigs or missing components quickly. Optional on-screen text
// helps when testing on-device.
// ============================================================================

using System;
using System.Text;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class XRDiagnosticReporter : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("Log the report to the console")] public bool logToConsole = true;
    [Tooltip("Show the report as overlay text")] public bool showOverlayText = true;
    [Tooltip("UI target; if null, one will be created")]
    public TextMeshProUGUI overlayText;

    [Header("Behavior")]
    [Tooltip("Seconds between automatic refreshes")] public float updateInterval = 1f;
    [Tooltip("Press this key to force refresh in Editor (New Input System)")] public UnityEngine.InputSystem.Key refreshKey = UnityEngine.InputSystem.Key.F9;
    [Tooltip("Include per-camera info (clear flags, background)")] public bool includeCameras = true;
    [Tooltip("Include anchor counts from Scene API")] public bool includeAnchors = true;

    private float _nextUpdate;
    private readonly StringBuilder _logBuilder = new StringBuilder(512);
    private readonly StringBuilder _uiBuilder = new StringBuilder(512);

    private void Start()
    {
        if (showOverlayText && overlayText == null)
        {
            CreateOverlay();
        }

        Refresh();
    }

    private void Update()
    {
        if (Time.time >= _nextUpdate)
        {
            _nextUpdate = Time.time + updateInterval;
            Refresh();
        }

#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (Application.isEditor && keyboard != null && refreshKey != UnityEngine.InputSystem.Key.None && Enum.IsDefined(typeof(UnityEngine.InputSystem.Key), refreshKey))
        {
            var keyCtrl = keyboard[refreshKey];
            if (keyCtrl != null && keyCtrl.wasPressedThisFrame)
            {
                Refresh();
            }
        }
#endif
    }

    private void Refresh()
    {
        _logBuilder.Length = 0;
        _uiBuilder.Length = 0;

        AppendHeader("XR Diagnostic Report");
        AppendOVRManagerSection();
        AppendRigSection();
        AppendPassthroughSection();
        AppendRoomSetupSection();

        if (includeCameras)
        {
            AppendCameraSection();
        }

        if (includeAnchors)
        {
            AppendAnchorSection();
        }

        string logText = _logBuilder.ToString();
        string uiText = _uiBuilder.ToString();

        if (logToConsole)
        {
            Debug.Log(logText);
        }

        if (showOverlayText && overlayText != null)
        {
            overlayText.text = uiText;
        }
    }

    private void AppendHeader(string title)
    {
        _logBuilder.AppendLine("========================================");
        _logBuilder.AppendLine(title);
        _logBuilder.AppendLine($"Time: {Time.time:F1}s");
        _logBuilder.AppendLine("========================================");

        _uiBuilder.AppendLine($"<b>{title}</b>");
        _uiBuilder.AppendLine($"Time: {Time.time:F1}s");
        _uiBuilder.AppendLine();
    }

    private void AppendOVRManagerSection()
    {
        var managers = FindObjectsOfType<OVRManager>();
        _logBuilder.AppendLine($"OVR Manager count: {managers.Length}");
        _uiBuilder.AppendLine($"OVR Manager: {ColorizeCount(managers.Length)}");

        foreach (var mgr in managers)
        {
            bool ptEnabled = mgr.isInsightPassthroughEnabled;
            _logBuilder.AppendLine($" - {GetPath(mgr.gameObject)} | PT: {ptEnabled} | Active: {mgr.isActiveAndEnabled}");
            _uiBuilder.AppendLine($" • {mgr.gameObject.name} (PT {(ptEnabled ? "ON" : "OFF")})");
        }

        _logBuilder.AppendLine();
        _uiBuilder.AppendLine();
    }

    private void AppendRigSection()
    {
        var rigs = FindObjectsOfType<OVRCameraRig>();
        _logBuilder.AppendLine($"OVR Camera Rigs: {rigs.Length}");
        _uiBuilder.AppendLine($"Rigs: {ColorizeCount(rigs.Length)}");

        foreach (var rig in rigs)
        {
            _logBuilder.AppendLine($" - {GetPath(rig.gameObject)} | Active: {rig.isActiveAndEnabled}");
            _uiBuilder.AppendLine($" • {rig.gameObject.name}");
        }

        _logBuilder.AppendLine();
        _uiBuilder.AppendLine();
    }

    private void AppendPassthroughSection()
    {
        var layers = FindObjectsOfType<OVRPassthroughLayer>();
        _logBuilder.AppendLine($"Passthrough Layers: {layers.Length}");
        _uiBuilder.AppendLine($"PT Layers: {ColorizeCount(layers.Length)}");

        foreach (var layer in layers)
        {
            _logBuilder.AppendLine($" - {GetPath(layer.gameObject)} | Hidden: {layer.hidden} | Opacity: {layer.textureOpacity:F2} | Overlay: {layer.overlayType}");
            _uiBuilder.AppendLine($" • {layer.gameObject.name} (hidden {layer.hidden}, {layer.textureOpacity:F2})");
        }

        _logBuilder.AppendLine();
        _uiBuilder.AppendLine();
    }

    private void AppendRoomSetupSection()
    {
        var room = FindObjectOfType<Quest3RoomSetup>();
        _logBuilder.AppendLine("Room Setup:");

        if (room == null)
        {
            _logBuilder.AppendLine(" - Quest3RoomSetup: MISSING");
            _uiBuilder.AppendLine("Room: <color=red>Missing Quest3RoomSetup</color>");
        }
        else
        {
            _logBuilder.AppendLine($" - Quest3RoomSetup on {GetPath(room.gameObject)} | Ready: {room.IsRoomReady} | FloorY: {room.FloorPosition.y:F2}");
            _uiBuilder.AppendLine($"Room: <color=green>OK</color> (Ready {(room.IsRoomReady ? "YES" : "NO")})");
        }

        _logBuilder.AppendLine();
        _uiBuilder.AppendLine();
    }

    private void AppendCameraSection()
    {
        var cameras = FindObjectsOfType<Camera>();
        _logBuilder.AppendLine($"Cameras: {cameras.Length}");
        _uiBuilder.AppendLine($"Cameras: {ColorizeCount(cameras.Length)}");

        foreach (var cam in cameras)
        {
            _logBuilder.AppendLine($" - {GetPath(cam.gameObject)} | Clear: {cam.clearFlags} | BG: {cam.backgroundColor} | Depth: {cam.depth}");
            _uiBuilder.AppendLine($" • {cam.gameObject.name} ({cam.clearFlags})");
        }

        _logBuilder.AppendLine();
        _uiBuilder.AppendLine();
    }

    private void AppendAnchorSection()
    {
        var sceneMgr = FindObjectOfType<OVRSceneManager>();
        var anchors = FindObjectsOfType<OVRSceneAnchor>();

        _logBuilder.AppendLine("Scene Anchors:");
        _logBuilder.AppendLine($" - OVRSceneManager: {(sceneMgr ? "Found" : "Missing")}");
        _logBuilder.AppendLine($" - Anchors detected: {anchors.Length}");

        _uiBuilder.AppendLine($"Scene API: {(sceneMgr ? "<color=green>Manager</color>" : "<color=red>No Manager</color>")}");
        _uiBuilder.AppendLine($"Anchors: {ColorizeCount(anchors.Length)}");
        _uiBuilder.AppendLine();
    }

    private void CreateOverlay()
    {
        var canvasGO = new GameObject("XRDiagCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var textGO = new GameObject("XRDiagText");
        textGO.transform.SetParent(canvasGO.transform, false);
        overlayText = textGO.AddComponent<TextMeshProUGUI>();
        overlayText.fontSize = 22;
        overlayText.color = Color.white;
        overlayText.alignment = TextAlignmentOptions.TopLeft;

        var rect = overlayText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(780f, 520f);
    }

    private string GetPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    private string ColorizeCount(int count)
    {
        if (count == 1) return "<color=green>1</color>";
        if (count == 0) return "<color=red>0</color>";
        return $"<color=yellow>{count}</color>";
    }
}
