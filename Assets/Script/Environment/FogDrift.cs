using UnityEngine;

// 낭떠러지 아래 안개층: 텍스처를 천천히 흘려서 안개가 움직이는 것처럼 보이게 한다.
[RequireComponent(typeof(Renderer))]
public class FogDrift : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.006f, 0.003f);
    [Tooltip("아주 느리게 밝기가 숨 쉬듯 변하는 폭")]
    [SerializeField, Range(0f, 0.5f)] private float breathe = 0.12f;

    private Material mat;
    private Color baseColor;
    private Vector2 offset;
    private float seed;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        mat = GetComponent<Renderer>().material;      // own copy so layers drift independently
        baseColor = mat.HasProperty(BaseColor) ? mat.GetColor(BaseColor) : Color.white;
        seed = Random.Range(0f, 100f);
        offset = new Vector2(Random.value, Random.value);
    }

    private void Update()
    {
        offset += speed * Time.deltaTime;
        if (mat.HasProperty(BaseMap)) mat.SetTextureOffset(BaseMap, offset);
        if (breathe > 0f && mat.HasProperty(BaseColor))
        {
            var c = baseColor;
            c.a *= 1f + Mathf.Sin(Time.time * 0.25f + seed) * breathe;
            mat.SetColor(BaseColor, c);
        }
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
