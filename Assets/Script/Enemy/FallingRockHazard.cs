using System.Collections;
using UnityEngine;

// 떨어지는 돌 (Hazard): 바닥에 경고 원이 커지고 천장에서 부스러기가 흘러내린 뒤 바위가 떨어진다.
// 떨어진 순간 원 안에 있으면 데미지. 플레이어가 가까이 있을 때만 반복.
public class FallingRockHazard : MonoBehaviour
{
    [Header("Rock")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float rockScale = 0.85f;
    [SerializeField] private float dropHeight = 14f;
    [SerializeField] private float gravity = 32f;

    [Header("Damage")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float radius = 1.4f;

    [Header("Timing")]
    [SerializeField] private float warnTime = 1.2f;
    [SerializeField] private Vector2 interval = new Vector2(2.5f, 4.5f);
    [Tooltip("플레이어가 이 거리 안에 있을 때만 떨어짐")]
    [SerializeField] private float activationRange = 22f;

    [Header("Look")]
    [SerializeField] private Material warningMaterial;
    [SerializeField] private Color warningColor = new Color(0.75f, 0.12f, 0.08f, 0.55f);
    [SerializeField] private Material particleMaterial;

    private Transform warn;
    private Material warnMat;
    private ParticleSystem pebbles, dust;
    private Transform player;

    private void Awake()
    {
        // warning disc on the ground
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Warning"; Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = new Vector3(0, 0.04f, 0);
        warn = disc.transform;
        var r = disc.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        if (warningMaterial != null) { warnMat = new Material(warningMaterial); r.sharedMaterial = warnMat; }
        else warnMat = r.material;
        disc.SetActive(false);

        pebbles = MakeParticles("Pebbles", new Vector3(0, dropHeight - 1f, 0), 0.12f, new Color(0.45f, 0.43f, 0.4f, 1f), 1.4f, 0f);
        var ps = pebbles.shape; ps.shapeType = ParticleSystemShapeType.Circle; ps.radius = radius * 0.6f; ps.rotation = new Vector3(90, 0, 0);
        var pm = pebbles.main; pm.gravityModifier = 1.2f;
        dust = MakeParticles("Impact Dust", new Vector3(0, 0.2f, 0), 0.9f, new Color(0.5f, 0.47f, 0.43f, 0.55f), 1.0f, 3.5f);
        var ds = dust.shape; ds.shapeType = ParticleSystemShapeType.Hemisphere; ds.radius = radius * 0.7f; ds.rotation = new Vector3(-90, 0, 0);
    }

    private ParticleSystem MakeParticles(string name, Vector3 local, float size, Color col, float life, float speed)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = local;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main; m.playOnAwake = false; m.loop = false; m.startLifetime = life; m.startSpeed = speed; m.startSize = size; m.startColor = col;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e = ps.emission; e.rateOverTime = 0;
        if (particleMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        return ps;
    }

    private IEnumerator Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        yield return new WaitForSeconds(Random.Range(0f, interval.y));   // de-sync neighbouring rocks
        while (true)
        {
            while (player == null || Vector3.Distance(player.position, transform.position) > activationRange)
                yield return new WaitForSeconds(0.25f);
            yield return Warn();
            yield return Drop();
            yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
        }
    }

    private IEnumerator Warn()
    {
        warn.gameObject.SetActive(true);
        var e = pebbles.emission; e.rateOverTime = 14f; pebbles.Play();
        for (float t = 0; t < warnTime; t += Time.deltaTime)
        {
            float k = t / warnTime;
            float d = Mathf.Lerp(0.3f, radius * 2f, Mathf.SmoothStep(0, 1, k));
            warn.localScale = new Vector3(d, 0.01f, d);
            var c = warningColor; c.a *= 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.Lerp(8f, 26f, k)));
            SetColor(c);
            yield return null;
        }
        e.rateOverTime = 0;
    }

    private IEnumerator Drop()
    {
        var rock = rockPrefab != null ? Instantiate(rockPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foreach (var c in rock.GetComponentsInChildren<Collider>()) Destroy(c);
        rock.transform.localScale = Vector3.one * rockScale;
        rock.transform.rotation = Random.rotation;
        float y = dropHeight, v = 0f;
        var spin = Random.insideUnitSphere * 240f;
        while (y > 0.5f * rockScale)
        {
            v += gravity * Time.deltaTime; y -= v * Time.deltaTime;
            rock.transform.position = transform.position + Vector3.up * Mathf.Max(y, 0.5f * rockScale);
            rock.transform.Rotate(spin * Time.deltaTime, Space.World);
            yield return null;
        }

        // impact
        warn.gameObject.SetActive(false);
        dust.Emit(30);
        foreach (var col in Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!col.CompareTag("Player")) continue;
            var h = col.GetComponentInParent<PlayerHealth>();
            if (h != null) h.TakeDamage(damage);
            break;
        }
        if (player != null && Vector3.Distance(player.position, transform.position) < 9f) CameraShake.Shake(0.12f, 0.25f);

        // the rock breaks apart and sinks away
        var s0 = rock.transform.localScale;
        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            rock.transform.localScale = s0 * (1f - k * 0.85f);
            rock.transform.position += Vector3.down * Time.deltaTime * 0.6f;
            yield return null;
        }
        Destroy(rock);
    }

    private void SetColor(Color c)
    {
        if (warnMat == null) return;
        if (warnMat.HasProperty("_BaseColor")) warnMat.SetColor("_BaseColor", c);
        if (warnMat.HasProperty("_Color")) warnMat.SetColor("_Color", c);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * dropHeight);
    }
}
