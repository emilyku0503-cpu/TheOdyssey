
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 번개: 화면을 흰 막으로 덮는 대신
//  1) 하늘에서 푸른빛 조명이 "번-쩍-번쩍" 불규칙하게 깜빡이며 세상을 실제로 밝히고
//  2) 멀리 하늘에 지그재그 번개 줄기가 잠깐 보이고
//  3) 화면은 아주 옅게만(UI 뒤에서) 번쩍이고
//  4) 거리만큼 늦게 천둥이 울림 (멀수록 늦고 작게)
public class LightningFlash : MonoBehaviour
{
    [Header("UI (optional, subtle)")]
    [SerializeField] private Image flashImage;

    [Header("Thunder Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip thunderSound;
    [Tooltip("여러 개 넣으면 번개마다 랜덤으로 골라 재생")]
    [SerializeField] private AudioClip[] thunderSounds;

    [Header("Lightning Settings")]
    [Tooltip("번개 사이 최소~최대 간격(초)")]
    [SerializeField] private Vector2 intervalRange = new Vector2(4f, 8f);
    [Tooltip("마지막 번쩍임이 사그라드는 시간")]
    [SerializeField] private float flashDuration = 0.45f;

    [Tooltip("화면 덮개의 최대 밝기 (0.15~0.2 정도가 자연스러움)")]
    [SerializeField, Range(0f, 1f)]
    private float flashIntensity = 0.18f;

    [Header("World Light")]
    [Tooltip("번개 빛을 내는 조명 (없으면 자동 생성)")]
    [SerializeField] private Light lightningLight;
    [SerializeField] private Color lightColor = new Color(0.78f, 0.86f, 1f);
    [SerializeField] private float lightIntensity = 2.0f;
    [Tooltip("번개 순간 주변광(환경광)도 같이 밝아지는 정도")]
    [SerializeField] private float ambientBoost = 0.9f;

    [Header("Bolt in the sky")]
    [SerializeField] private bool showBolt = true;
    [SerializeField] private Material boltMaterial;
    [SerializeField] private Vector2 boltDistance = new Vector2(55f, 110f);
    [SerializeField] private float boltHeight = 70f;

    [Header("Thunder Delay")]
    [Tooltip("소리 속도 대신 쓰는 값: 거리(m) / 이 값 = 천둥이 늦게 들리는 시간(초)")]
    [SerializeField] private float thunderMetersPerSecond = 45f;

    private float baseAmbientIntensity;
    private LineRenderer bolt;
    private LineRenderer branch;

    private void Awake()
    {
        if (flashImage != null)
        {
            flashImage.raycastTarget = false;
            SetFlashAlpha(0f);
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        if (lightningLight == null)
        {
            var go = new GameObject("LightningLight");
            go.transform.SetParent(transform, false);
            lightningLight = go.AddComponent<Light>();
            lightningLight.type = LightType.Directional;
            lightningLight.shadows = LightShadows.Soft;
        }
        lightningLight.color = lightColor;
        lightningLight.intensity = 0f;
        lightningLight.enabled = false;
        baseAmbientIntensity = RenderSettings.ambientIntensity;

        if (showBolt)
        {
            bolt = MakeLine("LightningBolt", 1.1f);
            branch = MakeLine("LightningBranch", 0.5f);
        }
    }

    private LineRenderer MakeLine(string name, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthCurve = new AnimationCurve(new Keyframe(0, width), new Keyframe(1, width * 0.25f));
        lr.numCapVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        if (boltMaterial != null) lr.sharedMaterial = boltMaterial;
        lr.enabled = false;
        return lr;
    }

    private IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            yield return StartCoroutine(PlayLightning());
        }
    }

    private IEnumerator PlayLightning()
    {
        var cam = Camera.main;
        Vector3 center = cam != null ? cam.transform.position : transform.position;

        // where in the sky it strikes (random direction, in front-ish of the camera more often)
        Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        float yaw = Random.Range(-70f, 70f);
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * fwd;
        float dist = Random.Range(boltDistance.x, boltDistance.y);
        Vector3 ground = center + dir * dist;
        ground.y = 0f;

        // light comes from the strike direction, steeply from above
        lightningLight.transform.rotation = Quaternion.LookRotation((ground + Vector3.down * boltHeight * 0.6f) - (ground + Vector3.up * boltHeight) + (center - ground) * 0.4f);
        float strength = Random.Range(0.6f, 1f);

        if (showBolt) BuildBolt(ground);

        // irregular flicker: a few quick pulses, then a longer fade
        int pulses = Random.Range(2, 4);
        for (int i = 0; i < pulses; i++)
        {
            float peak = strength * (i == pulses - 1 ? 1f : Random.Range(0.45f, 0.9f));
            SetFlash(peak, i == 0 || Random.value < 0.7f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.07f));
            SetFlash(peak * Random.Range(0.05f, 0.25f), Random.value < 0.4f);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.11f));
        }

        // thunder arrives later the farther the strike is
        StartCoroutine(Thunder(dist, strength));

        float t = 0f;
        float duration = Mathf.Max(0.05f, flashDuration);
        float start = strength;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            SetFlash(start * k * k, t < duration * 0.35f);
            yield return null;
        }
        SetFlash(0f, false);
    }

    private void SetFlash(float amount, bool boltVisible)
    {
        amount = Mathf.Clamp01(amount);
        lightningLight.enabled = amount > 0.001f;
        lightningLight.intensity = amount * lightIntensity;
        RenderSettings.ambientIntensity = baseAmbientIntensity * (1f + amount * ambientBoost);
        SetFlashAlpha(amount * flashIntensity);
        if (bolt != null) { bolt.enabled = boltVisible && amount > 0.05f; branch.enabled = bolt.enabled && branch.positionCount > 1; }
    }

    private void BuildBolt(Vector3 ground)
    {
        Vector3 top = ground + new Vector3(Random.Range(-12f, 12f), boltHeight, Random.Range(-12f, 12f));
        int segs = 14;
        var pts = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float f = i / (float)segs;
            Vector3 p = Vector3.Lerp(top, ground, f);
            if (i > 0 && i < segs) p += new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(-1f, 1f), Random.Range(-3.5f, 3.5f));
            pts[i] = p;
        }
        bolt.positionCount = pts.Length;
        bolt.SetPositions(pts);

        // one side branch from somewhere in the upper half
        int from = Random.Range(2, segs / 2);
        int bsegs = 5;
        var bp = new Vector3[bsegs + 1];
        Vector3 bdir = new Vector3(Random.Range(-1f, 1f), -1.2f, Random.Range(-1f, 1f)).normalized;
        bp[0] = pts[from];
        for (int i = 1; i <= bsegs; i++)
            bp[i] = bp[i - 1] + bdir * Random.Range(3f, 6f) + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        branch.positionCount = bp.Length;
        branch.SetPositions(bp);
    }

    private IEnumerator Thunder(float distance, float strength)
    {
        AudioClip clip = thunderSound;
        if (thunderSounds != null && thunderSounds.Length > 0)
        {
            // closer strikes use the first clip (sharper), far ones the others
            int idx = distance < (boltDistance.x + boltDistance.y) * 0.5f ? 0 : Random.Range(0, thunderSounds.Length);
            clip = thunderSounds[Mathf.Clamp(idx, 0, thunderSounds.Length - 1)];
        }
        if (audioSource == null || clip == null) yield break;

        yield return new WaitForSeconds(distance / Mathf.Max(1f, thunderMetersPerSecond));
        float near = Mathf.InverseLerp(boltDistance.y, boltDistance.x, distance);
        audioSource.pitch = Random.Range(0.9f, 1.05f);
        audioSource.PlayOneShot(clip, Mathf.Lerp(0.45f, 1f, near) * Mathf.Lerp(0.8f, 1f, strength));
    }

    private void SetFlashAlpha(float alpha)
    {
        if (flashImage == null)
            return;

        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }

    private void OnDisable()
    {
        SetFlashAlpha(0f);
        if (lightningLight != null) { lightningLight.intensity = 0f; lightningLight.enabled = false; }
        if (baseAmbientIntensity > 0f) RenderSettings.ambientIntensity = baseAmbientIntensity;
        if (bolt != null) { bolt.enabled = false; branch.enabled = false; }
    }
}
