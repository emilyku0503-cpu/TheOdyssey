using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class InkSceneTransition : MonoBehaviour
{
    public static InkSceneTransition Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject transitionPanel;
    [SerializeField] private GameObject loadingCover;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Ink Videos")]
    [SerializeField] private VideoClip inkFadeIn;
    [SerializeField] private VideoClip inkFadeOut;

    public static event System.Action FadeInStarted;
    public static event System.Action FadeOutStarted;

    public bool IsBusy { get; private set; } = true;

    private void Awake()
    {
        // Keep only one transition system in this scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Video settings
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.waitForFirstFrame = true;

        // Cover the screen immediately
        transitionPanel.SetActive(true);

        if (loadingCover != null)
            loadingCover.SetActive(true);
    }

    private void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        IsBusy = true;

        yield return PlayInkVideo(inkFadeIn, true);

        transitionPanel.SetActive(false);

        if (loadingCover != null)
            loadingCover.SetActive(false);

        IsBusy = false;
    }

    // Load the next scene with Ink Fade Out
    public void LoadSceneWithInk(string sceneName)
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Invalid scene: " + sceneName);
            return;
        }

        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        IsBusy = true;

        Time.timeScale = 1f;

        yield return PlayInkVideo(inkFadeOut, false);

        // Keep the screen covered during loading
        transitionPanel.SetActive(true);

        if (loadingCover != null)
            loadingCover.SetActive(true);

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("Scene could not be loaded: " + sceneName);

            transitionPanel.SetActive(false);

            if (loadingCover != null)
                loadingCover.SetActive(false);

            IsBusy = false;
        }
    }

    private IEnumerator PlayInkVideo(VideoClip clip, bool isFadeIn)
    {
        if (clip == null || videoPlayer == null)
        {
            Debug.LogWarning("Ink video is not assigned!");
            yield break;
        }

        if (isFadeIn)
        {
            transitionPanel.SetActive(true);

            if (loadingCover != null)
                loadingCover.SetActive(true);
        }
        else
        {
            transitionPanel.SetActive(false);

            if (loadingCover != null)
                loadingCover.SetActive(false);
        }

        videoPlayer.Stop();
        videoPlayer.clip = clip;

        bool finished = false;
        bool firstFrameReady = false;
        bool hasError = false;

        VideoPlayer.EventHandler onFinished =
            vp => finished = true;

        VideoPlayer.FrameReadyEventHandler onFrameReady =
            (vp, frameIndex) => firstFrameReady = true;

        VideoPlayer.ErrorEventHandler onError =
            (vp, message) =>
            {
                Debug.LogError("Ink video error: " + message);
                hasError = true;
                finished = true;
            };

        videoPlayer.sendFrameReadyEvents = true;

        videoPlayer.loopPointReached += onFinished;
        videoPlayer.frameReady += onFrameReady;
        videoPlayer.errorReceived += onError;

        // Prepare the video
        videoPlayer.Prepare();

        float prepareTime = 0f;

        while (!videoPlayer.isPrepared &&
               !hasError &&
               prepareTime < 10f)
        {
            prepareTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (videoPlayer.isPrepared && !hasError)
        {
            videoPlayer.Play();

            // Wait until the first video frame is ready
            float waitTime = 0f;

            while (!firstFrameReady &&
                   !finished &&
                   waitTime < 3f)
            {
                waitTime += Time.unscaledDeltaTime;
                yield return null;
            }

            if (firstFrameReady && !hasError)
            {
                transitionPanel.SetActive(true);

                if (loadingCover != null)
                    loadingCover.SetActive(false);

                // Synchronize BGM with the transition
                if (isFadeIn)
                {
                    FadeInStarted?.Invoke();
                }
                else
                {
                    FadeOutStarted?.Invoke();
                }

                // Wait until the video finishes
                while (!finished)
                    yield return null;
            }
            else
            {
                Debug.LogWarning("Ink video first frame failed.");
                videoPlayer.Stop();
            }
        }
        else
        {
            Debug.LogError("Ink video preparation failed.");
            videoPlayer.Stop();
        }

        // Remove video event listeners
        videoPlayer.loopPointReached -= onFinished;
        videoPlayer.frameReady -= onFrameReady;
        videoPlayer.errorReceived -= onError;

        videoPlayer.sendFrameReadyEvents = false;

        if (isFadeIn)
            videoPlayer.Stop();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}