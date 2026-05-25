using UnityEngine;

/// <summary>
/// Detecta cuando el jugador pasa por una puerta específica hacia la sala del boss
/// y activa la música del boss.
/// </summary>
public class BossRoomTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool isBossRoomEntrance = false; // Marca esta puerta como entrada a la sala del boss
    [SerializeField] private bool isBossRoomExit = false; // Marca esta puerta como salida de la sala del boss
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private LeverDoor doorLeverToDeactivate;
    [SerializeField] private bool closeDoorOnTrigger = true;
    [SerializeField] private bool startWavesOnTrigger = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (isBossRoomEntrance)
        {
            GameMusicManager musicManager = GameMusicManager.Instance;
            if (musicManager != null)
            {
                musicManager.EnterBossRoom();
            }

            if (closeDoorOnTrigger && doorLeverToDeactivate != null)
            {
                doorLeverToDeactivate.DeactivateLever();
            }

            if (startWavesOnTrigger && waveSpawner != null)
            {
                waveSpawner.StartWaves();
            }
        }
        else if (isBossRoomExit)
        {
            GameMusicManager musicManager = GameMusicManager.Instance;
            if (musicManager != null)
            {
                musicManager.ExitBossRoom();
            }
        }
    }
}
