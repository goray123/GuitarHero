// ./Tools/ValidateCombatFeel.ps1 -Suite GuitarSpin
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GuitarSpinValidation
{
    private const string Pending = "GuitarSpinValidation.Pending";
    private static readonly List<string> Results = new List<string>();
    private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    private static object Call(object owner, string name, params object[] args) =>
        owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    private static void Check(bool condition, string message) => Results.Add((condition ? "PASS: " : "FAIL: ") + message);
    private static void Near(float actual, float expected, string message) =>
        Check(Mathf.Abs(actual - expected) < 0.001f, message + " (actual " + actual + ", expected " + expected + ")");

    static GuitarSpinValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            RunChecks();
        };
    }

    public static void Run()
    {
        Application.runInBackground = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Keys(Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }

    private static GuitarSpinValidationTarget Target(string name, Vector2 position, bool duplicate = false)
    {
        var target = new GameObject(name, typeof(BoxCollider2D)).AddComponent<GuitarSpinValidationTarget>();
        target.transform.position = position;
        target.GetComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
        target.GetComponent<BoxCollider2D>().isTrigger = true;
        if (duplicate)
        {
            var child = new GameObject("Second Collider", typeof(BoxCollider2D));
            child.transform.SetParent(target.transform, false);
            child.GetComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
            child.GetComponent<BoxCollider2D>().isTrigger = true;
        }
        return target;
    }

    private static void RunChecks()
    {
        int exitCode = 0;
        Keyboard keyboard = null;
        var previousBackground = InputSystem.settings.backgroundBehavior;
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            // 헤드리스 에디터에는 포커스를 줄 Game View가 없다.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            var floor = new GameObject("Floor", typeof(BoxCollider2D));
            floor.transform.position = new Vector2(0f, -0.5f);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            var movement = player.GetComponent<PlayerMovement>();
            var body = player.GetComponent<Rigidbody2D>();
            var spin = player.GetComponent<PlayerGuitarSpin>();
            var visual = player.GetComponent<PlayerGuitarSpinVisual>();
            var health = player.GetComponent<PlayerHealth>();
            var attack = player.GetComponent<PlayerAttack>();
            var chord = player.GetComponent<PlayerChordStrike>();
            var rhythm = player.GetComponent<PlayerRhythmGauge>();
            player.GetComponent<PlayerHitStop>().enabled = false;
            Check(spin != null && visual != null, "Player prefab includes the skill and visual");
            Check(Get<Sprite>(visual, "guitarSprite") != null && Get<Sprite[]>(visual, "spinFrames").Length == 4,
                "Guitar art and looping swing frames are wired");
            Near(Get<float>(spin, "maximumDuration"), 4f, "Maximum duration is 4 seconds");
            Near(Get<float>(spin, "cooldownTime"), 6f, "Cooldown is 6 seconds after ending");
            Near(Get<float>(spin, "hitInterval"), 0.15f, "Tick interval is 0.15 seconds");
            body.position = new Vector2(0f, 2f);
            for (int i = 0; i < 120; i++)
            {
                Call(movement, "UpdateMovement", Time.time);
                Physics2D.Simulate(Time.fixedDeltaTime);
            }
            Check(movement.IsOnGround, "Validation player settles onto bare floor");
            Vector2 origin = spin.Center;
            var left = Target("Left", origin + Vector2.left, true);
            var right = Target("Right", origin + Vector2.right);
            var outside = Target("Outside", origin + Vector2.right * 3f);
            Physics2D.SyncTransforms();
            Keys(keyboard, Key.D);
            Check(spin.BlocksActions && !spin.IsSpinning, "Real D press reserves skill before the physics step");
            float start = Time.time;
            Call(spin, "UpdateSpin", start);
            Check(spin.IsSpinning, "Holding D starts channel on ground");
            Near(left.TotalDamage, 3f, "Left target takes one tick despite two colliders");
            Near(right.TotalDamage, 3f, "Right target takes the same tick");
            Near(outside.TotalDamage, 0f, "Outside target takes no damage");
            Near(health.CurrentHealth, health.MaxHealth, "Spin never damages its owner");
            Near(rhythm.CurrentGauge, 75f, "Successful skill ticks use half normal rhythm gain per target");
            Call(spin, "UpdateSpin", start + 0.14f);
            Near(left.TotalDamage, 3f, "No damage between ticks");
            Call(spin, "UpdateSpin", start + 0.15f);
            Near(left.TotalDamage, 6f, "Next tick damages each target once again");
            Set(rhythm, "currentGauge", 750f);
            Call(spin, "UpdateSpin", start + 0.30f);
            Near(left.TotalDamage, 10.5f, "Fever multiplies skill damage by 1.5");

            Keys(keyboard, Key.D, Key.RightArrow, Key.Z, Key.C, Key.X, Key.A, Key.S, Key.DownArrow);
            Call(movement, "Update");
            Call(movement, "UpdateMovement", Time.time);
            Call(attack, "UpdateAttack", Time.time);
            Call(chord, "UpdateSkill", Time.time);
            Check(!Get<bool>(movement, "jumpRequested") && !Get<bool>(movement, "slideRequested") &&
                !movement.IsSliding && !movement.IsCrouching, "Jump, dash and crouch are blocked during spin");
            Check(!attack.IsAttacking && !Get<bool>(attack, "attackRequested") && !chord.BlocksActions,
                "Basic attacks, A and S are blocked during spin");
            Near(body.linearVelocity.x, Get<float>(movement, "moveSpeed") * 0.4f, "Sideways movement runs at 40 percent");
            Near(body.linearVelocity.y, 0f, "Jump input cannot launch the player");
            Call(visual, "LateUpdate");
            Check(Get<GameObject>(visual, "visualRoot").activeSelf, "Spin effects appear");
            Near(player.transform.rotation.eulerAngles.z, 0f, "Physics body remains upright");

            float eventDamage = 0f;
            health.Damaged += hit => eventDamage = hit.Damage;
            health.ResetHealth();
            Check(health.TryTakeDamage(new AttackHit(20f, origin, Vector2.left, 0)), "Incoming hit is accepted during spin");
            Near(health.CurrentHealth, 86f, "20 incoming damage becomes 14 during spin");
            Near(eventDamage, 14f, "Damage event reports the reduced amount");
            Check(!health.TryTakeDamage(20f), "Existing post-hit invulnerability still prevents duplicate damage");
            Check(spin.IsSpinning, "Nonlethal damage does not cancel channel");

            Keys(keyboard);
            Check(!spin.IsSpinning && !spin.BlocksActions, "Releasing D stops immediately without another physics step");
            Near(Get<float>(spin, "nextUseTime"), Time.time + 6f, "Release starts the 6 second cooldown");
            Near(spin.DamageTakenMultiplier, 1f, "Release immediately removes damage reduction");
            Call(visual, "LateUpdate");
            Check(!Get<GameObject>(visual, "visualRoot").activeSelf, "Release hides spin effects");
            Call(movement, "UpdateMovement", Time.time);
            Call(attack, "UpdateAttack", Time.time);
            Check(!movement.IsSliding && !attack.IsAttacking && !chord.BlocksActions,
                "Blocked inputs do not activate after release");
            health.ResetHealth();
            health.TryTakeDamage(20f);
            Near(health.CurrentHealth, 80f, "Damage outside spin remains unmodified");
            Keys(keyboard, Key.D);
            Check(!spin.BlocksActions, "D cannot restart during cooldown");
            Keys(keyboard);
            Set(spin, "nextUseTime", 0f);
            Keys(keyboard, Key.D);
            Call(spin, "UpdateSpin", start);
            Check(spin.IsSpinning, "Fresh press starts again after cooldown");
            Call(spin, "UpdateSpin", start + 4f);
            Check(!spin.IsSpinning, "Holding D ends at maximum duration");
            Near(Get<float>(spin, "nextUseTime"), start + 10f, "Maximum duration starts cooldown at its end");
            Set(spin, "nextUseTime", 0f);
            Call(spin, "UpdateSpin", start + 20f);
            Check(!spin.BlocksActions, "Continued hold cannot automatically start another channel");

            Keys(keyboard);
            body.position = new Vector2(0f, 8f);
            Physics2D.SyncTransforms();
            Physics2D.Simulate(Time.fixedDeltaTime);
            Keys(keyboard, Key.D);
            Call(spin, "UpdateSpin", Time.time);
            Check(!spin.BlocksActions, "Airborne D press cannot start or reserve the skill");
            body.position = new Vector2(0f, 0.9f);
            body.linearVelocity = Vector2.zero;
            Keys(keyboard);
            for (int i = 0; i < 60; i++) Physics2D.Simulate(Time.fixedDeltaTime);
            Check(movement.IsOnGround, "Player lands for cancellation checks");
            Keys(keyboard, Key.A);
            Keys(keyboard, Key.A, Key.D);
            Check(chord.BlocksActions && !spin.BlocksActions, "D cannot interrupt an A skill request");
            chord.enabled = false;
            chord.enabled = true;
            Keys(keyboard);
            Keys(keyboard, Key.D);
            Call(spin, "UpdateSpin", Time.time);
            Check(spin.IsSpinning, "Channel starts before focus-loss check");
            Call(spin, "OnApplicationFocus", false);
            Check(!spin.BlocksActions && spin.DamageTakenMultiplier == 1f, "Focus loss removes channel and protection");
            Keys(keyboard);
            Set(spin, "nextUseTime", 0f);
            Keys(keyboard, Key.D);
            Call(spin, "UpdateSpin", Time.time);
            health.ResetHealth();
            health.TryTakeDamage(1000f);
            Check(health.IsDead && !spin.BlocksActions, "Lethal damage immediately ends channel");
            Keys(keyboard);
            health.ResetHealth();
            Set(spin, "nextUseTime", 0f);
            Keys(keyboard, Key.D);
            Call(spin, "UpdateSpin", Time.time);
            spin.enabled = false;
            Check(!spin.BlocksActions && spin.DamageTakenMultiplier == 1f, "Disabling skill clears all action locks and protection");
            Keys(keyboard);
            spin.enabled = true;
            Check(!spin.BlocksActions, "Re-enabling never restores a stale channel");
        }
        catch (Exception exception) { Results.Add("FAIL: " + exception); exitCode = 1; }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            if (Results.Exists(result => result.StartsWith("FAIL:"))) exitCode = 1;
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            string report = string.Join("\n", Results);
            File.WriteAllText(Path.Combine(Application.dataPath, "../guitar-spin-results.txt"), report);
            Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }
    }
}

public class GuitarSpinValidationTarget : Damageable
{
    public float TotalDamage { get; private set; }
    public override bool TryTakeDamage(float damage)
    {
        if (damage <= 0f) return false;
        TotalDamage += damage;
        return true;
    }
}
