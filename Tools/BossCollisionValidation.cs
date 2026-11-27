// Run with Tools/ValidateCombatFeel.ps1 -Suite BossCollision in an isolated project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BossCollisionValidation
{
    private const string Pending = "BossCollisionValidation.Pending";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Results = new List<string>();

    static BossCollisionValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            CheckInPlayMode();
        };
    }

    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Check(bool condition, string message)
    {
        Results.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the isolated validation runner.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void CheckInPlayMode()
    {
        int exitCode = 0;
        try
        {
            Time.timeScale = 1f;
            Physics2D.simulationMode = SimulationMode2D.Script;
            var floor = new GameObject("Validation floor", typeof(BoxCollider2D));
            floor.transform.position = Vector2.down * 0.5f;
            floor.GetComponent<BoxCollider2D>().size = new Vector2(200f, 1f);
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss1.prefab"));
            var movement = player.GetComponent<PlayerMovement>();
            var attack = player.GetComponent<PlayerAttack>();
            var owner = boss.GetComponent<Boss1ProximityAttack>();
            var body = player.GetComponent<Rigidbody2D>();
            var bossBody = boss.GetComponent<Rigidbody2D>();
            var box = player.GetComponent<BoxCollider2D>();
            var bossBox = boss.GetComponent<BoxCollider2D>();
            Vector2 standingSize = box.size;
            Vector2 standingOffset = box.offset;
            // 피격 연출과 사망이 수동 물리 시계의 충돌 검사를 중단하지 않도록 한다.
            player.GetComponent<PlayerHealth>().enabled = false;
            player.GetComponent<PlayerHitStop>().enabled = false;
            boss.GetComponent<Boss1Health>().enabled = false;
            body.position = new Vector2(2f, 0.75f);
            bossBody.position = new Vector2(2f, 0.02f);
            Call(owner, "Start");
            owner.BarrageActive = true;
            const float dt = 0.02f;
            float time = 0f;
            void Step()
            {
                Call(attack, "UpdateAttack", time);
                Call(movement, "UpdateMovement", time);
                Call(owner, "FixedUpdate");
                Physics2D.Simulate(dt);
                time += dt;
            }
            for (int i = 0; i < 30; i++) Step();
            Check(Physics2D.GetIgnoreCollision(box, bossBox) && box.Distance(bossBox).isOverlapped,
                "Real player and boss bodies overlap with physical collisions ignored");
            Call(movement, "UpdateCrouch", true);
            Physics2D.SyncTransforms();
            Set(movement, "moveInput", 1f);
            Call(movement, "UpdateMovement", time);
            Check(!movement.IsCrouching && box.size == standingSize && box.offset == standingOffset && body.linearVelocity.x > 0f,
                "Releasing crouch inside the real boss restores standing and horizontal movement");

            var ceiling = new GameObject("Validation ceiling", typeof(BoxCollider2D));
            var ceilingBox = ceiling.GetComponent<BoxCollider2D>();
            Call(movement, "UpdateCrouch", true);
            Physics2D.SyncTransforms();
            ceilingBox.size = new Vector2(3f, 0.2f);
            ceiling.transform.position = new Vector2(box.bounds.center.x, box.bounds.max.y + 0.2f);
            Physics2D.SyncTransforms();
            Call(movement, "UpdateMovement", time);
            Check(movement.IsCrouching && body.linearVelocity.x == 0f,
                "A real low ceiling still prevents standing inside the boss");
            ceilingBox.enabled = false;
            Call(movement, "UpdateMovement", time);
            Check(!movement.IsCrouching && box.size == standingSize && body.linearVelocity.x > 0f,
                "Removing the ceiling immediately restores movement");
            ceilingBox.enabled = true;
            ceilingBox.isTrigger = true;
            Call(movement, "UpdateCrouch", true);
            Physics2D.SyncTransforms();
            Call(movement, "UpdateMovement", time);
            Check(!movement.IsCrouching, "Trigger hitboxes do not prevent standing");
            Object.DestroyImmediate(ceiling);

            owner.BarrageActive = false;
            int crouches = 0;
            for (int i = 0; i < 10000; i++)
            {
                Set(movement, "moveInput", Mathf.Sign(bossBody.position.x - body.position.x));
                if (i % 30 == 0) Set(attack, "attackRequested", true);
                if (i % 170 == 0) Set(movement, "slideRequested", true);
                if (i % 211 == 0) Set(movement, "jumpRequested", true);
                if (i % 137 == 0 && !movement.IsSliding && !attack.IsAttacking)
                {
                    Call(movement, "UpdateCrouch", true);
                    crouches++;
                }
                Step();
                if (movement.IsCrouching) throw new Exception($"Crouch remained locked at physics step {i}.");
                if (!Physics2D.GetIgnoreCollision(box, bossBox)) throw new Exception($"Body collision resumed at physics step {i}.");
            }
            Check(crouches > 0, $"10000 physics steps of movement, attacks, slides and jumps finish without crouch lock ({crouches} crouches)");
            CheckLeapTrajectory(boss, player);
            CheckLeapBodyHitbox(boss, player);
        }
        catch (Exception exception)
        {
            Results.Add("FAIL: " + exception);
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            File.WriteAllLines(Path.Combine(Application.dataPath, "../boss-collision-results.txt"), Results);
            EditorApplication.Exit(exitCode);
        }
    }

    private static void CheckLeapTrajectory(GameObject boss, GameObject player)
    {
        var leap = boss.GetComponent<Boss1LeapSlam>();
        var body = boss.GetComponent<Rigidbody2D>();
        var playerBody = player.GetComponent<Rigidbody2D>();
        var owner = boss.GetComponent<Boss1ProximityAttack>();
        owner.BarrageActive = true;
        owner.Timeline.Stop();
        boss.GetComponent<Boss1BoostRush>().Cancel();
        boss.GetComponent<Boss1Backstep>().Cancel();
        const float dt = 0.01f;
        foreach (float x in new[] { -6f, 6f })
        {
            leap.Cancel();
            body.position = new Vector2(0f, 0.02f);
            body.linearVelocity = Vector2.zero;
            playerBody.position = new Vector2(x, 0.75f);
            Physics2D.SyncTransforms();
            float gravity = body.gravityScale;
            Check(leap.TryBegin(player.transform, 1f, true), $"Leap toward {x}: starts");
            leap.Tick(0.8f);
            Vector2 landing = leap.LandingPoint;
            float startY = body.position.y;
            float previousY = startY;
            bool rising = true, vertical = true, falling = true, noEarlyImpact = true;
            // Moving the target after takeoff must not move the marker or redirect the dive.
            playerBody.position = new Vector2(-x, 0.75f);
            for (int step = 1; step <= 71; step++)
            {
                leap.Tick(dt);
                Physics2D.Simulate(dt);
                Vector2 position = body.position;
                if (step <= 35) rising &= position.y >= previousY - 0.001f;
                if (step == 35)
                    Check(Mathf.Abs(position.x - landing.x) < 0.001f &&
                        Mathf.Abs(position.y - startY - 2.5f * owner.Height * Mathf.Abs(boss.transform.lossyScale.y)) < 0.001f,
                        $"Leap toward {x}: reaches the point above landing at half flight");
                if (step > 35)
                {
                    vertical &= Mathf.Abs(position.x - landing.x) < 0.001f;
                    falling &= position.y <= previousY + 0.001f;
                }
                if (step < 70)
                    noEarlyImpact &= leap.CurrentPhase == Boss1LeapSlam.Phase.Airborne && !leap.WavesActive;
                previousY = position.y;
            }
            Check(rising && vertical && falling, $"Leap toward {x}: rises then descends vertically");
            Check(leap.LandingPoint == landing && noEarlyImpact,
                $"Leap toward {x}: landing stays locked and no impact occurs in flight");
            Check(leap.CurrentPhase == Boss1LeapSlam.Phase.Impact && leap.WavesActive &&
                Mathf.Abs(body.gravityScale - gravity) < 0.001f,
                $"Leap toward {x}: impact and waves begin at flight end (within one physics step) and gravity returns");
        }
        leap.Cancel();
        Check(leap.TryBegin(player.transform, 1f, true), "Cancelled leap can restart");
        leap.Tick(0.8f);
        leap.Tick(0.1f);
        leap.Cancel();
        Check(!leap.IsBusy && !leap.WavesActive && !leap.Hitbox.enabled && body.gravityScale > 0f,
            "Cancelling a rising leap clears the attack and restores gravity");
    }

    private static void CheckLeapBodyHitbox(GameObject boss, GameObject player)
    {
        var leap = boss.GetComponent<Boss1LeapSlam>();
        var body = boss.GetComponent<Rigidbody2D>();
        var bodyBox = boss.GetComponent<BoxCollider2D>();
        var playerBody = player.GetComponent<Rigidbody2D>();
        var playerBox = player.GetComponent<BoxCollider2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var attacks = boss.GetComponent<Boss1AttackHitboxes>();
        int hits = 0;
        float totalDamage = 0f;
        void Record(PlayerMovement target, AttackHit hit)
        {
            if (target != movement) return;
            hits++;
            totalDamage += hit.Damage;
        }
        void Place(Vector2 position)
        {
            playerBody.position = position;
            playerBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        playerBody.gravityScale = 0f;
        playerBox.size = Vector2.one * 0.08f;
        playerBox.offset = Vector2.zero;
        attacks.PlayerHit += Record;
        try
        {
            body.position = new Vector2(0f, 0.02f);
            Place(new Vector2(6f, 0.75f));
            Check(leap.TryBegin(player.transform, 1f, true), "Body hitbox test starts a leap");
            leap.Tick(0.8f);
            leap.Tick(0.2f);
            Physics2D.Simulate(0.02f);
            Place(bodyBox.bounds.center);
            leap.Tick(0f);
            Check(!leap.Hitbox.enabled && hits == 0, "Overlapping the boss during ascent deals no damage");
            Place(new Vector2(50f, 20f));
            leap.Tick(0.2f);
            Physics2D.Simulate(0.02f);
            Place(bodyBox.bounds.center);
            leap.Tick(0f);
            Check(leap.CurrentPhase == Boss1LeapSlam.Phase.Airborne && leap.Hitbox.enabled &&
                leap.Hitbox.isTrigger && leap.Hitbox.size == bodyBox.size && leap.Hitbox.offset == bodyBox.offset &&
                hits == 1 && Mathf.Abs(totalDamage - 10f) < 0.001f,
                "Descending body trigger follows the boss body and delivers one 10% hit in midair");
            leap.Tick(0f);
            Place(leap.LandingPoint + Vector2.up * 0.05f);
            leap.Tick(0.31f);
            leap.Tick(0.01f);
            Check(leap.CurrentPhase == Boss1LeapSlam.Phase.Impact && leap.Hitbox.enabled && hits == 1,
                "Body stays active after landing without repeat body or wave damage");
            Place(new Vector2(50f, 20f));
            leap.Tick(0.15f);
            Check(!leap.Hitbox.enabled, "Recovery disables the body hitbox");

            leap.Cancel();
            Place(new Vector2(-6f, 0.75f));
            Check(leap.TryBegin(player.transform, 1f, true), "A new leap clears previous body hits");
            leap.Tick(0.8f);
            Place(new Vector2(50f, 20f));
            leap.Tick(0.71f);
            // Enlarging the warning must not create a damaging circle at the landing point.
            Set(leap, "landingRadiusH", 10f);
            Place((Vector2)bodyBox.bounds.center + Vector2.right * (bodyBox.bounds.extents.x + 0.5f));
            leap.Tick(0f);
            Check(hits == 1, "Standing outside the boss body inside the warning radius deals no body damage");
            Place(bodyBox.bounds.center);
            leap.Tick(0f);
            Check(hits == 2 && Mathf.Abs(totalDamage - 20f) < 0.001f,
                "Landing impact hits the upper torso even above the old ground-circle radius");
            leap.Cancel();
            Check(!leap.Hitbox.enabled && !leap.WavesActive, "Cancelling disables the body hitbox and waves");
        }
        finally
        {
            attacks.PlayerHit -= Record;
        }
    }
}
