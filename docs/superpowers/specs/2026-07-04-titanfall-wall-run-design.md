# Titanfall-Style Wall Run Design

## Goal

Replace the current sticky wall-slide behavior with an automatic, momentum-preserving wall run and a strong wall jump that feels compatible with the existing Quake-style air movement.

## Scope

This change adds one focused traversal mode: wall running. It does not introduce the project's future full movement state machine. The implementation must nevertheless keep wall-run lifecycle data isolated so it can later move behind a shared movement-state interface without rewriting its rules.

Animations, weapon behavior, stamina, audio, particles, wall-run VFX, and a class-per-state architecture are outside this change.

## Player Experience

- Wall run starts automatically when the airborne player touches a near-vertical wall.
- The player does not need to hold movement toward the wall.
- Contact never clamps momentum to run or sprint speed. The complete incoming horizontal speed is redirected along the wall and preserved.
- A faster entry therefore covers more distance during the same two-second run; a slower entry covers less.
- Wall run lasts at most `2.0` seconds.
- Entry vertical speed is preserved when rising. A falling player is caught into a level run, then the run gradually trends toward a gentle fall by the end of its duration.
- Pressing Jump preserves all wall-tangent speed and adds outward and upward velocity. Camera facing never replaces or reduces this momentum.
- Moving away from the wall, pressing Crouch, losing the wall, reaching the two-second limit, or jumping ends the run.
- Every exit requires physical separation before the same wall can be reacquired, preventing an expired run from restarting while contact continues. After a wall jump, the same wall also has a `0.2` second timed lock. An opposing wall remains immediately available.
- The first-person camera rolls `8` degrees toward the contacted wall and smoothly returns to level on exit.
- The existing fall-speed clamp (`wallSlideMaxFall`) is removed. There is no sticky wall slide after the change.

## Architecture

### Rigidbody authority

`PlayerMovementController` remains the only code that assigns `Rigidbody.linearVelocity`. No second MonoBehaviour competes for physics authority.

### Wall-run lifecycle

A new pure C# `WallRunState` class owns only wall-run runtime data and transitions:

- active/inactive status;
- elapsed time;
- retained wall normal;
- chosen tangent direction;
- preserved horizontal speed;
- entry vertical speed;
- last exited wall normal, separation state, and wall-jump cooldown.

It does not read input, perform physics queries, move a Rigidbody, or access scene objects. The controller supplies observations and consumes its state. When the full movement state machine is introduced later, this class can be wrapped or migrated into the shared state contract.

### Pure movement math

`MovementMath` owns vector calculations:

- choose a stable tangent direction from incoming velocity, input, and camera-right fallback;
- redirect the full horizontal speed onto the wall tangent without reducing its magnitude;
- calculate wall-run vertical velocity over normalized run time;
- calculate wall-jump velocity by preserving tangent speed and adding outward/upward impulse.

These functions remain stateless and are verified with EditMode tests.

### Controller integration

`PlayerMovementController` remains responsible for:

- ground and wall queries;
- reading Move, Jump, and Crouch input;
- deciding when observations permit entry;
- ticking `WallRunState`;
- choosing the grounded, sliding, wall-running, or airborne velocity path;
- consuming ground/wall jump buffers exactly once;
- exposing wall-run status and side to camera effects.

This keeps the current controller architecture intact without scattering new `_wallRunning` timer and cooldown fields throughout it.

## Wall Detection and Direction

The existing four body-axis wall probes remain the detection basis, but the implementation must not allocate a new direction array every physics tick. A valid wall has `abs(normal.y) < 0.3` and is inside `wallProbe` distance.

When several probes hit, the controller selects a valid wall that is not blocked by the same-wall cooldown and can produce a wall tangent.

Direction selection is deterministic:

1. Project incoming horizontal velocity onto the wall plane and use its sign when meaningful.
2. If the approach is effectively head-on, project the current wish direction onto the wall.
3. If that is also ambiguous, project camera-right onto the wall as an automatic fallback.
4. If no stable tangent exists or horizontal speed is effectively zero, do not enter wall run.

The chosen tangent is normalized, then multiplied by the full incoming horizontal speed. No `runSpeed`, `sprintSpeed`, or air-speed cap is applied at entry.

## Wall-Run Velocity

Horizontal velocity remains `tangentDirection * preservedHorizontalSpeed` for the run. Wall run does not add acceleration, but it never deletes acceleration or momentum earned before contact.

Vertical velocity begins at `max(entryVerticalSpeed, 0)`. Across normalized run time `elapsed / 2.0`, it moves smoothly toward `-3 m/s`. This produces a brief level/rising start and a readable gentle fall near the end without the old sticky slide cap.

Normal air acceleration and gravity resume immediately after exit.

## Exit Rules

The run ends when the first applicable condition occurs:

1. Jump is consumed: apply wall jump, require separation from the exited wall, and start the additional `0.2` second same-wall lock.
2. Crouch is pressed: detach into normal airborne movement.
3. Wish direction points away from the wall with `dot(wishDirection, wallNormal) > 0.25`.
4. Valid contact is absent beyond the existing `0.1` second coyote/contact grace.
5. Elapsed wall-run time reaches `2.0` seconds.
6. The player becomes grounded.

Exiting for any reason clears wall-run camera roll and blocks the exited wall until physical separation. Only a wall jump starts the additional timed `0.2` second lock.

## Wall Jump

Wall jump uses the active or most recently retained wall normal. Its horizontal result is:

`preserved tangent velocity + wallNormal * wallJumpPush`

Vertical velocity is set to the existing configured jump velocity. The tangent component is never clamped or replaced, so high-speed entries remain high-speed exits. The same buffered press cannot also trigger a later ground jump.

## Camera

`PlayerMovementController` exposes `IsWallRunning` and a signed wall side. `FirstPersonLook` combines this with the existing slide camera effect:

- sliding keeps its existing drop and roll behavior;
- wall running keeps normal eye height and targets `8` degrees of roll toward the wall;
- neither state can be active simultaneously;
- existing smoothing returns roll to zero on exit.

## Configuration

Retain:

- `wallProbe = 0.15 m`;
- `wallJumpPush = 7 m/s`;
- existing `coyoteTime = 0.1 s` for brief contact grace.

Add:

- `wallRunDuration = 2.0 s`;
- `sameWallCooldown = 0.2 s`;
- `wallRunEndFallSpeed = 3.0 m/s`.

Remove:

- `wallSlideMaxFall`.

Camera roll remains a serialized `FirstPersonLook` feel value with a default of `8` degrees.

## Verification

EditMode TDD covers:

- full horizontal speed preservation when redirecting onto a wall;
- deterministic tangent selection for angled and head-on entry;
- wall-run vertical progression;
- two-second expiry;
- same-wall separation/cooldown rejection and opposite-wall acceptance;
- wall jump preserving tangent momentum while adding outward/upward velocity;
- one press being consumed by only one jump path.

Controller integration is verified by Unity compilation, zero console errors, the complete EditMode suite, and direct scene-serialization checks. Final acceptance is a user Play Mode feel gate:

- automatic entry without holding into the wall;
- no speed loss on contact;
- faster entry travels farther;
- moving away and Crouch detach cleanly;
- wall jump feels strong even while looking at the wall;
- same-wall reattachment is blocked briefly while opposite-wall chaining works;
- camera roll communicates the contacted side without discomfort;
- running, jumping, air strafe, slide, slide hop, slope behavior, and FOV remain unchanged.

## Future State Machine Compatibility

The future movement state machine will likely own grounded, airborne, sliding, wall-running, and animation-aware transitions. This change does not preempt that design. It establishes three compatible boundaries now: one Rigidbody writer, pure velocity math, and isolated wall-run lifecycle state. Those boundaries allow later state extraction without changing wall-run feel rules.
