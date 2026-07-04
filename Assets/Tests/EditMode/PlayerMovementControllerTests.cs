using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OneMoreTime;
using UnityEditor;
using UnityEngine;

public class PlayerMovementControllerTests
{
    readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(_createdObjects[i]);

        _createdObjects.Clear();
    }

    [Test]
    public void FixedUpdate_BlockedFirstWallDoesNotHideEnterableOppositeWall()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        CreateBox("BlockedRightWall", new Vector3(0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        CreateBox("EnterableLeftWall", new Vector3(-0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        Physics.SyncTransforms();

        WallRunState wallRun = GetField<WallRunState>(controller, "_wallRun");
        wallRun.LockWallAfterJump(Vector3.left);
        SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);

        InvokeFixedUpdate(controller);

        Assert.IsFalse(GetField<bool>(controller, "_grounded"),
            "Fixture setup must remain airborne when probing side walls.");
        Assert.IsTrue(GetField<bool>(controller, "_onWall"),
            "Fixture setup must produce at least one valid wall contact.");
        Assert.IsTrue(wallRun.IsActive,
            "An ineligible first ray hit must not hide an enterable wall found by another probe.");
        Assert.Greater(Vector3.Dot(wallRun.WallNormal, Vector3.right), 0.9f);
    }

    [Test]
    public void FixedUpdate_BlockedWallStillTouchingAlongsideActiveWall_KeepsSeparationLock()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        CreateBox("ActiveRightWall", new Vector3(0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        CreateBox("BlockedLeftWall", new Vector3(-0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        Physics.SyncTransforms();

        WallRunState wallRun = GetField<WallRunState>(controller, "_wallRun");
        wallRun.LockWallAfterJump(Vector3.right);
        SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);

        InvokeFixedUpdate(controller);
        Assert.IsFalse(GetField<bool>(controller, "_grounded"),
            "Fixture setup must remain airborne when probing side walls.");
        Assert.IsTrue(GetField<bool>(controller, "_onWall"),
            "Fixture setup must produce at least one valid wall contact.");
        Assert.IsTrue(wallRun.IsActive,
            "Fixture setup must enter the eligible wall before testing retained separation.");

        for (int i = 1; i < 15; i++)
            InvokeFixedUpdate(controller);

        Assert.IsTrue(wallRun.IsActive);
        Assert.Greater(Vector3.Dot(wallRun.WallNormal, Vector3.left), 0.9f);
        Assert.IsFalse(wallRun.CanEnter(Vector3.right),
            "Seeing another wall must not clear separation while the blocked wall is still in the contact set.");
    }

    [Test]
    public void FixedUpdate_WallJumpAtDurationExpiry_UsesRetainedRunAndStartsCooldown()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        CreateBox("ActiveRightWall", new Vector3(0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        Physics.SyncTransforms();

        WallRunState wallRun = GetField<WallRunState>(controller, "_wallRun");
        Assert.IsTrue(wallRun.TryEnter(Vector3.left, Vector3.forward, 10f, 0f));
        wallRun.Tick(1.99f, true, Vector3.left);
        Assert.IsTrue(wallRun.IsActive);
        SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);
        PressJumpGate(controller, "_wallGate");

        InvokeFixedUpdate(controller);

        Assert.IsFalse(GetField<bool>(controller, "_grounded"),
            "Fixture setup must remain airborne at wall-run expiry.");
        Assert.IsTrue(GetField<bool>(controller, "_onWall"),
            "Fixture setup must retain the active wall contact at expiry.");
        Assert.GreaterOrEqual(wallRun.Elapsed, 2f,
            "The target tick must cross the wall-run duration boundary.");
        Assert.IsFalse(wallRun.IsActive);

        Vector3 horizontalVelocity = GetField<Vector3>(controller, "_lastHorizontalVelocity");
        Assert.AreEqual(10f, horizontalVelocity.z, 0.001f,
            "The wall jump must retain wall-run tangent speed.");
        Assert.Less(horizontalVelocity.x, 0f,
            "The wall jump must push outward along the retained wall normal.");

        wallRun.Tick(0.01f, false, Vector3.zero);
        Assert.IsFalse(wallRun.CanEnter(Vector3.left),
            "After separation, the jumped wall must remain blocked by wall-jump cooldown.");
    }

    [Test]
    public void FixedUpdate_WallJumpAtContactGraceExpiry_UsesTickStartEligibility()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        WallRunState wallRun = GetField<WallRunState>(controller, "_wallRun");
        Assert.IsTrue(wallRun.TryEnter(Vector3.left, Vector3.forward, 10f, 0f));
        wallRun.Tick(0.081f, false, Vector3.zero);
        Assert.IsTrue(wallRun.IsActive);

        JumpGate wallGate = GetField<JumpGate>(controller, "_wallGate");
        wallGate.Tick(0f, true);
        wallGate.Tick(0.081f, false);
        wallGate.PressJump();
        Assert.IsTrue(wallGate.CanConsumeJump,
            "The wall jump must be eligible at the start of the boundary tick.");
        SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);

        InvokeFixedUpdate(controller);

        Assert.IsFalse(GetField<bool>(controller, "_onWall"));
        Assert.IsFalse(wallRun.IsActive);
        Vector3 horizontalVelocity = GetField<Vector3>(controller, "_lastHorizontalVelocity");
        Assert.AreEqual(10f, horizontalVelocity.z, 0.001f,
            "The wall jump must retain wall-run tangent speed at contact-grace expiry.");
        Assert.Less(horizontalVelocity.x, 0f,
            "The wall jump must use the retained wall normal at contact-grace expiry.");

        wallRun.Tick(0.01f, false, Vector3.zero);
        Assert.IsFalse(wallRun.CanEnter(Vector3.left),
            "The contact-grace wall jump must start the same-wall cooldown.");
    }

    [Test]
    public void FixedUpdate_AirborneBufferedJump_ConsumesDoubleJumpOnce()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        JumpGate jumpGate = GetField<JumpGate>(controller, "_jumpGate");
        JumpGate wallGate = GetField<JumpGate>(controller, "_wallGate");
        DoubleJumpState doubleJump = GetField<DoubleJumpState>(controller, "_doubleJump");
        jumpGate.PressJump();
        wallGate.PressJump();

        InvokeFixedUpdate(controller);

        Assert.IsFalse(GetField<bool>(controller, "_grounded"));
        Assert.IsFalse(GetField<bool>(controller, "_onWall"));
        Assert.IsFalse(doubleJump.IsAvailable);
        Assert.IsFalse(jumpGate.HasBufferedJump);
        Assert.IsFalse(wallGate.HasBufferedJump);

        jumpGate.PressJump();
        wallGate.PressJump();
        InvokeFixedUpdate(controller);

        Assert.IsFalse(doubleJump.IsAvailable,
            "A second airborne press cannot create another charge without contact.");
        Assert.IsTrue(jumpGate.HasBufferedJump,
            "An unavailable double jump must not consume the buffered press.");
    }

    [Test]
    public void FixedUpdate_ContinuousWallContact_RefreshesOnlyOnEntry()
    {
        PlayerMovementController controller = CreateController(Vector3.zero);
        CreateBox("DoubleJumpWall", new Vector3(0.65f, 0f, 0f), new Vector3(0.1f, 3f, 3f));
        Physics.SyncTransforms();
        DoubleJumpState doubleJump = GetField<DoubleJumpState>(controller, "_doubleJump");
        doubleJump.TryConsume();
        SetField(controller, "_lastHorizontalVelocity", Vector3.forward * 10f);

        InvokeFixedUpdate(controller);
        Assert.IsTrue(GetField<bool>(controller, "_onWall"));
        Assert.IsTrue(doubleJump.TryConsume(), "Wall-contact entry must refresh the charge.");

        InvokeFixedUpdate(controller);

        Assert.IsFalse(doubleJump.IsAvailable,
            "Continuous wall contact must not refresh the consumed charge again.");
    }

    PlayerMovementController CreateController(Vector3 position)
    {
        Object projectInputAsset = AssetDatabase.LoadMainAssetAtPath("Assets/InputSystem_Actions.inputactions");
        Assert.IsNotNull(projectInputAsset, "The configured project input actions asset must exist.");
        Object inputAssetClone = Object.Instantiate(projectInputAsset);
        _createdObjects.Add(inputAssetClone);

        var player = new GameObject("PlayerMovementControllerTest");
        _createdObjects.Add(player);
        player.SetActive(false);
        player.transform.position = position;
        PlayerMovementController controller = player.AddComponent<PlayerMovementController>();
        SetField(controller, "inputAsset", inputAssetClone);
        InvokeAwake(controller);
        return controller;
    }

    void CreateBox(string name, Vector3 position, Vector3 size)
    {
        var box = new GameObject(name);
        _createdObjects.Add(box);
        box.transform.position = position;
        box.AddComponent<BoxCollider>().size = size;
    }

    static void InvokeFixedUpdate(PlayerMovementController controller)
    {
        typeof(PlayerMovementController)
            .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    static void InvokeAwake(PlayerMovementController controller)
    {
        typeof(PlayerMovementController)
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    static void PressJumpGate(PlayerMovementController controller, string fieldName)
    {
        object gate = GetField<object>(controller, fieldName);
        gate.GetType()
            .GetMethod("PressJump", BindingFlags.Instance | BindingFlags.Public)
            .Invoke(gate, null);
    }

    static T GetField<T>(PlayerMovementController controller, string fieldName)
    {
        return (T)typeof(PlayerMovementController)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(controller);
    }

    static void SetField(PlayerMovementController controller, string fieldName, object value)
    {
        typeof(PlayerMovementController)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, value);
    }
}
