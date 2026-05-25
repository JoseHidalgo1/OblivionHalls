using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestiona el spawning de oleadas de enemigos cuando una puerta se cierra.
/// Se activa cuando el jugador pasa por la puerta abierta.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public string enemyPrefabName;
        public GameObject enemyPrefab;
        public int count = 1;
    }

    [System.Serializable]
    public class EnemyPrefabMapping
    {
        public string enemyName;
        public GameObject prefab;
    }

    [System.Serializable]
    public class EnemyWave
    {
        public string waveName;
        public List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();
    }

    [Header("Configuración de Oleadas")]
    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
    
    [Header("Spawn")]
    [SerializeField] private Transform spawnPointsContainer; // Padre que contiene puntos de spawn (vacío o con transforms hijos)
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero; // Si no hay puntos de spawn, usa transform.position cuando sea Vector3.zero
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(5f, 5f, 0f); // Área donde aparecer enemigos
    [SerializeField] private GameObject enemyPrefab; // Prefab genérico si no se encuentran por nombre
    [SerializeField] private bool useFallbackPrefab = false;
    [SerializeField] private List<EnemyPrefabMapping> enemyPrefabMappings = new List<EnemyPrefabMapping>();
    
    [Header("Timing")]
    [SerializeField] private float delayBetweenWaves = 2f;
    [SerializeField] private float delayBeforeLevelComplete = 5f;
    [SerializeField] private float checkIntervalForWaveCompletion = 0.5f;
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool isSpawningWaves = false;

    void Start()
    {
        ValidateWavesConfiguration();
    }

    private void ValidateWavesConfiguration()
    {
        bool hasValidWave = false;

        foreach (EnemyWave wave in waves)
        {
            if (wave == null || wave.enemyEntries == null)
            {
                continue;
            }

            foreach (EnemySpawnEntry entry in wave.enemyEntries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.enemyPrefabName) && entry.count > 0)
                {
                    hasValidWave = true;
                    break;
                }
            }

            if (hasValidWave)
            {
                break;
            }
        }

        if (!hasValidWave)
        {
            SetupDefaultWaves();
            return;
        }

        for (int i = waves.Count - 1; i >= 0; i--)
        {
            EnemyWave wave = waves[i];
            if (wave == null || wave.enemyEntries == null || wave.enemyEntries.Count == 0)
            {
                waves.RemoveAt(i);
                continue;
            }

            for (int j = wave.enemyEntries.Count - 1; j >= 0; j--)
            {
                EnemySpawnEntry entry = wave.enemyEntries[j];
                if (entry == null || string.IsNullOrWhiteSpace(entry.enemyPrefabName) || entry.count <= 0)
                {
                    wave.enemyEntries.RemoveAt(j);
                }
            }

            if (wave.enemyEntries.Count == 0)
            {
                waves.RemoveAt(i);
            }
        }

        if (waves.Count == 0)
        {
            SetupDefaultWaves();
        }
    }

    private void SetupDefaultWaves()
    {
        waves.Clear();

        EnemyWave firstWave = new EnemyWave { waveName = "Centauros" };
        firstWave.enemyEntries.Add(new EnemySpawnEntry { enemyPrefabName = "Centauro", count = 2 });
        waves.Add(firstWave);

        EnemyWave secondWave = new EnemyWave { waveName = "Huargos" };
        secondWave.enemyEntries.Add(new EnemySpawnEntry { enemyPrefabName = "Huargo", count = 3 });
        waves.Add(secondWave);

        EnemyWave thirdWave = new EnemyWave { waveName = "Esqueletos y Zombies" };
        thirdWave.enemyEntries.Add(new EnemySpawnEntry { enemyPrefabName = "Esqueleto", count = 2 });
        thirdWave.enemyEntries.Add(new EnemySpawnEntry { enemyPrefabName = "Zombie", count = 2 });
        waves.Add(thirdWave);
    }

    /// <summary>
    /// Inicia el spawning de todas las oleadas.
    /// Llamar esto cuando la puerta se cierre.
    /// </summary>
    public void StartWaves()
    {
        if (isSpawningWaves)
        {
            return;
        }

        StartCoroutine(SpawnWavesCoroutine());
    }

    private IEnumerator SpawnWavesCoroutine()
    {
        isSpawningWaves = true;
        spawnedEnemies.Clear();

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            EnemyWave wave = waves[waveIndex];
            Debug.Log($"[WaveSpawner] Starting oleada {waveIndex + 1}: {wave.waveName}");

            foreach (EnemySpawnEntry entry in wave.enemyEntries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnEnemy(entry);
                    yield return new WaitForSeconds(0.2f);
                }
            }

            // Esperar a que se maten todos los enemigos de esta oleada
            yield return StartCoroutine(WaitForWaveCompletion());

            // Delay antes de la próxima oleada (excepto después de la última)
            if (waveIndex < waves.Count - 1)
            {
                Debug.Log($"[WaveSpawner] Wave {waveIndex + 1} completada. Esperando {delayBetweenWaves}s para la próxima...");
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        Debug.Log("[WaveSpawner] Todas las oleadas completadas. Esperando antes de mostrar panel de nivel completado...");
        yield return new WaitForSeconds(delayBeforeLevelComplete);

        // Mostrar panel de nivel completado
        if (LevelCompleteUI.Instance != null)
        {
            LevelCompleteUI.Instance.ShowLevelComplete();
        }

        isSpawningWaves = false;
    }

    private void SpawnEnemy(EnemySpawnEntry entry)
    {
        GameObject enemyInstance = null;

        if (entry == null)
        {
            Debug.LogWarning("[WaveSpawner] Entrada de enemy spawn nula.");
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.enemyPrefabName) && entry.enemyPrefab == null)
        {
            Debug.LogWarning("[WaveSpawner] Entrada de spawn sin prefab ni nombre.");
            return;
        }

        GameObject prefab = entry.enemyPrefab;
        if (prefab == null)
        {
            prefab = FindEnemyPrefab(entry.enemyPrefabName);
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>($"Enemies/{entry.enemyPrefabName}");
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>($"Prefabs/{entry.enemyPrefabName}");
        }
        if (prefab == null && enemyPrefab != null && useFallbackPrefab)
        {
            prefab = enemyPrefab;
        }

        if (prefab != null)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            enemyInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"[WaveSpawner] No se encontró prefab para el enemigo: {entry.enemyPrefabName}");
            return;
        }

        if (enemyInstance != null)
        {
            // Forzar vida 1 si tiene EnemyController
            var ec = enemyInstance.GetComponent<EnemyController>();
            if (ec != null)
            {
                var maxHealthField = enemyInstance.GetType().GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (maxHealthField != null)
                {
                    maxHealthField.SetValue(ec, 1);
                }
                var currentHealthField = enemyInstance.GetType().GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentHealthField != null)
                {
                    currentHealthField.SetValue(ec, 1);
                }
            }
            spawnedEnemies.Add(enemyInstance);
            Debug.Log($"[WaveSpawner] Enemigo spawneado: {entry.enemyPrefabName} en {enemyInstance.transform.position} (vida 1)");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        // Si hay puntos de spawn específicos, usar uno aleatorio
        if (spawnPointsContainer != null && spawnPointsContainer.childCount > 0)
        {
            Transform randomSpawnPoint = spawnPointsContainer.GetChild(Random.Range(0, spawnPointsContainer.childCount));
            return randomSpawnPoint.position;
        }

        // Si no, usar el área de spawn
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
            Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f),
            0f
        );

        Vector3 center = spawnAreaCenter;
        if (center == Vector3.zero)
        {
            center = transform.position;
        }
        return center + randomOffset;
    }

    private GameObject FindEnemyPrefab(string enemyName)
    {
        if (enemyPrefabMappings == null || enemyPrefabMappings.Count == 0)
        {
            return null;
        }

        foreach (EnemyPrefabMapping mapping in enemyPrefabMappings)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.enemyName) || mapping.prefab == null)
            {
                continue;
            }

            if (string.Equals(mapping.enemyName, enemyName, System.StringComparison.OrdinalIgnoreCase))
            {
                return mapping.prefab;
            }
        }

        return null;
    }

    private IEnumerator WaitForWaveCompletion()
    {
        while (spawnedEnemies.Count > 0)
        {
            // Limpiar referencias a enemigos destruidos o muertos
            spawnedEnemies.RemoveAll(enemy =>
            {
                if (enemy == null)
                {
                    return true;
                }

                EnemyController enemyController = enemy.GetComponent<EnemyController>();
                if (enemyController != null && enemyController.IsDead)
                {
                    return true;
                }

                return false;
            });

            if (spawnedEnemies.Count == 0)
            {
                break;
            }

            Debug.Log($"[WaveSpawner] Esperando: {spawnedEnemies.Count} enemigos aún vivos");
            yield return new WaitForSeconds(checkIntervalForWaveCompletion);
        }

        Debug.Log("[WaveSpawner] Oleada completada");
    }

    void OnDrawGizmos()
    {
        // Visualizar el área de spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}
