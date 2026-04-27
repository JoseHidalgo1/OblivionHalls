using System.Collections;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool invulnerableOnDamage = true;
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    private int currentHealth;
    private bool isDead;
    private bool isInvulnerable;

    private PlayerMovement playerMovement;

    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnDamaged;
    public event Action OnDied;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponentInChildren<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.OnDeathAnimationFinished += HandleDeathAnimationFinished;
        }

        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || isDead || isInvulnerable)
        {
            Debug.Log($"[PlayerHealth] Daño ignorado. amount={amount}, isDead={isDead}, isInvulnerable={isInvulnerable}");
            return;
        }

        Debug.Log($"[PlayerHealth] Recibe daño: {amount}. Vida antes: {currentHealth}/{maxHealth}");
        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"[PlayerHealth] Vida después: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            OnDamaged?.Invoke(currentHealth, maxHealth);
            Debug.Log("[PlayerHealth] Vida agotada. Ejecutando muerte.");
            Die();
            return;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke(currentHealth, maxHealth);

        if (playerMovement != null)
        {
            Debug.Log("[PlayerHealth] Activando animación de daño en PlayerMovement.");
            playerMovement.RecibirDano();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No se encontró PlayerMovement para reproducir Hurt.");
        }

        if (invulnerableOnDamage && invulnerabilityDuration > 0f)
        {
            Debug.Log($"[PlayerHealth] Inicia invulnerabilidad por {invulnerabilityDuration:0.00}s");
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        GameStats.GetOrCreate()?.RecordHealthHeal();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log("[PlayerHealth] Jugador murió.");

        if (playerMovement != null)
        {
            playerMovement.Morir();
        }
    }

    private void HandleDeathAnimationFinished()
    {
        Debug.Log("PlayerHealth: Invoking OnDied");
        OnDied?.Invoke();
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }
}
