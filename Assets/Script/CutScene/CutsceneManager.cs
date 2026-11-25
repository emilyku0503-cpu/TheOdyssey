
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;

[Serializable]
public class DialogueSequence
{
    public Sprite image;

    [TextArea(3, 6)]
    public string dialogue;

    public AudioClip sound;
}


public class CutsceneManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private GameObject cutscenePanel;
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject dialoguePanel;


    [Header("Typewriter Settings")]
    [SerializeField, Min(0.01f)]
    private float typingSpeed = 0.04f;

    [SerializeField, Min(0f)]
    private float autoAdvanceDelay = 2f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool textFinished = false;
    private float autoAdvanceTimer = 0f;


    [Header("Dialogue Settings")]
    [SerializeField] private float dialogueDuration = 5f;
    [SerializeField] private List<DialogueSequence> dialogues = new();

    [Header("Ink Transition")]
    [SerializeField] private GameObject transitionPanel;
    [SerializeField] private VideoPlayer transitionPlayer;
    [SerializeField] private VideoClip inkFadeIn;
    [SerializeField] private VideoClip inkFadeOut;
    [SerializeField] private float transitionDuration = 5f;

    [Header("Dialogue Audio")]
    [SerializeField, Range(0f, 1f)]
    private float dialogueVolume = 1f;

    [SerializeField, Min(0.1f)]
    private float audioFadeDuration = 1f;

    private AudioSource activeAudio;
    private AudioSource standbyAudio;
    private Coroutine audioFadeRoutine;

    [Header("Dialogue Fade")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float dialogueFadeDuration = 1f;

    private Coroutine dialogueFadeRoutine;

    [Header("Background Music Fade")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField, Min(0.1f)] private float musicFadeOutDuration = 2f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName;

    private enum CutsceneState
    {
        Opening,
        Dialogue,
        Closing,
        Finished
    }

    private CutsceneState state = CutsceneState.Opening;
    private int currentIndex = -1;
    private float timer;


    private IEnumerator Start()
    {
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 0f;

        if (transitionPlayer != null)
        {
            transitionPlayer.playOnAwake = false;
            transitionPlayer.isLooping = false;
        }

        // Hide opening panel
        if (openingPanel != null)
            openingPanel.SetActive(false);

        // Show cutscene image behind the transition
        if (cutscenePanel != null)
            cutscenePanel.SetActive(true);

        // Hide dialogue box during opening transition
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (transitionPanel != null)
            transitionPanel.SetActive(false);

        // Display first cutscene image
        if (dialogues.Count > 0)
            ShowDialogue(0);

        // Play 5-second ink fade in
        yield return PlayInkTransition(inkFadeIn, false);

        // Start dialogues
        state = CutsceneState.Dialogue;
        NextSequence();
    }



    private void Update()
    {
        if (state != CutsceneState.Dialogue)
            return;

        // Enter key input
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            if (isTyping)
            {
                // First Enter: Show all text immediately
                CompleteTyping();
            }
            else if (textFinished)
            {
                // Second Enter: Go to next dialogue
                NextSequence();
            }

            return;
        }

        // Wait 2 seconds after typing finishes
        if (textFinished)
        {
            autoAdvanceTimer += Time.unscaledDeltaTime;

            if (autoAdvanceTimer >= autoAdvanceDelay)
            {
                NextSequence();
            }
        }
    }




    private void NextSequence()
    {
        // Stop the previous typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        textFinished = false;
        autoAdvanceTimer = 0f;

        currentIndex++;

        // All dialogues finished
        if (currentIndex >= dialogues.Count)
        {
            state = CutsceneState.Closing;
            StartCoroutine(EndCutscene());
            return;
        }

        // Hide opening panel
        if (openingPanel != null)
            openingPanel.SetActive(false);

        // Show cutscene panel
        if (cutscenePanel != null)
            cutscenePanel.SetActive(true);

        // Show dialogue panel
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // Update image and dialogue text
        ShowDialogue(currentIndex);

        // Audio crossfade
        FadeToDialogueSound(dialogues[currentIndex].sound);

        // First dialogue: Fade In
        if (currentIndex == 0)
        {
            StartDialogueFade(1f);
        }

        // Start typewriter animation
        StartTyping();
    }



    // Start typing the current dialogue
    private void StartTyping()
    {
        if (dialogueText == null)
        {
            isTyping = false;
            textFinished = true;
            return;
        }

        // Calculate the total visible character count
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.ForceMeshUpdate();

        int totalCharacters =
            dialogueText.textInfo.characterCount;

        // Hide all characters initially
        dialogueText.maxVisibleCharacters = 0;

        isTyping = true;
        textFinished = false;
        autoAdvanceTimer = 0f;

        typingCoroutine =
            StartCoroutine(TypeText(totalCharacters));
    }


    // Reveal the text one character at a time
    private IEnumerator TypeText(int totalCharacters)
    {
        for (int i = 1; i <= totalCharacters; i++)
        {
            yield return new WaitForSecondsRealtime(typingSpeed);

            dialogueText.maxVisibleCharacters = i;
        }

        // Typing complete
        isTyping = false;
        textFinished = true;
        autoAdvanceTimer = 0f;
        typingCoroutine = null;
    }


    // Press Enter to reveal the whole sentence
    private void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Instantly reveal the entire dialogue
        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = int.MaxValue;

        isTyping = false;
        textFinished = true;
        autoAdvanceTimer = 0f;
    }

    private void ShowDialogue(int index)
    {
        DialogueSequence sequence = dialogues[index];

        if (dialogueText != null)
            dialogueText.text = sequence.dialogue;

        if (cutsceneImage != null)
        {
            cutsceneImage.sprite = sequence.image;
            cutsceneImage.enabled = sequence.image != null;

            if (sequence.image != null)
            {
                AspectRatioFitter fitter =
                    cutsceneImage.GetComponent<AspectRatioFitter>();

                if (fitter != null)
                {
                    fitter.aspectMode =
                        AspectRatioFitter.AspectMode.EnvelopeParent;

                    fitter.aspectRatio =
                        sequence.image.rect.width /
                        sequence.image.rect.height;
                }
            }
        }
    }

    private IEnumerator EndCutscene()
    {
        Coroutine musicFade = StartCoroutine(FadeOutBackgroundMusic());

        // Fade out the last dialogue sound
        StartAudioFadeOut();

        // Fade out DialoguePanel
        StartDialogueFade(0f);

        // Closing: 5-second ink fade out
        bool loadingNextScene =
            !string.IsNullOrWhiteSpace(nextSceneName);

        yield return PlayInkTransition(
            inkFadeOut,
            loadingNextScene
        );

        // Never unload the scene while its music is still fading.
        yield return musicFade;

        state = CutsceneState.Finished;

        if (loadingNextScene)
        {
            SceneManager.LoadSceneAsync(nextSceneName);
        }
        else
        {
            Debug.Log(
                "Cutscene finished. No next scene assigned."
            );
        }
    }

    private IEnumerator FadeOutBackgroundMusic()
    {
        if (backgroundMusic == null || !backgroundMusic.isPlaying)
            yield break;

        float startVolume = backgroundMusic.volume;
        float duration = Mathf.Max(0.1f, musicFadeOutDuration);
        float elapsed = 0f;

        while (elapsed < duration && backgroundMusic != null)
        {
            elapsed += Time.unscaledDeltaTime;
            backgroundMusic.volume = Mathf.Lerp(startVolume, 0f,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (backgroundMusic != null)
        {
            backgroundMusic.volume = 0f;
            backgroundMusic.Stop();
        }
    }

    private IEnumerator PlayInkTransition(
        VideoClip clip,
        bool keepVisibleAfter
    )
    {
        float duration = Mathf.Max(0.1f, transitionDuration);

        if (clip == null ||
            transitionPlayer == null ||
            transitionPanel == null)
        {
            // If no video is assigned, preserve the timing
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        transitionPlayer.Stop();
        transitionPlayer.clip = clip;
        transitionPlayer.Prepare();

        // Wait for video preparation
        float prepareTimer = 0f;

        while (!transitionPlayer.isPrepared &&
               prepareTimer < 10f)
        {
            prepareTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!transitionPlayer.isPrepared)
        {
            Debug.LogWarning("Ink transition could not prepare.");
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        // Fit video playback duration to 5 seconds
        if (transitionPlayer.canSetPlaybackSpeed &&
            transitionPlayer.length > 0)
        {
            transitionPlayer.playbackSpeed =
                (float)(transitionPlayer.length / duration);
        }

        transitionPanel.SetActive(true);
        transitionPlayer.Play();

        yield return new WaitForSecondsRealtime(duration);

        if (keepVisibleAfter)
        {
            // Keep the final ink-covered frame while loading
            transitionPlayer.Pause();
        }
        else
        {
            transitionPlayer.Stop();
            transitionPanel.SetActive(false);
        }
    }


    private void StartDialogueFade(float targetAlpha)
    {
        if (dialogueCanvasGroup == null)
            return;

        if (dialogueFadeRoutine != null)
            StopCoroutine(dialogueFadeRoutine);

        dialogueFadeRoutine =
            StartCoroutine(FadeDialogue(targetAlpha));
    }

    private IEnumerator FadeDialogue(float targetAlpha)
    {
        float startAlpha = dialogueCanvasGroup.alpha;
        float elapsed = 0f;

        float duration = Mathf.Max(0.01f, dialogueFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth fade
            t = t * t * (3f - 2f * t);

            dialogueCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        dialogueCanvasGroup.alpha = targetAlpha;
        dialogueFadeRoutine = null;
    }
    private void Awake()
    {
        activeAudio = gameObject.AddComponent<AudioSource>();
        standbyAudio = gameObject.AddComponent<AudioSource>();

        SetupAudio(activeAudio);
        SetupAudio(standbyAudio);
    }

    private void SetupAudio(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }


    private void FadeToDialogueSound(AudioClip newClip)
    {
        // Stop previous fade coroutine
        if (audioFadeRoutine != null)
            StopCoroutine(audioFadeRoutine);

        AudioSource outgoing = activeAudio;
        AudioSource incoming = standbyAudio;

        // Prepare next sound
        incoming.Stop();
        incoming.clip = newClip;
        incoming.volume = 0f;

        if (newClip != null)
            incoming.Play();

        // Swap AudioSources
        activeAudio = incoming;
        standbyAudio = outgoing;

        audioFadeRoutine = StartCoroutine(
            CrossfadeAudio(outgoing, incoming)
        );
    }

    private IEnumerator CrossfadeAudio(
        AudioSource outgoing,
        AudioSource incoming)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, audioFadeDuration);

        float oldVolume = outgoing.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth transition
            t = t * t * (3f - 2f * t);

            // Previous sound fades out
            outgoing.volume = Mathf.Lerp(
                oldVolume, 0f, t
            );

            // Next sound fades in
            if (incoming.clip != null)
            {
                incoming.volume = Mathf.Lerp(
                    0f, dialogueVolume, t
                );
            }

            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;

        if (incoming.clip != null)
            incoming.volume = dialogueVolume;

        audioFadeRoutine = null;
    }


    private void StartAudioFadeOut()
    {
        if (audioFadeRoutine != null)
            StopCoroutine(audioFadeRoutine);

        audioFadeRoutine = StartCoroutine(FadeOutAllAudio());
    }

    private IEnumerator FadeOutAllAudio()
    {
        float startA = activeAudio.volume;
        float startB = standbyAudio.volume;

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, audioFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            activeAudio.volume = Mathf.Lerp(startA, 0f, t);
            standbyAudio.volume = Mathf.Lerp(startB, 0f, t);

            yield return null;
        }

        activeAudio.Stop();
        standbyAudio.Stop();

        activeAudio.volume = 0f;
        standbyAudio.volume = 0f;

        audioFadeRoutine = null;
    }

}
