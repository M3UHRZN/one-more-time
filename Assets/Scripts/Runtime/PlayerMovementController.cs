using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.6 çekirdek: Rigidbody tabanlı koşu + zıplama (coyote/buffer/tok yerçekimi).
    /// Slide = issue #3 (Task 5). Air strafe = issue #4. Kamera cilası = issue #5.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputAsset;
        [SerializeField] MovementConfig config = new MovementConfig();
        [SerializeField] Transform cameraTransform;   // kamera-göreli hareket için
        [SerializeField] LayerMask groundMask = ~0;

        Rigidbody _rb;
        CapsuleCollider _capsule;
        InputActionMap _playerMap;
        InputAction _move, _jump;
        JumpGate _jumpGate;

        bool _grounded;

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
            _grounded = ProbeGround(out _);
            _jumpGate.Tick(dt, _grounded);

            Vector2 mv = _move.ReadValue<Vector2>();
            Vector3 wish = CameraRelative(mv);

            Vector3 v = _rb.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);

            if (_grounded)
            {
                Vector3 target = wish * config.runSpeed;
                float accel = wish.sqrMagnitude > 0.01f ? config.groundAccel : config.groundFriction;
                horiz = Vector3.MoveTowards(horiz, target, accel * dt);
            }
            // havada: yatay momentum korunur (air strafe = #4)

            float vy = v.y;
            if (_grounded && vy < 0f) vy = -2f;   // zemine yapış

            if (_jumpGate.TryConsumeJump())
                vy = MovementMath.JumpVelocity(config.jumpHeight, config.gravity);
            else
                vy += config.gravity * dt;

            _rb.linearVelocity = new Vector3(horiz.x, vy, horiz.z);

            FaceMoveDirection(wish, dt);
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

        /// Ayak altında zemin var mı; varsa normalini döndürür.
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
