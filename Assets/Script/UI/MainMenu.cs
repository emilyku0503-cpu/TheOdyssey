using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public GameObject AboutPanel;

    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    [Header("UI Sound")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;

    private void PlayUISound()
    {
        if (uiAudioSource != null && clickSound != null)
        {
            uiAudioSource.PlayOneShot(clickSound);
        }
    }

    private void Start()
    {
        fadePanel.alpha = 1f;
        StartCoroutine(FadeIn());
    }

    public void StartGame()
    {
        PlayUISound();
        StartCoroutine(StartGameWithFade());
    }

    public void OpenAboutPanel()
    {
        PlayUISound();
        StartCoroutine(OpenAboutPanelWithFade());
    }

    public void CloseAboutPanel()
    {
        PlayUISound();
        StartCoroutine(CloseAboutPanelWithFade());
    }

    public void QuitGame()
    {
        PlayUISound();
        StartCoroutine(QuitWithFade());
    }

    private IEnumerator StartGameWithFade()
    {
        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene("OpeningCutscene");
    }

    private IEnumerator OpenAboutPanelWithFade()
    {
        yield return StartCoroutine(FadeOut());

        AboutPanel.SetActive(true);

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator CloseAboutPanelWithFade()
    {
        yield return StartCoroutine(FadeOut());

        AboutPanel.SetActive(false);

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator QuitWithFade()
    {
        yield return StartCoroutine(FadeOut());

        Debug.Log("Quit Game");
        Application.Quit();
    }

    private IEnumerator FadeOut()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 0f;
    }
}