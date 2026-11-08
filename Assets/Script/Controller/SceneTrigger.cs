
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTrigger : MonoBehaviour
{
    [Header("Scene Transition")]
    public string sceneName;
    public Image fadeImage;
    public float fadeDuration = 1f;

    // NEW: Level Completion Settings
    [Header("Level Completion")]
    [SerializeField] private bool markLevelComplete = false;
    [SerializeField, Min(1)] private int completedLevelNumber = 1;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger touched by: " + other.name
                  + " / Tag: " + other.tag);

        if (other.CompareTag("Player") && !isTransitioning)
        {
            Debug.Log("Player detected! Loading: " + sceneName);

            isTransitioning = true;
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        float time = 0f;
        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Clamp01(time / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;

        // NEW: Mark the current level as completed
        if (markLevelComplete)
        {
            LevelProgress.CompleteLevel(completedLevelNumber);

            Debug.Log(
                "Level " + completedLevelNumber +
                " completed! Level " +
                (completedLevelNumber + 1) + " unlocked."
            );
        }

        // Existing scene loading
        SceneManager.LoadScene(sceneName);
    }
}
