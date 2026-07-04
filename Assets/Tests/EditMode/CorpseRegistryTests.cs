using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneMoreTime;
using UnityEngine;
using UnityEngine.TestTools;

public class CorpseRegistryTests
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
    public void Register_AddsCorpseToListAndCount()
    {
        var registry = new CorpseRegistry();
        var corpse = new GameObject("Corpse");
        _createdObjects.Add(corpse);

        registry.Register(corpse);

        Assert.AreEqual(1, registry.Count);
        Assert.AreSame(corpse, registry.Corpses[0]);
    }

    [Test]
    public void ClearAll_EmptiesList()
    {
        var registry = new CorpseRegistry();
        var first = new GameObject("Corpse1");
        var second = new GameObject("Corpse2");
        _createdObjects.Add(first);
        _createdObjects.Add(second);
        registry.Register(first);
        registry.Register(second);

        // Object.Destroy is correct for production (Play mode); running it in an EditMode
        // test logs Unity's edit-mode warning as an Error. Expect it rather than avoid it.
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode.*"));
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode.*"));
        registry.ClearAll();

        Assert.AreEqual(0, registry.Count);
    }
}
