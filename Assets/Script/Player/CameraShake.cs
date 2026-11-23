using UnityEngine;

// 메인 카메라 흔들기. Cinemachine 이 카메라 위치를 정한 "뒤"에 실행되어 매 프레임 살짝 밀어줌
// (다음 프레임에 Cinemachine 이 다시 위치를 잡으므로 흔들림이 쌓이지 않음).
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    private float strength, duration, time = 1f, total = 1f;

    public static void Shake(float strength, float duration)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var s = cam.GetComponent<CameraShake>();
        if (s == null) s = cam.gameObject.AddComponent<CameraShake>();
        s.strength = strength; s.total = Mathf.Max(0.01f, duration); s.time = 0f;
    }

    private void LateUpdate()
    {
        if (GameSettings.IsPaused || time >= total) return;
        time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float k = 1f - Mathf.Clamp01(time / total);
        transform.position += (transform.right * Random.Range(-1f, 1f) + transform.up * Random.Range(-1f, 1f)) * strength * k * k;
    }
}
