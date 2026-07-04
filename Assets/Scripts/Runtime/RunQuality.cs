using UnityEngine;

namespace OneMoreTime
{
    /// GDD §4.1: koşu kalitesinden RIGHT ON TIME olasılığı. Saf, test edilebilir.
    public static class RunQuality
    {
        const float Base = 60f;
        const float TimePenalty = 0.35f;
        const float CorpsePenalty = 2.5f;
        const float Floor = 8f;
        const float Cap = 60f;

        /// pRIGHT = clamp(60 − süre×0.35 − ceset×2.5, 8, 60)
        public static float RightOnTimeChance(float seconds, int corpses)
        {
            float value = Base - seconds * TimePenalty - corpses * CorpsePenalty;
            return Mathf.Clamp(value, Floor, Cap);
        }
    }
}
