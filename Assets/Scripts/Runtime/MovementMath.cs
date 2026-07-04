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

        /// Quake air strafe: istek yönündeki hız izdüşümü cap'in altındaysa fark kadar hız ekler.
        /// Dik strafe'te izdüşüm ~0 olduğundan dönerek sürekli hız kazanılır (momentum tutkalı).
        public static Vector3 AirAccelerate(Vector3 horizVel, Vector3 wishDir, float wishSpeed, float airAccel, float airSpeedCap, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f) return horizVel;
            float current = Vector3.Dot(horizVel, wishDir);
            float add = Mathf.Min(wishSpeed, airSpeedCap) - current;
            if (add <= 0f) return horizVel;
            float accel = Mathf.Min(airAccel * wishSpeed * dt, add);
            return horizVel + wishDir * accel;
        }

        /// Duvar boyunca kararlı yön: önce momentum, sonra input, son çare kamera sağı.
        public static Vector3 WallTangentDirection(Vector3 horizontalVelocity, Vector3 wishDir,
            Vector3 cameraRight, Vector3 wallNormal)
        {
            Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            if (n == Vector3.zero) return Vector3.zero;

            Vector3 source = Vector3.ProjectOnPlane(
                new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z), n);
            if (source.sqrMagnitude < 0.0001f)
                source = Vector3.ProjectOnPlane(new Vector3(wishDir.x, 0f, wishDir.z), n);
            if (source.sqrMagnitude < 0.0001f)
                source = Vector3.ProjectOnPlane(new Vector3(cameraRight.x, 0f, cameraRight.z), n);
            return source.sqrMagnitude > 0.0001f ? source.normalized : Vector3.zero;
        }

        /// Korunan yatay hız + iki saniye içinde nazik düşüşe geçen dikey hız.
        public static Vector3 WallRunVelocity(Vector3 tangentDirection, float horizontalSpeed,
            float entryVerticalSpeed, float elapsed, float duration, float endFallSpeed)
        {
            Vector3 tangent = new Vector3(tangentDirection.x, 0f, tangentDirection.z).normalized;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float vertical = Mathf.Lerp(Mathf.Max(0f, entryVerticalSpeed), -Mathf.Abs(endFallSpeed), t);
            Vector3 horizontal = tangent * horizontalSpeed;
            return new Vector3(horizontal.x, vertical, horizontal.z);
        }

        public static bool ShouldDetachFromWall(Vector3 wishDir, Vector3 wallNormal, float threshold)
        {
            if (wishDir.sqrMagnitude < 0.0001f) return false;
            Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            return n != Vector3.zero && Vector3.Dot(wishDir.normalized, n) > threshold;
        }

        /// Duvar zıplaması: duvara paralel yatay momentum korunur, duvara dik bileşen
        /// itme hızıyla değiştirilir, dikey hız zıplama hızına ayarlanır.
        public static Vector3 WallJumpVelocity(Vector3 velocity, Vector3 wallNormal, float pushSpeed, float jumpVelocity)
        {
            Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 tangent = Vector3.ProjectOnPlane(horiz, n);
            Vector3 result = tangent + n * pushSpeed;
            return new Vector3(result.x, jumpVelocity, result.z);
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
