using UnityEngine;

// 물 위 장식(연잎 등)이 파도에 맞춰 살짝 오르내리고 흔들리게 하는 스크립트.
// 위치마다 위상이 달라서 여러 개를 놓아도 똑같이 움직이지 않는다.
public class FloatBob : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 1.3f;
    [SerializeField] private float tiltAngle = 3f;
    [SerializeField] private float spinSpeed = 2f;

    private Vector3 startPos;
    private Quaternion startRot;
    private float phase;

    private void Awake()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        Vector3 p = transform.position;
        phase = p.x * 0.37f + p.z * 0.21f;
    }

    private void Update()
    {
        float t = Time.time * bobSpeed + phase;
        transform.localPosition = startPos + Vector3.up * (Mathf.Sin(t) * bobHeight);
        transform.localRotation = startRot
            * Quaternion.Euler(Mathf.Sin(t * 0.8f) * tiltAngle, Time.time * spinSpeed, Mathf.Cos(t * 0.7f) * tiltAngle);
    }
}
