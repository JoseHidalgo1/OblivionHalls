using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI healsText;
    [SerializeField] private TextMeshProUGUI generalRestartsText;
    [SerializeField] private Image nextLevelImage;
    [SerializeField] private Image mainMenuImage;
    [SerializeField] private Image quitImage;
    [Header("Scene Flow")]
    [SerializeField] private string nextLevelSceneName;
    [SerializeField] private string finalLevelSceneName = "MainMenu";

    [SerializeField] private Sprite nextLevelNormal;
    [SerializeField] private Sprite nextLevelHover;
    [SerializeField] private Sprite mainMenuNormal;
    [SerializeField] private Sprite mainMenuHover;
    [SerializeField] private Sprite quitNormal;
    [SerializeField] private Sprite quitHover;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        SetupImageButton(nextLevelImage, nextLevelNormal, nextLevelHover, GoToNextLevel);
        SetupImageButton(mainMenuImage, mainMenuNormal, mainMenuHover, GoToMainMenu);
        SetupImageButton(quitImage, quitNormal, quitHover, QuitGame);
    }

    private void SetupImageButton(Image image, Sprite normal, Sprite hover, UnityEngine.Events.UnityAction action)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = normal;
        image.raycastTarget = true;

        EventTrigger trigger = image.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => { image.sprite = hover; });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => { image.sprite = normal; });
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener((data) => { action(); });
        trigger.triggers.Add(clickEntry);
    }

    public void ShowLevelComplete()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // Play win music
        GameMusicManager musicManager = GameMusicManager.Instance;
        if (musicManager != null)
        {
            musicManager.PlayWinMusic();
        }

        GameStats stats = GameStats.GetOrCreate();
        if (stats != null)
        {
            float duration = stats.GetGameDuration();
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            durationText.text = $"Duración: {minutes:00}:{seconds:00}";
            enemiesKilledText.text = $"Enemigos Asesinados: {stats.GetEnemiesKilled()}";
            healsText.text = $"Curaciones realizadas: {stats.GetHealsPerformed()}";
            generalRestartsText.text = $"Reinicios generales: {stats.GetGeneralRestartCount()}";
        }

        Time.timeScale = 0f;
    }

    private void GoToNextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(nextLevelSceneName) && IsSceneInBuildSettings(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            return;
        }

        SceneManager.LoadScene(finalLevelSceneName);
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

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        GameStats.GetOrCreate()?.ResetStats(true);
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        GameStats.GetOrCreate()?.ResetStats(true);
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
