using System.Collections;
using UnityEngine;
using TMPro;
using StarterAssets;

// 화면 아래쪽에 "[SPACE] Jump" 같은 조작 힌트를 띄우고,
// 플레이어가 실제로 그 행동을 하면 체크 표시 후 사라진다.
public class TutorialHintUI : MonoBehaviour
{
    public enum Goal { None, Move, Look, Jump, DoubleJump, Sprint }

    public static TutorialHintUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Timing")]
    [SerializeField] private float fade = 0.3f;
    [Tooltip("행동 없이도 이 시간이 지나면 사라짐 (0 = 안 사라짐)")]
    [SerializeField] private float maxShowTime = 9f;
    [SerializeField] private float noGoalShowTime = 4f;

    private StarterAssetsInputs input;
    private ThirdPersonController controller;
    private Coroutine running;
    private Goal goal;
    private bool goalDone;
    private float progress;
    private RectTransform rect;
    private Vector2 basePos;

    private void Awake()
    {
        Instance = this;
        rect = (RectTransform)transform;
        basePos = rect.anchoredPosition;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (controller != null) controller.Jumped -= OnJumped;
    }

    private void FindPlayer()
    {
        if (input != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        input = p.GetComponent<StarterAssetsInputs>();
        controller = p.GetComponent<ThirdPersonController>();
        if (controller != null) controller.Jumped += OnJumped;
    }

    private void OnJumped(int count)
    {
        if (goal == Goal.Jump && count >= 1) goalDone = true;
        if (goal == Goal.DoubleJump && count >= 2) goalDone = true;
    }

    [Tooltip("대기 중인 힌트가 이보다 오래 기다렸으면 (이미 그 구간을 지나쳤으니) 건너뜀")]
    [SerializeField] private float queueExpire = 6f;

    private readonly System.Collections.Generic.Queue<(string keys, string body, Goal goal, float time)> queue =
        new System.Collections.Generic.Queue<(string, string, Goal, float)>();

    // if a hint is still waiting for the player, the next one waits its turn
    public void Show(string keys, string body, Goal g)
    {
        FindPlayer();
        if (running != null) { queue.Enqueue((keys, body, g, Time.unscaledTime)); return; }
        running = StartCoroutine(Run(keys, body, g));
    }

    private void Next()
    {
        running = null;
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            if (Time.unscaledTime - n.time > queueExpire) continue;
            running = StartCoroutine(Run(n.keys, n.body, n.goal));
            return;
        }
    }

    private IEnumerator Run(string keys, string body, Goal g)
    {
        goal = g; goalDone = false; progress = 0f;
        keyText.text = keys;
        bodyText.text = body;
        keyText.color = new Color(1f, 0.86f, 0.45f);

        // slide up + fade in (from whatever alpha we're at)
        float a0 = group.alpha;
        for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
        {
            float k = t / fade;
            group.alpha = Mathf.Lerp(a0, 1f, k);
            rect.anchoredPosition = basePos + Vector2.down * 25f * (1f - k);
            yield return null;
        }
        group.alpha = 1f; rect.anchoredPosition = basePos;

        float shown = 0f;
        float limit = g == Goal.None ? noGoalShowTime : maxShowTime;
        while (!goalDone && (limit <= 0f || shown < limit))
        {
            shown += Time.unscaledDeltaTime;
            CheckGoal();
            yield return null;
        }

        if (goalDone)
        {
            // little "done" pop: the key turns green
            keyText.color = new Color(0.55f, 0.9f, 0.45f);
            for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime) { rect.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t / 0.18f * Mathf.PI)); yield return null; }
            rect.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.6f);
        }

        for (float t = 0; t < fade; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / fade; yield return null; }
        group.alpha = 0f;
        goal = Goal.None;
        Next();
    }

    private void CheckGoal()
    {
        if (input == null) { FindPlayer(); return; }
        float dt = Time.unscaledDeltaTime;
        switch (goal)
        {
            case Goal.Move:
                if (input.move.sqrMagnitude > 0.04f) progress += dt;
                if (progress > 0.8f) goalDone = true;
                break;
            case Goal.Look:
                if (input.look.sqrMagnitude > 0.5f) progress += dt;
                if (progress > 0.5f) goalDone = true;
                break;
            case Goal.Sprint:
                if (input.sprint && input.move.sqrMagnitude > 0.04f) progress += dt;
                if (progress > 0.8f) goalDone = true;
                break;
            // Jump / DoubleJump are completed by the controller's Jumped event
        }
    }
}
