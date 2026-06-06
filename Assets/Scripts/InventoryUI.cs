using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Panel raíz (se activa/desactiva)")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Slots (10)")]
    [SerializeField] private InventorySlotView[] slotViews = new InventorySlotView[10];
    [SerializeField] private Transform slotsContainer;

    [Header("Panel de detalle (derecha)")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image detailsIcon;
    [SerializeField] private Text detailsName;
    [SerializeField] private Text detailsDescription;

    [Header("Estilo de texto")]
    [SerializeField] private Color detailsNameColor = new Color(0.95f, 0.97f, 1f, 1f);
    [SerializeField] private Color detailsDescriptionColor = new Color(0.86f, 0.9f, 0.98f, 1f);

    [Header("Ocultado")]
    [SerializeField] private bool useHardHideWithSetActive = true;

    [Header("Menú contextual (clic derecho)")]
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private Button contextUseButton;
    [SerializeField] private Button contextDropButton;

    [Header("Drop al suelo")]
    [SerializeField] private float dropDistance = 0.8f;
    [SerializeField] private float pickupRadius = 0.7f;
    [SerializeField] private string droppedItemSortingLayer = "Background";
    [SerializeField] private int droppedItemSortingOrder = -1;
    [SerializeField] private float droppedItemScale = 0.5f;

    [Header("Hint de recogida")]
    [SerializeField] private bool showPickupHint = true;
    [SerializeField] private string pickupHintMessage = "Interactuar";
    [SerializeField] private float pickupHintHeight = 0.7f;
    [SerializeField] private int pickupHintSortingOrder = 50;

    [Header("Feedback de arrastre")]
    [SerializeField] private Color dragHoverEmptyColor = new Color(0.2f, 0.75f, 0.3f, 0.95f);
    [SerializeField] private Color dragHoverOccupiedColor = new Color(0.2f, 0.55f, 0.95f, 0.95f);

    private bool isOpen = false;
    private CanvasGroup inventoryCanvasGroup;
    private bool usingCanvasGroupMode;
    private int contextSlotIndex = -1;
    private PlayerHealth cachedPlayerHealth;
    private PlayerFoodEnergy cachedPlayerFoodEnergy;
    private GameObject pickupHintObject;
    private TextMesh pickupHintText;
    private bool isDraggingItem;
    private int dragSourceIndex = -1;
    private Item dragSourceItem;
    private GameObject dragGhostObject;
    private Image dragGhostImage;
    private int dragHoverIndex = -1;
    private readonly Dictionary<Image, Color> slotOriginalColors = new Dictionary<Image, Color>();

    private int longPressSlotIndex = -1;
    private Coroutine longPressCoroutine;
    private bool longPressTriggered;
    private bool suppressNextClick;
    private readonly float longPressDuration = 1f;

    void Awake()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = gameObject;
        }

        usingCanvasGroupMode = !useHardHideWithSetActive || inventoryPanel == gameObject;

        if (usingCanvasGroupMode)
        {
            inventoryCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (inventoryCanvasGroup == null)
            {
                inventoryCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }

        SetVisible(false, true);
    }

    void Start()
    {
        SetVisible(false, true);
        TryAutoAssignSlotsContainer();
        BindSlotButtons();
        EnsureContextMenuReferences();
        EnsureDragGhostObject();
        BindContextMenuButtons();
        EnsurePickupHintObject();
        ApplyDetailsTextStyle();
        HideDetails();
        HideContextMenu();
    }

    void Update()
    {
        if (ActionInput.WasPressedThisFrame(GameAction.Inventory))
        {
            ToggleInventory();
        }

        UpdatePickupHint();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryCloseContextMenuOnOutsideClick(Mouse.current.position.ReadValue());
        }

        if (isDraggingItem && Mouse.current != null)
        {
            UpdateDragGhostPosition(Mouse.current.position.ReadValue());
        }

        if (ActionInput.WasPressedThisFrame(GameAction.Interact) || ActionInput.WasPressedThisFrame(GameAction.Pickup))
        {
            TryPickupNearbyDroppedItem();
        }
    }

    private void ToggleInventory()
    {
        SetVisible(!isOpen, false);

        if (isOpen)
        {
            Refresh();
        }
        else
        {
            HideDetails();
            HideContextMenu();
        }
    }

    private void SetVisible(bool visible, bool force)
    {
        if (inventoryPanel == null)
        {
            return;
        }

        if (!force && isOpen == visible)
        {
            return;
        }

        isOpen = visible;

        if (usingCanvasGroupMode)
        {
            if (inventoryCanvasGroup == null)
            {
                return;
            }

            inventoryCanvasGroup.alpha = visible ? 1f : 0f;
            inventoryCanvasGroup.interactable = visible;
            inventoryCanvasGroup.blocksRaycasts = visible;
            return;
        }

        inventoryPanel.SetActive(visible);
    }

    private void Refresh()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        int activeSlotCount = inv.SlotCount;
        ApplyVisualSlotCount(activeSlotCount);

        for (int index = 0; index < slotViews.Length; index++)
        {
            if (slotViews[index] == null)
            {
                continue;
            }

            bool shouldBeVisible = index < activeSlotCount;
            slotViews[index].SetVisible(shouldBeVisible);
            if (!shouldBeVisible)
            {
                continue;
            }

            Item item = inv.GetItemAt(index);
            slotViews[index].SetItem(item);
        }

        if (contextSlotIndex >= activeSlotCount)
        {
            HideContextMenu();
            HideDetails();
        }
    }

    private void TryAutoAssignSlotsContainer()
    {
        if (slotsContainer != null)
        {
            return;
        }

        if (inventoryPanel == null)
        {
            return;
        }

        Transform found = inventoryPanel.transform.Find("SlotsPanel");
        if (found != null)
        {
            slotsContainer = found;
        }
    }

    private void ApplyVisualSlotCount(int activeSlotCount)
    {
        if (slotsContainer == null)
        {
            return;
        }

        int visibleCount = Mathf.Max(0, activeSlotCount);
        for (int index = 0; index < slotsContainer.childCount; index++)
        {
            Transform child = slotsContainer.GetChild(index);
            if (child == null)
            {
                continue;
            }

            bool shouldShow = index < visibleCount;
            if (child.gameObject.activeSelf != shouldShow)
            {
                child.gameObject.SetActive(shouldShow);
            }
        }
    }

    private void BindSlotButtons()
    {
        for (int index = 0; index < slotViews.Length; index++)
        {
            if (slotViews[index] == null || slotViews[index].button == null)
            {
                continue;
            }

            int cachedIndex = index;
            slotViews[index].button.onClick.RemoveAllListeners();

            EventTrigger trigger = slotViews[index].button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = slotViews[index].button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();

            EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            pointerClickEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotPointerClick(cachedIndex, pointerData);
                }
            });

            trigger.triggers.Add(pointerClickEntry);

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

            pointerDownEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotPointerDown(cachedIndex, pointerData);
                }
            });
            trigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };

            pointerUpEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotPointerUp(cachedIndex, pointerData);
                }
            });
            trigger.triggers.Add(pointerUpEntry);

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.BeginDrag
            };

            beginDragEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotBeginDrag(cachedIndex, pointerData);
                }
            });
            trigger.triggers.Add(beginDragEntry);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Drag
            };

            dragEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotDrag(pointerData);
                }
            });
            trigger.triggers.Add(dragEntry);

            EventTrigger.Entry endDragEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.EndDrag
            };

            endDragEntry.callback.AddListener(data =>
            {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnSlotEndDrag(pointerData);
                }
            });
            trigger.triggers.Add(endDragEntry);
        }
    }

    private void OnSlotPointerDown(int slotIndex, PointerEventData pointerData)
    {
        if (pointerData.button != PointerEventData.InputButton.Left)
            return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
            return;

        Item item = inv.GetItemAt(slotIndex);
        if (item == null)
            return;

        CancelLongPress();
        longPressSlotIndex = slotIndex;
        longPressCoroutine = StartCoroutine(LongPressRoutine(slotIndex));
    }

    private void OnSlotPointerUp(int slotIndex, PointerEventData pointerData)
    {
        if (pointerData.button != PointerEventData.InputButton.Left)
            return;

        if (longPressTriggered && slotIndex == longPressSlotIndex)
        {
            longPressTriggered = false;
            suppressNextClick = true;
            CancelLongPress();
            return;
        }

        CancelLongPress();
    }

    private System.Collections.IEnumerator LongPressRoutine(int slotIndex)
    {
        float elapsed = 0f;
        while (elapsed < longPressDuration)
        {
            if (longPressSlotIndex != slotIndex || isDraggingItem)
            {
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        longPressTriggered = true;
        DropItemAt(slotIndex);
        CancelLongPress();
    }

    private void CancelLongPress()
    {
        longPressSlotIndex = -1;
        longPressTriggered = false;
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private void OnSlotBeginDrag(int slotIndex, PointerEventData pointerData)
    {
        if (pointerData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        CancelLongPress();

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            return;
        }

        Item item = inv.GetItemAt(slotIndex);
        if (item == null)
        {
            return;
        }

        isDraggingItem = true;
        dragSourceIndex = slotIndex;
        dragSourceItem = item;

        EnsureDragGhostObject();
        if (dragGhostImage != null)
        {
            dragGhostImage.sprite = item.icon;
            dragGhostImage.enabled = item.icon != null;
        }

        if (dragGhostObject != null)
        {
            dragGhostObject.SetActive(true);
        }

        ResetDragHoverVisual();
        UpdateDragGhostPosition(pointerData.position);
    }

    private void OnSlotDrag(PointerEventData pointerData)
    {
        if (!isDraggingItem)
        {
            return;
        }

        UpdateDragGhostPosition(pointerData.position);
        UpdateDragHoverVisual(pointerData);
    }

    private void OnSlotEndDrag(PointerEventData pointerData)
    {
        if (!isDraggingItem)
        {
            return;
        }

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            CancelDragState();
            return;
        }

        int targetIndex = GetSlotIndexUnderPointer(pointerData);
        if (targetIndex >= 0 && targetIndex < inv.SlotCount && dragSourceIndex >= 0 && dragSourceIndex < inv.SlotCount)
        {
            Item sourceItem = inv.GetItemAt(dragSourceIndex);
            Item targetItem = inv.GetItemAt(targetIndex);

            inv.SetItemAt(targetIndex, sourceItem);
            inv.SetItemAt(dragSourceIndex, targetItem);
            Refresh();
        }
        else if (dragSourceIndex >= 0 && dragSourceIndex < inv.SlotCount)
        {
            DropItemAt(dragSourceIndex);
        }

        CancelDragState();
    }

    private void UpdateDragHoverVisual(PointerEventData pointerData)
    {
        int hoverIndex = GetSlotIndexUnderPointer(pointerData);
        if (hoverIndex == dragHoverIndex)
        {
            return;
        }

        ResetDragHoverVisual();
        dragHoverIndex = hoverIndex;

        if (dragHoverIndex < 0 || dragHoverIndex >= slotViews.Length)
        {
            return;
        }

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || dragHoverIndex >= inv.SlotCount)
        {
            return;
        }

        InventorySlotView slotView = slotViews[dragHoverIndex];
        if (slotView == null || slotView.button == null)
        {
            return;
        }

        Image slotImage = slotView.button.GetComponent<Image>();
        if (slotImage == null)
        {
            return;
        }

        if (!slotOriginalColors.ContainsKey(slotImage))
        {
            slotOriginalColors.Add(slotImage, slotImage.color);
        }

        bool occupied = inv.GetItemAt(dragHoverIndex) != null;
        slotImage.color = occupied ? dragHoverOccupiedColor : dragHoverEmptyColor;
    }

    private void ResetDragHoverVisual()
    {
        foreach (KeyValuePair<Image, Color> pair in slotOriginalColors)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.color = pair.Value;
        }

        slotOriginalColors.Clear();
        dragHoverIndex = -1;
    }

    private int GetSlotIndexUnderPointer(PointerEventData pointerData)
    {
        if (EventSystem.current == null)
        {
            return -1;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;
            for (int slotIndex = 0; slotIndex < slotViews.Length; slotIndex++)
            {
                if (slotViews[slotIndex] == null || slotViews[slotIndex].button == null)
                {
                    continue;
                }

                Transform slotTransform = slotViews[slotIndex].button.transform;
                if (hitObject.transform == slotTransform || hitObject.transform.IsChildOf(slotTransform))
                {
                    return slotIndex;
                }
            }
        }

        return -1;
    }

    private void EnsureDragGhostObject()
    {
        if (inventoryPanel == null || dragGhostObject != null)
        {
            return;
        }

        dragGhostObject = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        dragGhostObject.transform.SetParent(inventoryPanel.transform, false);

        RectTransform rect = dragGhostObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(52f, 52f);

        dragGhostImage = dragGhostObject.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.enabled = false;

        dragGhostObject.SetActive(false);
    }

    private void UpdateDragGhostPosition(Vector2 screenPosition)
    {
        if (dragGhostObject == null || inventoryPanel == null)
        {
            return;
        }

        RectTransform rootRect = inventoryPanel.GetComponent<RectTransform>();
        RectTransform ghostRect = dragGhostObject.GetComponent<RectTransform>();
        Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
        Camera uiCamera = GetUICamera(canvas);

        if (rootRect == null || ghostRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPosition, uiCamera, out Vector2 localPoint);
        ghostRect.anchoredPosition = localPoint;
    }

    private void CancelDragState()
    {
        isDraggingItem = false;
        dragSourceIndex = -1;
        dragSourceItem = null;
        ResetDragHoverVisual();

        if (dragGhostImage != null)
        {
            dragGhostImage.sprite = null;
            dragGhostImage.enabled = false;
        }

        if (dragGhostObject != null)
        {
            dragGhostObject.SetActive(false);
        }
    }

    private void OnSlotPointerClick(int slotIndex, PointerEventData pointerData)
    {
        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            RectTransform slotRect = null;
            if (slotIndex >= 0 && slotIndex < slotViews.Length && slotViews[slotIndex] != null && slotViews[slotIndex].button != null)
            {
                slotRect = slotViews[slotIndex].button.GetComponent<RectTransform>();
            }

            OpenContextMenu(slotIndex, slotRect, pointerData.position);
            return;
        }

        if (pointerData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (pointerData.clickCount >= 2)
        {
            UseItemAt(slotIndex);
            return;
        }

        OnSlotClicked(slotIndex);
    }

    private void OnSlotClicked(int slotIndex)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            return;
        }

        Item item = inv.GetItemAt(slotIndex);
        if (item == null)
        {
            HideDetails();
            HideContextMenu();
            return;
        }

        ShowDetails(item);
        HideContextMenu();
    }

    private void OpenContextMenu(int slotIndex, RectTransform slotRect, Vector2 fallbackScreenPosition)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            return;
        }

        contextSlotIndex = slotIndex;
        EnsureContextMenuReferences();
        if (contextMenuPanel == null)
        {
            return;
        }

        Item item = inv.GetItemAt(slotIndex);
        bool hasItem = item != null;

        if (contextUseButton != null)
        {
            contextUseButton.interactable = hasItem;
        }

        if (contextDropButton != null)
        {
            contextDropButton.interactable = hasItem;
        }

        contextMenuPanel.SetActive(true);

        RectTransform menuRect = contextMenuPanel.GetComponent<RectTransform>();
        if (menuRect != null && inventoryPanel != null)
        {
            RectTransform rootRect = inventoryPanel.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                Vector2 targetScreenPosition = fallbackScreenPosition;

                if (slotRect != null)
                {
                    Vector3 worldCenter = slotRect.TransformPoint(slotRect.rect.center);
                    Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
                    Camera uiCamera = GetUICamera(canvas);
                    targetScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
                }

                Canvas parentCanvas = inventoryPanel.GetComponentInParent<Canvas>();
                Camera rootCamera = GetUICamera(parentCanvas);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, targetScreenPosition, rootCamera, out Vector2 localPoint);
                menuRect.anchoredPosition = localPoint;
            }
        }
    }

    private Camera GetUICamera(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void HideContextMenu()
    {
        contextSlotIndex = -1;
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
    }

    private void EnsureContextMenuReferences()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        if (contextMenuPanel != null)
        {
            return;
        }

        GameObject menuObject = new GameObject("ContextMenu", typeof(RectTransform), typeof(Image));
        menuObject.transform.SetParent(inventoryPanel.transform, false);

        RectTransform menuRect = menuObject.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(180f, 84f);

        Image background = menuObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.92f);

        contextMenuPanel = menuObject;
        contextUseButton = CreateContextButton(menuObject.transform, "UseButton", "Usar", new Vector2(0f, -6f));
        contextDropButton = CreateContextButton(menuObject.transform, "DropButton", "Soltar", new Vector2(0f, -44f));
    }

    private Button CreateContextButton(Transform parent, string objectName, string textLabel, Vector2 anchoredPos)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = anchoredPos;
        buttonRect.sizeDelta = new Vector2(-8f, 32f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.23f, 0.3f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = textLabel;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return buttonObject.GetComponent<Button>();
    }

    private void BindContextMenuButtons()
    {
        if (contextUseButton != null)
        {
            contextUseButton.onClick.RemoveAllListeners();
            contextUseButton.onClick.AddListener(HandleUseContextAction);
        }

        if (contextDropButton != null)
        {
            contextDropButton.onClick.RemoveAllListeners();
            contextDropButton.onClick.AddListener(HandleDropContextAction);
        }
    }

    private void HandleUseContextAction()
    {
        UseItemAt(contextSlotIndex);
    }

    private void HandleDropContextAction()
    {
        DropItemAt(contextSlotIndex);
    }

    private bool UseItemAt(int slotIndex)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || slotIndex < 0 || slotIndex >= inv.SlotCount)
        {
            return false;
        }

        Item item = inv.GetItemAt(slotIndex);
        if (item == null)
        {
            HideContextMenu();
            return false;
        }

        string normalizedName = (item.itemName ?? string.Empty).Trim().ToLowerInvariant();
        bool isHealingItem = normalizedName == "medicina" || normalizedName == "pastillas";
        bool isFoodItem = normalizedName == "lata";
        if (!isHealingItem && !isFoodItem)
        {
            HideContextMenu();
            return false;
        }

        PlayerHealth health = GetPlayerHealth();
        PlayerFoodEnergy foodEnergy = GetPlayerFoodEnergy();
        if (isHealingItem && health != null)
        {
            health.Heal(1);
        }
        else if (isFoodItem && foodEnergy != null)
        {
            foodEnergy.RestoreFood(2);
        }

        inv.SetItemAt(slotIndex, null);
        HideContextMenu();
        HideDetails();
        Refresh();
        return true;
    }

    private bool DropItemAt(int slotIndex)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || slotIndex < 0 || slotIndex >= inv.SlotCount)
        {
            return false;
        }

        Item item = inv.GetItemAt(slotIndex);
        if (item == null)
        {
            HideContextMenu();
            return false;
        }

        SpawnDroppedItem(item);
        inv.SetItemAt(slotIndex, null);

        string normalizedName = (item.itemName ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedName.Contains("mochila"))
        {
            inv.ResizeSlots(4);
        }

        HideContextMenu();
        HideDetails();
        Refresh();
        return true;
    }

    private void SpawnDroppedItem(Item item)
    {
        Vector3 spawnPosition = Vector3.zero;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            spawnPosition = playerObject.transform.position + Vector3.down * dropDistance;
        }

        GameObject dropped = new GameObject("Drop_" + item.itemName);
        dropped.transform.position = spawnPosition;

        DroppedItemWorld droppedData = dropped.AddComponent<DroppedItemWorld>();
        droppedData.SetItem(item);

        SpriteRenderer sr = dropped.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingLayerName = droppedItemSortingLayer;
        sr.sortingOrder = droppedItemSortingOrder;
        dropped.transform.localScale = new Vector3(droppedItemScale, droppedItemScale, 1f);

        CircleCollider2D col = dropped.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private void TryPickupNearbyDroppedItem()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        DroppedItemWorld dropped = FindNearestDroppedItem(playerObject.transform.position);
        if (dropped == null || dropped.Item == null)
        {
            return;
        }

        string itemName = (dropped.Item.itemName ?? string.Empty).Trim().ToLowerInvariant();
        if (itemName.Contains("mochila") && inv.SlotCount < 10)
        {
            inv.ResizeSlots(10);
        }

        bool added = inv.AddItem(dropped.Item);
        if (!added)
        {
            return;
        }

        Destroy(dropped.gameObject);
        HidePickupHint();
        Refresh();
    }

    private DroppedItemWorld FindNearestDroppedItem(Vector3 center)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(center, pickupRadius);
        DroppedItemWorld nearest = null;
        float nearestDistance = float.MaxValue;

        for (int index = 0; index < nearby.Length; index++)
        {
            Collider2D col = nearby[index];
            if (col == null)
            {
                continue;
            }

            DroppedItemWorld dropped = col.GetComponent<DroppedItemWorld>();
            if (dropped == null)
            {
                dropped = col.GetComponentInParent<DroppedItemWorld>();
            }

            if (dropped == null || dropped.Item == null)
            {
                continue;
            }

            float distance = Vector3.Distance(center, dropped.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = dropped;
            }
        }

        return nearest;
    }

    private void EnsurePickupHintObject()
    {
        if (!showPickupHint || pickupHintObject != null)
        {
            return;
        }

        pickupHintObject = new GameObject("PickupHint");
        pickupHintText = pickupHintObject.AddComponent<TextMesh>();
        pickupHintText.text = pickupHintMessage;
        pickupHintText.characterSize = 0.08f;
        pickupHintText.fontSize = 64;
        pickupHintText.color = Color.white;
        pickupHintText.alignment = TextAlignment.Center;
        pickupHintText.anchor = TextAnchor.MiddleCenter;

        MeshRenderer meshRenderer = pickupHintObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Player";
            meshRenderer.sortingOrder = pickupHintSortingOrder;
        }

        pickupHintObject.SetActive(false);
    }

    private void UpdatePickupHint()
    {
        if (!showPickupHint)
        {
            HidePickupHint();
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            HidePickupHint();
            return;
        }

        EnsurePickupHintObject();
        DroppedItemWorld nearest = FindNearestDroppedItem(playerObject.transform.position);
        if (nearest == null)
        {
            HidePickupHint();
            return;
        }

        if (pickupHintText != null)
        {
            pickupHintText.text = pickupHintMessage;
        }

        if (pickupHintObject != null)
        {
            pickupHintObject.transform.position = nearest.transform.position + Vector3.up * pickupHintHeight;
            pickupHintObject.SetActive(true);
        }
    }

    private void HidePickupHint()
    {
        if (pickupHintObject != null)
        {
            pickupHintObject.SetActive(false);
        }
    }

    private PlayerHealth GetPlayerHealth()
    {
        if (cachedPlayerHealth != null)
        {
            return cachedPlayerHealth;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return null;
        }

        cachedPlayerHealth = playerObject.GetComponent<PlayerHealth>();
        if (cachedPlayerHealth == null)
        {
            cachedPlayerHealth = playerObject.GetComponentInChildren<PlayerHealth>();
        }

        return cachedPlayerHealth;
    }

    private PlayerFoodEnergy GetPlayerFoodEnergy()
    {
        if (cachedPlayerFoodEnergy != null)
        {
            return cachedPlayerFoodEnergy;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return null;
        }

        cachedPlayerFoodEnergy = playerObject.GetComponent<PlayerFoodEnergy>();
        if (cachedPlayerFoodEnergy == null)
        {
            cachedPlayerFoodEnergy = playerObject.GetComponentInChildren<PlayerFoodEnergy>();
        }

        return cachedPlayerFoodEnergy;
    }

    private void TryCloseContextMenuOnOutsideClick(Vector2 mousePosition)
    {
        if (contextMenuPanel == null || !contextMenuPanel.activeSelf)
        {
            return;
        }

        RectTransform menuRect = contextMenuPanel.GetComponent<RectTransform>();
        if (menuRect != null && RectTransformUtility.RectangleContainsScreenPoint(menuRect, mousePosition))
        {
            return;
        }

        HideContextMenu();
    }

    private void ShowDetails(Item item)
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
        }

        if (detailsIcon != null)
        {
            detailsIcon.sprite = item.icon;
            detailsIcon.enabled = item.icon != null;
        }

        if (detailsName != null)
        {
            detailsName.color = detailsNameColor;
            detailsName.text = item.itemName;
        }

        if (detailsDescription != null)
        {
            detailsDescription.color = detailsDescriptionColor;
            detailsDescription.text = item.description;
        }
    }

    private void HideDetails()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }

    private void ApplyDetailsTextStyle()
    {
        if (detailsName != null)
        {
            detailsName.color = detailsNameColor;
        }

        if (detailsDescription != null)
        {
            detailsDescription.color = detailsDescriptionColor;
        }
    }
}

[System.Serializable]
public class InventorySlotView
{
    public Button button;
    public Image iconImage;

    public void SetItem(Item item)
    {
        bool hasItem = item != null;
        // Prefer explicit iconImage (legacy). If absent, use the Button's Image component so the
        // Button itself displays the item icon (useful when slots were simplified to only contain a Button).
        Image target = iconImage != null ? iconImage : (button != null ? button.GetComponent<Image>() : null);
        if (target != null)
        {
            target.sprite = hasItem ? item.icon : null;
            target.enabled = hasItem && item.icon != null;
            // Ensure the button doesn't block raycasts when showing icon only
            target.raycastTarget = false;
        }
    }

    public void SetVisible(bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }
}
