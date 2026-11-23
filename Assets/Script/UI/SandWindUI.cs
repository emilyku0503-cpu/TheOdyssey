using UnityEngine;
using UnityEngine.UI;

// 화면 맨 앞에서 모래바람이 날리는 UI 효과 (Screen Space Overlay 캔버스에서도 보임).
// 작은 모래 알갱이 + 바람결 따라 길게 늘어진 줄기를 하나의 메시로 그린다 (드로우콜 1개).
// 바람은 주기적으로 세졌다 약해지며(돌풍), 아래쪽(모래사장)일수록 진하다.
[RequireComponent(typeof(CanvasRenderer))]
public class SandWindUI : MaskableGraphic
{
    [Header("Amount")]
    [SerializeField] private int grainCount = 260;
    [SerializeField] private int streakCount = 36;

    [Header("Wind")]
    [Tooltip("+ 는 왼쪽→오른쪽")]
    [SerializeField] private float windSpeed = 420f;
    [SerializeField] private float gustPeriod = 6f;
    [SerializeField, Range(0f, 1f)] private float gustStrength = 0.55f;
    [Tooltip("화면 아래에서부터 이 비율 높이까지 주로 날림")]
    [SerializeField, Range(0.1f, 1f)] private float bandHeight = 0.45f;

    [Header("Look")]
    [Tooltip("밝은 모래 (하늘/바다 위에서 잘 보임)")]
    [SerializeField] private Color sandColor = new Color(1f, 0.94f, 0.80f, 1f);
    [Tooltip("진한 모래 (밝은 모래사장 위에서 잘 보임)")]
    [SerializeField] private Color darkSandColor = new Color(0.62f, 0.43f, 0.26f, 1f);
    [SerializeField, Range(0f, 1f)] private float darkRatio = 0.4f;
    [SerializeField, Range(0f, 1f)] private float grainAlpha = 0.75f;
    [SerializeField, Range(0f, 1f)] private float streakAlpha = 0.3f;
    [SerializeField] private Vector2 grainSize = new Vector2(2.5f, 6f);

    private struct P { public Vector2 pos; public float speed, size, phase, wobble, life, age, len; public bool streak, dark; }
    private P[] ps;
    private System.Random rnd = new System.Random(7);
    private float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Init();
    }

    private void Init()
    {
        ps = new P[Mathf.Max(0, grainCount) + Mathf.Max(0, streakCount)];
        for (int i = 0; i < ps.Length; i++) { Spawn(ref ps[i], i >= grainCount); ps[i].age = R(0f, ps[i].life); ps[i].pos.x = R(Left, Right); }
    }

    private float Left => rectTransform.rect.xMin - 60f;
    private float Right => rectTransform.rect.xMax + 60f;

    private void Spawn(ref P p, bool streak)
    {
        var r = rectTransform.rect;
        p.streak = streak;
        p.dark = !streak && rnd.NextDouble() < darkRatio;
        // mostly low over the beach, a few higher and fainter
        float h01 = Mathf.Pow((float)rnd.NextDouble(), streak ? 1.6f : 2.2f) * bandHeight;
        if (!streak && rnd.NextDouble() < 0.12) h01 = R(bandHeight, Mathf.Min(1f, bandHeight + 0.35f));
        p.pos = new Vector2(Left - R(0f, 200f), r.yMin + h01 * r.height);
        p.speed = R(0.7f, 1.35f) * (streak ? 1.25f : 1f);
        p.size = streak ? R(1.2f, 2.4f) : R(grainSize.x, grainSize.y);
        p.len = streak ? R(90f, 260f) : 0f;
        p.phase = R(0f, 100f);
        p.wobble = R(6f, 22f);
        p.life = streak ? R(1.2f, 2.4f) : R(2.5f, 6f);
        p.age = 0f;
    }

    private float Gust(float t, float x)
    {
        // a gust front sweeps across the screen every gustPeriod seconds
        float w = Mathf.Sin((t / Mathf.Max(0.1f, gustPeriod)) * Mathf.PI * 2f - x * 0.0025f) * 0.5f + 0.5f;
        return 1f - gustStrength + gustStrength * w * w;
    }

    private void Update()
    {
        if (ps == null || ps.Length != grainCount + streakCount) Init();
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
        float t = Time.unscaledTime;
        for (int i = 0; i < ps.Length; i++)
        {
            ref P p = ref ps[i];
            float g = Gust(t, p.pos.x);
            p.pos.x += windSpeed * p.speed * (0.45f + 0.9f * g) * dt;
            p.pos.y += Mathf.Sin(t * 2.1f + p.phase) * p.wobble * dt + (p.streak ? 0f : 4f * dt);
            p.age += dt;
            if (p.age > p.life || p.pos.x > Right) Spawn(ref p, i >= grainCount);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (ps == null) return;
        var r = rectTransform.rect;
        float t = Application.isPlaying ? Time.unscaledTime : 0f;
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            float life01 = Mathf.Clamp01(p.age / Mathf.Max(0.01f, p.life));
            float fade = Mathf.Sin(life01 * Mathf.PI);                               // fade in and out
            float height01 = Mathf.InverseLerp(r.yMin, r.yMax, p.pos.y);
            float low = 1f - Mathf.SmoothStep(0f, 1f, height01 / Mathf.Max(0.01f, bandHeight + 0.2f)) * 0.7f;
            float a = (p.streak ? streakAlpha : grainAlpha) * fade * low * (0.5f + 0.5f * Gust(t, p.pos.x));
            if (a <= 0.003f) continue;
            var c = (p.dark ? darkSandColor : sandColor) * color; c.a *= a;

            // direction of travel (mostly sideways, a little wobble)
            Vector2 dir = new Vector2(1f, Mathf.Cos(t * 2.1f + p.phase) * p.wobble / Mathf.Max(1f, windSpeed * p.speed)).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x);
            float len = p.streak ? p.len : p.size * 1.6f;     // grains are slightly stretched by the wind
            float wid = p.size;
            AddQuad(vh, p.pos, dir * (len * 0.5f), side * (wid * 0.5f), c, p.streak);
        }
    }

    private static void AddQuad(VertexHelper vh, Vector2 center, Vector2 halfLen, Vector2 halfWid, Color c, bool taper)
    {
        int i0 = vh.currentVertCount;
        Color tail = c; if (taper) tail.a = 0f;      // streaks fade out toward both ends
        Color head = c; if (taper) head.a = 0f;
        UIVertex v = UIVertex.simpleVert;
        v.color = tail; v.position = center - halfLen - halfWid; vh.AddVert(v);
        v.color = tail; v.position = center - halfLen + halfWid; vh.AddVert(v);
        v.color = c;    v.position = center + halfWid;           vh.AddVert(v);
        v.color = c;    v.position = center - halfWid;           vh.AddVert(v);
        v.color = head; v.position = center + halfLen + halfWid; vh.AddVert(v);
        v.color = head; v.position = center + halfLen - halfWid; vh.AddVert(v);
        vh.AddTriangle(i0, i0 + 1, i0 + 2); vh.AddTriangle(i0, i0 + 2, i0 + 3);
        vh.AddTriangle(i0 + 3, i0 + 2, i0 + 4); vh.AddTriangle(i0 + 3, i0 + 4, i0 + 5);
    }
}
