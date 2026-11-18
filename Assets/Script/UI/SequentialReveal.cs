using System.Collections;
using UnityEngine;

// UI 요소들을 단계별로 차례차례 나타나게 합니다.
// 각 단계의 요소들은 위에서 살짝 내려오며 서서히 나타납니다 (slide 를 끄면 제자리에서 나타남).
public class SequentialReveal : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public string label;
        public RectTransform[] targets;
        [Tooltip("위에서 내려오는 효과를 쓸지 (다른 스크립트가 위치를 움직이는 요소는 끄세요)")]
        public bool slide = true;
    }

    public Step[] steps;

    [Tooltip("화면이 열리고 첫 요소가 나오기까지 기다리는 시간")]
    public float startDelay = 0.6f;
    [Tooltip("다음 단계가 나오기까지의 간격")]
    public float stepInterval = 0.55f;
    [Tooltip("한 요소가 나타나는 데 걸리는 시간")]
    public float revealDuration = 0.8f;
    [Tooltip("위에서 내려오는 거리 (픽셀)")]
    public float slideDistance = 30f;

    private void Awake()
    {
        // 시작하자마자 모두 숨겨 둠 (버튼은 나타나기 전엔 누를 수 없음)
        foreach (var step in steps)
            foreach (var t in step.targets)
            {
                if (t == null) continue;
                var cg = GetGroup(t);
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
    }

    private IEnumerator Start()
    {
        yield return Wait(startDelay);
        foreach (var step in steps)
        {
            foreach (var t in step.targets)
                if (t != null) StartCoroutine(Reveal(t, step.slide));
            yield return Wait(stepInterval);
        }
    }

    // 씬 로딩 직후 첫 프레임의 큰 시간 튐을 무시하도록 한 프레임 최대 1/30초로 계산
    private static float Dt() => Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

    private static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Dt(); yield return null; }
    }

    private IEnumerator Reveal(RectTransform t, bool slide)
    {
        var cg = GetGroup(t);
        Vector2 endPos = t.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, slide ? slideDistance : 0f);
        float time = 0f;
        while (time < revealDuration)
        {
            time += Dt();
            float k = Mathf.Clamp01(time / revealDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out
            cg.alpha = eased;
            if (slide) t.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            yield return null;
        }
        cg.alpha = 1f;
        if (slide) t.anchoredPosition = endPos;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private static CanvasGroup GetGroup(RectTransform t)
    {
        var cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
