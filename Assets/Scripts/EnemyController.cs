using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackStartRange = 1.6f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackOffset = 0.7f;
    [SerializeField] private bool spriteFacesRightByDefault = false;
    [SerializeField] private bool invertSpriteFacing = false;

    [Header("Ataque")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackWindupRatio = 0.45f;
    [SerializeField] private float attackActiveDuration = 0.2f;
    [SerializeField] private float attackDamageCheckInterval = 0.05f;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;

    [Header("Animación")]
    [SerializeField] private Animator animator;

    [Header("Render")]
    [SerializeField] private bool forceSortingOverMap = true;
    [SerializeField] private string enemySortingLayer = "Player";
    [SerializeField] private int enemySortingOrder = 1;
    [SerializeField] private bool forceVisibleSpriteStyle = true;

    [Header("Loot")]
    [SerializeField] private bool dropLootOnDeath = true;
    [SerializeField, Range(0f, 1f)] private float lootDropChance = 0.6f;
    [SerializeField] private Item medicinaItem;
    [SerializeField] private Item pastillasItem;
    [SerializeField] private bool isBossEnemy = false;
    [SerializeField] private float levelCompleteDelay = 10f;
    [SerializeField] private GameObject bossCountdownPrefab;
    [SerializeField] private Vector3 bossCountdownOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float lootSpawnRadius = 0.25f;
    [SerializeField] private string lootSortingLayer = "Background";
    [SerializeField] private int lootSortingOrder = -1;

    private Rigidbody2D rb;
    private float lastAttackTime;
    private int currentHealth;
    private Coroutine attackCoroutine;
    private PlayerHealth targetHealth;

    private bool isAttacking;
    private bool isHit;
    private bool isDead;
    private bool lootDropped;
    private bool attackDamageApplied;
    private Vector2 facingDirection = Vector2.right;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        currentHealth = maxHealth;
    }

    void Start()
    {
        if (forceSortingOverMap)
        {
            ApplyEnemySorting();
        }

        ResolveSpriteFacingOverride();

        if (forceVisibleSpriteStyle)
        {
            EnsureVisibleSprites();
        }

        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                target = playerObject.transform;
            }
        }

        targetHealth = GetPlayerHealth(target);

        PlayAnimation("EnemigoIdle");
    }

    void OnValidate()
    {
        if (forceSortingOverMap)
        {
            ApplyEnemySorting();
        }

        if (forceVisibleSpriteStyle)
        {
            EnsureVisibleSprites();
        }
    }

    private void ApplyEnemySorting()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            SpriteRenderer rendererRef = renderers[index];
            if (rendererRef == null)
            {
                continue;
            }

            rendererRef.sortingLayerName = enemySortingLayer;
            rendererRef.sortingOrder = enemySortingOrder;
        }
    }

    private void EnsureVisibleSprites()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Shader spriteDefaultShader = Shader.Find("Sprites/Default");

        for (int index = 0; index < renderers.Length; index++)
        {
            SpriteRenderer rendererRef = renderers[index];
            if (rendererRef == null)
            {
                continue;
            }

            rendererRef.color = Color.white;

            if (spriteDefaultShader != null)
            {
                Material currentMaterial = rendererRef.sharedMaterial;
                bool needsDefaultMaterial = currentMaterial == null
                    || currentMaterial.shader == null
                    || currentMaterial.shader.name != "Sprites/Default";

                if (needsDefaultMaterial)
                {
                    rendererRef.material = new Material(spriteDefaultShader);
                }
            }
        }
    }

    void Update()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnimation("EnemigoDie");
            return;
        }

        // Si el jugador está muerto, dejar de atacar y quedarse quieto
        if (targetHealth != null && targetHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            isAttacking = false;
            attackDamageApplied = false;
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
            PlayAnimation("EnemigoIdle");
            return;
        }

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnimation("EnemigoIdle");
            return;
        }

        if (isHit)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 directionToTarget = (Vector2)target.position - (Vector2)transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        if (distanceToTarget > 0.001f)
        {
            facingDirection = directionToTarget.normalized;
            UpdateFacing(directionToTarget);
        }

        if (distanceToTarget > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnimation("EnemigoIdle");
            return;
        }

        if (distanceToTarget <= attackStartRange)
        {
            rb.linearVelocity = Vector2.zero;
            TryStartAttack();
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = directionToTarget.normalized;
        rb.linearVelocity = direction * moveSpeed;

        PlayAnimation("EnemigoRun");
    }

    private void UpdateFacing(Vector2 directionToTarget)
    {
        if (Mathf.Abs(directionToTarget.x) < 0.01f)
        {
            return;
        }

        float facingSign = directionToTarget.x >= 0f ? 1f : -1f;
        bool effectiveSpriteFacesRightByDefault = spriteFacesRightByDefault ^ invertSpriteFacing;
        if (!effectiveSpriteFacesRightByDefault)
        {
            facingSign *= -1f;
        }

        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * facingSign;
        transform.localScale = localScale;
    }

    private void ResolveSpriteFacingOverride()
    {
        string enemyName = name.ToLowerInvariant();
        if (
            enemyName.Contains("minotauro") ||
            enemyName.Contains("calabaza") ||
            enemyName.Contains("cyclope") ||
            enemyName.Contains("lobo") ||
            enemyName.Contains("espiritu") ||
            enemyName.Contains("esqueleto") ||
            enemyName.Contains("murcielago") ||
            enemyName.Contains("centauro") ||
            enemyName.Contains("zombie") ||
            enemyName.Contains("slimeazul") ||
            enemyName.Contains("slimemadrezul") ||
            enemyName.Contains("huargo")
        )
        {
            invertSpriteFacing = true;
        }
    }

    private void TryStartAttack()
    {
        if (isAttacking || isDead || isHit)
        {
            return;
        }

        if (Time.time < lastAttackTime + attackCooldown)
        {
            PlayAnimation("EnemigoIdle");
            return;
        }

        isAttacking = true;
        attackDamageApplied = false;
        lastAttackTime = Time.time;

        Debug.Log($"[Enemy] {name} inicia ataque. Distancia al jugador: {Vector2.Distance(transform.position, target.position):0.00}");

        PlayAnimation("EnemigoAttack");
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (!isDead && target != null)
        {
            Vector2 directionToTarget = (Vector2)target.position - (Vector2)transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > attackStartRange)
            {
                break;
            }

            if (distanceToTarget > 0.001f)
            {
                facingDirection = directionToTarget.normalized;
                UpdateFacing(directionToTarget);
            }

            PlayAnimation("EnemigoAttack");

            attackDamageApplied = false;
            float windupTime = Mathf.Max(0.01f, attackCooldown * attackWindupRatio);
            yield return new WaitForSeconds(windupTime);

            float elapsed = 0f;
            while (elapsed < attackActiveDuration && !isDead)
            {
                AttackHit();
                if (attackDamageApplied)
                {
                    break;
                }

                yield return new WaitForSeconds(attackDamageCheckInterval);
                elapsed += attackDamageCheckInterval;
            }

            float recoveryTime = Mathf.Max(0.01f, attackCooldown - windupTime);
            yield return new WaitForSeconds(recoveryTime);
        }

        attackCoroutine = null;
        isAttacking = false;
        attackDamageApplied = false;

        if (!isDead && !isHit)
        {
            PlayAnimation("EnemigoIdle");
        }
    }

    public void AttackHit()
    {
        if (isDead || attackDamageApplied)
        {
            return;
        }

        if (targetHealth == null)
        {
            targetHealth = GetPlayerHealth(target);
            Debug.Log($"[Enemy] {name} busca PlayerHealth: {(targetHealth != null ? "ENCONTRADO" : "NO ENCONTRADO")}");
        }

        if (targetHealth == null || targetHealth.IsDead)
        {
            Debug.Log($"[Enemy] {name} no puede dañar: PlayerHealth nulo o jugador muerto.");
            return;
        }

        Vector2 attackOrigin = (Vector2)transform.position + GetAttackDirection() * attackOffset;
        float distanceToTarget = Vector2.Distance(attackOrigin, target.position);
        Debug.Log($"[Enemy] {name} intenta golpear. Distancia desde punto de ataque: {distanceToTarget:0.00}. Rango efectivo: {(attackRange + 0.35f):0.00}");
        if (distanceToTarget <= attackRange + 0.35f)
        {
            Debug.Log($"[Enemy] {name} aplica {attackDamage} de daño al jugador.");
            targetHealth.ApplyDamage(attackDamage);
            attackDamageApplied = true;
        }
        else
        {
            Debug.Log($"[Enemy] {name} falla el golpe: jugador fuera del rango del punto de ataque.");
        }
    }

    // Llamar desde Animation Event al final del clip de ataque enemigo
    public void FinAtaqueEnemigo()
    {
        isAttacking = false;
        attackDamageApplied = false;

        if (!isDead && !isHit)
        {
            PlayAnimation("EnemigoIdle");
        }
    }

    public void ReceiveDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        isHit = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        PlayAnimation("EnemigoHit");
    }

    public void FinHitEnemigo()
    {
        isHit = false;

        if (!isDead)
        {
            PlayAnimation("EnemigoIdle");
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        isHit = false;
        rb.linearVelocity = Vector2.zero;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (isBossEnemy)
        {
            GameStats.GetOrCreate()?.RegisterBossDefeat();
            StartCoroutine(DelayedLevelComplete());
        }

        GameStats.GetOrCreate()?.EnemyKilled();
        PlayAnimation("EnemigoDie");
        TryDropLoot();
    }

    private IEnumerator DelayedLevelComplete()
    {
        float remainingTime = Mathf.Max(0f, levelCompleteDelay);
        GameObject counterObject = null;
        TextMeshPro counterText = null;

        if (bossCountdownPrefab != null)
        {
            counterObject = Instantiate(bossCountdownPrefab, transform.position + bossCountdownOffset, Quaternion.identity);
            counterText = counterObject.GetComponent<TextMeshPro>();
            if (counterText == null)
            {
                counterText = counterObject.GetComponentInChildren<TextMeshPro>();
            }
        }

        while (remainingTime > 0f)
        {
            if (counterText != null)
            {
                int secondsLeft = Mathf.CeilToInt(remainingTime);
                counterText.text = $"Nivel completado en {secondsLeft}s";
            }
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (counterText != null)
        {
            counterText.text = "Nivel completado!";
        }

        if (LevelCompleteUI.Instance != null)
        {
            LevelCompleteUI.Instance.ShowLevelComplete();
        }

        if (counterObject != null)
        {
            Destroy(counterObject, 2f);
        }
    }

    private void TryDropLoot()
    {
        if (!dropLootOnDeath || lootDropped)
        {
            return;
        }

        if (Random.value > lootDropChance)
        {
            return;
        }

        Item lootItem = PickLootItem();
        if (lootItem == null)
        {
            return;
        }

        Vector2 randomOffset = Random.insideUnitCircle * Mathf.Max(0f, lootSpawnRadius);
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

        GameObject dropped = new GameObject("Drop_" + lootItem.itemName);
        dropped.transform.position = spawnPosition;
        dropped.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        DroppedItemWorld droppedData = dropped.AddComponent<DroppedItemWorld>();
        droppedData.SetItem(lootItem);

        SpriteRenderer rendererRef = dropped.AddComponent<SpriteRenderer>();
        rendererRef.sprite = lootItem.icon;
        rendererRef.sortingLayerName = lootSortingLayer;
        rendererRef.sortingOrder = lootSortingOrder;

        CircleCollider2D circleCollider = dropped.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        lootDropped = true;
    }

    private Item PickLootItem()
    {
        bool hasMedicina = medicinaItem != null;
        bool hasPastillas = pastillasItem != null;

        if (!hasMedicina && !hasPastillas)
        {
            return null;
        }

        if (hasMedicina && !hasPastillas)
        {
            return medicinaItem;
        }

        if (!hasMedicina)
        {
            return pastillasItem;
        }

        return Random.value < 0.5f ? medicinaItem : pastillasItem;
    }

    private string currentAnimation;

    private Vector2 GetAttackDirection()
    {
        if (facingDirection.sqrMagnitude <= 0.001f)
        {
            return Vector2.right;
        }

        return facingDirection.normalized;
    }

    private PlayerHealth GetPlayerHealth(Transform source)
    {
        if (source == null)
        {
            return null;
        }

        PlayerHealth playerHealth = source.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth;
        }

        playerHealth = source.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth;
        }

        return source.GetComponentInChildren<PlayerHealth>();
    }

    private void PlayAnimation(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        // No reiniciar EnemigoDie si ya está reproduciéndose
        if (stateName == currentAnimation && (stateName == "EnemigoDie" || stateName == "DeathFront" || stateName == "DeathBack" || stateName == "DeathLeft" || stateName == "DeathRight"))
        {
            return;
        }

        currentAnimation = stateName;
        animator.Play(stateName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackStartRange);

        Gizmos.color = Color.red;
        Vector2 attackOrigin = (Vector2)transform.position + GetAttackDirection() * attackOffset;
        Gizmos.DrawWireSphere(attackOrigin, attackRange);
    }
}
