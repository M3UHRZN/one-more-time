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
