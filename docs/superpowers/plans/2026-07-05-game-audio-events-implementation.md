# Gameplay Audio Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the centralized audio-signaling layer from `docs/superpowers/specs/2026-07-05-game-audio-events-design.md`: gameplay/animation code raises named cues, one scene-level `GameAudioPlayer` owns actual clip playback via Inspector-assigned bindings.

**Architecture:** A shared `AudioCueId` enum + static `GameAudioEvents` hub decouples gameplay from playback. Cue→clip resolution (binding lookup, clip pick, pitch pick) lives in a pure static class `AudioCuePlayback` so it's unit-testable without touching real audio playback. `GameAudioPlayer` (MonoBehaviour) is the only thing that calls `AudioSource` APIs. `AnimationAudioCueEmitter` re-raises cues from animation events through the same hub. Six existing gameplay scripts get a one-line `GameAudioEvents.RaiseX(...)` call added at their existing event boundary — no other behavior changes.

**Tech Stack:** Unity 6000.5.2f1, C#, `com.unity.test-framework` (NUnit + `UnityEngine.TestTools.LogAssert`), Unity MCP for scene wiring.

## Global Constraints

- All new types live in the `OneMoreTime` namespace, under `Assets/Scripts/Runtime/`.
- New tests live in `Assets/Tests/EditMode/`, using the project's existing reflection-based `GetField`/`SetField` helper pattern for private/serialized fields (see `PlayerRespawnerTests.cs`).
- Class-level comments are a single Turkish line summarizing purpose (matching existing files); no other comments unless a non-obvious WHY exists.
- Pure logic (no `MonoBehaviour`/`AudioSource` calls) must be extracted into plain C# classes so it can be unit tested, matching the existing `CorpseRegistry` / `SlotMachine` / `RunQuality` pattern.
- The design spec is the source of truth for scope. Do not add mixer routing, ducking, footsteps, or any other item listed under the spec's "Out of Scope".
- Editor/scene automation (Task 8) must go through the `unity-mcp-skill` skill, not manual file edits to the `.unity` scene file.

---

### Task 1: Central cue id + event hub

**Files:**
- Create: `Assets/Scripts/Runtime/AudioCueId.cs`
- Create: `Assets/Scripts/Runtime/GameAudioEvents.cs`
- Test: `Assets/Tests/EditMode/GameAudioEventsTests.cs`

**Interfaces:**
- Produces: `enum AudioCueId { CoinPickup, PlayerDied, RunFinished, SlotInteractionStarted, SlotRightOnTime, SlotOneMoreTime, SlotNotThisTime, SlotLoseScreenShown, LossContinueConfirmed, SceneTransitionStarted }`
- Produces: `readonly struct AudioCuePayload { AudioCueId Cue; Vector3 Position; bool HasPosition; }`
- Produces: `static class GameAudioEvents` with `event Action<AudioCuePayload> CueRaised` and: `RaiseCoinPickup(Vector3)`, `RaisePlayerDied(Vector3)`, `RaiseRunFinished()`, `RaiseSlotInteractionStarted(Vector3)`, `RaiseSlotOutcome(SlotOutcome, Vector3)`, `RaiseSlotLoseScreenShown()`, `RaiseLossContinueConfirmed()`, `RaiseSceneTransitionStarted()`, `RaiseCue(AudioCueId)`, `RaiseCue(AudioCueId, Vector3)`.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/GameAudioEventsTests.cs`:

```csharp
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class GameAudioEventsTests
{
    [Test]
    public void RaiseCoinPickup_InvokesCueRaisedWithPosition()
    {
        AudioCuePayload received = default;
        int fireCount = 0;
        void Handler(AudioCuePayload payload) { received = payload; fireCount++; }

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseCoinPickup(new Vector3(1f, 2f, 3f));
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(AudioCueId.CoinPickup, received.Cue);
        Assert.IsTrue(received.HasPosition);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), received.Position);
    }

    [Test]
    public void RaiseRunFinished_InvokesCueRaisedWithoutPosition()
    {
        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseRunFinished();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.RunFinished, received.Cue);
        Assert.IsFalse(received.HasPosition);
    }

    [TestCase(SlotOutcome.RightOnTime, AudioCueId.SlotRightOnTime)]
    [TestCase(SlotOutcome.OneMoreTime, AudioCueId.SlotOneMoreTime)]
    [TestCase(SlotOutcome.NotThisTime, AudioCueId.SlotNotThisTime)]
    public void RaiseSlotOutcome_MapsOutcomeToMatchingCue(SlotOutcome outcome, AudioCueId expectedCue)
    {
        AudioCueId received = default;
        void Handler(AudioCuePayload payload) => received = payload.Cue;

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseSlotOutcome(outcome, Vector3.zero);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(expectedCue, received);
    }

    [Test]
    public void RaiseCue_WithNoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseSceneTransitionStarted());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run the EditMode suite (Test Runner window, or Unity MCP `run_tests`). Expected: compile error — `AudioCueId`/`GameAudioEvents`/`AudioCuePayload` do not exist yet.

- [ ] **Step 3: Implement `AudioCueId.cs`**

```csharp
namespace OneMoreTime
{
    /// Ses ipuçlarının paylaşılan kimliği (bkz. docs/superpowers/specs/2026-07-05-game-audio-events-design.md).
    public enum AudioCueId
    {
        CoinPickup,
        PlayerDied,
        RunFinished,
        SlotInteractionStarted,
        SlotRightOnTime,
        SlotOneMoreTime,
        SlotNotThisTime,
        SlotLoseScreenShown,
        LossContinueConfirmed,
        SceneTransitionStarted,
    }
}
```

- [ ] **Step 4: Implement `GameAudioEvents.cs`**

```csharp
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
```

- [ ] **Step 5: Run tests to verify they pass**

Run the EditMode suite filtered to `GameAudioEventsTests`. Expected: 4/4 PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Runtime/AudioCueId.cs Assets/Scripts/Runtime/GameAudioEvents.cs Assets/Tests/EditMode/GameAudioEventsTests.cs
git commit -m "feat: add central audio cue id and event hub"
```

---

### Task 2: Cue binding data + pure playback resolver

**Files:**
- Create: `Assets/Scripts/Runtime/AudioCueBinding.cs`
- Create: `Assets/Scripts/Runtime/AudioCuePlayback.cs`
- Test: `Assets/Tests/EditMode/AudioCuePlaybackTests.cs`

**Interfaces:**
- Consumes: `AudioCueId` (Task 1)
- Produces: `class AudioCueBinding { AudioCueId cue; AudioClip[] clips; AudioSource source; bool useWorldPosition; float volume; float pitchRandomRange; }`
- Produces: `static class AudioCuePlayback` with `FindBinding(IReadOnlyList<AudioCueBinding>, AudioCueId) : AudioCueBinding`, `PickClip(AudioCueBinding, Func<float>) : AudioClip`, `PickPitch(AudioCueBinding, Func<float>) : float`. Later tasks (3) consume these exact signatures.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/AudioCuePlaybackTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class AudioCuePlaybackTests
{
    [Test]
    public void FindBinding_ReturnsFirstMatchingCue()
    {
        var match = new AudioCueBinding { cue = AudioCueId.CoinPickup };
        var bindings = new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.PlayerDied },
            match,
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
        };

        Assert.AreSame(match, AudioCuePlayback.FindBinding(bindings, AudioCueId.CoinPickup));
    }

    [Test]
    public void FindBinding_NoMatch_ReturnsNull()
    {
        var bindings = new List<AudioCueBinding> { new AudioCueBinding { cue = AudioCueId.PlayerDied } };

        Assert.IsNull(AudioCuePlayback.FindBinding(bindings, AudioCueId.CoinPickup));
    }

    [Test]
    public void PickClip_NoClips_ReturnsNull()
    {
        var binding = new AudioCueBinding { clips = new AudioClip[0] };

        Assert.IsNull(AudioCuePlayback.PickClip(binding, () => 0f));
    }

    [Test]
    public void PickClip_SingleClip_AlwaysReturnsIt()
    {
        var clip = AudioClip.Create("clip", 1, 1, 44100, false);
        var binding = new AudioCueBinding { clips = new[] { clip } };

        Assert.AreSame(clip, AudioCuePlayback.PickClip(binding, () => 0.9f));
    }

    [Test]
    public void PickClip_RollAtUpperBound_ClampsToLastClip()
    {
        var clipA = AudioClip.Create("a", 1, 1, 44100, false);
        var clipB = AudioClip.Create("b", 1, 1, 44100, false);
        var binding = new AudioCueBinding { clips = new[] { clipA, clipB } };

        Assert.AreSame(clipB, AudioCuePlayback.PickClip(binding, () => 1f));
    }

    [Test]
    public void PickPitch_ZeroRange_AlwaysReturnsOne()
    {
        var binding = new AudioCueBinding { pitchRandomRange = 0f };

        Assert.AreEqual(1f, AudioCuePlayback.PickPitch(binding, () => 0.5f), 0.0001f);
    }

    [Test]
    public void PickPitch_UsesRangeAroundOne()
    {
        var binding = new AudioCueBinding { pitchRandomRange = 0.2f };

        Assert.AreEqual(0.9f, AudioCuePlayback.PickPitch(binding, () => 0f), 0.0001f);
        Assert.AreEqual(1.1f, AudioCuePlayback.PickPitch(binding, () => 1f), 0.0001f);
        Assert.AreEqual(1.0f, AudioCuePlayback.PickPitch(binding, () => 0.5f), 0.0001f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run the EditMode suite. Expected: compile error — `AudioCueBinding`/`AudioCuePlayback` do not exist yet.

- [ ] **Step 3: Implement `AudioCueBinding.cs`**

```csharp
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
```

- [ ] **Step 4: Implement `AudioCuePlayback.cs`**

```csharp
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
```

- [ ] **Step 5: Run tests to verify they pass**

Run the EditMode suite filtered to `AudioCuePlaybackTests`. Expected: 7/7 PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Runtime/AudioCueBinding.cs Assets/Scripts/Runtime/AudioCuePlayback.cs Assets/Tests/EditMode/AudioCuePlaybackTests.cs
git commit -m "feat: add audio cue binding data and pure playback resolver"
```

---

### Task 3: Scene player MonoBehaviour

**Files:**
- Create: `Assets/Scripts/Runtime/GameAudioPlayer.cs`
- Test: `Assets/Tests/EditMode/GameAudioPlayerTests.cs`

**Interfaces:**
- Consumes: `GameAudioEvents.CueRaised`, `AudioCuePlayback.FindBinding/PickClip/PickPitch`, `AudioCueBinding` (Tasks 1-2)
- Produces: `class GameAudioPlayer : MonoBehaviour` with private `List<AudioCueBinding> bindings` (serialized field, name `bindings` — later tasks/tests set it via reflection).

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/GameAudioPlayerTests.cs`:

```csharp
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class GameAudioPlayerTests
{
    readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
            if (_createdObjects[i]) Object.DestroyImmediate(_createdObjects[i]);

        _createdObjects.Clear();
    }

    [Test]
    public void MissingBinding_LogsWarningAndDoesNotThrow()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        go.AddComponent<GameAudioPlayer>();

        LogAssert.Expect(LogType.Warning, new Regex("no binding for cue CoinPickup.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseCoinPickup(Vector3.zero));
    }

    [Test]
    public void BindingWithNoClips_LogsWarningAndDoesNotThrow()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.CoinPickup, clips = new AudioClip[0] },
        });

        LogAssert.Expect(LogType.Warning, new Regex("binding for cue CoinPickup has no clips.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseCoinPickup(Vector3.zero));
    }

    [Test]
    public void WorldPositionBindingWithoutPosition_LogsWarningAndDoesNotThrow()
    {
        var clip = AudioClip.Create("clip", 1, 1, 44100, false);

        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.RunFinished, clips = new[] { clip }, useWorldPosition = true },
        });

        LogAssert.Expect(LogType.Warning, new Regex("needs a world position but none was raised.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseRunFinished());
    }

    [Test]
    public void OnValidate_DuplicateBindingsForSameCue_LogsWarning()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
        });

        LogAssert.Expect(LogType.Warning, new Regex("duplicate binding for cue CoinPickup.*"));
        InvokeOnValidate(player);
    }

    static void InvokeOnValidate(GameAudioPlayer player)
    {
        typeof(GameAudioPlayer)
            .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(player, null);
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run the EditMode suite. Expected: compile error — `GameAudioPlayer` does not exist yet.

- [ ] **Step 3: Implement `GameAudioPlayer.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime
{
    /// Sahnede tek örnek: GameAudioEvents'i dinler, cue'yu Inspector'da atanmış klibe çevirir.
    /// Eksik binding veya klip oynatmayı sessizce atlar — ses hatası gameplay'i asla durdurmaz.
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"GameAudioPlayer: no binding for cue {payload.Cue}.", this);
#endif
                return;
            }

            AudioClip clip = AudioCuePlayback.PickClip(binding, () => Random.value);
            if (clip == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"GameAudioPlayer: binding for cue {payload.Cue} has no clips.", this);
#endif
                return;
            }

            float pitch = AudioCuePlayback.PickPitch(binding, () => Random.value);

            if (binding.useWorldPosition)
            {
                if (!payload.HasPosition)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"GameAudioPlayer: cue {payload.Cue} needs a world position but none was raised.", this);
#endif
                    return;
                }

                PlayAtPoint(clip, payload.Position, binding.volume, pitch);
            }
            else
            {
                binding.source.pitch = pitch;
                binding.source.PlayOneShot(clip, binding.volume);
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode suite filtered to `GameAudioPlayerTests`. Expected: 4/4 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Runtime/GameAudioPlayer.cs Assets/Tests/EditMode/GameAudioPlayerTests.cs
git commit -m "feat: add scene-level GameAudioPlayer"
```

---

### Task 4: Animation bridge

**Files:**
- Create: `Assets/Scripts/Runtime/AnimationAudioCueEmitter.cs`
- Test: `Assets/Tests/EditMode/AnimationAudioCueEmitterTests.cs`

**Interfaces:**
- Consumes: `GameAudioEvents.RaiseCue(AudioCueId, Vector3)` (Task 1)
- Produces: `class AnimationAudioCueEmitter : MonoBehaviour` with public `RaiseConfiguredCue()` and `RaiseCueById(int cueId)`, and private serialized field `cue` of type `AudioCueId`.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/AnimationAudioCueEmitterTests.cs`:

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class AnimationAudioCueEmitterTests
{
    readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
            if (_createdObjects[i]) Object.DestroyImmediate(_createdObjects[i]);

        _createdObjects.Clear();
    }

    [Test]
    public void RaiseConfiguredCue_RaisesConfiguredCueAtEmitterPosition()
    {
        var go = new GameObject("Emitter");
        _createdObjects.Add(go);
        go.transform.position = new Vector3(1f, 2f, 3f);
        AnimationAudioCueEmitter emitter = go.AddComponent<AnimationAudioCueEmitter>();
        SetField(emitter, "cue", AudioCueId.SlotInteractionStarted);

        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            emitter.RaiseConfiguredCue();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.SlotInteractionStarted, received.Cue);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), received.Position);
    }

    [Test]
    public void RaiseCueById_RaisesCueCastFromInt()
    {
        var go = new GameObject("Emitter");
        _createdObjects.Add(go);
        AnimationAudioCueEmitter emitter = go.AddComponent<AnimationAudioCueEmitter>();

        AudioCueId received = default;
        void Handler(AudioCuePayload payload) => received = payload.Cue;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            emitter.RaiseCueById((int)AudioCueId.PlayerDied);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.PlayerDied, received);
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run the EditMode suite. Expected: compile error — `AnimationAudioCueEmitter` does not exist yet.

- [ ] **Step 3: Implement `AnimationAudioCueEmitter.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode suite filtered to `AnimationAudioCueEmitterTests`. Expected: 2/2 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Runtime/AnimationAudioCueEmitter.cs Assets/Tests/EditMode/AnimationAudioCueEmitterTests.cs
git commit -m "feat: add animation-event audio cue bridge"
```

---

### Task 5: Wire token pickup and player death

**Files:**
- Modify: `Assets/Scripts/Runtime/TokenPickup.cs`
- Modify: `Assets/Scripts/Runtime/PlayerRespawner.cs:32-39` (the `Kill()` method)
- Create: `Assets/Tests/EditMode/TokenPickupTests.cs`
- Modify: `Assets/Tests/EditMode/PlayerRespawnerTests.cs`

**Interfaces:**
- Consumes: `GameAudioEvents.RaiseCoinPickup(Vector3)`, `GameAudioEvents.RaisePlayerDied(Vector3)` (Task 1)

- [ ] **Step 1: Write the failing test for `TokenPickup`**

Create `Assets/Tests/EditMode/TokenPickupTests.cs`:

```csharp
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class TokenPickupTests
{
    readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
            if (_createdObjects[i]) Object.DestroyImmediate(_createdObjects[i]);

        _createdObjects.Clear();
    }

    [Test]
    public void OnTriggerEnter_PlayerHasTokens_RaisesCoinPickupAtPickupPosition()
    {
        var pickupGo = new GameObject("TokenPickup");
        _createdObjects.Add(pickupGo);
        pickupGo.transform.position = new Vector3(2f, 0f, 4f);
        TokenPickup pickup = pickupGo.AddComponent<TokenPickup>();

        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        playerGo.AddComponent<PlayerTokens>();
        BoxCollider collider = playerGo.AddComponent<BoxCollider>();

        AudioCuePayload received = default;
        int fireCount = 0;
        void Handler(AudioCuePayload payload) { received = payload; fireCount++; }
        GameAudioEvents.CueRaised += Handler;
        try
        {
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode.*"));
            InvokeOnTriggerEnter(pickup, collider);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(AudioCueId.CoinPickup, received.Cue);
        Assert.AreEqual(new Vector3(2f, 0f, 4f), received.Position);
    }

    static void InvokeOnTriggerEnter(TokenPickup pickup, Collider other)
    {
        typeof(TokenPickup)
            .GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pickup, new object[] { other });
    }
}
```

- [ ] **Step 2: Write the failing test for `PlayerRespawner.Kill`**

Add this test method to `Assets/Tests/EditMode/PlayerRespawnerTests.cs` (inside the existing class, alongside the other `[Test]` methods):

```csharp
    [Test]
    public void Kill_RaisesPlayerDiedAtDeathPosition()
    {
        var player = new GameObject("Player");
        _createdObjects.Add(player);
        player.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = player.AddComponent<Rigidbody>();

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = Vector3.zero;

        var corpsePrefab = new GameObject("CorpsePrefab");
        _createdObjects.Add(corpsePrefab);

        PlayerRespawner respawner = player.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);
        SetField(respawner, "corpsePrefab", corpsePrefab);

        Vector3 deathPosition = body.position;
        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            respawner.Kill();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        _createdObjects.Add(GetField<CorpseRegistry>(respawner, "_registry").Corpses[0]);
        Assert.AreEqual(AudioCueId.PlayerDied, received.Cue);
        Assert.AreEqual(deathPosition, received.Position);
    }
```

- [ ] **Step 3: Run tests to verify they fail**

Run the EditMode suite filtered to `TokenPickupTests` and `PlayerRespawnerTests`. Expected: `OnTriggerEnter_PlayerHasTokens_RaisesCoinPickupAtPickupPosition` and `Kill_RaisesPlayerDiedAtDeathPosition` FAIL (no cue raised — `fireCount == 0` / `received` stays default).

- [ ] **Step 4: Wire the hooks**

Edit `Assets/Scripts/Runtime/TokenPickup.cs`:

```csharp
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
```

Edit `Assets/Scripts/Runtime/PlayerRespawner.cs`, in `Kill()`:

```csharp
        public void Kill()
        {
            Vector3 deathPosition = body.position + Vector3.up * spawnYOffset;
            GameObject corpse = Instantiate(corpsePrefab, deathPosition, Quaternion.identity);
            _registry.Register(corpse);

            GameAudioEvents.RaisePlayerDied(deathPosition);
            TeleportToSpawn();
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run the EditMode suite filtered to `TokenPickupTests` and `PlayerRespawnerTests`. Expected: all PASS (including the pre-existing `PlayerRespawnerTests` tests, unaffected).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Runtime/TokenPickup.cs Assets/Scripts/Runtime/PlayerRespawner.cs Assets/Tests/EditMode/TokenPickupTests.cs Assets/Tests/EditMode/PlayerRespawnerTests.cs
git commit -m "feat: raise audio cues on coin pickup and player death"
```

---

### Task 6: Wire run finish and loss flow

**Files:**
- Modify: `Assets/Scripts/Runtime/RunController.cs:35-46` (the `Finish()` method)
- Modify: `Assets/Scripts/Runtime/LossFlowController.cs:30-58` (`ShowLoseScreen()` and `ForceContinue()`)
- Modify: `Assets/Tests/EditMode/RunControllerTests.cs`
- Modify: `Assets/Tests/EditMode/LossFlowControllerTests.cs`

**Interfaces:**
- Consumes: `GameAudioEvents.RaiseRunFinished()`, `GameAudioEvents.RaiseSlotLoseScreenShown()`, `GameAudioEvents.RaiseLossContinueConfirmed()` (Task 1)

- [ ] **Step 1: Write the failing test for `RunController.Finish`**

Add this test method to `Assets/Tests/EditMode/RunControllerTests.cs`:

```csharp
    [Test]
    public void Finish_RaisesRunFinishedCue()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerRespawner player = playerGo.AddComponent<PlayerRespawner>();

        var runGo = new GameObject("RunController");
        _createdObjects.Add(runGo);
        RunController run = runGo.AddComponent<RunController>();
        SetField(run, "player", player);

        RunTimer timer = GetField<RunTimer>(run, "_timer");
        timer.Begin();

        int fireCount = 0;
        void Handler(AudioCuePayload payload) => fireCount++;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            run.Finish();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
    }
```

- [ ] **Step 2: Write the failing tests for `LossFlowController`**

Add a builder helper and two test methods to `Assets/Tests/EditMode/LossFlowControllerTests.cs` (inside the existing class, alongside the existing test and helpers — do not modify the existing `ForceContinue_ClearsCorpsesResetsTokensAndRestartsRun` test):

```csharp
    LossFlowController BuildLossFlowController()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        playerGo.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = playerGo.AddComponent<Rigidbody>();

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = Vector3.zero;

        PlayerRespawner respawner = playerGo.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);

        PlayerTokens tokens = playerGo.AddComponent<PlayerTokens>();

        var runGo = new GameObject("Run");
        _createdObjects.Add(runGo);
        RunController run = runGo.AddComponent<RunController>();

        var slotGo = new GameObject("Slot");
        _createdObjects.Add(slotGo);
        slotGo.SetActive(false);
        SlotController slot = slotGo.AddComponent<SlotController>();

        var movementGo = new GameObject("Movement");
        _createdObjects.Add(movementGo);
        movementGo.SetActive(false);
        PlayerMovementController movement = movementGo.AddComponent<PlayerMovementController>();
        SetField(movement, "_rb", movementGo.GetComponent<Rigidbody>());

        var lookGo = new GameObject("Look");
        _createdObjects.Add(lookGo);
        lookGo.SetActive(false);
        FirstPersonLook look = lookGo.AddComponent<FirstPersonLook>();

        var camGo = new GameObject("Cam");
        _createdObjects.Add(camGo);
        SpeedFovEffect fov = camGo.AddComponent<SpeedFovEffect>();

        var hudGo = new GameObject("Hud");
        _createdObjects.Add(hudGo);
        SlotHudDebug hud = hudGo.AddComponent<SlotHudDebug>();

        var interactionGo = new GameObject("Interaction");
        _createdObjects.Add(interactionGo);
        interactionGo.SetActive(false);
        SlotMachineInteraction interaction = interactionGo.AddComponent<SlotMachineInteraction>();
        SetField(interaction, "movement", movement);
        SetField(interaction, "look", look);
        SetField(interaction, "fov", fov);
        SetField(interaction, "slot", slot);
        SetField(interaction, "hud", hud);
        SetField(interaction, "cameraTransform", camGo.transform);

        var loseScreenGo = new GameObject("LoseScreen");
        _createdObjects.Add(loseScreenGo);
        loseScreenGo.SetActive(false);
        CanvasGroup group = loseScreenGo.AddComponent<CanvasGroup>();
        LoseScreen loseScreen = loseScreenGo.AddComponent<LoseScreen>();
        SetField(loseScreen, "group", group);
        loseScreenGo.SetActive(true);

        var lossGo = new GameObject("LossFlow");
        _createdObjects.Add(lossGo);
        lossGo.SetActive(false);
        LossFlowController loss = lossGo.AddComponent<LossFlowController>();
        SetField(loss, "slot", slot);
        SetField(loss, "player", respawner);
        SetField(loss, "tokens", tokens);
        SetField(loss, "run", run);
        SetField(loss, "interaction", interaction);
        SetField(loss, "loseScreen", loseScreen);

        return loss;
    }

    [Test]
    public void ShowLoseScreen_WhenArmed_RaisesSlotLoseScreenShownCue()
    {
        LossFlowController loss = BuildLossFlowController();
        SetField(loss, "_lossArmed", true);

        int fireCount = 0;
        void Handler(AudioCuePayload payload) => fireCount++;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            loss.ShowLoseScreen();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void ForceContinue_RaisesLossContinueConfirmedCue()
    {
        // BuildLossFlowController registers no corpse, so ClearCorpses() has nothing to
        // destroy here — unlike ForceContinue_ClearsCorpsesResetsTokensAndRestartsRun, no
        // edit-mode Destroy error is expected.
        LossFlowController loss = BuildLossFlowController();

        int fireCount = 0;
        void Handler(AudioCuePayload payload) => fireCount++;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            loss.ForceContinue();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
    }
```

- [ ] **Step 3: Run tests to verify they fail**

Run the EditMode suite filtered to `RunControllerTests` and `LossFlowControllerTests`. Expected: the three new tests FAIL (`fireCount == 0`).

- [ ] **Step 4: Wire the hooks**

Edit `Assets/Scripts/Runtime/RunController.cs`, in `Finish()`:

```csharp
        public void Finish()
        {
            if (HasFinished) return;

            _timer.Stop();
            float seconds = _timer.Elapsed;
            int corpses = player.CorpseCount;
            float rightChance = RunQuality.RightOnTimeChance(seconds, corpses, parTime, runQuality);
            LastResult = new RunResult(seconds, corpses, rightChance);
            HasFinished = true;
            GameAudioEvents.RaiseRunFinished();
            RunFinished?.Invoke(LastResult);
        }
```

Edit `Assets/Scripts/Runtime/LossFlowController.cs`, in `ShowLoseScreen()` and `ForceContinue()`:

```csharp
        public void ShowLoseScreen()
        {
            if (!_lossArmed) return;

            loseScreen.Show();
            GameAudioEvents.RaiseSlotLoseScreenShown();
            _awaitingContinue = true;
        }
```

```csharp
        public void ForceContinue()
        {
            interaction.EndInteraction(instant: true); // teleport ile çakışmasın diye anlık
            player.ClearCorpses();
            player.ResetToSpawn();
            tokens.ResetToDefault();
            slot.ClearAfterLoss();
            run.BeginRun();
            GameAudioEvents.RaiseLossContinueConfirmed();
            _awaitingContinue = false;
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run the EditMode suite filtered to `RunControllerTests` and `LossFlowControllerTests`. Expected: all PASS (including the pre-existing tests, unaffected).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Runtime/RunController.cs Assets/Scripts/Runtime/LossFlowController.cs Assets/Tests/EditMode/RunControllerTests.cs Assets/Tests/EditMode/LossFlowControllerTests.cs
git commit -m "feat: raise audio cues on run finish and loss flow steps"
```

---

### Task 7: Wire slot interaction, slot outcome, and scene transition

**Files:**
- Modify: `Assets/Scripts/Runtime/SlotMachineInteraction.cs:67-89` (`BeginInteraction()` and `HandleSpinResolved(...)`)
- Modify: `Assets/Scripts/Runtime/SceneFadeTransition.cs:21-27` (`LoadScene(...)`)

**Interfaces:**
- Consumes: `GameAudioEvents.RaiseSlotInteractionStarted(Vector3)`, `GameAudioEvents.RaiseSlotOutcome(SlotOutcome, Vector3)`, `GameAudioEvents.RaiseSceneTransitionStarted()` (Task 1)

No automated tests for this task: `SlotMachineInteraction` has no existing test file — its `Awake()` requires a real `InputActionAsset` and `BeginInteraction()`/the coroutine paths require an active `Animator`/camera rig, so building the fixture would need scaffolding disproportionate to a one-line audio hook. `SceneFadeTransition.LoadScene` drives an actual scene load via `SceneManager.LoadSceneAsync`, which EditMode tests cannot exercise. Both are covered by the design spec's own acceptance gate: compile-clean plus a focused manual playtest (Task 8, step 8).

- [ ] **Step 1: Wire `SlotMachineInteraction`**

Edit `Assets/Scripts/Runtime/SlotMachineInteraction.cs`, in `BeginInteraction()`:

```csharp
        void BeginInteraction()
        {
            run.Finish(); // koşu zaten bittiyse no-op (RunController.HasFinished guard'lı)
            _interacting = true;

            _savedCamPos = cameraTransform.position;
            _savedCamRot = cameraTransform.rotation;

            movement.SetControlEnabled(false);
            look.SetControlEnabled(false);
            fov.enabled = false;

            GameAudioEvents.RaiseSlotInteractionStarted(transform.position);

            if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);
            _cameraRoutine = StartCoroutine(MoveCamera(viewpoint.position, viewpoint.rotation, zoomDuration, null));
        }
```

And in `HandleSpinResolved(...)`:

```csharp
        void HandleSpinResolved(SlotSpinResult result)
        {
            hud.enabled = false;
            slot.InputLocked = true;
            GameAudioEvents.RaiseSlotOutcome(result.Outcome, transform.position);
            machineAnimator.SetTrigger(SpinTrigger); // idle -> Pull-lever -> Shuffle (otomatik)
            StartCoroutine(PlaySpinSequence(result.Outcome));
        }
```

- [ ] **Step 2: Wire `SceneFadeTransition`**

Edit `Assets/Scripts/Runtime/SceneFadeTransition.cs`, in `LoadScene(...)`:

```csharp
        public void LoadScene(string sceneName)
        {
            if (_transitioning) return;

            _transitioning = true;
            GameAudioEvents.RaiseSceneTransitionStarted();
            StartCoroutine(LoadSceneRoutine(sceneName));
        }
```

- [ ] **Step 3: Verify the project compiles with zero errors**

Run the EditMode test suite (any filter) via Test Runner or Unity MCP `run_tests` — a compile error in any script would fail the whole run. Expected: full suite still PASS (no new tests added by this task; all pre-existing tests unaffected).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Runtime/SlotMachineInteraction.cs Assets/Scripts/Runtime/SceneFadeTransition.cs
git commit -m "feat: raise audio cues on slot interaction, spin outcome, and scene transition"
```

---

### Task 8: Wire the scene player and manually verify

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity` (via Unity Editor / Unity MCP tools only — do not hand-edit the `.unity` YAML)

This task requires a live Unity Editor session. **Invoke the `unity-mcp-skill` skill before starting.**

- [ ] **Step 1: Confirm zero compile errors**

Use the Unity MCP `read_console` resource/tool. Expected: no errors, `isCompiling` false.

- [ ] **Step 2: Create the `GameAudioPlayer` GameObject**

In `SampleScene`, create an empty GameObject named `GameAudioPlayer` at the scene root. Add the `GameAudioPlayer` component. Add one `AudioSource` component to the same GameObject (this is the shared, non-spatial source for cues that don't need a world position).

- [ ] **Step 3: Populate the ten initial cue bindings**

On the `GameAudioPlayer` component's `bindings` list, add one entry per `AudioCueId`, leaving `clips` empty (designers fill these in later per the spec's authoring workflow) and `source` set only where needed:

| cue | useWorldPosition | source |
|---|---|---|
| `CoinPickup` | true | — |
| `PlayerDied` | true | — |
| `RunFinished` | false | the `AudioSource` from Step 2 |
| `SlotInteractionStarted` | true | — |
| `SlotRightOnTime` | true | — |
| `SlotOneMoreTime` | true | — |
| `SlotNotThisTime` | true | — |
| `SlotLoseScreenShown` | false | the `AudioSource` from Step 2 |
| `LossContinueConfirmed` | false | the `AudioSource` from Step 2 |
| `SceneTransitionStarted` | false | the `AudioSource` from Step 2 |

- [ ] **Step 4: Save the scene**

Save `SampleScene` via the Unity MCP `manage_scene` tool (or File → Save).

- [ ] **Step 5: Run the full EditMode suite**

Run all EditMode tests (Test Runner window or Unity MCP `run_tests`). Expected: all tests from Tasks 1-6 PASS, zero regressions in pre-existing tests.

- [ ] **Step 6: Manual playtest**

Enter Play mode and confirm (per the design spec's Verification section — clips will be silent until a designer assigns them, but each should log no warnings once bindings exist):
- coin pickup logs no missing-binding warning on pickup;
- death (press the self-destruct key) logs no missing-binding warning;
- run-finish (reach the slot machine) logs no missing-binding warning;
- entering slot interaction range and pressing Interact logs no missing-binding warning;
- each slot spin outcome logs no missing-binding warning;
- the lose screen and forced-continue path log no missing-binding warning;
- triggering a scene transition (winning the slot spin) logs no missing-binding warning.

- [ ] **Step 7: Commit the scene change**

```bash
git add Assets/Scenes/SampleScene.unity
git commit -m "feat: wire GameAudioPlayer into SampleScene with initial cue bindings"
```

---

## Deferred (per spec's Future Extensions — do not implement now)

- Footstep cadence cues.
- Slot lever/reel-stop cues migrating from gameplay timing to `AnimationAudioCueEmitter` timing.
- Mixer routing, ducking, reverb zones, occlusion, distance culling.
- Actual clip assignment in the Inspector (designer task, out of engineering scope).
