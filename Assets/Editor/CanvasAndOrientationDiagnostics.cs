using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class CanvasAndOrientationDiagnostics
{
    [MenuItem("Build/Diagnose Canvas & Orientation Issues")]
    public static void DiagnoseCanvasAndOrientation()
    {
        Debug.Log("=== Canvas & Orientation Diagnostics ===\n");

        // 1. Check Player Settings
        CheckPlayerSettings();

        // 2. Load MainMenu and check all Canvas
        CheckMainMenuCanvases();

        // 3. Verify ForceLandscape script
        VerifyForceLandscapeScript();

        Debug.Log("\n=== Diagnostics Complete ===");
    }

    private static void CheckPlayerSettings()
    {
        Debug.Log("\n[PlayerSettings]");

        var defaultOrientation = PlayerSettings.defaultInterfaceOrientation;
        Debug.Log($"Default Orientation: {defaultOrientation}");
        
        if (defaultOrientation != UIOrientation.LandscapeLeft && defaultOrientation != UIOrientation.LandscapeRight)
        {
            Debug.LogWarning("⚠ Default Orientation is NOT landscape! Change it to LandscapeLeft or LandscapeRight in Player Settings.");
        }
        else
        {
            Debug.Log("✓ Default Orientation is landscape");
        }

        // Check allowed orientations for Android
        bool allowedPortrait = PlayerSettings.allowedAutorotateToPortrait;
        bool allowedPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool allowedLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool allowedLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;

        Debug.Log($"Allowed Orientations - Portrait: {allowedPortrait}, UpsideDown: {allowedPortraitUpsideDown}, LandscapeLeft: {allowedLandscapeLeft}, LandscapeRight: {allowedLandscapeRight}");
        
        if (allowedPortrait || allowedPortraitUpsideDown)
        {
            Debug.LogWarning("⚠ Portrait orientation is ALLOWED! Disable it to force landscape.");
        }
        else
        {
            Debug.Log("✓ Portrait orientation is disabled");
        }
    }

    private static void CheckMainMenuCanvases()
    {
        Debug.Log("\n[MainMenu Scene Canvases]");

        // Load MainMenu scene in editor
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(mainMenuPath, OpenSceneMode.Single);

        Canvas[] canvases = GetCanvasesInScene(scene);
        Debug.Log($"Found {canvases.Length} Canvas(es) in MainMenu");

        if (canvases.Length == 0)
        {
            Debug.LogWarning("⚠ NO CANVAS FOUND in MainMenu!");
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"\n  Canvas: {canvas.name}");
            
            // Check rendering mode
            Debug.Log($"    - Render Mode: {canvas.renderMode}");
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"    ⚠ Not ScreenSpaceOverlay (UI won't scale correctly on different resolutions)");
            }

            // Check Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"    - Canvas Scaler: UI Scale Mode = {scaler.uiScaleMode}");
                Debug.Log($"      Reference Resolution: {scaler.referenceResolution}");
                Debug.Log($"      Scale with Screen Size: {(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize ? "Yes" : "No")}");

                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    Debug.LogWarning($"    ⚠ UI Scale Mode should be 'Scale With Screen Size' for mobile responsiveness!");
                }

                // Check reference resolution (should be landscape: width > height)
                if (scaler.referenceResolution.x < scaler.referenceResolution.y)
                {
                    Debug.LogWarning($"    ⚠ Reference Resolution looks like PORTRAIT ({scaler.referenceResolution.x}x{scaler.referenceResolution.y}), should be LANDSCAPE!");
                }
                else
                {
                    Debug.Log($"    ✓ Reference Resolution is landscape aspect ratio");
                }
            }
            else
            {
                Debug.LogWarning($"    ⚠ NO CANVAS SCALER found! This is required for mobile.");
            }

            // Check Graphics Raycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"    ✓ GraphicRaycaster present");
            }
            else
            {
                Debug.LogWarning($"    ⚠ NO GRAPHIC RAYCASTER found! UI won't be interactive.");
            }

            // List all immediate children
            Debug.Log($"    - Immediate Children ({canvas.transform.childCount}):");
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                LayoutGroup layout = child.GetComponent<LayoutGroup>();
                string layoutInfo = layout != null ? $" [Layout: {layout.GetType().Name}]" : "";
                Debug.Log($"      • {child.name}{layoutInfo}");
            }
        }

        // Check EventSystem in scene
        EventSystem[] eventSystems = GetEventSystemsInScene(scene);
        if (eventSystems.Length > 0)
        {
            Debug.Log($"\n  ✓ EventSystem present ({eventSystems.Length})");
        }
        else
        {
            Debug.LogWarning($"\n  ⚠ NO EVENTSYSTEM found! UI won't work.");
        }
    }

    private static Canvas[] GetCanvasesInScene(Scene scene)
    {
        var canvasesList = new System.Collections.Generic.List<Canvas>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            canvasesList.AddRange(root.GetComponentsInChildren<Canvas>());
        }
        return canvasesList.ToArray();
    }

    private static EventSystem[] GetEventSystemsInScene(Scene scene)
    {
        var eventSystemsList = new System.Collections.Generic.List<EventSystem>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            eventSystemsList.AddRange(root.GetComponentsInChildren<EventSystem>());
        }
        return eventSystemsList.ToArray();
    }

    private static void VerifyForceLandscapeScript()
    {
        Debug.Log("\n[ForceLandscape Script]");

        var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Runtime/ForceLandscape.cs");
        if (script != null)
        {
            Debug.Log("✓ ForceLandscape script exists");
            Debug.Log("  Note: This script runs at [DefaultExecutionOrder(-200)] Awake()");
            Debug.Log("  It should set Screen.orientation = ScreenOrientation.LandscapeLeft");
        }
        else
        {
            Debug.LogWarning("⚠ ForceLandscape script NOT found!");
        }
    }
}
