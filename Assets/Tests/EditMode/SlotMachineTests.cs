using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime;

public class SlotMachineTests
{
    [Test]
    public void BeginSession_ResetsSpinNumberToOne()
    {
        var machine = new SlotMachine(() => 0.99f);
        machine.BeginSession(50f);

        Assert.AreEqual(1, machine.SpinNumber);
    }

    [Test]
    public void Spin_OneMoreTimeOutcome_IncrementsSpinNumberAndRaisesPNot()
    {
        // pRight=8 (taban) → Right=8, spin1 Not=5, One=87 → roll 0.5 (=50) düşer One aralığına.
        var machine = new SlotMachine(() => 0.5f);
        machine.BeginSession(8f);

        SlotSpinResult result = machine.Spin();

        Assert.AreEqual(SlotOutcome.OneMoreTime, result.Outcome);
        Assert.AreEqual(2, machine.SpinNumber);
        Assert.Greater(machine.CurrentOdds.Not, result.Odds.Not);
    }

    [Test]
    public void Spin_RightOnTimeOutcome_DoesNotAdvanceSpinNumber()
    {
        var machine = new SlotMachine(() => 0f); // en düşük roll → her zaman Right
        machine.BeginSession(50f);

        machine.Spin();

        Assert.AreEqual(1, machine.SpinNumber);
    }

    [Test]
    public void Spin_NotThisTimeOutcome_DoesNotAdvanceSpinNumber()
    {
        var machine = new SlotMachine(() => 0.999f); // en yüksek roll → her zaman Not
        machine.BeginSession(50f);

        machine.Spin();

        Assert.AreEqual(1, machine.SpinNumber);
    }

    [Test]
    public void Spin_PRightStaysLockedAcrossSession()
    {
        var machine = new SlotMachine(() => 0.5f); // One'a düşürecek roll
        machine.BeginSession(20f);

        float firstRight = machine.Spin().Odds.Right;
        float secondRight = machine.Spin().Odds.Right;

        Assert.AreEqual(firstRight, secondRight, 0.0001f);
    }

    [Test]
    public void Spin_RollSequence_ProducesExpectedOutcomeSequence()
    {
        // pRight=8 → Right=8. spin1 Not=5,One=87. spin2 Not=9,One=83.
        var rolls = new Queue<float>(new[] { 0.5f, 0.5f, 0f });
        var machine = new SlotMachine(() => rolls.Dequeue());
        machine.BeginSession(8f);

        var outcomes = new List<SlotOutcome>
        {
            machine.Spin().Outcome,
            machine.Spin().Outcome,
            machine.Spin().Outcome,
        };

        CollectionAssert.AreEqual(
            new[] { SlotOutcome.OneMoreTime, SlotOutcome.OneMoreTime, SlotOutcome.RightOnTime },
            outcomes);
    }
}
