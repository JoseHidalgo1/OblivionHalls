using UnityEngine;

/// <summary>
/// Gestiona los datos del jugador y los 4 slots de inventario.
/// Adjunta este componente al mismo GameObject que PlayerHealth.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Datos del Jugador")]
    public string playerName = "Zyros";
    public int age = 21;
    public string occupation = "Estudiante";

    [Header("Inventario (4 slots)")]
    public Item[] slots = new Item[4];

    [Header("Item inicial (Bate Metálico)")]
    [SerializeField] private Item startingItem;

    private PlayerHealth playerHealth;

    public int CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : 0;
    public int MaxHealth => playerHealth != null ? playerHealth.MaxHealth : 0;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInChildren<PlayerHealth>();

        // Inicializar slots vacíos
        if (slots == null || slots.Length != 4)
            slots = new Item[4];

        // Colocar el item inicial en el slot 0
        if (startingItem != null)
            slots[0] = startingItem;
    }
}
