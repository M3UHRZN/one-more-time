# Momentum-Preserving Double Jump Design

## Goal

Add one airborne jump charge to the existing Rigidbody movement controller. Ground or valid wall contact refreshes the charge. The second jump preserves all horizontal momentum and resets vertical speed to the normal configured jump speed.

## Behavior

- The player has at most one double-jump charge.
- Any grounded physics tick refreshes the charge.
- Any valid wall-contact physics tick refreshes the charge, including contact that starts or sustains a wall run.
- Ground and wall jumps do not consume the double-jump charge. The player can double jump after either one.
- Jump resolution order is ground jump, then wall jump, then double jump.
- Double jump is available only while airborne when neither a ground jump nor a wall jump can be consumed.
- Consuming the double jump removes the charge until the next ground or wall contact.
- Double jump uses `MovementMath.ApplyJump` with the existing `jumpHeight` and `gravity`, so vertical velocity is replaced by the normal jump velocity and horizontal velocity is preserved exactly.
- Existing jump buffer behavior applies: a buffered Jump may consume the double jump once the controller reaches the airborne fallback path, provided the charge is still available.
- Slide, air strafe, wall run, wall jump, coyote time, jump buffering, FPS camera, and FOV behavior remain unchanged.

## Architecture

Use a small physics-free `DoubleJumpState` rather than expanding `JumpGate` or introducing the future full movement state machine.

`DoubleJumpState` owns one boolean charge and exposes three operations:

- refresh from ground or wall contact;
- query availability;
- consume once.

`PlayerMovementController` supplies contact observations and resolves jump priority. It remains the only writer of `Rigidbody.linearVelocity`.

This boundary is intentionally small. A future movement state machine can own or wrap the same state without changing double-jump rules.

## Data Flow

1. Probe ground and all wall contacts.
2. Refresh `DoubleJumpState` when grounded or touching a valid wall.
3. Tick the existing ground and wall jump gates.
4. Resolve ground and wall jumps exactly as today.
5. If neither path consumes Jump and the player is airborne, consume `DoubleJumpState` and apply the normal jump velocity.
6. Preserve the existing horizontal velocity and clear slide state after a successful double jump.

## Tests

EditMode tests cover:

- initial charge availability;
- a single consume succeeds and the second consume fails;
- ground contact refreshes a consumed charge;
- wall contact refreshes a consumed charge;
- ground and wall jump sources retain priority over double jump;
- double jump preserves horizontal momentum and resets vertical speed through the existing jump math;
- controller integration consumes the charge only on the airborne fallback path.

Unity verification requires zero compile-console errors and the complete EditMode suite passing. Final feel verification checks that double jump works after ground jump and wall jump, cannot be repeated without contact, and does not reduce earned movement speed.

## Out of Scope

- Different jump height or gravity for the second jump.
- Extra VFX, audio, animation, UI indicators, or input bindings.
- Multiple configurable air-jump charges.
- A full movement state machine.

