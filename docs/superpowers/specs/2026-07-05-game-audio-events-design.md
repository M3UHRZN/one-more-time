# Gameplay Audio Events Design

## Goal

Add the smallest centralized audio-signaling layer that lets gameplay code say "this happened, play a sound" while keeping clip assignment in the Inspector. Discrete gameplay events must route through one shared path, and animation-timed sounds must be able to trigger that same path without adding per-sound gameplay code.

## Scope

This change covers the current event-heavy gameplay in `SampleScene`:

- token pickup;
- player death / respawn sacrifice;
- run finish;
- slot interaction start;
- slot spin result;
- real loss screen shown;
- loss continue / reset;
- scene transition start.

The design also includes a tiny animation bridge for sounds that must land on an exact animation frame, such as future slot lever clicks or reel-stop hits.

## Out of Scope

- a footstep cadence system;
- mixer groups, ducking, reverb zones, occlusion, or distance culling systems;
- addressable or database-driven audio loading;
- procedural audio variation beyond simple clip randomization;
- editor tooling beyond normal serialized Inspector fields;
- replacing current animator setup or adding a full animation-audio framework.

## Authoring Experience

- One scene-level `GameAudioPlayer` owns cue playback.
- Designers drag clips into serialized cue slots in the Inspector and are done.
- Gameplay scripts never hold `AudioClip` references and never call `AudioSource.Play*` directly.
- Animation clips can trigger exact-frame cues by calling one small emitter method from animation events.
- Adding a new cue should usually mean: add one enum entry, add one Inspector binding, and raise it from the relevant gameplay or animation hook.

## Architecture

### Central cue id

Use one `AudioCueId` enum as the shared identifier for audio cues. This keeps Inspector selection simple and avoids typo-prone strings.

Initial cue ids should cover the current requested surface area:

- `CoinPickup`
- `PlayerDied`
- `RunFinished`
- `SlotInteractionStarted`
- `SlotRightOnTime`
- `SlotOneMoreTime`
- `SlotNotThisTime`
- `SlotLoseScreenShown`
- `LossContinueConfirmed`
- `SceneTransitionStarted`

Animation-only cues can live in the same enum later, for example `SlotLeverPull`, `SlotReelStop`, or `FootstepConcrete`.

### Event hub

`GameAudioEvents` is a small static hub. It exposes typed convenience methods such as:

- `RaiseCoinPickup(Vector3 position)`
- `RaisePlayerDied(Vector3 position)`
- `RaiseRunFinished()`
- `RaiseSlotInteractionStarted(Vector3 position)`
- `RaiseSlotOutcome(SlotOutcome outcome, Vector3 position)`
- `RaiseSlotLoseScreenShown()`
- `RaiseLossContinueConfirmed()`
- `RaiseSceneTransitionStarted()`

Internally, these methods publish one common payload containing:

- `AudioCueId cue`
- optional world position

Gameplay callers therefore stay readable, while playback still routes through one shared subscription path.

### Scene player

`GameAudioPlayer` is a MonoBehaviour placed once in the scene. It subscribes to `GameAudioEvents` and maps cue ids to serialized playback bindings.

Each binding contains only the minimum authoring data:

- `AudioCueId cue`
- `AudioClip[] clips`
- `AudioSource source` for non-spatial or explicitly routed playback
- `bool useWorldPosition`
- optional volume multiplier
- optional small pitch-randomization range

If `useWorldPosition` is false, playback uses the assigned `AudioSource`.
If `useWorldPosition` is true, playback uses the provided event position through a simple one-shot world playback path.

This keeps authoring Inspector-first while avoiding a second custom component on every gameplay object just to play one sound.

### Animation bridge

`AnimationAudioCueEmitter` is a tiny MonoBehaviour with public methods callable from animation events. Its job is not to play audio directly; it only re-raises a chosen `AudioCueId` through the same central path.

The emitter should support:

- playing a configured cue from the emitter's transform position;
- optionally playing a passed-in cue id if Unity animation-event parameter support is used later.

This keeps animation-timed sounds aligned to keyframes without creating a separate audio architecture.

## Event Sources

The first implementation should hook only natural source points that already exist:

1. `TokenPickup.OnTriggerEnter` raises `CoinPickup`.
2. `PlayerRespawner.Kill()` raises `PlayerDied`.
3. `RunController.Finish()` raises `RunFinished`.
4. `SlotMachineInteraction.BeginInteraction()` raises `SlotInteractionStarted`.
5. `SlotMachineInteraction.HandleSpinResolved(...)` or the immediately surrounding resolved path raises one of:
   - `SlotRightOnTime`
   - `SlotOneMoreTime`
   - `SlotNotThisTime`
6. `LossFlowController.ShowLoseScreen()` raises `SlotLoseScreenShown`.
7. `LossFlowController.ForceContinue()` raises `LossContinueConfirmed`.
8. `SceneFadeTransition.LoadScene(...)` raises `SceneTransitionStarted`.

No unrelated cleanup or refactor is part of this work. Existing gameplay ownership stays where it is.

## Data Flow

1. A gameplay script reaches a natural event boundary.
2. That script raises a named `GameAudioEvents` helper.
3. The helper converts the event to a single `AudioCueId` plus optional position payload.
4. `GameAudioPlayer` receives the payload, finds the matching serialized binding, selects a clip, and plays it.
5. If the cue is animation-timed instead, an animation event calls `AnimationAudioCueEmitter`, which raises the same central event payload and then follows the same playback path.

This gives one audio-routing path for both gameplay events and animation events.

## Failure Handling

- Missing cue binding is a no-op in play mode and should log a warning only in the editor or development builds.
- Binding with no clips is also a no-op with the same lightweight warning behavior.
- Duplicate bindings for the same `AudioCueId` should be treated as invalid authoring data; `OnValidate` or subscription-time validation should warn clearly and use the first valid binding.
- Audio failures must never block gameplay flow. If no sound can play, the game still proceeds normally.

## Verification

Implementation verification should stay lightweight and practical:

- Unity compilation succeeds with zero console errors.
- The scene contains one valid `GameAudioPlayer` with serialized cue bindings.
- Each initial gameplay hook raises exactly one cue at the correct event boundary.
- A short playtest confirms:
  - coin pickup sound fires on pickup;
  - death sound fires on sacrifice / hazard death;
  - run-finish sound fires once;
  - slot interaction start fires once on entering the machine flow;
  - each slot outcome fires the correct outcome cue;
  - lose-screen and continue sounds fire on the correct loss-flow steps;
  - scene transition sound fires when fade/load starts.
- An animation-event smoke test confirms one emitter-triggered cue can be heard at the expected frame.

Automated tests are not the primary value of this change. The main risk is wrong hook placement or wrong Inspector wiring, so compile safety plus focused play verification is the acceptance gate.

## Future Extensions

This design intentionally leaves room for later additions without changing the basic workflow:

- footstep cadence can emit `AudioCueId` values later;
- surface-specific footsteps can be added by extending cue ids, not by replacing the system;
- slot-machine animation clips can gradually migrate exact-frame sounds from gameplay timing to animation timing;
- mixer routing or richer cue settings can be added to bindings later if the project grows.

The core rule should remain unchanged: gameplay and animation announce cues, and one scene-level player owns actual clip playback.
