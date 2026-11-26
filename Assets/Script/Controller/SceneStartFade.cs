using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneStartFade : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 2f;

    private void Awake()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = false;

        SetAlpha(1f);
    }

    private IEnumerator Start()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}