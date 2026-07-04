using NUnit.Framework;
using OneMoreTime;

public class RunQualityTests
{
    static RunQualityConfig DefaultConfig() => new RunQualityConfig();

    [Test]
    public void RightOnTimeChance_AtOrUnderPar_NoTimePenalty()
    {
        // parTime=30, seconds<=30 → overPar=0, yalnızca cap.
        Assert.AreEqual(60f, RunQuality.RightOnTimeChance(25f, 0, 30f, DefaultConfig()), 0.0001f);
        Assert.AreEqual(60f, RunQuality.RightOnTimeChance(30f, 0, 30f, DefaultConfig()), 0.0001f);
    }

    [Test]
    public void RightOnTimeChance_OverPar_PenalizesOnlyExcessSeconds()
    {
        // parTime=30, seconds=40 → overPar=10 → 60 - 10*1 = 50.
        Assert.AreEqual(50f, RunQuality.RightOnTimeChance(40f, 0, 30f, DefaultConfig()), 0.0001f);
    }

    [Test]
    public void RightOnTimeChance_CorpsesPenalizeRegardlessOfPar()
    {
        // parTime=30, seconds=30 (overPar=0), 4 ceset → 60 - 4*5 = 40.
        Assert.AreEqual(40f, RunQuality.RightOnTimeChance(30f, 4, 30f, DefaultConfig()), 0.0001f);
    }

    [Test]
    public void RightOnTimeChance_FarOverParAndManyCorpses_ClampsToFloor()
    {
        Assert.AreEqual(8f, RunQuality.RightOnTimeChance(200f, 10, 30f, DefaultConfig()), 0.0001f);
    }

    [Test]
    public void RightOnTimeChance_ZeroParTime_TreatsAllSecondsAsOverPar()
    {
        // parTime=0 → overPar=seconds. Par Time'ı hesaba katmayan eski davranışı doğrular.
        Assert.AreEqual(35f, RunQuality.RightOnTimeChance(25f, 0, 0f, DefaultConfig()), 0.0001f);
    }
}
