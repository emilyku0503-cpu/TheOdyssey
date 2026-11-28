using UnityEngine;
using UnityEngine.SceneManagement;

// Keep only the water audio alive, not the water mesh or gameplay objects.
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class WreckedShipAmbience : MonoBehaviour
{
    private static WreckedShipAmbience instance;
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        if (instance != null && instance != this)
        {
            source.Stop();
            source.enabled = false;
            Destroy(gameObject);
            return;
        }
        instance = this;
        source.loop = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (instance == this && !source.isPlaying) source.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || scene.name == "Wrecked Ship" ||
            scene.name == "CutSCene#2") return;
        source.Stop();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
