using NUnit.Framework;
using OneMoreTime;

public class RunQualityTests
{
    [Test]
    public void RightOnTimeChance_PerfectRun_MatchesGddTable()
    {
        // GDD §4.2: 25s, 0 ceset → ~%51
        Assert.AreEqual(51.25f, RunQuality.RightOnTimeChance(25f, 0), 0.001f);
    }

    [Test]
    public void RightOnTimeChance_GoodRun_MatchesGddTable()
    {
        // 45s, 2 ceset → ~%39
        Assert.AreEqual(39.25f, RunQuality.RightOnTimeChance(45f, 2), 0.001f);
    }

    [Test]
    public void RightOnTimeChance_AverageRun_MatchesGddTable()
    {
        // 70s, 4 ceset → ~%25
        Assert.AreEqual(25.5f, RunQuality.RightOnTimeChance(70f, 4), 0.001f);
    }

    [Test]
    public void RightOnTimeChance_BadRun_ClampsToFloor()
    {
        // 110s, 8 ceset → taban %8
        Assert.AreEqual(8f, RunQuality.RightOnTimeChance(110f, 8), 0.001f);
    }

    [Test]
    public void RightOnTimeChance_InstantCleanRun_ClampsToCap()
    {
        Assert.AreEqual(60f, RunQuality.RightOnTimeChance(0f, 0), 0.001f);
    }
}
