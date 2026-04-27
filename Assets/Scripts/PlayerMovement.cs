using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;

    [Header("Ataque")]
    public Key attackKey = Key.E;
    public int attackDamage = 1;
    public float attackRange = 1.1f;

    [Header("Animación")]
    public Animator animator;

    [Header("Visual")]
    [SerializeField] private bool forceVisibleSpriteStyle = true;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private FacingDirection facingDirection = FacingDirection.Front;
    private string currentAnimation;
    private PlayerFoodEnergy foodEnergy;

    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private Coroutine hurtFallbackCoroutine;

    [Header("Fallback de animación")]
    [SerializeField] private float hurtFallbackDuration = 0.35f;

    public bool IsDead => isDead;
    public bool CanMove => !isDead && !isAttacking && !isHurt;

    private enum FacingDirection
    {
        Back,
        Front,
        Left,
        Right
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        foodEnergy = GetComponent<PlayerFoodEnergy>();

        if (forceVisibleSpriteStyle)
        {
            EnsureVisibleSprites();
        }
    }

    private void EnsureVisibleSprites()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        Shader spriteDefaultShader = Shader.Find("Sprites/Default");

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[index];
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.color = Color.white;

            if (spriteDefaultShader != null)
            {
                Material currentMaterial = spriteRenderer.sharedMaterial;
                bool needsDefaultMaterial = currentMaterial == null
                    || currentMaterial.shader == null
                    || currentMaterial.shader.name != "Sprites/Default";

                if (needsDefaultMaterial)
                {
                    spriteRenderer.material = new Material(spriteDefaultShader);
                }
            }
        }
    }

    void Update()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            // Solo reproducir Death si no está bloqueada
            if (!deathLocked || !currentAnimation.StartsWith("Death"))
                PlayDirectionalAnimation("Death");
            // Nunca volver a reproducir Death si ya está bloqueada
            return;
        }

        movementInput = GetMovementInput();
        if (movementInput.sqrMagnitude > 0.0001f)
        {
            UpdateFacingDirection(movementInput);
        }

        if (!isAttacking && !isHurt && Keyboard.current != null && Keyboard.current[attackKey].wasPressedThisFrame)
        {
            IniciarAtaque();
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (isDead || isAttacking || isHurt)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool shiftHeld = false;
        if (Keyboard.current != null)
        {
            shiftHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }

        float effectiveSpeed = moveSpeed;
        if (foodEnergy != null)
        {
            effectiveSpeed = foodEnergy.GetEffectiveSpeed(moveSpeed, movementInput);
        }

        rb.linearVelocity = movementInput * effectiveSpeed;

        if (foodEnergy != null)
        {
            foodEnergy.RegisterMovement(movementInput, effectiveSpeed, Time.fixedDeltaTime, shiftHeld);
        }
    }

    private Vector2 GetMovementInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float horizontal = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                           - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f)
                         - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

        Vector2 input = new Vector2(horizontal, vertical);
        return input.normalized;
    }

    private void UpdateFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            facingDirection = direction.x >= 0f ? FacingDirection.Right : FacingDirection.Left;
            return;
        }

        facingDirection = direction.y >= 0f ? FacingDirection.Back : FacingDirection.Front;
    }

    private void UpdateAnimation()
    {
        if (isDead)
        {
            // No volver a reproducir Death si ya está
            if (!currentAnimation.StartsWith("Death"))
                PlayDirectionalAnimation("Death");
            return;
        }

        if (isAttacking)
        {
            PlayDirectionalAnimation("Attack");
            return;
        }

        if (isHurt)
        {
            // No volver a reproducir Hurt si ya está
            if (!currentAnimation.StartsWith("Hurt"))
                PlayDirectionalAnimation("Hurt");
            return;
        }

        if (movementInput.sqrMagnitude > 0.0001f)
        {
            PlayDirectionalAnimation("Walk");
            return;
        }

        PlayDirectionalAnimation("Idle");
    }

    private void PlayDirectionalAnimation(string baseState)
    {
        if (animator == null || !animator.enabled)
            return;

        // Si está muerto, nunca reproducir ninguna animación
        if (isDead)
            return;

        string stateName = baseState + facingDirection;
        if (stateName == currentAnimation)
            return;
        animator.Play(stateName);
        currentAnimation = stateName;
    }

    public void IniciarAtaque()
    {
        if (isDead)
        {
            return;
        }

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        PlayDirectionalAnimation("Attack");
    }

    public void AttackHit()
    {
        if (isDead)
        {
            return;
        }

        Vector2 forward = GetFacingVector();
        Vector2 attackCenter = (Vector2)transform.position + forward * (attackRange * 0.6f);

        // Detección directa por componente, sin LayerMask
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange);
        for (int index = 0; index < hits.Length; index++)
        {
            if (hits[index].gameObject == gameObject)
            {
                continue;
            }

            EnemyController enemy = hits[index].GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.ReceiveDamage(attackDamage);
            }
        }
    }


    private bool hurtLocked = false; // Evita encadenar Hurt

    public void RecibirDano()
    {
        if (isDead || isHurt || hurtLocked)
        {
            return;
        }

        isHurt = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        PlayDirectionalAnimation("Hurt");
        hurtLocked = true; // Bloquea hasta que termine la animación

        // Fallback: si no llega el Animation Event FinHurt, se desbloquea solo
        if (hurtFallbackCoroutine != null)
        {
            StopCoroutine(hurtFallbackCoroutine);
        }
        hurtFallbackCoroutine = StartCoroutine(HurtFallbackRoutine());
    }

    private System.Collections.IEnumerator HurtFallbackRoutine()
    {
        yield return new WaitForSeconds(hurtFallbackDuration);

        if (!isDead && isHurt)
        {
            isHurt = false;
            hurtLocked = false;
            UpdateAnimation();
        }

        hurtFallbackCoroutine = null;
    }


    public event System.Action OnDeathAnimationFinished;

    private bool deathLocked = false; // Evita repetir Death
    private bool deathAnimationFinished = false;

    public void Morir()
    {
        if (deathLocked) return;
        isDead = true;
        isAttacking = false;
        isHurt = false;
        deathLocked = true;
        rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.enabled = true;
            string deathState = "Death" + facingDirection;
            animator.Play(deathState, 0, 0f);
            currentAnimation = deathState;
            // Fallback: deshabilita el Animator tras la duración del clip
            StartCoroutine(DisableAnimatorAfterDeath());
        }
    }

    private System.Collections.IEnumerator DisableAnimatorAfterDeath()
    {
        // Espera un frame para que el estado Death empiece
        yield return null;
        if (animator == null) yield break;

        float timeout = 1.6f; // Duración del clip + margen (clips duran ~1.40s)
        float elapsed = 0f;

        // Espera hasta que el clip termine O se agote el timeout
        // (si el clip tiene Loop Time activo, normalizedTime nunca llega a 1)
        while (elapsed < timeout)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.loop && info.normalizedTime >= 0.99f)
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Congela en el último frame deshabilitando el Animator
        if (animator != null)
            animator.enabled = false;
        if (!deathAnimationFinished)
        {
            deathAnimationFinished = true;
            Debug.Log("Death animation finished via fallback, invoking OnDeathAnimationFinished");
            OnDeathAnimationFinished?.Invoke();
        }
    }


    // Llamar desde Animation Event al final del clip Hurt
    public void FinHurt()
    {
        if (hurtFallbackCoroutine != null)
        {
            StopCoroutine(hurtFallbackCoroutine);
            hurtFallbackCoroutine = null;
        }

        isHurt = false;
        hurtLocked = false;
        UpdateAnimation();
    }


    // Llamar desde Animation Event al final del clip Attack
    public void FinAtaque()
    {
        isAttacking = false;
        UpdateAnimation();
    }

    // Llamar desde Animation Event al final del clip Death (opcional, el fallback ya lo maneja)
    public void FinDeath()
    {
        rb.linearVelocity = Vector2.zero;
        if (animator != null)
            animator.enabled = false;
        if (!deathAnimationFinished)
        {
            deathAnimationFinished = true;
            Debug.Log("Death animation finished via event, invoking OnDeathAnimationFinished");
            OnDeathAnimationFinished?.Invoke();
        }
    }

    private Vector2 GetFacingVector()
    {
        switch (facingDirection)
        {
            case FacingDirection.Back:
                return Vector2.up;
            case FacingDirection.Left:
                return Vector2.left;
            case FacingDirection.Right:
                return Vector2.right;
            default:
                return Vector2.down;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 forward = GetFacingVector();
        Vector2 attackCenter = (Vector2)transform.position + forward * (attackRange * 0.6f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackCenter, attackRange);
    }
}
