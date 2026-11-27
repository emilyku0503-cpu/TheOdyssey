using UnityEngine;

// 위아래로 천천히 오르내리는 발판 (타이밍 맞춰 뛰어야 하는 기둥).
[RequireComponent(typeof(Rigidbody))]
public class BobbingPlatform : MonoBehaviour
{
    [SerializeField] private float amplitude = 1.2f;
    [SerializeField] private float period = 3.2f;
    [Tooltip("0~1: 다른 기둥과 박자를 엇갈리게")]
    [SerializeField, Range(0f, 1f)] private float phase = 0f;

    private Vector3 home;
    private Rigidbody body;

    private void Awake()
    {
        home = transform.position;
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        float y = Mathf.Sin((Time.time / Mathf.Max(0.1f, period) + phase) * Mathf.PI * 2f) * amplitude;
        body.MovePosition(home + Vector3.up * y);
    }
}
