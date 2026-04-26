using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeartHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image heartImage;

    [Header("Sprites (6 frames)")]
    [SerializeField] private Sprite[] corazonFrames = new Sprite[6];
    [SerializeField] private Sprite[] corazonParticulasFrames = new Sprite[6];

    [Header("Feedback de daño")]
    [SerializeField] private float particulasDuration = 0.2f;

    [Header("Posición fija en pantalla")]
    [SerializeField] private bool forceTopLeft = true;
    [SerializeField] private Vector2 topLeftOffset = new Vector2(24f, -24f);

    private int currentFrameIndex;
    private Coroutine flashCoroutine;

    void Awake()
    {
        if (heartImage == null)
        {
            heartImage = GetComponent<Image>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (forceTopLeft && heartImage != null)
        {
            RectTransform rect = heartImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
        }
    }

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        Subscribe();
        if (playerHealth != null)
        {
            UpdateHeart(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDied -= HandleDied;
        }
    }

    private void Subscribe()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.OnHealthChanged -= HandleHealthChanged;
        playerHealth.OnDamaged -= HandleDamaged;
        playerHealth.OnDied -= HandleDied;

        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerHealth.OnDamaged += HandleDamaged;
        playerHealth.OnDied += HandleDied;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHeart(currentHealth, maxHealth);
    }

    private void HandleDamaged(int currentHealth, int maxHealth)
    {
        UpdateHeart(currentHealth, maxHealth);

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(ShowDamageHeart());
    }

    private void HandleDied()
    {
        UpdateHeart(0, playerHealth != null ? playerHealth.MaxHealth : 5);
    }

    private void UpdateHeart(int currentHealth, int maxHealth)
    {
        if (heartImage == null)
        {
            return;
        }

        currentFrameIndex = CalculateFrameIndex(currentHealth, maxHealth, corazonFrames.Length);
        Sprite normalSprite = GetSprite(corazonFrames, currentFrameIndex);
        if (normalSprite != null)
        {
            heartImage.sprite = normalSprite;
        }
    }

    private int CalculateFrameIndex(int currentHealth, int maxHealth, int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        if (maxHealth <= 0)
        {
            return frameCount - 1;
        }

        float normalizedMissing = 1f - Mathf.Clamp01((float)currentHealth / maxHealth);
        int index = Mathf.RoundToInt(normalizedMissing * (frameCount - 1));
        return Mathf.Clamp(index, 0, frameCount - 1);
    }

    private IEnumerator ShowDamageHeart()
    {
        if (heartImage == null)
        {
            yield break;
        }

        Sprite particulasSprite = GetSprite(corazonParticulasFrames, currentFrameIndex);
        if (particulasSprite != null)
        {
            heartImage.sprite = particulasSprite;
        }

        yield return new WaitForSeconds(particulasDuration);

        Sprite normalSprite = GetSprite(corazonFrames, currentFrameIndex);
        if (normalSprite != null)
        {
            heartImage.sprite = normalSprite;
        }

        flashCoroutine = null;
    }

    private Sprite GetSprite(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(index, 0, frames.Length - 1);
        return frames[safeIndex];
    }
}
