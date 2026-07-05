using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class GameAudioPlayerTests
{
    readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
            if (_createdObjects[i]) Object.DestroyImmediate(_createdObjects[i]);

        _createdObjects.Clear();
    }

    [Test]
    public void MissingBinding_LogsWarningAndDoesNotThrow()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        go.AddComponent<GameAudioPlayer>();

        LogAssert.Expect(LogType.Warning, new Regex("no binding for cue CoinPickup.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseCoinPickup(Vector3.zero));
    }

    [Test]
    public void BindingWithNoClips_LogsWarningAndDoesNotThrow()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.CoinPickup, clips = new AudioClip[0] },
        });

        LogAssert.Expect(LogType.Warning, new Regex("binding for cue CoinPickup has no clips.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseCoinPickup(Vector3.zero));
    }

    [Test]
    public void WorldPositionBindingWithoutPosition_LogsWarningAndDoesNotThrow()
    {
        var clip = AudioClip.Create("clip", 1, 1, 44100, false);

        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.RunFinished, clips = new[] { clip }, useWorldPosition = true },
        });

        LogAssert.Expect(LogType.Warning, new Regex("needs a world position but none was raised.*"));
        Assert.DoesNotThrow(() => GameAudioEvents.RaiseRunFinished());
    }

    [Test]
    public void OnValidate_DuplicateBindingsForSameCue_LogsWarning()
    {
        var go = new GameObject("AudioPlayer");
        _createdObjects.Add(go);
        GameAudioPlayer player = go.AddComponent<GameAudioPlayer>();
        SetField(player, "bindings", new List<AudioCueBinding>
        {
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
            new AudioCueBinding { cue = AudioCueId.CoinPickup },
        });

        LogAssert.Expect(LogType.Warning, new Regex("duplicate binding for cue CoinPickup.*"));
        InvokeOnValidate(player);
    }

    static void InvokeOnValidate(GameAudioPlayer player)
    {
        typeof(GameAudioPlayer)
            .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(player, null);
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
