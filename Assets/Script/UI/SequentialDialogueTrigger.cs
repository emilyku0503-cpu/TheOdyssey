using System.Collections;
using UnityEngine;
using TMPro;

// 플레이어가 트리거에 들어오면 대사 여러 줄을 순서대로 보여준다 (한 번만).
// 컷씬과 같은 대화창(초상화 + 이름판)을 아래에서 밀어 올리며 보여주고,
// 줄이 바뀔 때는 글자만 바뀐다. 플레이어 조작은 막지 않는다.
[RequireComponent(typeof(Collider))]
public class SequentialDialogueTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("이름판 글자 (비워두면 이름 표시 안 함)")]
    [SerializeField] private TMP_Text speakerNameText;

    [Header("Speaker")]
    [SerializeField] private string speakerName = "Athena";
    [Tooltip("대화창이 나타날 때 한 번 재생 (예: 신의 목소리 느낌의 종소리)")]
    [SerializeField] private AudioClip appearSound;
    [SerializeField, Range(0f, 1f)] private float appearVolume = 0.6f;

    [Header("Lines")]
    [TextArea(2, 4)]
    [SerializeField] private string[] lines =
    {
        "The rocks beyond the lotus rise too high, Odysseus.",
        "Seek the wings of Hermes. They will carry you upward."
    };

    [Header("Timing")]
    [SerializeField] private float secondsPerLine = 3.5f;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float slideDistance = 80f;
    [SerializeField] private bool triggerOnce = true;

    private CanvasGroup group;
    private CanvasGroup textGroup;
    private RectTransform panelRect;
    private Vector2 panelPos;
    private bool hasTriggered;
    private Coroutine running;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        if (dialogueUI != null)
        {
            group = dialogueUI.GetComponent<CanvasGroup>();
            if (group == null) group = dialogueUI.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            panelRect = dialogueUI.GetComponent<RectTransform>();
            if (panelRect != null) panelPos = panelRect.anchoredPosition;
            dialogueUI.SetActive(false);
        }
        if (dialogueText == null && dialogueUI != null)
            dialogueText = dialogueUI.GetComponentInChildren<TMP_Text>(true);
        if (dialogueText != null)
        {
            textGroup = dialogueText.GetComponent<CanvasGroup>();
            if (textGroup == null) textGroup = dialogueText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;
        if (dialogueUI == null || dialogueText == null || lines.Length == 0) return;

        hasTriggered = true;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        if (speakerNameText != null) speakerNameText.text = speakerName;
        dialogueText.text = lines[0];
        textGroup.alpha = 1f;

        dialogueUI.SetActive(true);
        if (appearSound != null) AudioSource.PlayClipAtPoint(appearSound, Camera.main ? Camera.main.transform.position : transform.position, appearVolume);

        // panel slides up + fades in
        yield return Animate(t =>
        {
            group.alpha = t;
            if (panelRect != null) panelRect.anchoredPosition = panelPos - Vector2.up * slideDistance * (1f - EaseOut(t));
        });

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                yield return Animate(t => textGroup.alpha = 1f - t);
                dialogueText.text = lines[i];
                yield return Animate(t => textGroup.alpha = t);
            }
            yield return new WaitForSeconds(secondsPerLine);
        }

        // panel slides down + fades out
        yield return Animate(t =>
        {
            group.alpha = 1f - t;
            if (panelRect != null) panelRect.anchoredPosition = panelPos - Vector2.up * slideDistance * EaseOut(t) * 0.5f;
        });

        if (panelRect != null) panelRect.anchoredPosition = panelPos;
        dialogueUI.SetActive(false);
        running = null;
    }

    private IEnumerator Animate(System.Action<float> step)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            step(Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        step(1f);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
