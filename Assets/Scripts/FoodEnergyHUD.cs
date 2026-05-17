using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class FoodEnergyHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerFoodEnergy playerFoodEnergy;
    [SerializeField] private HeartHUD heartHUD;
    [SerializeField] private Image foodImage;
    [SerializeField] private Image energyImage;
    [SerializeField] private bool autoAlign = true;
    [SerializeField] private bool useFixedPositions = false;
    [SerializeField] private Vector2 foodFixedPosition = new Vector2(60f, -24f);
    [SerializeField] private Vector2 energyFixedPosition = new Vector2(100f, -24f);

    [Header("Comida")]
    [SerializeField] private Sprite[] foodSprites = new Sprite[0];
    [SerializeField] private Sprite[] foodParticleSprites = new Sprite[0];

    [Header("Energia")]
    [SerializeField] private Sprite[] energySprites = new Sprite[0];
    [SerializeField] private Sprite[] energyParticleSprites = new Sprite[0];

    void Awake()
    {
        EnsureImagesExist();
        if (autoAlign)
        {
            AlignToHeart();
        }

        if (Application.isPlaying)
        {
            if (playerFoodEnergy == null)
            {
                playerFoodEnergy = FindFirstObjectByType<PlayerFoodEnergy>();
            }

            if (heartHUD == null)
            {
                heartHUD = FindFirstObjectByType<HeartHUD>();
            }

            if (foodImage == null)
            {
                foodImage = FindChildImage("FoodImage");
            }

            if (energyImage == null)
            {
                energyImage = FindChildImage("EnergyImage");
            }
        }
    }

    void OnValidate()
    {
        EnsureImagesExist();
        if (autoAlign)
        {
            AlignToHeart();
        }
    }

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        Subscribe();
        if (playerFoodEnergy != null)
        {
            UpdateFoodHUD(playerFoodEnergy.CurrentFood, playerFoodEnergy.MaxFood, playerFoodEnergy.IsFoodParticleActive);
            UpdateEnergyHUD(playerFoodEnergy.CurrentEnergy, playerFoodEnergy.MaxEnergy, playerFoodEnergy.IsEnergyParticleActive);
        }
    }

    void OnDisable()
    {
        if (playerFoodEnergy != null)
        {
            playerFoodEnergy.OnFoodChanged -= UpdateFoodHUD;
            playerFoodEnergy.OnEnergyChanged -= UpdateEnergyHUD;
        }
    }

    private void Subscribe()
    {
        if (playerFoodEnergy == null)
        {
            playerFoodEnergy = FindFirstObjectByType<PlayerFoodEnergy>();
        }

        if (heartHUD == null)
        {
            heartHUD = FindFirstObjectByType<HeartHUD>();
        }

        if (playerFoodEnergy == null)
        {
            return;
        }

        playerFoodEnergy.OnFoodChanged -= UpdateFoodHUD;
        playerFoodEnergy.OnEnergyChanged -= UpdateEnergyHUD;

        playerFoodEnergy.OnFoodChanged += UpdateFoodHUD;
        playerFoodEnergy.OnEnergyChanged += UpdateEnergyHUD;
    }

    private void UpdateFoodHUD(int currentFood, int maxFood, bool useParticles)
    {
        if (foodImage == null)
        {
            return;
        }

        Sprite selectedSprite = GetSpriteForValue(useParticles ? foodParticleSprites : foodSprites, currentFood, maxFood);
        if (selectedSprite != null)
        {
            foodImage.sprite = selectedSprite;
        }
    }

    private void UpdateEnergyHUD(int currentEnergy, int maxEnergy, bool useParticles)
    {
        if (energyImage == null)
        {
            return;
        }

        Sprite selectedSprite = GetSpriteForValue(useParticles ? energyParticleSprites : energySprites, currentEnergy, maxEnergy);
        if (selectedSprite != null)
        {
            energyImage.sprite = selectedSprite;
        }
    }

    private Sprite GetSpriteForValue(Sprite[] frames, int currentValue, int maxValue)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        int index = CalculateFrameIndex(currentValue, maxValue, frames.Length);
        return frames[index];
    }

    private int CalculateFrameIndex(int currentValue, int maxValue, int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        if (maxValue <= 0)
        {
            return frameCount - 1;
        }

        float normalizedMissing = 1f - Mathf.Clamp01((float)currentValue / maxValue);
        int index = Mathf.RoundToInt(normalizedMissing * (frameCount - 1));
        return Mathf.Clamp(index, 0, frameCount - 1);
    }

    private void AlignToHeart()
    {
        if (!autoAlign || heartHUD == null || heartHUD.HeartImage == null || foodImage == null || energyImage == null)
        {
            return;
        }

        RectTransform heartRect = heartHUD.HeartImage.rectTransform;
        RectTransform foodRect = foodImage.rectTransform;
        RectTransform energyRect = energyImage.rectTransform;

        if (useFixedPositions)
        {
            // Usar posiciones relativas al viewport para que escalen con la pantalla
            foodRect.anchorMin = new Vector2(0.07f, 0.95f); // 7% desde izquierda, 5% desde arriba
            foodRect.anchorMax = new Vector2(0.07f, 0.95f);
            foodRect.pivot = new Vector2(0f, 1f);
            foodRect.sizeDelta = heartRect.sizeDelta;
            foodRect.anchoredPosition = Vector2.zero;

            energyRect.anchorMin = new Vector2(0.11f, 0.95f); // 11% desde izquierda
            energyRect.anchorMax = new Vector2(0.11f, 0.95f);
            energyRect.pivot = new Vector2(0f, 1f);
            energyRect.sizeDelta = heartRect.sizeDelta;
            energyRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            // Alinear al corazón
            float heartWidth = heartRect.sizeDelta.x;

            foodRect.anchorMin = heartRect.anchorMin;
            foodRect.anchorMax = heartRect.anchorMax;
            foodRect.pivot = heartRect.pivot;
            foodRect.sizeDelta = heartRect.sizeDelta;
            foodRect.anchoredPosition = heartRect.anchoredPosition + new Vector2(heartWidth, 0f);

            energyRect.anchorMin = heartRect.anchorMin;
            energyRect.anchorMax = heartRect.anchorMax;
            energyRect.pivot = heartRect.pivot;
            energyRect.sizeDelta = heartRect.sizeDelta;
            energyRect.anchoredPosition = heartRect.anchoredPosition + new Vector2(heartWidth * 2.2f, 0f);
        }
    }

    private void EnsureImagesExist()
    {
        if (heartHUD == null)
        {
            heartHUD = FindFirstObjectByType<HeartHUD>();
        }

        if (foodImage == null)
        {
            foodImage = CreateHUDImage("FoodImage");
        }

        if (energyImage == null)
        {
            energyImage = CreateHUDImage("EnergyImage");
        }
    }

    private Image FindChildImage(string childName)
    {
        if (transform.parent == null)
        {
            return null;
        }

        Transform child = transform.parent.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<Image>();
    }

    private Image CreateHUDImage(string name)
    {
        GameObject imageObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (transform.parent != null)
        {
            imageObj.transform.SetParent(transform.parent, false);
        }
        else
        {
            imageObj.transform.SetParent(transform, false);
        }
        Image image = imageObj.GetComponent<Image>();
        image.rectTransform.sizeDelta = new Vector2(32f, 32f);
        image.color = Color.white;
        return image;
    }
}
