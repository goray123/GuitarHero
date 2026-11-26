// Run with Tools/ValidateCombatFeel.ps1 -Suite BossHitbox in an isolated project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossHitboxValidation
{
    private static readonly List<string> Results = new List<string>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    private static void Check(bool condition, string message)
    {
        Results.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) throw new Exception(message);
    }
    private static void Set(Component target, string field, float value)
    {
        using var settings = new SerializedObject(target);
        settings.FindProperty(field).floatValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the isolated validation runner.");
        GameObject boss = null, player = null;
        int exitCode = 0;
        Results.Clear();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss1.prefab"));
            boss.transform.position = new Vector3(4f, 2f);
            boss.transform.localScale = Vector3.one * 1.4f;
            player = new GameObject("Body-side hitbox test", typeof(BoxCollider2D), typeof(PlayerMovement), typeof(PlayerHealth));
            player.GetComponent<BoxCollider2D>().size = Vector2.one * 0.08f;
            var health = player.GetComponent<PlayerHealth>();
            var owner = boss.GetComponent<Boss1ProximityAttack>();
            var attacks = boss.GetComponent<Boss1AttackHitboxes>();
            var rush = boss.GetComponent<Boss1BoostRush>();
            Call(owner, "Awake");
            Call(attacks, "Awake");
            Call(rush, "Awake");
            string[] fields = { "sweepBodyReachH", "punchBodyReachH", "slamBodyReachH" };
            foreach (float facing in new[] { -1f, 1f })
            {
                owner.GetType().GetProperty("Facing").GetSetMethod(true).Invoke(owner, new object[] { facing });
                for (int attack = 1; attack <= 3; attack++)
                {
                    owner.Timeline.Begin(attack);
                    owner.Timeline.Advance(owner.Timeline.Windup + owner.Timeline.Active * 0.5f);
                    attacks.GetArea(attack, out var originalCenter, out var originalSize);
                    float outer = originalCenter.x + originalSize.x * 0.5f;
                    foreach (float reach in new[] { 0f, 0.75f, 0f })
                    {
                        Set(attacks, fields[attack - 1], reach);
                        attacks.GetArea(attack, out var center, out var size);
                        Check(Mathf.Abs(center.x + size.x * 0.5f - outer) < 0.001f,
                            $"Melee {attack}/{facing}/{reach}: outer edge stays fixed");
                        Check(Mathf.Abs(center.x - size.x * 0.5f - (owner.BodyFront - reach * owner.Height)) < 0.001f &&
                            Mathf.Abs(size.y - originalSize.y) < 0.001f,
                            $"Melee {attack}/{facing}/{reach}: body edge moves without changing height");
                        player.transform.position = boss.transform.TransformPoint(new Vector2(facing * (owner.BodyFront - 0.4f * owner.Height), center.y));
                        health.ResetHealth();
                        Call(attacks, "OnDisable");
                        attacks.Evaluate();
                        float damage = reach > 0f ? (attack == 3 ? 10f : 6f) : 0f;
                        Check(Mathf.Abs(health.MaxHealth - health.CurrentHealth - damage) < 0.001f,
                            $"Melee {attack}/{facing}/{reach}: edited collider controls body-side damage");
                    }
                }
                owner.Timeline.Stop();
                attacks.Close();
                foreach (float reach in new[] { 0f, 0.75f, 0f })
                {
                    Set(rush, "bodyReachH", reach);
                    Check(rush.TryBegin(player.transform, owner.Height, facing, 1f, true), "Rush starts");
                    var body = boss.GetComponent<BoxCollider2D>();
                    var hitbox = rush.Hitbox;
                    float inner = facing * (hitbox.offset.x - body.offset.x) - hitbox.size.x * 0.5f;
                    Check(Mathf.Abs(inner - (body.size.x * 0.5f - reach * owner.Height)) < 0.001f,
                        $"Rush {facing}/{reach}: runtime body edge matches editor geometry");
                    player.transform.position = boss.transform.TransformPoint(body.offset + Vector2.right * facing * (body.size.x * 0.5f - 0.4f * owner.Height));
                    health.ResetHealth();
                    typeof(Boss1BoostRush).GetMethod("EvaluateHitbox", Private).Invoke(rush, new object[] { 0f });
                    Check(Mathf.Abs(health.MaxHealth - health.CurrentHealth - (reach > 0f ? 10f : 0f)) < 0.001f,
                        $"Rush {facing}/{reach}: edited collider controls body-side damage");
                    rush.Cancel();
                }
            }
        }
        catch (Exception exception)
        {
            Results.Add(exception.ToString());
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            if (player != null) Object.DestroyImmediate(player);
            if (boss != null) Object.DestroyImmediate(boss);
            File.WriteAllLines(Path.Combine(Application.dataPath, "../boss-hitbox-results.txt"), Results);
            EditorApplication.Exit(exitCode);
        }
    }
}
