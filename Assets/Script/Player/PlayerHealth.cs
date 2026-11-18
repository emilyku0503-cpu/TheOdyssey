using System.Collections;
using UnityEngine;

// 체력 3칸. 맞거나 물에 빠질 때마다 1칸(데미지만큼) 줄고 마지막 체크포인트로 리스폰.
// 다 닳으면 GameOver 화면으로.
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth = 3;

    [SerializeField] private float damageCooldown = 2f;
    [SerializeField] private float blinkDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private HealthUI healthUI;

    [Header("Damage Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;

    [Header("Game Over")]
    [Tooltip("체력이 0이 된 뒤 GameOver 화면으로 넘어가기까지 기다리는 시간 (피격 소리가 들리도록)")]
    [SerializeField] private float gameOverDelay = 0.8f;

    private bool canTakeDamage = true;
    private bool isDead;
    private Renderer[] renderers;
    private PlayerRespawn respawn;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        respawn = GetComponent<PlayerRespawn>();

        // 씬을 새로 시작하거나 GameOver 에서 YES 로 돌아오면 항상 꽉 찬 체력
        currentHealth = maxHealth;

        if (!IsControlledPlayer())
            return; // NPC 에 복사된 캐릭터는 체력 UI 를 건드리지 않음

        if (healthUI == null)
        {
            healthUI = FindFirstObjectByType<HealthUI>();
        }

        if (healthUI != null) healthUI.UpdateHealth(currentHealth);
    }

    // 실제로 조작하는 플레이어인지 (Athena 등에 복사된 캐릭터는 제외)
    private bool IsControlledPlayer()
    {
        var controller = GetComponent<StarterAssets.ThirdPersonController>();
        return controller != null && controller.enabled;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Damage damage = hit.collider.GetComponentInParent<Damage>();

        if (damage != null)
        {
            TakeDamage(damage.damageAmount);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Damage damage = other.GetComponentInParent<Damage>();

        if (damage != null)
        {
            TakeDamage(damage.damageAmount);
        }
    }

    // 적/장애물에 맞았을 때 (맞은 직후 잠깐은 무적)
    public void TakeDamage(int amount)
    {
        if (!canTakeDamage || isDead || !IsControlledPlayer())
            return;

        LoseHealth(amount);
    }

    // 물(KillZone)에 빠졌을 때: 무적 시간과 상관없이 항상 1칸 줄고 리스폰
    public void FallIntoWater()
    {
        if (isDead || !IsControlledPlayer())
            return;

        LoseHealth(1);
    }

    private void LoseHealth(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (healthUI != null) healthUI.UpdateHealth(currentHealth);

        Debug.Log("Remaining Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            GameOver();
            return;
        }

        // 체력이 줄 때마다 마지막 체크포인트로 돌아감
        if (respawn != null) respawn.Respawn();

        StopAllCoroutines();
        SetRenderers(true);
        StartCoroutine(DamageCooldown());
        StartCoroutine(Blink());
    }

    public void AddHealth(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Health: " + currentHealth);

        if (healthUI != null) healthUI.UpdateHealth(currentHealth);
    }

    private IEnumerator DamageCooldown()
    {
        canTakeDamage = false;

        yield return new WaitForSeconds(damageCooldown);

        canTakeDamage = true;
    }

    private IEnumerator Blink()
    {
        float elapsed = 0f;

        while (elapsed < blinkDuration)
        {
            SetRenderers(false);
            yield return new WaitForSeconds(blinkInterval);

            SetRenderers(true);
            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval * 2f;
        }

        SetRenderers(true);
    }

    private void SetRenderers(bool visible)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }

    private void GameOver()
    {
        if (isDead) return; // 같은 순간 여러 번 맞아도 한 번만 처리
        isDead = true;
        canTakeDamage = false;
        StopAllCoroutines();
        SetRenderers(true);
        Debug.Log("GAME OVER");
        StartCoroutine(GoToGameOver());
    }

    private IEnumerator GoToGameOver()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        GameOverMenu.Show();
    }
}
