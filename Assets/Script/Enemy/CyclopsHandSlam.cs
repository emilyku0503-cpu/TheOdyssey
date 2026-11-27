using System.Collections;
using UnityEngine;

// 키클롭스의 손 (Enemy): 제자리에 고정된 채 정해진 간격으로 들어 올렸다가 바닥을 내리친다.
//  - 대기(공중) → 예고(살짝 더 올라가며 떨림, 바닥 그림자가 짙어짐) → 내리치기 → 잠깐 머무름 → 천천히 올라감
//  - 내리친 순간 손 아래에 있으면 데미지
//  - Pause(초): 양털 아이템 등으로 잠시 공격을 멈추게 할 때 사용
public class CyclopsHandSlam : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("움직이는 손 모델 (이 오브젝트의 자식)")]
    [SerializeField] private Transform hand;
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private Material particleMaterial;

    [Header("Motion")]
    [SerializeField] private float raiseHeight = 5f;
    [SerializeField] private float waitUp = 2.0f;
    [SerializeField] private float telegraph = 0.9f;
    [SerializeField] private float slamTime = 0.14f;
    [SerializeField] private float holdDown = 0.7f;
    [SerializeField] private float riseTime = 1.1f;
    [Tooltip("여러 손의 박자를 엇갈리게")]
    [SerializeField] private float startDelay = 0f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [Tooltip("손바닥이 덮는 영역 (손 기준 로컬)")]
    [SerializeField] private Vector3 hitCenter = new Vector3(0.2f, 0.9f, -1.2f);
    [SerializeField] private Vector3 hitSize = new Vector3(3.4f, 1.8f, 4.8f);

    private float pausedUntil;
    private Transform shadow;
    private Material shadowMat;
    private ParticleSystem dust;
    private Transform player;

    public bool IsPaused => Time.time < pausedUntil;
    public void Pause(float seconds) => pausedUntil = Mathf.Max(pausedUntil, Time.time + seconds);

    private void Awake()
    {
        if (hand == null && transform.childCount > 0) hand = transform.GetChild(0);
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Hand Shadow"; Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = hitCenter + Vector3.down * (hitCenter.y - 0.05f);
        shadow = disc.transform;
        var r = disc.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        shadowMat = shadowMaterial != null ? new Material(shadowMaterial) : r.material; r.sharedMaterial = shadowMat;

        var go = new GameObject("Slam Dust"); go.transform.SetParent(transform, false); go.transform.localPosition = shadow.localPosition + Vector3.up * 0.2f;
        dust = go.AddComponent<ParticleSystem>(); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = dust.main; m.playOnAwake = false; m.loop = false; m.startLifetime = 1.1f; m.startSpeed = 4.5f; m.startSize = 1.0f;
        m.startColor = new Color(0.5f, 0.47f, 0.43f, 0.5f); m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e = dust.emission; e.rateOverTime = 0;
        var s = dust.shape; s.shapeType = ParticleSystemShapeType.Circle; s.radius = 2.2f; s.rotation = new Vector3(90, 0, 0);
        if (particleMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
    }

    private IEnumerator Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform;
        hand.localPosition = Vector3.up * raiseHeight;
        SetShadow(0.15f, 0.7f);
        yield return new WaitForSeconds(startDelay);
        while (true)
        {
            // wait (and stay up while paused)
            for (float t = 0; t < waitUp || IsPaused; t += Time.deltaTime)
            {
                hand.localPosition = Vector3.up * (raiseHeight + Mathf.Sin(Time.time * 1.3f) * 0.15f);
                yield return null;
            }

            // telegraph: lift a little more and tremble, the shadow darkens and tightens
            for (float t = 0; t < telegraph; t += Time.deltaTime)
            {
                float k = t / telegraph;
                var shake = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * 0.06f * k;
                hand.localPosition = Vector3.up * (raiseHeight + 0.8f * Mathf.SmoothStep(0, 1, k)) + shake;
                SetShadow(Mathf.Lerp(0.15f, 0.6f, k), Mathf.Lerp(0.7f, 1f, k));
                yield return null;
            }

            // slam
            float top = hand.localPosition.y;
            for (float t = 0; t < slamTime; t += Time.deltaTime)
            {
                float k = t / slamTime;
                hand.localPosition = Vector3.up * Mathf.Lerp(top, 0f, k * k);
                yield return null;
            }
            hand.localPosition = Vector3.zero;
            Impact();

            yield return new WaitForSeconds(holdDown);

            // rise slowly
            for (float t = 0; t < riseTime; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / riseTime);
                hand.localPosition = Vector3.up * Mathf.Lerp(0f, raiseHeight, k);
                SetShadow(Mathf.Lerp(0.6f, 0.15f, k), Mathf.Lerp(1f, 0.7f, k));
                yield return null;
            }
        }
    }

    private void Impact()
    {
        dust.Emit(40);
        var c = transform.TransformPoint(hitCenter);
        foreach (var col in Physics.OverlapBox(c, hitSize * 0.5f, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!col.CompareTag("Player")) continue;
            var h = col.GetComponentInParent<PlayerHealth>();
            if (h != null) h.TakeDamage(damage);
            break;
        }
        if (player != null && Vector3.Distance(player.position, transform.position) < 14f) CameraShake.Shake(0.25f, 0.35f);
    }

    private void SetShadow(float alpha, float scale)
    {
        shadow.localScale = new Vector3(hitSize.x * scale, 0.01f, hitSize.z * scale);
        var col = new Color(0f, 0f, 0f, alpha);
        if (shadowMat.HasProperty("_BaseColor")) shadowMat.SetColor("_BaseColor", col);
        if (shadowMat.HasProperty("_Color")) shadowMat.SetColor("_Color", col);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(hitCenter), transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, hitSize);
    }
}
