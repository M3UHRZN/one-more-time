namespace OneMoreTime
{
    /// GDD §3.3: jeton cüzdanı. Varsayılan 1 jeton, yalnızca kayıpta sıfırlanır (#11).
    /// Saf, test edilebilir.
    public class TokenWallet
    {
        public int Count { get; private set; } = 1;

        public void Add(int amount) => Count += amount;

        public bool TrySpend()
        {
            if (Count <= 0) return false;
            Count--;
            return true;
        }

        public void ResetToDefault() => Count = 1;
    }
}
