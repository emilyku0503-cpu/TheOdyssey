
using UnityEngine;

public class HangingLightSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingAngle = 15f;
    public float swingSpeed = 1.5f;

    [Header("Swing Axis")]
    public Vector3 swingAxis = Vector3.forward;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;

        transform.localRotation =
            initialRotation *
            Quaternion.AngleAxis(angle, swingAxis.normalized);
    }
}
