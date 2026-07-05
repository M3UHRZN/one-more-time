using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime
{
    /// Sahnede tek örnek: GameAudioEvents'i dinler, cue'yu Inspector'da atanmış klibe çevirir.
    /// Eksik binding veya klip oynatmayı sessizce atlar — ses hatası gameplay'i asla durdurmaz.
    [ExecuteAlways]
    public class GameAudioPlayer : MonoBehaviour
    {
        [SerializeField] List<AudioCueBinding> bindings = new List<AudioCueBinding>();

        void OnEnable() => GameAudioEvents.CueRaised += HandleCueRaised;
        void OnDisable() => GameAudioEvents.CueRaised -= HandleCueRaised;

        void OnValidate()
        {
            var seen = new HashSet<AudioCueId>();
            foreach (AudioCueBinding binding in bindings)
            {
                if (binding == null) continue;
                if (!seen.Add(binding.cue))
                    Debug.LogWarning($"GameAudioPlayer: duplicate binding for cue {binding.cue}; only the first is used.", this);
            }
        }

        void HandleCueRaised(AudioCuePayload payload)
        {
            AudioCueBinding binding = AudioCuePlayback.FindBinding(bindings, payload.Cue);
            if (binding == null)
            {
                Debug.LogWarning($"GameAudioPlayer: no binding for cue {payload.Cue}.");
                return;
            }

            AudioClip clip = AudioCuePlayback.PickClip(binding, () => Random.value);
            if (clip == null)
            {
                Debug.LogWarning($"GameAudioPlayer: binding for cue {payload.Cue} has no clips.");
                return;
            }

            float pitch = AudioCuePlayback.PickPitch(binding, () => Random.value);

            if (binding.useWorldPosition)
            {
                if (!payload.HasPosition)
                {
                    Debug.LogWarning($"GameAudioPlayer: cue {payload.Cue} needs a world position but none was raised.");
                    return;
                }

                PlayAtPoint(clip, payload.Position, binding.volume, pitch);
            }
            else
            {
                AudioSource activeSource = binding.source != null ? binding.source : GetComponent<AudioSource>();
                if (activeSource == null)
                {
                    Debug.LogWarning($"GameAudioPlayer: cue {payload.Cue} is set to 2D but no AudioSource is available on the player or binding.");
                    return;
                }
                activeSource.pitch = pitch;
                activeSource.PlayOneShot(clip, binding.volume);
            }
        }

        static void PlayAtPoint(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            var temp = new GameObject("OneShotAudio");
            temp.transform.position = position;

            AudioSource source = temp.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.Play();

            Destroy(temp, clip.length / Mathf.Max(0.01f, pitch));
        }
    }
}
