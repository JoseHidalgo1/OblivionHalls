using UnityEngine;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    private float gameStartTime;
    private int enemiesKilled;

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

    public void EnemyKilled()
    {
        enemiesKilled++;
    }

    public float GetGameDuration()
    {
        return Time.time - gameStartTime;
    }

    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }

    public void ResetStats()
    {
        gameStartTime = Time.time;
        enemiesKilled = 0;
    }
}