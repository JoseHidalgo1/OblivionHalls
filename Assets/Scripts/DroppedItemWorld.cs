using UnityEngine;

public class DroppedItemWorld : MonoBehaviour
{
    [SerializeField] private Item item;

    public Item Item => item;

    public void SetItem(Item value)
    {
        item = value;
    }
}
