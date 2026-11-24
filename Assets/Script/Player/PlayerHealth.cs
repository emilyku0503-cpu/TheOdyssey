
using System.Collections;
using UnityEngine;

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
    [Tooltip("Delay before showing GameOver screen")]
    [SerializeField] private float gameOverDelay = 0.8f;

    // NEW: Debug Mode
    [Header("Debug Settings")]
    [Tooltip("When enabled, Game Over will only print a Debug Log instead of opening the Game Over screen.")]
    [SerializeField] private bool debugMode = false;

    [Header("Tutorial")]
    [Tooltip("켜져 있으면 맞거나 물에 빠져도 체력이 줄지 않음 (피격 연출과 리스폰은 그대로). TutorialEndTrigger 를 지나면 꺼짐")]
    [SerializeField] private bool tutorialProtection = false;
    public bool TutorialProtection { get => tutorialProtection; set => tutorialProtection = value; }

    private bool canTakeDamage = true;
    private bool isDead;
    private Renderer[] renderers;
    private PlayerRespawn respawn;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        respawn = GetComponent<PlayerRespawn>();

        currentHealth = maxHealth;

        if (!IsControlledPlayer())
            return;

        if (healthUI == null)
        {
            healthUI = FindFirstObjectByType<HealthUI>();
        }

        if (healthUI != null)
            healthUI.UpdateHealth(currentHealth);
    }

    private bool IsControlledPlayer()
    {
        var controller =
            GetComponent<StarterAssets.ThirdPersonController>();

        return controller != null && controller.enabled;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Damage damage =
            hit.collider.GetComponentInParent<Damage>();

        if (damage != null)
        {
            TakeDamage(damage.damageAmount);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Damage damage =
            other.GetComponentInParent<Damage>();

        if (damage != null)
        {
            TakeDamage(damage.damageAmount);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!canTakeDamage || isDead || !IsControlledPlayer())
            return;

        LoseHealth(amount);
    }

    public void FallIntoWater()
    {
        if (isDead || !IsControlledPlayer())
            return;

        diedInWater = true;
        LoseHealth(1);
        diedInWater = false;
    }

    // 마지막 체력을 물에 빠져서 잃었는지 (죽음 연출: 쓰러짐 대신 가라앉음)
    private bool diedInWater;

    private void LoseHealth(int amount)
    {
        if (tutorialProtection) amount = 0;   // tutorial: keep the hit feel + respawn, but no health loss
        currentHealth -= amount;
        currentHealth =
            Mathf.Clamp(currentHealth, 0, maxHealth);

        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (healthUI != null)
            healthUI.UpdateHealth(currentHealth);

        Debug.Log("Remaining Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            GameOver();
            return;
        }

        // 히트스톱·카메라 흔들림·붉은 번쩍임 → 화면이 어두워진 사이에 체크포인트로 이동
        var reaction = GetComponent<HitReaction>();
        if (reaction == null) reaction = gameObject.AddComponent<HitReaction>();
        reaction.Play(respawn != null ? (System.Action)respawn.Respawn : null);

        StopAllCoroutines();
        SetRenderers(true);

        StartCoroutine(DamageCooldown());
        StartCoroutine(Blink());
    }

    public void AddHealth(int amount)
    {
        currentHealth += amount;
        currentHealth =
            Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Health: " + currentHealth);

        if (healthUI != null)
            healthUI.UpdateHealth(currentHealth);
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
        if (isDead)
            return;

        isDead = true;
        canTakeDamage = false;

        StopAllCoroutines();
        SetRenderers(true);

        Debug.Log("GAME OVER");

        // NEW: Debug Mode
        if (debugMode)
        {
            Debug.LogWarning(
                "[DEBUG MODE] Player Health reached 0. " +
                "Game Over screen and scene transition skipped."
            );

            return;
        }

        // Normal Game Over: death sequence (fall / sink, slow motion, red flash, fade to black), then GameOver screen
        var death = GetComponent<DeathSequence>();
        if (death == null) death = gameObject.AddComponent<DeathSequence>();
        death.Play(diedInWater, GameOverMenu.Show);
    }

    private IEnumerator GoToGameOver()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);

        GameOverMenu.Show();
    }
}
