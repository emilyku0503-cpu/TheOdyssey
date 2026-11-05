
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScreenBlink : MonoBehaviour
{
    [Header("Blink Settings")]
    public float duration = 3f;
    public float blinkInterval = 0.5f;

    private Image overlay;
    private Coroutine blinkRoutine;
    private float endTime;

    void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        SetAlpha(0f);
    }

    public void PlayBlink()
    {
        endTime = Time.time + duration;

        if (blinkRoutine == null)
        {
            blinkRoutine = StartCoroutine(BlinkEffect());
        }
    }

    IEnumerator BlinkEffect()
    {
        float startTime = Time.time;

        while (Time.time < endTime)
        {
            float cycle = ((Time.time - startTime)
                % blinkInterval) / blinkInterval;

            float alpha;

            if (cycle < 0.16f)
                alpha = cycle / 0.16f;
            else if (cycle < 0.46f)
                alpha = 1f;
            else if (cycle < 0.62f)
                alpha = 1f - (cycle - 0.46f) / 0.16f;
            else
                alpha = 0f;

            SetAlpha(Mathf.Clamp01(alpha));

            yield return null;
        }

        SetAlpha(0f);
        blinkRoutine = null;
    }

    void SetAlpha(float alpha)
    {
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
