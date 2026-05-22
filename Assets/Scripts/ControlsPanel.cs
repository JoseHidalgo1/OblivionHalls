using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

    [ExecuteAlways]
public class ControlsPanel : MonoBehaviour
{
    private const string PanelName = "ControlsPanel";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimePanelExists()
    {
        RemoveLegacyPanels();
        ControlsPanel[] panels = FindAllPanels();
        if (panels.Length > 0)
        {
            if (panels.Length > 1)
                CleanupDuplicates(panels);
            return;
        }

        Canvas canvas = FindBestCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("ControlsPanel: No Canvas found in the loaded scene. Please add a Canvas to GameScene or manually place the controls panel.");
            return;
        }

        GameObject panel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);
        panelImage.raycastTarget = true;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.15f);
        panelRect.anchorMax = new Vector2(0.8f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        ControlsPanel panelScript = panel.AddComponent<ControlsPanel>();
        panelScript.HidePanel();
        Debug.Log("ControlsPanel: Auto-created runtime panel in scene " + canvas.gameObject.scene.name + ".");
    }

    private static void RemoveLegacyPanels()
    {
        foreach (GameObject root in GetLoadedSceneRootObjects())
        {
            if (root == null)
                continue;
            if (!root.name.Contains(PanelName))
                continue;
            if (root.GetComponent<ControlsPanel>() == null)
            {
                if (Application.isPlaying)
                    Destroy(root);
                else
                    DestroyImmediate(root);
            }
        }
    }

    private static GameObject[] GetLoadedSceneRootObjects()
    {
        List<GameObject> roots = new List<GameObject>();
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            roots.AddRange(scene.GetRootGameObjects());
        }
        return roots.ToArray();
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

        ControlsPanel keep = panels[0];
        for (int i = 1; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(panels[i].gameObject);
                else
                    DestroyImmediate(panels[i].gameObject);
            }
        }
        Debug.LogWarning($"ControlsPanel: Found {panels.Length} duplicate panels. Kept one and removed the rest.");
    }

    private static Canvas FindBestCanvas()
    {
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas fallback = null;
        foreach (var canvas in allCanvases)
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
    private RectTransform panelRect;
    private Image panelBackgroundImage;
    private GameObject panelBody;
    private GameObject menuBody;
    private GameObject controlsMenuBody;
    private RectTransform contentRoot;
    private Text statusText;
    private bool isWaitingForKey;
    private GameAction actionBeingRebound;
    private bool panelVisible;
    private bool controlsMenuVisible;
    private bool isPauseMode;

    private readonly GameAction[] displayedActions = new[]
    {
        GameAction.MoveUp,
        GameAction.MoveDown,
        GameAction.MoveLeft,
        GameAction.MoveRight,
        GameAction.Sprint,
        GameAction.Attack,
        GameAction.Jump,
        GameAction.Interact,
        GameAction.Inventory,
        GameAction.ToggleMap,
        GameAction.Pickup,
        GameAction.Pause
    };

    void Awake()
    {
        RemoveLegacyPanels();
        ControlsPanel[] panels = FindAllPanels();
        if (panels.Length > 1)
            CleanupDuplicates(panels);

        panelRect = GetComponent<RectTransform>();
        panelBackgroundImage = GetComponent<Image>();
        if (panelBackgroundImage == null)
        {
            panelBackgroundImage = gameObject.AddComponent<Image>();
            panelBackgroundImage.color = new Color(0f, 0f, 0f, 0.85f);
        }

        BuildPanelUI();
        RefreshBindingsUI();
        if (Application.isPlaying)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }
    void Start()
    {
    }

    void OnEnable()
    {
        RemoveLegacyPanels();
        ControlsPanel[] panels = FindAllPanels();
        if (panels.Length > 1)
            CleanupDuplicates(panels);

        BuildPanelUI();
        RefreshBindingsUI();
        if (Application.isPlaying)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    void Update()
    {
        if (Application.isPlaying && Keyboard.current != null && IsPauseAllowed())
        {
            if (Keyboard.current[KeyBindings.GetKey(GameAction.Pause)]?.wasPressedThisFrame == true)
            {
                if (!isWaitingForKey)
                {
                    TogglePanel();
                }
            }
        }

        if (!isWaitingForKey)
            return;

        DetectNewKey();
    }

    private void BuildPanelUI()
    {
        if (contentRoot != null)
            return;

        if (panelBody == null)
        {
            Transform existingBody = transform.Find("PanelBody");
            if (existingBody != null)
            {
                if (Application.isPlaying)
                    Destroy(existingBody.gameObject);
                else
                    DestroyImmediate(existingBody.gameObject);
            }
        }

        panelBody = new GameObject("PanelBody", typeof(RectTransform));
        panelBody.transform.SetParent(transform, false);
        RectTransform bodyRect = panelBody.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        menuBody = new GameObject("PauseMenuBody", typeof(RectTransform));
        menuBody.transform.SetParent(bodyRect, false);
        RectTransform menuRect = menuBody.GetComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        GameObject titleObj = CreateTextObject("PauseTitle", "PAUSA", 22, TextAnchor.UpperCenter, menuRect, new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.92f));
        titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

        CreateMenuButton(menuRect, "ContinueButton", "Continuar", new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.65f), HidePanel);
        CreateMenuButton(menuRect, "ControlsButton", "Controles", new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.5f), () => ShowControlsMenu(true));
        CreateMenuButton(menuRect, "QuitButton", "Salir", new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.35f), QuitGame);

        controlsMenuBody = new GameObject("ControlsMenuBody", typeof(RectTransform));
        controlsMenuBody.transform.SetParent(bodyRect, false);
        RectTransform controlsRect = controlsMenuBody.GetComponent<RectTransform>();
        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = Vector2.zero;
        controlsRect.offsetMax = Vector2.zero;

        GameObject controlsTitleObj = CreateTextObject("ControlsTitle", "Controles y teclas asignadas", 18, TextAnchor.UpperCenter, controlsRect, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.9f));
        controlsTitleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

        GameObject infoObj = CreateTextObject("ControlsInfo", "Haz clic en Cambiar y presiona una tecla nueva para asignarla. Presiona Esc para cancelar.", 14, TextAnchor.UpperCenter, controlsRect, new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.78f));
        Text infoText = infoObj.GetComponent<Text>();
        infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject statusObj = CreateTextObject("StatusText", "", 14, TextAnchor.MiddleCenter, controlsRect, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.12f));
        statusText = statusObj.GetComponent<Text>();

        contentRoot = CreateScrollView("BindingsScrollView", new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.66f), controlsRect);
        CreateMenuButton(controlsRect, "BackButton", "Volver", new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.1f), BackFromControls);
        controlsMenuBody.SetActive(false);
    }

    private bool IsPauseAllowed()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        return !activeScene.Equals("MainMenu", StringComparison.OrdinalIgnoreCase);
    }
    private void RefreshBindingsUI()
    {
        if (contentRoot == null)
            return;

        ClearContent();

        float rowHeight = 38f;
        float spacing = 8f;
        float contentHeight = displayedActions.Length * (rowHeight + spacing) - spacing;
        contentRoot.sizeDelta = new Vector2(0f, Mathf.Max(contentHeight, 0f));

        for (int i = 0; i < displayedActions.Length; i++)
        {
            GameAction action = displayedActions[i];
            float yPosition = -i * (rowHeight + spacing);
            CreateActionRow(action, yPosition, rowHeight);
        }
    }

    private void ClearContent()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(contentRoot.GetChild(i).gameObject);
            }
        }
    }

    private void CreateActionRow(GameAction action, float y, float height)
    {
        GameObject row = new GameObject(action + "Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        row.transform.SetParent(contentRoot, false);
        Image rowImage = row.GetComponent<Image>();
        rowImage.color = new Color(0.14f, 0.14f, 0.14f, 0.85f);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(0f, height);

        string actionLabel = GetActionLabel(action);
        string keyLabel = KeyBindings.GetKeyDisplayName(action);

        GameObject nameTextObj = CreateTextObject(action + "Label", actionLabel, 14, TextAnchor.MiddleLeft, rowRect, new Vector2(0.02f, 0f), new Vector2(0.55f, 1f));
        Text nameText = nameTextObj.GetComponent<Text>();
        nameText.color = Color.white;
        nameText.fontStyle = FontStyle.Bold;

        GameObject keyTextObj = CreateTextObject(action + "Key", keyLabel, 14, TextAnchor.MiddleRight, rowRect, new Vector2(0.55f, 0f), new Vector2(0.72f, 1f));
        Text keyText = keyTextObj.GetComponent<Text>();
        keyText.color = Color.white;

        GameObject buttonObj = new GameObject(action + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(row.transform, false);
        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() => BeginRebind(action));

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.78f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.98f, 0.9f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        GameObject buttonTextObj = CreateTextObject(action + "ButtonText", "Cambiar", 13, TextAnchor.MiddleCenter, buttonRect, Vector2.zero, Vector2.one);
        Text buttonText = buttonTextObj.GetComponent<Text>();
        buttonText.color = Color.white;
    }

    private void TogglePanel()
    {
        if (panelVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        panelVisible = true;
        SetMenuState(false);
        isPauseMode = true;
        SetPanelVisibility(true);
        SetPauseState(true);
    }

    public void ShowControlsMenu()
    {
        ShowControlsMenu(true);
    }

    public void ShowControlsMenu(bool pauseMode)
    {
        panelVisible = true;
        controlsMenuVisible = true;
        isPauseMode = pauseMode;
        SetMenuState(true);
        SetPanelVisibility(true);
        SetPauseState(pauseMode);
        RefreshBindingsUI();
    }

    public void HidePanel()
    {
        panelVisible = false;
        controlsMenuVisible = false;
        SetPanelVisibility(false);
        SetPauseState(false);
        isPauseMode = false;
        isWaitingForKey = false;
        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    private void BackFromControls()
    {
        if (isPauseMode)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    private void QuitGame()
    {
        SetPauseState(false);
        SceneManager.LoadScene("MainMenu");
    }

    private void SetPauseState(bool paused)
    {
        if (!Application.isPlaying)
            return;

        Time.timeScale = paused ? 0f : 1f;
    }

    private void SetMenuState(bool showControls)
    {
        if (menuBody != null)
            menuBody.SetActive(!showControls);
        if (controlsMenuBody != null)
            controlsMenuBody.SetActive(showControls);
    }

    private void SetPanelVisibility(bool visible)
    {
        if (panelBackgroundImage != null)
        {
            panelBackgroundImage.enabled = visible;
        }
        if (panelBody != null)
        {
            panelBody.SetActive(visible);
        }
    }

    private void DetectNewKey()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelRebind();
            return;
        }

        foreach (Key key in Enum.GetValues(typeof(Key)))
        {
            if (key == Key.None)
                continue;

            var control = Keyboard.current[key];
            if (control == null)
                continue;

            if (control.wasPressedThisFrame)
            {
                KeyBindings.SetKey(actionBeingRebound, key);
                isWaitingForKey = false;
                if (statusText != null)
                {
                    statusText.text = $"Tecla asignada: {KeyBindings.GetKeyDisplayName(key)} para '{GetActionLabel(actionBeingRebound)}'.";
                }
                RefreshBindingsUI();
                return;
            }
        }
    }

    private void BeginRebind(GameAction action)
    {
        if (isWaitingForKey)
            return;

        actionBeingRebound = action;
        isWaitingForKey = true;
        if (statusText != null)
        {
            statusText.text = $"Presiona una nueva tecla para '{GetActionLabel(action)}' o Esc para cancelar.";
        }
    }

    private void CancelRebind()
    {
        isWaitingForKey = false;
        if (statusText != null)
        {
            statusText.text = "Reasignación cancelada.";
        }
    }

    private GameObject CreateTextObject(string name, string text, int size, TextAnchor alignment, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = obj.GetComponent<Text>();
        label.text = text;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return obj;
    }

    private void CreateMenuButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(action);

        GameObject textObj = CreateTextObject(name + "Text", label, 16, TextAnchor.MiddleCenter, rect, Vector2.zero, Vector2.one);
        Text buttonText = textObj.GetComponent<Text>();
        buttonText.color = Color.white;
        buttonText.fontStyle = FontStyle.Bold;
    }

    private RectTransform CreateScrollView(string name, Vector2 anchorMin, Vector2 anchorMax, RectTransform parent)
    {
        GameObject scrollView = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollView.transform.SetParent(parent, false);
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = anchorMin;
        scrollRectTransform.anchorMax = anchorMax;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        Image scrollImage = scrollView.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.25f);

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;

        GameObject viewport = new GameObject(name + "Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.05f);
        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = new GameObject(name + "Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.content = contentRect;
        return contentRect;
    }

    private void CreateCloseButton(RectTransform parent)
    {
        GameObject buttonObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0.02f);
        rect.anchorMax = new Vector2(0.65f, 0.08f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(HidePanel);

        GameObject textObj = CreateTextObject("CloseButtonText", "Cerrar", 14, TextAnchor.MiddleCenter, rect, Vector2.zero, Vector2.one);
        Text buttonText = textObj.GetComponent<Text>();
        buttonText.color = Color.white;
        buttonText.fontStyle = FontStyle.Bold;
    }

    private string GetActionLabel(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveUp: return "Mover Arriba";
            case GameAction.MoveDown: return "Mover Abajo";
            case GameAction.MoveLeft: return "Mover Izquierda";
            case GameAction.MoveRight: return "Mover Derecha";
            case GameAction.Sprint: return "Correr";
            case GameAction.Attack: return "Atacar";
            case GameAction.Jump: return "Saltar";
            case GameAction.Interact: return "Interactuar";
            case GameAction.Inventory: return "Abrir Inventario";
            case GameAction.ToggleMap: return "Mostrar Mapa";
            case GameAction.Pickup: return "Recoger";
            case GameAction.Pause: return "Pausa/Menu";
            default: return action.ToString();
            }
        }
    }