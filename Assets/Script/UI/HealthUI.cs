using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 체력바 UI.
// healthBar + barStates 를 넣으면 체력 수치에 맞는 그림으로 통째로 갈아끼움
// (barStates[0] = 빈 바, [1] = 1칸, [2] = 2칸, [3] = 꽉 참).
// 비워두면 예전처럼 hearts 를 채움/빈 그림으로 바꿈.
public class HealthUI : MonoBehaviour
{
    [Header("Health Bar (state images)")]
    [SerializeField] private Image healthBar;
    [Tooltip("체력 0, 1, 2, 3 일 때의 그림 순서대로")]
    [SerializeField] private Sprite[] barStates;

    [Header("Hit Effect")]
    [Tooltip("체력이 줄 때 바가 살짝 흔들리는 효과")]
    [SerializeField] private bool shakeOnDamage = true;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 10f;

    [Header("Legacy Hearts (optional)")]
    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite filledHeart;
    [SerializeField] private Sprite emptyHeart;

    private int lastHealth = -1;
    private Vector2 barBasePos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (healthBar != null) barBasePos = healthBar.rectTransform.anchoredPosition;
    }

    public void UpdateHealth(int currentHealth)
    {
        if (healthBar != null && barStates != null && barStates.Length > 0)
        {
            int index = Mathf.Clamp(currentHealth, 0, barStates.Length - 1);
            healthBar.sprite = barStates[index];

            if (shakeOnDamage && lastHealth >= 0 && currentHealth < lastHealth && isActiveAndEnabled)
            {
                if (shakeRoutine != null) StopCoroutine(shakeRoutine);
                shakeRoutine = StartCoroutine(Shake());
            }
        }

        if (hearts != null)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].sprite = i < currentHealth ? filledHeart : emptyHeart;
            }
        }

        lastHealth = currentHealth;
    }

    private IEnumerator Shake()
    {
        var rt = healthBar.rectTransform;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - t / shakeDuration;
            rt.anchoredPosition = barBasePos + Random.insideUnitCircle * shakeStrength * falloff;
            yield return null;
        }
        rt.anchoredPosition = barBasePos;
        shakeRoutine = null;
    }
}
