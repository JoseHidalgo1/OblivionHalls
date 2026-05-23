using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject controlsPanel; // Asigna el panel de controles aquí

    [Header("Opciones automáticas")]
    public bool autoCreateControlsPanel = true;
    public bool showControlsPanelInEditor = true;
    public string controlsPanelName = "ControlsPanel";

    [Header("Escena de juego")]
    public string gameSceneName = "GameScene";

    [Header("Imágenes del menú")]
    public GameObject playImageObject;
    public GameObject controlsImageObject;
    public GameObject quitImageObject;
    public GameObject audioSettingsImageObject;

    [Header("Sprites opcionales")]
    public Sprite playNormal;
    public Sprite playHover;
    public Sprite playPressed;
    public Sprite controlsNormal;
    public Sprite controlsHover;
    public Sprite controlsPressed;
    public Sprite quitNormal;
    public Sprite quitHover;
    public Sprite quitPressed;
    public Sprite audioNormal;
    public Sprite audioHover;
    public Sprite audioPressed;

    public void PlayGame()
    {
        string sceneToLoad = GetValidGameSceneName();
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No se encontró la escena de juego. Añádela a Build Settings o configura gameSceneName en MenuController.");
        }
    }

    private string GetValidGameSceneName()
    {
        if (!string.IsNullOrEmpty(gameSceneName) && IsSceneInBuildSettings(gameSceneName))
        {
            return gameSceneName;
        }

        int totalScenes = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < totalScenes; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name != null && name != SceneManager.GetActiveScene().name)
            {
                return name;
            }
        }

        return null;
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < totalScenes; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    public void ShowControls()
    {
        if (controlsPanel == null && autoCreateControlsPanel)
        {
            EnsureControlsPanel();
        }
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
            ControlsPanel panelScript = controlsPanel.GetComponent<ControlsPanel>();
            if (panelScript != null)
            {
                panelScript.ShowControlsMenu(false);
            }
        }
    }

    public void HideControls()
    {
        if (controlsPanel != null)
        {
            if (Application.isPlaying)
            {
                ControlsPanel panelScript = controlsPanel.GetComponent<ControlsPanel>();
                if (panelScript != null)
                {
                    panelScript.HidePanel();
                }
                else
                {
                    controlsPanel.SetActive(false);
                }
            }
            else if (showControlsPanelInEditor)
            {
                controlsPanel.SetActive(true);
            }
        }
    }

    public void ShowAudioSettings()
    {
        if (controlsPanel == null && autoCreateControlsPanel)
        {
            EnsureControlsPanel();
        }
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
            ControlsPanel panelScript = controlsPanel.GetComponent<ControlsPanel>();
            if (panelScript != null)
            {
                panelScript.ShowAudioSettings(false);
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    void Awake()
    {
        if (controlsPanel == null && autoCreateControlsPanel)
        {
            EnsureControlsPanel();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas != null && audioSettingsImageObject == null)
        {
            GameObject existingAudioButton = FindRecursiveAudioButton(canvas.transform, "AudioSettingsButton");
            if (existingAudioButton != null)
            {
                audioSettingsImageObject = existingAudioButton;
            }
        }
        EnsureAudioSettingsButton(canvas);

        SetupImageButton(playImageObject, PlayGame, playNormal, playHover, playPressed);
        SetupImageButton(controlsImageObject, ShowControls, controlsNormal, controlsHover, controlsPressed);
        SetupImageButton(quitImageObject, QuitGame, quitNormal, quitHover, quitPressed);
        SetupImageButton(audioSettingsImageObject, ShowAudioSettings, audioNormal, audioHover, audioPressed);
        if (canvas != null && canvas.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (controlsPanel != null)
        {
            ControlsPanel panelScript = controlsPanel.GetComponent<ControlsPanel>();
            if (panelScript != null)
            {
                panelScript.HidePanel();
            }
            else
            {
                controlsPanel.SetActive(false);
            }
        }

        // Ensure AudioManager and EventSystem are set up at runtime only
        if (Application.isPlaying)
        {
            AudioManager.GetOrCreate();
        }
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
        }

        // Play main menu music
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayTrack("MainMenu");
        }
    }

    private void OnEnable()
    {
        if (controlsPanel == null && autoCreateControlsPanel)
        {
            EnsureControlsPanel();
        }
        else if (!Application.isPlaying && showControlsPanelInEditor)
        {
            controlsPanel.SetActive(true);
        }
    }

    private void OnValidate()
    {
        if (controlsPanel == null && autoCreateControlsPanel)
        {
            EnsureControlsPanel();
        }
    }

    private void SetupImageButton(GameObject imageObj, UnityAction action, Sprite normal, Sprite hover, Sprite pressed)
    {
        if (imageObj == null)
        {
            return;
        }

        Image image = imageObj.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.raycastTarget = true;
        AnimatedMenuButton animatedButton = imageObj.GetComponent<AnimatedMenuButton>();
        if (animatedButton == null)
        {
            animatedButton = imageObj.AddComponent<AnimatedMenuButton>();
        }

        if (normal != null)
            animatedButton.normalSprite = normal;
        if (hover != null)
            animatedButton.hoverSprite = hover;
        if (pressed != null)
            animatedButton.pressedSprite = pressed;

        animatedButton.onClick.RemoveListener(action);
        animatedButton.onClick.AddListener(action);
    }

    private void EnsureAudioSettingsButton(Canvas canvas)
    {
        if (audioSettingsImageObject != null)
            return;

        if (canvas == null)
            return;

        GameObject buttonObj = new GameObject("AudioSettingsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AnimatedMenuButton));
        buttonObj.transform.SetParent(canvas.transform, false);
        audioSettingsImageObject = buttonObj;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.8f, 0.8f);
        rect.anchorMax = new Vector2(0.95f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 0.9f);

        GameObject textObj = new GameObject("AudioSettingsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(buttonObj.transform, false);
        Text text = textObj.GetComponent<Text>();
        text.text = "Sonido";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 14;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private GameObject FindRecursiveAudioButton(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;
            GameObject found = FindRecursiveAudioButton(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void EnsureControlsPanel()
    {
        if (controlsPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("No se encontró Canvas para crear el panel de controles.");
            return;
        }

        GameObject panel = new GameObject(controlsPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);
        panelImage.raycastTarget = true;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.15f);
        panelRect.anchorMax = new Vector2(0.8f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        controlsPanel = panel;

        panel.AddComponent<ControlsPanel>();

        if (Application.isPlaying)
        {
            ControlsPanel panelScript = controlsPanel.GetComponent<ControlsPanel>();
            if (panelScript != null)
            {
                panelScript.HidePanel();
            }
            else
            {
                controlsPanel.SetActive(false);
            }
        }
        else if (showControlsPanelInEditor)
        {
            controlsPanel.SetActive(true);
        }
    }

    private void CreateControlsTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("ControlsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(parent, false);
        Text titleText = titleObj.GetComponent<Text>();
        titleText.text = "Controles y mecánicas";
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.fontSize = 19;
        titleText.color = Color.white;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.82f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private void CreateControlsText(Transform parent)
    {
        GameObject textObj = new GameObject("ControlsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);
        Text controlsText = textObj.GetComponent<Text>();
        controlsText.text = "Movimiento: WASD / flechas\n" +
                    "Coger objetos: Q\n" +
                    "Abrir inventario: Tab\n" +
                    "Correr: mantener Shift\n" +
                    "Pausa / menú: Esc\n\n" +
                            "Comida: se consume cada 10 pasos, a 0 reduces velocidad\n" +
                            "Energía: correr consume energía, se recarga +1/s cuando no corres\n" +
                            "Salud: corazón muestra vida y cambia al recibir daño";
        controlsText.alignment = TextAnchor.UpperLeft;
        controlsText.fontSize = 14;
        controlsText.fontStyle = FontStyle.Bold;
        controlsText.color = Color.white;
        controlsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        controlsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        controlsText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.15f);
        textRect.anchorMax = new Vector2(0.9f, 0.82f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(HideControls);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.35f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.65f, 0.12f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.GetComponent<Text>();
        buttonText.text = "Volver";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontSize = 14;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.color = Color.white;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
