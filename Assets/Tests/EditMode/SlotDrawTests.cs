using NUnit.Framework;
using OneMoreTime;

public class SlotDrawTests
{
    // pRight=70 → clamp 60'a; spin=1 → Not=5. Right=60, One=35, Not=5.
    // Sınırlar: [0,60)=Right, [60,95)=One, [95,100)=Not.
    static SlotOdds FixtureOdds() => SlotOdds.From(70f, 1);

    [Test]
    public void Pick_RollAtZero_ReturnsRightOnTime()
    {
        Assert.AreEqual(SlotOutcome.RightOnTime, SlotDraw.Pick(FixtureOdds(), 0f));
    }

    [Test]
    public void Pick_RollJustBelowRightThreshold_ReturnsRightOnTime()
    {
        Assert.AreEqual(SlotOutcome.RightOnTime, SlotDraw.Pick(FixtureOdds(), 0.5999f));
    }

    [Test]
    public void Pick_RollAtRightThreshold_ReturnsOneMoreTime()
    {
        Assert.AreEqual(SlotOutcome.OneMoreTime, SlotDraw.Pick(FixtureOdds(), 0.60f));
    }

    [Test]
    public void Pick_RollAtRightPlusOneThreshold_ReturnsNotThisTime()
    {
        Assert.AreEqual(SlotOutcome.NotThisTime, SlotDraw.Pick(FixtureOdds(), 0.95f));
    }

    [Test]
    public void Pick_RollNearOne_ReturnsNotThisTime()
    {
        Assert.AreEqual(SlotOutcome.NotThisTime, SlotDraw.Pick(FixtureOdds(), 0.999f));
    }
}
