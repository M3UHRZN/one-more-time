namespace OneMoreTime
{
    /// GDD §3.2/§4.1: bitişte kilitlenen koşu özeti. Slot (#8) bunu tüketecek.
    public readonly struct RunResult
    {
        public readonly float Seconds;
        public readonly int Corpses;
        public readonly float RightChance;

        public RunResult(float seconds, int corpses, float rightChance)
        {
            Seconds = seconds;
            Corpses = corpses;
            RightChance = rightChance;
        }
    }
}
