using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateAndInjectMobileHUD
{
    [MenuItem("Build/Create Mobile HUD Prefab and Inject")]
    public static void CreateAndInject()
    {
        // Ensure Prefab folder
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string prefabPath = prefabFolder + "/MobileHUD.prefab";

        // Create root
        GameObject root = new GameObject("MobileHUD_PrefabRoot");
        root.AddComponent<MobileHUDVisibility>();

        // Canvas
        GameObject canvasObj = new GameObject("MobileHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Joystick container
        GameObject joystickGO = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(Image));
        joystickGO.transform.SetParent(canvasObj.transform, false);
        RectTransform jr = joystickGO.GetComponent<RectTransform>();
        jr.anchorMin = new Vector2(0f, 0f);
        jr.anchorMax = new Vector2(0f, 0f);
        jr.pivot = new Vector2(0f, 0f);
        jr.anchoredPosition = new Vector2(180f, 180f);
        jr.sizeDelta = new Vector2(260f, 260f);
        Image ji = joystickGO.GetComponent<Image>();
        ji.color = new Color(0f, 0f, 0f, 0.3f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(joystickGO.transform, false);
        RectTransform hr = handle.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0.5f, 0.5f);
        hr.anchorMax = new Vector2(0.5f, 0.5f);
        hr.anchoredPosition = Vector2.zero;
        hr.sizeDelta = new Vector2(100f, 100f);
        Image hi = handle.GetComponent<Image>();
        hi.color = new Color(1f, 1f, 1f, 0.8f);

        // Add VirtualJoystick component and assign handle
        var vj = joystickGO.AddComponent<global::VirtualJoystick>();
        vj.GetType().GetField("handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).SetValue(vj, handle.GetComponent<RectTransform>());

        // Create buttons
        CreateButton(canvasObj.transform, "AttackButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 160f), "Atacar", GameAction.Attack);
        CreateButton(canvasObj.transform, "InteractButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 160f), "Interact", GameAction.Interact);
        CreateButton(canvasObj.transform, "PauseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -80f), "Pausa", GameAction.Pause);
        CreateButton(canvasObj.transform, "InventoryButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-220f, -80f), "Invent", GameAction.Inventory);

        // Save prefab
        Object prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
        if (!success)
        {
            Debug.LogError("Failed to save MobileHUD prefab.");
            Object.DestroyImmediate(root);
            return;
        }

        Debug.Log($"Mobile HUD prefab created at {prefabPath}");

        // Ask user whether to inject into all scenes
        if (EditorUtility.DisplayDialog("Inject Mobile HUD", "Prefab created. Inject into all scenes in project now? This will open and modify scenes.", "Yes, inject", "No"))
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            string currentScenePath = SceneManager.GetActiveScene().path;
            foreach (string guid in guids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool found = false;
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    if (rootObj.name == "MobileHUD_Root")
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = "MobileHUD_Root";
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"Injected Mobile HUD into scene: {scenePath}");
                }
                else
                {
                    Debug.Log($"Scene already contains Mobile HUD: {scenePath}");
                }
            }

            // Reopen previous scene if any
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }
        }

        Object.DestroyImmediate(root);
    }

    [MenuItem("Build/Update Mobile HUD Buttons To Tap")]
    public static void UpdateButtonsToTap()
    {
        // Update prefab if exists
        string prefabPath = "Assets/Prefabs/MobileHUD.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            var buttons = prefab.GetComponentsInChildren<global::MobileButton>(true);
            foreach (var b in buttons)
            {
                b.mode = global::MobileButton.InteractionMode.Tap;
                EditorUtility.SetDirty(b);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Updated MobileHUD prefab MobileButton.mode => Tap");
        }

        // Update all scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        string current = SceneManager.GetActiveScene().path;
        foreach (var g in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                if (root.name != "MobileHUD_Root") continue;
                var mb = root.GetComponentsInChildren<global::MobileButton>(true);
                foreach (var b in mb)
                {
                    if (b.mode != global::MobileButton.InteractionMode.Tap)
                    {
                        b.mode = global::MobileButton.InteractionMode.Tap;
                        EditorUtility.SetDirty(b);
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Updated MobileHUD buttons in scene: {path}");
            }
        }

        if (!string.IsNullOrEmpty(current))
            EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
    }

    private static void CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, string label, GameAction action)
    {
        GameObject btn = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform r = btn.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = new Vector2(1f, 0f);
        r.sizeDelta = new Vector2(140f, 140f);
        r.anchoredPosition = anchoredPos;
        Image img = btn.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        GameObject txt = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txt.transform.SetParent(btn.transform, false);
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        Text t = txt.GetComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var mb = btn.AddComponent<global::MobileButton>();
        mb.action = action;
        // Use Tap mode for HUD buttons so a single click toggles actions
        mb.mode = global::MobileButton.InteractionMode.Tap;
    }
}
