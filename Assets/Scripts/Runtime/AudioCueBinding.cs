using System;
using UnityEngine;

namespace OneMoreTime
{
    /// Bir AudioCueId'nin Inspector'da nasıl çalınacağını taşıyan saf veri.
    [Serializable]
    public class AudioCueBinding
    {
        public AudioCueId cue;
        public AudioClip[] clips;
        public AudioSource source;
        public bool useWorldPosition;
        public float volume = 1f;
        public float pitchRandomRange = 0f;
    }
}
