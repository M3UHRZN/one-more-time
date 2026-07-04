using NUnit.Framework;
using OneMoreTime;

public class SlotOddsTests
{
    [Test]
    public void From_FirstSpin_NotIsFloorValue()
    {
        SlotOdds odds = SlotOdds.From(50f, 1);

        Assert.AreEqual(5f, odds.Not, 0.0001f);
    }

    [Test]
    public void From_SixthSpin_NotClampsToCeiling()
    {
        // 5 + (6-1)*4 = 25 → tam tavanda
        SlotOdds odds = SlotOdds.From(50f, 6);

        Assert.AreEqual(25f, odds.Not, 0.0001f);
    }

    [Test]
    public void From_TenthSpin_NotStaysClampedAtCeiling()
    {
        SlotOdds odds = SlotOdds.From(50f, 10);

        Assert.AreEqual(25f, odds.Not, 0.0001f);
    }

    [Test]
    public void From_OddsAlwaysSumToOneHundred()
    {
        SlotOdds odds = SlotOdds.From(30f, 3);

        Assert.AreEqual(100f, odds.Right + odds.One + odds.Not, 0.0001f);
    }

    [Test]
    public void From_PRightOutOfRange_ClampsToGddBounds()
    {
        SlotOdds tooLow = SlotOdds.From(2f, 1);
        SlotOdds tooHigh = SlotOdds.From(90f, 1);

        Assert.AreEqual(8f, tooLow.Right, 0.0001f);
        Assert.AreEqual(60f, tooHigh.Right, 0.0001f);
    }
}
