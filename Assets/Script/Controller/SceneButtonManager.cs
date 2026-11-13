
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string scene1Name;
    [SerializeField] private string scene2Name;
    [SerializeField] private string scene3Name;

    public void LoadScene1()
    {
        LoadScene(scene1Name);
    }

    public void LoadScene2()
    {
        LoadScene(scene2Name);
    }

    public void LoadScene3()
    {
        LoadScene(scene3Name);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene not found: " + sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
