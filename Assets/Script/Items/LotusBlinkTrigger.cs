
using UnityEngine;

public class LotusBlinkTrigger : MonoBehaviour
{
    public ScreenBlink screenBlink;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Touched by: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Lotus Blink Triggered!");
            if (screenBlink != null)
            {
                screenBlink.PlayBlink();
            }
        }
    }
}
