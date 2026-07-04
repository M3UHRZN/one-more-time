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
            MovementJumpResolver.Choose(true, true, true, false));
    }

    [Test]
    public void NoActiveWallRun_PrioritizesGroundWhenBothAreAvailable()
    {
        Assert.AreEqual(MovementJumpSource.Ground,
            MovementJumpResolver.Choose(false, true, true, false));
    }

    [Test]
    public void OnlyWallAvailable_SelectsWall()
    {
        Assert.AreEqual(MovementJumpSource.Wall,
            MovementJumpResolver.Choose(false, false, true, false));
    }

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
}
