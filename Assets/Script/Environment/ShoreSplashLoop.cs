using UnityEngine;

// 해변 물보라: 몇 초마다(랜덤 간격) 파티클을 다시 터뜨려 파도가 철썩이는 느낌을 냄.
[RequireComponent(typeof(ParticleSystem))]
public class ShoreSplashLoop : MonoBehaviour
{
    [Tooltip("다음 물보라까지 걸리는 시간 범위(초)")]
    public Vector2 interval = new Vector2(2.5f, 4.5f);

    [Tooltip("카메라에서 이보다 멀면 터뜨리지 않음 (성능)")]
    public float maxDistance = 60f;

    private ParticleSystem ps;
    private float timer;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        timer = Random.Range(0f, interval.y); // 물보라마다 시작 박자를 다르게
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(interval.x, interval.y);

        var cam = Camera.main;
        if (cam != null && (cam.transform.position - transform.position).sqrMagnitude > maxDistance * maxDistance)
            return;

        ps.Play(true); // 자식(거품)까지 같이
    }
}
