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

        [Header("Facing / Ground probe")]
        public float turnSpeed = 720f;      // deg/s (TPS yönelme)
        public float groundProbe = 0.15f;   // m (zemin yoklama payı)
    }
}
