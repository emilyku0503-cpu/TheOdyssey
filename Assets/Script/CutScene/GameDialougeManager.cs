
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameDialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text characterName;
    [SerializeField] private Image characterProfile;

    [Header("Character Profiles")]
    [SerializeField] private Sprite athenaProfile;
    [SerializeField] private Sprite odysseusProfile;

    [System.Serializable]
    public class DialogueEntry
    {
        public string characterName;

        [TextArea(3, 6)]
        public string dialogue;
    }

    [Header("Dialogue Settings")]
    [SerializeField] private DialogueEntry[] dialogues;

    [Header("Auto Advance Settings")]
    [SerializeField] private float autoAdvanceDelay = 3f;
    [SerializeField] private bool requireEnterOnLastDialogue = true;

    [Header("Dialogue Animation")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float slideDistance = 120f;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private float sceneStartFadeDuration = 1f;
    [SerializeField] private float sceneEndFadeDuration = 1f;

    private RectTransform panelRect;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    private enum DialogueState
    {
        Inactive,
        ScreenTransition,
        Entering,
        Visible,
        Exiting
    }

    private DialogueState state = DialogueState.Inactive;

    private bool skipFadeInRequested;
    private bool advanceRequested;

    private void Awake()
    {
        if (dialoguePanel == null || blackFadeImage == null)
        {
            Debug.LogError(
                "GameDialogueManager: Missing Dialogue Panel or Black Fade Image!"
            );
            enabled = false;
            return;
        }

        panelRect = dialoguePanel.GetComponent<RectTransform>();
        canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();

        if (panelRect == null || canvasGroup == null)
        {
            Debug.LogError(
                "Dialogue Panel requires RectTransform and CanvasGroup!"
            );
            enabled = false;
            return;
        }

        originalPosition = panelRect.anchoredPosition;

        canvasGroup.alpha = 0f;
        dialoguePanel.SetActive(false);

        // Force black screen immediately
        blackFadeImage.gameObject.SetActive(true);
        blackFadeImage.enabled = true;
        blackFadeImage.raycastTarget = true;

        SetBlackAlpha(1f);

        Debug.Log("Awake: Black screen initialized");
    }

    private IEnumerator Start()
    {
        state = DialogueState.ScreenTransition;

        // Ensure the first frame renders completely black
        SetBlackAlpha(1f);

        yield return null;

        Debug.Log("Starting Fade In. Alpha: " +
                  blackFadeImage.color.a);

        // Black -> Transparent
        yield return FadeScreen(
            1f,
            0f,
            sceneStartFadeDuration
        );

        Debug.Log("Fade In Completed!");

        blackFadeImage.raycastTarget = false;


        if (dialogues == null || dialogues.Length == 0)
        {
            Debug.LogWarning("No dialogues assigned.");
            state = DialogueState.Inactive;
            yield break;
        }

        dialoguePanel.SetActive(true);

        // Play all dialogue entries
        for (int i = 0; i < dialogues.Length; i++)
        {
            ShowDialogue(i);

            // Slide Up + Fade In
            state = DialogueState.Entering;
            skipFadeInRequested = false;

            yield return FadeDialogueIn();

            // Fully visible
            CompleteFadeIn();
            state = DialogueState.Visible;
            advanceRequested = false;

            // Wait for Enter or auto advance
            bool isLastDialogue = i == dialogues.Length - 1;

            bool allowAutoAdvance =
                !isLastDialogue || !requireEnterOnLastDialogue;

            float timer = 0f;

            while (!advanceRequested)
            {
                if (allowAutoAdvance &&
                    timer >= Mathf.Max(0f, autoAdvanceDelay))
                {
                    break;
                }

                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            // Slide Down + Fade Out
            state = DialogueState.Exiting;

            yield return FadeDialogueOut();
        }

        // All dialogues finished
        dialoguePanel.SetActive(false);

        // SCENE END: Transparent -> Black
        state = DialogueState.ScreenTransition;
        blackFadeImage.raycastTarget = true;

        yield return FadeScreen(
            0f,
            1f,
            sceneEndFadeDuration
        );

        // Load next scene
        if (string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                "Scene not found or not assigned: " + nextSceneName
            );

            // Restore screen if scene loading fails
            yield return FadeScreen(
                1f,
                0f,
                sceneStartFadeDuration
            );

            blackFadeImage.raycastTarget = false;
            state = DialogueState.Inactive;
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void Update()
    {
        if (!EnterPressed())
            return;

        if (state == DialogueState.Entering)
        {
            // First Enter: skip the entering animation
            skipFadeInRequested = true;
        }
        else if (state == DialogueState.Visible)
        {
            // Next Enter: proceed to next dialogue
            advanceRequested = true;
        }

        // Ignore Enter during screen transitions and Fade Out
    }

    private bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }
#endif

        return false;
    }

    private void ShowDialogue(int index)
    {
        DialogueEntry entry = dialogues[index];

        if (characterName != null)
            characterName.text = entry.characterName;

        if (dialogueText != null)
            dialogueText.text = entry.dialogue;

        UpdateCharacterProfile(entry.characterName);

        panelRect.anchoredPosition =
            originalPosition + Vector2.down * slideDistance;

        canvasGroup.alpha = 0f;
    }

    private void UpdateCharacterProfile(string speaker)
    {
        if (characterProfile == null)
            return;

        Sprite selectedProfile = null;

        switch (speaker?.Trim().ToLowerInvariant())
        {
            case "athena":
                selectedProfile = athenaProfile;
                break;

            case "odysseus":
                selectedProfile = odysseusProfile;
                break;
        }

        characterProfile.sprite = selectedProfile;
        characterProfile.enabled = selectedProfile != null;
    }

    private IEnumerator FadeDialogueIn()
    {
        Vector2 startPosition =
            originalPosition + Vector2.down * slideDistance;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, fadeInDuration);

        while (elapsed < duration)
        {
            if (skipFadeInRequested)
                break;

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            panelRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    originalPosition,
                    smoothT
                );

            canvasGroup.alpha = smoothT;

            yield return null;
        }

        CompleteFadeIn();
    }

    private void CompleteFadeIn()
    {
        panelRect.anchoredPosition = originalPosition;
        canvasGroup.alpha = 1f;
        skipFadeInRequested = false;
    }

    private IEnumerator FadeDialogueOut()
    {
        Vector2 endPosition =
            originalPosition + Vector2.down * slideDistance;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, fadeOutDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            panelRect.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    endPosition,
                    smoothT
                );

            canvasGroup.alpha = 1f - smoothT;

            yield return null;
        }

        panelRect.anchoredPosition = endPosition;
        canvasGroup.alpha = 0f;
    }

    private IEnumerator FadeScreen(
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);

        SetBlackAlpha(startAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                smoothT
            );

            SetBlackAlpha(alpha);

            yield return null;
        }

        SetBlackAlpha(endAlpha);
    }

    private void SetBlackAlpha(float alpha)
    {
        Color color = blackFadeImage.color;
        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = Mathf.Clamp01(alpha);

        blackFadeImage.color = color;
    }
}
