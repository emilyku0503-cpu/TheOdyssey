using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using StarterAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 부스터 상태에서 점프할 때마다(첫 점프/더블 점프 모두), 발밑(월드 위치)에 황금색 바람 고리가 퍼지는 이펙트.
// 프리팹/텍스처 없이 코드로 메시와 파티클을 만들어서 플레이어에 붙이기만 하면 동작한다.
//  - 순간 플래시 원판  : "지금 발동했다"는 신호
//  - 메인 고리 + 잔향 고리 : 바깥으로 퍼지며 얇아지고 사라짐 (ease-out)
//  - 바람 줄기 파티클   : 바깥/아래로 뿜어져 나가는 공기
//  - 금빛 입자         : 부스터의 여운
[DisallowMultipleComponent]
public class DoubleJumpWindRing : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("켜면 부스터(Boost)가 활성화된 동안에만 고리가 나온다")]
    [SerializeField] private bool onlyWhenBoosted = true;
    [SerializeField] private float footOffset = 0.05f;

    [Header("Look")]
    [Tooltip("HDR 색. Intensity를 올리면 Bloom에서 빛난다")]
    [ColorUsage(false, true)]
    [SerializeField] private Color goldColor = new Color(3.0f, 2.0f, 0.45f, 1f);
    [Tooltip("비워두면 URP Particles/Unlit 가산 머티리얼을 자동 생성")]
    [SerializeField] private Material effectMaterial;

    [Header("Main Ring")]
    [SerializeField] private float ringDuration = 0.4f;
    [SerializeField] private float ringStartRadius = 0.25f;
    [SerializeField] private float ringEndRadius = 2.2f;
    [SerializeField] private float ringStartWidth = 0.35f;
    [SerializeField] private float ringEndWidth = 0.05f;

    [Header("Echo Ring")]
    [SerializeField] private float echoDelay = 0.07f;
    [SerializeField, Range(0f, 1f)] private float echoScale = 0.7f;

    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float flashRadius = 0.8f;

    [Header("Wind Streaks")]
    [SerializeField] private int streakCount = 18;
    [SerializeField] private float streakSpeed = 7f;

    [Header("Body Sparkles (캐릭터 전체를 감싸며 떠오르는 입자)")]
    [SerializeField] private int sparkleCount = 40;
    [Tooltip("입자 전용 HDR 색. 고리보다 밝고 하얗게 해서 눈에 띄게")]
    [ColorUsage(false, true)]
    [SerializeField] private Color sparkleColor = new Color(5.0f, 4.0f, 1.6f, 1f);
    [SerializeField] private Vector2 sparkleSize = new Vector2(0.04f, 0.08f);
    [Tooltip("캐릭터 몸에서 입자가 떨어진 거리 (CharacterController 반지름에 더해짐)")]
    [SerializeField] private float sparkleBodyPadding = 0.12f;
    [SerializeField] private Vector2 sparkleRiseSpeed = new Vector2(0.4f, 1.1f);
    [Tooltip("몸 주위를 도는 속도 (소용돌이 느낌)")]
    [SerializeField] private float sparkleSwirlSpeed = 0.8f;
    [SerializeField] private Vector2 sparkleLifetime = new Vector2(0.6f, 1.0f);

    [Header("Feedback")]
    [SerializeField] private AudioClip whooshClip;
    [SerializeField, Range(0f, 1f)] private float whooshVolume = 0.7f;
    [SerializeField] private float shakeStrength = 0.04f;
    [SerializeField] private float shakeDuration = 0.12f;

    private const int Segments = 64;

    private enum FxShape { Ring, Disc }

    private class Fx
    {
        public GameObject go;
        public MeshRenderer renderer;
        public Mesh mesh;
        public Vector3[] verts;
        public Color[] colors;
        public FxShape shape;
        public float age, delay, duration;
        public float r0, r1, w0, w1, intensity;
    }

    private readonly List<Fx> active = new List<Fx>();

    private ThirdPersonController controller;
    private CharacterController body;
    private Boost boost;
    private AudioSource audioSource;
    private Material ringMat;
    private Material particleMat;
    private Material sparkleMat;
    private Texture2D softDot;
    private ParticleSystem streaks;
    private ParticleSystem sparkles;

#if UNITY_EDITOR
    // 컴포넌트를 에디터에서 추가하면 프로젝트의 Woosh 효과음을 자동으로 넣어줌
    private void Reset()
    {
        whooshClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/SFX Placeholder/Woosh.mp3");
    }
#endif

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        boost = GetComponent<Boost>();
        body = GetComponent<CharacterController>();

        ringMat = effectMaterial != null ? effectMaterial : CreateAdditiveMaterial(null, goldColor);
        softDot = CreateSoftDot(32);
        particleMat = CreateAdditiveMaterial(softDot, goldColor);
        sparkleMat = CreateAdditiveMaterial(softDot, sparkleColor);

        streaks = CreateParticles("WindRing_Streaks", particleMat, true, 3.5f, 0.2f);
        sparkles = CreateParticles("WindRing_Sparkles", sparkleMat, false, 0.6f, 0.3f);
        // 몸을 감싸는 입자는 캐릭터와 함께 움직이도록 로컬 공간에서 시뮬레이션
        var sparkleMain = sparkles.main;
        sparkleMain.simulationSpace = ParticleSystemSimulationSpace.Local;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        if (controller != null) audioSource.outputAudioMixerGroup = controller.masterMixerGroup;
    }

    private void OnEnable()
    {
        if (controller != null) controller.Jumped += HandleJumped;
    }

    private void OnDisable()
    {
        if (controller != null) controller.Jumped -= HandleJumped;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < active.Count; i++) Kill(active[i]);
        active.Clear();
        if (effectMaterial == null && ringMat != null) Destroy(ringMat);
        if (particleMat != null) Destroy(particleMat);
        if (sparkleMat != null) Destroy(sparkleMat);
        if (softDot != null) Destroy(softDot);
    }

    private void HandleJumped(int jumpCount)
    {
        // 부스터 상태라면 첫 점프든 더블 점프든 매번 발동
        if (onlyWhenBoosted && (boost == null || !boost.IsBoostActive)) return;
        Play(transform.position + Vector3.up * footOffset);
    }

    // 다른 스크립트에서 직접 호출해도 됨
    public void Play(Vector3 pos)
    {
        // 플래시 → 메인 고리 → 잔향 고리 순서
        Spawn(FxShape.Disc, pos, 0f, flashDuration, flashRadius * 0.3f, flashRadius, 0f, 0f, 1.2f);
        Spawn(FxShape.Ring, pos, 0f, ringDuration, ringStartRadius, ringEndRadius, ringStartWidth, ringEndWidth, 1f);
        Spawn(FxShape.Ring, pos + Vector3.down * 0.03f, echoDelay, ringDuration * 0.9f,
              ringStartRadius * echoScale, ringEndRadius * echoScale, ringStartWidth * 0.6f, ringEndWidth, 0.55f);

        EmitStreaks(pos);
        EmitSparkles();

        if (whooshClip != null) audioSource.PlayOneShot(whooshClip, whooshVolume);
        if (shakeStrength > 0f) CameraShake.Shake(shakeStrength, shakeDuration);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Fx fx = active[i];
            fx.age += dt;

            float local = fx.age - fx.delay;
            if (local < 0f) { fx.renderer.enabled = false; continue; }
            fx.renderer.enabled = true;

            float p = Mathf.Clamp01(local / fx.duration);
            if (p >= 1f) { Kill(fx); active.RemoveAt(i); continue; }

            float e = 1f - (1f - p) * (1f - p) * (1f - p);       // ease-out cubic
            float fade = Mathf.Pow(1f - p, 1.5f) * fx.intensity;
            float r = Mathf.Lerp(fx.r0, fx.r1, e);

            if (fx.shape == FxShape.Ring)
                BuildRing(fx, r, Mathf.Lerp(fx.w0, fx.w1, e), fade);
            else
                BuildDisc(fx, r, fade);
        }
    }

    // ---------- 고리 / 원판 ----------

    private void Spawn(FxShape shape, Vector3 pos, float delay, float duration,
                       float r0, float r1, float w0, float w1, float intensity)
    {
        var go = new GameObject(shape == FxShape.Ring ? "WindRing" : "WindFlash");
        go.transform.position = pos;   // 플레이어를 따라가지 않고 점프한 자리에 남는다

        var mesh = new Mesh { name = go.name };
        mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = ringMat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.enabled = false;

        var fx = new Fx
        {
            go = go, renderer = mr, mesh = mesh, shape = shape,
            delay = delay, duration = Mathf.Max(0.01f, duration),
            r0 = r0, r1 = r1, w0 = w0, w1 = w1, intensity = intensity
        };

        int[] tris;
        if (shape == FxShape.Ring)
        {
            // 링마다 3개 버텍스: 안쪽(투명) - 앞 가장자리(밝음) - 바깥(투명)
            fx.verts = new Vector3[Segments * 3];
            fx.colors = new Color[Segments * 3];
            tris = new int[Segments * 12];
            int t = 0;
            for (int s = 0; s < Segments; s++)
            {
                int a = s * 3, b = ((s + 1) % Segments) * 3;
                for (int k = 0; k < 2; k++)
                {
                    tris[t++] = a + k; tris[t++] = b + k;     tris[t++] = a + k + 1;
                    tris[t++] = b + k; tris[t++] = b + k + 1; tris[t++] = a + k + 1;
                }
            }
        }
        else
        {
            fx.verts = new Vector3[Segments + 1];
            fx.colors = new Color[Segments + 1];
            tris = new int[Segments * 3];
            for (int s = 0; s < Segments; s++)
            {
                tris[s * 3] = 0;
                tris[s * 3 + 1] = 1 + (s + 1) % Segments;
                tris[s * 3 + 2] = 1 + s;
            }
        }

        if (shape == FxShape.Ring) BuildRing(fx, r0, w0, 0f); else BuildDisc(fx, r0, 0f);
        mesh.triangles = tris;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(r1 * 2.5f, 0.5f, r1 * 2.5f));

        active.Add(fx);
    }

    private static void BuildRing(Fx fx, float r, float w, float alpha)
    {
        float inner = Mathf.Max(0f, r - w);
        float outer = r + w * 0.35f;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color lit = new Color(1f, 1f, 1f, alpha);

        for (int s = 0; s < Segments; s++)
        {
            float ang = s * Mathf.PI * 2f / Segments;
            float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
            int i = s * 3;
            fx.verts[i]     = new Vector3(c * inner, 0f, sn * inner);
            fx.verts[i + 1] = new Vector3(c * r,     0f, sn * r);
            fx.verts[i + 2] = new Vector3(c * outer, 0f, sn * outer);
            fx.colors[i] = clear; fx.colors[i + 1] = lit; fx.colors[i + 2] = clear;
        }
        fx.mesh.vertices = fx.verts;
        fx.mesh.colors = fx.colors;
    }

    private static void BuildDisc(Fx fx, float r, float alpha)
    {
        fx.verts[0] = Vector3.zero;
        fx.colors[0] = new Color(1f, 1f, 1f, alpha);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int s = 0; s < Segments; s++)
        {
            float ang = s * Mathf.PI * 2f / Segments;
            fx.verts[s + 1] = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            fx.colors[s + 1] = clear;
        }
        fx.mesh.vertices = fx.verts;
        fx.mesh.colors = fx.colors;
    }

    private static void Kill(Fx fx)
    {
        if (fx.mesh != null) Destroy(fx.mesh);
        if (fx.go != null) Destroy(fx.go);
    }

    // ---------- 파티클 ----------

    private void EmitStreaks(Vector3 pos)
    {
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < streakCount; i++)
        {
            float ang = (i + Random.Range(-0.3f, 0.3f)) / streakCount * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            ep.position = pos + dir * 0.2f;
            ep.velocity = dir * streakSpeed * Random.Range(0.7f, 1.2f) + Vector3.down * Random.Range(0.3f, 1.2f);
            ep.startLifetime = Random.Range(0.25f, 0.4f);
            ep.startSize = Random.Range(0.05f, 0.09f);
            ep.startColor = Color.white;
            streaks.Emit(ep, 1);
        }
    }

    // 캐릭터 몸 높이 전체에 원기둥 모양으로 뿌려서 몸을 감싸게 함 (로컬 좌표)
    private void EmitSparkles()
    {
        float radius = (body != null ? body.radius : 0.3f) + sparkleBodyPadding;
        float height = body != null ? body.height : 1.8f;
        Vector3 center = body != null ? body.center : new Vector3(0f, 0.9f, 0f);
        float bottom = center.y - height * 0.5f;

        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < sparkleCount; i++)
        {
            float ang = (i + Random.Range(-0.4f, 0.4f)) / sparkleCount * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 tangent = new Vector3(-dir.z, 0f, dir.x);
            float r = radius * Random.Range(0.85f, 1.15f);
            float y = bottom + height * Random.Range(0.05f, 1.0f);

            ep.position = new Vector3(center.x, 0f, center.z) + dir * r + Vector3.up * y;
            ep.velocity = tangent * sparkleSwirlSpeed
                        + Vector3.up * Random.Range(sparkleRiseSpeed.x, sparkleRiseSpeed.y);
            ep.startLifetime = Random.Range(sparkleLifetime.x, sparkleLifetime.y);
            ep.startSize = Random.Range(sparkleSize.x, sparkleSize.y);
            ep.startColor = Color.white;
            sparkles.Emit(ep, 1);
        }
    }

    private ParticleSystem CreateParticles(string name, Material mat, bool stretch, float drag, float endSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = drag;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (stretch)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 1.5f;
        }
        else
        {
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }

        ps.Play();
        return ps;
    }

    // ---------- 머티리얼 / 텍스처 ----------

    private Material CreateAdditiveMaterial(Texture tex, Color tint)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        var m = new Material(sh) { name = "WindRing_Additive (runtime)" };
        SetF(m, "_Surface", 1f);   // Transparent
        SetF(m, "_Blend", 2f);     // Additive
        SetF(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetF(m, "_DstBlend", (float)BlendMode.One);
        SetF(m, "_SrcBlendAlpha", (float)BlendMode.One);
        SetF(m, "_DstBlendAlpha", (float)BlendMode.One);
        SetF(m, "_ZWrite", 0f);
        SetF(m, "_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        return m;
    }

    private static void SetF(Material m, string prop, float v)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, v);
    }

    private static Texture2D CreateSoftDot(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "WindRing_SoftDot"
        };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
            float a = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return tex;
    }
}
