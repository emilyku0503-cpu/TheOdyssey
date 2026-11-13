
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[Serializable]
public class MapRevealLevel
{
    public string levelName;
    public string sceneName;

    [Header("Route Animation")]
    public VideoClip routeVideo;
    public GameObject completedRouteImage;

    [Header("Hide When Video Starts")]
    public GameObject hideWhenVideoStarts;

    [Header("Level UI")]
    public RectTransform levelUI;
    public CanvasGroup canvasGroup;
    public Button levelButton;
}

public class MapRevealManager : MonoBehaviour
{
    [Header("Levels - In Progression Order")]
    [SerializeField] private List<MapRevealLevel> levels = new();

    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoOverlay;
    [SerializeField] private RenderTexture videoTexture;

    [Header("UI Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float slideDistance = 120f;

    [Header("UI Sounds")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip selectSound;

    private const string RevealSaveKey =
        "Odyssey_MapLastRevealed";

    private readonly List<Vector2> finalPositions = new();
    private readonly List<UnityAction> buttonActions = new();

    private bool isBusy = true;
    private bool videoFinished;
    private bool videoFailed;

    private int highestUnlocked;
    private int lastRevealed;

    private void Awake()
    {
        // Configure VideoPlayer
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.renderMode =
                VideoRenderMode.RenderTexture;

            videoPlayer.targetTexture = videoTexture;

            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
        }

        if (videoOverlay != null)
        {
            videoOverlay.texture = videoTexture;
            videoOverlay.raycastTarget = false;
            videoOverlay.enabled = false;
        }

        if (levels.Count == 0)
            return;

        // Load saved progress
        highestUnlocked = Mathf.Clamp(
            LevelProgress.HighestUnlockedLevel,
            1,
            levels.Count
        );

        lastRevealed = Mathf.Clamp(
            PlayerPrefs.GetInt(RevealSaveKey, 0),
            0,
            levels.Count
        );

        // Handle reset progress
        if (lastRevealed > highestUnlocked)
        {
            lastRevealed = 0;
            PlayerPrefs.SetInt(RevealSaveKey, 0);
            PlayerPrefs.Save();
        }

        // Initialize level UI
        for (int i = 0; i < levels.Count; i++)
        {
            int index = i;
            MapRevealLevel level = levels[i];

            Vector2 finalPosition =
                level.levelUI != null
                ? level.levelUI.anchoredPosition
                : Vector2.zero;

            finalPositions.Add(finalPosition);

            bool revealed = i < lastRevealed;
            bool unlocked = i < highestUnlocked;

            if (level.levelUI != null)
            {
                level.levelUI.gameObject.SetActive(true);

                level.levelUI.anchoredPosition =
                    revealed
                    ? finalPosition
                    : finalPosition +
                      Vector2.down * slideDistance;
            }

            if (level.canvasGroup != null)
            {
                level.canvasGroup.alpha =
                    revealed ? 1f : 0f;

                level.canvasGroup.interactable =
                    revealed && unlocked;

                level.canvasGroup.blocksRaycasts =
                    revealed && unlocked;
            }

            if (level.completedRouteImage != null)
            {
                level.completedRouteImage.SetActive(
                    revealed
                );
            }

            if (level.levelButton != null)
            {
                level.levelButton.interactable =
                    revealed && unlocked;

                UnityAction action =
                    () => SelectLevel(index);

                level.levelButton.onClick.AddListener(action);
                buttonActions.Add(action);
            }
            else
            {
                buttonActions.Add(null);
            }
        }

    }

    private IEnumerator Start()
    {
        if (levels.Count == 0)
        {
            Debug.LogWarning("No levels assigned!");
            yield break;
        }

        Debug.Log(
            "Highest Unlocked: " + highestUnlocked +
            " | Last Revealed: " + lastRevealed
        );

        // Wait until opening Ink Transition ends
        if (InkSceneTransition.Instance != null)
        {
            while (InkSceneTransition.Instance.IsBusy)
                yield return null;
        }

        // Reveal newly unlocked levels
        for (int i = lastRevealed; i < highestUnlocked; i++)
        {
            MapRevealLevel level = levels[i];

            Debug.Log("Revealing: " + level.levelName);

            // Hide previous static image before video starts
            if (level.hideWhenVideoStarts != null)
            {
                level.hideWhenVideoStarts.SetActive(false);
            }

            // STEP 1: Play route video
            if (level.routeVideo != null)
            {
                yield return PlayRouteVideo(
                    level.routeVideo
                );
            }

            // STEP 2: Show static route image
            if (level.completedRouteImage != null)
            {
                level.completedRouteImage.SetActive(true);
            }

            // STEP 3: Slide Up + Fade In level UI
            yield return RevealLevelUI(i);

            // STEP 4: Save reveal progress
            PlayerPrefs.SetInt(RevealSaveKey, i + 1);
            PlayerPrefs.Save();
        }
        // Keep images hidden if their replacement
        // animation has already been revealed
        for (int i = 0; i < lastRevealed; i++)
        {
            GameObject imageToHide = levels[i].hideWhenVideoStarts;

            if (imageToHide != null)
            {
                imageToHide.SetActive(false);
            }
        }

        isBusy = false;

        Debug.Log("Map reveal sequence completed!");
    }

    private IEnumerator PlayRouteVideo(VideoClip clip)
    {
        if (videoPlayer == null ||
            videoOverlay == null ||
            videoTexture == null)
        {
            Debug.LogWarning(
                "Missing VideoPlayer references!"
            );
            yield break;
        }

        videoPlayer.Stop();

        videoFinished = false;
        videoFailed = false;

        videoPlayer.clip = clip;
        videoOverlay.enabled = false;

        // Clear previous frame
        if (!videoTexture.IsCreated())
            videoTexture.Create();

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = videoTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;

        // Prepare video
        videoPlayer.Prepare();

        float prepareTimer = 0f;

        while (!videoPlayer.isPrepared &&
               !videoFailed &&
               prepareTimer < 10f)
        {
            prepareTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared || videoFailed)
        {
            Debug.LogWarning(
                "Video preparation failed: " + clip.name
            );
            yield break;
        }

        // Show video overlay
        videoOverlay.enabled = true;

        // Play video
        videoPlayer.Play();

        float playbackTimer = 0f;

        float maxPlaybackTime = Mathf.Max(
            10f,
            (float)clip.length + 5f
        );

        while (!videoFinished &&
               !videoFailed &&
               playbackTimer < maxPlaybackTime)
        {
            playbackTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        videoPlayer.Stop();

        // Hide video overlay
        videoOverlay.enabled = false;
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        videoFinished = true;
    }

    private void OnVideoError(
        VideoPlayer player,
        string message
    )
    {
        videoFailed = true;
        Debug.LogError("Map video error: " + message);
    }

    private IEnumerator RevealLevelUI(int index)
    {
        MapRevealLevel level = levels[index];

        if (level.levelUI == null ||
            level.canvasGroup == null)
        {
            Debug.LogError(
                "Missing Level UI references: " +
                level.levelName
            );

            yield break;
        }

        Vector2 targetPosition =
            finalPositions[index];

        Vector2 startPosition =
            targetPosition +
            Vector2.down * slideDistance;

        float elapsed = 0f;

        float duration = Mathf.Max(
            0.01f,
            fadeInDuration
        );

        level.levelUI.anchoredPosition =
            startPosition;

        level.canvasGroup.alpha = 0f;

        // Animate UI
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            float smoothT = Mathf.SmoothStep(
                0f, 1f, t
            );

            // Slide Up
            level.levelUI.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            // Fade In
            level.canvasGroup.alpha = smoothT;

            yield return null;
        }

        // Complete animation
        level.levelUI.anchoredPosition =
            targetPosition;

        level.canvasGroup.alpha = 1f;

        // Activate button
        level.canvasGroup.interactable = true;
        level.canvasGroup.blocksRaycasts = true;

        if (level.levelButton != null)
        {
            level.levelButton.interactable = true;
        }

        Debug.Log(level.levelName + " unlocked!");
    }

    private void SelectLevel(int index)
    {
        Debug.Log(
       "[MAP CLICK] Index: " + index +
       " | IsBusy: " + isBusy +
       " | InkBusy: " +
       (InkSceneTransition.Instance != null &&
        InkSceneTransition.Instance.IsBusy)
        );

        if (isBusy ||
            index < 0 ||
            index >= levels.Count)
            return;

        if (InkSceneTransition.Instance != null &&
            InkSceneTransition.Instance.IsBusy)
            return;

        int levelNumber = index + 1;

        bool unlocked =
            levelNumber == 1 ||
            LevelProgress.IsUnlocked(levelNumber);

        if (!unlocked)
            return;

        string sceneName = levels[index].sceneName;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Invalid scene name: " + sceneName
            );
            return;
        }

        isBusy = true;

        if (uiAudioSource != null &&
            selectSound != null)
        {
            uiAudioSource.PlayOneShot(selectSound);
        }

        StartCoroutine(LoadLevel(sceneName));
    }

    private IEnumerator LoadLevel(string sceneName)
    {
        // Wait until click sound ends
        if (selectSound != null &&
            uiAudioSource != null)
        {
            yield return new WaitForSecondsRealtime(
                selectSound.length
            );
        }

        // Ink transition
        if (InkSceneTransition.Instance != null)
        {
            while (InkSceneTransition.Instance.IsBusy)
                yield return null;

            InkSceneTransition.Instance.LoadSceneWithInk(
                sceneName
            );
        }
        else
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }

        for (int i = 0; i < buttonActions.Count; i++)
        {
            if (buttonActions[i] != null &&
                levels[i].levelButton != null)
            {
                levels[i].levelButton.onClick.RemoveListener(
                    buttonActions[i]
                );
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG - Reset Map Reveal")]
    private void ResetMapReveal()
    {
        PlayerPrefs.DeleteKey(RevealSaveKey);
        PlayerPrefs.Save();

        Debug.Log("Map reveal history reset!");
    }
#endif


#if UNITY_EDITOR

    [ContextMenu("DEBUG - Test Level 2 Reveal")]
    private void DebugTestLevel2()
    {
        // Unlock Level 2
        LevelProgress.CompleteLevel(1);

        // Mark Level 1 animation as already watched
        PlayerPrefs.SetInt("Odyssey_MapLastRevealed", 1);
        PlayerPrefs.Save();

        Debug.Log("Level 2 reveal ready for testing!");
    }

#endif

#if UNITY_EDITOR

    [ContextMenu("DEBUG - Test Level 3 Reveal")]
    private void DebugTestLevel3()
    {
        // Unlock Level 3 by completing Level 2
        LevelProgress.CompleteLevel(2);

        // Mark Level 1 and Level 2 reveals as completed
        PlayerPrefs.SetInt("Odyssey_MapLastRevealed", 2);
        PlayerPrefs.Save();

        Debug.Log("Level 3 reveal ready for testing!");
    }

#endif


#if UNITY_EDITOR

    [ContextMenu("DEBUG - Reset All Progress")]
    private void DebugResetAllProgress()
    {
        // Reset level unlock progress
        LevelProgress.ResetProgress();

        // Reset map animation history
        PlayerPrefs.DeleteKey("Odyssey_MapLastRevealed");

        PlayerPrefs.Save();

        Debug.Log("All level and map progress reset!");
    }

#endif

}
