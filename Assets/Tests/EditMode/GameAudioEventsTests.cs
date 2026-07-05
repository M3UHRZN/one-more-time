using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class GameAudioEventsTests
{
    [Test]
    public void RaiseCoinPickup_InvokesCueRaisedWithPosition()
    {
        AudioCuePayload received = default;
        int fireCount = 0;
        void Handler(AudioCuePayload payload) { received = payload; fireCount++; }

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseCoinPickup(new Vector3(1f, 2f, 3f));
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(AudioCueId.CoinPickup, received.Cue);
        Assert.IsTrue(received.HasPosition);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), received.Position);
    }

    [Test]
    public void RaiseRunFinished_InvokesCueRaisedWithoutPosition()
    {
        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseRunFinished();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.RunFinished, received.Cue);
        Assert.IsFalse(received.HasPosition);
    }

    [TestCase(SlotOutcome.RightOnTime, AudioCueId.SlotRightOnTime)]
    [TestCase(SlotOutcome.OneMoreTime, AudioCueId.SlotOneMoreTime)]
    [TestCase(SlotOutcome.NotThisTime, AudioCueId.SlotNotThisTime)]
    public void RaiseSlotOutcome_MapsOutcomeToMatchingCue(SlotOutcome outcome, AudioCueId expectedCue)
    {
        AudioCueId received = default;
        void Handler(AudioCuePayload payload) => received = payload.Cue;

        GameAudioEvents.CueRaised += Handler;
        try
        {
            GameAudioEvents.RaiseSlotOutcome(outcome, Vector3.zero);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(expectedCue, received);
    }

    [Test]
    public void RaiseCue_WithNoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseSceneTransitionStarted());
    }
}
