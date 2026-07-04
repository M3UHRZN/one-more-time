using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.3: oyuncunun jeton cüzdanı. İnce sarmalayıcı; mantık TokenWallet'ta.
    public class PlayerTokens : MonoBehaviour
    {
        readonly TokenWallet _wallet = new TokenWallet();

        public int Count => _wallet.Count;

        public void Add(int amount) => _wallet.Add(amount);

        public bool TrySpend() => _wallet.TrySpend();

        /// GDD §3.5: kayıpta jeton 1'e sıfırlanır (#11 çağıracak).
        public void ResetToDefault() => _wallet.ResetToDefault();
    }
}
