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

        public bool TryConsumeJump()
        {
            if (_bufferTimer > 0f && _coyoteTimer > 0f)
            {
                _bufferTimer = 0f;
                _coyoteTimer = 0f;
                return true;
            }
            return false;
        }
    }
}
