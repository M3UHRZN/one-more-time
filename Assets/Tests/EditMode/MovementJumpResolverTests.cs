using NUnit.Framework;
using OneMoreTime;

public class MovementJumpResolverTests
{
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
