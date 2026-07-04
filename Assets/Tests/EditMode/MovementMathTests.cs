using NUnit.Framework;
using UnityEngine;
using OneMoreTime;

public class MovementMathTests
{
    [Test]
    public void JumpVelocity_MatchesPhysics()
    {
        // v = sqrt(2 * 1.4 * 25) = sqrt(70) ≈ 8.3666
        Assert.AreEqual(8.3666f, MovementMath.JumpVelocity(1.4f, -25f), 0.001f);
    }

    [Test]
    public void SlideStartSpeed_BoostsFromRunFloor()
    {
        Assert.AreEqual(9.8f, MovementMath.SlideStartSpeed(7f, 7f, 1.4f), 0.0001f);
    }

    [Test]
    public void SlideStartSpeed_PreservesHigherCurrentSpeed()
    {
        // mevcut hız run'dan yüksekse taban odur
        Assert.AreEqual(16.8f, MovementMath.SlideStartSpeed(12f, 7f, 1.4f), 0.0001f);
    }

    [Test]
    public void SlopeAccel_FlatGround_IsZero()
    {
        Assert.AreEqual(0f, MovementMath.SlopeAccel(Vector3.up, Vector3.forward, -25f), 0.0001f);
    }

    [Test]
    public void SlopeAccel_Downhill_IsPositive()
    {
        // 45° eğim, hareket yönü aşağı doğru → +12.5 m/s^2
        Vector3 n = new Vector3(0f, 1f, 1f).normalized;
        Assert.AreEqual(12.5f, MovementMath.SlopeAccel(n, Vector3.forward, -25f), 0.01f);
    }

    [Test]
    public void SlopeAccel_Uphill_ClampedToZero()
    {
        Vector3 n = new Vector3(0f, 1f, 1f).normalized;
        Assert.AreEqual(0f, MovementMath.SlopeAccel(n, Vector3.back, -25f), 0.0001f);
    }

    [Test]
    public void ApplyJump_PreservesHorizontal_SetsVertical()
    {
        // slide hop: yatay momentum %100 korunur
        Vector3 r = MovementMath.ApplyJump(new Vector3(9.8f, 0f, 0f), 8.37f);
        Assert.AreEqual(9.8f, r.x, 0.0001f);
        Assert.AreEqual(8.37f, r.y, 0.0001f);
        Assert.AreEqual(0f, r.z, 0.0001f);
    }
}
