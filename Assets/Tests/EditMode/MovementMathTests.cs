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
    public void SlideVelocity_SidewaysOnSlope_CurvesTowardFallLine()
    {
        // n=(0,1,1) normalize → fall-line +Z/-Y. Yanal (+X) giren hız aşağı (+Z, -Y) kazanmalı.
        Vector3 n = new Vector3(0f, 1f, 1f).normalized;
        Vector3 r = MovementMath.SlideVelocity(new Vector3(5f, 0f, 0f), n, -25f, 0f, 0.02f);
        Assert.Greater(r.z, 0f);   // fall-line'a doğru momentum kazandı
        Assert.Less(r.y, 0f);      // yüzey boyunca aşağı iniyor (sekmiyor)
        Assert.AreEqual(5f, r.x, 0.01f); // yanal bileşen (sürtünme=0) korunuyor
    }

    [Test]
    public void SlideVelocity_FlatGround_OnlyFrictionNoTurn()
    {
        // Düz zemin: yön değişmez, yalnız sürtünme hızı azaltır.
        Vector3 r = MovementMath.SlideVelocity(new Vector3(10f, 0f, 0f), Vector3.up, -25f, 8f, 0.02f);
        Assert.AreEqual(0f, r.z, 0.0001f);
        Assert.AreEqual(0f, r.y, 0.0001f);
        Assert.AreEqual(10f - 8f * 0.02f, r.x, 0.001f); // 9.84
    }

    [Test]
    public void SlideVelocity_StaysTangentToSlope()
    {
        // Çıkan hız daima eğim düzleminde (yüzeye teğet) → hop yok.
        Vector3 n = new Vector3(0f, 2f, 1f).normalized;
        Vector3 r = MovementMath.SlideVelocity(new Vector3(3f, 0f, 4f), n, -25f, 5f, 0.02f);
        Assert.AreEqual(0f, Vector3.Dot(r, n), 0.001f);
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

    [Test]
    public void ProjectOnGround_FlatGround_UnchangedDirection()
    {
        Vector3 r = MovementMath.ProjectOnGround(Vector3.forward, Vector3.up);
        Assert.AreEqual(Vector3.forward.x, r.x, 0.0001f);
        Assert.AreEqual(Vector3.forward.y, r.y, 0.0001f);
        Assert.AreEqual(Vector3.forward.z, r.z, 0.0001f);
    }

    [Test]
    public void ProjectOnGround_DownhillSlope_TiltsDownwardAndStaysUnitLength()
    {
        // 45° eğim, düz "forward" hareket yönü aşağı-eğime yansıtılınca negatif Y almalı
        Vector3 n = new Vector3(0f, 1f, 1f).normalized;
        Vector3 r = MovementMath.ProjectOnGround(Vector3.forward, n);
        Assert.Less(r.y, 0f);
        Assert.AreEqual(1f, r.magnitude, 0.0001f);
    }

    [Test]
    public void ProjectOnGround_ZeroInput_ReturnsZero()
    {
        Vector3 r = MovementMath.ProjectOnGround(Vector3.zero, Vector3.up);
        Assert.AreEqual(Vector3.zero, r);
    }

    [Test]
    public void AirAccelerate_NoInput_Unchanged()
    {
        Vector3 v = new Vector3(10f, 0f, 0f);
        Vector3 r = MovementMath.AirAccelerate(v, Vector3.zero, 0f, 15f, 1f, 0.02f);
        Assert.AreEqual(v, r);
    }

    [Test]
    public void AirAccelerate_PerpendicularStrafe_AddsSpeed()
    {
        // Quake özü: hıza dik istek yönünde izdüşüm 0 → cap'e kadar hız EKLENİR, toplam hız büyür.
        Vector3 v = new Vector3(10f, 0f, 0f);
        Vector3 r = MovementMath.AirAccelerate(v, Vector3.forward, 7f, 15f, 1f, 0.02f);
        Assert.Greater(r.z, 0f);
        Assert.Greater(r.magnitude, v.magnitude);
    }

    [Test]
    public void AirAccelerate_AlongVelocity_NoGainPastCap()
    {
        // Hız yönünde istek: izdüşüm (10) zaten cap'in (1) üstünde → hiç ekleme yok.
        Vector3 v = new Vector3(10f, 0f, 0f);
        Vector3 r = MovementMath.AirAccelerate(v, Vector3.right, 7f, 15f, 1f, 0.02f);
        Assert.AreEqual(10f, r.x, 0.0001f);
        Assert.AreEqual(0f, r.z, 0.0001f);
    }

    [Test]
    public void WallJumpVelocity_PushesAwayFromWallAndUp()
    {
        // Duvar normali +X: sonuç +X itme ve jumpVelocity dikey hız içermeli.
        Vector3 r = MovementMath.WallJumpVelocity(new Vector3(0f, -5f, 0f), Vector3.right, 7f, 8.37f);
        Assert.AreEqual(7f, r.x, 0.0001f);
        Assert.AreEqual(8.37f, r.y, 0.0001f);
    }

    [Test]
    public void WallJumpVelocity_PreservesTangentMomentum()
    {
        // Duvara paralel (Z) momentum korunur — Titanfall duvar zıplaması hızı öldürmez.
        Vector3 r = MovementMath.WallJumpVelocity(new Vector3(-2f, -3f, 9f), Vector3.right, 7f, 8.37f);
        Assert.AreEqual(9f, r.z, 0.0001f);
        Assert.AreEqual(7f, r.x, 0.0001f);
    }

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
}
