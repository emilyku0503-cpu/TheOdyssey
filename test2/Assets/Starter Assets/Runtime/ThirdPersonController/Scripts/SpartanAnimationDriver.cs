using UnityEngine;
using StarterAssets;

public class SpartanAnimationDriver : MonoBehaviour
{
    public Animator spartanAnimator;
    public CharacterController characterController;
    public ThirdPersonController playerController;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int DoubleJumpHash = Animator.StringToHash("DoubleJump");

    void OnEnable()
    {
        if (playerController != null) playerController.Jumped += HandleJumped;
    }

    void OnDisable()
    {
        if (playerController != null) playerController.Jumped -= HandleJumped;
    }

    void HandleJumped(int jumpCount)
    {
        if (jumpCount == 1)
        {
            spartanAnimator.ResetTrigger(DoubleJumpHash);
            spartanAnimator.SetTrigger(JumpHash);
        }
        else
        {
            spartanAnimator.ResetTrigger(JumpHash);
            spartanAnimator.SetTrigger(DoubleJumpHash);
        }
    }

    void Update()
    {
        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;
        spartanAnimator.SetFloat(SpeedHash, horizontalVelocity.magnitude);

        // 점프 직후 프레임에 Grounded=true가 들어가서 바로 Idle로 돌아가는 문제 방지
        bool grounded = playerController.Grounded && playerController.VerticalVelocity <= 0f;
        spartanAnimator.SetBool(GroundedHash, grounded);

        if (grounded)
        {
            spartanAnimator.ResetTrigger(JumpHash);
            spartanAnimator.ResetTrigger(DoubleJumpHash);
        }
    }
}