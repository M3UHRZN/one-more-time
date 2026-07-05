using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.3: riskli/zor noktalara yerleştirilen jeton toplama objesi.
    public class TokenPickup : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            PlayerTokens tokens = other.GetComponentInParent<PlayerTokens>();
            if (!tokens) return;

            tokens.Add(1);
            GameAudioEvents.RaiseCoinPickup(transform.position);
            Destroy(gameObject);
        }
    }
}
