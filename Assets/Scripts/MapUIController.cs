using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

// Permite que los objetos de UI se creen y mantengan visibles en el Editor.
[ExecuteAlways]
[DisallowMultipleComponent]
public class MapUIController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";

    [Header("Map Camera")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private Vector2 mapSize = new Vector2(40f, 40f);
    [SerializeField] private Tilemap[] mapTilemaps;
    [SerializeField] private bool autoDetectMapBounds = true;
    [SerializeField] private float fullMapPadding = 1.1f;
    [SerializeField] private LayerMask mapCullingMask = ~0;
    [SerializeField] private float fullMapSize = 22f;

    [Header("UI")]
    [SerializeField] private Sprite fullMapFrameSprite;
    [SerializeField] private float fullMapWidthRatio = 0.78f;
    [SerializeField] private float fullMapHeightRatio = 0.78f;
    [SerializeField] private Color fullMapBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Key toggleMapKey = Key.M;

    private Camera mapCamera;
    private RenderTexture mapRenderTexture;
    private GameObject fullMapRoot;
    private RawImage fullMapImage;
    private bool isFullMapOpen = false;

    private const string MapCameraName = "_MapUICamera";
    private const string MapCanvasName = "MapUICanvas";
    private const string TogglePrefKey = "MapUI_ToggleKey";

    // Event fired when the toggle key changes at runtime.
    public event Action<Key> OnToggleKeyChanged;

    // Remove extra GameObjects with the same name, keeping one preferred instance.
    // Preference order: an instance belonging to the active scene, then any instance in a scene, then the first found.
    private void RemoveExtraNamed(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == name).ToArray();
        if (all == null || all.Length <= 1)
            return;

        int keepIndex = -1;
        var active = SceneManager.GetActiveScene();

        // Try to keep one in the active scene
        for (int i = 0; i < all.Length; i++)
        {
            var go = all[i];
            if (go == null) continue;
            if (go.scene.IsValid() && go.scene == active)
            {
                keepIndex = i;
                break;
            }
        }

        // Otherwise prefer any that belongs to a scene (not assets)
        if (keepIndex == -1)
        {
            for (int i = 0; i < all.Length; i++)
            {
                var go = all[i];
                if (go == null) continue;
                if (go.scene.IsValid())
                {
                    keepIndex = i;
                    break;
                }
            }
        }

        if (keepIndex == -1)
            keepIndex = 0;

        for (int i = 0; i < all.Length; i++)
        {
            if (i == keepIndex) continue;
            var go = all[i];
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    private void Awake()
    {
        // Load saved toggle key if present
        if (PlayerPrefs.HasKey(TogglePrefKey))
        {
            try
            {
                toggleMapKey = (Key)PlayerPrefs.GetInt(TogglePrefKey, (int)toggleMapKey);
            }
            catch
            {
                // ignore and keep default
            }
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (autoDetectMapBounds)
        {
            if (mapTilemaps == null || mapTilemaps.Length == 0)
            {
                mapTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
            ComputeMapBounds();
        }

        SetupMapCamera();
        SetupUI();
        fullMapSize = Mathf.Max(fullMapSize, Mathf.Max(mapSize.x, mapSize.y) * 0.5f);
        UpdateCameraForMode();
    }

    // Public API to get/set the toggle key at runtime. Optionally save to PlayerPrefs.
    public Key GetToggleKey()
    {
        return toggleMapKey;
    }

    public void SetToggleKey(Key newKey, bool save = false)
    {
        if (toggleMapKey == newKey) return;
        toggleMapKey = newKey;
        if (save)
        {
            PlayerPrefs.SetInt(TogglePrefKey, (int)newKey);
            PlayerPrefs.Save();
        }
        OnToggleKeyChanged?.Invoke(newKey);
    }

    private void OnEnable()
    {
        // Avoid creating runtime UI or cameras during editor validation.
        // Actual runtime setup occurs in Awake()/Start().
    }

    private void OnValidate()
    {
        // Avoid creating runtime UI or cameras during editor validation.
        // Editor-time validation should not instantiate scene objects.
    }

    private void Update()
    {
        // Only in play mode; ignore editor updates for gameplay logic
        if (!Application.isPlaying)
            return;

        if (ActionInput.WasPressedThisFrame(GameAction.ToggleMap))
        {
            ToggleFullMap();
        }

        if (playerTransform == null)
        {
            return;
        }

        if (fullMapRoot != null && fullMapRoot.activeInHierarchy)
        {
            UpdateFullMapCamera();
        }
    }

    private void SetupMapCamera()
    {
        GameObject existing = GameObject.Find(MapCameraName);
        if (existing != null)
        {
            mapCamera = existing.GetComponent<Camera>();
            if (mapCamera != null)
            {
                mapCamera.cullingMask = mapCullingMask;
                mapCamera.clearFlags = CameraClearFlags.SolidColor;
                mapCamera.backgroundColor = Color.clear;
                    mapCamera.enabled = true;
                mapCamera.gameObject.SetActive(true);
            }
        }

        if (mapCamera == null)
        {
            GameObject mapCameraObject = new GameObject(MapCameraName);
            mapCameraObject.hideFlags = HideFlags.None;
            mapCamera = mapCameraObject.AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.farClipPlane = 100f;
            mapCamera.cullingMask = mapCullingMask;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Color.clear;
            mapCamera.enabled = true;
            mapCamera.depth = -100;
            mapCamera.gameObject.SetActive(true);
        }

        if (mapRenderTexture != null)
        {
            if (mapCamera != null && mapCamera.targetTexture == mapRenderTexture)
            {
                mapCamera.targetTexture = null;
            }
            mapRenderTexture.Release();
            mapRenderTexture = null;
        }
        mapRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.Default)
        {
            useMipMap = false,
            autoGenerateMips = false
        };
        mapRenderTexture.Create();
        mapCamera.targetTexture = mapRenderTexture;
    }

    private void SetupUI()
    {
        // Ensure we don't keep duplicated UI objects created previously in the scene/editor.
        RemoveExtraNamed("FullMapPanel");
        RemoveExtraNamed(MapCanvasName);
        RemoveExtraNamed(MapCameraName);

        // Find and reuse the existing FullMapPanel from the scene (manual placement by user)
        if (fullMapRoot == null)
        {
            GameObject existingFullRoot = GameObject.Find("FullMapPanel");
            if (existingFullRoot != null)
            {
                fullMapRoot = existingFullRoot;
            }
        }

        // Setup canvas (required for UI)
        GameObject canvasObject = GameObject.Find(MapCanvasName);
        Canvas canvas;
        if (canvasObject != null)
        {
            canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }
        }
        else
        {
            canvasObject = new GameObject(MapCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        canvas.sortingOrder = 1000;
        canvasObject.transform.SetParent(null);

        // If no FullMapPanel was found, create one
        if (fullMapRoot == null)
        {
            fullMapRoot = new GameObject("FullMapPanel", typeof(RectTransform), typeof(CanvasGroup));
            fullMapRoot.transform.SetParent(canvas.transform, false);
        }

        CreateFullMap(canvas.transform);
        SyncUIVisibility();
    }

    

    private void CreateFullMap(Transform parent)
    {
        // fullMapRoot already exists from manual placement or was created in SetupUI; just configure it.
        if (fullMapRoot == null)
            return;

        // Reparent to canvas if needed
        if (fullMapRoot.transform.parent != parent)
        {
            fullMapRoot.transform.SetParent(parent, false);
        }

        RectTransform rootRect = fullMapRoot.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = fullMapRoot.AddComponent<RectTransform>();

        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        float width = Screen.width > 0 ? Screen.width : 1920f;
        float height = Screen.height > 0 ? Screen.height : 1080f;
        rootRect.sizeDelta = new Vector2(width * fullMapWidthRatio, height * fullMapHeightRatio);
        rootRect.anchoredPosition = Vector2.zero;

        // Remove background image component if it exists (we only want the raw map image)
        Image background = fullMapRoot.GetComponent<Image>();
        if (background != null)
        {
            DestroyImmediate(background);
        }

        Transform rawTransform = fullMapRoot.transform.Find("FullMapImage");
        GameObject rawImageGO = rawTransform != null ? rawTransform.gameObject : null;
        if (rawImageGO == null)
        {
            rawImageGO = new GameObject("FullMapImage", typeof(RectTransform), typeof(RawImage));
            rawImageGO.transform.SetParent(fullMapRoot.transform, false);
        }

        RectTransform rawRect = rawImageGO.GetComponent<RectTransform>();
        if (rawRect == null)
            rawRect = rawImageGO.AddComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;

        RawImage rawImage = rawImageGO.GetComponent<RawImage>();
        if (rawImage == null)
            rawImage = rawImageGO.AddComponent<RawImage>();
        
        fullMapImage = rawImage;
        fullMapImage.texture = mapRenderTexture;
        fullMapImage.color = Color.white;
        fullMapImage.raycastTarget = false;
    }

    private void ToggleFullMap()
    {
        isFullMapOpen = !isFullMapOpen;
        SyncUIVisibility();
        UpdateCameraForMode();
    }

    private void SyncUIVisibility()
    {
        if (fullMapRoot != null)
        {
            fullMapRoot.SetActive(isFullMapOpen);
        }
    }

    private void UpdateCameraForMode()
    {
        if (mapCamera == null)
        {
            return;
        }
        mapCamera.enabled = isFullMapOpen;
        if (!isFullMapOpen)
            return;

        mapCamera.orthographicSize = fullMapSize;
        mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, -10f);
        if (mapCamera != null)
        {
            mapCamera.Render();
        }
    }

    private void ComputeMapBounds()
    {
        if (mapTilemaps == null || mapTilemaps.Length == 0)
        {
            return;
        }

        Bounds combinedBounds = new Bounds();
        bool firstBounds = true;

        foreach (Tilemap tilemap in mapTilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            Bounds localBounds = tilemap.localBounds;
            Vector3 worldMin = tilemap.transform.TransformPoint(localBounds.min);
            Vector3 worldMax = tilemap.transform.TransformPoint(localBounds.max);
            Bounds worldBounds = new Bounds();
            worldBounds.SetMinMax(Vector3.Min(worldMin, worldMax), Vector3.Max(worldMin, worldMax));

            if (firstBounds)
            {
                combinedBounds = worldBounds;
                firstBounds = false;
            }
            else
            {
                combinedBounds.Encapsulate(worldBounds);
            }
        }

        if (!firstBounds)
        {
            mapCenter = new Vector2(combinedBounds.center.x, combinedBounds.center.y);
            fullMapSize = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y) * fullMapPadding;
            if (fullMapSize < 1f)
            {
                fullMapSize = 1f;
            }
        }
    }

    

    private void UpdateFullMapCamera()
    {
        if (mapCamera == null)
        {
            return;
        }

        mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, -10f);
        mapCamera.orthographicSize = fullMapSize;
        mapCamera.Render();
    }

    private void OnDestroy()
    {
        if (mapRenderTexture != null)
        {
            mapRenderTexture.Release();
            if (Application.isPlaying)
            {
                Destroy(mapRenderTexture);
            }
            else
            {
                DestroyImmediate(mapRenderTexture);
            }
        }

        if (mapCamera != null)
        {
            if (Application.isPlaying)
            {
                Destroy(mapCamera.gameObject);
            }
            else
            {
                DestroyImmediate(mapCamera.gameObject);
            }
        }
    }
}
