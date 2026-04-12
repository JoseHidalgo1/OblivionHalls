using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class MapSortingSetup : EditorWindow
{
    // Orden correcto: número más bajo = más atrás (fondo), más alto = más al frente
    private static readonly Dictionary<string, int> LayerOrders = new Dictionary<string, int>
    {
        { "water_floor3",              -14 },
        { "walls_under_water",         -13 },
        { "water_detailization2",      -12 },
        { "water_detailization",       -11 },
        { "Floor2_pool",               -10 },
        { "Floor2_darker_surface",     -9  },
        { "Floor",                     -8  },
        { "Floor_darker_surface",      -7  },
        { "Objects_under_wall",        -6  },
        { "Walls",                     -5  },
        { "Windows",                   -4  },
        { "Lights",                    -3  },
        { "traps",                     -2  },
        { "Objects",                   -1  },
        { "Objects2",                   0  },
    };

    private static readonly HashSet<string> SolidLayerNames = new HashSet<string>
    {
        "walls_under_water",
        "Walls",
        "Windows",
        "Lights",
        "traps",
        "Objects_under_wall",
        "Objects",
        "Objects2",
    };

    [MenuItem("Tools/Setup Map Sorting Layers")]
    public static void SetupSortingLayers()
    {
        EnsureSortingLayerExists("Background");
        EnsureSortingLayerExists("Player");

        // Buscar Dungeon1 en la escena
        GameObject dungeon = GameObject.Find("Dungeon1");
        if (dungeon == null)
        {
            EditorUtility.DisplayDialog("Map Sorting Setup",
                "No se encontró 'Dungeon1' en la escena.", "OK");
            return;
        }

        // Recoger TODOS los TilemapRenderer dentro de Dungeon1
        TilemapRenderer[] allRenderers = dungeon.GetComponentsInChildren<TilemapRenderer>(true);
        Debug.Log($"[MapSortingSetup] Encontrados {allRenderers.Length} TilemapRenderer en Dungeon1.");

        int modified = 0;

        foreach (TilemapRenderer tr in allRenderers)
        {
            string goName = tr.gameObject.name;
            Debug.Log($"[MapSortingSetup] Procesando: '{goName}'");

            if (LayerOrders.TryGetValue(goName, out int order))
            {
                tr.sortingLayerName = "Background";
                tr.sortingOrder = order;
                EditorUtility.SetDirty(tr);
                Debug.Log($"[MapSortingSetup] ✓ '{goName}' → sortingOrder={order}");
                modified++;
            }
            else
            {
                // No está en la lista: asignar Background con orden -15 (más al fondo)
                tr.sortingLayerName = "Background";
                tr.sortingOrder = -15;
                EditorUtility.SetDirty(tr);
                Debug.LogWarning($"[MapSortingSetup] '{goName}' no está en la lista → asignado a -15");
            }
        }

        // Marcar escena como modificada para que se guarde
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Map Sorting Setup",
            $"¡Listo! Se configuraron {modified} capas del mapa.\n" +
            $"Total TilemapRenderers encontrados: {allRenderers.Length}\n\n" +
            "Revisa la Consola de Unity para ver el detalle de cada capa.\n\n" +
            "Ahora ejecuta también:\nTools → Setup Player Sorting Layer", "OK");

        Debug.Log($"[MapSortingSetup] Completado: {modified}/{allRenderers.Length} capas configuradas.");
    }

    [MenuItem("Tools/Setup Player Sorting Layer")]
    public static void SetupPlayerSorting()
    {
        EnsureSortingLayerExists("Background");
        EnsureSortingLayerExists("Player");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Jugador");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            EditorUtility.DisplayDialog("Map Sorting Setup",
                "No se encontró el personaje.\nAsegúrate de que el GameObject del jugador tiene el Tag 'Player', o se llama 'Jugador' o 'Player'.", "OK");
            return;
        }

        int count = 0;

        // SpriteRenderer en el objeto raíz
        SpriteRenderer rootSr = player.GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.sortingLayerName = "Player";
            rootSr.sortingOrder = 0;
            EditorUtility.SetDirty(rootSr);
            count++;
            Debug.Log($"[MapSortingSetup] ✓ SpriteRenderer raíz '{player.name}' → Player / 0");
        }

        // SpriteRenderers en todos los hijos
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 0;
            EditorUtility.SetDirty(sr);
            count++;
            Debug.Log($"[MapSortingSetup] ✓ SpriteRenderer hijo '{sr.gameObject.name}' → Player / 0");
        }

        if (count == 0)
        {
            EditorUtility.DisplayDialog("Map Sorting Setup",
                $"Se encontró '{player.name}' pero NO tiene ningún SpriteRenderer.\nRevisa que el sprite del personaje esté en ese GameObject o en un hijo.", "OK");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Map Sorting Setup",
            $"¡Listo! '{player.name}' configurado:\n  Sorting Layer: Player\n  Order in Layer: 0\n  SpriteRenderers modificados: {count}", "OK");
    }

    [MenuItem("Tools/Fix Player Visibility (Black Sprite)")]
    public static void FixPlayerVisibility()
    {
        EnsureSortingLayerExists("Background");
        EnsureSortingLayerExists("Player");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Jugador");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            EditorUtility.DisplayDialog("Fix Player Visibility",
                "No se encontró el jugador. Usa Tag 'Player' o nombre 'Jugador'/'Player'.", "OK");
            return;
        }

        int fixedCount = 0;
        Material spritesDefault = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");

        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.color = Color.white;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 0;

            if (spritesDefault != null)
            {
                sr.sharedMaterial = spritesDefault;
            }

            EditorUtility.SetDirty(sr);
            fixedCount++;
        }

        if (fixedCount == 0)
        {
            EditorUtility.DisplayDialog("Fix Player Visibility",
                $"Se encontró '{player.name}' pero no tiene SpriteRenderer.", "OK");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Fix Player Visibility",
            $"¡Listo! Se corrigieron {fixedCount} SpriteRenderer(s) del jugador.\n\n" +
            "Aplicado:\n- Color blanco\n- Material Sprites/Default\n- Sorting Layer Player", "OK");
    }

    [MenuItem("Tools/Setup Map Colliders (Walls & Objects)")]
    public static void SetupMapColliders()
    {
        GameObject dungeon = ResolveMapRootFromSelectionOrName();
        if (dungeon == null)
        {
            EditorUtility.DisplayDialog("Setup Map Colliders",
                "No se encontró el mapa. Selecciona el GameObject raíz del mapa en la jerarquía y vuelve a ejecutar.", "OK");
            return;
        }

        int configured = 0;
        int skipped = 0;

        // 1) Capas tipo Tilemap
        Tilemap[] allTilemaps = dungeon.GetComponentsInChildren<Tilemap>(true);
        for (int index = 0; index < allTilemaps.Length; index++)
        {
            Tilemap tilemap = allTilemaps[index];
            string layerName = tilemap.gameObject.name;
            if (!SolidLayerNames.Contains(layerName))
            {
                skipped++;
                continue;
            }

            TilemapCollider2D tileCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tileCollider == null)
            {
                tileCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            }

            Rigidbody2D rb2d = tilemap.GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                rb2d = tilemap.gameObject.AddComponent<Rigidbody2D>();
            }

            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
            }

            rb2d.bodyType = RigidbodyType2D.Static;
            rb2d.simulated = true;

            tileCollider.isTrigger = false;
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            composite.isTrigger = false;

            EditorUtility.SetDirty(tilemap.gameObject);
            EditorUtility.SetDirty(tileCollider);
            EditorUtility.SetDirty(rb2d);
            EditorUtility.SetDirty(composite);

            configured++;
            Debug.Log($"[MapSortingSetup] Collider sólido configurado en '{layerName}'.");
        }

        // 2) Capas tipo SpriteRenderer (fallback para imports que no generan Tilemap)
        SpriteRenderer[] allSpriteLayers = dungeon.GetComponentsInChildren<SpriteRenderer>(true);
        for (int index = 0; index < allSpriteLayers.Length; index++)
        {
            SpriteRenderer spriteLayer = allSpriteLayers[index];
            if (spriteLayer == null)
            {
                continue;
            }

            string layerName = spriteLayer.gameObject.name;
            if (!SolidLayerNames.Contains(layerName))
            {
                continue;
            }

            // Si ya tiene collider 2D no lo duplicamos
            Collider2D existingCollider = spriteLayer.GetComponent<Collider2D>();
            if (existingCollider == null)
            {
                BoxCollider2D box = spriteLayer.gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = false;
            }
            else
            {
                existingCollider.isTrigger = false;
            }

            Rigidbody2D rb2d = spriteLayer.GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                rb2d = spriteLayer.gameObject.AddComponent<Rigidbody2D>();
            }

            rb2d.bodyType = RigidbodyType2D.Static;
            rb2d.simulated = true;

            EditorUtility.SetDirty(spriteLayer.gameObject);
            EditorUtility.SetDirty(rb2d);
            configured++;
            Debug.Log($"[MapSortingSetup] Collider sólido (SpriteLayer) configurado en '{layerName}'.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Map Colliders",
            $"¡Listo!\nCapas sólidas configuradas: {configured}\nCapas ignoradas: {skipped}\n\n" +
            "Ahora ejecuta: Tools → Setup Player Physics Collider", "OK");
    }

    [MenuItem("Tools/Setup Player Physics Collider")]
    public static void SetupPlayerPhysicsCollider()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Jugador");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            EditorUtility.DisplayDialog("Setup Player Physics",
                "No se encontró el jugador. Usa Tag 'Player' o nombre 'Jugador'/'Player'.", "OK");
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.simulated = true;

        Collider2D[] allColliders = player.GetComponentsInChildren<Collider2D>(true);
        if (allColliders.Length == 0)
        {
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.isTrigger = false;
            allColliders = player.GetComponentsInChildren<Collider2D>(true);
        }

        int colliderCount = 0;
        foreach (Collider2D col in allColliders)
        {
            col.isTrigger = false;
            EditorUtility.SetDirty(col);
            colliderCount++;
        }

        EditorUtility.SetDirty(rb);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Player Physics",
            $"¡Listo!\nJugador: {player.name}\nColliders ajustados: {colliderCount}\n\n" +
            "Configurado:\n- Rigidbody2D Dynamic\n- Gravity 0\n- Freeze Rotation\n- isTrigger = false", "OK");
    }

    [MenuItem("Tools/Force Physics Layers To Default")]
    public static void ForcePhysicsLayersToDefault()
    {
        GameObject mapRoot = ResolveMapRootFromSelectionOrName();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Jugador");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            EditorUtility.DisplayDialog("Force Physics Layers",
                "No se encontró el jugador.", "OK");
            return;
        }

        int changedObjects = 0;

        // Jugador a Default (layer 0)
        changedObjects += SetLayerRecursively(player, 0);

        // Mapa sólido a Default (layer 0)
        if (mapRoot != null)
        {
            Transform[] allChildren = mapRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < allChildren.Length; index++)
            {
                Transform child = allChildren[index];
                if (child == null)
                {
                    continue;
                }

                if (SolidLayerNames.Contains(child.name))
                {
                    changedObjects += SetLayerRecursively(child.gameObject, 0);
                }
            }
        }

        // Asegurar matriz de colisión Default-Default
        Physics2D.IgnoreLayerCollision(0, 0, false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Force Physics Layers",
            $"¡Listo!\nObjetos ajustados a layer Default: {changedObjects}\n\n" +
            "Se forzó colisión Default↔Default activa.", "OK");
    }

    [MenuItem("Tools/Diagnose Physics (Player vs Map)")]
    public static void DiagnosePhysics()
    {
        GameObject mapRoot = ResolveMapRootFromSelectionOrName();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Jugador");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            EditorUtility.DisplayDialog("Diagnose Physics",
                "No se encontró el jugador.", "OK");
            return;
        }

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>(true);

        Debug.Log("================ PHYSICS DIAGNOSIS ================");
        Debug.Log($"[Player] name={player.name} layer={LayerMask.LayerToName(player.layer)}({player.layer})");
        Debug.Log($"[Player] Rigidbody2D={(playerRb != null ? playerRb.bodyType.ToString() : "MISSING")}");
        Debug.Log($"[Player] Colliders={playerCols.Length}");

        for (int index = 0; index < playerCols.Length; index++)
        {
            Collider2D col = playerCols[index];
            Debug.Log($"[PlayerCollider] {col.GetType().Name} on {col.gameObject.name} trigger={col.isTrigger} enabled={col.enabled}");
        }

        int mapColliderCount = 0;
        if (mapRoot != null)
        {
            Collider2D[] mapCols = mapRoot.GetComponentsInChildren<Collider2D>(true);
            mapColliderCount = mapCols.Length;
            Debug.Log($"[Map] root={mapRoot.name} colliders={mapColliderCount}");

            for (int index = 0; index < mapCols.Length; index++)
            {
                Collider2D col = mapCols[index];
                if (SolidLayerNames.Contains(col.gameObject.name))
                {
                    string geometryInfo = string.Empty;
                    if (col is TilemapCollider2D tilemapCollider)
                    {
                        geometryInfo = $" shapeCount={tilemapCollider.shapeCount} compositeOperation={tilemapCollider.compositeOperation}";
                    }
                    else if (col is CompositeCollider2D compositeCollider)
                    {
                        geometryInfo = $" pathCount={compositeCollider.pathCount} pointCount={compositeCollider.pointCount} generation={compositeCollider.generationType}";
                    }

                    Debug.Log($"[MapCollider] {col.gameObject.name} type={col.GetType().Name} layer={LayerMask.LayerToName(col.gameObject.layer)}({col.gameObject.layer}) trigger={col.isTrigger}{geometryInfo}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Map] No map root resolved. Select map root and run diagnosis again.");
        }

        bool defaultCollidesWithDefault = !Physics2D.GetIgnoreLayerCollision(0, 0);
        Debug.Log($"[Matrix] Default<->Default collides = {defaultCollidesWithDefault}");
        Debug.Log("===================================================");

        EditorUtility.DisplayDialog("Diagnose Physics",
            $"Diagnóstico escrito en Console.\n\nPlayer colliders: {playerCols.Length}\nMap colliders: {mapColliderCount}", "OK");
    }

    [MenuItem("Tools/Force Direct Tilemap Colliders (No Composite)")]
    public static void ForceDirectTilemapColliders()
    {
        GameObject mapRoot = ResolveMapRootFromSelectionOrName();
        if (mapRoot == null)
        {
            EditorUtility.DisplayDialog("Force Direct Colliders",
                "No se encontró el mapa. Selecciona el objeto raíz del mapa y vuelve a ejecutar.", "OK");
            return;
        }

        int adjusted = 0;
        Tilemap[] tilemaps = mapRoot.GetComponentsInChildren<Tilemap>(true);
        for (int index = 0; index < tilemaps.Length; index++)
        {
            Tilemap tilemap = tilemaps[index];
            if (!SolidLayerNames.Contains(tilemap.gameObject.name))
            {
                continue;
            }

            TilemapCollider2D tileCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tileCollider == null)
            {
                tileCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            }

            Rigidbody2D rb = tilemap.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = tilemap.gameObject.AddComponent<Rigidbody2D>();
            }

            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                Object.DestroyImmediate(composite);
            }

            tileCollider.compositeOperation = Collider2D.CompositeOperation.None;
            tileCollider.isTrigger = false;
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = true;

            EditorUtility.SetDirty(tilemap.gameObject);
            EditorUtility.SetDirty(tileCollider);
            EditorUtility.SetDirty(rb);
            adjusted++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Force Direct Colliders",
            $"¡Listo! Se ajustaron {adjusted} capas sólidas con TilemapCollider2D directo (sin composite).", "OK");
    }

    [MenuItem("Tools/Build Fallback Blocking Colliders")]
    public static void BuildFallbackBlockingColliders()
    {
        BuildFallbackBlockingCollidersInternal(0.84f);
    }

    [MenuItem("Tools/Build Fallback Blocking Colliders (Tighter)")]
    public static void BuildFallbackBlockingCollidersTighter()
    {
        BuildFallbackBlockingCollidersInternal(0.72f);
    }

    [MenuItem("Tools/Enable Manual Colliders Mode")]
    public static void EnableManualCollidersMode()
    {
        GameObject mapRoot = ResolveMapRootFromSelectionOrName();
        if (mapRoot == null)
        {
            EditorUtility.DisplayDialog("Manual Colliders Mode",
                "No se encontró el mapa. Selecciona el objeto raíz del mapa y vuelve a ejecutar.", "OK");
            return;
        }

        int disabled = 0;
        Transform generatedRoot = mapRoot.transform.Find("GeneratedCollisionBlocks");

        // Desactivar todos los colliders automáticos del mapa para evitar choques no deseados
        Collider2D[] autoColliders = mapRoot.GetComponentsInChildren<Collider2D>(true);
        for (int index = 0; index < autoColliders.Length; index++)
        {
            Collider2D col = autoColliders[index];
            if (col == null)
            {
                continue;
            }

            string goName = col.gameObject.name;
            bool isGenerated = goName.StartsWith("col_") || (generatedRoot != null && col.transform.IsChildOf(generatedRoot));
            bool isSolidLayer = SolidLayerNames.Contains(goName);

            if (isGenerated || isSolidLayer || col is TilemapCollider2D || col is CompositeCollider2D)
            {
                col.enabled = false;
                EditorUtility.SetDirty(col);
                disabled++;
            }
        }

        // Crear contenedor para colliders manuales
        Transform manualRoot = mapRoot.transform.Find("ManualColliders");
        if (manualRoot == null)
        {
            GameObject manual = new GameObject("ManualColliders");
            manual.layer = 0;
            manual.transform.SetParent(mapRoot.transform, false);
            manualRoot = manual.transform;
        }

        // Crear ejemplo inicial para editar/mover
        if (manualRoot.Find("WallCollider_01") == null)
        {
            GameObject sample = new GameObject("WallCollider_01");
            sample.layer = 0;
            sample.transform.SetParent(manualRoot, false);
            BoxCollider2D box = sample.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
            sample.transform.position = mapRoot.transform.position;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Manual Colliders Mode",
            $"Listo. Colliders automáticos desactivados: {disabled}\n\n" +
            "Se creó 'ManualColliders/WallCollider_01'.\n" +
            "Duplica y mueve ese objeto para bloquear solo donde tú quieras.", "OK");
    }

    private static void BuildFallbackBlockingCollidersInternal(float colliderSizeFactor)
    {
        GameObject mapRoot = ResolveMapRootFromSelectionOrName();
        if (mapRoot == null)
        {
            EditorUtility.DisplayDialog("Fallback Blocking Colliders",
                "No se encontró el mapa. Selecciona el objeto raíz del mapa y vuelve a ejecutar.", "OK");
            return;
        }

        Tilemap[] tilemaps = mapRoot.GetComponentsInChildren<Tilemap>(true);
        if (tilemaps == null || tilemaps.Length == 0)
        {
            EditorUtility.DisplayDialog("Fallback Blocking Colliders",
                "No se encontraron Tilemaps en el mapa seleccionado.", "OK");
            return;
        }

        HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        Grid layoutGrid = null;

        for (int index = 0; index < tilemaps.Length; index++)
        {
            Tilemap tilemap = tilemaps[index];
            if (tilemap == null || !SolidLayerNames.Contains(tilemap.gameObject.name))
            {
                continue;
            }

            if (layoutGrid == null)
            {
                layoutGrid = tilemap.layoutGrid;
            }

            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cell))
                    {
                        occupiedCells.Add(cell);
                    }
                }
            }
        }

        Transform existingRoot = mapRoot.transform.Find("GeneratedCollisionBlocks");
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot.gameObject);
        }

        GameObject collisionRoot = new GameObject("GeneratedCollisionBlocks");
        collisionRoot.layer = 0;
        collisionRoot.transform.SetParent(mapRoot.transform, false);

        Rigidbody2D rb = collisionRoot.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        Vector3 cellSize = layoutGrid != null ? layoutGrid.cellSize : new Vector3(1f, 1f, 0f);
        float safeFactor = Mathf.Clamp(colliderSizeFactor, 0.35f, 1f);
        float colliderWidth = Mathf.Abs(cellSize.x) * safeFactor;
        float colliderHeight = Mathf.Abs(cellSize.y) * safeFactor;

        int created = 0;
        foreach (Vector3Int cell in occupiedCells)
        {
            GameObject block = new GameObject($"col_{cell.x}_{cell.y}");
            block.layer = 0;
            block.transform.SetParent(collisionRoot.transform, false);

            Vector3 worldCenter = layoutGrid != null
                ? layoutGrid.GetCellCenterWorld(cell)
                : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            block.transform.position = worldCenter;

            BoxCollider2D box = block.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
            box.size = new Vector2(colliderWidth, colliderHeight);

            created++;
        }

        EditorUtility.SetDirty(collisionRoot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Fallback Blocking Colliders",
            $"¡Listo!\nCeldas bloqueantes generadas: {created}\nFactor de tamaño: {safeFactor:0.00}\n\n" +
            "Se creó el objeto 'GeneratedCollisionBlocks' dentro del mapa.", "OK");
    }

    private static GameObject ResolveMapRootFromSelectionOrName()
    {
        if (Selection.activeGameObject != null)
        {
            return Selection.activeGameObject;
        }

        GameObject byName = GameObject.Find("Dungeon1");
        if (byName != null)
        {
            return byName;
        }

        // Fallback: buscar un objeto que tenga hijos con nombres de capas conocidas
        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int index = 0; index < allTransforms.Length; index++)
        {
            Transform transform = allTransforms[index];
            if (transform == null)
            {
                continue;
            }

            bool containsKnownLayer = transform
                .GetComponentsInChildren<Transform>(true)
                .Any(child => SolidLayerNames.Contains(child.name));

            if (containsKnownLayer)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static int SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
        {
            return 0;
        }

        int changed = 0;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform tr = transforms[index];
            if (tr == null)
            {
                continue;
            }

            if (tr.gameObject.layer != layer)
            {
                tr.gameObject.layer = layer;
                EditorUtility.SetDirty(tr.gameObject);
                changed++;
            }
        }

        return changed;
    }

    private static void EnsureSortingLayerExists(string layerName)
    {
        // Verificar si ya existe
        foreach (var layer in SortingLayer.layers)
        {
            if (layer.name == layerName) return;
        }

        // Añadirlo via SerializedObject (único modo en editor sin plugin)
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");

        sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
        SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
        newLayer.FindPropertyRelative("name").stringValue = layerName;
        newLayer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();

        tagManager.ApplyModifiedProperties();
        Debug.Log($"[MapSortingSetup] Sorting Layer '{layerName}' creado automáticamente.");
    }
}
