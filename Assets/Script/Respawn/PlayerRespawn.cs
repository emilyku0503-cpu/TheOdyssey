using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    private Transform currentCheckpoint;
    private CharacterController characterController;
    private Vector3 fallbackPosition;
    private Quaternion fallbackRotation = Quaternion.identity;

    // ---- 씬이 바뀌어도(GameOver 화면을 거쳐도) 남아 있는 마지막 체크포인트 정보 ----
    private static bool hasSavedCheckpoint;
    private static string savedScene;
    private static Vector3 savedPosition;
    private static Quaternion savedRotation;

    // GameOver 화면에서 YES 를 누르면 true → 다음에 씬이 열릴 때 마지막 체크포인트에서 시작
    private static bool respawnOnNextLoad;

    // 에디터에서 Play 를 새로 시작할 때 이전 플레이의 값이 남지 않도록 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hasSavedCheckpoint = false;
        respawnOnNextLoad = false;
    }

    // GameOver 에서 YES 를 눌렀을 때 호출
    public static void RequestRespawnAtCheckpoint()
    {
        respawnOnNextLoad = true;
    }

    // 메인메뉴로 나가는 등 처음부터 다시 시작할 때 호출
    public static void ClearSavedCheckpoint()
    {
        hasSavedCheckpoint = false;
        respawnOnNextLoad = false;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // 실제로 조작하는 플레이어만 처리 (NPC 등에 복사된 캐릭터는 무시)
        // → 둘 다 같은 자리로 순간이동하면 플레이어가 복사본 위에 올라타 계속 낙하 모션이 됨
        if (!IsControlledPlayer())
            return;

        // 체크포인트를 아직 안 지났을 때 돌아갈 곳 = 스테이지 시작 위치
        fallbackPosition = transform.position;
        fallbackRotation = transform.rotation;

        string scene = SceneManager.GetActiveScene().name;
        if (respawnOnNextLoad && hasSavedCheckpoint && savedScene == scene)
        {
            MoveTo(savedPosition, savedRotation);
            // GameOver 에서 돌아온 경우엔 그 체크포인트가 새 기본 리스폰 위치
            fallbackPosition = savedPosition;
            fallbackRotation = savedRotation;
            Debug.Log("Respawned at last checkpoint: " + savedPosition);
        }
        else if (hasSavedCheckpoint && savedScene != scene)
        {
            // 다른 스테이지로 넘어왔으면 예전 스테이지의 체크포인트는 버림
            hasSavedCheckpoint = false;
        }
        respawnOnNextLoad = false;
    }

    private bool IsControlledPlayer()
    {
        var controller = GetComponent<StarterAssets.ThirdPersonController>();
        return controller != null && controller.enabled;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;

        if (checkpoint != null && IsControlledPlayer())
        {
            hasSavedCheckpoint = true;
            savedScene = SceneManager.GetActiveScene().name;
            savedPosition = checkpoint.position;
            savedRotation = checkpoint.rotation;
        }
    }

    public void Respawn()
    {
        if (currentCheckpoint != null)
            MoveTo(currentCheckpoint.position, currentCheckpoint.rotation);
        else
            MoveTo(fallbackPosition, fallbackRotation); // 아직 체크포인트 없음 → 시작 위치(또는 GameOver 후 복귀한 체크포인트)
    }

    // 체크포인트(RespawnPoint)는 플랫폼 모델을 따라 옆으로 기울어져 있을 수 있음.
    // 그 회전을 그대로 쓰면 캐릭터가 누운 채로 서서 헤엄/낙하 모션이 되므로,
    // 바라보는 방향(좌우)만 가져오고 몸은 항상 똑바로 세움.
    private static Quaternion UprightYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            // 앞쪽이 위/아래를 향하는 경우엔 위쪽 방향을 기준으로 삼음
            forward = rotation * Vector3.up;
            forward.y = 0f;
        }
        if (forward.sqrMagnitude < 0.0001f) return Quaternion.identity;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private void MoveTo(Vector3 position, Quaternion rotation)
    {
        if (characterController != null) characterController.enabled = false;

        transform.position = position;
        transform.rotation = UprightYaw(rotation);
        Physics.SyncTransforms();

        if (characterController != null) characterController.enabled = true;
    }
}
