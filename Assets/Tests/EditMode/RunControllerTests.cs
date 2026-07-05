using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class RunControllerTests
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
    public void Finish_LocksResultFromElapsedTimeAndCorpseCountAndFiresEventOnce()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerRespawner player = playerGo.AddComponent<PlayerRespawner>();
        CorpseRegistry registry = GetField<CorpseRegistry>(player, "_registry");
        for (int i = 0; i < 2; i++)
        {
            var corpse = new GameObject("Corpse" + i);
            _createdObjects.Add(corpse);
            registry.Register(corpse);
        }

        var runGo = new GameObject("RunController");
        _createdObjects.Add(runGo);
        RunController run = runGo.AddComponent<RunController>();
        SetField(run, "player", player);

        RunTimer timer = GetField<RunTimer>(run, "_timer");
        timer.Begin();
        timer.Tick(45f);

        int fireCount = 0;
        RunResult received = default;
        run.RunFinished += result =>
        {
            fireCount++;
            received = result;
        };

        run.Finish();

        Assert.IsTrue(run.HasFinished);
        Assert.AreEqual(45f, run.LastResult.Seconds, 0.0001f);
        Assert.AreEqual(2, run.LastResult.Corpses);
        // Varsayılan parTime=60, seconds=45 → overPar=0; 2 ceset × 5 ceza = 60-10 = 50.
        Assert.AreEqual(50f, run.LastResult.RightChance, 0.0001f);
        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(run.LastResult.Seconds, received.Seconds, 0.0001f);
        Assert.IsFalse(run.IsRunning, "Finish must stop the timer.");

        run.Finish();

        Assert.AreEqual(1, fireCount, "A second Finish call must be a no-op.");
    }

    [Test]
    public void Finish_RaisesRunFinishedCue()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerRespawner player = playerGo.AddComponent<PlayerRespawner>();

        var runGo = new GameObject("RunController");
        _createdObjects.Add(runGo);
        RunController run = runGo.AddComponent<RunController>();
        SetField(run, "player", player);

        RunTimer timer = GetField<RunTimer>(run, "_timer");
        timer.Begin();

        int fireCount = 0;
        void Handler(AudioCuePayload payload) => fireCount++;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            run.Finish();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
    }

    static T GetField<T>(Object component, string fieldName)
    {
        return (T)component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(component);
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
