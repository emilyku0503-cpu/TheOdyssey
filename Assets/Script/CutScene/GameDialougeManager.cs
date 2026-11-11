
using UnityEngine;
using UnityEngine.UI;
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

    private int currentDialogueIndex = 0;
    private Coroutine autoAdvanceCoroutine;
    private bool isPlaying = false;

    private void Start()
    {
        if (dialogues == null || dialogues.Length == 0)
            return;

        currentDialogueIndex = 0;
        isPlaying = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowDialogue();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (EnterPressed())
        {
            NextDialogue();
        }
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

    private void ShowDialogue()
    {
        DialogueEntry entry = dialogues[currentDialogueIndex];

        // Update character name
        if (characterName != null)
            characterName.text = entry.characterName;

        // Update dialogue text
        if (dialogueText != null)
            dialogueText.text = entry.dialogue;

        // Update character profile
        UpdateCharacterProfile(entry.characterName);

        // Reset auto advance timer
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);

        autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
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

    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);

        autoAdvanceCoroutine = null;
        NextDialogue();
    }

    private void NextDialogue()
    {
        if (!isPlaying)
            return;

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        currentDialogueIndex++;

        if (currentDialogueIndex < dialogues.Length)
        {
            ShowDialogue();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isPlaying = false;

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);

            autoAdvanceCoroutine = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
