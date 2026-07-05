using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class LossFlowControllerTests
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
    public void ForceContinue_ClearsCorpsesResetsTokensAndRestartsRun()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        playerGo.transform.position = new Vector3(5f, 1f, 3f);
        Rigidbody body = playerGo.AddComponent<Rigidbody>();

        var spawnPointGo = new GameObject("SpawnPoint");
        _createdObjects.Add(spawnPointGo);
        spawnPointGo.transform.position = Vector3.zero;

        PlayerRespawner respawner = playerGo.AddComponent<PlayerRespawner>();
        SetField(respawner, "body", body);
        SetField(respawner, "spawnPoint", spawnPointGo.transform);

        // Bir ceset kaydet (registry'ye doğrudan) — ForceContinue'nun temizlediğini doğrulamak için.
        CorpseRegistry registry = GetField<CorpseRegistry>(respawner, "_registry");
        var corpse = new GameObject("Corpse");
        _createdObjects.Add(corpse);
        registry.Register(corpse);

        PlayerTokens tokens = playerGo.AddComponent<PlayerTokens>();
        tokens.TrySpend(); // Count -> 0, jetonsuz kayıp durumunu simüle eder.

        var runGo = new GameObject("Run");
        _createdObjects.Add(runGo);
        RunController run = runGo.AddComponent<RunController>();

        var slotGo = new GameObject("Slot");
        _createdObjects.Add(slotGo);
        slotGo.SetActive(false); // OnEnable'da RunController referansı kurulu değil.
        SlotController slot = slotGo.AddComponent<SlotController>();

        // ForceContinue artık interaction.EndInteraction(instant:true) çağırıyor — o çağrının
        // dokunduğu alanlar (movement/look/fov/slot/cameraTransform/hud) kadarı kuruluyor.
        var movementGo = new GameObject("Movement");
        _createdObjects.Add(movementGo);
        movementGo.SetActive(false); // Awake (inputAsset ister) tetiklenmesin.
        PlayerMovementController movement = movementGo.AddComponent<PlayerMovementController>();
        SetField(movement, "_rb", movementGo.GetComponent<Rigidbody>());

        var lookGo = new GameObject("Look");
        _createdObjects.Add(lookGo);
        lookGo.SetActive(false);
        FirstPersonLook look = lookGo.AddComponent<FirstPersonLook>();

        var camGo = new GameObject("Cam");
        _createdObjects.Add(camGo);
        SpeedFovEffect fov = camGo.AddComponent<SpeedFovEffect>();

        var hudGo = new GameObject("Hud");
        _createdObjects.Add(hudGo);
        SlotHudDebug hud = hudGo.AddComponent<SlotHudDebug>();

        var interactionGo = new GameObject("Interaction");
        _createdObjects.Add(interactionGo);
        interactionGo.SetActive(false);
        SlotMachineInteraction interaction = interactionGo.AddComponent<SlotMachineInteraction>();
        SetField(interaction, "movement", movement);
        SetField(interaction, "look", look);
        SetField(interaction, "fov", fov);
        SetField(interaction, "slot", slot);
        SetField(interaction, "hud", hud);
        SetField(interaction, "cameraTransform", camGo.transform);

        var lossGo = new GameObject("LossFlow");
        _createdObjects.Add(lossGo);
        lossGo.SetActive(false);
        LossFlowController loss = lossGo.AddComponent<LossFlowController>();
        SetField(loss, "slot", slot);
        SetField(loss, "player", respawner);
        SetField(loss, "tokens", tokens);
        SetField(loss, "run", run);
        SetField(loss, "interaction", interaction);

        // Object.Destroy Play mode için doğrudur; EditMode'da çalıştırınca Unity'nin
        // edit-mode uyarısını Error olarak loglar (bkz. CorpseRegistryTests).
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode.*"));
        loss.ForceContinue();

        Assert.AreEqual(0, respawner.CorpseCount, "Kayıpta tüm cesetler silinmeli.");
        Assert.AreEqual(spawnPointGo.transform.position, body.position, "Oyuncu spawn'a ışınlanmalı.");
        Assert.AreEqual(1, tokens.Count, "Jeton 1'e sıfırlanmalı.");
        Assert.IsTrue(run.IsRunning, "Yeni koşu başlamalı.");
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
