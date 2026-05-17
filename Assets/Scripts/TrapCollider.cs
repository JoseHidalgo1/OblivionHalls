using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Maneja la detección de colisiones con trampas (pinchos, etc.)
/// El Tilemap de trampas debe tener TilemapCollider2D marcado como IsTrigger = true
/// para que este script pueda detectar al jugador.
/// 
/// El daño SOLO se aplica cuando el jugador está completamente dentro de la celda de la trampa,
/// no cuando está al lado o parcialmente fuera de los límites.
/// </summary>
public class TrapCollider : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int trapDamage = 1;
    [SerializeField] private float damageCheckInterval = 2f; // Evita daño constante
    [SerializeField] private Tilemap trapTilemap;

    private PlayerHealth playerHealth;
    private Transform playerTransform;
    private Collider2D playerCollider;
    private float lastDamageTime;
    private readonly System.Collections.Generic.Dictionary<int, float> lastDamageTimeByTarget = new System.Collections.Generic.Dictionary<int, float>();
    private Grid grid;
    private Vector3 lastPlayerPos;
    private BoxCollider2D[] boxColliders; // si el diseñador coloca colliders por trampa
    private EnemyController[] enemyControllers;

    void Start()
    {
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Jugador");
        }
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }

        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            }
            playerTransform = playerObj.transform;
            playerCollider = playerObj.GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                playerCollider = playerObj.GetComponentInChildren<Collider2D>();
            }
        }

        if (trapTilemap == null)
        {
            trapTilemap = GetComponent<Tilemap>();
        }

        // Buscar BoxCollider2D localizados en el GameObject de la trampa (o hijos).
        // Si existen, usamos estos colliders como el área de daño en lugar del Tilemap.
        boxColliders = GetComponentsInChildren<BoxCollider2D>(true);
        // Si no existen como hijos, intentar buscar por nombre en toda la escena (p. ej. "Traps_01...Traps_08")
        if ((boxColliders == null || boxColliders.Length == 0))
        {
            var allBoxes = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<BoxCollider2D>();
            foreach (var b in allBoxes)
            {
                if (b == null) continue;
                string n = b.gameObject.name ?? "";
                // Buscar nombres comunes que el diseñador haya usado para trampas
                if (n.StartsWith("Traps_") || n.StartsWith("Trap") || n.Contains("Trap"))
                {
                    list.Add(b);
                }
            }
            if (list.Count > 0)
            {
                boxColliders = list.ToArray();
            }
        }
        Debug.Log($"[TrapCollider] BoxColliders encontrados: {(boxColliders!=null?boxColliders.Length:0)}");

        enemyControllers = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        Debug.Log($"[TrapCollider] Enemigos encontrados: {(enemyControllers != null ? enemyControllers.Length : 0)}");

        // Auto-configurar los BoxCollider2D encontrados: marcar como trigger y añadir Rigidbody2D kinematic si hace falta
        if (boxColliders != null && boxColliders.Length > 0)
        {
            foreach (var box in boxColliders)
            {
                if (box == null) continue;
                // Marcar como trigger para que no produzcan físicas indeseadas
                if (!box.isTrigger)
                {
                    box.isTrigger = true;
                }

                // Asegurar que existe un Rigidbody2D en el mismo GameObject (o añadir uno)
                Rigidbody2D rb = box.gameObject.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = box.gameObject.AddComponent<Rigidbody2D>();
                }

                // Configurar como Kinematic mediante bodyType (isKinematic está obsoleto)
                if (rb.bodyType != RigidbodyType2D.Kinematic)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }

                // Evitar rotaciones accidentales
                rb.freezeRotation = true;
            }
        }

        grid = trapTilemap?.layoutGrid;
        // Inicializar posición previa a la posición de los PIES del jugador (para evitar saltos falsos al correr)
        if (playerTransform != null)
        {
            if (playerCollider != null)
            {
                lastPlayerPos = new Vector3(playerTransform.position.x, playerCollider.bounds.min.y + 0.01f, playerTransform.position.z);
            }
            else
            {
                lastPlayerPos = playerTransform.position;
            }
        }
        else
        {
            lastPlayerPos = Vector3.zero;
        }

        lastDamageTime = -damageCheckInterval;
        Debug.Log("[TrapCollider] Script de daño de trampas inicializado.");
    }

    /// <summary>
    /// Verifica si un collider de entidad está dentro del área de una trampa.
    /// </summary>
    private bool IsColliderOnTrap(Collider2D entityCollider)
    {
        if (entityCollider == null)
        {
            return false;
        }

        // Si existen BoxCollider2D en esta trampa, usar sus bounds para decidir daño.
        if (boxColliders != null && boxColliders.Length > 0)
        {
            Bounds entityBounds = entityCollider.bounds;
            foreach (var box in boxColliders)
            {
                if (box == null || !box.enabled)
                {
                    continue;
                }

                if (box.bounds.Intersects(entityBounds))
                {
                    return true;
                }
            }

            return false;
        }

        // Si no hay BoxCollider2D, usar la detección por Tilemap (legacy)
        if (trapTilemap == null)
        {
            return false;
        }

        Vector3 currentFeetPos;
        if (entityCollider != null)
        {
            currentFeetPos = new Vector3(entityCollider.transform.position.x, entityCollider.bounds.min.y + 0.01f, entityCollider.transform.position.z);
        }
        else
        {
            currentFeetPos = entityCollider.transform.position;
        }

        Vector3Int currentCell = trapTilemap.WorldToCell(currentFeetPos);
        Vector3Int lastCell = trapTilemap.WorldToCell(lastPlayerPos);

        if (currentCell == lastCell)
        {
            return trapTilemap.HasTile(currentCell);
        }

        int stepsX = Mathf.Abs(currentCell.x - lastCell.x);
        int stepsY = Mathf.Abs(currentCell.y - lastCell.y);
        int maxSteps = Mathf.Max(stepsX, stepsY);

        if (maxSteps == 0)
        {
            return trapTilemap.HasTile(currentCell);
        }

        for (int i = 0; i <= maxSteps; i++)
        {
            float t = maxSteps > 0 ? (float)i / maxSteps : 0f;
            Vector3 interpolated = Vector3.Lerp(lastPlayerPos, currentFeetPos, t);
            Vector3Int checkCell = trapTilemap.WorldToCell(interpolated);

            if (trapTilemap.HasTile(checkCell))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanDamageTarget(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        int id = target.GetInstanceID();
        if (lastDamageTimeByTarget.TryGetValue(id, out float lastTime))
        {
            return Time.time - lastTime >= damageCheckInterval;
        }

        return true;
    }

    private void SetLastDamageTime(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        lastDamageTimeByTarget[target.GetInstanceID()] = Time.time;
    }

    private bool TryApplyDamage(GameObject target)
    {
        if (target == null || !CanDamageTarget(target))
        {
            return false;
        }

        var playerHealthComponent = target.GetComponentInParent<PlayerHealth>();
        if (playerHealthComponent != null)
        {
            GameObject damageTarget = playerHealthComponent.gameObject;
            if (!CanDamageTarget(damageTarget))
            {
                return false;
            }
            Debug.Log($"[TrapCollider] Jugador sobre la trampa. Aplicando {trapDamage} de daño.");
            playerHealthComponent.ApplyDamage(trapDamage);
            SetLastDamageTime(damageTarget);
            return true;
        }
        var enemyController = target.GetComponentInParent<EnemyController>();
        if (enemyController != null)
        {
            GameObject damageTarget = enemyController.gameObject;
            if (!CanDamageTarget(damageTarget))
            {
                return false;
            }
            Debug.Log($"[TrapCollider] Enemigo {damageTarget.name} sobre la trampa. Aplicando {trapDamage} de daño.");
            enemyController.ReceiveDamage(trapDamage);
            SetLastDamageTime(damageTarget);
            return true;
        }

        return false;
    }

    private void ApplyDamageToEntitiesOnTrap()
    {
        if (boxColliders == null || boxColliders.Length == 0)
        {
            return;
        }

        if (playerCollider != null && IsColliderOnTrap(playerCollider) && playerHealth != null)
        {
            TryApplyDamage(playerCollider.gameObject);
        }

        if (enemyControllers != null)
        {
            foreach (var enemy in enemyControllers)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null && IsColliderOnTrap(enemyCollider))
                {
                    TryApplyDamage(enemy.gameObject);
                }
            }
        }
    }

    void Update()
    {
        if (trapTilemap == null || playerTransform == null || playerHealth == null)
        {
            return;
        }

        ApplyDamageToEntitiesOnTrap();

        // Actualizar la posición anterior con la posición de los pies para la siguiente iteración
        if (playerCollider != null)
        {
            lastPlayerPos = new Vector3(playerTransform.position.x, playerCollider.bounds.min.y + 0.01f, playerTransform.position.z);
        }
        else
        {
            lastPlayerPos = playerTransform.position;
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsColliderOnTrap(collision))
        {
            return;
        }

        TryApplyDamage(collision.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsColliderOnTrap(collision.collider))
        {
            return;
        }

        TryApplyDamage(collision.collider.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsColliderOnTrap(collision.collider))
        {
            return;
        }

        TryApplyDamage(collision.collider.gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsColliderOnTrap(collision))
        {
            return;
        }

        TryApplyDamage(collision.gameObject);
    }
}
