using NUnit.Framework;
using OneMoreTime;

public class JumpGateTests
{
    [Test]
    public void GroundedPress_Jumps()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, true);
        g.PressJump();
        Assert.IsTrue(g.TryConsumeJump());
    }

    [Test]
    public void SinglePress_JumpsOnlyOnce()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, true);
        g.PressJump();
        Assert.IsTrue(g.TryConsumeJump());
        // aynı grounded karede yeni basış yok → ikinci tüketim başarısız
        Assert.IsFalse(g.TryConsumeJump());
    }

    [Test]
    public void CoyoteWindow_AllowsJumpShortlyAfterLeavingGround()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, true);   // zemine değdi → coyote dolu
        g.Tick(0.05f, false);  // 0.05s havada (< 0.1 coyote)
        g.PressJump();
        Assert.IsTrue(g.TryConsumeJump());
    }

    [Test]
    public void CoyoteExpired_NoJump()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, true);
        g.Tick(0.2f, false);   // 0.2s havada (> 0.1 coyote)
        g.PressJump();
        Assert.IsFalse(g.TryConsumeJump());
    }

    [Test]
    public void JumpBuffer_PressBeforeLanding_JumpsOnLand()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, false);  // havada
        g.PressJump();         // inişten hemen önce basış
        g.Tick(0.05f, true);   // 0.05s sonra iniş (< 0.15 buffer)
        Assert.IsTrue(g.TryConsumeJump());
    }

    [Test]
    public void JumpBuffer_Expired_NoJump()
    {
        var g = new JumpGate(0.1f, 0.15f);
        g.Tick(0.02f, false);
        g.PressJump();
        g.Tick(0.2f, false);   // 0.2s > 0.15 buffer, hâlâ havada
        g.Tick(0.02f, true);   // sonra iniş
        Assert.IsFalse(g.TryConsumeJump());
    }
}
