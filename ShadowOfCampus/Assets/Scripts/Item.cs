using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventario/Item")]
public class Item : ScriptableObject
{
    [Header("Información")]
    public string itemName = "Objeto";
    public Sprite icon;
    [TextArea] public string description = "";
}
