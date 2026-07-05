using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime
{
    /// Cue -> binding eşleşmesi ve klip/pitch seçimi. Saf, test edilebilir;
    /// GameAudioPlayer bu sonuçları gerçek AudioSource çağrılarına çevirir.
    public static class AudioCuePlayback
    {
        public static AudioCueBinding FindBinding(IReadOnlyList<AudioCueBinding> bindings, AudioCueId cue)
        {
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].cue == cue) return bindings[i];

            return null;
        }

        public static AudioClip PickClip(AudioCueBinding binding, Func<float> random01)
        {
            if (binding.clips == null || binding.clips.Length == 0) return null;

            int index = Mathf.Min(binding.clips.Length - 1, Mathf.FloorToInt(random01() * binding.clips.Length));
            return binding.clips[index];
        }

        public static float PickPitch(AudioCueBinding binding, Func<float> random01)
        {
            if (binding.pitchRandomRange <= 0f) return 1f;

            float half = binding.pitchRandomRange * 0.5f;
            return 1f + Mathf.Lerp(-half, half, random01());
        }
    }
}
