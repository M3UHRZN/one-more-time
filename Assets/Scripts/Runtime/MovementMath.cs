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

        /// Kayak fiziği: mevcut hızı eğim düzlemine izdüşür, yerçekiminin eğim-boyu
        /// bileşenini VEKTÖR olarak ekler (hız fall-line'a kıvrılır), sonra sürtünme uygular.
        /// Düz zeminde gAlong = 0 → yalnız sürtünme; yön korunur.
        public static Vector3 SlideVelocity(Vector3 velocity, Vector3 groundNormal, float gravity, float friction, float dt)
        {
            Vector3 planar = Vector3.ProjectOnPlane(velocity, groundNormal);
            Vector3 gAlong = Vector3.ProjectOnPlane(new Vector3(0f, gravity, 0f), groundNormal);
            Vector3 vNew = planar + gAlong * dt;
            float sp = vNew.magnitude;
            if (sp < 0.0001f) return Vector3.zero;
            return vNew.normalized * Mathf.Max(0f, sp - friction * dt);
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
