using UnityEngine;
using UnityEngine.UI;

// 연기처럼 흐르는 모래 먼지 (Screen Space Overlay 캔버스용 UI 효과).
// 부드러운 먼지 덩어리들이 바람을 타고 옆으로 흘러가며 천천히 커지고 흩어진다.
// 덩어리 안의 무늬(노이즈)도 계속 흘러서 연기처럼 일렁인다. 드로우콜 1개.
[RequireComponent(typeof(CanvasRenderer))]
public class SandSmokeUI : MaskableGraphic
{
    [Header("Amount")]
    [SerializeField] private int puffCount = 16;

    [Header("Wind")]
    [Tooltip("+ 는 왼쪽→오른쪽")]
    [SerializeField] private float windSpeed = 150f;
    [SerializeField] private float gustPeriod = 7f;
    [SerializeField, Range(0f, 1f)] private float gustStrength = 0.5f;
    [Tooltip("화면 아래에서부터 이 비율 높이까지 먼지가 흐름")]
    [SerializeField, Range(0.1f, 1f)] private float bandHeight = 0.42f;

    [Header("Look")]
    [SerializeField] private Color dustColor = new Color(0.93f, 0.80f, 0.60f, 1f);
    [SerializeField, Range(0f, 1f)] private float opacity = 0.32f;
    [SerializeField] private Vector2 puffWidth = new Vector2(420f, 900f);
    [SerializeField] private Vector2 puffHeight = new Vector2(90f, 210f);
    [Tooltip("먼지 무늬가 흐르는 속도")]
    [SerializeField] private float swirlSpeed = 0.06f;

    private struct Puff { public Vector2 pos; public float w, h, speed, life, age, rise, uvx, uvy, seed; }
    private Puff[] puffs;
    private Texture2D tex;
    private System.Random rnd = new System.Random(11);
    private float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    public override Texture mainTexture => tex != null ? tex : s_WhiteTexture;

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        if (tex == null) tex = MakeNoise(256, 128);
        Init();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (tex != null) { if (Application.isPlaying) Destroy(tex); else DestroyImmediate(tex); }
    }

    private float Left => rectTransform.rect.xMin;
    private float Right => rectTransform.rect.xMax;

    private void Init()
    {
        puffs = new Puff[Mathf.Max(0, puffCount)];
        for (int i = 0; i < puffs.Length; i++) { Spawn(ref puffs[i]); puffs[i].age = R(0f, puffs[i].life); puffs[i].pos.x = R(Left - 300f, Right); }
    }

    private void Spawn(ref Puff p)
    {
        var r = rectTransform.rect;
        p.w = R(puffWidth.x, puffWidth.y);
        p.h = R(puffHeight.x, puffHeight.y);
        float h01 = Mathf.Pow((float)rnd.NextDouble(), 1.6f) * bandHeight;     // mostly hugging the beach
        p.pos = new Vector2(Left - p.w * 0.5f - R(0f, 250f), r.yMin + h01 * r.height);
        p.speed = R(0.7f, 1.3f);
        p.life = R(6f, 11f);
        p.age = 0f;
        p.rise = R(2f, 10f);
        p.uvx = R(0f, 1f); p.uvy = R(0f, 1f); p.seed = R(0f, 100f);
    }

    private float Gust(float t, float x)
    {
        float w = Mathf.Sin((t / Mathf.Max(0.1f, gustPeriod)) * Mathf.PI * 2f - x * 0.002f) * 0.5f + 0.5f;
        return 1f - gustStrength + gustStrength * w * w;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (puffs == null || puffs.Length != puffCount) Init();
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
        float t = Time.unscaledTime;
        for (int i = 0; i < puffs.Length; i++)
        {
            ref Puff p = ref puffs[i];
            float g = Gust(t, p.pos.x);
            p.pos.x += windSpeed * p.speed * (0.5f + 0.8f * g) * dt;
            p.pos.y += (p.rise + Mathf.Sin(t * 0.7f + p.seed) * 8f) * dt;
            p.uvx -= swirlSpeed * (0.6f + g) * dt;           // noise flows through the puff = smoke-like churning
            p.uvy += swirlSpeed * 0.35f * Mathf.Sin(t * 0.4f + p.seed) * dt;
            p.age += dt;
            if (p.age > p.life || p.pos.x - p.w > Right) Spawn(ref p);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (puffs == null) return;
        var r = rectTransform.rect;
        float t = Application.isPlaying ? Time.unscaledTime : 0f;
        const int NX = 6, NY = 4;   // small vertex grid per puff: soft elliptical edge via vertex alpha
        for (int i = 0; i < puffs.Length; i++)
        {
            var p = puffs[i];
            float life01 = Mathf.Clamp01(p.age / Mathf.Max(0.01f, p.life));
            float fade = Mathf.Sin(life01 * Mathf.PI);
            float grow = Mathf.Lerp(0.75f, 1.35f, life01);                          // spreads out as it travels
            float height01 = Mathf.InverseLerp(r.yMin, r.yMax, p.pos.y);
            float low = 1f - Mathf.Clamp01(height01 / Mathf.Max(0.05f, bandHeight + 0.15f)) * 0.75f;
            float a = opacity * fade * low * (0.55f + 0.45f * Gust(t, p.pos.x));
            if (a <= 0.003f) continue;
            float w = p.w * grow, h = p.h * grow;
            int start = vh.currentVertCount;
            for (int yi = 0; yi < NY; yi++)
            for (int xi = 0; xi < NX; xi++)
            {
                float u = xi / (float)(NX - 1), v = yi / (float)(NY - 1);
                float ex = (u - 0.5f) * 2f, ey = (v - 0.5f) * 2f;
                float shape = Mathf.Clamp01(1f - (ex * ex + ey * ey));                 // ellipse falloff
                shape *= shape;
                var c = dustColor * color; c.a *= a * shape;
                var vert = UIVertex.simpleVert;
                vert.position = new Vector3(p.pos.x + (u - 0.5f) * w, p.pos.y + (v - 0.5f) * h);
                vert.color = c;
                vert.uv0 = new Vector2(p.uvx + u * (w / 700f), p.uvy + v * (h / 350f));
                vh.AddVert(vert);
            }
            for (int yi = 0; yi < NY - 1; yi++)
            for (int xi = 0; xi < NX - 1; xi++)
            {
                int i0 = start + yi * NX + xi;
                vh.AddTriangle(i0, i0 + NX, i0 + NX + 1);
                vh.AddTriangle(i0, i0 + NX + 1, i0 + 1);
            }
        }
    }

    // tileable wispy noise (white RGB, noise in alpha)
    private static Texture2D MakeNoise(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "SandSmokeNoise", hideFlags = HideFlags.DontSave };
        var rnd = new System.Random(3);
        float[][,] grids = new float[4][,];
        int[] cells = { 4, 8, 16, 32 };
        for (int o = 0; o < 4; o++)
        {
            int cx = cells[o], cy = Mathf.Max(2, cells[o] / 2);
            grids[o] = new float[cx, cy];
            for (int x = 0; x < cx; x++) for (int y = 0; y < cy; y++) grids[o][x, y] = (float)rnd.NextDouble();
        }
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float n = 0f, amp = 0.55f, sum = 0f;
            for (int o = 0; o < 4; o++)
            {
                var g = grids[o]; int cx = g.GetLength(0), cy = g.GetLength(1);
                float fx = x / (float)w * cx, fy = y / (float)h * cy;
                int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                float tx = fx - x0, ty = fy - y0;
                tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                float a = g[x0 % cx, y0 % cy], b = g[(x0 + 1) % cx, y0 % cy], c = g[x0 % cx, (y0 + 1) % cy], d = g[(x0 + 1) % cx, (y0 + 1) % cy];
                n += Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * amp;
                sum += amp; amp *= 0.5f;
            }
            n /= sum;
            n = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((n - 0.32f) / 0.5f));     // wisps with gaps
            px[y * w + x] = new Color32(255, 255, 255, (byte)(n * 255));
        }
        t.SetPixels32(px); t.Apply(false, false);
        return t;
    }
}
