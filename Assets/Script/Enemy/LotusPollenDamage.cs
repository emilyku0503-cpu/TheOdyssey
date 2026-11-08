
using System.Collections;
using UnityEngine;

public class LotusPollen : MonoBehaviour
{
    [Header("Pollen Attack")]
    [SerializeField] private ParticleSystem pollen;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private int particlesPerShot = 20;

    private Damage damage;
    private Coroutine attackCoroutine;

    private void Awake()
    {
        if (pollen == null)
            pollen = GetComponent<ParticleSystem>();

        damage = GetComponent<Damage>();
    }

    private void Start()
    {
        if (pollen == null || damage == null)
        {
            Debug.LogWarning("Missing ParticleSystem or Damage!");
            return;
        }

        // Keep the particle simulation running
        pollen.Play();

        // Fire pollen at set intervals
        attackCoroutine = StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            // Fire pollen particles
            pollen.Emit(particlesPerShot);

            Debug.Log("Lotus fired pollen!");

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, attackInterval)
            );
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || damage == null)
            return;

        // Use existing Damage.cs value
        playerHealth.TakeDamage(damage.damageAmount);

        Debug.Log("Player hit by Lotus Pollen!");
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }
}
