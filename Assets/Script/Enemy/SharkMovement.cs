
using UnityEngine;

public class SharkMovement : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;

    [Header("Dive Settings")]
    [SerializeField] private float diveDepth = 1.5f;

    [Range(0.05f, 0.45f)]
    [SerializeField] private float emergeEnd = 0.25f;

    [Range(0f, 0.95f)]
    [SerializeField] private float diveStart = 0.7f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private float elapsedTime;
    private float travelDuration;

    private bool movingToB = true;

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("Patrol points are missing!");
            enabled = false;
            return;
        }

        if (Vector3.Distance(pointA.position, pointB.position)
            < 0.01f)
        {
            Debug.LogWarning("Patrol points are too close!");
            enabled = false;
            return;
        }

        // Begin underwater at Point A
        transform.position =
            pointA.position + Vector3.down * diveDepth;

        SetNextTarget();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsedTime / travelDuration
        );

        // Smooth acceleration and deceleration
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        Vector3 horizontalPosition = Vector3.Lerp(
            startPosition,
            endPosition,
            easedT
        );

        // Gradually emerge from underwater
        float emerge = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(0f, emergeEnd, t)
        );

        // Gradually dive near destination
        float dive = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(diveStart, 1f, t)
        );

        float submergeAmount =
            1f - emerge * (1f - dive);

        // Move underwater smoothly
        transform.position =
            horizontalPosition
            + Vector3.down * diveDepth * submergeAmount;

        // Reached destination
        if (t >= 1f)
        {
            movingToB = !movingToB;
            SetNextTarget();
        }
    }

    private void SetNextTarget()
    {
        startPosition = movingToB
            ? pointA.position
            : pointB.position;

        endPosition = movingToB
            ? pointB.position
            : pointA.position;

        float distance = Vector3.Distance(
            startPosition,
            endPosition
        );

        travelDuration = Mathf.Max(
            0.01f,
            distance / Mathf.Max(0.01f, speed)
        );

        elapsedTime = 0f;

        FaceTarget();
    }

    private void FaceTarget()
    {
        Vector3 direction =
            endPosition - startPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Shark forward = +Z
        transform.rotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            pointA.position,
            pointB.position
        );

        Gizmos.DrawWireSphere(pointA.position, 0.2f);
        Gizmos.DrawWireSphere(pointB.position, 0.2f);
    }
}
