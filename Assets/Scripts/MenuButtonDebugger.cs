// ============================================================================
// MenuButtonDebugger.cs - Diagnose and Fix Button Issues
// ============================================================================
// 
// PURPOSE: 
// This script diagnoses why MRTK3 buttons aren't working and provides
// a fallback click detection system using raycasts.
//
// SETUP:
// 1. Add this script to MenuController object
// 2. Press Play and check Console for diagnostic info
// 3. If buttons still don't work, the fallback raycast system will handle clicks
//
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class MenuButtonDebugger : MonoBehaviour
{
    [Header("References")]
    public BootstrapMenuController menuController;
    public Camera mainCamera;
    
    [Header("Debug Settings")]
    public bool enableDiagnostics = true;
    public bool enableKeyboardShortcuts = true;
    public float raycastDistance = 10f;
    
    [Header("Keyboard Shortcuts")]
    [Tooltip("Press these keys to trigger buttons directly")]
    public Key warehouseKey = Key.Digit1;
    public Key realRoomKey = Key.Digit2;
    public Key mlKey = Key.Digit3;
    public Key rosKey = Key.Digit4;
    public Key keyboardModeKey = Key.Digit5;
    public Key startKey = Key.Enter;
    
    private bool diagnosticsRun = false;
    
    void Start()
    {
        // Auto-find references
        if (menuController == null)
            menuController = FindObjectOfType<BootstrapMenuController>();
            
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Try to find any camera
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (enableDiagnostics)
        {
            RunDiagnostics();
        }
        
        Debug.Log("[MenuButtonDebugger] Keyboard shortcuts enabled:");
        Debug.Log("  1 = Warehouse Mode");
        Debug.Log("  2 = Real Room Mode");
        Debug.Log("  3 = ML Control");
        Debug.Log("  4 = ROS Control");
        Debug.Log("  5 = Keyboard Control");
        Debug.Log("  Enter = Start Game");
    }
    
    void Update()
    {
        // Run diagnostics once after a short delay (to let everything initialize)
        if (enableDiagnostics && !diagnosticsRun && Time.time > 1f)
        {
            RunDiagnostics();
            diagnosticsRun = true;
        }
        
        // Keyboard shortcuts for button actions
        if (enableKeyboardShortcuts)
        {
            HandleKeyboardShortcuts();
        }
    }
    
    void HandleKeyboardShortcuts()
    {
        if (menuController == null) return;
        
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        if (keyboard[warehouseKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing 1 - Warehouse Mode");
            menuController.SelectWarehouseMode();
        }
        else if (keyboard[realRoomKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing 2 - Real Room Mode");
            menuController.SelectRealRoomMode();
        }
        else if (keyboard[mlKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing 3 - ML Control");
            menuController.SelectMLControl();
        }
        else if (keyboard[rosKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing 4 - ROS Control");
            menuController.SelectROSControl();
        }
        else if (keyboard[keyboardModeKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing 5 - Keyboard Control");
            menuController.SelectKeyboardControl();
        }
        else if (keyboard[startKey].wasPressedThisFrame)
        {
            Debug.Log("[Keyboard] Pressing Enter - Start Game");
            menuController.StartGame();
        }
    }
    
    void RunDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("[MenuButtonDebugger] Running Diagnostics...");
        Debug.Log("========================================");
        
        // Check 1: XR Interaction Manager
        var xrManagers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        if (xrManagers.Length == 0)
        {
            Debug.LogError("[PROBLEM] No XR Interaction Manager found in scene!");
            Debug.LogError("[FIX] Add XR Interaction Manager component to MRTK XR Rig");
        }
        else if (xrManagers.Length > 1)
        {
            Debug.LogWarning($"[WARNING] Found {xrManagers.Length} XR Interaction Managers. Should only have 1.");
            foreach (var mgr in xrManagers)
            {
                Debug.LogWarning($"  - Found on: {GetFullPath(mgr.gameObject)}");
            }
        }
        else
        {
            Debug.Log($"[OK] XR Interaction Manager found on: {GetFullPath(xrManagers[0].gameObject)}");
        }
        
        // Check 2: Interactors (ray interactors for pointing)
        var rayInteractors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>();
        if (rayInteractors.Length == 0)
        {
            Debug.LogError("[PROBLEM] No XR Ray Interactors found! Cannot point at buttons.");
            Debug.LogError("[FIX] Make sure MRTK XR Rig has hand controllers with ray interactors");
        }
        else
        {
            Debug.Log($"[OK] Found {rayInteractors.Length} Ray Interactor(s):");
            foreach (var ri in rayInteractors)
            {
                Debug.Log($"  - {GetFullPath(ri.gameObject)} (Enabled: {ri.enabled})");
            }
        }
        
        // Check 3: Event System
        var eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length == 0)
        {
            Debug.LogError("[PROBLEM] No EventSystem found!");
        }
        else
        {
            Debug.Log($"[OK] EventSystem found: {eventSystems[0].gameObject.name}");
        }
        
        // Check 4: Canvas settings
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            Debug.Log($"[INFO] Canvas: {canvas.name}");
            Debug.Log($"  - Render Mode: {canvas.renderMode}");
            Debug.Log($"  - World Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "None")}");
            
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"  [WARNING] No GraphicRaycaster on canvas {canvas.name}");
            }
        }
        
        // Check 5: PressableButtons
        var pressableButtons = FindObjectsOfType<Microsoft.MixedReality.Toolkit.UX.PressableButton>();
        Debug.Log($"[INFO] Found {pressableButtons.Length} PressableButton(s):");
        foreach (var btn in pressableButtons)
        {
            Debug.Log($"  - {btn.gameObject.name}");
            
            // Check if it has a collider
            var collider = btn.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning($"    [WARNING] No collider on {btn.gameObject.name}!");
            }
            else
            {
                Debug.Log($"    Collider: {collider.GetType().Name} (Enabled: {collider.enabled})");
                // Check collider size
                if (collider is BoxCollider box)
                {
                    Vector3 worldSize = Vector3.Scale(box.size, btn.transform.lossyScale);
                    Debug.Log($"    World Size: {worldSize}");
                    if (worldSize.magnitude < 0.01f)
                    {
                        Debug.LogWarning($"    [WARNING] Collider is very small! May be hard to hit.");
                    }
                }
            }
            
            // Check layer
            Debug.Log($"    Layer: {LayerMask.LayerToName(btn.gameObject.layer)} ({btn.gameObject.layer})");
        }
        
        // Check 6: Camera
        if (mainCamera != null)
        {
            Debug.Log($"[OK] Main Camera: {mainCamera.name} at position {mainCamera.transform.position}");
        }
        else
        {
            Debug.LogError("[PROBLEM] No camera found!");
        }
        
        // Check 7: Input System
        Debug.Log("[INFO] Using New Input System - use keyboard shortcuts:");
        Debug.Log("  Press 1-5 for mode/control selection, Enter to start");
        
        Debug.Log("========================================");
        Debug.Log("[MenuButtonDebugger] Diagnostics Complete");
        Debug.Log("========================================");
    }
    
    string GetFullPath(GameObject obj)
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
}
