# Titanfall-Style Wall Run Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace sticky wall sliding with an automatic two-second wall run that preserves all incoming horizontal momentum and produces a strong momentum-preserving wall jump.

**Architecture:** `PlayerMovementController` remains the only Rigidbody velocity writer. A pure `WallRunState` class owns the isolated wall-run lifecycle for future state-machine migration, while `MovementMath` owns all stateless tangent, wall-run velocity, detach, and wall-jump calculations.

**Tech Stack:** Unity 6000.5.2f1, C#, Rigidbody, Unity Input System 1.19.0, NUnit EditMode, Unity MCP for controller-run compile/test/scene verification.

## Global Constraints

- Work on the current `fature/movement-system-2` branch. Do not create or switch branches.
- Do not add a full class-per-state movement state machine in this change.
- `PlayerMovementController` is the only writer of `Rigidbody.linearVelocity`.
- Wall run starts automatically while airborne; holding input toward the wall is not required.
- Preserve the complete incoming horizontal speed. Never clamp wall-run entry or wall-jump tangent momentum to run, sprint, or air-speed values.
- Wall run lasts at most `2.0` seconds and ends at a gentle `-3 m/s` vertical speed.
- Every exit blocks the same wall until physical separation. Wall jump additionally applies a `0.2` second same-wall cooldown; opposite walls remain available.
- Moving away with `dot(wishDirection, wallNormal) > 0.25`, Crouch, ground contact, contact loss beyond `0.1` seconds, Jump, or timeout ends wall run.
- Wall jump preserves all tangent velocity and adds the existing configured outward/upward impulse.
- Wall-run camera roll is `8` degrees toward the wall.
- Remove sticky `wallSlideMaxFall`; do not add wall attraction, wall friction, wall-run acceleration, animation, audio, VFX, stamina, or weapons work.
- Preserve run/sprint, coyote `0.1`, jump buffer `0.15`, Quake air strafe, slide/fall-line steering, slide hop, slopes, FPS look, and speed FOV.
- Implementers write code/tests only. The controller session performs Unity MCP compilation, tests, scene save, and serialization checks.
- Do not stage or commit unrelated user/editor changes. Do not add a `Co-Authored-By: Claude` trailer.

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Runtime/WallRunState.cs` | Create | Isolated wall-run lifecycle, duration, contact grace, separation and cooldown locks |
| `Assets/Tests/EditMode/WallRunStateTests.cs` | Create | Pure lifecycle tests |
| `Assets/Scripts/Runtime/MovementMath.cs` | Modify | Tangent selection, wall-run velocity, detach decision, existing wall-jump math |
| `Assets/Tests/EditMode/MovementMathTests.cs` | Modify | Pure wall-run vector tests |
| `Assets/Scripts/Runtime/MovementConfig.cs` | Modify | Wall-run tuning; remove sticky slide cap |
| `Assets/Scripts/Runtime/PlayerMovementController.cs` | Modify | Detection, automatic entry, velocity routing, exits, jump integration, public camera state |
| `Assets/Scripts/Runtime/FirstPersonLook.cs` | Modify | Signed 8-degree wall-run roll |
| `Assets/Scenes/SampleScene.unity` | Modify through controller MCP | Serialize new config/camera fields; retain test wall |

---

### Task 1: Pure Wall-Run Lifecycle State (TDD)

**Files:**
- Create: `Assets/Scripts/Runtime/WallRunState.cs`
- Create: `Assets/Tests/EditMode/WallRunStateTests.cs`

**Interfaces:**
- Produces: `WallRunState(float duration, float contactGrace, float sameWallCooldown)`
- Produces: `bool TryEnter(Vector3 wallNormal, Vector3 direction, float horizontalSpeed, float entryVerticalSpeed)`
- Produces: `void Tick(float dt, bool hasWallContact, Vector3 contactNormal)`
- Produces: `void Exit(bool wallJump)` and `void LockWallAfterJump(Vector3 wallNormal)`
- Produces: `bool CanEnter(Vector3 wallNormal)`
- Produces read-only properties: `IsActive`, `Elapsed`, `WallNormal`, `Direction`, `HorizontalSpeed`, `EntryVerticalSpeed`

- [ ] **Step 1: Write the failing lifecycle tests**

Create `Assets/Tests/EditMode/WallRunStateTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using OneMoreTime;

public class WallRunStateTests
{
    [Test]
    public void TryEnter_StoresMomentumAndContact()
    {
        var state = new WallRunState(2f, 0.1f, 0.2f);

        bool entered = state.TryEnter(Vector3.right, Vector3.forward, 12f, 4f);

        Assert.IsTrue(entered);
        Assert.IsTrue(state.IsActive);
        Assert.AreEqual(Vector3.right, state.WallNormal);
        Assert.AreEqual(Vector3.forward, state.Direction);
        Assert.AreEqual(12f, state.HorizontalSpeed, 0.0001f);
        Assert.AreEqual(4f, state.EntryVerticalSpeed, 0.0001f);
    }

    [Test]
    public void DurationExpiry_BlocksSameWallUntilSeparation()
    {
        var state = new WallRunState(2f, 0.1f, 0.2f);
        state.TryEnter(Vector3.right, Vector3.forward, 10f, 0f);

        state.Tick(2f, true, Vector3.right);

        Assert.IsFalse(state.IsActive);
        Assert.IsFalse(state.CanEnter(Vector3.right));

        state.Tick(0.02f, false, Vector3.zero);
        Assert.IsTrue(state.CanEnter(Vector3.right));
    }

    [Test]
    public void WallJumpExit_LocksSameWallForCooldownAfterSeparation()
    {
        var state = new WallRunState(2f, 0.1f, 0.2f);
        state.TryEnter(Vector3.right, Vector3.forward, 10f, 0f);
        state.Exit(true);

        state.Tick(0.01f, false, Vector3.zero);
        Assert.IsFalse(state.CanEnter(Vector3.right));

        state.Tick(0.2f, false, Vector3.zero);
        Assert.IsTrue(state.CanEnter(Vector3.right));
    }

    [Test]
    public void OppositeWall_IsAvailableDuringSameWallLock()
    {
        var state = new WallRunState(2f, 0.1f, 0.2f);
        state.LockWallAfterJump(Vector3.right);

        Assert.IsFalse(state.CanEnter(Vector3.right));
        Assert.IsTrue(state.CanEnter(Vector3.left));
    }
}
```

- [ ] **Step 2: Verify RED through the controller**

Controller runs Unity script import/compile.

Expected: compilation fails only because `WallRunState` does not exist. Record the exact CS0246/CS0103 diagnostics in `.superpowers/sdd/wall-run-task-1-report.md`.

- [ ] **Step 3: Implement the minimal lifecycle state**

Create `Assets/Scripts/Runtime/WallRunState.cs`:

```csharp
using UnityEngine;

namespace OneMoreTime
{
    /// Wall-run yaşam döngüsü. Fizik/input okumaz; gelecekteki movement state machine'e taşınabilir.
    public sealed class WallRunState
    {
        const float SameWallDot = 0.9f;

        readonly float _duration;
        readonly float _contactGrace;
        readonly float _sameWallCooldown;

        float _contactGraceRemaining;
        float _sameWallCooldownRemaining;
        bool _requiresSeparation;
        Vector3 _blockedWallNormal;
        Vector3 _cooldownWallNormal;

        public bool IsActive { get; private set; }
        public float Elapsed { get; private set; }
        public Vector3 WallNormal { get; private set; }
        public Vector3 Direction { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public float EntryVerticalSpeed { get; private set; }

        public WallRunState(float duration, float contactGrace, float sameWallCooldown)
        {
            _duration = duration;
            _contactGrace = contactGrace;
            _sameWallCooldown = sameWallCooldown;
        }

        public bool CanEnter(Vector3 wallNormal)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            if (n == Vector3.zero) return false;
            if (_requiresSeparation && Vector3.Dot(n, _blockedWallNormal) >= SameWallDot) return false;
            return _sameWallCooldownRemaining <= 0f
                || Vector3.Dot(n, _cooldownWallNormal) < SameWallDot;
        }

        public bool TryEnter(Vector3 wallNormal, Vector3 direction, float horizontalSpeed, float entryVerticalSpeed)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            Vector3 tangent = new Vector3(direction.x, 0f, direction.z).normalized;
            if (IsActive || !CanEnter(n) || tangent == Vector3.zero || horizontalSpeed <= 0.001f)
                return false;

            IsActive = true;
            Elapsed = 0f;
            WallNormal = n;
            Direction = tangent;
            HorizontalSpeed = horizontalSpeed;
            EntryVerticalSpeed = Mathf.Max(0f, entryVerticalSpeed);
            _contactGraceRemaining = _contactGrace;
            return true;
        }

        public void Tick(float dt, bool hasWallContact, Vector3 contactNormal)
        {
            _sameWallCooldownRemaining = Mathf.Max(0f, _sameWallCooldownRemaining - dt);
            Vector3 contact = HorizontalNormal(contactNormal);

            if (_requiresSeparation
                && (!hasWallContact || contact == Vector3.zero
                    || Vector3.Dot(contact, _blockedWallNormal) < SameWallDot))
            {
                _requiresSeparation = false;
            }

            if (!IsActive) return;

            Elapsed += dt;
            bool sameContact = hasWallContact && contact != Vector3.zero
                && Vector3.Dot(contact, WallNormal) >= SameWallDot;
            _contactGraceRemaining = sameContact
                ? _contactGrace
                : Mathf.Max(0f, _contactGraceRemaining - dt);

            if (Elapsed >= _duration || _contactGraceRemaining <= 0f)
                Exit(false);
        }

        public void Exit(bool wallJump)
        {
            if (!IsActive) return;
            Vector3 exitedWall = WallNormal;
            IsActive = false;
            BlockUntilSeparation(exitedWall);
            if (wallJump) StartCooldown(exitedWall);
        }

        public void LockWallAfterJump(Vector3 wallNormal)
        {
            Vector3 n = HorizontalNormal(wallNormal);
            if (n == Vector3.zero) return;
            BlockUntilSeparation(n);
            StartCooldown(n);
        }

        void BlockUntilSeparation(Vector3 wallNormal)
        {
            _requiresSeparation = true;
            _blockedWallNormal = wallNormal;
        }

        void StartCooldown(Vector3 wallNormal)
        {
            _cooldownWallNormal = wallNormal;
            _sameWallCooldownRemaining = _sameWallCooldown;
        }

        static Vector3 HorizontalNormal(Vector3 normal)
        {
            Vector3 n = new Vector3(normal.x, 0f, normal.z);
            return n.sqrMagnitude > 0.0001f ? n.normalized : Vector3.zero;
        }
    }
}
```

- [ ] **Step 4: Verify GREEN**

Controller compiles and runs EditMode.

Expected: existing `22` tests plus `4` new lifecycle tests = `26/26` passing, zero console errors.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/Scripts/Runtime/WallRunState.cs Assets/Scripts/Runtime/WallRunState.cs.meta Assets/Tests/EditMode/WallRunStateTests.cs Assets/Tests/EditMode/WallRunStateTests.cs.meta
git commit -m "feat: add state-machine-ready wall-run lifecycle"
```

---

### Task 2: Wall-Run Vector Math (TDD)

**Files:**
- Modify: `Assets/Scripts/Runtime/MovementMath.cs`
- Modify: `Assets/Tests/EditMode/MovementMathTests.cs`

**Interfaces:**
- Produces: `WallTangentDirection(Vector3 horizontalVelocity, Vector3 wishDir, Vector3 cameraRight, Vector3 wallNormal)`
- Produces: `WallRunVelocity(Vector3 tangentDirection, float horizontalSpeed, float entryVerticalSpeed, float elapsed, float duration, float endFallSpeed)`
- Produces: `ShouldDetachFromWall(Vector3 wishDir, Vector3 wallNormal, float threshold)`
- Retains: `WallJumpVelocity(Vector3 velocity, Vector3 wallNormal, float pushSpeed, float jumpVelocity)`

- [ ] **Step 1: Add the six failing math tests**

Append to `MovementMathTests`:

```csharp
[Test]
public void WallTangentDirection_AngledEntry_UsesVelocityProjection()
{
    Vector3 r = MovementMath.WallTangentDirection(
        new Vector3(10f, 0f, 6f), Vector3.zero, Vector3.forward, Vector3.right);
    Assert.AreEqual(Vector3.forward, r);
}

[Test]
public void WallTangentDirection_HeadOnEntry_UsesWishDirection()
{
    Vector3 r = MovementMath.WallTangentDirection(
        Vector3.left * 10f, Vector3.forward, Vector3.back, Vector3.right);
    Assert.AreEqual(Vector3.forward, r);
}

[Test]
public void WallTangentDirection_AmbiguousInput_UsesCameraRightFallback()
{
    Vector3 r = MovementMath.WallTangentDirection(
        Vector3.back * 10f, Vector3.zero, Vector3.right, Vector3.forward);
    Assert.AreEqual(Vector3.right, r);
}

[Test]
public void WallRunVelocity_PreservesFullHorizontalSpeed()
{
    Vector3 r = MovementMath.WallRunVelocity(
        Vector3.forward, 14f, 4f, 0f, 2f, 3f);
    Assert.AreEqual(14f, new Vector3(r.x, 0f, r.z).magnitude, 0.0001f);
    Assert.AreEqual(4f, r.y, 0.0001f);
}

[Test]
public void WallRunVelocity_AtDuration_ReachesGentleFall()
{
    Vector3 r = MovementMath.WallRunVelocity(
        Vector3.forward, 8f, 0f, 2f, 2f, 3f);
    Assert.AreEqual(-3f, r.y, 0.0001f);
}

[Test]
public void ShouldDetachFromWall_OnlyWhenWishPointsAwayPastThreshold()
{
    Assert.IsTrue(MovementMath.ShouldDetachFromWall(Vector3.right, Vector3.right, 0.25f));
    Assert.IsFalse(MovementMath.ShouldDetachFromWall(Vector3.forward, Vector3.right, 0.25f));
    Assert.IsFalse(MovementMath.ShouldDetachFromWall(Vector3.zero, Vector3.right, 0.25f));
}
```

- [ ] **Step 2: Verify RED through the controller**

Expected: compilation fails only because the three new `MovementMath` methods do not exist. Record diagnostics in `.superpowers/sdd/wall-run-task-2-report.md`.

- [ ] **Step 3: Add the minimal math methods**

Insert before `WallJumpVelocity` in `MovementMath.cs`:

```csharp
/// Duvar boyunca kararlı yön: önce momentum, sonra input, son çare kamera sağı.
public static Vector3 WallTangentDirection(Vector3 horizontalVelocity, Vector3 wishDir,
    Vector3 cameraRight, Vector3 wallNormal)
{
    Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
    if (n == Vector3.zero) return Vector3.zero;

    Vector3 source = Vector3.ProjectOnPlane(
        new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z), n);
    if (source.sqrMagnitude < 0.0001f)
        source = Vector3.ProjectOnPlane(new Vector3(wishDir.x, 0f, wishDir.z), n);
    if (source.sqrMagnitude < 0.0001f)
        source = Vector3.ProjectOnPlane(new Vector3(cameraRight.x, 0f, cameraRight.z), n);
    return source.sqrMagnitude > 0.0001f ? source.normalized : Vector3.zero;
}

/// Korunan yatay hız + iki saniye içinde nazik düşüşe geçen dikey hız.
public static Vector3 WallRunVelocity(Vector3 tangentDirection, float horizontalSpeed,
    float entryVerticalSpeed, float elapsed, float duration, float endFallSpeed)
{
    Vector3 tangent = new Vector3(tangentDirection.x, 0f, tangentDirection.z).normalized;
    float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
    float vertical = Mathf.Lerp(Mathf.Max(0f, entryVerticalSpeed), -Mathf.Abs(endFallSpeed), t);
    Vector3 horizontal = tangent * horizontalSpeed;
    return new Vector3(horizontal.x, vertical, horizontal.z);
}

public static bool ShouldDetachFromWall(Vector3 wishDir, Vector3 wallNormal, float threshold)
{
    if (wishDir.sqrMagnitude < 0.0001f) return false;
    Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
    return n != Vector3.zero && Vector3.Dot(wishDir.normalized, n) > threshold;
}
```

- [ ] **Step 4: Verify GREEN**

Controller compiles and runs EditMode.

Expected: `26 + 6 = 32/32` passing, zero console errors. Existing wall-jump tests must remain unchanged and passing.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/Scripts/Runtime/MovementMath.cs Assets/Tests/EditMode/MovementMathTests.cs
git commit -m "feat: add momentum-preserving wall-run math"
```

---

### Task 3: Automatic Wall Run and Momentum-Preserving Jump Integration

**Files:**
- Modify: `Assets/Scripts/Runtime/MovementConfig.cs`
- Modify: `Assets/Scripts/Runtime/PlayerMovementController.cs`

**Interfaces:**
- Consumes: Task 1 `WallRunState`
- Consumes: Task 2 wall-run math methods
- Produces: `public bool IsWallRunning`
- Produces: `public float WallRunSide`

- [ ] **Step 1: Replace sticky wall configuration**

Replace the existing Wall block in `MovementConfig.cs`:

```csharp
[Header("Wall")]
public float wallProbe = 0.15f;          // m, kapsül yüzeyinden duvar arama mesafesi
public float wallRunDuration = 2f;       // s, tek temasın azami wall-run süresi
public float sameWallCooldown = 0.2f;    // s, wall-jump sonrası aynı duvara dönüş kilidi
public float wallRunEndFallSpeed = 3f;   // m/s, sürenin sonunda nazik düşüş
public float wallJumpPush = 7f;          // m/s, duvardan dışa itme
```

Delete `wallSlideMaxFall`.

- [ ] **Step 2: Add controller state and non-allocating probe directions**

Add beside controller fields:

```csharp
static readonly Vector3[] WallProbeLocalDirections =
{
    Vector3.forward, Vector3.right, Vector3.back, Vector3.left
};

WallRunState _wallRun;
Vector3 _lastHorizontalVelocity;
```

Add public camera-facing state beside `HorizontalSpeed`/`IsSliding`:

```csharp
public bool IsWallRunning => _wallRun != null && _wallRun.IsActive;
public float WallRunSide
{
    get
    {
        if (!IsWallRunning) return 0f;
        Vector3 right = cameraTransform ? cameraTransform.right : transform.right;
        return Mathf.Sign(Vector3.Dot(_wallRun.WallNormal, right));
    }
}
```

In `Awake`, after both `JumpGate` instances:

```csharp
_wallRun = new WallRunState(config.wallRunDuration, config.coyoteTime, config.sameWallCooldown);
```

- [ ] **Step 3: Tick, exit, and automatically enter wall run**

Keep ground/wall probing and gate ticks, naming the current hit `probedWallNormal`:

```csharp
_grounded = ProbeGround(out Vector3 groundNormal);
_jumpGate.Tick(dt, _grounded);
_onWall = !_grounded && ProbeWall(out Vector3 probedWallNormal);
if (_onWall) _wallNormal = probedWallNormal;
_wallGate.Tick(dt, _onWall);
_wallRun.Tick(dt, _onWall, probedWallNormal);
```

After reading `wish`, `crouchHeld`, and the current horizontal velocity, add:

```csharp
if (_wallRun.IsActive
    && (_grounded || crouchHeld
        || MovementMath.ShouldDetachFromWall(wish, _wallRun.WallNormal, 0.25f)))
{
    _wallRun.Exit(false);
}

if (!_wallRun.IsActive && !_grounded && _onWall && !crouchHeld
    && !MovementMath.ShouldDetachFromWall(wish, probedWallNormal, 0.25f)
    && _wallRun.CanEnter(probedWallNormal))
{
    Vector3 entryVelocity = _lastHorizontalVelocity.sqrMagnitude > 0.0001f
        ? _lastHorizontalVelocity
        : horiz;
    float entrySpeed = Mathf.Max(entryVelocity.magnitude, horiz.magnitude);
    Vector3 fallbackRight = cameraTransform ? cameraTransform.right : transform.right;
    Vector3 direction = MovementMath.WallTangentDirection(
        entryVelocity, wish, fallbackRight, probedWallNormal);
    _wallRun.TryEnter(probedWallNormal, direction, entrySpeed, v.y);
}
```

This intentionally uses the previous commanded horizontal velocity, captured before collision response can erase head-on momentum.

- [ ] **Step 4: Route active wall-run velocity**

Insert the wall-run branch between grounded movement and ordinary air movement:

```csharp
else if (_wallRun.IsActive)
{
    nextVel = MovementMath.WallRunVelocity(
        _wallRun.Direction,
        _wallRun.HorizontalSpeed,
        _wallRun.EntryVerticalSpeed,
        _wallRun.Elapsed,
        config.wallRunDuration,
        config.wallRunEndFallSpeed);
}
else
{
    Vector3 wishDir = wish.sqrMagnitude > 0.0001f ? wish.normalized : Vector3.zero;
    horiz = MovementMath.AirAccelerate(horiz, wishDir, wish.magnitude * config.runSpeed,
        config.airAccel, config.airSpeedCap, dt);
    nextVel = new Vector3(horiz.x, v.y + config.gravity * dt, horiz.z);
}
```

Delete the old `wallSlideMaxFall` clamp.

- [ ] **Step 5: Preserve wall-run momentum during jump and consume once**

Replace jump consumption with:

```csharp
if (_jumpGate.TryConsumeJump())
{
    _wallGate.CancelPendingJump();
    if (_wallRun.IsActive) _wallRun.Exit(false);
    nextVel = MovementMath.ApplyJump(nextVel, MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
    _sliding = false;
}
else if ((_wallRun.IsActive || _wallRun.CanEnter(_wallNormal))
    && _wallGate.TryConsumeJump())
{
    _jumpGate.CancelPendingJump();
    Vector3 jumpNormal = _wallRun.IsActive ? _wallRun.WallNormal : _wallNormal;
    if (_wallRun.IsActive)
        _wallRun.Exit(true);
    else
        _wallRun.LockWallAfterJump(jumpNormal);
    nextVel = MovementMath.WallJumpVelocity(nextVel, jumpNormal,
        config.wallJumpPush, MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
}
```

At the end of `FixedUpdate`, replace the existing horizontal-speed assignment with:

```csharp
_rb.linearVelocity = nextVel;
_lastHorizontalVelocity = new Vector3(nextVel.x, 0f, nextVel.z);
HorizontalSpeed = _lastHorizontalVelocity.magnitude;
```

- [ ] **Step 6: Remove the per-tick wall-probe allocation**

Replace `ProbeWall` with:

```csharp
bool ProbeWall(out Vector3 normal)
{
    Vector3 center = transform.position + _capsule.center;
    float dist = _capsule.radius + config.wallProbe;
    foreach (Vector3 localDirection in WallProbeLocalDirections)
    {
        Vector3 direction = transform.TransformDirection(localDirection);
        if (Physics.Raycast(center, direction, out var hit, dist,
                groundMask, QueryTriggerInteraction.Ignore)
            && Mathf.Abs(hit.normal.y) < 0.3f)
        {
            normal = hit.normal;
            return true;
        }
    }
    normal = Vector3.zero;
    return false;
}
```

- [ ] **Step 7: Controller verification**

Controller imports/compiles scripts, checks zero console errors, and runs EditMode.

Expected: `32/32` passing. Direct static scan confirms:

- no `wallSlideMaxFall` reference;
- only one `_rb.linearVelocity =` writer;
- no `new Vector3[]` or array initializer inside `ProbeWall`;
- wall entry uses `_lastHorizontalVelocity` and never `runSpeed`/`sprintSpeed` as a clamp.

- [ ] **Step 8: Commit**

```powershell
git add -- Assets/Scripts/Runtime/MovementConfig.cs Assets/Scripts/Runtime/PlayerMovementController.cs
git commit -m "feat: replace sticky wall slide with automatic wall run"
```

---

### Task 4: Wall-Run Camera Roll and Scene Serialization

**Files:**
- Modify: `Assets/Scripts/Runtime/FirstPersonLook.cs`
- Modify through controller MCP: `Assets/Scenes/SampleScene.unity`

**Interfaces:**
- Consumes: `PlayerMovementController.IsWallRunning`
- Consumes: `PlayerMovementController.WallRunSide`

- [ ] **Step 1: Add the wall-run camera tuning field**

Add beneath existing slide camera fields:

```csharp
[SerializeField] float wallRunTilt = 8f;    // derece, temas edilen duvara doğru roll
```

- [ ] **Step 2: Combine slide and wall-run roll targets**

Replace the current target-roll calculation in `Update` with:

```csharp
bool sliding = controller && controller.IsSliding;
bool wallRunning = controller && controller.IsWallRunning;
float targetY = sliding ? _eyeHeight - slideEyeDrop : _eyeHeight;
float targetRoll = sliding
    ? slideTilt
    : wallRunning ? controller.WallRunSide * wallRunTilt : 0f;
```

Keep the existing smoothing, camera position assignment, and final `Quaternion.Euler(_pitch, 0f, _roll)`.

- [ ] **Step 3: Compile before scene save**

Controller imports/compiles and verifies zero console errors before touching the scene.

- [ ] **Step 4: Save and verify scene serialization**

Controller uses Unity MCP only to save `SampleScene.unity`, then verifies YAML directly:

- Player config serializes `wallProbe: 0.15`, `wallRunDuration: 2`, `sameWallCooldown: 0.2`, `wallRunEndFallSpeed: 3`, `wallJumpPush: 7`.
- `wallSlideMaxFall` is absent.
- `FirstPersonLook.wallRunTilt` serializes as `8`.
- `WallJumpTestWall` remains at position `(3.5, 17.5, 20)` and scale `(1, 8, 12)`.
- No `PhysicsMaterial`/`(Instance)` block or missing-script residue appears.

- [ ] **Step 5: Full automated verification**

Controller runs EditMode.

Expected: `32/32` passing, zero console errors.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/Scripts/Runtime/FirstPersonLook.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: add signed wall-run camera roll"
```

---

### Task 5: Human Feel Gate and Whole-Branch Review

**Files:**
- No code changes unless the feel gate finds a behavioral defect.

- [ ] **Step 1: User Play Mode checklist**

The user verifies:

1. Airborne wall contact starts wall run without holding input into the wall.
2. Head-on or angled contact does not erase speed; high-speed entry travels farther than low-speed entry.
3. The run ends after two seconds and cannot restart on the same wall until separation.
4. Moving away and Crouch detach immediately.
5. Wall jump preserves wall-tangent speed and adds strong outward/upward motion even while looking at the wall.
6. Same-wall reattachment is briefly blocked; opposite-wall chaining works.
7. Camera rolls 8 degrees toward the wall and returns cleanly.
8. Run, jump, air strafe, slide, slide hop, slope behavior, FPS look, and FOV retain their approved feel.

- [ ] **Step 2: Fix behavioral defects only through RED/GREEN tests**

For each reproducible defect, add or adapt the smallest EditMode test that fails for the observed cause, verify RED through controller Unity compilation/test, apply the minimal fix, and verify GREEN. Tuning-only changes use serialized fields and require a fresh Play Mode feel check.

- [ ] **Step 3: Final whole-branch review**

After human approval, generate one review package from `git merge-base main HEAD` to `HEAD`. Dispatch the most capable reviewer with the approved design, this plan, automated evidence, and any per-task Minor findings. Resolve all Critical/Important findings through one fix subagent and re-review.

- [ ] **Step 4: Finish the branch**

Run `superpowers:finishing-a-development-branch` only after final review is clean and tests are freshly green. Preserve the current branch/workspace unless the user explicitly chooses merge, PR, keep, or discard.
