using UnityEngine;

// 이미지(배경)의 크기를 아주 천천히 바꿉니다.
// 기본값은 줌아웃: 살짝 확대된 상태에서 시작해 원래 크기로 돌아와, 마지막엔 테두리가 온전히 보입니다.
// (fromScale < toScale 로 두면 줌인)
public class SlowZoom : MonoBehaviour
{
    [Tooltip("시작 크기 배율 (1.06 = 6% 확대된 상태에서 시작)")]
    public float fromScale = 1.06f;

    [Tooltip("최종 크기 배율 (1 = 원래 크기)")]
    public float toScale = 1f;

    [Tooltip("최종 크기까지 걸리는 시간(초)")]
    public float duration = 18f;

    private Vector3 baseScale;
    private float elapsed;

    private void Awake()
    {
        baseScale = transform.localScale;
        transform.localScale = baseScale * fromScale;
    }

    private void Update()
    {
        if (elapsed >= duration) return;
        // 씬 로딩 직후 첫 프레임은 시간이 크게 튀므로 한 프레임 최대 1/30초로 제한
        elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float t = Mathf.Clamp01(elapsed / duration);
        t = 1f - (1f - t) * (1f - t); // 처음엔 조금 빠르고 끝으로 갈수록 느려짐 (ease-out)
        transform.localScale = baseScale * Mathf.Lerp(fromScale, toScale, t);
    }
}
