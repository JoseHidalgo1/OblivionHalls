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
}
