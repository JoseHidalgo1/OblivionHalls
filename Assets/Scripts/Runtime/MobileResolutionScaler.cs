using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-150)]
public class MobileResolutionScaler : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnStartup()
    {
        if (GameObject.Find("_MobileResolutionScaler") != null)
            return;

        var go = new GameObject("_MobileResolutionScaler");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileResolutionScaler>();
    }

    void Start()
    {
        ApplyScalingToAllCanvases();
        ApplyCameraAspectCorrection();
    }

    private void ApplyScalingToAllCanvases()
    {
        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>();
        if (scalers == null || scalers.Length == 0)
            return;

        float deviceWidth = Screen.width;
        float deviceHeight = Screen.height;
        float deviceAspect = deviceWidth / deviceHeight;

        Vector2 referenceLandscape = new Vector2(1920, 1080);
        float referenceAspect = referenceLandscape.x / referenceLandscape.y;

        foreach (var scaler in scalers)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceLandscape;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = deviceAspect >= referenceAspect ? 0f : 1f;
            scaler.scaleFactor = Mathf.Clamp(scaler.scaleFactor, 0.5f, 2.0f);

            Debug.Log($"[MobileResolutionScaler] Applied scaler to {scaler.gameObject.name}: ref={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}");
        }
    }

    private void ApplyCameraAspectCorrection()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic)
            return;

        float targetAspect = 16f / 9f;
        float currentAspect = (float)Screen.width / Screen.height;
        float baseSize = mainCamera.orthographicSize;

        // Preserve a consistent horizontal view across aspect ratios.
        mainCamera.orthographicSize = baseSize * (targetAspect / currentAspect);
        Debug.Log($"[MobileResolutionScaler] Adjusted camera orthographic size to {mainCamera.orthographicSize} for aspect {currentAspect:F3}");
    }
}
