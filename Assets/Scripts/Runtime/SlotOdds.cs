using UnityEngine;

namespace OneMoreTime
{
    /// GDD §4.1: çevirme başına olasılık üçlüsü. Saf, test edilebilir.
    public readonly struct SlotOdds
    {
        public readonly float Right;
        public readonly float One;
        public readonly float Not;

        SlotOdds(float right, float one, float not)
        {
            Right = right;
            One = one;
            Not = not;
        }

        /// pRIGHT koşu kalitesine kilitlidir; RunQuality tarafında (RunQualityConfig ile)
        /// zaten sınırlandığı için burada olduğu gibi kabul edilir.
        /// pNOT = clamp(5 + (çevirmeNo−1)×4, 5, 25). pONE kalan olasılıktır.
        public static SlotOdds From(float pRight, int spinNumber)
        {
            float right = pRight;
            float not = Mathf.Clamp(5f + (spinNumber - 1) * 4f, 5f, 25f);
            float one = Mathf.Max(0f, 100f - right - not);
            return new SlotOdds(right, one, not);
        }
    }
}
