using UnityEngine;
using UnityEngine.UI;

// TheOdyssey/UI/BackgroundFX 셰이더(돛 펄럭임, 물결 일렁임)에 시간을 넘겨준다.
// 게임이 일시정지(timeScale 0)여도 배경은 계속 움직이도록 실제 시간을 사용.
[RequireComponent(typeof(Graphic))]
public class BackgroundFXTime : MonoBehaviour
{
    private static readonly int FxTime = Shader.PropertyToID("_FxTime");
    private Material mat;

    private void Awake()
    {
        var g = GetComponent<Graphic>();
        if (g.material != null && g.material.HasProperty(FxTime))
        {
            mat = new Material(g.material);   // runtime copy so the asset isn't modified while playing
            g.material = mat;
        }
    }

    private void Update()
    {
        if (mat != null) mat.SetFloat(FxTime, Time.unscaledTime);
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
