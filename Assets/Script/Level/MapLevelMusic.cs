using UnityEngine;
using UnityEngine.SceneManagement;

// Attach only to the dedicated BGM root in Map and Wrecked Ship.
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MapLevelMusic : MonoBehaviour
{
    private static MapLevelMusic instance;
    private AudioSource music;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    private void Awake()
    {
        music = GetComponent<AudioSource>();
        music.playOnAwake = false;

        if (instance != null && instance != this)
        {
            // Keep the original source and its current playback position.
            music.Stop();
            music.enabled = false;
            Destroy(gameObject);
            return;
        }

        instance = this;
        music.loop = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (instance == this && !music.isPlaying)
            music.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Additive overlays do not end the music session.
        if (mode != LoadSceneMode.Single ||
            scene.name == "Map" || scene.name == "Wrecked Ship" ||
            scene.name == "CutSCene#2") return;

        music.Stop();
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
