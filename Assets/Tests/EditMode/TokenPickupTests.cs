using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class TokenPickupTests
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
    public void OnTriggerEnter_PlayerHasTokens_RaisesCoinPickupAtPickupPosition()
    {
        var pickupGo = new GameObject("TokenPickup");
        _createdObjects.Add(pickupGo);
        pickupGo.transform.position = new Vector3(2f, 0f, 4f);
        TokenPickup pickup = pickupGo.AddComponent<TokenPickup>();

        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        playerGo.AddComponent<PlayerTokens>();
        BoxCollider collider = playerGo.AddComponent<BoxCollider>();

        AudioCuePayload received = default;
        int fireCount = 0;
        void Handler(AudioCuePayload payload) { received = payload; fireCount++; }
        GameAudioEvents.CueRaised += Handler;
        try
        {
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode.*"));
            InvokeOnTriggerEnter(pickup, collider);
        }
        finally
        {
            GameAudioEvents.CueRaised -= Handler;
        }

        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(AudioCueId.CoinPickup, received.Cue);
        Assert.AreEqual(new Vector3(2f, 0f, 4f), received.Position);
    }

    static void InvokeOnTriggerEnter(TokenPickup pickup, Collider other)
    {
        typeof(TokenPickup)
            .GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pickup, new object[] { other });
    }
}
