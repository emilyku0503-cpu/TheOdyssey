using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StarterAssets;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameSettings : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer audioMixer;
    [Tooltip("The existing volume slider now controls Music.")]
    public Slider volumeSlider;
    public Slider sfxSlider;
    public GameObject settingsPanel;

    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;
    public string homeScene = "Level";

    [Header("Player Control (automatically found if empty)")]
    public StarterAssetsInputs playerInput;
    public ThirdPersonController thirdPersonController;

    private const string MusicKey = "Settings.MusicVolume";
    private const string SfxKey = "Settings.SFXVolume";
    private static GameSettings pauseOwner;
    public static bool IsPaused => pauseOwner != null;
    private bool paused;
    private bool leaving;
    private float previousTimeScale;
    private bool previousControllerEnabled, previousInputEnabled;
    private bool previousCursorInput, previousCursorLocked, previousCursorVisible;
    private CursorLockMode previousCursorMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPauseOwner() => pauseOwner = null;

    private void Start()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("GameSettings: assign the settings panel.", this);
            enabled = false;
            return;
        }
        UpgradeLegacyControls();
        ConfigureSlider(volumeSlider, MusicKey);
        ConfigureSlider(sfxSlider, SfxKey);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(SetVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        ApplyVolume("MusicVolume", PlayerPrefs.GetFloat(MusicKey, 1f));
        ApplyVolume("SFXVolume", PlayerPrefs.GetFloat(SfxKey, 1f));
        settingsPanel.SetActive(false);
    }

    // Upgrade old prefab instances without replacing their artwork or buttons.
    // Explicitly assigned pairs of sliders keep their authored layout.
    private void UpgradeLegacyControls()
    {
        if (volumeSlider == null || sfxSlider != null) return;
        sfxSlider = Instantiate(volumeSlider, volumeSlider.transform.parent);
        sfxSlider.name = "SFXSlider";
        volumeSlider.name = "MusicSlider";
        SetRow(volumeSlider.GetComponent<RectTransform>(), 110f, 70f);
        SetRow(sfxSlider.GetComponent<RectTransform>(), 110f, -40f);
        // The old cloned persistent listener targets the obsolete master volume.
        volumeSlider.onValueChanged = new Slider.SliderEvent();
        sfxSlider.onValueChanged = new Slider.SliderEvent();

        foreach (TMP_Text label in settingsPanel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.gameObject.name != "VolumeLabel" && label.text.Trim() != "Volume") continue;
            label.text = "Music";
            SetRow(label.rectTransform, -330f, 70f);
            TMP_Text sfxLabel = Instantiate(label, label.transform.parent);
            sfxLabel.name = "SFXLabel";
            sfxLabel.text = "SFX";
            SetRow(sfxLabel.rectTransform, -330f, -40f);
            break;
        }
    }

    private static void SetRow(RectTransform rect, float x, float y)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private static void ConfigureSlider(Slider slider, string key)
    {
        if (slider == null) return;
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f)));
    }

    private void Update()
    {
        bool escape = false;
#if ENABLE_INPUT_SYSTEM
        escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        escape = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!escape || leaving) return;
        if (paused) CloseSettings(); else OpenSettings();
    }

    // Retain this name for existing prefab/scene event bindings.
    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        ApplyVolume("MusicVolume", volume);
        PlayerPrefs.SetFloat(MusicKey, volume);
    }

    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        ApplyVolume("SFXVolume", volume);
        PlayerPrefs.SetFloat(SfxKey, volume);
    }

    private void ApplyVolume(string parameter, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogError("GameSettings: assign GameAudioMixer, used by the title screen.", this);
            return;
        }
        float db = volume <= 0.0001f ? -80f : 20f * Mathf.Log10(Mathf.Clamp01(volume));
        if (!audioMixer.SetFloat(parameter, db))
            Debug.LogError("GameSettings: missing exposed mixer parameter " + parameter, this);
    }

    public void OpenSettings()
    {
        if (paused || leaving || IsPaused || settingsPanel == null) return;
        if (thirdPersonController == null)
            thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
        if (playerInput == null && thirdPersonController != null)
            playerInput = thirdPersonController.GetComponent<StarterAssetsInputs>();
        // Do not interrupt a death sequence or a temporary hit stop.
        if (Time.timeScale <= 0f) return;
        if (thirdPersonController != null)
        {
            var health = thirdPersonController.GetComponent<PlayerHealth>();
            if (!thirdPersonController.enabled || (health != null && health.currentHealth <= 0)) return;
        }
        previousTimeScale = Time.timeScale;
        previousCursorMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        if (thirdPersonController != null)
        {
            previousControllerEnabled = thirdPersonController.enabled;
            thirdPersonController.enabled = false;
        }
        if (playerInput != null)
        {
            previousInputEnabled = playerInput.enabled;
            previousCursorInput = playerInput.cursorInputForLook;
            previousCursorLocked = playerInput.cursorLocked;
            ClearInput();
            playerInput.cursorInputForLook = false;
            playerInput.cursorLocked = false;
            playerInput.enabled = false;
        }
        paused = true;
        pauseOwner = this;
        Time.timeScale = 0f;
        settingsPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        if (leaving) return;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        RestoreGameplay();
        PlayerPrefs.Save();
    }

    private void ClearInput()
    {
        if (playerInput == null) return;
        playerInput.move = playerInput.look = Vector2.zero;
        playerInput.jump = playerInput.sprint = false;
    }

    private void RestoreGameplay()
    {
        if (!paused) return;
        paused = false;
        if (pauseOwner == this) pauseOwner = null;
        ClearInput();
        if (thirdPersonController != null) thirdPersonController.enabled = previousControllerEnabled;
        if (playerInput != null)
        {
            playerInput.cursorInputForLook = previousCursorInput;
            playerInput.cursorLocked = previousCursorLocked;
            playerInput.enabled = previousInputEnabled;
        }
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorMode;
        Cursor.visible = previousCursorVisible;
    }

    public void GoToHome()
    {
        if (leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(homeScene))
        {
            Debug.LogError("GameSettings: scene not in build list: " + homeScene, this);
            return;
        }
        leaving = true;
        PlayerPrefs.Save();
        StartCoroutine(GoToHomeWithFade());
    }

    private IEnumerator GoToHomeWithFade()
    {
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeDuration));
                yield return null;
            }
            fadePanel.alpha = 1f;
        }
        RestoreGameplay();
        SceneManager.LoadScene(homeScene);
    }

    private void OnDisable()
    {
        RestoreGameplay();
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(SetVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }
}
