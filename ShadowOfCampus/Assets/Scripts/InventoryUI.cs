using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Panel raíz (se activa/desactiva)")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Slots (10)")]
    [SerializeField] private InventorySlotView[] slotViews = new InventorySlotView[10];

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

    private bool isOpen = false;
    private CanvasGroup inventoryCanvasGroup;
    private bool usingCanvasGroupMode;

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
        BindSlotButtons();
        ApplyDetailsTextStyle();
        HideDetails();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
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

        for (int index = 0; index < slotViews.Length; index++)
        {
            if (slotViews[index] == null)
            {
                continue;
            }

            Item item = inv.GetItemAt(index);
            slotViews[index].SetItem(item);
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
            slotViews[index].button.onClick.AddListener(() => OnSlotClicked(cachedIndex));
        }
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
            return;
        }

        ShowDetails(item);
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

        if (iconImage != null)
        {
            iconImage.sprite  = hasItem ? item.icon : null;
            iconImage.enabled = hasItem && item.icon != null;
        }
    }
}
