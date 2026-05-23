using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance { get; private set; }

    [Header("Boss Room Detection")]
    public string bossRoomName = "BossRoom"; // Name of the boss room game object or area
    private bool isInBossRoom = false;
    private bool hasPlayedBossMusic = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Play exploration music when entering GameScene
        AudioManager audioManager = AudioManager.GetOrCreate();
        if (audioManager != null && !audioManager.IsPlaying("Exploration"))
        {
            audioManager.CrossfadeTrack("Exploration", 1.5f);
        }
    }

    void Update()
    {
        // Check if player is in boss room
        CheckBossRoomStatus();
    }

    private void CheckBossRoomStatus()
    {
        // Method 1: Check if player is in a specific collider/trigger area
        // This requires the boss room to have a trigger collider with tag "BossRoom"
        // Method 2: Manual trigger from BossRoom script
    }

    /// <summary>
    /// Call this when the player enters the boss room
    /// </summary>
    public void EnterBossRoom()
    {
        if (isInBossRoom && hasPlayedBossMusic)
            return;

        isInBossRoom = true;
        hasPlayedBossMusic = true;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.CrossfadeTrack("Boss", 2f);
        }

        Debug.Log("Entered boss room - playing boss music");
    }

    /// <summary>
    /// Call this when the player leaves the boss room
    /// </summary>
    public void ExitBossRoom()
    {
        isInBossRoom = false;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.CrossfadeTrack("Exploration", 2f);
        }

        Debug.Log("Exited boss room - playing exploration music");
    }

    /// <summary>
    /// Call this when the boss is defeated
    /// </summary>
    public void PlayWinMusic()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.CrossfadeTrack("Win", 2f);
        }

        Debug.Log("Boss defeated - playing win music");
    }

    /// <summary>
    /// Call this when the player dies
    /// </summary>
    public void PlayDeathMusic()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayTrack("Death");
        }

        Debug.Log("Player died - playing death music");
    }

    public bool IsInBossRoom()
    {
        return isInBossRoom;
    }
}
