using System;
using UnityEngine;

namespace OneMoreTime
{
    /// Bir ses ipucunun ne zaman, hangi (opsiyonel) dünya konumunda tetiklendiğini taşır.
    public readonly struct AudioCuePayload
    {
        public readonly AudioCueId Cue;
        public readonly Vector3 Position;
        public readonly bool HasPosition;

        public AudioCuePayload(AudioCueId cue, Vector3 position, bool hasPosition)
        {
            Cue = cue;
            Position = position;
            HasPosition = hasPosition;
        }
    }

    /// Gameplay/animasyonun "bu oldu, sesi çal" demesini sağlayan tek merkezi olay hub'ı.
    /// Çağıranlar AudioClip/AudioSource bilmez; GameAudioPlayer dinleyip çalar.
    public static class GameAudioEvents
    {
        public static event Action<AudioCuePayload> CueRaised;

        public static void RaiseCoinPickup(Vector3 position) => RaiseCue(AudioCueId.CoinPickup, position);
        public static void RaisePlayerDied(Vector3 position) => RaiseCue(AudioCueId.PlayerDied, position);
        public static void RaiseRunFinished() => RaiseCue(AudioCueId.RunFinished);
        public static void RaiseSlotInteractionStarted(Vector3 position) => RaiseCue(AudioCueId.SlotInteractionStarted, position);
        public static void RaiseSlotLoseScreenShown() => RaiseCue(AudioCueId.SlotLoseScreenShown);
        public static void RaiseLossContinueConfirmed() => RaiseCue(AudioCueId.LossContinueConfirmed);
        public static void RaiseSceneTransitionStarted() => RaiseCue(AudioCueId.SceneTransitionStarted);
        public static void RaiseSlotLeverPulled(Vector3 position) => RaiseCue(AudioCueId.SlotLeverPulled, position);
        public static void RaiseSlotShuffle(Vector3 position) => RaiseCue(AudioCueId.SlotShuffle, position);

        public static void RaiseSlotOutcome(SlotOutcome outcome, Vector3 position) => RaiseCue(CueForOutcome(outcome), position);

        /// Animasyon köprüsü gibi çağıranların herhangi bir cue'yu doğrudan tetiklemesi için.
        public static void RaiseCue(AudioCueId cue) => CueRaised?.Invoke(new AudioCuePayload(cue, Vector3.zero, false));
        public static void RaiseCue(AudioCueId cue, Vector3 position) => CueRaised?.Invoke(new AudioCuePayload(cue, position, true));

        static AudioCueId CueForOutcome(SlotOutcome outcome)
        {
            switch (outcome)
            {
                case SlotOutcome.RightOnTime: return AudioCueId.SlotRightOnTime;
                case SlotOutcome.OneMoreTime: return AudioCueId.SlotOneMoreTime;
                default: return AudioCueId.SlotNotThisTime;
            }
        }
    }
}
