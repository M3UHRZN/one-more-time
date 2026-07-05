using UnityEngine;

namespace OneMoreTime
{
    /// PlayerMovementController durumunu Animator parametrelerine aktarır. İnce köprü;
    /// karar mantığı yok, yalnızca okuma + Animator.Set*.
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] PlayerMovementController movement;

        static readonly int SpeedParam = Animator.StringToHash("Speed");
        static readonly int GroundedParam = Animator.StringToHash("Grounded");
        static readonly int SlidingParam = Animator.StringToHash("Sliding");
        static readonly int WallRunningParam = Animator.StringToHash("WallRunning");
        static readonly int WallRunSideParam = Animator.StringToHash("WallRunSide");
        static readonly int JumpParam = Animator.StringToHash("Jump");
        static readonly int WallJumpParam = Animator.StringToHash("WallJump");
        static readonly int DoubleJumpParam = Animator.StringToHash("DoubleJump");
        static readonly int LandParam = Animator.StringToHash("Land");

        Animator _animator;

        void Awake() => _animator = GetComponent<Animator>();

        void OnEnable()
        {
            movement.Landed += HandleLanded;
            movement.Jumped += HandleJumped;
        }

        void OnDisable()
        {
            movement.Landed -= HandleLanded;
            movement.Jumped -= HandleJumped;
        }

        void Update()
        {
            _animator.SetFloat(SpeedParam, movement.HorizontalSpeed);
            _animator.SetBool(GroundedParam, movement.IsGrounded);
            _animator.SetBool(SlidingParam, movement.IsSliding);
            _animator.SetBool(WallRunningParam, movement.IsWallRunning);
            _animator.SetFloat(WallRunSideParam, movement.WallRunSide);
        }

        void HandleLanded() => _animator.SetTrigger(LandParam);

        void HandleJumped(MovementJumpSource source)
        {
            switch (source)
            {
                case MovementJumpSource.Ground: _animator.SetTrigger(JumpParam); break;
                case MovementJumpSource.Wall: _animator.SetTrigger(WallJumpParam); break;
                case MovementJumpSource.Double: _animator.SetTrigger(DoubleJumpParam); break;
            }
        }
    }
}
