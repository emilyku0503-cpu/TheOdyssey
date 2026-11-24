using System.Collections;
using UnityEngine;

// 튜토리얼 구간의 끝. 플레이어가 지나가면 체력 보호(PlayerHealth.TutorialProtection)를 끈다.
// 체크포인트에서 이어하기 등으로 이미 이 선을 넘어선 위치에서 시작하면 바로 끈다.
// (트리거의 파란 화살표 = 진행 방향)
[RequireComponent(typeof(Collider))]
public class TutorialEndTrigger : MonoBehaviour
{
    private bool ended;

    private IEnumerator Start()
    {
        GetComponent<Collider>().isTrigger = true;
        // wait for PlayerStartPosition / PlayerRespawn to place the player
        yield return null;
        yield return null;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && Vector3.Dot(p.transform.position - transform.position, transform.forward) > 0f)
            End(p);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        End(other.gameObject);
    }

    private void End(GameObject player)
    {
        if (ended) return;
        var health = player.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        ended = true;
        health.TutorialProtection = false;
        Debug.Log("Tutorial finished: health is now active.");
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 3f);
    }
}
