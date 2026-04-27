using UnityEngine;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    private float gameStartTime;
    private int enemiesKilled;
    private int generalRestartCount;
    private int healthHealCount;
    private int foodRestoreCount;
    private bool bossDefeated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameStartTime = Time.time;
    }

    public static GameStats GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject statsObject = new GameObject("GameStats");
        Instance = statsObject.AddComponent<GameStats>();
        DontDestroyOnLoad(statsObject);
        Instance.gameStartTime = Time.time;
        return Instance;
    }

    public void EnemyKilled()
    {
        enemiesKilled++;
    }

    public void RegisterGeneralRestart()
    {
        generalRestartCount++;
    }

    public void RecordHealthHeal()
    {
        healthHealCount++;
    }

    public void RecordFoodRestore()
    {
        foodRestoreCount++;
    }

    public void RegisterBossDefeat()
    {
        bossDefeated = true;
    }

    public float GetGameDuration()
    {
        return Time.time - gameStartTime;
    }

    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }

    public int GetHealsPerformed()
    {
        return healthHealCount + foodRestoreCount;
    }

    public int GetGeneralRestartCount()
    {
        return generalRestartCount;
    }

    public bool HasBossBeenDefeated()
    {
        return bossDefeated;
    }

    public void ResetStats(bool resetGeneralRestarts = true)
    {
        gameStartTime = Time.time;
        enemiesKilled = 0;
        healthHealCount = 0;
        foodRestoreCount = 0;
        bossDefeated = false;
        if (resetGeneralRestarts)
        {
            generalRestartCount = 0;
        }
    }
}