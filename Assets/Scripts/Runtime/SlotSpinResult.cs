namespace OneMoreTime
{
    /// Tek bir çevirmenin sonucu ve o çevirmede kullanılan odds.
    public readonly struct SlotSpinResult
    {
        public readonly SlotOutcome Outcome;
        public readonly SlotOdds Odds;
        public readonly int SpinNumber;

        public SlotSpinResult(SlotOutcome outcome, SlotOdds odds, int spinNumber)
        {
            Outcome = outcome;
            Odds = odds;
            SpinNumber = spinNumber;
        }
    }
}
