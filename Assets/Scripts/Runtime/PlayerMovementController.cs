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
        static readonly Vector3[] WallProbeLocalDirections =
        {
            Vector3.forward, Vector3.right, Vector3.back, Vector3.left
        };
        /// Wall-run girişi yalnızca yan (right/left) temastan tetiklenir — ön/arka duvara
        /// koşarken aniden yana kaymayı önler. Ön/arka yine de genel duvar teması sayılır
        /// (wall-jump-off-any-wall, double-jump reset vb. için).
        static readonly bool[] WallProbeIsSide = { false, true, false, true };

        [SerializeField] InputActionAsset inputAsset;
        [SerializeField] MovementConfig config = new MovementConfig();
        [SerializeField] Transform cameraTransform;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] LayerMask wallMask = ~0;

        Rigidbody _rb;
        CapsuleCollider _capsule;
        InputActionMap _playerMap;
        InputAction _move, _jump, _crouch, _sprint;
        JumpGate _jumpGate;
        JumpGate _wallGate;
        DoubleJumpState _doubleJump;
        WallRunState _wallRun;
        Vector3 _lastHorizontalVelocity;
        Vector3 _wallNormal;
        bool _onWall;

        bool _grounded;
        bool _sliding;

        /// Efekt katmanı için: yatay hız (FOV, hız çizgileri eşiği ~10 m/s hook'u).
        public float HorizontalSpeed { get; private set; }
        public bool IsSliding => _sliding;
        public bool IsWallRunning => _wallRun != null && _wallRun.IsActive;
        public float WallRunSide
        {
            get
            {
                if (!IsWallRunning) return 0f;
                Vector3 right = cameraTransform ? cameraTransform.right : transform.right;
                return Mathf.Sign(Vector3.Dot(_wallRun.WallNormal, right));
            }
        }

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
            _sprint = _playerMap.FindAction("Sprint", true);
            _jumpGate = new JumpGate(config.coyoteTime, config.jumpBuffer);
            _wallGate = new JumpGate(config.coyoteTime, config.jumpBuffer);
            _doubleJump = new DoubleJumpState();
            _wallRun = new WallRunState(config.wallRunDuration, config.coyoteTime, config.sameWallCooldown);
        }

        void OnEnable() => _playerMap?.Enable();
        void OnDisable() => _playerMap?.Disable();

        void Update()
        {
            if (_jump.WasPressedThisFrame())
            {
                _jumpGate.PressJump();
                _wallGate.PressJump();
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _grounded = ProbeGround(out Vector3 groundNormal);
            _jumpGate.Tick(dt, _grounded);
            Vector3 probedWallNormal = Vector3.zero;
            bool hasWallContact = ProbeWall(out probedWallNormal, out bool hasBlockedWallContact,
                out bool hasEnterableWallContact);
            _onWall = !_grounded && hasWallContact;
            _doubleJump.ObserveContacts(_grounded, hasWallContact);
            if (_onWall) _wallNormal = probedWallNormal;
            bool wallJumpAvailableAtTickStart = _wallRun.IsActive && _wallGate.CanConsumeJump;
            _wallGate.Tick(dt, _onWall);
            bool wallRunOwnedTick = _wallRun.IsActive;
            bool wallJumpFromOwnedRun = wallRunOwnedTick
                && (wallJumpAvailableAtTickStart || _wallGate.CanConsumeJump);
            Vector3 ownedWallNormal = wallRunOwnedTick ? _wallRun.WallNormal : Vector3.zero;
            _wallRun.Tick(dt, _onWall, probedWallNormal, hasBlockedWallContact);
            bool wallRunExpiredThisTick = wallRunOwnedTick && !_wallRun.IsActive
                && _wallRun.Elapsed >= config.wallRunDuration;

            Vector2 mv = _move.ReadValue<Vector2>();
            Vector3 wish = CameraRelative(mv);

            Vector3 v = _rb.linearVelocity;
            // Zeminde iken gerçek hızı zemin düzleminden oku: sadece X/Z alırsak
            // eğimdeki dikey bileşeni "kaybederiz" ve hız her tick'te sönümlenir (momentum bug).
            Vector3 planarNow = _grounded ? Vector3.ProjectOnPlane(v, groundNormal) : new Vector3(v.x, 0f, v.z);
            float speed = planarNow.magnitude;
            Vector3 horiz = speed > 0.001f
                ? new Vector3(planarNow.x, 0f, planarNow.z).normalized * speed
                : Vector3.zero;
            bool crouchHeld = _crouch.IsPressed();
            bool sprintHeld = _sprint.IsPressed();

            bool nonJumpWallRunExitRequested = _wallRun.IsActive
                && (_grounded || crouchHeld
                    || MovementMath.ShouldDetachFromWall(wish, _wallRun.WallNormal, 0.25f));
            if (MovementJumpResolver.ShouldExitWallRunBeforeJump(
                    _wallRun.IsActive, wallJumpFromOwnedRun, nonJumpWallRunExitRequested))
                _wallRun.Exit(false);

            if (!wallJumpFromOwnedRun && !_wallRun.IsActive && !_grounded && hasEnterableWallContact
                && !crouchHeld
                && !MovementMath.ShouldDetachFromWall(wish, probedWallNormal, 0.25f)
                && _wallRun.CanEnter(probedWallNormal))
            {
                Vector3 entryVelocity = _lastHorizontalVelocity.sqrMagnitude > 0.0001f
                    ? _lastHorizontalVelocity
                    : horiz;
                float entrySpeed = Mathf.Max(entryVelocity.magnitude, horiz.magnitude);
                Vector3 fallbackRight = cameraTransform ? cameraTransform.right : transform.right;
                Vector3 direction = MovementMath.WallTangentDirection(
                    entryVelocity, wish, fallbackRight, probedWallNormal);
                _wallRun.TryEnter(probedWallNormal, direction, entrySpeed, v.y);
            }

            if (!_sliding && _grounded && crouchHeld && speed > config.runSpeed * 0.5f)
            {
                float boosted = MovementMath.SlideStartSpeed(speed, config.runSpeed, config.slideBoost);
                Vector3 headDir = planarNow.sqrMagnitude > 0.001f ? planarNow.normalized : transform.forward;
                v = headDir * boosted; // boost'lu başlangıç hızı, zemin düzleminde
                _sliding = true;
            }
            else if (_sliding && (!crouchHeld || !_grounded || speed < config.slideMinSpeed))
            {
                _sliding = false;
            }

            if (!_sliding && _grounded)
            {
                float targetSpeed = sprintHeld ? config.sprintSpeed : config.runSpeed;
                Vector3 target = wish * targetSpeed;
                float accel = wish.sqrMagnitude > 0.01f ? config.groundAccel : config.groundFriction;
                horiz = Vector3.MoveTowards(horiz, target, accel * dt);
            }

            Vector3 nextVel;
            if (wallJumpFromOwnedRun)
            {
                nextVel = MovementMath.WallRunVelocity(
                    _wallRun.Direction,
                    _wallRun.HorizontalSpeed,
                    _wallRun.EntryVerticalSpeed,
                    _wallRun.Elapsed,
                    config.wallRunDuration,
                    config.wallRunEndFallSpeed);
            }
            else if (_sliding && _grounded)
            {
                // Yerçekiminin eğim-boyu bileşeni vektör olarak eklenir: hız fall-line'a kıvrılır.
                nextVel = MovementMath.SlideVelocity(v, groundNormal, config.gravity, config.slideFriction, dt);
            }
            else if (_grounded)
            {
                // Zemin düzlemine izdüşür: hareket eğimi takip etsin, sekmesin.
                Vector3 groundDir = MovementMath.ProjectOnGround(horiz, groundNormal);
                nextVel = groundDir * horiz.magnitude;
            }
            else if (_wallRun.IsActive || wallRunExpiredThisTick)
            {
                nextVel = MovementMath.WallRunVelocity(
                    _wallRun.Direction,
                    _wallRun.HorizontalSpeed,
                    _wallRun.EntryVerticalSpeed,
                    _wallRun.Elapsed,
                    config.wallRunDuration,
                    config.wallRunEndFallSpeed);
            }
            else
            {
                Vector3 wishDir = wish.sqrMagnitude > 0.0001f ? wish.normalized : Vector3.zero;
                horiz = MovementMath.AirAccelerate(horiz, wishDir, wish.magnitude * config.runSpeed,
                    config.airAccel, config.airSpeedCap, dt);
                nextVel = new Vector3(horiz.x, v.y + config.gravity * dt, horiz.z);
            }

            bool wallJumpAvailable = wallJumpFromOwnedRun
                || ((_wallRun.IsActive || _wallRun.CanEnter(_wallNormal))
                    && _wallGate.CanConsumeJump);
            bool doubleJumpAvailable = !_grounded
                && _doubleJump.IsAvailable
                && _jumpGate.HasBufferedJump;
            MovementJumpSource jumpSource = MovementJumpResolver.Choose(
                _wallRun.IsActive || wallJumpFromOwnedRun,
                _jumpGate.CanConsumeJump,
                wallJumpAvailable,
                doubleJumpAvailable);

            if (jumpSource == MovementJumpSource.Ground && _jumpGate.TryConsumeJump())
            {
                _wallGate.CancelPendingJump();
                if (_wallRun.IsActive) _wallRun.Exit(false);
                nextVel = MovementMath.ApplyJump(nextVel, MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
                _sliding = false;
            }
            else if (jumpSource == MovementJumpSource.Wall
                && _wallGate.TryConsumeJump(wallJumpAvailableAtTickStart))
            {
                _jumpGate.CancelPendingJump();
                Vector3 jumpNormal = wallJumpFromOwnedRun
                    ? ownedWallNormal
                    : _wallRun.IsActive ? _wallRun.WallNormal : _wallNormal;
                if (_wallRun.IsActive)
                    _wallRun.Exit(true);
                else
                    _wallRun.LockWallAfterJump(jumpNormal);
                nextVel = MovementMath.WallJumpVelocity(nextVel, jumpNormal,
                    config.wallJumpPush, MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
                _sliding = false;
            }
            else if (jumpSource == MovementJumpSource.Double
                && _jumpGate.TryConsumeBufferedJump()
                && _doubleJump.TryConsume())
            {
                _wallGate.CancelPendingJump();
                nextVel = MovementMath.ApplyJump(nextVel,
                    MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
                _sliding = false;
            }

            _rb.linearVelocity = nextVel;
            _lastHorizontalVelocity = new Vector3(nextVel.x, 0f, nextVel.z);
            HorizontalSpeed = _lastHorizontalVelocity.magnitude;
        }

        Vector3 CameraRelative(Vector2 mv)
        {
            Vector3 f = cameraTransform ? cameraTransform.forward : Vector3.forward;
            Vector3 r = cameraTransform ? cameraTransform.right : Vector3.right;
            f.y = 0f; r.y = 0f;
            Vector3 wish = f.normalized * mv.y + r.normalized * mv.x;
            return Vector3.ClampMagnitude(wish, 1f);
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

        void OnDrawGizmosSelected()
        {
            CapsuleCollider capsule = _capsule ? _capsule : GetComponent<CapsuleCollider>();
            if (!capsule) return;

            Vector3 center = transform.position + capsule.center;

            float groundRadius = capsule.radius * 0.9f;
            float groundDist = (capsule.height * 0.5f) - capsule.radius + config.groundProbe;
            Gizmos.color = _grounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(center, groundRadius);
            Gizmos.DrawWireSphere(center + Vector3.down * groundDist, groundRadius);

            float wallDist = capsule.radius + config.wallProbe;
            Gizmos.color = _onWall ? Color.cyan : Color.yellow;
            for (int i = 0; i < WallProbeLocalDirections.Length; i++)
            {
                Vector3 direction = transform.TransformDirection(WallProbeLocalDirections[i]);
                Gizmos.DrawRay(center, direction * wallDist);
            }
        }

        bool ProbeWall(out Vector3 normal, out bool hasBlockedWallContact, out bool hasEnterableWallContact)
        {
            Vector3 center = transform.position + _capsule.center;
            float dist = _capsule.radius + config.wallProbe;
            Vector3 firstNormal = Vector3.zero;
            Vector3 activeNormal = Vector3.zero;
            Vector3 enterableNormal = Vector3.zero;
            hasBlockedWallContact = false;

            for (int i = 0; i < WallProbeLocalDirections.Length; i++)
            {
                Vector3 direction = transform.TransformDirection(WallProbeLocalDirections[i]);
                if (!Physics.Raycast(center, direction, out var hit, dist,
                        wallMask, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(hit.normal.y) >= 0.3f)
                    continue;

                Vector3 hitNormal = hit.normal;
                if (firstNormal == Vector3.zero) firstNormal = hitNormal;
                if (_wallRun.MatchesBlockedWall(hitNormal)) hasBlockedWallContact = true;

                if (_wallRun.IsActive)
                {
                    if (activeNormal == Vector3.zero && _wallRun.MatchesActiveWall(hitNormal))
                        activeNormal = hitNormal;
                }
                else if (enterableNormal == Vector3.zero && WallProbeIsSide[i] && _wallRun.CanEnter(hitNormal))
                {
                    enterableNormal = hitNormal;
                }
            }

            normal = _wallRun.IsActive && activeNormal != Vector3.zero
                ? activeNormal
                : !_wallRun.IsActive && enterableNormal != Vector3.zero
                    ? enterableNormal
                    : firstNormal;
            hasEnterableWallContact = enterableNormal != Vector3.zero;
            return firstNormal != Vector3.zero;
        }
    }
}
