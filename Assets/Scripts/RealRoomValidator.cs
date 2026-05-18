// ============================================================================
// RealRoomValidator.cs - Validates Real Room MR Setup
// ============================================================================
// 
// PURPOSE: 
// Checks that all components are properly configured for Quest 3 passthrough
// and room detection. Shows status on-screen for debugging.
//
// SETUP:
// 1. Add this script to any GameObject in 20_RealRoomMR scene
// 2. Build to Quest 3
// 3. Look at the on-screen debug info to see what's working/broken
//
// ============================================================================

using UnityEngine;
using TMPro;
using System.Text;

public class RealRoomValidator : MonoBehaviour
{
    [Header("UI (Optional)")]
    [Tooltip("Text to show validation results (creates one if null)")]
    public TextMeshProUGUI statusText;
    
    [Header("Settings")]
    public bool showOnScreenDebug = true;
    public float updateInterval = 1f;
    
    private float nextUpdateTime = 0f;
    private StringBuilder sb = new StringBuilder();
    
    // Cached references
    private OVRManager ovrManager;
    private OVRPassthroughLayer passthroughLayer;
    private Quest3RoomSetup roomSetup;
    private RealRoomMRManager mrManager;
    
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("[RealRoomValidator] Starting validation...");
        Debug.Log("========================================");
        
        // Find all components
        ovrManager = FindObjectOfType<OVRManager>();
        passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
        roomSetup = FindObjectOfType<Quest3RoomSetup>();
        mrManager = FindObjectOfType<RealRoomMRManager>();
        
        // Create on-screen debug text if needed
        if (showOnScreenDebug && statusText == null)
        {
            CreateDebugText();
        }
        
        // Run initial validation
        ValidateSetup();
    }
    
    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdateStatus();
        }
    }
    
    void ValidateSetup()
    {
        Debug.Log("[Validator] --- OVR Manager ---");
        if (ovrManager != null)
        {
            Debug.Log($"  OVR Manager found on: {ovrManager.gameObject.name}");
            Debug.Log($"  Passthrough Support: {OVRManager.instance?.isInsightPassthroughEnabled}");
        }
        else
        {
            Debug.LogError("[Validator] NO OVR MANAGER FOUND! Passthrough won't work.");
        }
        
        Debug.Log("[Validator] --- Passthrough Layer ---");
        if (passthroughLayer != null)
        {
            Debug.Log($"  Passthrough Layer found: {passthroughLayer.gameObject.name}");
            Debug.Log($"  Hidden: {passthroughLayer.hidden}");
            Debug.Log($"  Opacity: {passthroughLayer.textureOpacity}");
        }
        else
        {
            Debug.LogError("[Validator] NO PASSTHROUGH LAYER FOUND! Add OVRPassthroughLayer component.");
        }
        
        Debug.Log("[Validator] --- Quest3RoomSetup ---");
        if (roomSetup != null)
        {
            Debug.Log($"  Quest3RoomSetup found: {roomSetup.gameObject.name}");
            // Note: We can't easily check the enablePassthrough field from here without reflection
            // but the user should verify in Inspector
        }
        else
        {
            Debug.LogWarning("[Validator] No Quest3RoomSetup found. Room detection may not work.");
        }
        
        Debug.Log("[Validator] --- RealRoomMRManager ---");
        if (mrManager != null)
        {
            Debug.Log($"  RealRoomMRManager found: {mrManager.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[Validator] No RealRoomMRManager found.");
        }
        
        // Check for robots
        var robots = FindObjectsOfType<RosSharp.Control.AGVController>();
        Debug.Log($"[Validator] Found {robots.Length} robot(s) with AGVController");
        
        Debug.Log("========================================");
        Debug.Log("[RealRoomValidator] Validation complete");
        Debug.Log("========================================");
    }
    
    void UpdateStatus()
    {
        if (statusText == null) return;
        
        sb.Clear();
        sb.AppendLine("<b>== REAL ROOM MR STATUS ==</b>");
        sb.AppendLine();
        
        // Check passthrough
        bool passthroughOK = false;
        if (OVRManager.instance != null)
        {
            passthroughOK = OVRManager.instance.isInsightPassthroughEnabled;
            sb.AppendLine($"Passthrough: {(passthroughOK ? "<color=green>ENABLED</color>" : "<color=red>DISABLED</color>")}");
        }
        else
        {
            sb.AppendLine("Passthrough: <color=red>NO OVR MANAGER</color>");
        }
        
        // Check passthrough layer
        if (passthroughLayer != null)
        {
            sb.AppendLine($"PT Layer: <color=green>Found</color> ({(passthroughLayer.hidden ? "Hidden" : "Visible")})");
        }
        else
        {
            sb.AppendLine("PT Layer: <color=red>MISSING</color>");
        }
        
        // Check room setup
        if (roomSetup != null)
        {
            sb.AppendLine($"Room Setup: <color=green>Found</color>");
            sb.AppendLine($"  Room Ready: {(roomSetup.IsRoomReady ? "<color=green>YES</color>" : "<color=yellow>Waiting...</color>")}");
        }
        else
        {
            sb.AppendLine("Room Setup: <color=red>MISSING</color>");
        }
        
        // Check floor detection
        if (roomSetup != null && roomSetup.FloorPosition.y > float.NegativeInfinity)
        {
            sb.AppendLine($"Floor Y: <color=green>{roomSetup.FloorPosition.y:F2}m</color>");
        }
        else
        {
            sb.AppendLine("Floor: <color=yellow>Not detected</color>");
        }
        
        // Check robots
        var robots = FindObjectsOfType<RosSharp.Control.AGVController>();
        sb.AppendLine($"Robots: <color=green>{robots.Length}</color>");
        
        // XR status
#if UNITY_ANDROID
        sb.AppendLine($"Platform: <color=green>Android/Quest</color>");
#else
        sb.AppendLine($"Platform: <color=yellow>Editor</color>");
#endif
        
        statusText.text = sb.ToString();
    }
    
    void CreateDebugText()
    {
        // Create a Canvas
        var canvasGO = new GameObject("ValidatorCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        
        // Create Text
        var textGO = new GameObject("ValidatorText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        statusText = textGO.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 24;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        
        var rect = statusText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(600, 400);
        
        Debug.Log("[Validator] Created on-screen debug display");
    }
    
    // Call this from a button or keyboard to force passthrough enable
    public void ForceEnablePassthrough()
    {
        Debug.Log("[Validator] Attempting to force-enable passthrough...");
        
        if (OVRManager.instance != null)
        {
            // Try to enable passthrough
            if (!OVRManager.instance.isInsightPassthroughEnabled)
            {
                Debug.Log("[Validator] Passthrough was disabled, attempting to enable...");
                // This may not work at runtime, but worth trying
            }
        }
        
        if (passthroughLayer != null)
        {
            passthroughLayer.hidden = false;
            Debug.Log("[Validator] Passthrough layer unhidden");
        }
    }
}
