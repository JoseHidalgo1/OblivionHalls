using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventario")]
    [SerializeField] private int slotCount = 10;
    [SerializeField] private Item[] slots;

    [Header("Item inicial")]
    [SerializeField] private Item startingItem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (slotCount < 1)
        {
            slotCount = 10;
        }

        if (slots == null || slots.Length != slotCount)
        {
            Item[] newSlots = new Item[slotCount];
            if (slots != null)
            {
                int copyLength = Mathf.Min(slots.Length, newSlots.Length);
                for (int index = 0; index < copyLength; index++)
                {
                    newSlots[index] = slots[index];
                }
            }
            slots = newSlots;
        }

        if (startingItem != null && slots[0] == null)
        {
            slots[0] = startingItem;
        }
    }

    public int SlotCount => slots != null ? slots.Length : 0;

    public Item GetItemAt(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length)
        {
            return null;
        }

        return slots[index];
    }

    public void SetItemAt(int index, Item item)
    {
        if (slots == null || index < 0 || index >= slots.Length)
        {
            return;
        }

        slots[index] = item;
    }

    public bool AddItem(Item item)
    {
        if (item == null || slots == null)
        {
            return false;
        }

        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index] == null)
            {
                slots[index] = item;
                return true;
            }
        }

        return false;
    }

    public void ResizeSlots(int newSlotCount)
    {
        if (newSlotCount < 1)
        {
            newSlotCount = 1;
        }

        if (slots == null)
        {
            slots = new Item[newSlotCount];
            slotCount = newSlotCount;
            return;
        }

        if (slots.Length == newSlotCount)
        {
            slotCount = newSlotCount;
            return;
        }

        Item[] resized = new Item[newSlotCount];
        int copyLength = Mathf.Min(slots.Length, resized.Length);
        for (int index = 0; index < copyLength; index++)
        {
            resized[index] = slots[index];
        }

        slots = resized;
        slotCount = newSlotCount;
    }
}
