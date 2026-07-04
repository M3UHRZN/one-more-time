using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;

public class SlotControllerTests
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
    public void Spin_NotThisTimeWithToken_IsForgivenAndAdvancesSpinNumber()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerTokens tokens = playerGo.AddComponent<PlayerTokens>();

        var slotGo = new GameObject("Slot");
        _createdObjects.Add(slotGo);
        slotGo.SetActive(false); // OnEnable tetiklenmesin: bu testte RunController olmadan çalışıyoruz.
        SlotController slot = slotGo.AddComponent<SlotController>();
        SetField(slot, "tokens", tokens);

        // roll=0.999 → Right+One her zaman <=95 olduğundan NotThisTime her spin'de garanti çekilir.
        SetField(slot, "_machine", new SlotMachine(() => 0.999f));
        InvokeHandleRunFinished(slot, new RunResult(0f, 0, 8f));

        slot.Spin();

        Assert.IsTrue(slot.LastSpinForgiven, "1 jeton varken NOT THIS TIME affedilmeli.");
        Assert.IsTrue(slot.CanSpin, "Af sonrası çevirmeye devam edilebilmeli.");
        Assert.AreEqual(2, slot.SpinNumber, "Af, ONE MORE TIME gibi çevirme sayısını ilerletmeli.");
        Assert.AreEqual(0, slot.TokenCount);

        slot.Spin();

        Assert.IsFalse(slot.LastSpinForgiven, "Jetonsuz NOT THIS TIME affedilmemeli.");
        Assert.IsTrue(slot.Lost);
        Assert.IsFalse(slot.CanSpin);
    }

    [Test]
    public void Spin_NotThisTimeWithoutToken_RaisesRunLostEventOnce()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerTokens tokens = playerGo.AddComponent<PlayerTokens>();
        tokens.TrySpend(); // Count -> 0

        var slotGo = new GameObject("Slot");
        _createdObjects.Add(slotGo);
        slotGo.SetActive(false);
        SlotController slot = slotGo.AddComponent<SlotController>();
        SetField(slot, "tokens", tokens);
        SetField(slot, "_machine", new SlotMachine(() => 0.999f));
        InvokeHandleRunFinished(slot, new RunResult(0f, 0, 8f));

        int lostCount = 0;
        slot.RunLost += () => lostCount++;

        slot.Spin();

        Assert.AreEqual(1, lostCount, "RunLost tam bir kez tetiklenmeli.");
        Assert.IsTrue(slot.Lost);
    }

    [Test]
    public void ClearAfterLoss_ResetsAllStateFlags()
    {
        var playerGo = new GameObject("Player");
        _createdObjects.Add(playerGo);
        PlayerTokens tokens = playerGo.AddComponent<PlayerTokens>();
        tokens.TrySpend(); // Count -> 0

        var slotGo = new GameObject("Slot");
        _createdObjects.Add(slotGo);
        slotGo.SetActive(false);
        SlotController slot = slotGo.AddComponent<SlotController>();
        SetField(slot, "tokens", tokens);
        SetField(slot, "_machine", new SlotMachine(() => 0.999f));
        InvokeHandleRunFinished(slot, new RunResult(0f, 0, 8f));
        slot.Spin(); // gerçek kayıp: Lost=true, CanSpin=false, LastSpin set

        slot.ClearAfterLoss();

        Assert.IsFalse(slot.Won);
        Assert.IsFalse(slot.Lost);
        Assert.IsFalse(slot.CanSpin);
        Assert.IsNull(slot.LastSpin);
        Assert.IsFalse(slot.LastSpinForgiven);
    }

    static void InvokeHandleRunFinished(SlotController slot, RunResult result)
    {
        typeof(SlotController)
            .GetMethod("HandleRunFinished", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(slot, new object[] { result });
    }

    static void SetField(Object component, string fieldName, object value)
    {
        component.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, value);
    }
}
