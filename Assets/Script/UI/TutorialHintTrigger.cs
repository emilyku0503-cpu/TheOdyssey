using System.Collections;
using UnityEngine;

// 플레이어가 들어오면 조작 힌트를 한 번 띄운다. (TutorialHintUI 필요)
[RequireComponent(typeof(Collider))]
public class TutorialHintTrigger : MonoBehaviour
{
    [SerializeField] private string keys = "[SPACE]";
    [SerializeField] private string message = "Jump";
    [SerializeField] private TutorialHintUI.Goal goal = TutorialHintUI.Goal.Jump;

    [Header("Start")]
    [Tooltip("켜면 트리거 대신 씬 시작 후 자동으로 표시")]
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private float delay = 0f;

    private bool done;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        if (showOnStart) StartCoroutine(ShowAfter(delay));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showOnStart || done || !other.CompareTag("Player")) return;
        StartCoroutine(ShowAfter(delay));
    }

    private IEnumerator ShowAfter(float d)
    {
        if (done) yield break;
        done = true;
        if (d > 0f) yield return new WaitForSeconds(d);
        if (TutorialHintUI.Instance != null) TutorialHintUI.Instance.Show(keys, message, goal);
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
