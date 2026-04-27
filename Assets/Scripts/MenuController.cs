using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject controlsPanel; // Asigna el panel de controles aquí

    [Header("Opciones automáticas")]
    public bool autoCreateControlsPanel = true;
    public string controlsPanelName = "ControlsPanel";

    [Header("Escena de juego")]
    public string gameSceneName = "GameScene";

    [Header("Imágenes del menú")]
    public GameObject playImageObject;
    public GameObject controlsImageObject;
    public GameObject quitImageObject;

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
        }
    }

    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
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
        SetupImageButton(playImageObject, PlayGame, playNormal, playHover, playPressed);
        SetupImageButton(controlsImageObject, ShowControls, controlsNormal, controlsHover, controlsPressed);
        SetupImageButton(quitImageObject, QuitGame, quitNormal, quitHover, quitPressed);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        if (canvas != null && canvas.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
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

        CreateControlsTitle(panel.transform);
        CreateControlsText(panel.transform);
        CreateCloseButton(panel.transform);

        controlsPanel.SetActive(false);
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
