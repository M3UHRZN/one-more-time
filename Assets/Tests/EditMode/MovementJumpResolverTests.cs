using NUnit.Framework;
using OneMoreTime;

public class MovementJumpResolverTests
{
    [Test]
    public void ActiveWallJump_DefersSimultaneousNonJumpExit()
    {
        Assert.IsFalse(MovementJumpResolver.ShouldExitWallRunBeforeJump(true, true, true),
            "A consumable active wall jump must defer a simultaneous grounded/crouch/away exit.");
    }

    [Test]
    public void ActiveWallRun_PrioritizesWallOverGroundCoyote()
    {
        Assert.AreEqual(MovementJumpSource.Wall,
            MovementJumpResolver.Choose(true, true, true));
    }

    [Test]
    public void NoActiveWallRun_PrioritizesGroundWhenBothAreAvailable()
    {
        Assert.AreEqual(MovementJumpSource.Ground,
            MovementJumpResolver.Choose(false, true, true));
    }

    [Test]
    public void OnlyWallAvailable_SelectsWall()
    {
        Assert.AreEqual(MovementJumpSource.Wall,
            MovementJumpResolver.Choose(false, false, true));
    }
}
