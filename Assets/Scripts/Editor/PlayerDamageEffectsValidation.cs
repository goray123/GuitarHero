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
public static class PlayerDamageEffectsValidation
{
    private const string Pending = "PlayerDamageEffectsValidation.Pending";
    private static readonly List<string> Results = new List<string>();
    private static string ReportPath => Path.Combine(Application.dataPath, "../Temp/player-damage-effects-results.txt");

    static PlayerDamageEffectsValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                CheckInPlayMode();
            }
        };
    }

    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run this validation in a separate Unity batch process.");
        Results.Clear();
        Application.runInBackground = true;
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Effects/PlayerDamage/DamageFlash.shader");
        if (shader == null) throw new Exception("Damage flash shader was not imported");
        foreach (var message in ShaderUtil.GetShaderMessages(shader))
            if (message.severity.ToString() == "Error") throw new Exception(message.message);
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

    private static FieldInfo Field(object owner, string name) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);

    private static void Invoke(object owner, string name, params object[] arguments) =>
        owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, arguments);

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

    private static async Task WaitReal(float seconds)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + seconds;
        while (Time.realtimeSinceStartupAsDouble < deadline) await NextFrame();
    }

    private static BluntHitBurst[] Sparks() => Object.FindObjectsByType<BluntHitBurst>(FindObjectsSortMode.None);

    private static async void CheckInPlayMode()
    {
        GameObject player = null;
        GameObject cameraObject = null;
        Sprite temporarySprite = null;
        int exitCode = 0;
        try
        {
            Time.timeScale = 1f;
            await NextFrame();
            await NextFrame();
            cameraObject = new GameObject("Damage Feedback Test Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Vector3 cameraPosition = new Vector3(2f, 3f, -10f);
            cameraObject.transform.position = cameraPosition;
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            PlayerDamageEffects effects = player.GetComponent<PlayerDamageEffects>();
            PlayerHitStop hitStop = player.GetComponent<PlayerHitStop>();
            PlayerHitCameraShake shake = player.GetComponent<PlayerHitCameraShake>();
            Check(health != null && effects != null, "Player prefab connects health and damage feedback");
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                if (behaviour != health && behaviour != effects && behaviour != hitStop && behaviour != shake)
                    behaviour.enabled = false;
            player.GetComponent<Animator>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            var source = player.GetComponent<SpriteRenderer>();
            var originalMaterial = source.sharedMaterial;
            Color originalTint = new Color(0.75f, 0.85f, 0.95f, 0.7f);
            source.color = originalTint;
            var flash = player.transform.Find("Damage Flash").GetComponent<SpriteRenderer>();
            var overlay = Get<RawImage>(effects, "overlayImage");
            var overlayRoot = Get<GameObject>(effects, "overlayRoot");
            var overlayTexture = Get<Texture2D>(effects, "overlayTexture");
            Check(Get<float>(effects, "flashDuration") == 0.05f && !flash.enabled && !overlay.enabled,
                "Default flash lasts 50 ms and feedback starts hidden");
            Check(Get<float>(health, "invulnerabilityDuration") == 1f &&
                Get<float>(effects, "overlayDuration") == 1f && Get<float>(effects, "blinkInterval") == 0.1f,
                "Prefab configures one-second protection and red edges with 100 ms blinking");
            Check(Get<float>(hitStop, "damageDuration") == 0.05f && Get<float>(shake, "damageDuration") == 0.2f,
                "Prefab configures 50 ms damage hitstop and 200 ms camera shake");
            Check(!overlay.raycastTarget && overlayRoot.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay,
                "Damage edges cover the screen without blocking input");
            Check(Get<Material>(effects, "flashMaterial") != null && Get<BluntHitBurst>(effects, "sparkPrefab") != null,
                "Flash material and spark prefab references resolve");
            int events = 0;
            bool reentrantDamage = false;
            AttackHit lastHit = default;
            health.Damaged += hit =>
            {
                events++;
                lastHit = hit;
                if (events == 1) reentrantDamage = health.TryTakeDamage(1f);
            };
            foreach (float damage in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon })
                Check(!health.TryTakeDamage(damage), "Reject invalid or ineffective damage: " + damage);
            Check(events == 0 && !flash.enabled && !overlay.enabled && !hitStop.IsStopped && Sparks().Length == 0,
                "Rejected damage produces no feedback or hitstop");

            var contact = new Vector2(0.3f, 0.6f);
            Damageable receiver = health;
            Check(receiver.TryTakeDamage(new AttackHit(12f, contact, Vector2.left, 8)), "Boss-style AttackHit is accepted");
            Check(events == 1 && health.CurrentHealth == 88f && lastHit.Point == contact && lastHit.Direction == Vector2.left,
                "Damage event preserves contact and direction and fires exactly once");
            Check(!reentrantDamage && health.IsInvulnerable && !health.TryTakeDamage(5f) &&
                !receiver.TryTakeDamage(new AttackHit(5f, contact, Vector2.right, 9)) && events == 1,
                "Protection rejects simultaneous, reentrant and float-overload damage without replaying feedback");
            Check(hitStop.IsStopped && Time.timeScale == 0f, "Accepted player damage immediately freezes game time");
            Check(overlay.enabled && Mathf.Approximately(overlay.color.a, Get<Color>(effects, "overlayColor").a),
                "Red edges appear at full intensity immediately on damage");
            Check(flash.enabled && flash.sprite == source.sprite && flash.color.a == source.color.a,
                "Flash immediately covers the current sprite while preserving source alpha");
            var sparks = Sparks();
            Check(sparks.Length == 1 && (Vector2)sparks[0].transform.position == contact &&
                sparks[0].GetComponent<SpriteRenderer>().color == Get<Color>(effects, "sparkColor"),
                "One red spark appears at the actual contact");
            Check(source.sharedMaterial == originalMaterial && source.color == originalTint,
                "Damage feedback preserves the original material and tint");

            temporarySprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            source.sprite = temporarySprite;
            source.flipX = true;
            source.flipY = true;
            source.sortingOrder = 12;
            Invoke(effects, "LateUpdate");
            Check(flash.sprite == temporarySprite && flash.flipX && flash.flipY && flash.sortingOrder == 13,
                "Flash follows animation changes, facing and sorting order");

            double protectionEnd = Get<double>(health, "invulnerableUntil");
            double feedbackStart = Get<double>(effects, "feedbackStartedAt");
            bool sawHidden = false, sawVisible = false, sawCameraOffset = false;
            while (Time.unscaledTimeAsDouble < feedbackStart + 0.65d)
            {
                await NextFrame();
                sawHidden |= source.forceRenderingOff;
                sawVisible |= !source.forceRenderingOff;
                sawCameraOffset |= Vector3.Distance(cameraObject.transform.position, cameraPosition) > 0.00001f;
            }
            Check(!hitStop.IsStopped && Time.timeScale == 1f && !flash.enabled,
                "Hitstop restores game time and the short flash ends");
            Check(sawHidden && sawVisible && health.IsInvulnerable, "Sprite alternates visibility while protection is active");
            Check(sawCameraOffset, "Damage shakes the actual camera during the feedback sequence");
            Check(overlay.enabled && overlay.color.a > 0f && overlay.color.a < Get<Color>(effects, "overlayColor").a,
                "Red screen edges fade while invulnerability continues");
            Check(!health.TryTakeDamage(1f) && events == 1 &&
                Get<double>(health, "invulnerableUntil") == protectionEnd &&
                Get<double>(effects, "feedbackStartedAt") == feedbackStart,
                "Blocked contacts do not extend protection or restart effects");
            await WaitReal(0.45f);
            Check(!health.IsInvulnerable && !source.forceRenderingOff && !flash.enabled && !overlay.enabled,
                "Protection, blinking and screen overlay all finish after one real second");
            Check(Vector3.Distance(cameraObject.transform.position, cameraPosition) < 0.00001f,
                "Camera returns to its original position without drift");
            Check(health.TryTakeDamage(1f) && events == 2, "Damage is accepted again after protection expires");

            hitStop.enabled = false;
            Check(Time.timeScale == 1f && !hitStop.IsStopped, "Disabling hitstop during a freeze restores time");
            hitStop.enabled = true;
            health.ResetHealth();
            Time.timeScale = 0.5f;
            Invoke(hitStop, "OnHit", Vector2.zero, Vector2.right, 3);
            double attackResumeAt = Get<double>(hitStop, "resumeAt");
            Check(health.TryTakeDamage(1f) && hitStop.IsStopped && Get<double>(hitStop, "resumeAt") >= attackResumeAt,
                "Simultaneous attack and damage share one hitstop without shortening the stronger attack");
            await WaitReal(0.2f);
            Check(!hitStop.IsStopped && Time.timeScale == 0.5f,
                "Overlapping hitstops restore the previous slow-motion scale exactly once");
            Time.timeScale = 1f;

            int sparkCount = Sparks().Length;
            health.enabled = false;
            Check(!health.TryTakeDamage(1f) && Sparks().Length == sparkCount, "Disabled health rejects damage without feedback");
            health.enabled = true;
            effects.enabled = false;
            Check(health.TryTakeDamage(1f) && !flash.enabled && Sparks().Length == sparkCount,
                "Disabling feedback preserves damage and stops its visuals");
            Check(!source.forceRenderingOff && !overlayRoot.activeSelf,
                "Disabling feedback restores sprite visibility and hides the screen overlay");
            effects.enabled = true;
            health.ResetHealth();
            health.TryTakeDamage(2f);
            Check(Sparks().Length == sparkCount + 1 && flash.enabled, "Re-enabled feedback subscribes only once");
            effects.enabled = false;
            Check(!flash.enabled && source.sharedMaterial == originalMaterial && source.color == originalTint,
                "Disabling feedback immediately clears the flash without modifying the sprite");
            effects.enabled = true;
            health.ResetHealth();
            hitStop.enabled = false;
            Time.timeScale = 0f;
            hitStop.enabled = true;
            Check(health.TryTakeDamage(1f) && !hitStop.IsStopped, "Damage does not take ownership of an existing pause");
            await WaitReal(1.1f);
            Check(Time.timeScale == 0f && !health.IsInvulnerable && !source.forceRenderingOff && !flash.enabled && !overlay.enabled,
                "Real-time feedback completes during a pause without unpausing the game");
            Time.timeScale = 1f;
            health.ResetHealth();
            Check(health.TryTakeDamage(1000f) && health.IsDead && flash.enabled, "Lethal damage still plays feedback");
            int eventsBefore = events;
            sparkCount = Sparks().Length;
            Check(!health.TryTakeDamage(1f) && events == eventsBefore && Sparks().Length == sparkCount,
                "Dead players reject further damage without replaying feedback");
            Object.Destroy(effects);
            await NextFrame();
            await NextFrame();
            Check(player.transform.Find("Damage Flash") == null && overlayRoot == null && overlayTexture == null &&
                !source.forceRenderingOff, "Removing feedback cleans up both overlays, texture and blinking");
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
            if (player != null) Object.Destroy(player);
            if (cameraObject != null) Object.Destroy(cameraObject);
            if (temporarySprite != null) Object.Destroy(temporarySprite);
            foreach (var spark in Sparks()) Object.Destroy(spark.gameObject);
            File.WriteAllText(ReportPath, string.Join("\n", Results));
            EditorApplication.Exit(exitCode);
        }
    }
}
