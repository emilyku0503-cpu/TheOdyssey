using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 죽음 연출: 조작 멈춤 → 자세가 굳은 채 쓰러짐(물에서는 가라앉음) + 슬로모션
//          → 화면이 붉게 번쩍 → 천천히 검게 어두워짐 → 끝나면 onDone 호출 (GameOver 로 이동)
public class DeathSequence : MonoBehaviour
{
    [Header("Timing (real seconds)")]
    [Tooltip("쓰러지는 데 걸리는 시간")]
    public float fallDuration = 0.75f;
    [Tooltip("슬로모션이 유지되는 시간")]
    public float slowMotionDuration = 0.9f;
    [Range(0.05f, 1f)] public float slowMotionScale = 0.35f;
    [Tooltip("검게 어두워지는 시간")]
    public float fadeToBlackDuration = 0.9f;
    [Tooltip("검은 화면에서 잠깐 머무는 시간")]
    public float holdBlack = 0.3f;

    [Header("Look")]
    [Tooltip("쓰러지는 각도")]
    public float fallAngle = 85f;
    public Color hitFlashColor = new Color(0.75f, 0.05f, 0.03f, 0.55f);
    [Tooltip("물에 빠져 죽을 때 가라앉는 깊이")]
    public float sinkDepth = 1.2f;

    private Image overlay;
    private bool playing;

    // real time per frame, capped so a hitch at the moment of death doesn't skip the whole animation
    private static float Dt() => Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

    public void Play(bool inWater, Action onDone)
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Run(inWater, onDone));
    }

    private IEnumerator Run(bool inWater, Action onDone)
    {
        FreezePlayer();
        CreateOverlay();

        // slow motion starts right at the moment of death
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;

        StartCoroutine(inWater ? Sink() : FallOver());

        // red flash: quick in, slower out
        yield return Fade(new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, 0f), hitFlashColor, 0.08f);
        yield return Fade(hitFlashColor, new Color(hitFlashColor.r * 0.3f, 0f, 0f, 0.25f), 0.45f);

        float remainingSlow = slowMotionDuration - 0.53f;
        if (remainingSlow > 0f) yield return new WaitForSecondsRealtime(remainingSlow);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // darken to black
        yield return Fade(overlay.color, Color.black, fadeToBlackDuration);
        yield return new WaitForSecondsRealtime(holdBlack);

        playing = false;
        onDone?.Invoke();
    }

    // stop input, movement and animation so the body keeps the pose it died in
    private void FreezePlayer()
    {
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;
        var input = GetComponent<StarterAssets.StarterAssetsInputs>();
        if (input != null) { input.move = Vector2.zero; input.look = Vector2.zero; input.jump = false; input.sprint = false; }
#if ENABLE_INPUT_SYSTEM
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = false;
#endif
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        var anim = GetComponent<Animator>();
        if (anim != null) anim.speed = 0f;
    }

    // tip the whole visible body forward around the feet (camera target stays put)
    private IEnumerator FallOver()
    {
        Vector3 pivot = transform.position;
        Vector3 axis = transform.right;
        float done = 0f, t = 0f;
        while (t < fallDuration)
        {
            t += Dt();
            float k = Mathf.Clamp01(t / fallDuration);
            float eased = k * k * k; // starts slow, slams at the end
            float target = eased * fallAngle;
            float step = target - done;
            done = target;
            foreach (Transform child in transform)
                if (child.name != "PlayerCameraRoot" && !child.name.StartsWith("Rain"))
                    child.RotateAround(pivot, axis, step);
            yield return null;
        }
        // small bounce when hitting the ground
        float b = 0f;
        while (b < 0.18f)
        {
            b += Dt();
            float s = Mathf.Sin(b / 0.18f * Mathf.PI) * 4f * Dt() / 0.18f;
            foreach (Transform child in transform)
                if (child.name != "PlayerCameraRoot" && !child.name.StartsWith("Rain"))
                    child.RotateAround(pivot, axis, -s);
            yield return null;
        }
    }

    // in water: the body slowly goes under (camera target stays above the surface)
    private IEnumerator Sink()
    {
        float t = 0f, dur = fallDuration + 0.8f, moved = 0f;
        while (t < dur)
        {
            t += Dt();
            float k = Mathf.Clamp01(t / dur);
            float target = sinkDepth * (k * k);
            float step = target - moved;
            moved = target;
            foreach (Transform child in transform)
                if (child.name != "PlayerCameraRoot" && !child.name.StartsWith("Rain"))
                    child.position += Vector3.down * step;
            yield return null;
        }
    }

    private void CreateOverlay()
    {
        var go = new GameObject("DeathOverlay", typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above the HUD
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

    private void OnDestroy()
    {
        // never leave the game stuck in slow motion
        if (playing) { Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f; }
    }
}
