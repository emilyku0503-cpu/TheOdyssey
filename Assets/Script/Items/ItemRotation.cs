
using UnityEngine;

public class ItemRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;

    private float yRotation = 0f;

    void Start()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
    }

    void Update()
    {
        yRotation += rotationSpeed * Time.deltaTime;

        transform.localRotation = Quaternion.Euler(
            0f,
            yRotation,
            -20f
        );
    }
}
