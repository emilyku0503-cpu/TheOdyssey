
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public enum StartPoint
    {
        PointA,
        PointB
    }

    [Header("Patrol Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Random Speed")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 3f;

    [Header("Starting Point")]
    [SerializeField] private StartPoint startPoint = StartPoint.PointA;

    private Vector3 segmentStart;
    private Vector3 segmentEnd;

    private float currentSpeed;
    private float elapsedTime;
    private float travelDuration;

    private bool movingToB;

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("Patrol points are missing!");
            enabled = false;
            return;
        }

        // Randomize speed once
        currentSpeed = Random.Range(
            Mathf.Max(0.01f, Mathf.Min(minSpeed, maxSpeed)),
            Mathf.Max(0.01f, Mathf.Max(minSpeed, maxSpeed))
        );

        // Set starting point
        movingToB = startPoint == StartPoint.PointA;

        transform.position = movingToB
            ? pointA.position
            : pointB.position;

        SetNextTarget();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / travelDuration);

        // Ease In & Out
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(
            segmentStart,
            segmentEnd,
            easedT
        );

        // Switch direction after reaching destination
        if (t >= 1f)
        {
            movingToB = !movingToB;
            SetNextTarget();
        }
    }

    private void SetNextTarget()
    {
        segmentStart = transform.position;

        segmentEnd = movingToB
            ? pointB.position
            : pointA.position;

        travelDuration = Mathf.Max(
            0.01f,
            Vector3.Distance(segmentStart, segmentEnd)
            / currentSpeed
        );

        elapsedTime = 0f;
    }

    // Draw patrol path in Scene View
    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawWireSphere(pointA.position, 0.2f);
        Gizmos.DrawWireSphere(pointB.position, 0.2f);
    }
}
