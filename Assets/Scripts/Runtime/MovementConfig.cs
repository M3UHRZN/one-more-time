using System;
using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.6 başlangıç his parametreleri. Playtest ile ayarlanır.
    [Serializable]
    public class MovementConfig
    {
        [Header("Ground")]
        public float runSpeed = 7f;         // m/s
        public float sprintSpeed = 10f;     // m/s, faster gait — stamina system deferred
        public float groundAccel = 60f;     // m/s^2 (snappy)
        public float groundFriction = 40f;  // m/s^2

        [Header("Jump / Gravity")]
        public float jumpHeight = 1.4f;     // m
        public float gravity = -25f;        // m/s^2 (tok düşüş)
        public float coyoteTime = 0.1f;     // s
        public float jumpBuffer = 0.15f;    // s

        [Header("Slide")]
        public float slideBoost = 1.4f;     // +%40 anlık
        public float slideMinSpeed = 3f;    // altına düşünce slide biter
        public float slideFriction = 8f;    // m/s^2 düz zemin slide sönümü

        [Header("Air")]
        public float airAccel = 15f;     // hava ivme çarpanı (Quake sv_airaccelerate dengi)
        public float airSpeedCap = 1f;   // m/s, istek yönü izdüşüm tavanı (strafe kazancı sınırı)

        [Header("Wall")]
        public float wallProbe = 0.15f;          // m, kapsül yüzeyinden duvar arama mesafesi
        public float wallRunDuration = 2f;       // s, tek temasın azami wall-run süresi
        public float sameWallCooldown = 0.2f;    // s, wall-jump sonrası aynı duvara dönüş kilidi
        public float wallRunEndFallSpeed = 3f;   // m/s, sürenin sonunda nazik düşüş
        public float wallJumpPush = 7f;          // m/s, duvardan dışa itme

        [Header("Ground probe")]
        public float groundProbe = 0.15f;   // m (zemin yoklama payı)

        [Header("Step-up")]
        public float stepHeight = 0.3f;        // m, aşılabilir engel üst yüksekliği (ayak hizasından)
        public float stepForwardProbe = 0.1f;  // m, kapsül yarıçapı ötesinde ileri arama payı
        public float stepClimbSpeed = 4f;      // m/s, yumuşak tırmanış dikey hızı
        public float stepMinSpeed = 1.5f;      // m/s, tetikleme için asgari yatay hız
    }
}
