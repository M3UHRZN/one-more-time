using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.1: diken/tuzak/void kill-floor. Trigger collider'a eklenir.
    public class Hazard : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            other.GetComponentInParent<PlayerRespawner>()?.Kill();
        }
    }
}
