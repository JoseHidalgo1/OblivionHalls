using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
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
    private GameObject audioMenuBody;
    private RectTransform contentRoot;
    private Text statusText;
    private bool isWaitingForKey;
    private GameAction actionBeingRebound;
    private bool panelVisible;
    private bool audioMenuVisible;
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
        if (Application.isPlaying && IsPauseAllowed())
        {
            bool pausePressed = Input.GetKeyDown(KeyCode.Escape);
            if (Keyboard.current != null)
            {
                pausePressed |= Keyboard.current[KeyBindings.GetKey(GameAction.Pause)]?.wasPressedThisFrame == true;
            }

            if (pausePressed)
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

        Transform existingBody = transform.Find("PanelBody");
        if (existingBody != null)
        {
            panelBody = existingBody.gameObject;
            menuBody = panelBody.transform.Find("PauseMenuBody")?.gameObject;
            controlsMenuBody = panelBody.transform.Find("ControlsMenuBody")?.gameObject;
            audioMenuBody = panelBody.transform.Find("AudioMenuBody")?.gameObject;
            Transform scrollView = panelBody.transform.Find("BindingsScrollView");
            if (scrollView != null)
            {
                ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
                if (scrollRect != null)
                    contentRoot = scrollRect.content;
            }
            InitializeExistingPanelUI();
            return;
        }

        if (panelBody == null)
        {
            Transform existingPanelBody = transform.Find("PanelBody");
            if (existingPanelBody != null)
            {
                if (Application.isPlaying)
                    Destroy(existingPanelBody.gameObject);
                else
                    DestroyImmediate(existingPanelBody.gameObject);
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

        GameObject titleObj = CreateTextObject("PauseTitle", "PAUSA", 22, TextAnchor.UpperCenter, menuRect, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.82f));
        titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

        CreateMenuButton(menuRect, "ContinueButton", "Continuar", new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.65f), HidePanel);
        CreateMenuButton(menuRect, "ControlsButton", "Controles", new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.5f), () => ShowControlsMenu(true));
        CreateMenuButton(menuRect, "AudioButton", "Sonido", new Vector2(0.3f, 0.28f), new Vector2(0.7f, 0.38f), () => ShowAudioSettings(true));
        CreateMenuButton(menuRect, "QuitButton", "Salir", new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.25f), QuitGame);

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

        // Audio Settings Panel
        audioMenuBody = new GameObject("AudioMenuBody", typeof(RectTransform));
        audioMenuBody.transform.SetParent(bodyRect, false);
        RectTransform audioRect = audioMenuBody.GetComponent<RectTransform>();
        audioRect.anchorMin = Vector2.zero;
        audioRect.anchorMax = Vector2.one;
        audioRect.offsetMin = Vector2.zero;
        audioRect.offsetMax = Vector2.zero;

        GameObject audioTitleObj = CreateTextObject("AudioTitle", "Sonido", 20, TextAnchor.UpperCenter, audioRect, new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.95f));
        audioTitleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

        CreateAudioVolumeSliders(audioRect);
        CreateMenuButton(audioRect, "AudioBackButton", "Volver", new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.1f), BackFromAudioSettings);
        audioMenuBody.SetActive(false);
    }

    private void CreateAudioVolumeSliders(RectTransform parent)
    {
        AudioManager audioManager = Application.isPlaying ? AudioManager.GetOrCreate() : null;
        string[] trackNames = { "MainMenu", "Exploration", "Boss", "Win", "Death", "Loading" };
        string[] trackLabels = { "Menú Principal", "Exploración", "Boss", "Victoria", "Muerte", "Carga" };
        float[] defaultVolumes = { 1f, 1f, 0.8f, 1f, 0.9f, 1f };

        RectTransform audioContent = CreateScrollView("AudioScrollView", new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.78f), parent);

        float rowHeight = 100f;
        float spacing = 12f;
        float contentHeight = trackNames.Length * (rowHeight + spacing);
        audioContent.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < trackNames.Length; i++)
        {
            string trackName = trackNames[i];
            string trackLabel = trackLabels[i];
            float volumeValue = audioManager != null ? audioManager.GetTrackVolume(trackName) : defaultVolumes[i];
            float yPos = -(i * (rowHeight + spacing));

            GameObject row = new GameObject(trackName + "Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(audioContent, false);
            Image rowImage = row.GetComponent<Image>();
            rowImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, rowHeight);
            rowRect.anchoredPosition = new Vector2(0f, yPos);

            GameObject labelObj = CreateTextObject(trackName + "Label", trackLabel, 18, TextAnchor.UpperLeft, rowRect, new Vector2(0.05f, 0.6f), new Vector2(0.5f, 0.95f));
            Text labelText = labelObj.GetComponent<Text>();
            labelText.color = Color.white;
            labelText.fontStyle = FontStyle.Bold;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;

            GameObject valueObj = CreateTextObject(trackName + "Value", Mathf.RoundToInt(volumeValue * 100).ToString(), 16, TextAnchor.MiddleCenter, rowRect, new Vector2(0.76f, 0.25f), new Vector2(0.95f, 0.55f));
            Text valueText = valueObj.GetComponent<Text>();
            valueText.color = Color.white;
            valueText.fontStyle = FontStyle.Bold;

            GameObject sliderObj = new GameObject(trackName + "Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            sliderObj.transform.SetParent(rowRect, false);
            Image sliderImage = sliderObj.GetComponent<Image>();
            sliderImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.05f, 0.1f);
            sliderRect.anchorMax = new Vector2(0.75f, 0.45f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = volumeValue;
            slider.targetGraphic = sliderImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;
            slider.colors = ColorBlock.defaultColorBlock;

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 0.8f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(sliderObj.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 1f);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 40f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            slider.handleRect = handleRect;
            slider.transition = Selectable.Transition.ColorTint;

            if (audioManager != null)
            {
                string currentTrackName = trackName;
                Text currentValueText = valueText;
                slider.onValueChanged.AddListener(value =>
                {
                    audioManager.SetTrackVolume(currentTrackName, value);
                    currentValueText.text = Mathf.RoundToInt(value * 100).ToString();
                });
            }
        }
    }

    private void InitializeExistingPanelUI()
    {
        InitializeExistingButtons(menuBody);
        InitializeExistingButtons(controlsMenuBody);
        InitializeExistingButtons(audioMenuBody);
        InitializeExistingAudioSliders(audioMenuBody);
    }

    private void InitializeExistingButtons(GameObject root)
    {
        if (root == null)
            return;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.targetGraphic = button.targetGraphic ?? button.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;

            switch (button.gameObject.name)
            {
                case "ContinueButton":
                    button.onClick.AddListener(HidePanel);
                    break;
                case "ControlsButton":
                    button.onClick.AddListener(() => ShowControlsMenu(true));
                    break;
                case "AudioButton":
                    button.onClick.AddListener(() => ShowAudioSettings(true));
                    break;
                case "QuitButton":
                    button.onClick.AddListener(QuitGame);
                    break;
                case "BackButton":
                    button.onClick.AddListener(BackFromControls);
                    break;
                case "AudioBackButton":
                    button.onClick.AddListener(BackFromAudioSettings);
                    break;
                case "CloseButton":
                    button.onClick.AddListener(HidePanel);
                    break;
            }
        }
    }

    private void InitializeExistingAudioSliders(GameObject root)
    {
        if (root == null)
            return;

        AudioManager audioManager = AudioManager.GetOrCreate();
        if (audioManager == null)
            return;

        string[] trackNames = { "MainMenu", "Exploration", "Boss", "Win", "Death" };
        foreach (string trackName in trackNames)
        {
            Slider slider = null;
            foreach (Slider candidate in root.GetComponentsInChildren<Slider>(true))
            {
                if (candidate != null && candidate.gameObject.name == trackName + "Slider")
                {
                    slider = candidate;
                    break;
                }
            }

            if (slider == null)
                continue;

            Text valueText = slider.transform.parent.Find(trackName + "Value")?.GetComponent<Text>();
            if (slider == null || valueText == null)
                continue;

            float volumeValue = audioManager.GetTrackVolume(trackName);
            slider.value = volumeValue;
            valueText.text = Mathf.RoundToInt(volumeValue * 100).ToString();
            InitializeAudioSlider(slider, valueText, trackName, audioManager);
        }
    }

    private void InitializeAudioSlider(Slider slider, Text valueText, string trackName, AudioManager audioManager)
    {
        if (slider == null || valueText == null || audioManager == null)
            return;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value =>
        {
            audioManager.SetTrackVolume(trackName, value);
            valueText.text = Mathf.RoundToInt(value * 100).ToString();
        });
    }

    private void UpdateAudioSlider(string trackName, RectTransform sliderBgRect, RectTransform sliderFillRect, Text volumeText, AudioManager audioManager)
    {
        // Get mouse position relative to slider
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBgRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            float sliderWidth = sliderBgRect.rect.width;
            float normalizedValue = Mathf.Clamp01(localPoint.x / sliderWidth);
            
            audioManager.SetTrackVolume(trackName, normalizedValue);
            
            // Update UI
            sliderFillRect.anchorMax = new Vector2(normalizedValue, 1f);
            volumeText.text = (Mathf.Round(normalizedValue * 100)).ToString();
        }
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
        audioMenuVisible = false;
        isPauseMode = pauseMode;
        SetMenuState(showControls: true, showAudio: false);
        SetPanelVisibility(true);
        SetPauseState(pauseMode);
        RefreshBindingsUI();
    }

    public void ShowAudioSettings(bool pauseMode)
    {
        panelVisible = true;
        audioMenuVisible = true;
        isPauseMode = pauseMode;
        SetMenuState(showControls: false, showAudio: true);
        SetPanelVisibility(true);
        SetPauseState(pauseMode);
    }

    public void HidePanel()
    {
        panelVisible = false;
        audioMenuVisible = false;
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
            menuBody.SetActive(!showControls && !audioMenuVisible);
        if (controlsMenuBody != null)
            controlsMenuBody.SetActive(showControls);
        if (audioMenuBody != null)
            audioMenuBody.SetActive(audioMenuVisible);
    }

    private void SetMenuState(bool showControls, bool showAudio)
    {
        if (menuBody != null)
            menuBody.SetActive(!showControls && !showAudio);
        if (controlsMenuBody != null)
            controlsMenuBody.SetActive(showControls);
        if (audioMenuBody != null)
            audioMenuBody.SetActive(showAudio);
    }

    private void BackFromAudioSettings()
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
        scrollRect.viewport = viewportRect;
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