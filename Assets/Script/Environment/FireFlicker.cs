using UnityEngine;

// 불꽃 조명: 밝기·색·위치를 불규칙하게 흔들어 주변을 일렁이게 비춤.
[RequireComponent(typeof(Light))]
public class FireFlicker : MonoBehaviour
{
    [Tooltip("평균 밝기")]
    public float baseIntensity = 1.6f;
    [Tooltip("밝기가 흔들리는 폭")]
    public float flickerAmount = 0.7f;
    [Tooltip("흔들리는 속도")]
    public float flickerSpeed = 7f;
    [Tooltip("빛의 위치가 흔들리는 폭 (그림자·빛이 춤추는 느낌)")]
    public float wobble = 0.08f;
    public Color colorA = new Color(1f, 0.55f, 0.15f);
    public Color colorB = new Color(1f, 0.75f, 0.35f);

    private Light fireLight;
    private Vector3 basePos;
    private float seed;

    private void Awake()
    {
        fireLight = GetComponent<Light>();
        basePos = transform.localPosition;
        seed = Random.Range(0f, 1000f); // 불꽃마다 다른 리듬
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed + seed;
        // 느린 일렁임 + 빠른 떨림
        float n = Mathf.PerlinNoise(t * 0.35f, seed) * 0.65f + Mathf.PerlinNoise(t * 1.7f, seed + 10f) * 0.35f;
        fireLight.intensity = Mathf.Max(0f, baseIntensity + (n - 0.5f) * 2f * flickerAmount);
        fireLight.color = Color.Lerp(colorA, colorB, Mathf.PerlinNoise(t * 0.5f, seed + 20f));
        transform.localPosition = basePos + new Vector3(
            (Mathf.PerlinNoise(t * 0.6f, seed + 30f) - 0.5f),
            (Mathf.PerlinNoise(t * 0.6f, seed + 40f) - 0.5f) * 0.6f,
            (Mathf.PerlinNoise(t * 0.6f, seed + 50f) - 0.5f)) * 2f * wobble;
    }
}
