using UnityEngine;

namespace OneMoreTime
{
    /// GDD §4.1: koşu kalitesinden RIGHT ON TIME olasılığı. Saf, test edilebilir.
    /// Par Time'a göre normalize edilir: par'ın altındaki koşular ceza almaz,
    /// yalnızca par'ı AŞAN süre cezalandırılır (bkz. RunQualityConfig).
    public static class RunQuality
    {
        public static float RightOnTimeChance(float seconds, int corpses, float parTime, RunQualityConfig config)
        {
            float overPar = Mathf.Max(0f, seconds - parTime);
            float value = config.cap - overPar * config.timePenaltyPerSecondOverPar - corpses * config.corpsePenalty;
            return Mathf.Clamp(value, config.floor, config.cap);
        }
    }
}
