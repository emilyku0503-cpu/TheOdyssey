
using UnityEngine;

public class LotusBlinkTrigger : MonoBehaviour
{
    [Header("Screen Blink")]
    [SerializeField] private ScreenBlink screenBlink;
    [SerializeField] private float blinkCooldown = 3f;

    private float nextBlinkTime;

    private void OnParticleCollision(GameObject other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (Time.time < nextBlinkTime)
            return;

        nextBlinkTime = Time.time + blinkCooldown;

        Debug.Log("Lotus Blink Triggered!");

        if (screenBlink != null)
        {
            screenBlink.PlayBlink();
        }
    }
}
