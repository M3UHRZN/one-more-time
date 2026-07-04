using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.6: Rigidbody koşu + zıplama (#2) + slide + momentum korunumu + slide hop (#3).
    /// Air strafe = issue #4. Kamera cilası = issue #5.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputAsset;
        [SerializeField] MovementConfig config = new MovementConfig();
        [SerializeField] Transform cameraTransform;
        [SerializeField] LayerMask groundMask = ~0;

        Rigidbody _rb;
        CapsuleCollider _capsule;
        InputActionMap _playerMap;
        InputAction _move, _jump, _crouch, _sprint;
        JumpGate _jumpGate;

        bool _grounded;
        bool _sliding;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _playerMap = inputAsset.FindActionMap("Player", true);
            _move = _playerMap.FindAction("Move", true);
            _jump = _playerMap.FindAction("Jump", true);
            _crouch = _playerMap.FindAction("Crouch", true);
            _sprint = _playerMap.FindAction("Sprint", false);
            _jumpGate = new JumpGate(config.coyoteTime, config.jumpBuffer);
        }

        void OnEnable() => _playerMap?.Enable();
        void OnDisable() => _playerMap?.Disable();

        void Update()
        {
            if (_jump.WasPressedThisFrame())
                _jumpGate.PressJump();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _grounded = ProbeGround(out Vector3 groundNormal);
            _jumpGate.Tick(dt, _grounded);

            Vector2 mv = _move.ReadValue<Vector2>();
            Vector3 wish = CameraRelative(mv);

            Vector3 v = _rb.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            float speed = horiz.magnitude;
            bool crouchHeld = _crouch.IsPressed();
            bool sprintHeld = _sprint != null && _sprint.IsPressed();

            if (!_sliding && _grounded && crouchHeld && speed > config.runSpeed * 0.5f)
            {
                float boosted = MovementMath.SlideStartSpeed(speed, config.runSpeed, config.slideBoost);
                Vector3 dir0 = horiz.sqrMagnitude > 0.001f ? horiz.normalized : transform.forward;
                horiz = dir0 * boosted;
                _sliding = true;
            }
            else if (_sliding && (!crouchHeld || !_grounded || speed < config.slideMinSpeed))
            {
                _sliding = false;
            }

            if (_sliding)
            {
                Vector3 dir = horiz.sqrMagnitude > 0.001f ? horiz.normalized : transform.forward;
                float slopeA = MovementMath.SlopeAccel(groundNormal, dir, config.gravity);
                float newSpeed = horiz.magnitude + slopeA * dt - config.slideFriction * dt;
                horiz = dir * Mathf.Max(0f, newSpeed);
            }
            else if (_grounded)
            {
                float targetSpeed = sprintHeld ? config.sprintSpeed : config.runSpeed;
                Vector3 target = wish * targetSpeed;
                float accel = wish.sqrMagnitude > 0.01f ? config.groundAccel : config.groundFriction;
                horiz = Vector3.MoveTowards(horiz, target, accel * dt);
            }

            float vy = v.y;
            if (_grounded && vy < 0f) vy = -2f;

            Vector3 nextVel = new Vector3(horiz.x, vy, horiz.z);
            if (_jumpGate.TryConsumeJump())
            {
                nextVel = MovementMath.ApplyJump(nextVel, MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
                _sliding = false;
            }
            else
            {
                nextVel.y += config.gravity * dt;
            }

            _rb.linearVelocity = nextVel;

            if (!_sliding) FaceMoveDirection(wish, dt);
        }

        Vector3 CameraRelative(Vector2 mv)
        {
            Vector3 f = cameraTransform ? cameraTransform.forward : Vector3.forward;
            Vector3 r = cameraTransform ? cameraTransform.right : Vector3.right;
            f.y = 0f; r.y = 0f;
            Vector3 wish = f.normalized * mv.y + r.normalized * mv.x;
            return Vector3.ClampMagnitude(wish, 1f);
        }

        void FaceMoveDirection(Vector3 wish, float dt)
        {
            if (wish.sqrMagnitude < 0.01f) return;
            Quaternion target = Quaternion.LookRotation(wish, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, target, config.turnSpeed * dt));
        }

        bool ProbeGround(out Vector3 normal)
        {
            float r = _capsule.radius * 0.9f;
            Vector3 center = transform.position + _capsule.center;
            float castDist = (_capsule.height * 0.5f) - _capsule.radius + config.groundProbe;
            if (Physics.SphereCast(center, r, Vector3.down, out var hit, castDist,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return true;
            }
            normal = Vector3.up;
            return false;
        }
    }
}
