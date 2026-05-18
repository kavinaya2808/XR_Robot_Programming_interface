using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Editor helper to clean up common scene issues
/// </summary>
public class SceneCleanupHelper : EditorWindow
{
    [MenuItem("Tools/MR Robot Explorer/Fix Common Issues")]
    static void FixCommonIssues()
    {
        int fixCount = 0;
        
        // Fix 1: Remove duplicate EventSystems
        var eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystems.Length > 1)
        {
            Debug.Log($"[Cleanup] Found {eventSystems.Length} EventSystems, keeping first, removing others...");
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Undo.DestroyObjectImmediate(eventSystems[i].gameObject);
                fixCount++;
            }
        }
        
        // Fix 2: Try to find and remove PointableCanvas if no module exists
        var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction.Runtime");
        var pointableModuleType = System.Type.GetType("Oculus.Interaction.PointableCanvasModule, Oculus.Interaction.Runtime");
        
        if (pointableCanvasType != null)
        {
            var canvases = FindObjectsOfType(pointableCanvasType);
            var modules = FindObjectsOfType(pointableModuleType);
            
            if (canvases.Length > 0 && modules.Length == 0)
            {
                Debug.Log($"[Cleanup] Found {canvases.Length} PointableCanvas without PointableCanvasModule. Removing them...");
                foreach (var canvas in canvases)
                {
                    var component = canvas as Component;
                    if (component != null)
                    {
                        Undo.DestroyObjectImmediate(component);
                        fixCount++;
                    }
                }
            }
        }
        
        Debug.Log($"[Cleanup] ✓ Fixed {fixCount} issues. Now manually add missing tags in Edit → Project Settings → Tags and Layers");
        
        if (fixCount > 0)
        {
            EditorUtility.DisplayDialog("Scene Cleanup", 
                $"Fixed {fixCount} issues!\n\n" +
                "Still need to manually:\n" +
                "1. Add 'Shelf', 'Obstacle', 'Box' tags in Project Settings\n" +
                "2. Start Docker for ROS connection",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Scene Cleanup", 
                "No automatic fixes needed.\n\n" +
                "Check manually:\n" +
                "1. Tags in Project Settings\n" +
                "2. PointableCanvas components\n" +
                "3. Duplicate EventSystems",
                "OK");
        }
    }
    
    [MenuItem("Tools/MR Robot Explorer/Add Required Tags")]
    static void AddRequiredTags()
    {
        // Get the TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        string[] requiredTags = { "Shelf", "Obstacle", "Box", "Robot" };
        int addedCount = 0;
        
        foreach (string tag in requiredTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                addedCount++;
                Debug.Log($"[Cleanup] Added tag: {tag}");
            }
        }
        
        tagManager.ApplyModifiedProperties();
        
        EditorUtility.DisplayDialog("Tags Added", 
            $"Added {addedCount} new tags:\n" +
            string.Join(", ", requiredTags),
            "OK");
    }
}

