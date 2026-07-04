using System;
using UnityEngine;

namespace OneMoreTime
{
    /// GDD §4: pRIGHT eğrisinin ayarlanabilir katsayıları. Playtest ile ayarlanır
    /// (bkz. MovementConfig.cs pattern'i) — kod değişikliği gerekmeden Inspector'dan.
    [Serializable]
    public class RunQualityConfig
    {
        [Header("pRIGHT eğrisi (Par Time üstü her saniye/ceset başına ceza)")]
        public float timePenaltyPerSecondOverPar = 1f;  // Par'ın üstündeki her saniye
        public float corpsePenalty = 5f;                // ceset başına
        public float floor = 8f;
        public float cap = 60f;
    }
}
