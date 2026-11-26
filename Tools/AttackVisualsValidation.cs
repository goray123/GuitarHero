// Run in the isolated project prepared by ValidateCombatFeel.ps1 -Suite AttackVisuals.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class AttackVisualsValidation
{
    private const string Pending = "AttackVisualsValidation.Pending";
    private static readonly List<string> Results = new List<string>();

    static AttackVisualsValidation()
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
        Results.Clear();
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

    private static FieldInfo Field(object owner, string name) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    private static void Call(object owner, string name, params object[] args) =>
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

    private static async Task WaitReal(float seconds)
    {
        double until = Time.realtimeSinceStartupAsDouble + seconds;
        while (Time.realtimeSinceStartupAsDouble < until) await NextFrame();
    }

    private static void StartSwing(PlayerAttack attack, int step, float facing = 1f, float vertical = 0f)
    {
        Call(attack, "StopAttack");
        // Standard swings use a grounded fixture; the down attack uses the airborne fixture.
        Set(attack, "movement", vertical < 0f ? attack.GetComponent<PlayerMovement>() : null);
        Set(attack, "comboStep", step - 1);
        Set(attack, "requestedVerticalDirection", vertical);
        Call(attack, "StartAttack", Time.time, facing);
        float active = Get<float>(attack, "activeDuration");
        Set(attack, "windupEndTime", Time.time - active * 0.6f);
        Set(attack, "activeEndTime", Time.time + active * 0.4f);
        Set(attack, "attackEndTime", Time.time + active * 0.4f + 0.2f);
        Call(attack, "UpdateAttack", Time.time);
        attack.GetComponent<Animator>().Update(0f);
        Physics2D.SyncTransforms();
    }

    private static float TotalAlpha(Mesh mesh)
    {
        float sum = 0f;
        foreach (Color color in mesh.colors) sum += color.a;
        return sum;
    }

    private static bool FlashVisible(EnemyHitFlash flash)
    {
        foreach (var overlay in Get<List<SpriteRenderer>>(flash, "overlays"))
            if (overlay != null && overlay.enabled && overlay.gameObject.activeInHierarchy) return true;
        return false;
    }

    private static void Capture(Camera camera, string name)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        var texture = new RenderTexture(1024, 640, 24);
        texture.Create();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name + ".png"), image.EncodeToPNG());
        RenderTexture.active = previous;
        Object.Destroy(image);
        texture.Release();
        Object.Destroy(texture);
    }

    private static async void CheckInPlayMode()
    {
        int exitCode = 0;
        try
        {
            Time.timeScale = 1f;
            await NextFrame();
            await NextFrame();
            var cameraObject = new GameObject("Attack Visuals Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.transform.position = new Vector3(0.7f, 0f, -10f);
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var light = new GameObject("Validation Light").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            player.transform.position = Vector3.zero;
            var attack = player.GetComponent<PlayerAttack>();
            var trail = player.GetComponent<PlayerAttackTrail>();
            var hitStop = player.GetComponent<PlayerHitStop>();
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                if (behaviour != attack && behaviour != trail && behaviour != hitStop && behaviour is not PlayerHitEffects)
                    behaviour.enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;

            var target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainingDummy.prefab"));
            target.transform.position = new Vector3(1.1f, 0f, 0f);
            target.GetComponentInChildren<Animator>().enabled = false;
            if (target.TryGetComponent<Rigidbody2D>(out var targetBody)) targetBody.simulated = false;
            var health = target.GetComponent<TrainingDummy>();
            var source = target.GetComponentInChildren<SpriteRenderer>();
            Material originalMaterial = source.sharedMaterial;
            source.color = new Color(0.7f, 0.8f, 0.9f, 0.65f);
            Color originalTint = source.color;
            var renderer = Get<MeshRenderer>(trail, "trailRenderer");
            var mesh = Get<Mesh>(trail, "mesh");
            Check(trail != null && renderer.sharedMaterial != null && !renderer.enabled,
                "Player prefab connects the trail material and starts with the trail hidden");

            foreach (int step in new[] { 1, 2, 3 })
            foreach (float facing in new[] { 1f, -1f })
            {
                StartSwing(attack, step, facing);
                Call(trail, "LateUpdate");
                BoxCollider2D box = attack.CurrentAttackHitbox;
                Check(renderer.enabled && Vector3.Distance(renderer.bounds.center, box.bounds.center) < 0.0001f &&
                    Vector3.Distance(renderer.bounds.size, box.bounds.size) < 0.0001f,
                    $"Attack {step}, facing {facing}: trail reaches the actual hitbox extents");
            }
            foreach (float vertical in new[] { 1f, -1f })
            {
                StartSwing(attack, 1, 1f, vertical);
                Call(trail, "LateUpdate");
                Check(attack.SwingDirection.y == vertical && renderer.enabled &&
                    Vector3.Distance(renderer.bounds.size, attack.CurrentAttackHitbox.bounds.size) < 0.0001f,
                    $"Directional attack {vertical}: trail follows the vertical attack bounds");
            }

            StartSwing(attack, 1);
            Call(trail, "LateUpdate");
            float whiffAlpha = TotalAlpha(mesh);
            double noPulse = Get<double>(trail, "pulseStartedAt");
            Check(target.GetComponent<EnemyHitFlash>() == null && double.IsNegativeInfinity(noPulse),
                "A whiff draws the range without flashing a target or pulsing the trail");
            Capture(camera, "attack-trail-whiff");
            attack.TryHit(attack.CurrentAttackHitbox, target.GetComponent<BoxCollider2D>());
            Call(trail, "LateUpdate");
            var flash = target.GetComponent<EnemyHitFlash>();
            Check(health.CurrentHealth < health.MaxHealth && flash != null && FlashVisible(flash),
                "Accepted damage flashes the struck enemy immediately");
            Check(Time.timeScale == 0f && TotalAlpha(mesh) > whiffAlpha,
                "Confirmed damage brightens the trail while the hit pose is frozen");
            Capture(camera, "attack-trail-impact");
            double flashStart = Get<double>(flash, "startedAt");
            double pulseStart = Get<double>(trail, "pulseStartedAt");
            float frozenProgress = attack.SwingProgress;
            attack.TryHit(attack.CurrentAttackHitbox, target.GetComponent<BoxCollider2D>());
            Check(Get<double>(flash, "startedAt") == flashStart && Get<double>(trail, "pulseStartedAt") == pulseStart,
                "Repeated contact in the same swing does not restart either flash");
            source.flipX = true;
            Call(flash, "LateUpdate");
            var overlay = Get<List<SpriteRenderer>>(flash, "overlays")[0];
            Check(overlay.sprite == source.sprite && overlay.flipX && source.sharedMaterial == originalMaterial && source.color == originalTint,
                "Enemy flash tracks the pose and facing without changing its original material or tint");

            var second = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainingDummy.prefab"));
            second.transform.position = new Vector3(1.5f, 0f, 0f);
            if (second.TryGetComponent<Rigidbody2D>(out var secondBody)) secondBody.simulated = false;
            attack.TryHit(attack.CurrentAttackHitbox, second.GetComponent<BoxCollider2D>());
            Check(second.GetComponent<EnemyHitFlash>() != null && FlashVisible(second.GetComponent<EnemyHitFlash>()),
                "A multi-target swing flashes each enemy that actually takes damage");

            // Disable automatic restoration to exercise both visual clocks during a full pause.
            Set(hitStop, "isStopped", false);
            Time.timeScale = 0f;
            await WaitReal(0.08f);
            Call(trail, "LateUpdate");
            Check(!FlashVisible(flash) && source.color == originalTint && source.sharedMaterial == originalMaterial,
                "Enemy flash ends in real time and leaves the source rendering intact");
            Check(Mathf.Abs(attack.SwingProgress - frozenProgress) < 0.0001f &&
                Mathf.Abs(TotalAlpha(mesh) - whiffAlpha) < 0.001f,
                "Impact pulse ends in real time while the underlying swing remains frozen");
            attack.CancelForSlide();
            Call(trail, "LateUpdate");
            Check(!renderer.enabled, "Slide cancellation clears the trail even while time is stopped");

            Time.timeScale = 1f;
            StartSwing(attack, 1);
            Set(attack, "windupEndTime", Time.time - Get<float>(attack, "activeDuration") - 0.01f);
            await WaitReal(0.12f);
            Check(!renderer.enabled, "The range trail fades out after the swing");
            StartSwing(attack, 2);
            trail.enabled = false;
            Check(!renderer.enabled, "Disabling trail feedback hides its renderer immediately");
            trail.enabled = true;
            StartSwing(attack, 1);
            Check(renderer.enabled && player.GetComponentsInChildren<MeshRenderer>().Length == 1,
                "Re-enabling feedback reuses one mesh without leaving duplicate trails");

            var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss1.prefab"));
            await NextFrame();
            var bodyVisual = boss.GetComponent<Boss1SlamSpriteVisual>();
            var bossFlash = boss.AddComponent<EnemyHitFlash>();
            bossFlash.Play(Get<Material>(player.GetComponent<PlayerHitEffects>(), "enemyFlashMaterial"), 0.045f);
            var bossSources = Get<List<SpriteRenderer>>(bossFlash, "sources");
            Check(bodyVisual.BodyRenderer != null && bossSources.Contains(bodyVisual.BodyRenderer) &&
                !bossSources.Exists(sprite => sprite.name.Contains("Flame") || sprite.name.Contains("Preview")),
                "Boss flash follows the animated body and excludes attack warnings and booster flames");
            bossFlash.enabled = false;
            Check(!FlashVisible(bossFlash), "Disabling enemy feedback clears all overlay sprites");

            health.TryTakeDamage(float.MaxValue);
            StartSwing(attack, 1);
            double beforeRejectedHit = Get<double>(trail, "pulseStartedAt");
            attack.TryHit(attack.CurrentAttackHitbox, target.GetComponent<BoxCollider2D>());
            Check(Get<double>(trail, "pulseStartedAt") == beforeRejectedHit && !FlashVisible(flash),
                "Rejected damage on a dead enemy triggers neither feedback effect");
            Object.Destroy(boss);
            Object.Destroy(second);
            Object.Destroy(target);
            Object.Destroy(player);
            await NextFrame();
        }
        catch (Exception exception)
        {
            Results.Add("FAIL: " + exception);
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            Time.timeScale = 1f;
            if (Results.Exists(result => result.StartsWith("FAIL:"))) exitCode = 1;
            File.WriteAllText(Path.Combine(Application.dataPath, "../attack-visuals-results.txt"), string.Join("\n", Results));
            EditorApplication.Exit(exitCode);
        }
    }
}
