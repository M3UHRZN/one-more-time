using UnityEngine;

namespace OneMoreTime
{
    /// Animasyon event'lerinden çağrılan köprü: sesi kendisi çalmaz, seçili cue'yu
    /// emitter'ın konumundan merkezi GameAudioEvents hattına yeniden yollar.
    public class AnimationAudioCueEmitter : MonoBehaviour
    {
        [SerializeField] AudioCueId cue;

        /// Animation Event: parametresiz, Inspector'da ayarlı cue'yu oynatır.
        public void RaiseConfiguredCue() => GameAudioEvents.RaiseCue(cue, transform.position);

        /// Animation Event: int parametreli — birden fazla cue arasından event'in int alanıyla seçim yapan klipler için.
        public void RaiseCueById(int cueId) => GameAudioEvents.RaiseCue((AudioCueId)cueId, transform.position);
    }
}
