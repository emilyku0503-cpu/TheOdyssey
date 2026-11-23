using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 타임라인 컷씬이 끝나면 다음 씬을 불러온다.
// Enter 를 누르면 마지막 페이드 직전으로 건너뛴다.
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneEndLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "LotusCutscene";
    [Tooltip("건너뛰기 시 이동할 위치: 끝에서 몇 초 전 (페이드아웃이 보이도록)")]
    [SerializeField] private float skipToSecondsBeforeEnd = 1.2f;
    [SerializeField] private bool allowSkip = true;

    private PlayableDirector director;
    private bool loading;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.extrapolationMode = DirectorWrapMode.None;
        director.stopped += OnStopped;
    }

    private void OnDestroy()
    {
        if (director != null) director.stopped -= OnStopped;
    }

    private void Update()
    {
        if (!allowSkip || loading || director.state != PlayState.Playing) return;
#if ENABLE_INPUT_SYSTEM
        bool skip = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#else
        bool skip = Input.GetKeyDown(KeyCode.Return);
#endif
        if (skip)
        {
            double target = director.duration - skipToSecondsBeforeEnd;
            if (director.time < target) director.time = target;
        }
    }

    private void OnStopped(PlayableDirector d)
    {
        if (loading || string.IsNullOrEmpty(nextSceneName)) return;
        loading = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
