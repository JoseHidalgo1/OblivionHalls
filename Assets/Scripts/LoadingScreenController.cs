using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    public VideoClip loadingVideoClip;
    public string loadingAudioTrackName = "Loading";
    public string nextAudioTrackName;
    public string targetSceneName = "MainMenu";
    public bool blockInput = true;
    public bool playLoadingAudio = true;

    private Canvas loadingCanvas;
    private RawImage loadingImage;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private float videoDuration = 0f;
    private AsyncOperation sceneLoadOperation;

    public static LoadingScreenController Create(VideoClip clip, string audioTrackName)
    {
        return Create(clip, audioTrackName, null);
    }

    public static LoadingScreenController Create(VideoClip clip, string audioTrackName, string nextAudioTrackName)
    {
        GameObject loaderObject = new GameObject("LoadingScreenController");
        LoadingScreenController controller = loaderObject.AddComponent<LoadingScreenController>();
        controller.loadingVideoClip = clip;
        controller.loadingAudioTrackName = audioTrackName;
        controller.nextAudioTrackName = nextAudioTrackName;
        return controller;
    }

    private void Awake()
    {
        if (loadingVideoClip == null)
        {
            Debug.LogWarning("LoadingScreenController requires a loading video clip.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (loadingVideoClip == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(targetSceneName) && SceneManager.GetActiveScene().name != targetSceneName)
        {
            sceneLoadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            if (sceneLoadOperation != null)
            {
                sceneLoadOperation.allowSceneActivation = false;
            }
        }

        CreateLoadingOverlay();
        StartCoroutine(PlayLoadingSequence());
    }

    private IEnumerator PlayLoadingSequence()
    {
        if (playLoadingAudio)
        {
            AudioManager audioManager = AudioManager.GetOrCreate();
            if (audioManager != null)
            {
                audioManager.PlayTrack(loadingAudioTrackName);
            }
        }

        videoPlayer.Play();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoDuration = Mathf.Max(0.1f, (float)videoPlayer.clip.length);
        float elapsed = 0f;

        while (elapsed < videoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (sceneLoadOperation != null)
        {
            while (!sceneLoadOperation.isDone && sceneLoadOperation.progress < 0.9f)
            {
                yield return null;
            }

            if (!sceneLoadOperation.isDone)
            {
                sceneLoadOperation.allowSceneActivation = true;
            }

            while (!sceneLoadOperation.isDone)
            {
                yield return null;
            }
        }

        yield return null;

        TeardownLoadingOverlay();

        if (!string.IsNullOrEmpty(nextAudioTrackName))
        {
            AudioManager audioManager = AudioManager.Instance ?? AudioManager.GetOrCreate();
            audioManager?.PlayTrack(nextAudioTrackName);
        }

        Destroy(gameObject);
    }

    private void CreateLoadingOverlay()
    {
        GameObject canvasObject = new GameObject("LoadingScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        loadingCanvas = canvasObject.GetComponent<Canvas>();
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        if (blockInput)
        {
            canvasObject.AddComponent<CanvasGroup>();
        }

        canvasObject.transform.SetParent(transform, false);

        GameObject imageObject = new GameObject("LoadingVideoImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.localScale = Vector3.one;
        imageRect.sizeDelta = Vector2.zero;

        loadingImage = imageObject.GetComponent<RawImage>();
        loadingImage.color = Color.white;
        loadingImage.raycastTarget = blockInput;

        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        renderTexture.name = "LoadingVideoRenderTexture";
        loadingImage.texture = renderTexture;

        GameObject playerObject = new GameObject("LoadingVideoPlayer", typeof(VideoPlayer));
        playerObject.transform.SetParent(transform, false);
        videoPlayer = playerObject.GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = loadingVideoClip;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
    }

    private void TeardownLoadingOverlay()
    {
        if (loadingCanvas != null)
        {
            Destroy(loadingCanvas.gameObject);
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void OnDestroy()
    {
        TeardownLoadingOverlay();
    }
}
