using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class PlayerRespawnerTests
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
    public void Kill_SpawnsCorpseAtDeathPositionAndTeleportsPlayerToSpawn()
    {
        var player = new GameObject("Player");
        _createdObjects.Add(player);
        player.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.linearVelocity = new Vector3(4f, 0f, 0f);

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = new Vector3(0f, 0f, 0f);

        var corpsePrefab = new GameObject("CorpsePrefab");
        _createdObjects.Add(corpsePrefab);

        PlayerRespawner respawner = player.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);
        SetField(respawner, "corpsePrefab", corpsePrefab);

        Vector3 deathPosition = body.position;
        respawner.Kill();

        Assert.AreEqual(1, respawner.CorpseCount);
        GameObject spawnedCorpse = GetField<CorpseRegistry>(respawner, "_registry").Corpses[0];
        _createdObjects.Add(spawnedCorpse);
        Assert.AreEqual(deathPosition, spawnedCorpse.transform.position);

        Assert.AreEqual(spawnPointGo.transform.position, body.position);
        Assert.AreEqual(Vector3.zero, body.linearVelocity);
    }

    [Test]
    public void ResetToSpawn_TeleportsWithoutSpawningCorpse()
    {
        var player = new GameObject("Player");
        _createdObjects.Add(player);
        player.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.linearVelocity = new Vector3(4f, 0f, 0f);

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = new Vector3(0f, 0f, 0f);

        PlayerRespawner respawner = player.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);

        respawner.ResetToSpawn();

        Assert.AreEqual(0, respawner.CorpseCount, "ResetToSpawn ceset spawn ETMEMELİ.");
        Assert.AreEqual(spawnPointGo.transform.position, body.position);
        Assert.AreEqual(Vector3.zero, body.linearVelocity);
    }

    [Test]
    public void Kill_RaisesPlayerDiedAtDeathPosition()
    {
        var player = new GameObject("Player");
        _createdObjects.Add(player);
        player.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = player.AddComponent<Rigidbody>();

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = Vector3.zero;

        var corpsePrefab = new GameObject("CorpsePrefab");
        _createdObjects.Add(corpsePrefab);

        PlayerRespawner respawner = player.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);
        SetField(respawner, "corpsePrefab", corpsePrefab);

        Vector3 deathPosition = body.position;
        AudioCuePayload received = default;
        void Handler(AudioCuePayload payload) => received = payload;
        GameAudioEvents.CueRaised += Handler;
        try
        {
            respawner.Kill();
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        _createdObjects.Add(GetField<CorpseRegistry>(respawner, "_registry").Corpses[0]);
        Assert.AreEqual(AudioCueId.PlayerDied, received.Cue);
        Assert.AreEqual(deathPosition, received.Position);
    }

    static T GetField<T>(PlayerRespawner respawner, string fieldName)
    {
        return (T)typeof(PlayerRespawner)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(respawner);
    }

    static void SetField(PlayerRespawner respawner, string fieldName, object value)
    {
        typeof(PlayerRespawner)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(respawner, value);
    }
}
