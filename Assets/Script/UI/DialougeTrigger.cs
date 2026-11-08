
using System.Collections;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueUI;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 10f;

    private bool hasTriggered = false;

    private void Start()
    {
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Trigger only once
        if (hasTriggered)
            return;

        hasTriggered = true;

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
            StartCoroutine(HideDialogue());
        }

        Debug.Log("Dialogue Triggered!");
    }

    private IEnumerator HideDialogue()
    {
        yield return new WaitForSeconds(displayDuration);

        if (dialogueUI != null)
            dialogueUI.SetActive(false);

        Debug.Log("Dialogue Hidden!");
    }
}
