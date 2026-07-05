using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class AudioCuePlaybackTests
{
    [Test]
    public void FindBinding_ReturnsFirstMatchingCue()
    {
        var match = new AudioCueBinding { cue = AudioCueId.CoinPickup };
        var bindings = new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.PlayerDied },
            match,
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
        };

        Assert.AreSame(match, AudioCuePlayback.FindBinding(bindings, AudioCueId.CoinPickup));
    }

    [Test]
    public void FindBinding_NoMatch_ReturnsNull()
    {
        var bindings = new List<AudioCueBinding> { new AudioCueBinding { cue = AudioCueId.PlayerDied } };

        Assert.IsNull(AudioCuePlayback.FindBinding(bindings, AudioCueId.CoinPickup));
    }

    [Test]
    public void PickClip_NoClips_ReturnsNull()
    {
        var binding = new AudioCueBinding { clips = new AudioClip[0] };

        Assert.IsNull(AudioCuePlayback.PickClip(binding, () => 0f));
    }

    [Test]
    public void PickClip_SingleClip_AlwaysReturnsIt()
    {
        var clip = AudioClip.Create("clip", 1, 1, 44100, false);
        var binding = new AudioCueBinding { clips = new[] { clip } };

        Assert.AreSame(clip, AudioCuePlayback.PickClip(binding, () => 0.9f));
    }

    [Test]
    public void PickClip_RollAtUpperBound_ClampsToLastClip()
    {
        var clipA = AudioClip.Create("a", 1, 1, 44100, false);
        var clipB = AudioClip.Create("b", 1, 1, 44100, false);
        var binding = new AudioCueBinding { clips = new[] { clipA, clipB } };

        Assert.AreSame(clipB, AudioCuePlayback.PickClip(binding, () => 1f));
    }

    [Test]
    public void PickPitch_ZeroRange_AlwaysReturnsOne()
    {
        var binding = new AudioCueBinding { pitchRandomRange = 0f };

        Assert.AreEqual(1f, AudioCuePlayback.PickPitch(binding, () => 0.5f), 0.0001f);
    }

    [Test]
    public void PickPitch_UsesRangeAroundOne()
    {
        var binding = new AudioCueBinding { pitchRandomRange = 0.2f };

        Assert.AreEqual(0.9f, AudioCuePlayback.PickPitch(binding, () => 0f), 0.0001f);
        Assert.AreEqual(1.1f, AudioCuePlayback.PickPitch(binding, () => 1f), 0.0001f);
        Assert.AreEqual(1.0f, AudioCuePlayback.PickPitch(binding, () => 0.5f), 0.0001f);
    }
}
