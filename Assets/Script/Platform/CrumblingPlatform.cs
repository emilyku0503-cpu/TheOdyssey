using System.Collections;
using System.Linq;
using UnityEngine;

// 밟으면 덜덜 떨다가 낭떠러지로 떨어지고, 잠시 뒤 아래에서 다시 올라오는 발판.
// 이 오브젝트에 "밟음 감지용" 트리거 박스를 두고, 실제 발판(기둥)은 자식으로 둔다.
public class CrumblingPlatform : MonoBehaviour
{
    [SerializeField] private float shakeTime = 0.75f;
    [SerializeField] private float shakeAmount = 0.07f;
    [SerializeField] private float fallDistance = 9f;
    [SerializeField] private float fallTime = 0.9f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float riseTime = 1.2f;

    private Vector3 home;
    private bool busy;
    private Collider[] solids;
    private ParticleSystem dust;

    private void Awake()
    {
        home = transform.position;
        solids = GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();
        dust = GetComponentInChildren<ParticleSystem>(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (busy || !other.CompareTag("Player")) return;
        StartCoroutine(Crumble());
    }

    private IEnumerator Crumble()
    {
        busy = true;
        if (dust != null) { dust.gameObject.SetActive(true); dust.Play(); }

        // rattle: a warning you can still escape
        for (float t = 0; t < shakeTime; t += Time.deltaTime)
        {
            float k = t / shakeTime;
            transform.position = home + new Vector3(Random.Range(-1f, 1f), Random.Range(-0.4f, 0.4f), Random.Range(-1f, 1f)) * shakeAmount * (0.5f + k);
            yield return null;
        }

        // drop into the abyss
        foreach (var c in solids) c.enabled = false;
        for (float t = 0; t < fallTime; t += Time.deltaTime)
        {
            float k = t / fallTime;
            transform.position = home + Vector3.down * fallDistance * k * k;
            yield return null;
        }
        transform.position = home + Vector3.down * fallDistance;

        yield return new WaitForSeconds(respawnDelay);

        // rise back from the fog
        foreach (var c in solids) c.enabled = true;
        for (float t = 0; t < riseTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / riseTime);
            transform.position = Vector3.Lerp(home + Vector3.down * fallDistance, home, k);
            yield return null;
        }
        transform.position = home;
        busy = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        transform.position = home;
        if (solids != null) foreach (var c in solids) c.enabled = true;
        busy = false;
    }
}
