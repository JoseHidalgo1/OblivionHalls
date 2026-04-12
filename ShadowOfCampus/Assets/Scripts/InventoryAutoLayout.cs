using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class InventoryAutoLayout : MonoBehaviour
{
    [Header("Raíz")]
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private RectTransform slotsPanel;
    [SerializeField] private RectTransform detailsPanel;

    [Header("Tamaño del inventario")]
    [SerializeField] private Vector2 panelSize = new Vector2(920f, 520f);
    [SerializeField] private Vector2 outerPadding = new Vector2(30f, 30f);
    [SerializeField] private float panelSpacing = 30f;

    [Header("Slots")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 2;
    [SerializeField] private Vector2 slotSpacing = new Vector2(26f, 26f);
    [SerializeField] private Vector2 slotPadding = new Vector2(24f, 24f);

    [Header("Visual de slots")]
    [SerializeField] private Color slotBackgroundColor = new Color(0f, 0f, 0f, 0.22f);
    [SerializeField] private Color slotFrameColor = new Color(1f, 1f, 1f, 0.65f);
    [SerializeField] private Vector2 slotFrameThickness = new Vector2(2f, -2f);

    [Header("Detalle")]
    [SerializeField] private float detailsWidth = 260f;
    [SerializeField] private Vector2 detailsPadding = new Vector2(18f, 18f);
    [SerializeField] private Vector2 detailsIconSize = new Vector2(110f, 110f);
    [SerializeField] private float detailsHeaderSpacing = 14f;
    [SerializeField] private float detailsNameHeight = 40f;

    [Header("Escalado de UI")]
    [SerializeField] private bool configureCanvasScaler = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float screenMatch = 0.5f;

    [Header("Tipografía de detalle")]
    [SerializeField] private int detailsNameFontSize = 34;
    [SerializeField] private int detailsDescriptionFontSize = 26;
    [SerializeField] private bool useBestFit = true;
    [SerializeField] private int minBestFitName = 20;
    [SerializeField] private int maxBestFitName = 40;
    [SerializeField] private int minBestFitDescription = 16;
    [SerializeField] private int maxBestFitDescription = 32;

    [Header("Aplicación")]
    [SerializeField] private bool autoApplyOnStart = true;

    void Reset()
    {
        TryAutoAssign();
    }

    void OnValidate()
    {
        TryAutoAssign();
    }

    void Start()
    {
        if (autoApplyOnStart)
        {
            ApplyLayout();
        }
    }

    [ContextMenu("Apply Inventory Layout")]
    public void ApplyLayout()
    {
        TryAutoAssign();

        if (inventoryPanel == null)
        {
            return;
        }

        ConfigureCanvasForScreen();
        ConfigureInventoryPanel();
        ConfigureSlotsPanel();
        ConfigureDetailsPanel();
    }

    private void ConfigureCanvasForScreen()
    {
        if (!configureCanvasScaler || inventoryPanel == null)
        {
            return;
        }

        Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = screenMatch;
    }

    private void TryAutoAssign()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = transform as RectTransform;
        }

        if (inventoryPanel == null)
        {
            return;
        }

        if (slotsPanel == null)
        {
            Transform foundSlots = inventoryPanel.Find("SlotsPanel");
            if (foundSlots != null)
            {
                slotsPanel = foundSlots as RectTransform;
            }
        }

        if (detailsPanel == null)
        {
            Transform foundDetails = inventoryPanel.Find("DetailsPanel");
            if (foundDetails != null)
            {
                detailsPanel = foundDetails as RectTransform;
            }
        }
    }

    private void ConfigureInventoryPanel()
    {
        inventoryPanel.anchorMin = new Vector2(0.5f, 0.5f);
        inventoryPanel.anchorMax = new Vector2(0.5f, 0.5f);
        inventoryPanel.pivot = new Vector2(0.5f, 0.5f);
        inventoryPanel.sizeDelta = panelSize;
        inventoryPanel.anchoredPosition = Vector2.zero;
    }

    private void ConfigureSlotsPanel()
    {
        if (slotsPanel == null)
        {
            return;
        }

        float left = outerPadding.x;
        float top = outerPadding.y;
        float bottom = outerPadding.y;
        float rightSectionWidth = detailsWidth + panelSpacing + outerPadding.x;

        slotsPanel.anchorMin = new Vector2(0f, 0f);
        slotsPanel.anchorMax = new Vector2(1f, 1f);
        slotsPanel.pivot = new Vector2(0f, 1f);
        slotsPanel.offsetMin = new Vector2(left, bottom);
        slotsPanel.offsetMax = new Vector2(-rightSectionWidth, -top);

        GridLayoutGroup grid = slotsPanel.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = slotsPanel.gameObject.AddComponent<GridLayoutGroup>();
        }

        Rect rect = slotsPanel.rect;
        float availableWidth = Mathf.Max(1f, rect.width - slotPadding.x * 2f - slotSpacing.x * (columns - 1));
        float availableHeight = Mathf.Max(1f, rect.height - slotPadding.y * 2f - slotSpacing.y * (rows - 1));
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.spacing = slotSpacing;
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.padding = new RectOffset(
            Mathf.RoundToInt(slotPadding.x),
            Mathf.RoundToInt(slotPadding.x),
            Mathf.RoundToInt(slotPadding.y),
            Mathf.RoundToInt(slotPadding.y));
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int index = 0; index < slotsPanel.childCount; index++)
        {
            RectTransform slot = slotsPanel.GetChild(index) as RectTransform;
            if (slot == null)
            {
                continue;
            }

            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = new Vector2(cellSize, cellSize);

            Button button = slot.GetComponent<Button>();
            Image slotImage = slot.GetComponent<Image>();

            if (slotImage != null)
            {
                slotImage.color = slotBackgroundColor;
                slotImage.raycastTarget = true;
            }

            Outline frame = slot.GetComponent<Outline>();
            if (frame == null)
            {
                frame = slot.gameObject.AddComponent<Outline>();
            }

            frame.effectColor = slotFrameColor;
            frame.effectDistance = slotFrameThickness;
            frame.useGraphicAlpha = false;

            if (button != null && slotImage != null)
            {
                button.targetGraphic = slotImage;
                ColorBlock colors = button.colors;
                colors.normalColor = slotBackgroundColor;
                colors.highlightedColor = new Color(slotBackgroundColor.r, slotBackgroundColor.g, slotBackgroundColor.b, slotBackgroundColor.a + 0.08f);
                colors.pressedColor = new Color(slotBackgroundColor.r, slotBackgroundColor.g, slotBackgroundColor.b, slotBackgroundColor.a + 0.14f);
                colors.selectedColor = new Color(slotBackgroundColor.r, slotBackgroundColor.g, slotBackgroundColor.b, slotBackgroundColor.a + 0.08f);
                colors.disabledColor = new Color(slotBackgroundColor.r, slotBackgroundColor.g, slotBackgroundColor.b, 0.08f);
                button.colors = colors;
            }

            for (int childIndex = 0; childIndex < slot.childCount; childIndex++)
            {
                Transform child = slot.GetChild(childIndex);
                if (child.name.ToLower().Contains("text") || child.name.ToLower().Contains("button"))
                {
                    Graphic graphic = child.GetComponent<Graphic>();
                    if (graphic != null)
                    {
                        graphic.enabled = false;
                    }
                }
            }

            Transform iconTransform = slot.Find("ItemIcon");
            if (iconTransform is RectTransform iconRect)
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(cellSize * 0.62f, cellSize * 0.62f);
                iconRect.anchoredPosition = Vector2.zero;

                Image iconImage = iconRect.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.raycastTarget = false;
                }
            }
        }
    }

    private void ConfigureDetailsPanel()
    {
        if (detailsPanel == null)
        {
            return;
        }

        detailsPanel.anchorMin = new Vector2(1f, 0f);
        detailsPanel.anchorMax = new Vector2(1f, 1f);
        detailsPanel.pivot = new Vector2(1f, 1f);
        detailsPanel.offsetMin = new Vector2(-outerPadding.x - detailsWidth, outerPadding.y);
        detailsPanel.offsetMax = new Vector2(-outerPadding.x, -outerPadding.y);

        Transform iconTransform = detailsPanel.Find("DetailsIcon");
        if (iconTransform is RectTransform iconRect)
        {
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = detailsIconSize;
            iconRect.anchoredPosition = new Vector2(0f, -detailsPadding.y);
        }

        Transform nameTransform = detailsPanel.Find("DetailsName");
        if (nameTransform is RectTransform nameRect)
        {
            float nameY = detailsPadding.y + detailsIconSize.y + detailsHeaderSpacing;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(detailsPadding.x, -(nameY + detailsNameHeight));
            nameRect.offsetMax = new Vector2(-detailsPadding.x, -nameY);

            Text nameText = nameRect.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.fontSize = detailsNameFontSize;
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                nameText.resizeTextForBestFit = useBestFit;
                nameText.resizeTextMinSize = minBestFitName;
                nameText.resizeTextMaxSize = maxBestFitName;
                nameText.alignment = TextAnchor.MiddleLeft;
            }
        }

        Transform descriptionTransform = detailsPanel.Find("DetailsDescription");
        if (descriptionTransform is RectTransform descriptionRect)
        {
            float topOffset = detailsPadding.y + detailsIconSize.y + detailsHeaderSpacing + detailsNameHeight + detailsHeaderSpacing;
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.pivot = new Vector2(0.5f, 1f);
            descriptionRect.offsetMin = new Vector2(detailsPadding.x, detailsPadding.y);
            descriptionRect.offsetMax = new Vector2(-detailsPadding.x, -topOffset);

            Text descriptionText = descriptionRect.GetComponent<Text>();
            if (descriptionText != null)
            {
                descriptionText.fontSize = detailsDescriptionFontSize;
                descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                descriptionText.resizeTextForBestFit = useBestFit;
                descriptionText.resizeTextMinSize = minBestFitDescription;
                descriptionText.resizeTextMaxSize = maxBestFitDescription;
                descriptionText.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
