namespace OneMoreTime
{
    /// GDD §3.4: sonuç tablosu mantığı — makara eşleştirme DEĞİL. Önce sonuç çekilir,
    /// makaralar sonra buna animasyonla getirilir (#9). Saf, test edilebilir.
    public static class SlotDraw
    {
        /// roll01 ∈ [0,1). Right/One/Not sırasıyla kümülatif aralıklara bölünür.
        public static SlotOutcome Pick(SlotOdds odds, float roll01)
        {
            float x = roll01 * 100f;
            if (x < odds.Right) return SlotOutcome.RightOnTime;
            if (x < odds.Right + odds.One) return SlotOutcome.OneMoreTime;
            return SlotOutcome.NotThisTime;
        }
    }
}
