using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayerMovementTests : InputTestFixture
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject player;
    private GameObject floor;
    private Component movement;
    private Rigidbody2D body;
    private BoxCollider2D collider;
    private Keyboard keyboard;
    private SimulationMode2D previousSimulationMode;
    private Vector2 originalSize;
    private Vector2 originalOffset;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        previousSimulationMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        keyboard = InputSystem.AddDevice<Keyboard>();

        floor = new GameObject("Test floor");
        floor.transform.position = new Vector3(1000f, 999f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);

        player = new GameObject("Test player");
        player.SetActive(false);
        player.transform.position = new Vector3(1000f, 1000f, 0f);
        body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        collider = player.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        originalSize = collider.size;
        originalOffset = collider.offset;
        player.AddComponent<SpriteRenderer>();
        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Characters/Player/Animations/Player.controller");

        Type movementType = Type.GetType("PlayerMovement, Assembly-CSharp", true);
        movement = player.AddComponent(movementType);
        ((Behaviour)movement).enabled = false;
        Set("inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions"));
        Invoke("Awake");
        player.SetActive(true);
        animator.Rebind();
        Invoke("OnEnable");
        Land();
    }

    [TearDown]
    public override void TearDown()
    {
        if (movement != null)
        {
            Invoke("OnDisable");
            Invoke("OnDestroy");
        }

        UnityEngine.Object.DestroyImmediate(player);
        UnityEngine.Object.DestroyImmediate(floor);
        if (keyboard != null)
            InputSystem.RemoveDevice(keyboard);
        Physics2D.simulationMode = previousSimulationMode;
        base.TearDown();
    }

    [Test]
    public void MainScene_HasInputActionsAssigned()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        try
        {
            Component sceneMovement = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneMovement = root.GetComponentInChildren(movement.GetType());
                if (sceneMovement != null)
                    break;
            }

            Assert.That(sceneMovement, Is.Not.Null);
            var settings = new SerializedObject(sceneMovement);
            Assert.That(settings.FindProperty("inputActions").objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/InputSystem_Actions.inputactions")));
            Assert.That(settings.FindProperty("coyoteTime").floatValue,
                Is.EqualTo(0.1f).Within(0.001f));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void InputActions_MoveAndJumpDoNotReduceHorizontalSpeed()
    {
        Press(Key.A, Key.W);
        Tick(10f);
        Assert.That(body.linearVelocity.x, Is.EqualTo(-8f).Within(0.001f));
        Assert.That(body.linearVelocity.y, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void Slide_ContinuesPastCliffUntilDurationEnds()
    {
        Press(Key.S);
        Tick(10f);
        TakeOff();
        Tick(10.2f);
        Assert.That(Get<bool>("isSliding"), Is.True);
        Assert.That(body.linearVelocity.x, Is.EqualTo(12f));
        Assert.That(body.linearVelocity.y, Is.LessThan(0f));
        Tick(10.49f);
        Assert.That(Get<bool>("isSliding"), Is.True);
        Tick(10.5f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        Assert.That(body.linearVelocity.x, Is.Zero);
        Tick(10.7f);
        Assert.That(body.linearVelocity.x, Is.Zero);
    }

    [Test]
    public void Slide_EndZerosHorizontalSpeedEvenWithMoveHeld()
    {
        Press(Key.S);
        Tick(10f);
        TakeOff();
        Press(Key.D);
        Tick(10.5f);
        Assert.That(body.linearVelocity.x, Is.Zero);
        Tick(10.52f);
        Assert.That(body.linearVelocity.x, Is.EqualTo(8f));
    }

    [Test]
    public void Slide_ColliderKeepsFeetAndRestoresAtEnd()
    {
        float bottom = collider.offset.y - collider.size.y * 0.5f;
        Press(Key.S);
        Tick(10f);
        Assert.That(collider.size.y, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(collider.offset.y - collider.size.y * 0.5f,
            Is.EqualTo(bottom).Within(0.001f));
        Tick(10.5f);
        Assert.That(collider.size, Is.EqualTo(originalSize));
        Assert.That(collider.offset, Is.EqualTo(originalOffset));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Slide_AirborneTapOrHoldStartsOnLanding(bool keepHeld)
    {
        Tick(10f);
        TakeOff();
        Press(Key.S);
        Tick(10.2f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        if (!keepHeld)
            Press();
        Tick(11f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        Land();
        Tick(11.5f);
        Assert.That(Get<bool>("isSliding"), Is.True);
        Assert.That(Get<bool>("slideRequested"), Is.False);
    }

    [Test]
    public void Coyote_JumpWorksWithinPointOneSeconds()
    {
        Tick(10f);
        TakeOff();
        Press(Key.W);
        Tick(10.08f);
        Assert.That(body.linearVelocity.y, Is.EqualTo(10f));
    }

    [Test]
    public void Coyote_JumpExpiresAfterPointOneSeconds()
    {
        Tick(10f);
        TakeOff();
        Press(Key.W);
        Tick(10.12f);
        Assert.That(body.linearVelocity.y, Is.LessThan(0f));
    }

    [Test]
    public void Coyote_SlideWorksWithinPointOneSeconds()
    {
        Tick(10f);
        TakeOff();
        Press(Key.S);
        Tick(10.08f);
        Assert.That(Get<bool>("isSliding"), Is.True);
    }

    [Test]
    public void Coyote_JumpConsumesAirborneSlideOpportunity()
    {
        Tick(10f);
        TakeOff();
        Press(Key.W);
        Tick(10.04f);
        Press(Key.S);
        Tick(10.08f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        Land();
        Tick(11f);
        Assert.That(Get<bool>("isSliding"), Is.True);
    }

    [Test]
    public void Slide_CooldownIsTwoSecondsAndHoldDoesNotRepeat()
    {
        Press(Key.S);
        Tick(10f);
        Tick(10.5f);
        Assert.That(Get<float>("nextSlideTime"), Is.EqualTo(12.5f));
        Land();
        Tick(12.6f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        Press();
        Press(Key.S);
        Tick(12.62f);
        Assert.That(Get<bool>("isSliding"), Is.True);
    }

    [Test]
    public void Slide_CooldownRejectsGroundedPressWithoutDelayedRepeat()
    {
        Press(Key.S);
        Tick(10f);
        Tick(10.5f);
        Land();
        Press();
        Press(Key.S);
        Tick(12.4f);
        Assert.That(Get<bool>("isSliding"), Is.False);
        Tick(12.6f);
        Assert.That(Get<bool>("isSliding"), Is.False);
    }

    [Test]
    public void SimultaneousJumpAndSlide_PrioritizesJump()
    {
        Press(Key.W, Key.S);
        Tick(10f);
        Assert.That(body.linearVelocity.y, Is.EqualTo(10f));
        Assert.That(Get<bool>("isSliding"), Is.False);
        Assert.That(Get<bool>("slideRequested"), Is.False);
    }

    [Test]
    public void Disable_RestoresColliderAndClearsBufferedInput()
    {
        Press(Key.S);
        Tick(10f);
        Invoke("OnDisable");
        Assert.That(collider.size, Is.EqualTo(originalSize));
        Assert.That(collider.offset, Is.EqualTo(originalOffset));
        Assert.That(Get<bool>("isSliding"), Is.False);
        Assert.That(Get<bool>("slideRequested"), Is.False);
    }

    private void Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }

    private void Tick(float time)
    {
        Physics2D.SyncTransforms();
        Physics2D.Simulate(0.02f);
        Invoke("Update");
        Invoke("UpdateMovement", time);
    }

    private void Land()
    {
        body.position = new Vector2(1000f, 1000f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        for (int i = 0; i < 10; i++)
            Physics2D.Simulate(0.02f);
    }

    private void TakeOff()
    {
        body.position = new Vector2(1000f, 1003f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        Physics2D.Simulate(0.02f);
    }

    private object Invoke(string name, params object[] args)
    {
        return movement.GetType().GetMethod(name, PrivateInstance).Invoke(movement, args);
    }

    private T Get<T>(string name)
    {
        return (T)movement.GetType().GetField(name, PrivateInstance).GetValue(movement);
    }

    private void Set(string name, object value)
    {
        movement.GetType().GetField(name, PrivateInstance).SetValue(movement, value);
    }
}
