using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    [Header("Shared audio source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound effects")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.7f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isActiveAndEnabled &&
            button.IsActive() &&
            button.IsInteractable())
        {
            PlaySound(hoverSound, hoverVolume);
        }
    }

    private void PlayClickSound()
    {
        // An existing click action may already have hidden this button.
        // The shared Audio Source can still finish playing the sound.
        PlaySound(clickSound, clickVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource != null &&
            audioSource.isActiveAndEnabled &&
            clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }
}