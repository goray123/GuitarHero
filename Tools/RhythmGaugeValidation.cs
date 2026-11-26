// Run through ValidateRhythmGauge.ps1 in an isolated copy of the project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
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
        var validationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var arena = EditorSceneManager.OpenScene("Assets/Scenes/Boss01_Arena.unity", OpenSceneMode.Additive);
        foreach (var root in arena.GetRootGameObjects())
        {
            var rush = root.GetComponentInChildren<Boss1BoostRush>(true);
            if (rush == null) continue;
            var boss = Object.Instantiate(rush.gameObject);
            boss.name = "S Rush Validation Boss";
            boss.SetActive(false);
            SceneManager.MoveGameObjectToScene(boss, validationScene);
            break;
        }
        EditorSceneManager.CloseScene(arena, true);
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
        float[] speeds = { 0.75f, 1.25f, 1.5f, 2f };
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
        Near(gauge.CurrentGauge, 75f, "Basic accepted damage awards 75");
        gauge.RecordConfirmedDamage(RhythmHitKind.Enhanced, 1f);
        Near(gauge.CurrentGauge, 225f, "Enhanced accepted damage awards 150 independently of damage amount");
        gauge.RecordConfirmedDamage(RhythmHitKind.Note, 5f);
        Near(gauge.CurrentGauge, 262.5f, "One note awards 37.5");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            gauge.RecordConfirmedDamage(RhythmHitKind.Basic, invalid);
        Near(gauge.CurrentGauge, 262.5f, "Rejected or non-finite damage awards nothing");
        gauge.NotifyBossDamageTaken(0f);
        Near(gauge.CurrentGauge, 262.5f, "Rejected boss damage does not reduce rhythm");
        gauge.NotifyBossDamageTaken(1f);
        Near(gauge.CurrentGauge, 162.5f, "One accepted boss hit removes 100");
        gauge.NotifyBossDamageTaken(1f);
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

    private static void StartBasicContact(PlayerAttack attack, float speed)
    {
        float windup = Get<float>(attack, "basicWindupTime");
        float active = Get<float>(attack, "basicActiveTime");
        Set(attack, "comboStep", 0);
        // Contact delay is capped at half the active window.
        Call(attack, "StartAttack", Time.time - (windup + 0.75f * active) / speed, 0f);
        Call(attack, "UpdateAttack", Time.time);
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
        StartBasicContact(attack, 1.5f);
        Near(attack.CurrentAttackSpeedMultiplier, 1.5f, "Attack snapshots High speed");
        Near(Get<float>(attack, "windupDuration"), Get<float>(attack, "basicWindupTime") / 1.5f,
            "Windup is divided by speed");
        float health = dummy.CurrentHealth;
        attack.TryHit(hitbox, target);
        Near(dummy.CurrentHealth, health - 10f, "High attack applies 8 x 1.25 damage");
        Near(gauge.CurrentGauge, 815f, "A real hit crosses into FEVER with +75");
        attack.TryHit(hitbox, target);
        Near(gauge.CurrentGauge, 815f, "Duplicate target collider does not award rhythm twice");
        Near(attack.CurrentAttackSpeedMultiplier, 1.5f, "FEVER gained mid-attack does not change its speed");
        Near(attack.CurrentAttackDamageMultiplier, 1.25f, "FEVER gained mid-attack does not change its damage");

        Call(attack, "StopAttack");
        StartBasicContact(attack, 2f);
        Near(attack.CurrentAttackSpeedMultiplier, 2f, "Next attack uses FEVER speed");
        Call(gauge, "SetGauge", 0f);
        health = dummy.CurrentHealth;
        attack.TryHit(hitbox, target);
        Near(dummy.CurrentHealth, health - 12f, "Started FEVER attack keeps 8 x 1.5 damage after downgrade");
        float basicDuration = Get<float>(attack, "basicWindupTime") + Get<float>(attack, "basicActiveTime") +
            Get<float>(attack, "basicRecoveryTime");
        Near(Get<float>(attack, "attackEndTime") - Get<float>(attack, "attackStartTime"), basicDuration / 2f,
            "FEVER halves the complete basic attack time");

        Call(attack, "StopAttack");
        gauge.ResetGauge();
        StartBasicContact(attack, 0.75f);
        Near(attack.CurrentAttackSpeedMultiplier, 0.75f, "Basic attack snapshots Low speed");
        attack.TryHit(hitbox, target);
        attack.TryHit(hitbox, second.GetComponent<BoxCollider2D>());
        attack.TryHit(hitbox, second.GetComponent<BoxCollider2D>());
        Near(gauge.CurrentGauge, 150f, "Two distinct enemies award 150, repeated contact awards nothing");

        Call(attack, "StopAttack");
        gauge.ResetGauge();
        Set(attack, "comboStep", 0);
        Call(attack, "StartAttack", Time.time, 0f);
        Near(gauge.CurrentGauge, 0f, "Starting or missing an attack gives no rhythm");
        Call(attack, "StopAttack");

        Call(dummy, "Respawn");
        dummy.TryTakeDamage(2000f);
        StartBasicContact(attack, 0.75f);
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
        Near(gauge.CurrentGauge, 37.5f, "One projectile only awards 37.5 once");
        gauge.ResetGauge();
        for (int i = 0; i < 5; i++)
        {
            var item = LaunchNote(player);
            Call(item, "TryContact", target, Vector2.zero, Vector2.right);
        }
        Near(gauge.CurrentGauge, 187.5f, "All five accepted notes award 187.5");
        note = LaunchNote(player, 0f);
        Call(note, "TryContact", target, Vector2.zero, Vector2.right);
        Near(gauge.CurrentGauge, 187.5f, "Rejected projectile damage awards nothing");
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
        float basicHitStop = Get<float>(stop, "basicDuration");
        float thirdHitStop = Get<float>(stop, "thirdDuration");
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
        Near(Get<float>(attack, "activeEndTime") - landedAt, Get<float>(attack, "thirdLandingActiveTime") / 2f,
            "Landing active time retains the starting FEVER speed after gauge drops");
        Near(Get<float>(attack, "attackEndTime") - Get<float>(attack, "activeEndTime"), Get<float>(attack, "thirdRecoveryTime") / 2f,
            "Landing recovery retains the starting FEVER speed");
        await WaitUntil(() => !attack.IsAttacking);
        Check(landings == 1, "Faster third attack lands once without getting stuck");
        Near(Get<float>(stop, "basicDuration"), basicHitStop, "Attack speed leaves basic hitstop unchanged");
        Near(Get<float>(stop, "thirdDuration"), thirdHitStop, "Attack speed leaves heavy hitstop unchanged");
        Near(gauge.CurrentGauge, 0f, "Air attack without damage awards nothing");
    }

    private static void SlideCancelWindows(PlayerAttack attack)
    {
        float start = Get<float>(attack, "attackStartTime");
        float windupEnd = Get<float>(attack, "windupEndTime");
        float activeEnd = Get<float>(attack, "activeEndTime");
        float end = Get<float>(attack, "attackEndTime");
        string kind = Get<float>(attack, "verticalAttackDirection") != 0f ? "directional" : "combo " + Get<int>(attack, "comboStep");
        Check(attack.CanCancelWithSlide(start) && attack.CanCancelWithSlide(windupEnd - 0.001f),
            "Slide can cancel throughout windup: " + kind);
        Check(!attack.CanCancelWithSlide(windupEnd) && !attack.CanCancelWithSlide(activeEnd - 0.001f),
            "Slide cannot cancel during the active window: " + kind);
        Check(attack.CanCancelWithSlide(activeEnd) && attack.CanCancelWithSlide(end - 0.001f),
            "Slide can cancel throughout recovery: " + kind);
        Check(!attack.CanCancelWithSlide(end), "Slide cancel window ends with the attack: " + kind);
    }

    private static async Task DirectionalAttackRules(GameObject player, TrainingDummy dummy)
    {
        var attack = player.GetComponent<PlayerAttack>();
        var movement = player.GetComponent<PlayerMovement>();
        var body = player.GetComponent<Rigidbody2D>();
        var animator = player.GetComponent<Animator>();
        var collider = player.GetComponent<BoxCollider2D>();
        player.GetComponent<PlayerHitStop>().enabled = false;
        var previousBackground = InputSystem.settings.backgroundBehavior;
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        // 헤드리스 에디터에는 포커스를 줄 Game View가 없음.
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            Call(movement, "Update");
        }
        void Start(float vertical = 0f)
        {
            Call(attack, "StopAttack");
            Set(attack, "requestedVerticalDirection", vertical);
            Call(attack, "StartAttack", Time.time, 0f);
        }
        void ResolveInput()
        {
            Call(attack, "UpdateAttack", Time.time);
            Call(movement, "UpdateMovement", Time.time);
        }
        try
        {
            dummy.transform.position = new Vector3(12f, 1f, 0f);
            Call(attack, "StopAttack");
            await WaitUntil(() => movement.IsOnGround);
            Vector2 groundedPosition = body.position;
            float gravity = body.gravityScale;
            Set(attack, "comboStep", 0);
            Start();
            Check(Get<int>(attack, "comboStep") == 1, "Ground combo starts at one");
            SlideCancelWindows(attack);
            Start();
            Check(Get<int>(attack, "comboStep") == 2, "Ground combo advances to two");
            SlideCancelWindows(attack);
            Call(attack, "StopAttack");
            body.position = new Vector2(0f, 4f);
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            Physics2D.SyncTransforms();
            await WaitUntil(() => !movement.IsOnGround);
            foreach (int expected in new[] { 1, 2, 1, 2 })
            {
                Start();
                Check(Get<int>(attack, "comboStep") == expected, "Air combo loops one/two: " + expected);
                SlideCancelWindows(attack);
                Check(attack.GetAttackVelocity(new Vector2(4f, -2f), false, Time.time, 0.02f, gravity) == new Vector2(4f, -2f),
                    "Air basic attack preserves horizontal and vertical velocity");
            }
            float airAttackMoveSpeed = Get<float>(movement, "moveSpeed") * Get<float>(movement, "basicAttackMoveSpeedMultiplier");
            Keys(Key.LeftArrow);
            ResolveInput();
            Near(body.linearVelocity.x, -airAttackMoveSpeed, "Air attack can move left with the configured attack speed multiplier");
            Keys(Key.RightArrow);
            ResolveInput();
            Near(body.linearVelocity.x, airAttackMoveSpeed, "Air attack can change movement direction with the configured attack speed multiplier");
            Keys();
            Call(attack, "StopAttack");
            Set(attack, "comboResetAt", Time.time + Get<float>(attack, "comboResetTime"));
            body.position = groundedPosition;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = gravity;
            Physics2D.SyncTransforms();
            await WaitUntil(() => movement.IsOnGround);
            Start();
            Check(Get<int>(attack, "comboStep") == 3, "Air second attack continues into grounded third attack");
            SlideCancelWindows(attack);
            Start(1f);
            SlideCancelWindows(attack);
            Check(Get<int>(attack, "comboStep") == 0 && Get<float>(attack, "verticalAttackDirection") == 1f,
                "Ground upper attack resets the basic combo");
            Check(attack.GetAttackVelocity(new Vector2(4f, 2f), true, Time.time, 0.02f, gravity) == new Vector2(0f, 2f),
                "Ground upper attack neither lunges nor adds vertical movement");
            animator.Update(0f);
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.PlayerUpperAttack"), "Upper attack uses the supplied animation");
            float activeAt = Get<float>(attack, "windupEndTime") + Get<float>(attack, "contactDelayDuration") + 0.001f;
            Call(attack, "UpdateAttack", activeAt);
            BoxCollider2D upper = Get<BoxCollider2D>(attack, "upperAttackHitbox");
            BoxCollider2D down = Get<BoxCollider2D>(attack, "downAttackHitbox");
            Check(upper != null && upper.enabled && down != null && !down.enabled && !Get<BoxCollider2D>(attack, "attack1Hitbox").enabled,
                "Directional hitboxes are connected and mutually exclusive");
            Call(dummy, "Respawn");
            Physics2D.SyncTransforms();
            Vector2 hitDirection = Vector2.zero;
            int hitCombo = -1;
            void OnHit(Vector2 point, Vector2 direction, int combo) { hitDirection = direction; hitCombo = combo; }
            attack.HitConfirmed += OnHit;
            attack.TryHit(upper, dummy.GetComponent<BoxCollider2D>());
            attack.TryHit(upper, dummy.GetComponent<BoxCollider2D>());
            Near(dummy.MaxHealth - dummy.CurrentHealth, Get<float>(attack, "upperAttackDamage"), "Upper attack damages each target once");
            Check(hitDirection == Vector2.up && hitCombo == 0, "Upper hit reports upward direction outside the combo");
            Transform pivot = Get<Transform>(dummy, "reactionPivot");
            Near(pivot.localPosition.x, Get<Vector3>(dummy, "restPivotPosition").x, "Upper hit does not invent horizontal recoil");
            attack.HitConfirmed -= OnHit;
            Call(attack, "UpdateAttack", Get<float>(attack, "attackEndTime") + 0.01f);
            animator.Update(0f);
            Check(!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.PlayerUpperAttack"), "Upper animation exits after recovery");
            Start();
            Check(Get<int>(attack, "comboStep") == 1, "Basic attack restarts at one after upper attack");
            Start(-1f);
            Check(Get<float>(attack, "verticalAttackDirection") == 0f && Get<int>(attack, "comboStep") == 2,
                "Down attack cannot start on the ground");
            Call(attack, "StopAttack");
            body.position = new Vector2(0f, 4f);
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            Physics2D.SyncTransforms();
            await WaitUntil(() => !movement.IsOnGround);
            Set(attack, "comboStep", 1);
            Start();
            Check(Get<int>(attack, "comboStep") == 2, "Ground first attack continues into air second attack");
            foreach (float vertical in new[] { 1f, -1f })
            {
                Start(vertical);
                SlideCancelWindows(attack);
                Check(Get<int>(attack, "comboStep") == 0 && Get<float>(attack, "verticalAttackDirection") == vertical,
                    "Air directional attack resets combo: " + vertical);
                Check(attack.GetAttackVelocity(new Vector2(-4f, -2f), false, Time.time, 0.02f, gravity) == new Vector2(-4f, -2f),
                    "Directional air attack does not rise, dive, or bounce");
                activeAt = Get<float>(attack, "windupEndTime") + Get<float>(attack, "contactDelayDuration") + 0.001f;
                Call(attack, "UpdateAttack", activeAt);
                Check(upper.enabled == (vertical > 0f) && down.enabled == (vertical < 0f), "Only the requested vertical hitbox activates");
                if (vertical < 0f)
                {
                    animator.Update(0f);
                    Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.PlayerDownAttack"), "Down attack uses the supplied animation");
                    Call(dummy, "Respawn");
                    attack.TryHit(down, dummy.GetComponent<BoxCollider2D>());
                    Near(dummy.MaxHealth - dummy.CurrentHealth, Get<float>(attack, "downAttackDamage"), "Down attack uses its own damage");
                }
                Call(attack, "UpdateAttack", Get<float>(attack, "attackEndTime") + 0.01f);
                Check(!attack.IsAttacking && !Get<bool>(attack, "waitingForLanding"), "Directional attack finishes in the air without waiting for landing");
                Start();
                Check(Get<int>(attack, "comboStep") == 1, "Basic attack restarts at one after directional air attack");
            }
            Call(attack, "StopAttack");
            Keys(Key.UpArrow, Key.RightArrow, Key.X);
            ResolveInput();
            Check(Get<float>(attack, "verticalAttackDirection") == 1f && !Get<bool>(movement, "jumpRequested"),
                "Up plus X attacks upward without requesting a jump");
            Near(body.linearVelocity.x, Get<float>(movement, "moveSpeed"), "Diagonal input preserves full horizontal speed");
            Keys();
            Set(attack, "activeEndTime", Time.time - 0.01f);
            Set(attack, "attackEndTime", Time.time + 0.05f);
            Keys(Key.DownArrow, Key.X);
            Check(Get<bool>(attack, "attackRequested"), "Down plus X can queue in recovery");
            Keys();
            Call(attack, "UpdateAttack", Get<float>(attack, "attackEndTime") + 0.001f);
            Check(Get<float>(attack, "verticalAttackDirection") == -1f, "Buffered attack preserves direction after releasing the keys");
            Call(attack, "StopAttack");
            body.position = groundedPosition;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = gravity;
            Physics2D.SyncTransforms();
            await WaitUntil(() => movement.IsOnGround);
            Vector2 standingSize = collider.size;
            float footY = collider.bounds.min.y;
            Keys(Key.DownArrow);
            ResolveInput();
            Check(movement.IsCrouching && !movement.IsSliding && collider.size.y < standingSize.y, "Down arrow crouches with a shorter collider");
            Near(collider.bounds.min.y, footY, "Crouching keeps the feet fixed");
            animator.Update(0f);
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.PlayerSit"), "Crouching uses the supplied sit animation");
            Keys();
            ResolveInput();
            Check(!movement.IsCrouching && collider.size == standingSize, "Releasing down restores the standing collider");

            foreach (string phase in new[] { "windup", "active", "recovery" })
            {
                Set(attack, "comboStep", 0);
                Start();
                float elapsed = phase == "windup" ? 0f : Get<float>(attack, "windupDuration") +
                    (phase == "active" ? 0.001f : Get<float>(attack, "activeDuration") + 0.001f);
                Set(attack, "comboStep", 0);
                Call(attack, "StartAttack", Time.time - elapsed, 0f);
                Set(movement, "nextSlideTime", Time.time);
                Keys(Key.C);
                ResolveInput();
                if (phase == "active")
                {
                    Check(attack.IsAttacking && !movement.IsSliding, "C cannot interrupt an active attack");
                    float recoveryAt = Get<float>(attack, "activeEndTime");
                    Call(attack, "UpdateAttack", recoveryAt);
                    Call(movement, "UpdateMovement", recoveryAt);
                }
                Check(movement.IsSliding && !attack.IsAttacking && !Get<bool>(attack, "attackRequested"),
                    "C cancels attack and clears queued input from " + phase);
                Check(!Get<BoxCollider2D>(attack, "attack1Hitbox").enabled && !Get<BoxCollider2D>(attack, "attack2Hitbox").enabled,
                    "Canceled attack leaves no basic hitbox active");
                Keys();
                Set(movement, "slideEndTime", Time.time - 0.01f);
                ResolveInput();
                await WaitUntil(() => movement.IsOnGround);
            }

            var skill = player.GetComponent<PlayerChordStrike>();
            Near(Get<float>(skill, "cooldownTime"), 5f, "A skill defaults to a five-second cooldown");
            Keys(Key.A);
            Call(skill, "FixedUpdate");
            Check(skill.BlocksActions, "A skill starts when ready");
            float nextUse = Get<float>(skill, "nextUseTime");
            Near(nextUse - Get<float>(skill, "startedAt"), 5f, "Cooldown begins at actual skill activation");
            Keys();
            Set(skill, "startedAt", Time.time - Get<float>(skill, "duration") - 0.01f);
            Call(skill, "FixedUpdate");
            Keys(Key.A);
            Check(!skill.BlocksActions && !Get<bool>(skill, "requested"), "A pressed during cooldown is discarded");
            Set(skill, "requested", true);
            Call(skill, "FixedUpdate");
            Check(!skill.BlocksActions && Get<float>(skill, "nextUseTime") == nextUse,
                "Physics update rejects stale skill requests without extending cooldown");
            Keys();
            Set(skill, "nextUseTime", Time.time + 0.001f);
            Keys(Key.A);
            Check(!skill.BlocksActions, "A remains blocked immediately before cooldown expires");
            Keys();
            Set(skill, "nextUseTime", Time.time);
            Keys(Key.A);
            Call(skill, "FixedUpdate");
            Check(skill.BlocksActions, "A can activate exactly when its cooldown expires");
            Keys();
            skill.enabled = false;
            skill.enabled = true;
            Keys(Key.A);
            Check(!skill.BlocksActions, "Disabling and enabling the skill does not bypass cooldown");
            Keys();
            Call(attack, "StopAttack");
            Set(attack, "comboStep", 0);
            Keys(Key.X);
            ResolveInput();
            Check(attack.IsAttacking, "Skill cooldown leaves normal attacks available");
            Keys();
            Call(attack, "StopAttack");
            Set(movement, "nextSlideTime", Time.time);
            Keys(Key.C);
            ResolveInput();
            Check(movement.IsSliding && !attack.IsAttacking, "C starts slide rather than crouch or attack");
            Keys();
            Set(movement, "slideEndTime", Time.time - 0.01f);
            ResolveInput();
            await WaitUntil(() => movement.IsOnGround);
            Keys(Key.Z);
            ResolveInput();
            Check(body.linearVelocity.y > 0f && !attack.IsAttacking, "Z jumps rather than attacks");
            Keys();
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        }
    }

    private static void DownwardChordRules(GameObject player, TrainingDummy dummy)
    {
        var skill = player.GetComponent<PlayerChordStrike>();
        var movement = player.GetComponent<PlayerMovement>();
        var attack = player.GetComponent<PlayerAttack>();
        var body = player.GetComponent<Rigidbody2D>();
        var sprite = player.GetComponent<SpriteRenderer>();
        var gauge = player.GetComponent<PlayerRhythmGauge>();
        var previousSimulation = Physics2D.simulationMode;
        var previousBackground = InputSystem.settings.backgroundBehavior;
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            Call(movement, "Update");
        }
        void ClearNotes()
        {
            foreach (var note in Object.FindObjectsByType<ChordNoteProjectile>(FindObjectsSortMode.None))
                Object.DestroyImmediate(note.gameObject);
        }
        try
        {
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Call(attack, "StopAttack");
            movement.enabled = false;
            movement.enabled = true;
            float dt = Time.fixedDeltaTime;
            // Ground, ascending air, and falling air must all gain the same height.
            float[] starts = { body.position.y, 6f, 10f };
            float[] speeds = { 0f, 8f, -15f };
            for (int test = 0; test < starts.Length; test++)
            {
                Keys();
                skill.enabled = false;
                skill.enabled = true;
                ClearNotes();
                Set(skill, "nextUseTime", Time.time + 100f);
                Set(skill, "nextDownwardUseTime", 0f);
                body.position = new Vector2(0f, starts[test]);
                body.linearVelocity = Vector2.zero;
                sprite.flipX = test == 1;
                Physics2D.SyncTransforms();
                Physics2D.Simulate(dt);
                Check(movement.IsOnGround == (test == 0), "S starting contact state is ground, then airborne");
                starts[test] = body.position.y;
                body.linearVelocity = new Vector2(4f, speeds[test]);
                Keys(Key.S, Key.RightArrow);
                float start = Time.time;
                Call(skill, "UpdateSkill", start);
                Check(skill.BlocksActions && Get<bool>(skill, "downward"), "S starts independently of A cooldown: " + test);
                Near(Get<float>(skill, "nextDownwardUseTime") - start, 5f, "S cooldown starts at activation");
                Check(Object.FindObjectsByType<ChordNoteProjectile>(FindObjectsSortMode.None).Length == 0,
                    "S does not fire before the apex");
                Keys(Key.S, Key.LeftArrow, Key.Z, Key.C, Key.X, Key.A);
                float duration = Get<float>(skill, "duration");
                int steps = Mathf.RoundToInt(duration / dt);
                for (int i = 0; i < steps; i++)
                {
                    float current = start + i * dt;
                    Call(skill, "UpdateSkill", current);
                    Call(attack, "UpdateAttack", current);
                    Call(movement, "UpdateMovement", current);
                    Physics2D.Simulate(dt);
                }
                Near(body.position.x, 0f, "Movement input cannot move S horizontally");
                Near(body.position.y - starts[test], Get<float>(skill, "downwardJumpHeight"),
                    "S rises the same height regardless of starting height or velocity: " + test);
                Check(!movement.IsSliding && !attack.IsAttacking && sprite.flipX == (test == 1),
                    "Jump, slide, attack, A and facing changes stay blocked during S");
                Set(skill, "startedAt", Time.time - duration);
                Call(skill, "LateUpdate");
                float expectedTilt = test == 1 ? 60f : -60f;
                Near(Mathf.DeltaAngle(0f, Get<SpriteRenderer>(skill, "aimSprite").transform.localEulerAngles.z),
                    expectedTilt, "S visibly aims down 60 degrees at the apex");
                Set(skill, "startedAt", start);
                gauge.ResetGauge();
                Call(skill, "UpdateSkill", start + duration);
                var notes = Object.FindObjectsByType<ChordNoteProjectile>(FindObjectsSortMode.None);
                Check(notes.Length == 3 && !skill.BlocksActions, "Apex fires exactly three notes and immediately releases actions");
                var angles = new List<float>();
                Call(dummy, "Respawn");
                float health = dummy.CurrentHealth;
                foreach (var note in notes)
                {
                    Vector2[] path = Get<Vector2[]>(note, "path");
                    Vector2 end = path[path.Length - 1];
                    Check(end.y < 0f && (test == 1 ? end.x < 0f : end.x > 0f), "S note aims down in the captured facing direction");
                    angles.Add(Mathf.Atan2(-end.y, Mathf.Abs(end.x)) * Mathf.Rad2Deg);
                    Near(Get<float>(note, "range"), Get<float>(skill, "noteRange"), "S reuses A note range");
                    Near(Get<float>(note, "speed"), Get<float>(skill, "noteSpeed"), "S reuses A note speed");
                    Call(note, "TryContact", dummy.GetComponent<Collider2D>(), Vector2.zero, end.normalized);
                }
                angles.Sort();
                if (angles.Count == 3)
                    for (int i = 0; i < 3; i++) Near(angles[i], 42f + 18f * i, "S fan covers 42, 60 and 78 degrees below horizontal");
                Near(health - dummy.CurrentHealth, 3f * Get<float>(skill, "noteDamage"), "Three S notes apply the existing per-note damage");
                Near(gauge.CurrentGauge, 112.5f, "Three accepted S hits use the existing rhythm rewards");
                Keys(Key.RightArrow);
                Call(movement, "UpdateMovement", start + duration);
                Near(body.linearVelocity.x, Get<float>(movement, "moveSpeed"), "Movement resumes on the exact firing tick");
                Check(!Get<bool>(movement, "jumpRequested") && !Get<bool>(movement, "slideRequested") &&
                    !Get<bool>(attack, "attackRequested"), "Inputs pressed during S are discarded");
                Keys(Key.S);
                Check(!skill.BlocksActions, "S presses during cooldown are discarded");
                skill.enabled = false;
                Check(!sprite.forceRenderingOff && !Get<SpriteRenderer>(skill, "aimSprite").enabled && body.rotation == 0f,
                    "Cancel restores the original sprite without rotating the body collider");
                skill.enabled = true;
                Keys();
                Keys(Key.S);
                Check(!skill.BlocksActions, "Re-enabling S cannot bypass its cooldown");
            }
            Keys();
            Set(skill, "nextUseTime", Time.time);
            Keys(Key.A);
            Call(skill, "UpdateSkill", Time.time);
            Check(skill.BlocksActions && !Get<bool>(skill, "downward"), "A can start while S is cooling down");
            Call(skill, "UpdateSkill", Time.time + Get<float>(skill, "duration"));
            Keys();
            Set(skill, "nextDownwardUseTime", Time.time);
            Keys(Key.S);
            Call(skill, "UpdateSkill", Time.time);
            Check(skill.BlocksActions && Get<bool>(skill, "downward"), "S can start exactly at its cooldown boundary");
        }
        finally
        {
            skill.enabled = false;
            skill.enabled = true;
            ClearNotes();
            InputSystem.RemoveDevice(keyboard);
            Physics2D.simulationMode = previousSimulation;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        }
    }

    private static void DownwardChordRushRules(GameObject player)
    {
        Boss1BoostRush rush = null;
        foreach (var candidate in Object.FindObjectsByType<Boss1BoostRush>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.name == "S Rush Validation Boss") rush = candidate;
        Check(rush != null, "Rush dodge uses the actual boss arena overrides");
        if (rush == null) return;
        var owner = rush.GetComponent<Boss1ProximityAttack>();
        foreach (var behaviour in rush.GetComponents<MonoBehaviour>())
            if (behaviour != rush) behaviour.enabled = false;
        Set(owner, "player", player.transform);
        rush.gameObject.SetActive(true);
        Call(owner, "Start");
        var bossBody = rush.GetComponent<Rigidbody2D>();
        var body = player.GetComponent<Rigidbody2D>();
        var box = player.GetComponent<BoxCollider2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var skill = player.GetComponent<PlayerChordStrike>();
        var health = player.GetComponent<PlayerHealth>();
        Call(player.GetComponent<PlayerAttack>(), "StopAttack");
        player.GetComponent<PlayerDamageEffects>().enabled = false;
        health.enabled = true;
        Set(health, "invulnerabilityDuration", 0f);
        Set(movement, "moveInput", 0f);
        Physics2D.IgnoreCollision(box, rush.GetComponent<BoxCollider2D>());
        var previousSimulation = Physics2D.simulationMode;
        float configuredHeight = Get<float>(skill, "downwardJumpHeight");
        int hits = 0;
        void OnHit(PlayerMovement target, AttackHit hit) { if (target == movement) hits++; }
        rush.PlayerHit += OnHit;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            float dt = Time.fixedDeltaTime;
            // Include the old 0.8 height as a control, plus early/late timing in both directions.
            for (int test = 0; test < 5; test++)
            {
                float facing = test < 3 ? 1f : -1f;
                float lead = test == 2 || test == 4 ? 0.18f : 0.16f;
                float time = Time.time;
                rush.Cancel();
                skill.enabled = false;
                skill.enabled = true;
                Set(skill, "nextDownwardUseTime", 0f);
                Set(skill, "downwardJumpHeight", test == 0 ? 0.8f : configuredHeight);
                health.ResetHealth();
                hits = 0;
                body.position = new Vector2(5f * facing, 1f);
                bossBody.position = new Vector2(0f, 0.02f);
                body.linearVelocity = bossBody.linearVelocity = Vector2.zero;
                for (int i = 0; i < 30; i++)
                {
                    Call(movement, "UpdateMovement", time);
                    Physics2D.Simulate(dt);
                    time += dt;
                }
                Check(movement.IsOnGround, "S rush dodge starts on the bare floor");
                Check(rush.TryBegin(player.transform, owner.Height, facing, test % 2 == 0 ? 1f : 0.8f, true),
                    "Arena boost rush starts");
                while (rush.Stage == Boss1BoostRush.RushStage.Windup) rush.Tick(dt);
                float speed = Get<float>(rush, "travelDistance") * owner.Height * Mathf.Abs(rush.transform.lossyScale.y) /
                    Get<float>(rush, "travelTime");
                float front = rush.Hitbox.transform.TransformPoint(rush.Hitbox.offset).x +
                    facing * rush.Hitbox.size.x * Mathf.Abs(rush.transform.lossyScale.x) * 0.5f;
                Physics2D.SyncTransforms();
                float nearEdge = box.bounds.center.x - facing * box.bounds.extents.x;
                body.position += Vector2.right * (front + facing * speed * lead - nearEdge);
                Physics2D.SyncTransforms();
                Call(skill, "RequestSkill", true);
                bool passedPlayer = false;
                for (int i = 0; i < 100 && rush.Stage == Boss1BoostRush.RushStage.Moving; i++)
                {
                    Call(skill, "UpdateSkill", time);
                    Call(movement, "UpdateMovement", time);
                    rush.Tick(dt);
                    bossBody.linearVelocity = new Vector2(rush.DesiredSpeed, bossBody.linearVelocity.y);
                    Physics2D.Simulate(dt);
                    time += dt;
                    if ((bossBody.position.x - body.position.x) * facing > 2f) passedPlayer = true;
                }
                Check(passedPlayer, "Rush travels completely past the player during the dodge");
                Check(test == 0 ? hits == 1 && health.CurrentHealth < health.MaxHealth :
                    hits == 0 && health.CurrentHealth == health.MaxHealth,
                    test == 0 ? "Old 0.8-height S reproduces the boost-rush hit" :
                    $"Ground S avoids the full arena rush without invulnerability: facing={facing}, lead={lead:F2}s");
                foreach (var note in Object.FindObjectsByType<ChordNoteProjectile>(FindObjectsSortMode.None))
                    Object.DestroyImmediate(note.gameObject);
            }
        }
        finally
        {
            rush.PlayerHit -= OnHit;
            rush.Cancel();
            Set(skill, "downwardJumpHeight", configuredHeight);
            Physics2D.simulationMode = previousSimulation;
            Object.Destroy(rush.gameObject);
        }
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
            await DirectionalAttackRules(player, dummy);
            DownwardChordRules(player, dummy);
            DownwardChordRushRules(player);
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
