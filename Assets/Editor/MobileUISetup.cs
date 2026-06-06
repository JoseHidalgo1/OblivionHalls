using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MobileUISetup
{
    [MenuItem("Build/Create Mobile HUD")]
    public static void CreateMobileHUD()
    {
        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasObj = new GameObject("MobileHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Create joystick container
        GameObject joystickGO = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(Image));
        joystickGO.transform.SetParent(canvas.transform, false);
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

        var vj = joystickGO.AddComponent<UnityEngine.UI.Mask>(); // for nicer visuals
        joystickGO.AddComponent<UnityEngine.UI.Image>();
        var vjc = joystickGO.AddComponent<global::VirtualJoystick>();
        vjc.GetType().GetField("handle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValue(vjc, handle.GetComponent<RectTransform>());

        // Create buttons: Attack, Interact, Pause, Inventory
        CreateButton(canvas.transform, "AttackButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 160f), "Atacar", GameAction.Attack);
        CreateButton(canvas.transform, "InteractButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 160f), "Interact", GameAction.Interact);
        CreateButton(canvas.transform, "PauseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -80f), "Pausa", GameAction.Pause);
        CreateButton(canvas.transform, "InventoryButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-220f, -80f), "Invent", GameAction.Inventory);

        Debug.Log("Mobile HUD created. Tweak positions and styles as needed.");
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
        mb.mode = global::MobileButton.InteractionMode.Tap;
    }
}
