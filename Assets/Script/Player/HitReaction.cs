using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 맞았을 때의 손맛 + 리스폰 전환.
//  1) 히트스톱: 아주 잠깐(0.07초) 시간이 멈춤
//  2) 카메라 흔들림 + 화면 가장자리 붉은 번쩍임
//  3) 화면이 빠르게 어두워짐 → 그 사이에 체크포인트로 이동 → 다시 밝아짐 (순간이동이 안 보이게)
public class HitReaction : MonoBehaviour
{
    [Header("Hit Stop")]
    public float hitStopDuration = 0.07f;

    [Header("Camera Shake")]
    public float shakeStrength = 0.18f;
    public float shakeDuration = 0.3f;

    [Header("Red Flash")]
    public Color flashColor = new Color(0.8f, 0.05f, 0.03f, 0.35f);

    [Header("Respawn Transition (real seconds)")]
    [Tooltip("맞은 뒤 화면이 어두워지기 시작할 때까지")]
    public float beforeFade = 0.18f;
    public float fadeOut = 0.18f;
    public float holdDark = 0.12f;
    public float fadeIn = 0.4f;
    public Color fadeColor = new Color(0.02f, 0.03f, 0.06f, 1f);

    private Image overlay;
    private bool busy;

    private static float Dt() => Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

    // respawn: called while the screen is dark (null = no teleport, just the hit feel)
    public void Play(Action respawn)
    {
        if (busy) return;
        busy = true;
        StartCoroutine(Run(respawn));
    }

    private IEnumerator Run(Action respawn)
    {
        EnsureOverlay();
        CameraShake.Shake(shakeStrength, shakeDuration);

        // hit stop
        float prevScale = Time.timeScale;
        Time.timeScale = 0f;
        overlay.color = flashColor;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        yield return new WaitUntil(() => !GameSettings.IsPaused);
        Time.timeScale = prevScale <= 0f ? 1f : prevScale;

        if (respawn == null)
        {
            yield return Fade(flashColor, Clear(flashColor), 0.3f);
            busy = false;
            yield break;
        }

        // red flash fades a little, then quickly to dark
        yield return Fade(flashColor, Color.Lerp(flashColor, Clear(flashColor), 0.5f), beforeFade);
        SetControl(false);
        yield return Fade(overlay.color, fadeColor, fadeOut);

        respawn();                         // teleport while nobody can see it
        yield return new WaitForSecondsRealtime(holdDark);
        yield return new WaitUntil(() => !GameSettings.IsPaused);
        SetControl(true);
        yield return Fade(fadeColor, Clear(fadeColor), fadeIn);
        busy = false;
    }

    private void SetControl(bool on)
    {
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = on;
        var input = GetComponent<StarterAssets.StarterAssetsInputs>();
        if (input != null && !on) { input.move = Vector2.zero; input.jump = false; }
    }

    private static Color Clear(Color c) { c.a = 0f; return c; }

    private void EnsureOverlay()
    {
        if (overlay != null) return;
        var go = new GameObject("HitOverlay", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // over the HUD, under the death overlay
        var imgGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        overlay = imgGo.GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = Color.clear;
    }

    private IEnumerator Fade(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Dt();
            overlay.color = Color.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        overlay.color = to;
    }

    private void OnDisable()
    {
        if (busy) { if (!GameSettings.IsPaused) { Time.timeScale = 1f; SetControl(true); } if (overlay) overlay.color = Color.clear; busy = false; }
    }
}
