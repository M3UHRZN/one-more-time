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
