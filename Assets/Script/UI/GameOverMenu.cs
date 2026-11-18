using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 게임오버 화면: YES = 직전 스테이지의 마지막 체크포인트에서 다시 시작
// (체크포인트를 아직 안 지났으면 스테이지 처음부터), NO = 메인메뉴로.
// 게임오버로 넘어갈 때는 GameOverMenu.Show() 를 호출하면 현재 씬을 기억해 둡니다.
public class GameOverMenu : MonoBehaviour
{
    public const string GameOverSceneName = "GameOver";

    // 게임오버 직전에 플레이하던 씬 (YES를 누르면 여기로 돌아감)
    public static string LastPlayedScene;

    [Tooltip("LastPlayedScene 이 없을 때(예: 에디터에서 바로 GameOver 씬을 실행) 다시 시작할 씬")]
    public string fallbackScene = "Level";
    public string mainMenuScene = "MainMenu";

    [Header("Selection Arrow")]
    [Tooltip("마우스를 올린 버튼 옆으로 따라 움직이는 화살표")]
    public RectTransform arrow;
    [Tooltip("버튼 텍스트 왼쪽에서 화살표까지의 거리")]
    public float arrowGap = 30f;
    public float arrowSpeed = 14f;
    [Tooltip("처음에 화살표가 가리킬 버튼 (YES)")]
    public RectTransform defaultTarget;

    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    [Header("UI Sound")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;

    private Vector2 arrowTargetPos;
    private bool busy;

    public static void Show()
    {
        LastPlayedScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(GameOverSceneName);
    }

    private void Start()
    {
        Time.timeScale = 1f; // 일시정지 상태로 넘어와도 화면이 멈추지 않게
        // 게임 중에 마우스 커서를 숨기거나 잠갔어도 버튼을 누를 수 있게 풀어줌
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (defaultTarget != null) MoveArrowTo(defaultTarget, true);
        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            StartCoroutine(Fade(1f, 0f));
        }
    }

    private void Update()
    {
        if (arrow == null) return;
        float k = 1f - Mathf.Exp(-arrowSpeed * Time.unscaledDeltaTime);
        arrow.anchoredPosition = Vector2.Lerp(arrow.anchoredPosition, arrowTargetPos, k);
    }

    // 버튼에 마우스를 올리면 MenuArrowTarget 이 호출
    public void MoveArrowTo(RectTransform button, bool instant = false)
    {
        if (arrow == null || button == null) return;
        var text = button.GetComponentInChildren<TMPro.TMP_Text>();
        float halfText = text != null ? text.preferredWidth * 0.5f : button.rect.width * 0.5f;
        arrowTargetPos = button.anchoredPosition + new Vector2(-(halfText + arrowGap + arrow.rect.width * 0.5f), 0f);
        if (instant) arrow.anchoredPosition = arrowTargetPos;
    }

    public void PlayAgain()
    {
        if (busy) return;
        string scene = string.IsNullOrEmpty(LastPlayedScene) ? fallbackScene : LastPlayedScene;
        // 스테이지 처음이 아니라 마지막으로 지나간 체크포인트에서 다시 시작
        PlayerRespawn.RequestRespawnAtCheckpoint();
        StartCoroutine(LoadWithFade(scene));
    }

    public void BackToMainMenu()
    {
        if (busy) return;
        // 메인메뉴로 나가면 다음엔 처음부터 시작하도록 체크포인트 초기화
        PlayerRespawn.ClearSavedCheckpoint();
        StartCoroutine(LoadWithFade(mainMenuScene));
    }

    private IEnumerator LoadWithFade(string scene)
    {
        busy = true;
        if (uiAudioSource != null && clickSound != null) uiAudioSource.PlayOneShot(clickSound);
        if (fadePanel != null) yield return Fade(0f, 1f);
        if (scene != mainMenuScene)
        {
            // 게임으로 돌아갈 땐 다시 커서를 잠가서 시점 조작이 바로 되게
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        SceneManager.LoadScene(scene);
    }

    private IEnumerator Fade(float from, float to)
    {
        fadePanel.blocksRaycasts = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f); // 로딩 직후 시간 튐 방지
            fadePanel.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadePanel.alpha = to;
        fadePanel.blocksRaycasts = to > 0.5f;
    }
}
