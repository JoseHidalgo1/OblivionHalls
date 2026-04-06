using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la UI del inventario. Adjunta al GameObject del panel de inventario.
/// 
/// Estructura de UI requerida en la escena:
///   InventoryPanel (este script)
///   ├── PlayerInfoPanel
///   │   ├── NameText       (Text/TMP)
///   │   ├── AgeText        (Text/TMP)
///   │   ├── OccupationText (Text/TMP)
///   │   └── HealthText     (Text/TMP)
///   └── SlotsPanel
///       ├── Slot0
///       │   ├── ItemIcon (Image)
///       │   └── ItemName (Text/TMP)
///       ├── Slot1
///       ├── Slot2
///       └── Slot3
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Panel raíz (se activa/desactiva)")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Textos de info del jugador")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text ageText;
    [SerializeField] private Text occupationText;
    [SerializeField] private Text healthText;

    [Header("Slots (exactamente 4)")]
    [SerializeField] private SlotUI[] slotUIs = new SlotUI[4];

    private bool isOpen = false;
    private CanvasGroup inventoryCanvasGroup;

    void Awake()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = gameObject;
        }

        inventoryCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        if (inventoryCanvasGroup == null)
        {
            inventoryCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
        }

        SetVisible(false, true);
    }

    void Start()
    {
        SetVisible(false, true);
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
            Refresh();
    }

    private void SetVisible(bool visible, bool force)
    {
        if (inventoryPanel == null || inventoryCanvasGroup == null)
        {
            return;
        }

        if (!force && isOpen == visible)
        {
            return;
        }

        isOpen = visible;
        inventoryCanvasGroup.alpha = visible ? 1f : 0f;
        inventoryCanvasGroup.interactable = visible;
        inventoryCanvasGroup.blocksRaycasts = visible;
    }

    private void Refresh()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // Info del jugador
        if (nameText != null)       nameText.text       = "Nombre: " + inv.playerName;
        if (ageText != null)        ageText.text        = "Edad: "   + inv.age;
        if (occupationText != null) occupationText.text = "Ocupación: " + inv.occupation;
        if (healthText != null)     healthText.text     = "Vida: "   + inv.CurrentHealth + " / " + inv.MaxHealth;

        // Slots
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            Item item = (inv.slots != null && i < inv.slots.Length) ? inv.slots[i] : null;
            slotUIs[i].SetItem(item);
        }
    }
}

/// <summary>
/// Datos de un slot de UI. Asigna desde el Inspector.
/// </summary>
[System.Serializable]
public class SlotUI
{
    public Image iconImage;
    public Text nameText;

    public void SetItem(Item item)
    {
        bool hasItem = item != null;

        if (iconImage != null)
        {
            iconImage.sprite  = hasItem ? item.icon : null;
            iconImage.enabled = hasItem && item.icon != null;
        }

        if (nameText != null)
            nameText.text = hasItem ? item.itemName : "";
    }
}
