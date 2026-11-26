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
}
