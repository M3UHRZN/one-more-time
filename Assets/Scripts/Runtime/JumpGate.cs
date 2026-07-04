using UnityEngine;

namespace OneMoreTime
{
    /// Coyote time + jump buffer kararını kapsüller. Saf, test edilebilir.
    /// PressJump edge-triggered çağrılmalı (bkz. controller WasPressedThisFrame).
    public class JumpGate
    {
        readonly float _coyoteTime;
        readonly float _bufferTime;
        float _coyoteTimer;
        float _bufferTimer;

        public JumpGate(float coyoteTime, float bufferTime)
        {
            _coyoteTime = coyoteTime;
            _bufferTime = bufferTime;
        }

        public void Tick(float dt, bool isGrounded)
        {
            _coyoteTimer = isGrounded ? _coyoteTime : Mathf.Max(0f, _coyoteTimer - dt);
            _bufferTimer = Mathf.Max(0f, _bufferTimer - dt);
        }

        public void PressJump() => _bufferTimer = _bufferTime;

        public void CancelPendingJump() => _bufferTimer = 0f;

        public bool HasBufferedJump => _bufferTimer > 0f;
        public bool CanConsumeJump => HasBufferedJump && _coyoteTimer > 0f;

        public bool TryConsumeJump() => TryConsumeJump(false);

        public bool TryConsumeJump(bool wasAvailableAtTickStart)
        {
            if (wasAvailableAtTickStart || CanConsumeJump)
            {
                _bufferTimer = 0f;
                _coyoteTimer = 0f;
                return true;
            }
            return false;
        }

        public bool TryConsumeBufferedJump()
        {
            if (!HasBufferedJump) return false;
            _bufferTimer = 0f;
            return true;
        }
    }
}
