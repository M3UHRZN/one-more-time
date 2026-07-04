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
        public float wallProbe = 0.15f;        // m, kapsül yüzeyinden duvar arama mesafesi
        public float wallSlideMaxFall = 3f;    // m/s, duvar kayarken düşüş hız tavanı
        public float wallJumpPush = 7f;        // m/s, duvardan itme

        [Header("Ground probe")]
        public float groundProbe = 0.15f;   // m (zemin yoklama payı)
    }
}
