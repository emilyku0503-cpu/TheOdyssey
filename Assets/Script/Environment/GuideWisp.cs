using UnityEngine;

// 길 안내 빛 구슬: 위아래로 천천히 떠다니며 숨 쉬듯 밝아졌다 어두워짐.
public class GuideWisp : MonoBehaviour
{
    public float bobHeight = 0.15f;
    public float bobSpeed = 1.2f;
    public float pulseAmount = 0.25f;
    public float pulseSpeed = 1.8f;

    private Vector3 basePos;
    private Vector3 baseScale;
    private Light glow;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        basePos = transform.localPosition;
        baseScale = transform.localScale;
        glow = GetComponentInChildren<Light>();
        if (glow != null) baseIntensity = glow.intensity;
        seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float t = Time.time + seed;
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * bobSpeed) * bobHeight;
        float pulse = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * (0.9f + 0.1f * pulse);
        if (glow != null) glow.intensity = baseIntensity * pulse;
    }
}
