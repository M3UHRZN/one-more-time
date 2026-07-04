using UnityEngine;

namespace OneMoreTime
{
    /// Wall-run yaşam döngüsü. Fizik/input okumaz; gelecekteki movement state machine'e taşınabilir.
    public sealed class WallRunState
    {
        const float SameWallDot = 0.9f;

        readonly float _duration;
        readonly float _contactGrace;
        readonly float _sameWallCooldown;

        float _contactGraceRemaining;
        float _sameWallCooldownRemaining;
        bool _requiresSeparation;
        Vector3 _blockedWallNormal;
        Vector3 _cooldownWallNormal;

        public bool IsActive { get; private set; }
        public float Elapsed { get; private set; }
        public Vector3 WallNormal { get; private set; }
        public Vector3 Direction { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public float EntryVerticalSpeed { get; private set; }

        public WallRunState(float duration, float contactGrace, float sameWallCooldown)
        {
            _duration = duration;
            _contactGrace = contactGrace;
            _sameWallCooldown = sameWallCooldown;
        }

        public bool CanEnter(Vector3 wallNormal)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            if (n == Vector3.zero) return false;
            if (_requiresSeparation && Vector3.Dot(n, _blockedWallNormal) >= SameWallDot) return false;
            return _sameWallCooldownRemaining <= 0f
                || Vector3.Dot(n, _cooldownWallNormal) < SameWallDot;
        }

        public bool TryEnter(Vector3 wallNormal, Vector3 direction, float horizontalSpeed, float entryVerticalSpeed)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            Vector3 tangent = new Vector3(direction.x, 0f, direction.z).normalized;
            if (IsActive || !CanEnter(n) || tangent == Vector3.zero || horizontalSpeed <= 0.001f)
                return false;

            IsActive = true;
            Elapsed = 0f;
            WallNormal = n;
            Direction = tangent;
            HorizontalSpeed = horizontalSpeed;
            EntryVerticalSpeed = Mathf.Max(0f, entryVerticalSpeed);
            _contactGraceRemaining = _contactGrace;
            return true;
        }

        public void Tick(float dt, bool hasWallContact, Vector3 contactNormal)
        {
            _sameWallCooldownRemaining = Mathf.Max(0f, _sameWallCooldownRemaining - dt);
            Vector3 contact = HorizontalNormal(contactNormal);

            if (_requiresSeparation
                && (!hasWallContact || contact == Vector3.zero
                    || Vector3.Dot(contact, _blockedWallNormal) < SameWallDot))
            {
                _requiresSeparation = false;
            }

            if (!IsActive) return;

            Elapsed += dt;
            bool sameContact = hasWallContact && contact != Vector3.zero
                && Vector3.Dot(contact, WallNormal) >= SameWallDot;
            _contactGraceRemaining = sameContact
                ? _contactGrace
                : Mathf.Max(0f, _contactGraceRemaining - dt);

            if (Elapsed >= _duration || _contactGraceRemaining <= 0f)
                Exit(false);
        }

        public void Exit(bool wallJump)
        {
            if (!IsActive) return;
            Vector3 exitedWall = WallNormal;
            IsActive = false;
            BlockUntilSeparation(exitedWall);
            if (wallJump) StartCooldown(exitedWall);
        }

        public void LockWallAfterJump(Vector3 wallNormal)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            if (n == Vector3.zero) return;
            BlockUntilSeparation(n);
            StartCooldown(n);
        }

        void BlockUntilSeparation(Vector3 wallNormal)
        {
            _requiresSeparation = true;
            _blockedWallNormal = wallNormal;
        }

        void StartCooldown(Vector3 wallNormal)
        {
            _cooldownWallNormal = wallNormal;
            _sameWallCooldownRemaining = _sameWallCooldown;
        }

        static Vector3 HorizontalNormal(Vector3 normal)
        {
            Vector3 n = new Vector3(normal.x, 0f, normal.z);
            return n.sqrMagnitude > 0.0001f ? n.normalized : Vector3.zero;
        }
    }
}
