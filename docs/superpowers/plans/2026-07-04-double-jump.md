# Momentum-Preserving Double Jump Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one momentum-preserving airborne jump that refreshes on ground or new wall contact without allowing continuous-wall infinite refreshes.

**Architecture:** Add a small physics-free `DoubleJumpState` for charge and wall-contact-edge tracking. Extend the existing jump resolver so ground and wall jumps remain ahead of double jump, and expose buffered-input consumption from `JumpGate`; `PlayerMovementController` supplies contact observations and applies the existing jump math.

**Tech Stack:** Unity 6000.5.2f1, C#, Rigidbody, new Input System 1.19.0, NUnit EditMode.

## Global Constraints

- Preserve complete horizontal momentum; double jump sets vertical velocity through `MovementMath.ApplyJump` using existing `jumpHeight = 1.4` and `gravity = -25` tuning.
- Ground contact refreshes continuously. Wall contact refreshes only on a no-contact-to-contact transition; continuous wall contact cannot refresh repeatedly.
- Jump priority is ground, then wall, then double jump.
- Ground and wall jumps do not consume the double-jump charge.
- Preserve run/sprint, coyote `0.1`, jump buffer `0.15`, Quake air strafe, slide/fall-line steering, slide hop, wall run, wall jump, slopes, FPS look, and speed FOV.
- `PlayerMovementController` remains the only writer of `Rigidbody.linearVelocity`.
- Do not add a full movement state machine, extra input binding, VFX, audio, animation, UI, configurable charge counts, or scene changes.
- Do not switch branches. Do not add `Co-Authored-By: Claude` trailers.
- Implementers edit code only. The controller performs Unity compilation and EditMode tests through `unity-mcp-skill`, using MCP only for verification.

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Runtime/DoubleJumpState.cs` | Create | Physics-free charge and wall-contact rising-edge latch |
| `Assets/Tests/EditMode/DoubleJumpStateTests.cs` | Create | Charge, ground refresh, wall latch, separation/recontact tests |
| `Assets/Scripts/Runtime/JumpGate.cs` | Modify | Expose buffered-input query and consumption without coyote requirement |
| `Assets/Tests/EditMode/JumpGateTests.cs` | Modify | Buffered-only consume regression |
| `Assets/Scripts/Runtime/MovementJumpResolver.cs` | Modify | Add `Double` source as final priority |
| `Assets/Tests/EditMode/MovementJumpResolverTests.cs` | Modify | Ground/wall/double priority tests |
| `Assets/Scripts/Runtime/PlayerMovementController.cs` | Modify | Observe contacts, resolve/consume double jump, apply jump velocity |
| `Assets/Tests/EditMode/PlayerMovementControllerTests.cs` | Modify | Airborne fallback and continuous-wall controller integration |

---

### Task 1: Double-Jump Charge and Wall-Contact Latch

**Files:**
- Create: `Assets/Scripts/Runtime/DoubleJumpState.cs`
- Create: `Assets/Tests/EditMode/DoubleJumpStateTests.cs`

**Interfaces:**
- Produces: `DoubleJumpState.IsAvailable`, `ObserveContacts(bool grounded, bool onWall)`, and `TryConsume()`.
- Consumes: no Unity scene or physics API.

- [ ] **Step 1: Write the failing state tests**

Create `Assets/Tests/EditMode/DoubleJumpStateTests.cs`:

```csharp
using NUnit.Framework;
using OneMoreTime;

public class DoubleJumpStateTests
{
    [Test]
    public void NewState_HasOneAvailableCharge()
    {
        var state = new DoubleJumpState();
        Assert.IsTrue(state.IsAvailable);
    }

    [Test]
    public void TryConsume_SucceedsOnceThenFails()
    {
        var state = new DoubleJumpState();
        Assert.IsTrue(state.TryConsume());
        Assert.IsFalse(state.TryConsume());
    }

    [Test]
    public void GroundContact_RefreshesConsumedCharge()
    {
        var state = new DoubleJumpState();
        state.TryConsume();

        state.ObserveContacts(true, false);

        Assert.IsTrue(state.IsAvailable);
    }

    [Test]
    public void ContinuousWallContact_DoesNotRefreshTwice()
    {
        var state = new DoubleJumpState();
        state.TryConsume();
        state.ObserveContacts(false, true);
        Assert.IsTrue(state.TryConsume());

        state.ObserveContacts(false, true);

        Assert.IsFalse(state.IsAvailable);
    }

    [Test]
    public void WallSeparationThenRecontact_RefreshesAgain()
    {
        var state = new DoubleJumpState();
        state.ObserveContacts(false, true);
        state.TryConsume();
        state.ObserveContacts(false, false);

        state.ObserveContacts(false, true);

        Assert.IsTrue(state.IsAvailable);
    }
}
```

- [ ] **Step 2: Run RED through the controller**

Use Unity MCP `refresh_unity` for script compilation, then `run_tests` in EditMode.

Expected: compilation fails because `DoubleJumpState` does not exist. The existing 40 tests remain unchanged.

- [ ] **Step 3: Implement the minimal state**

Create `Assets/Scripts/Runtime/DoubleJumpState.cs`:

```csharp
namespace OneMoreTime
{
    /// One airborne jump charge. Ground refreshes continuously; wall refreshes on contact entry.
    public sealed class DoubleJumpState
    {
        bool _wasOnWall;

        public bool IsAvailable { get; private set; } = true;

        public void ObserveContacts(bool grounded, bool onWall)
        {
            if (grounded || (onWall && !_wasOnWall))
                IsAvailable = true;

            _wasOnWall = onWall;
        }

        public bool TryConsume()
        {
            if (!IsAvailable) return false;
            IsAvailable = false;
            return true;
        }
    }
}
```

- [ ] **Step 4: Run GREEN**

Compile through Unity MCP, confirm zero console errors, then run the complete EditMode suite.

Expected: `45/45` passing, 0 failed, 0 skipped.

- [ ] **Step 5: Static self-review and commit**

Confirm `DoubleJumpState` contains no physics, input, Rigidbody, or scene references. Include Unity-generated `.meta` files.

```powershell
git add -- Assets/Scripts/Runtime/DoubleJumpState.cs Assets/Scripts/Runtime/DoubleJumpState.cs.meta Assets/Tests/EditMode/DoubleJumpStateTests.cs Assets/Tests/EditMode/DoubleJumpStateTests.cs.meta
git commit -m "feat: add double-jump charge state"
```

---

### Task 2: Buffered Input and Jump Priority

**Files:**
- Modify: `Assets/Scripts/Runtime/JumpGate.cs`
- Modify: `Assets/Tests/EditMode/JumpGateTests.cs`
- Modify: `Assets/Scripts/Runtime/MovementJumpResolver.cs`
- Modify: `Assets/Tests/EditMode/MovementJumpResolverTests.cs`

**Interfaces:**
- Consumes: existing `JumpGate` buffer timer and `MovementJumpSource` arbitration.
- Produces: `JumpGate.HasBufferedJump`, `JumpGate.TryConsumeBufferedJump()`, `MovementJumpSource.Double`, and `MovementJumpResolver.Choose(bool wallRunActive, bool groundAvailable, bool wallAvailable, bool doubleAvailable)`.

- [ ] **Step 1: Write failing buffered-input tests**

Append to `Assets/Tests/EditMode/JumpGateTests.cs`:

```csharp
[Test]
public void TryConsumeBufferedJump_DoesNotRequireCoyoteAndConsumesOnce()
{
    var gate = new JumpGate(0.1f, 0.15f);
    gate.PressJump();

    Assert.IsTrue(gate.HasBufferedJump);
    Assert.IsTrue(gate.TryConsumeBufferedJump());
    Assert.IsFalse(gate.HasBufferedJump);
    Assert.IsFalse(gate.TryConsumeBufferedJump());
}
```

- [ ] **Step 2: Write failing priority tests**

Append to `Assets/Tests/EditMode/MovementJumpResolverTests.cs`:

```csharp
[Test]
public void OnlyDoubleAvailable_SelectsDouble()
{
    Assert.AreEqual(MovementJumpSource.Double,
        MovementJumpResolver.Choose(false, false, false, true));
}

[Test]
public void GroundAvailable_BeatsDouble()
{
    Assert.AreEqual(MovementJumpSource.Ground,
        MovementJumpResolver.Choose(false, true, false, true));
}

[Test]
public void ActiveWallAvailable_BeatsDoubleAndGround()
{
    Assert.AreEqual(MovementJumpSource.Wall,
        MovementJumpResolver.Choose(true, true, true, true));
}
```

Update the three existing `MovementJumpResolver.Choose` calls in that test file by adding `false` as the final `doubleAvailable` argument. Do not change their expected results.

- [ ] **Step 3: Run RED**

Compile and run EditMode through Unity MCP.

Expected: compilation errors for missing `HasBufferedJump`, `TryConsumeBufferedJump`, `MovementJumpSource.Double`, and the four-argument resolver.

- [ ] **Step 4: Add buffered-only input consumption**

In `JumpGate.cs`, replace the current `CanConsumeJump` property with the following and add the new method after the existing `TryConsumeJump` overloads:

```csharp
public bool HasBufferedJump => _bufferTimer > 0f;
public bool CanConsumeJump => HasBufferedJump && _coyoteTimer > 0f;

public bool TryConsumeBufferedJump()
{
    if (!HasBufferedJump) return false;
    _bufferTimer = 0f;
    return true;
}
```

Do not alter `TryConsumeJump(bool wasAvailableAtTickStart)`; its contact-grace boundary behavior is already covered.

- [ ] **Step 5: Extend the jump resolver**

Replace `MovementJumpSource` and `Choose` in `MovementJumpResolver.cs` with:

```csharp
public enum MovementJumpSource { None, Ground, Wall, Double }

public static MovementJumpSource Choose(bool wallRunActive,
    bool groundAvailable, bool wallAvailable, bool doubleAvailable)
{
    if (wallRunActive && wallAvailable) return MovementJumpSource.Wall;
    if (groundAvailable) return MovementJumpSource.Ground;
    if (wallAvailable) return MovementJumpSource.Wall;
    if (doubleAvailable) return MovementJumpSource.Double;
    return MovementJumpSource.None;
}
```

Keep `ShouldExitWallRunBeforeJump` unchanged.

- [ ] **Step 6: Update the controller call for compilation only**

In `PlayerMovementController.FixedUpdate`, add `false` as the fourth argument to the existing resolver call:

```csharp
MovementJumpSource jumpSource = MovementJumpResolver.Choose(
    _wallRun.IsActive || wallJumpFromOwnedRun,
    _jumpGate.CanConsumeJump,
    wallJumpAvailable,
    false);
```

Task 3 replaces this temporary `false` with real double-jump availability.

- [ ] **Step 7: Run GREEN**

Compile, confirm zero console errors, and run all EditMode tests.

Expected: `49/49` passing, 0 failed, 0 skipped.

- [ ] **Step 8: Commit**

```powershell
git add -- Assets/Scripts/Runtime/JumpGate.cs Assets/Tests/EditMode/JumpGateTests.cs Assets/Scripts/Runtime/MovementJumpResolver.cs Assets/Tests/EditMode/MovementJumpResolverTests.cs Assets/Scripts/Runtime/PlayerMovementController.cs
git commit -m "feat: add buffered double-jump priority"
```

---

### Task 3: Controller Integration

**Files:**
- Modify: `Assets/Scripts/Runtime/PlayerMovementController.cs`
- Modify: `Assets/Tests/EditMode/PlayerMovementControllerTests.cs`

**Interfaces:**
- Consumes: `DoubleJumpState.ObserveContacts`, `DoubleJumpState.TryConsume`, `JumpGate.HasBufferedJump`, `JumpGate.TryConsumeBufferedJump`, and `MovementJumpSource.Double`.
- Produces: one functional airborne double jump with ground/new-wall refresh and preserved horizontal momentum.

- [ ] **Step 1: Add failing controller fallback test**

Append to `PlayerMovementControllerTests.cs`:

```csharp
[Test]
public void FixedUpdate_AirborneBufferedJump_ConsumesDoubleJumpOnce()
{
    PlayerMovementController controller = CreateController(Vector3.zero);
    JumpGate jumpGate = GetField<JumpGate>(controller, "_jumpGate");
    JumpGate wallGate = GetField<JumpGate>(controller, "_wallGate");
    DoubleJumpState doubleJump = GetField<DoubleJumpState>(controller, "_doubleJump");
    jumpGate.PressJump();
    wallGate.PressJump();

    InvokeFixedUpdate(controller);

    Assert.IsFalse(GetField<bool>(controller, "_grounded"));
    Assert.IsFalse(GetField<bool>(controller, "_onWall"));
    Assert.IsFalse(doubleJump.IsAvailable);
    Assert.IsFalse(jumpGate.HasBufferedJump);
    Assert.IsFalse(wallGate.HasBufferedJump);

    jumpGate.PressJump();
    wallGate.PressJump();
    InvokeFixedUpdate(controller);

    Assert.IsFalse(doubleJump.IsAvailable,
        "A second airborne press cannot create another charge without contact.");
    Assert.IsTrue(jumpGate.HasBufferedJump,
        "An unavailable double jump must not consume the buffered press.");
}
```

- [ ] **Step 2: Add failing continuous-wall refresh test**

Append to the same test file:

```csharp
[Test]
public void FixedUpdate_ContinuousWallContact_RefreshesOnlyOnEntry()
{
    PlayerMovementController controller = CreateController(Vector3.zero);
    CreateBox("DoubleJumpWall", new Vector3(0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
    Physics.SyncTransforms();
    DoubleJumpState doubleJump = GetField<DoubleJumpState>(controller, "_doubleJump");
    doubleJump.TryConsume();
    SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);

    InvokeFixedUpdate(controller);
    Assert.IsTrue(GetField<bool>(controller, "_onWall"));
    Assert.IsTrue(doubleJump.TryConsume(), "Wall-contact entry must refresh the charge.");

    InvokeFixedUpdate(controller);

    Assert.IsFalse(doubleJump.IsAvailable,
        "Continuous wall contact must not refresh the consumed charge again.");
}
```

- [ ] **Step 3: Run RED**

Compile and run EditMode.

Expected: tests compile, then fail when reflection cannot find the `_doubleJump` field. No production double-jump path exists yet.

- [ ] **Step 4: Add controller state and contact observation**

Add the field beside existing movement state:

```csharp
DoubleJumpState _doubleJump;
```

Initialize it in `Awake` after the jump gates:

```csharp
_doubleJump = new DoubleJumpState();
```

In `FixedUpdate`, immediately after `hasWallContact` and `_onWall` are calculated, observe both contact types:

```csharp
_doubleJump.ObserveContacts(_grounded, hasWallContact);
```

Use raw `hasWallContact`, not `_onWall`, so a valid wall remains a refresh source even on a grounded tick. The state latch prevents continuous-wall refresh.

- [ ] **Step 5: Feed real double availability into arbitration**

Before the resolver call, calculate:

```csharp
bool doubleJumpAvailable = !_grounded
    && _doubleJump.IsAvailable
    && _jumpGate.HasBufferedJump;
```

Replace the temporary fourth `false` argument with `doubleJumpAvailable`:

```csharp
MovementJumpSource jumpSource = MovementJumpResolver.Choose(
    _wallRun.IsActive || wallJumpFromOwnedRun,
    _jumpGate.CanConsumeJump,
    wallJumpAvailable,
    doubleJumpAvailable);
```

- [ ] **Step 6: Consume and apply double jump after wall jump**

Add this branch after the existing Wall branch:

```csharp
else if (jumpSource == MovementJumpSource.Double
    && _jumpGate.TryConsumeBufferedJump()
    && _doubleJump.TryConsume())
{
    _wallGate.CancelPendingJump();
    nextVel = MovementMath.ApplyJump(nextVel,
        MovementMath.JumpVelocity(config.jumpHeight, config.gravity));
    _sliding = false;
}
```

Ground and wall branches remain unchanged and therefore do not consume `_doubleJump`.

- [ ] **Step 7: Run GREEN**

Compile through Unity MCP, read the console for zero errors, and run the full EditMode suite.

Expected: `51/51` passing, 0 failed, 0 skipped.

- [ ] **Step 8: Static verification**

```powershell
rg -n "_rb\.linearVelocity\s*=" Assets/Scripts/Runtime
rg -n "wallSlideMaxFall|new Vector3\[\]" Assets/Scripts/Runtime
git diff --check
```

Expected:

- exactly one Rigidbody velocity writer, in `PlayerMovementController`;
- no sticky wall-slide field;
- no per-tick wall-probe array allocation;
- no whitespace errors.

- [ ] **Step 9: Commit**

```powershell
git add -- Assets/Scripts/Runtime/PlayerMovementController.cs Assets/Tests/EditMode/PlayerMovementControllerTests.cs
git commit -m "feat: add momentum-preserving double jump"
```

---

### Task 4: Feel Gate and Final Review

**Files:**
- No planned code or scene changes.

**Interfaces:**
- Consumes: complete double-jump implementation and Unity verification evidence.
- Produces: user feel approval and final whole-branch review verdict.

- [ ] **Step 1: User Play Mode verification**

The user verifies:

1. Ground jump, then Jump in air: second jump fires and does not reduce horizontal speed.
2. A third airborne Jump does nothing until ground or wall contact.
3. Wall touch or wall run refreshes one double jump.
4. Wall jump leaves the double jump available afterward.
5. Holding against one wall cannot generate repeated double-jump charges; separation and recontact refreshes it.
6. Ground/wall jump priority, air strafe, slide hop, wall-run momentum, camera roll, and FOV still feel unchanged.

- [ ] **Step 2: Final whole-branch review**

Generate a review package from the double-jump design base through HEAD. Dispatch the most capable reviewer using `superpowers:requesting-code-review`. Fix Critical and Important findings in one fix agent with focused RED/GREEN coverage, then re-review.

- [ ] **Step 3: Finish the branch**

After fresh Unity compilation and the full EditMode suite pass, use `superpowers:finishing-a-development-branch`. Do not switch, merge, push, or delete the branch without the user's explicit option selection.
