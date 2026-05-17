using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private Image restartImage;
    [SerializeField] private Image mainMenuImage;
    [SerializeField] private Image quitImage;

    [SerializeField] private Sprite restartNormal;
    [SerializeField] private Sprite restartHover;
    [SerializeField] private Sprite mainMenuNormal;
    [SerializeField] private Sprite mainMenuHover;
    [SerializeField] private Sprite quitNormal;
    [SerializeField] private Sprite quitHover;

    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDied += ShowGameOver;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        SetupImageButton(restartImage, restartNormal, restartHover, RestartGame);
        SetupImageButton(mainMenuImage, mainMenuNormal, mainMenuHover, GoToMainMenu);
        SetupImageButton(quitImage, quitNormal, quitHover, QuitGame);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= ShowGameOver;
        }
    }

    private void SetupImageButton(Image image, Sprite normal, Sprite hover, UnityEngine.Events.UnityAction action)
    {
        if (image == null) return;

        image.sprite = normal;

        EventTrigger trigger = image.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        // Pointer Enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { image.sprite = hover; });
        trigger.triggers.Add(enterEntry);

        // Pointer Exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { image.sprite = normal; });
        trigger.triggers.Add(exitEntry);

        // Pointer Click
        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((data) => { action(); });
        trigger.triggers.Add(clickEntry);
    }

    private void ShowGameOver()
    {
        Debug.Log("GameOverUI: Showing Game Over");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        GameStats stats = GameStats.GetOrCreate();
        if (stats != null)
        {
            float duration = stats.GetGameDuration();
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            durationText.text = $"Duración: {minutes:00}:{seconds:00}";

            enemiesKilledText.text = $"Enemigos Asesinados: {stats.GetEnemiesKilled()}";
        }

        // Pause the game
        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        GameStats.GetOrCreate()?.RegisterGeneralRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        Application.Quit();
    }
}