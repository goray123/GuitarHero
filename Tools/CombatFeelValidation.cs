// Copied to an isolated project's Assets/Editor by ValidateCombatFeel.ps1.
// This never changes the user's open scene or enters Play mode in their editor.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CombatFeelValidation
{
    private const string Pending = "CombatFeelValidation.Pending";
    private static readonly List<string> Results = new List<string>();

    static CombatFeelValidation()
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
        Debug.Log("Combat feel validation: entering Play mode");
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

    private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(object owner, string name) => (T)Field(owner, name).GetValue(owner);
    private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
    private static void Call(object owner, string name, params object[] arguments) =>
        owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, arguments);

    private static Task NextFrame()
    {
        // An empty headless editor has no Game view requesting player updates.
        // Wait for a real player frame rather than repeatedly draining editor tasks.
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

    private static async Task WaitUntil(Func<bool> predicate, float timeout = 2f)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Timed out waiting for runtime state");
            await NextFrame();
        }
    }

    private static async Task WaitReal(float seconds)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + seconds;
        await WaitUntil(() => Time.realtimeSinceStartupAsDouble >= deadline, seconds + 1f);
    }

    private static async void CheckInPlayMode()
    {
        int exitCode = 0;
        try
        {
            Debug.Log("Combat feel validation: Play mode checks started");
            Time.timeScale = 1f;
            // EnteredPlayMode precedes the first complete runtime update.
            await NextFrame();
            await NextFrame();
            GameObject cameraObject = new GameObject("Validation Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            Vector3 cameraRest = cameraObject.transform.position;
            GameObject floor = new GameObject("Validation Ground", typeof(BoxCollider2D));
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(30f, 1f);

            GameObject player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            GameObject target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainingDummy.prefab"));
            PlayerAttack attack = player.GetComponent<PlayerAttack>();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            PlayerHitStop hitStop = player.GetComponent<PlayerHitStop>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            TrainingDummy dummy = target.GetComponent<TrainingDummy>();
            Transform pivot = Get<Transform>(dummy, "reactionPivot");
            Transform healthBar = Get<GameObject>(dummy, "healthBar").transform;
            movement.enabled = false;
            body.simulated = false;
            player.transform.position = new Vector3(0f, 1f, 0f);
            target.transform.position = new Vector3(1f, 1f, 0f);

            Check(pivot != null && target.GetComponent<Animator>() == null && target.GetComponentInChildren<Animator>() != null,
                "Dummy visual animator is on a child, with an assigned foot pivot");
            Check(!healthBar.IsChildOf(pivot) && !target.GetComponent<BoxCollider2D>().transform.IsChildOf(pivot),
                "Health bar and hurtbox are outside the recoil hierarchy");
            HealthBarUI healthUI = Get<HealthBarUI>(dummy, "healthBarUI");
            Canvas healthCanvas = healthBar.GetComponent<Canvas>();
            Image currentFill = Get<Image>(healthUI, "currentFill");
            Image damageFill = Get<Image>(healthUI, "damageFill");
            Check(healthCanvas != null && healthCanvas.renderMode == RenderMode.WorldSpace,
                "Health bar uses a World Space Canvas");
            Check(healthBar.GetComponentsInChildren<SpriteRenderer>().Length == 0,
                "Health bar contains no SpriteRenderers");
            Check(Mathf.Abs(healthBar.localPosition.y - 1.12f) < 0.001f,
                "Canvas retains its position above the dummy");
            Check(currentFill.type == Image.Type.Filled && damageFill.type == Image.Type.Filled &&
                currentFill.fillMethod == Image.FillMethod.Horizontal && damageFill.fillMethod == Image.FillMethod.Horizontal &&
                currentFill.fillOrigin == 0 && damageFill.fillOrigin == 0,
                "Both UI fills drain horizontally from the right edge");
            Check(currentFill.sprite != null && damageFill.sprite != null &&
                currentFill.transform.GetSiblingIndex() > damageFill.transform.GetSiblingIndex() &&
                currentFill.color.g > currentFill.color.r && damageFill.color.r > damageFill.color.g,
                "Green actual health renders in front of orange damage trail");
            foreach (Image image in healthBar.GetComponentsInChildren<Image>())
                Check(!image.raycastTarget, "Health UI does not intercept input: " + image.name);
            Canvas.ForceUpdateCanvases();
            Check(currentFill.rectTransform.rect.width > 0f && currentFill.rectTransform.rect.height > 0f,
                "Canvas fill rectangles have positive visible dimensions");
            Check(currentFill.fillAmount == 1f && damageFill.fillAmount == 1f,
                "Spawn initializes both bars to full health");
            dummy.TryTakeDamage(10f);
            Check(Mathf.Approximately(currentFill.fillAmount, 0.8f) && damageFill.fillAmount == 1f,
                "Damage immediately reduces green while orange retains previous health");
            Call(healthUI, "Advance", 0.2f);
            Check(damageFill.fillAmount == 1f, "Damage trail holds for its initial delay");
            Call(healthUI, "Advance", 0.15f);
            float intermediateTrail = damageFill.fillAmount;
            Check(intermediateTrail > currentFill.fillAmount && intermediateTrail < 1f,
                "Orange smoothly catches up after the delay");
            dummy.TryTakeDamage(5f);
            Check(Mathf.Approximately(currentFill.fillAmount, 0.7f) && damageFill.fillAmount == intermediateTrail,
                "Repeated damage keeps the orange bar continuous without jumping up");
            Call(healthUI, "Advance", 0.2f);
            Check(damageFill.fillAmount == intermediateTrail, "Repeated damage restarts the hold delay");
            Time.timeScale = 0f;
            Call(healthUI, "Update");
            Check(damageFill.fillAmount == intermediateTrail, "Health trail freezes during hitstop or pause");
            Time.timeScale = 1f;
            Call(healthUI, "Advance", 0.5f);
            Check(Mathf.Approximately(damageFill.fillAmount, 0.7f) && healthUI.IsSettled,
                "Orange settles exactly at actual health");
            healthUI.SetHealth(45f, 50f);
            Check(Mathf.Approximately(currentFill.fillAmount, 0.9f) && currentFill.fillAmount == damageFill.fillAmount,
                "Healing resets both bars without a false damage trail");
            Call(dummy, "Respawn");
            Check(currentFill.fillAmount == 1f && damageFill.fillAmount == 1f,
                "Respawn clears the previous damage animation");
            dummy.TryTakeDamage(50f);
            Check(dummy.IsDead && currentFill.fillAmount == 0f && damageFill.fillAmount == 1f,
                "Lethal damage leaves the orange trail to finish");
            Set(dummy, "deathEndsAt", Time.time - 0.01f);
            Call(dummy, "UpdateState", Time.time);
            Check(healthBar.gameObject.activeSelf, "Death keeps the canvas visible while orange is draining");
            Call(healthUI, "Advance", 1f);
            Call(dummy, "UpdateState", Time.time);
            Check(!healthBar.gameObject.activeSelf && damageFill.fillAmount == 0f,
                "Finished death trail hides the canvas at zero health");
            Call(dummy, "Respawn");
            Check(healthBar.gameObject.activeSelf && currentFill.fillAmount == 1f && damageFill.fillAmount == 1f,
                "Dead dummy respawns with a visible full canvas");
            target.transform.position = new Vector3(1f, 1f, 0f);
            Check(Get<float>(hitStop, "basicDuration") == 0.035f && Get<float>(hitStop, "thirdDuration") == 0.085f,
                "Basic/heavy hitstop uses the tuned 35/85 ms profile");
            Check(Get<float>(attack, "attack1Damage") == 8f && Get<float>(attack, "attack2Damage") == 8f && Get<float>(attack, "attack3Damage") == 25f,
                "Existing 8/8/25 damage is preserved");
            Check(Get<float>(attack, "attackBufferTime") == 0.1f, "Attack queue window is 100 ms");
            PlayerHitAudio audio = player.GetComponent<PlayerHitAudio>();
            foreach (string clipField in new[] { "basicHitClip", "thirdHitClip", "swingClip", "resonanceClip", "landingClip" })
            {
                AudioClip clip = Get<AudioClip>(audio, clipField);
                Check(clip != null && clip.length > 0f, "Imported and connected audio: " + clipField);
            }
            Check(player.GetComponent<PlayerHitCameraShake>() != null, "Camera impact component is connected");

            foreach (float step in new[] { 0.02f, 1f / 60f })
            {
                Call(attack, "StopAttack");
                Set(attack, "comboStep", 0);
                Call(attack, "StartAttack", 0f, 1f);
                float distance = 0f;
                for (int i = 0; i < 30; i++)
                    distance += attack.GetAttackVelocity(Vector2.zero, true, i * step, step, body.gravityScale).x * step;
                Check(Mathf.Abs(distance - 0.8f) < 0.0001f, "Lunge preserves 0.8 units at dt=" + step);
                Check(attack.GetAttackVelocity(Vector2.zero, true, 0.14f, step, body.gravityScale).x > 0f,
                    "Lunge continues into the swing at dt=" + step);
            }

            Call(attack, "StopAttack");
            Set(attack, "comboStep", 0);
            float now = Time.time;
            Call(attack, "StartAttack", now, 0f);
            Call(attack, "OnAttack", default(InputAction.CallbackContext));
            Check(!Get<bool>(attack, "attackRequested"), "Early windup input is not queued");
            Call(attack, "UpdateAttack", now + 0.125f);
            Check(!Get<bool>(attack, "isDamageActive"), "Damage does not begin on the first swing frame");
            Call(attack, "StopAttack");
            Set(attack, "comboStep", 0);
            Call(attack, "StartAttack", now - 0.16f, 0f);
            Call(attack, "UpdateAttack", now);
            Check(Get<bool>(attack, "isDamageActive"), "Contact becomes active after the configured delay");
            int confirmed = 0;
            Vector2 contact = Vector2.zero;
            attack.HitConfirmed += (point, direction, combo) => { confirmed++; contact = point; };
            float health = dummy.CurrentHealth;
            Vector3 rootBefore = target.transform.position;
            Vector3 barBefore = healthBar.position;
            Vector3 pivotRest = pivot.localPosition;
            BoxCollider2D hurtbox = target.GetComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            Vector3 boundsBefore = hurtbox.bounds.center;
            attack.TryHit(Get<BoxCollider2D>(attack, "attack1Hitbox"), hurtbox);
            attack.TryHit(Get<BoxCollider2D>(attack, "attack1Hitbox"), hurtbox);
            Check(dummy.CurrentHealth == health - 8f && confirmed == 1, "A swing damages a target exactly once");
            Check(Mathf.Abs(contact.x - hurtbox.bounds.min.x) < Physics2D.defaultContactOffset + 0.002f,
                "Impact point lies on the target-facing surface: point=" + contact + ", bounds=" + hurtbox.bounds +
                ", flipX=" + player.GetComponent<SpriteRenderer>().flipX);
            Check(pivot.localPosition.x > pivotRest.x && target.transform.position == rootBefore && healthBar.position == barBefore && hurtbox.bounds.center == boundsBefore,
                "Directional recoil moves only the visual, leaving root, bar and hurtbox fixed");
            Check(hitStop.IsStopped && Time.timeScale == 0f, "Confirmed hit enters hitstop");
            Call(attack, "OnAttack", default(InputAction.CallbackContext));
            Check(Get<bool>(attack, "attackRequested"), "Input during hitstop is queued");
            double frozenAt = Time.realtimeSinceStartupAsDouble;
            int frozenFrame = Time.frameCount;
            bool recoilHeld = true;
            while (hitStop.IsStopped)
            {
                recoilHeld &= Get<float>(dummy, "reactionElapsed") == 0f;
                await NextFrame();
                if (Time.realtimeSinceStartupAsDouble - frozenAt > 2f)
                    throw new Exception("Hitstop did not restore: frames=" + (Time.frameCount - frozenFrame) +
                        ", enabled=" + hitStop.isActiveAndEnabled + ", playing=" + Application.isPlaying +
                        ", paused=" + EditorApplication.isPaused + ", now=" + Time.realtimeSinceStartupAsDouble +
                        ", resume=" + Get<double>(hitStop, "resumeAt"));
            }
            Check(recoilHeld, "Recoil holds during hitstop");
            Check(Time.timeScale == 1f, "Hitstop restores the previous time scale");
            await WaitUntil(() => Get<int>(attack, "comboStep") == 2);
            Check(!Get<bool>(attack, "attackRequested"), "Queued attack is consumed once and advances the combo");

            Call(attack, "StopAttack");
            Set(attack, "comboStep", 0);
            Call(attack, "StartAttack", Time.time, 0f);
            Set(attack, "activeEndTime", Time.time - 0.01f);
            Set(attack, "attackEndTime", Time.time + 0.05f);
            Call(attack, "OnAttack", default(InputAction.CallbackContext));
            Check(Get<bool>(attack, "attackRequested"), "Late recovery input is queued");
            attack.CancelForSlide();
            Check(!Get<bool>(attack, "attackRequested") && !attack.IsAttacking,
                "Slide cancellation clears queued attacks");
            attack.ResumeComboAfterSlide(Time.time);
            Check(Get<int>(attack, "comboStep") == 1 && !Get<bool>(attack, "comboTimerPaused"),
                "Slide retains the combo step and resumes its timer");
            Time.timeScale = 0f;
            Call(attack, "OnAttack", default(InputAction.CallbackContext));
            Check(!Get<bool>(attack, "attackRequested"), "Menu pause does not queue an attack");
            Time.timeScale = 1f;

            Call(dummy, "Respawn");
            float restYScale = pivot.localScale.y;
            dummy.TryTakeDamage(new AttackHit(1f, Vector2.zero, Vector2.down, 3));
            Check(pivot.localScale.y < restYScale, "Downward heavy hit compresses the anchored dummy");
            Set(dummy, "reactionElapsed", Get<float>(dummy, "reactionDuration"));
            Call(dummy, "ApplyReaction");
            Check(pivot.localPosition == pivotRest && pivot.localScale.y == restYScale,
                "Reaction returns exactly to the rest pose");

            Call(dummy, "Respawn");
            target.transform.position = new Vector3(0.9f, 1f, 0f);
            player.transform.position = new Vector3(0f, 1f, 0f);
            body.linearVelocity = Vector2.zero;
            body.simulated = true;
            movement.enabled = true;
            await WaitUntil(() => movement.IsOnGround);
            float startingY = player.transform.position.y;
            float highestY = startingY;
            int landings = 0;
            attack.SlamLanded += point => landings++;
            Set(attack, "comboStep", 2);
            Call(attack, "StartAttack", Time.time, 0f);
            await WaitUntil(() =>
            {
                highestY = Mathf.Max(highestY, player.transform.position.y);
                return landings > 0;
            });
            Check(highestY - startingY > 0.4f, "Stationary third attack hops without horizontal input");
            await WaitUntil(() => !attack.IsAttacking);
            Check(landings == 1, "Third attack emits exactly one landing event");
            Check(dummy.CurrentHealth == dummy.MaxHealth - 25f,
                "Dive and landing together apply heavy damage once");
            await WaitReal(0.4f);
            Check((cameraObject.transform.position - cameraRest).sqrMagnitude < 0.000001f,
                "Camera returns to its original position without drift");

            BluntHitBurst burst = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BluntHitBurst>("Assets/Effects/BluntHit/BluntHitEffect.prefab"));
            burst.Play(Vector2.right, 1f, 0, 10);
            Sprite firstSprite = burst.GetComponent<SpriteRenderer>().sprite;
            Time.timeScale = 0f;
            await WaitReal(0.055f);
            Check(burst.GetComponent<SpriteRenderer>().sprite != firstSprite,
                "Impact flash advances during a full time stop");
            Check(Get<float>(burst, "elapsed") == 0f, "Debris tail remains frozen with the world");
            Time.timeScale = 1f;

            PlayerHitCameraShake shake = player.GetComponent<PlayerHitCameraShake>();
            Call(shake, "Kick", Vector2.right, 0.05f);
            await NextFrame();
            shake.enabled = false;
            Check((cameraObject.transform.position - cameraRest).sqrMagnitude < 0.000001f,
                "Disabling camera feedback removes its remaining offset");

            Object.Destroy(burst.gameObject);
            Object.Destroy(player);
            Object.Destroy(target);
            await NextFrame();
            Check(Time.timeScale == 1f, "Cleanup leaves game time running");
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
            File.WriteAllText(Path.Combine(Application.dataPath, "../combat-feel-results.txt"), report);
            Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }
    }
}
