using UnityEngine;

namespace OneMoreTime
{
    /// GDD §2: bitiş çizgisi trigger'ı. Oyuncu geçince koşuyu sonlandırır.
    public class FinishLine : MonoBehaviour
    {
        [SerializeField] RunController run;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerRespawner>())
                run.Finish();
        }
    }
}
