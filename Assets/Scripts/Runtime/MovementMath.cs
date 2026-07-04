using UnityEngine;

namespace OneMoreTime
{
    /// Saf, test edilebilir hareket matematiği. Runtime state tutmaz.
    public static class MovementMath
    {
        /// v = sqrt(2 * h * |g|)
        public static float JumpVelocity(float jumpHeight, float gravity)
            => Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));

        /// Slide başlangıç hızı: mevcut hızı koru (taban run hızı), sonra boost.
        public static float SlideStartSpeed(float currentSpeed, float runSpeed, float slideBoost)
            => Mathf.Max(currentSpeed, runSpeed) * slideBoost;

        /// Eğim boyunca aşağı ivme (m/s^2). Düz / yokuş-yukarı yönde 0.
        public static float SlopeAccel(Vector3 slopeNormal, Vector3 moveDir, float gravity)
        {
            Vector3 g = new Vector3(0f, gravity, 0f);
            Vector3 alongSlope = g - Vector3.Dot(g, slopeNormal) * slopeNormal;
            float a = Vector3.Dot(alongSlope, moveDir.normalized);
            return Mathf.Max(0f, a);
        }

        /// Zıplama: dikey hızı ata, yatay momentumu %100 koru (slide hop).
        public static Vector3 ApplyJump(Vector3 velocity, float jumpVelocity)
            => new Vector3(velocity.x, jumpVelocity, velocity.z);

        /// Hareket yönünü zemin düzlemine izdüşürür; yerde hareket eğimi takip eder.
        public static Vector3 ProjectOnGround(Vector3 moveDir, Vector3 groundNormal)
        {
            Vector3 projected = Vector3.ProjectOnPlane(moveDir, groundNormal);
            return projected.sqrMagnitude > 0.0001f ? projected.normalized : Vector3.zero;
        }
    }
}
