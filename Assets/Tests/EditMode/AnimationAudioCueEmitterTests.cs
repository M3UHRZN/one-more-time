using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class AnimationAudioCueEmitterTests
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
    public void RaiseConfiguredCue_RaisesConfiguredCueAtEmitterPosition()
    {
        var go = new GameObject("Emitter");
        _createdObjects.Add(go);
        go.transform.position = new Vector3(1f, 2f, 3f);
        AnimationAudioCueEmitter emitter = go.AddComponent<AnimationAudioCueEmitter>();
        SetField(emitter, "cue", AudioCueId.SlotInteractionStarted);

        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            emitter.RaiseConfiguredCue();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.SlotInteractionStarted, received.Cue);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), received.Position);
    }

    [Test]
    public void RaiseCueById_RaisesCueCastFromInt()
    {
        var go = new GameObject("Emitter");
        _createdObjects.Add(go);
        AnimationAudioCueEmitter emitter = go.AddComponent<AnimationAudioCueEmitter>();

        AudioCueId received = default;
        void Handler(AudioCuePayload payload) => received = payload.Cue;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            emitter.RaiseCueById((int)AudioCueId.PlayerDied);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(AudioCueId.PlayerDied, received);
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
