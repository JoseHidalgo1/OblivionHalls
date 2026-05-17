using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Maneja la interacción con cofres de tesoro.
/// El jugador debe estar cerca del cofre y presionar F para abrirlo y obtener un objeto aleatorio.
/// </summary>
public class ChestInteraction : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private Key interactionKey = Key.F;
    [SerializeField] private float interactionRange = 1.5f;

    [Header("Objeto aleatorio")]
    [SerializeField] private Item[] possibleLoot = { };

    [Header("UI Prompt")]
    [SerializeField] private string promptMessage = "Presiona F para abrir";
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float promptScale = 0.25f;
    [SerializeField] private string promptSortingLayer = "UI";
    [SerializeField] private int promptSortingOrder = 10000;
    [SerializeField] private Color promptColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private float promptFadeDuration = 1.0f;
    [SerializeField] private Font promptFont;

    [Header("Animación de apertura (Tiled Layers)")]
    [SerializeField] private float openAnimationDuration = 0.5f;
    [SerializeField] private Tilemap closedChestTilemap;
    [SerializeField] private Tilemap openChestTilemap;

    private static readonly System.Collections.Generic.Dictionary<Tilemap, System.Collections.Generic.Dictionary<Vector3Int, TileBase>> s_OpenChestTilesByMap = new System.Collections.Generic.Dictionary<Tilemap, System.Collections.Generic.Dictionary<Vector3Int, TileBase>>();
    private static readonly System.Collections.Generic.HashSet<Tilemap> s_ClearedOpenChestMaps = new System.Collections.Generic.HashSet<Tilemap>();

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private InventoryManager inventoryManager;
    private bool isOpen = false;
    private GameObject promptObject;
    private bool isPlayerInRange = false;
    private TextMesh promptText;
    private Vector3Int chestCellPosition;
    private TileBase chestOpenTileAtCell;

    void Start()
    {
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Jugador");
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            inventoryManager = playerObj.GetComponent<InventoryManager>();
        }

        // Calcular la celda del cofre en el tilemap
        if (closedChestTilemap != null && openChestTilemap != null)
        {
            if (!s_OpenChestTilesByMap.TryGetValue(openChestTilemap, out var mapTiles))
            {
                mapTiles = new System.Collections.Generic.Dictionary<Vector3Int, TileBase>();
                foreach (var cell in openChestTilemap.cellBounds.allPositionsWithin)
                {
                    TileBase tile = openChestTilemap.GetTile(cell);
                    if (tile != null)
                    {
                        mapTiles[cell] = tile;
                    }
                }
                s_OpenChestTilesByMap[openChestTilemap] = mapTiles;
            }

            if (!s_ClearedOpenChestMaps.Contains(openChestTilemap))
            {
                foreach (var cell in mapTiles.Keys)
                {
                    openChestTilemap.SetTile(cell, null);
                }
                s_ClearedOpenChestMaps.Add(openChestTilemap);
                Debug.Log("[ChestInteraction] Capa ChestOpen limpiada al iniciar.");
            }

            chestCellPosition = FindChestCellPosition();
            if (s_OpenChestTilesByMap[openChestTilemap].TryGetValue(chestCellPosition, out var openTile))
            {
                chestOpenTileAtCell = openTile;
            }
            else
            {
                Debug.LogWarning($"[ChestInteraction] No se encontró tile abierto en ChestOpen para la celda {chestCellPosition}.");
            }

            Debug.Log($"[ChestInteraction] Cofre en posición {chestCellPosition} inicializado. Capa abierta ocultada.");
        }

        // Cargar objetos posibles si no están asignados
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null)
            {
                inventoryManager = FindFirstObjectByType<InventoryManager>();
            }
        }

        if (possibleLoot == null || possibleLoot.Length == 0)
        {
            LoadLootInEditor();
            if (possibleLoot == null || possibleLoot.Length == 0)
            {
                Debug.LogWarning("[ChestInteraction] No se encontraron items. Asigna manualmente en el Inspector o mueve los assets a Resources/Items.");
            }
        }
    }

    private Vector3Int FindChestCellPosition()
    {
        Vector3Int baseCell = closedChestTilemap.WorldToCell(transform.position);
        if (closedChestTilemap.HasTile(baseCell) || openChestTilemap.HasTile(baseCell))
        {
            return baseCell;
        }

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkCell = new Vector3Int(baseCell.x + x, baseCell.y + y, baseCell.z);
                if (closedChestTilemap.HasTile(checkCell) || openChestTilemap.HasTile(checkCell))
                {
                    return checkCell;
                }
            }
        }

        return baseCell;
    }

    private System.Collections.Generic.List<Vector3Int> GetChestOpenRegion(Vector3Int startCell)
    {
        var region = new System.Collections.Generic.List<Vector3Int>();
        if (openChestTilemap == null || !s_OpenChestTilesByMap.TryGetValue(openChestTilemap, out var openTiles))
        {
            return region;
        }

        var visited = new System.Collections.Generic.HashSet<Vector3Int>();
        var stack = new System.Collections.Generic.Stack<Vector3Int>();
        stack.Push(startCell);

        while (stack.Count > 0)
        {
            Vector3Int cell = stack.Pop();
            if (visited.Contains(cell))
            {
                continue;
            }
            visited.Add(cell);

            if (!openTiles.ContainsKey(cell))
            {
                continue;
            }

            region.Add(cell);

            var neighbors = new[]
            {
                new Vector3Int(cell.x + 1, cell.y, cell.z),
                new Vector3Int(cell.x - 1, cell.y, cell.z),
                new Vector3Int(cell.x, cell.y + 1, cell.z),
                new Vector3Int(cell.x, cell.y - 1, cell.z)
            };
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && openTiles.ContainsKey(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return region;
    }

    void Update()
    {
        if (playerTransform == null || isOpen)
        {
            return;
        }

        // Calcular distancia al jugador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = distanceToPlayer <= interactionRange;

        // Si cambió el estado de rango
        if (inRange != isPlayerInRange)
        {
            isPlayerInRange = inRange;
            if (inRange)
            {
                ShowPrompt();
            }
            else
            {
                HidePrompt();
            }
        }

            // Detectar tecla F
            if (inRange)
            {
                if (Keyboard.current != null && Keyboard.current[interactionKey].wasPressedThisFrame)
                {
                    OpenChest();
                }
                else if (Keyboard.current == null && Input.GetKeyDown(KeyCode.F))
                {
                    OpenChest();
                }
            }
    }

    private void ShowPrompt()
    {
        if (promptObject != null)
        {
            return;
        }

        promptObject = new GameObject("ChestPrompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = promptLocalOffset;
        promptObject.transform.localRotation = Quaternion.identity;
        promptObject.transform.localScale = Vector3.one * promptScale;

        promptText = promptObject.AddComponent<TextMesh>();
        promptText.text = promptMessage;
        promptText.fontSize = 10;
        promptText.alignment = TextAlignment.Center;
        promptText.anchor = TextAnchor.MiddleCenter;
        promptText.characterSize = 0.25f;
        promptText.color = promptColor;

        if (promptFont == null)
        {
            promptFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (promptFont != null)
        {
            promptText.font = promptFont;
            MeshRenderer promptRenderer = promptObject.GetComponent<MeshRenderer>();
            promptRenderer.sharedMaterial = promptFont.material;
        }

        MeshRenderer renderer = promptObject.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = GetTargetSortingLayer(promptSortingLayer, "Player");
        renderer.sortingOrder = promptSortingOrder;

        // Fade out automático
        StartCoroutine(FadeOutPrompt());
    }

    private void HidePrompt()
    {
        if (promptObject != null)
        {
            Destroy(promptObject);
            promptObject = null;
        }
    }

    private IEnumerator FadeOutPrompt()
    {
        if (promptObject == null) yield break;

        MeshRenderer renderer = promptObject.GetComponent<MeshRenderer>();
        Color originalColor = renderer.material.color;
        float elapsedTime = 0f;

        // Solo esperar si el jugador se va
        while (promptObject != null && isPlayerInRange && elapsedTime < promptFadeDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        // No desvanecerse si el jugador sigue en rango
        if (isPlayerInRange)
        {
            yield break;
        }

        // Fade out
        elapsedTime = 0f;
        while (promptObject != null && elapsedTime < promptFadeDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / promptFadeDuration);
            Color newColor = originalColor;
            newColor.a = alpha;
            renderer.material.color = newColor;
        }

        if (promptObject != null)
        {
            Destroy(promptObject);
        }
    }
        private string GetTargetSortingLayer(string preferredLayer, string fallbackLayer)
        {
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name == preferredLayer)
                {
                    return preferredLayer;
                }
            }

            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name == fallbackLayer)
                {
                    return fallbackLayer;
                }
            }

            return SortingLayer.layers.Length > 0 ? SortingLayer.layers[0].name : fallbackLayer;
        }

    private void OpenChest()
    {
        isOpen = true;
        HidePrompt();

        Debug.Log("[ChestInteraction] Abriendo cofre...");

        // Al abrir el cofre: ocultar el cerrado y mostrar el abierto en todas las celdas del cofre
        if (closedChestTilemap != null && openChestTilemap != null)
        {
            var regionCells = GetChestOpenRegion(chestCellPosition);
            if (regionCells.Count > 0)
            {
                foreach (var cell in regionCells)
                {
                    closedChestTilemap.SetTile(cell, null);
                    if (s_OpenChestTilesByMap[openChestTilemap].TryGetValue(cell, out var openTile))
                    {
                        openChestTilemap.SetTile(cell, openTile);
                    }
                }
                Debug.Log($"[ChestInteraction] Cofre abierto en {regionCells.Count} celdas alrededor de {chestCellPosition}.");
            }
            else if (chestOpenTileAtCell != null)
            {
                closedChestTilemap.SetTile(chestCellPosition, null);
                openChestTilemap.SetTile(chestCellPosition, chestOpenTileAtCell);
                Debug.Log($"[ChestInteraction] Cofre abierto en celda {chestCellPosition} (fallback).");
            }
            else
            {
                Debug.LogWarning("[ChestInteraction] No se encontró región de cofre abierto para esta celda.");
            }
        }

        // Dar el loot al jugador
        if (possibleLoot != null && possibleLoot.Length > 0 && inventoryManager != null)
        {
            Item randomLoot = possibleLoot[Random.Range(0, possibleLoot.Length)];
            bool addedSuccessfully = inventoryManager.AddItem(randomLoot);
            if (addedSuccessfully)
            {
                Debug.Log($"[ChestInteraction] ¡Encontraste: {randomLoot.itemName}!");
            }
            else
            {
                Debug.LogWarning("[ChestInteraction] No hay espacio en el inventario.");
            }
        }
        else
        {
            Debug.LogWarning("[ChestInteraction] No hay items disponibles o InventoryManager no encontrado.");
        }

        StartCoroutine(AnimateChestOpening());
    }

    private void LoadLootInEditor()
    {
#if UNITY_EDITOR
        string[] assetGuids = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Items" });
        if (assetGuids != null && assetGuids.Length > 0)
        {
            possibleLoot = new Item[assetGuids.Length];
            for (int i = 0; i < assetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                possibleLoot[i] = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
            }
        }
#endif
        if (possibleLoot == null || possibleLoot.Length == 0)
        {
            possibleLoot = Resources.LoadAll<Item>("Items");
        }
    }

    private IEnumerator AnimateChestOpening()
    {
        // Si se usa Animator, el trigger ya fue enviado y el clip puede reproducirse.
        // Si no se usa Animator, el sprite se cambia inmediatamente a openedChestSprite.
        yield return new WaitForSeconds(openAnimationDuration);

        // El cofre permanece abierto (isOpen = true previene más interacciones)
        Debug.Log("[ChestInteraction] Cofre abierto.");
    }

    // Para debugging: mostrar rango de interacción
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
