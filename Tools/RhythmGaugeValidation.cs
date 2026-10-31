// Run through ValidateRhythmGauge.ps1 in an isolated copy of the project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RhythmGaugeValidation
{
    private const string Pending = "RhythmGaugeValidation.Pending";
    private static readonly List<string> Results = new List<string>();

    static RhythmGaugeValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            CheckInPlayMode();
        };
    }

    public static void Run()
    {
        Application.runInBackground = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Check(bool condition, string message)
    {
        string result = (condition ? "PASS: " : "FAIL: ") + message;
        Results.Add(result);
        Debug.Log(result);
    }

    private static void Near(float actual, float expected, string message) =>
        Check(Mathf.Abs(actual - expected) < 0.001f, message + " (actual " + actual + ", expected " + expected + ")");

    private static FieldInfo Field(object owner, string name) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    private static object Call(object owner, string name, params object[] args) =>
        owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);

    private static Task NextFrame()
    {
        var completion = new TaskCompletionSource<bool>();
        int frame = Time.frameCount;
        void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (Time.frameCount == frame) return;
            EditorApplication.update -= Tick;
            completion.SetResult(true);
        }
        EditorApplication.update += Tick;
        EditorApplication.QueuePlayerLoopUpdate();
        return completion.Task;
    }

    private static async Task WaitUntil(Func<bool> predicate, float timeout = 3f)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartupAsDouble >= deadline) throw new Exception("Runtime state timed out");
            await NextFrame();
        }
    }

    private static async Task WaitReal(float seconds)
    {
        double until = Time.realtimeSinceStartupAsDouble + seconds;
        await WaitUntil(() => Time.realtimeSinceStartupAsDouble >= until, seconds + 2f);
    }

    private static void GaugeRules(PlayerRhythmGauge gauge)
    {
        Near(gauge.CurrentGauge, 0f, "Prefab starts at zero");
        Check(gauge.Stage == RhythmStage.Low, "Spawn starts in Low");
        float[] points = { 0f, 249.99f, 250f, 499.99f, 500f, 749.99f, 750f, 1000f };
        RhythmStage[] stages =
        {
            RhythmStage.Low, RhythmStage.Low, RhythmStage.Normal, RhythmStage.Normal,
            RhythmStage.High, RhythmStage.High, RhythmStage.Fever, RhythmStage.Fever
        };
        for (int i = 0; i < points.Length; i++)
        {
            Call(gauge, "SetGauge", points[i]);
            Check(gauge.Stage == stages[i], "Exact stage boundary G=" + points[i]);
        }
        float[] speeds = { 1f, 1.25f, 1.5f, 2f };
        float[] damages = { 1f, 1f, 1.25f, 1.5f };
        float[] decays = { 12.5f, 25f, 50f, 100f };
        for (int i = 0; i < 4; i++)
        {
            Call(gauge, "SetGauge", i * 250f);
            Near(gauge.AttackSpeedMultiplier, speeds[i], "Stage speed " + i);
            Near(gauge.AttackDamageMultiplier, damages[i], "Stage damage " + i);
            Near(gauge.DecayPerSecond, decays[i], "Stage decay " + i);
        }

        gauge.ResetGauge();
        gauge.RecordConfirmedDamage(RhythmHitKind.Basic, 8f);
        Near(gauge.CurrentGauge, 30f, "Basic accepted damage awards 30");
        gauge.RecordConfirmedDamage(RhythmHitKind.Enhanced, 1f);
        Near(gauge.CurrentGauge, 90f, "Enhanced accepted damage awards 60 independently of damage amount");
        gauge.RecordConfirmedDamage(RhythmHitKind.Note, 5f);
        Near(gauge.CurrentGauge, 105f, "One note awards 15");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            gauge.RecordConfirmedDamage(RhythmHitKind.Basic, invalid);
        Near(gauge.CurrentGauge, 105f, "Rejected or non-finite damage awards nothing");
        gauge.NotifyBossDamageTaken(0f);
        Near(gauge.CurrentGauge, 105f, "Rejected boss damage does not reduce rhythm");
        gauge.NotifyBossDamageTaken(1f);
        Near(gauge.CurrentGauge, 5f, "One accepted boss hit removes 100");
        gauge.NotifyBossDamageTaken(999f);
        Near(gauge.CurrentGauge, 0f, "Boss damage clamps to zero independently of damage amount");
        Call(gauge, "SetGauge", 995f);
        gauge.RecordConfirmedDamage(RhythmHitKind.Basic, 8f);
        Near(gauge.CurrentGauge, 1000f, "Rewards clamp at 1000");

        Call(gauge, "SetGauge", 260f);
        Call(gauge, "Advance", 1f);
        Near(gauge.CurrentGauge, 242.5f, "Decay integrates remaining time below the Normal boundary");
        Call(gauge, "SetGauge", 760f);
        Call(gauge, "Advance", 1f);
        Near(gauge.CurrentGauge, 705f, "Decay integrates FEVER to High at the crossing");
        Call(gauge, "SetGauge", 1000f);
        Call(gauge, "Advance", 2.5f);
        Near(gauge.CurrentGauge, 750f, "1000 to 750 takes 2.5 gameplay seconds without hits");
        Call(gauge, "Advance", 0.1f);
        Near(gauge.CurrentGauge, 745f, "Decay immediately changes below 750");
        Call(gauge, "SetGauge", 1000f);
        Call(gauge, "Advance", 37.5f);
        Near(gauge.CurrentGauge, 0f, "Large delta integrates all four bands to zero");

        Call(gauge, "SetGauge", 500f);
        gauge.enabled = false;
        gauge.RecordConfirmedDamage(RhythmHitKind.Basic, 8f);
        Near(gauge.CurrentGauge, 0f, "Disabled owner clears rhythm and earns no rewards");
        gauge.enabled = true;
        Near(gauge.CurrentGauge, 0f, "Re-enabled player starts at zero");
    }

    private static void AttackRules(GameObject player, TrainingDummy dummy, TrainingDummy second)
    {
        var gauge = player.GetComponent<PlayerRhythmGauge>();
        var attack = player.GetComponent<PlayerAttack>();
        BoxCollider2D hitbox = Get<BoxCollider2D>(attack, "attack1Hitbox");
        BoxCollider2D target = dummy.GetComponent<BoxCollider2D>();
        Set(dummy, "maxHealth", 1000f);
        Set(second, "maxHealth", 1000f);
        Call(dummy, "Respawn");
        Call(second, "Respawn");
        Time.timeScale = 0f;

        Call(gauge, "SetGauge", 740f);
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time - 0.13f, 0f);
        Call(attack, "UpdateAttack", Time.time);
        Near(attack.CurrentAttackSpeedMultiplier, 1.5f, "Attack snapshots High speed");
        Near(Get<float>(attack, "windupDuration"), 0.125f / 1.5f, "Windup is divided by speed");
        float health = dummy.CurrentHealth;
        attack.TryHit(hitbox, target);
        Near(dummy.CurrentHealth, health - 10f, "High attack applies 8 x 1.25 damage");
        Near(gauge.CurrentGauge, 770f, "A real hit crosses into FEVER with +30");
        attack.TryHit(hitbox, target);
        Near(gauge.CurrentGauge, 770f, "Duplicate target collider does not award rhythm twice");
        Near(attack.CurrentAttackSpeedMultiplier, 1.5f, "FEVER gained mid-attack does not change its speed");
        Near(attack.CurrentAttackDamageMultiplier, 1.25f, "FEVER gained mid-attack does not change its damage");

        Call(attack, "StopAttack");
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time - 0.09f, 0f);
        Call(attack, "UpdateAttack", Time.time);
        Near(attack.CurrentAttackSpeedMultiplier, 2f, "Next attack uses FEVER speed");
        Call(gauge, "SetGauge", 0f);
        health = dummy.CurrentHealth;
        attack.TryHit(hitbox, target);
        Near(dummy.CurrentHealth, health - 12f, "Started FEVER attack keeps 8 x 1.5 damage after downgrade");
        Near(Get<float>(attack, "attackEndTime") - Get<float>(attack, "attackStartTime"), 0.1875f,
            "FEVER shortens the complete basic attack to 187.5 ms");

        Call(attack, "StopAttack");
        gauge.ResetGauge();
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time - 0.16f, 0f);
        Call(attack, "UpdateAttack", Time.time);
        attack.TryHit(hitbox, target);
        attack.TryHit(hitbox, second.GetComponent<BoxCollider2D>());
        attack.TryHit(hitbox, second.GetComponent<BoxCollider2D>());
        Near(gauge.CurrentGauge, 60f, "Two distinct enemies award 60, repeated contact awards nothing");

        Call(attack, "StopAttack");
        gauge.ResetGauge();
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time, 0f);
        Near(gauge.CurrentGauge, 0f, "Starting or missing an attack gives no rhythm");
        Call(attack, "StopAttack");

        Call(dummy, "Respawn");
        dummy.TryTakeDamage(2000f);
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time - 0.16f, 0f);
        Call(attack, "UpdateAttack", Time.time);
        attack.TryHit(hitbox, target);
        Near(gauge.CurrentGauge, 0f, "Dead targets do not award rhythm");
        Call(attack, "StopAttack");
        Call(dummy, "Respawn");
    }

    private static ChordNoteProjectile LaunchNote(GameObject player, float damage = 5f)
    {
        var skill = player.GetComponent<PlayerChordStrike>();
        var prefab = Get<ChordNoteProjectile>(skill, "notePrefab");
        var note = Object.Instantiate(prefab, player.transform.position, Quaternion.identity);
        note.Launch(player.transform, 1f, 0f, 36f, 0.3f, 3f, 10f, damage, ~0, 0, 10);
        return note;
    }

    private static void NoteRules(GameObject player, TrainingDummy dummy)
    {
        var gauge = player.GetComponent<PlayerRhythmGauge>();
        Collider2D target = dummy.GetComponent<Collider2D>();
        gauge.ResetGauge();
        Call(dummy, "Respawn");
        var note = LaunchNote(player);
        Call(note, "TryContact", target, Vector2.zero, Vector2.right);
        Call(note, "TryContact", target, Vector2.zero, Vector2.right);
        Near(gauge.CurrentGauge, 15f, "One projectile only awards 15 once");
        gauge.ResetGauge();
        for (int i = 0; i < 5; i++)
        {
            var item = LaunchNote(player);
            Call(item, "TryContact", target, Vector2.zero, Vector2.right);
        }
        Near(gauge.CurrentGauge, 75f, "All five accepted notes award 75");
        note = LaunchNote(player, 0f);
        Call(note, "TryContact", target, Vector2.zero, Vector2.right);
        Near(gauge.CurrentGauge, 75f, "Rejected projectile damage awards nothing");
        note = LaunchNote(player);
        gauge.ResetGauge();
        Call(note, "TryContact", target, Vector2.zero, Vector2.right);
        Near(gauge.CurrentGauge, 0f, "Projectile from the previous life does not refill reset rhythm");
    }

    private static void UIRules(GameObject player)
    {
        var gauge = player.GetComponent<PlayerRhythmGauge>();
        var ui = player.GetComponent<PlayerRhythmGaugeUI>();
        Check(ui != null, "Player prefab includes a connected rhythm HUD");
        Canvas canvas = Get<GameObject>(ui, "hudRoot").GetComponent<Canvas>();
        Check(canvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD renders as a screen overlay");
        Call(gauge, "SetGauge", 750f);
        Near(Get<Image>(ui, "fill").rectTransform.anchorMax.x, 0.75f, "HUD shows the actual gauge fill");
        Check(Get<Text>(ui, "stageText").text.Contains("FEVER"), "HUD updates the stage name immediately");
        Check(Get<Text>(ui, "statsText").text.Contains("2.00") && Get<Text>(ui, "statsText").text.Contains("1.50"),
            "HUD shows next-attack speed and damage multipliers");
        Call(gauge, "SetGauge", 1000f);
        Check(Get<Text>(ui, "valueText").text == "1000 / 1000", "HUD displays exact maximum gauge");
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
            Check(!graphic.raycastTarget, "HUD does not block gameplay input: " + graphic.name);
        Canvas.ForceUpdateCanvases();
        Check(Get<Image>(ui, "fill").rectTransform.rect.height > 0f, "HUD bar has a positive visible height");
        gauge.ResetGauge();
    }

    private static async Task TimeAndLandingRules(GameObject player, TrainingDummy dummy, TrainingDummy second)
    {
        var gauge = player.GetComponent<PlayerRhythmGauge>();
        var attack = player.GetComponent<PlayerAttack>();
        var stop = player.GetComponent<PlayerHitStop>();
        Call(gauge, "SetGauge", 300f);
        Time.timeScale = 0f;
        await WaitReal(0.08f);
        Near(gauge.CurrentGauge, 300f, "Menu pause freezes natural decay");
        Time.timeScale = 1f;
        Call(stop, "OnHit", Vector2.zero, Vector2.right, 1);
        Check(stop.IsStopped && Time.timeScale == 0f, "Real hitstop freezes gameplay");
        float stoppedGauge = gauge.CurrentGauge;
        while (stop.IsStopped)
        {
            Near(gauge.CurrentGauge, stoppedGauge, "Gauge holds while hitstop is active");
            await NextFrame();
        }
        Near(gauge.CurrentGauge, stoppedGauge, "Hitstop does not consume hidden rhythm time");
        await WaitReal(0.08f);
        Check(gauge.CurrentGauge < stoppedGauge, "Natural decay resumes when gameplay runs");

        dummy.transform.position = new Vector3(12f, 1f, 0f);
        second.transform.position = new Vector3(15f, 1f, 0f);
        player.transform.position = new Vector3(0f, 1f, 0f);
        var body = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<PlayerMovement>();
        body.linearVelocity = Vector2.zero;
        body.simulated = true;
        movement.enabled = true;
        await WaitUntil(() => movement.IsOnGround);
        int landings = 0;
        float landedAt = 0f;
        attack.SlamLanded += point => { landings++; landedAt = Time.time; };
        Call(gauge, "SetGauge", 750f);
        Set(attack, "comboStep", 2);
        Call(attack, "StartAttack", Time.time, 0f);
        Near(attack.CurrentAttackSpeedMultiplier, 2f, "Third attack snapshots FEVER speed");
        Call(gauge, "SetGauge", 0f);
        await WaitUntil(() => landings > 0);
        Near(Get<float>(attack, "activeEndTime") - landedAt, 0.0625f,
            "Landing active time retains the starting FEVER speed after gauge drops");
        Near(Get<float>(attack, "attackEndTime") - Get<float>(attack, "activeEndTime"), 0.0625f,
            "Landing recovery retains the starting FEVER speed");
        await WaitUntil(() => !attack.IsAttacking);
        Check(landings == 1, "Faster third attack lands once without getting stuck");
        Near(Get<float>(stop, "basicDuration"), 0.035f, "Attack speed leaves basic hitstop unchanged");
        Near(Get<float>(stop, "thirdDuration"), 0.085f, "Attack speed leaves heavy hitstop unchanged");
        Near(gauge.CurrentGauge, 0f, "Air attack without damage awards nothing");
    }

    private static async void CheckInPlayMode()
    {
        int exitCode = 0;
        try
        {
            await NextFrame();
            await NextFrame();
            var camera = new GameObject("Validation Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 1f, -10f);
            var floor = new GameObject("Validation Ground", typeof(BoxCollider2D));
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            var dummy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainingDummy.prefab")).GetComponent<TrainingDummy>();
            var second = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainingDummy.prefab")).GetComponent<TrainingDummy>();
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position = new Vector3(0f, 1f, 0f);
            dummy.transform.position = new Vector3(1f, 1f, 0f);
            second.transform.position = new Vector3(2f, 1f, 0f);
            Physics2D.SyncTransforms();
            var gauge = player.GetComponent<PlayerRhythmGauge>();
            Check(gauge != null, "Player prefab includes the rhythm component");
            GaugeRules(gauge);
            AttackRules(player, dummy, second);
            NoteRules(player, dummy);
            UIRules(player);
            await TimeAndLandingRules(player, dummy, second);
            GameObject ownedHUD = Get<GameObject>(player.GetComponent<PlayerRhythmGaugeUI>(), "hudRoot");
            Object.Destroy(player);
            Object.Destroy(dummy.gameObject);
            Object.Destroy(second.gameObject);
            await NextFrame();
            Check(ownedHUD == null, "Destroying the player removes its screen HUD, including inactive objects");
            Check(Time.timeScale == 1f, "Cleanup leaves gameplay running");
        }
        catch (Exception exception)
        {
            Results.Add("FAIL: " + exception);
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            if (Results.Exists(result => result.StartsWith("FAIL:"))) exitCode = 1;
            Time.timeScale = 1f;
            string report = string.Join("\n", Results);
            File.WriteAllText(Path.Combine(Application.dataPath, "../rhythm-gauge-results.txt"), report);
            Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }
    }
}
