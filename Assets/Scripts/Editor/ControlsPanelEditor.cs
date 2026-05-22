#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ControlsPanelEditor
{
    static ControlsPanelEditor()
    {
        EditorApplication.hierarchyChanged += EnsurePanelInOpenScene;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EnsurePanelInOpenScene();
        };

        EnsurePanelInOpenScene();
    }

    private static void EnsurePanelInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        ControlsPanel[] panels = FindAllPanels();
        if (panels.Length > 0)
        {
            if (panels.Length > 1)
                CleanupDuplicates(panels);
            return;
        }

        Canvas canvas = FindBestCanvas();
        if (canvas == null)
            return;

        CreateControlsPanel(canvas);
    }

    private static ControlsPanel[] FindAllPanels()
    {
        ControlsPanel[] panels = Resources.FindObjectsOfTypeAll<ControlsPanel>();
        return Array.FindAll(panels, panel => panel != null && panel.gameObject.scene.isLoaded);
    }

    private static void CleanupDuplicates(ControlsPanel[] panels)
    {
        if (panels.Length <= 1)
            return;

        for (int i = 1; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(panels[i].gameObject);
            }
        }
    }

    private static Canvas FindBestCanvas()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas fallback = null;
        foreach (var canvas in canvases)
        {
            if (canvas == null || !canvas.gameObject.scene.isLoaded)
                continue;

            if (fallback == null)
                fallback = canvas;

            string name = canvas.gameObject.name.ToLowerInvariant();
            if (name.Contains("hud") || name.Contains("heart"))
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        return fallback;
    }

    private static void CreateControlsPanel(Canvas canvas)
    {
        GameObject panel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ControlsPanel));
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.15f);
        panelRect.anchorMax = new Vector2(0.8f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);
        panelImage.raycastTarget = true;

        Undo.RegisterCreatedObjectUndo(panel, "Create ControlsPanel");
        EditorUtility.SetDirty(panel);
    }
}
#endif