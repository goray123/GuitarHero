// 로비에서 Play 모드 진입 후 Unity CLI script run으로 실행하는 기본 흐름 검증.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class Boss1EncounterValidation
{
    private static readonly List<string> results = new List<string>();

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        results.Add("PASS: " + message);
        Debug.Log("PASS: " + message);
    }

    private static async Task NextFrame()
    {
        await Awaitable.NextFrameAsync();
    }

    private static async Task Until(Func<bool> condition, float timeout = 6f)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Runtime state timed out.");
            await NextFrame();
        }
    }

    private static async Task Hold(Keyboard keyboard, float seconds, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        float end = Time.time + seconds;
        do { await NextFrame(); } while (Time.time < end);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await NextFrame();
    }

    public static async Task<string> Run()
    {
        results.Clear();
        Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().name == "Lobby", "Start in lobby Play mode");
        var inputSettings = InputSystem.settings;
        var editorInput = inputSettings.editorInputBehaviorInPlayMode;
        var backgroundInput = inputSettings.backgroundBehavior;
        // CLI 검사 중 게임 뷰 포커스에 따라 가상 키보드가 꺼지는 것을 막는다.
        inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            await Hold(keyboard, 0.1f, Key.UpArrow);
            Check(SceneManager.GetActiveScene().name == "Lobby", "Up Arrow away from board stays in lobby");
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(6.25f, body.position.y);
            body.linearVelocity = Vector2.zero;
            await NextFrame();
            await NextFrame();
            await Hold(keyboard, 0.1f, Key.UpArrow);
            await Until(() => SceneManager.GetActiveScene().name == "Boss01_Arena");
            await NextFrame();
            Check(true, "Up Arrow at board loads Boss01_Arena");

            var encounter = Object.FindFirstObjectByType<Boss1Encounter>();
            var boss = Object.FindFirstObjectByType<Boss1Health>(FindObjectsInactive.Include);
            var left = GameObject.Find("ArenaLeftGate").GetComponent<BoxCollider2D>();
            var right = GameObject.Find("ArenaRightGate").GetComponent<BoxCollider2D>();
            Check(encounter.CurrentPhase == Boss1Encounter.Phase.Approach && !boss.gameObject.activeSelf && !left.enabled && !right.enabled,
                "Approach starts with hidden boss and open gates");
            player = Object.FindFirstObjectByType<PlayerMovement>();
            body = player.GetComponent<Rigidbody2D>();
            Check(Mathf.Approximately(body.position.x, -12f), "Player starts at the left spawn");
            var dummies = Object.FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None)
                .OrderBy(d => d.transform.position.x).ToArray();
            Check(dummies.Length == 2 && dummies.All(d => d.transform.position.x < -12f),
                "Two real training dummies stand left of the spawn");
            var rhythm = player.GetComponent<PlayerRhythmGauge>();
            foreach (var dummy in dummies)
            {
                body.position = new Vector2(dummy.transform.position.x - 1.25f, body.position.y);
                body.linearVelocity = Vector2.zero;
                await NextFrame();
                await NextFrame();
                float before = dummy.CurrentHealth;
                await Hold(keyboard, 0.04f, Key.RightArrow);
                await Hold(keyboard, 0.05f, Key.X);
                await Until(() => dummy.CurrentHealth < before);
                await Until(() => !player.GetComponent<PlayerAttack>().IsAttacking);
            }
            Check(rhythm.CurrentGauge > 0f, "Warm-up attacks damage both dummies and build rhythm");
            uint trainingLife = rhythm.LifeVersion;
            body.position = new Vector2(5.8f, body.position.y);
            body.linearVelocity = Vector2.zero;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            await Until(() => encounter.CurrentPhase == Boss1Encounter.Phase.Entrance);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            double entranceStarted = Time.realtimeSinceStartupAsDouble;
            Check(!player.enabled && !player.GetComponent<PlayerAttack>().enabled && !player.GetComponent<PlayerChordStrike>().enabled,
                "Cinematic suspends movement, attacks and skills before the drop");
            Check(!player.GetComponent<PlayerRhythmGaugeUI>().enabled && GameObject.Find("Player Guitar HUD") == null,
                "Cinematic hides player HUD without resetting player health");
            Check(!boss.TryTakeDamage(1f), "Boss cannot take damage during its entrance");
            await Until(() => boss.gameObject.activeSelf);
            Check(boss.gameObject.activeSelf && left.enabled && right.enabled && boss.transform.position.y > 5f &&
                !boss.enabled && !boss.GetComponent<Boss1ProximityAttack>().enabled,
                $"Crossing trigger closes gates and drops the boss without attacking (active={boss.gameObject.activeSelf}, gates={left.enabled}/{right.enabled}, y={boss.transform.position.y:F2}, damage={boss.enabled}, AI={boss.GetComponent<Boss1ProximityAttack>().enabled})");
            Check(rhythm.CurrentGauge == 0f && rhythm.LifeVersion != trainingLife,
                "Boss entrance clears training rhythm and invalidates old projectile rewards");
            var art = boss.GetComponent<Boss1SlamSpriteVisual>();
            await Until(() => art.BodyRenderer != null && art.BodyRenderer.enabled);
            string[] bodyNames = { "Torso", "Head", "Facing Sensor", "Rear Arm", "Front Arm", "Rear Fist", "Front Fist", "Left Leg", "Right Leg" };
            Check(boss.transform.Find("Prototype Shapes").GetComponentsInChildren<SpriteRenderer>()
                .Where(r => bodyNames.Contains(r.name)).All(r => !r.enabled),
                "Falling boss uses original sprite with all geometric body parts hidden");
            var camera = Object.FindFirstObjectByType<CityCameraFollow>();
            Check(camera.ViewCamera.orthographicSize < 7.5f, "Entrance zooms the camera toward the boss");
            var cinematic = encounter.transform.Find("Boss Entrance HUD");
            var title = cinematic.Find("Boss Title").GetComponent<CanvasGroup>();
            var name = title.transform.Find("Name").GetComponent<Text>();
            Check(name.text == "메가 메카트론 JT8-3" && name.font.HasCharacter('메') && name.font.HasCharacter('론'),
                "Boss title uses the requested name and a bundled Korean font");
            Check(cinematic.Find("Top Letterbox").GetComponent<RectTransform>().anchorMin.y < 0.93f &&
                cinematic.Find("Bottom Letterbox").GetComponent<RectTransform>().anchorMax.y > 0.07f,
                "Top and bottom cinematic bars cover 7.5 percent each");
            await Until(() => Mathf.Approximately(Time.timeScale, 0.6f));
            double slowStarted = Time.realtimeSinceStartupAsDouble;
            Check(boss.GetComponent<Rigidbody2D>().position.y > 0.1f && !player.enabled,
                "First entrance slows to 0.6 speed just before landing with controls still locked");
            await Until(() => !Mathf.Approximately(Time.timeScale, 0.6f));
            Check(Time.realtimeSinceStartupAsDouble - slowStarted <= 0.45,
                "Entrance slow motion lasts at most 0.35 seconds plus one frame");
            await Until(() => title.alpha > 0.9f);
            Check(Mathf.Abs(boss.GetComponent<Rigidbody2D>().position.y) < 0.1f && !boss.enabled,
                "Large title is revealed on landing while combat remains suspended");
            Check(Time.timeScale == 1f, "Landing hitstop restores normal speed after the entrance slow motion");
            ScreenCapture.CaptureScreenshot("Temp/BossEntrance_Title.png");
            await NextFrame();
            uint entranceLife = rhythm.LifeVersion;
            rhythm.RecordConfirmedDamage(RhythmHitKind.Basic, 1f);
            await Until(() => encounter.CurrentPhase == Boss1Encounter.Phase.Combat);
            Check(boss.enabled && boss.GetComponent<Boss1ProximityAttack>().enabled &&
                Mathf.Abs(boss.GetComponent<Rigidbody2D>().position.y) < 0.1f,
                "Boss lands on floor before combat starts");
            Check(rhythm.CurrentGauge == 0f && rhythm.LifeVersion != entranceLife,
                "Combat start clears rhythm earned during the entrance again");
            double firstEntranceDuration = Time.realtimeSinceStartupAsDouble - entranceStarted;
            Check(player.enabled && player.GetComponent<PlayerAttack>().enabled && player.GetComponent<PlayerChordStrike>().enabled &&
                player.GetComponent<PlayerRhythmGaugeUI>().enabled && GameObject.Find("Player Guitar HUD") != null &&
                !cinematic.gameObject.activeSelf && Mathf.Approximately(camera.ViewCamera.orthographicSize, 8f) && Time.timeScale == 1f,
                "Combat restores controls, normal framing and time scale without leaving cinematic UI");
            var bossHud = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(c => c.name == "Boss1 Health HUD");
            var healthFrame = bossHud.transform.Find("Boss Health/Health Gauge/Amp Frame");
            var healthFill = healthFrame.Find("Health Signal").GetComponent<Image>();
            var damageFill = healthFrame.Find("Damage Trail").GetComponent<Image>();
            Check(healthFill.sprite != null && healthFill.fillAmount < 0.15f,
                "Boss health bar starts empty after controls and combat are restored");
            Check(damageFill.transform.GetSiblingIndex() < healthFill.transform.GetSiblingIndex() &&
                damageFill.sprite == healthFill.sprite && damageFill.color.g > healthFill.color.g &&
                !damageFill.raycastTarget && !healthFill.raycastTarget,
                "Bright damage trail shares the signal shape and renders behind orange actual health");
            await Until(() => healthFill.fillAmount > 0.05f);
            float reveal = healthFill.fillAmount;
            Check(reveal < 0.95f && boss.TryTakeDamage(boss.MaxHealth * 0.1f) &&
                Mathf.Approximately(healthFill.fillAmount, reveal * 0.9f) && Mathf.Approximately(damageFill.fillAmount, reveal),
                "Damage during the fill immediately changes real health and leaves a separate trail");
            await Until(() => Mathf.Approximately(healthFill.fillAmount, 0.9f));
            var healthUI = bossHud.GetComponent<HealthBarUI>();
            await Until(() => healthUI.IsSettled);
            Check(Mathf.Approximately(boss.CurrentHealth, boss.MaxHealth * 0.9f) &&
                Mathf.Approximately(damageFill.fillAmount, 0.9f),
                "Finishing the entrance fill preserves damage taken during combat");
            healthUI.enabled = false;
            var advance = typeof(HealthBarUI).GetMethod("Advance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check(boss.TryTakeDamage(boss.MaxHealth * 0.15f) && Mathf.Approximately(healthFill.fillAmount, 0.75f) &&
                Mathf.Approximately(damageFill.fillAmount, 0.9f),
                "Accepted boss damage immediately drains orange while bright damage remains");
            advance.Invoke(healthUI, new object[] { 0.2f });
            Check(Mathf.Approximately(damageFill.fillAmount, 0.9f), "Boss damage trail holds for 0.25 seconds");
            advance.Invoke(healthUI, new object[] { 0.15f });
            float trailBeforeHit = damageFill.fillAmount;
            Check(trailBeforeHit > 0.75f && trailBeforeHit < 0.9f, "Boss damage trail eases down after the hold");
            boss.TryTakeDamage(boss.MaxHealth * 0.05f);
            Check(Mathf.Approximately(healthFill.fillAmount, 0.7f) && damageFill.fillAmount == trailBeforeHit,
                "Repeated boss hits restart the delay without jumping the trail upward");
            advance.Invoke(healthUI, new object[] { 0.2f });
            Time.timeScale = 0f;
            typeof(HealthBarUI).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(healthUI, null);
            Check(damageFill.fillAmount == trailBeforeHit, "Boss damage trail holds on repeated hits and freezes during pause");
            Time.timeScale = 1f;
            advance.Invoke(healthUI, new object[] { 0.45f });
            Check(Mathf.Approximately(damageFill.fillAmount, 0.7f) && healthUI.IsSettled,
                "Boss damage trail settles exactly after its 0.4 second drain");
            healthUI.enabled = true;
            var leftPanel = left.transform.Find("SteelShutterPanel");
            var rightPanel = right.transform.Find("SteelShutterPanel");
            Check(Mathf.Abs(leftPanel.position.y) < .02f && Mathf.Abs(rightPanel.position.y) < .02f,
                "Both visible steel shutters close down to the floor");

            var box = player.GetComponent<BoxCollider2D>();
            body.position = new Vector2(-3.3f, body.position.y);
            body.linearVelocity = Vector2.zero;
            await Hold(keyboard, 0.35f, Key.LeftArrow);
            Check(box.bounds.min.x >= left.bounds.max.x - 0.06f, "Left gate blocks walking escape");
            await Hold(keyboard, 0.55f, Key.LeftArrow, Key.C);
            Check(box.bounds.min.x >= left.bounds.max.x - 0.06f, "Closed steel shutter also blocks sliding escape");
            var dashCooldown = GameObject.Find("Player Guitar HUD").transform.Find("Guitar/Dash Cooldown").GetComponent<Image>();
            Check(dashCooldown.sprite != null && dashCooldown.fillAmount > 0f &&
                Mathf.Abs(dashCooldown.fillAmount - player.SlideCooldownFraction) < 0.05f &&
                Mathf.Abs(dashCooldown.color.a - 0.9f) < 0.01f,
                "Guitar HUD shows the slide cooldown with the arena opacity setting");
            body.position = new Vector2(23.3f, body.position.y);
            body.linearVelocity = Vector2.zero;
            await Hold(keyboard, 0.35f, Key.RightArrow);
            Check(box.bounds.max.x <= right.bounds.min.x + 0.06f, "Right gate blocks walking escape");
            boss.TryTakeDamage(boss.MaxHealth);
            await NextFrame();
            Check(encounter.CurrentPhase == Boss1Encounter.Phase.Cleared && !left.enabled && !right.enabled,
                "Boss defeat reopens both gates");
            await Until(() => leftPanel.position.y > 12.9f && rightPanel.position.y > 12.9f);
            Check(true, "Both steel shutters visibly retract after defeat");
            await Until(() => !leftPanel.gameObject.activeSelf && !rightPanel.gameObject.activeSelf);
            Check(!leftPanel.gameObject.activeSelf && !rightPanel.gameObject.activeSelf,
                "Fully open shutters hide inside the ceiling");
            Check(Object.FindFirstObjectByType<CityCameraFollow>().HorizontalBounds == new Vector2(-24f, 28f),
                "Boss defeat restores approach camera bounds");

            SceneManager.LoadScene("Boss01_Arena");
            await NextFrame();
            await NextFrame();
            encounter = Object.FindFirstObjectByType<Boss1Encounter>();
            player = Object.FindFirstObjectByType<PlayerMovement>();
            body = player.GetComponent<Rigidbody2D>();
            boss = Object.FindFirstObjectByType<Boss1Health>(FindObjectsInactive.Include);
            camera = Object.FindFirstObjectByType<CityCameraFollow>();
            var musicObject = new GameObject("Validation Music", typeof(AudioSource));
            var music = musicObject.GetComponent<AudioSource>();
            var clip = AudioClip.Create("Validation Music", 4410, 1, 44100, false);
            music.clip = clip;
            music.volume = 0.8f;
            music.loop = true;
            music.Play();
            body.position = new Vector2(6.1f, 0.75f);
            body.linearVelocity = Vector2.zero;
            await Until(() => boss.gameObject.activeSelf);
            Check(Mathf.Abs(music.volume - 0.2f) < 0.01f, "Entrance ducks existing looping music to 25 percent");
            encounter.enabled = false;
            await NextFrame();
            Check(player.enabled && player.GetComponent<PlayerAttack>().enabled && player.GetComponent<PlayerChordStrike>().enabled &&
                !boss.gameObject.activeSelf && Mathf.Approximately(camera.ViewCamera.orthographicSize, 8f) &&
                Mathf.Abs(music.volume - 0.8f) < 0.01f,
                "Interrupted entrance restores controls, music, camera and hidden boss");
            Check(!GameObject.Find("ArenaLeftGate").GetComponent<BoxCollider2D>().enabled &&
                !GameObject.Find("ArenaRightGate").GetComponent<BoxCollider2D>().enabled,
                "Interrupted entrance reopens the arena gates");
            body.position = new Vector2(6.1f, body.position.y);
            encounter.enabled = true;
            double replayStarted = Time.realtimeSinceStartupAsDouble;
            bool replaySlowed = false;
            await Until(() => {
                replaySlowed |= Mathf.Approximately(Time.timeScale, 0.6f);
                return encounter.CurrentPhase == Boss1Encounter.Phase.Combat;
            });
            Check(Time.realtimeSinceStartupAsDouble - replayStarted < firstEntranceDuration - 0.3,
                "Retry keeps the entrance impact with a shorter anticipation and title hold");
            Check(!replaySlowed, "Retry skips the first entrance slow motion");
            Check(Time.timeScale == 1f && Mathf.Abs(music.volume - 0.8f) < 0.01f && player.enabled,
                "Retry leaves music, time and controls restored");
            encounter.enabled = false;
            await NextFrame();
            var replayFill = GameObject.Find("Boss1 Health HUD").transform
                .Find("Boss Health/Health Gauge/Amp Frame/Health Signal").GetComponent<Image>();
            Check(Mathf.Approximately(replayFill.fillAmount, 1f), "Interrupting the health fill leaves no partially filled HUD");
            var hitStop = player.GetComponent<PlayerHitStop>();
            hitStop.SlowFor(0.35f, 0.6f);
            hitStop.StopFor(0.08f, true);
            Check(hitStop.IsStopped && Time.timeScale == 0f, "Hitstop can interrupt slow motion immediately");
            await Until(() => !hitStop.IsStopped);
            Check(Time.timeScale == 1f, "Overlapping slow motion and hitstop restore the original speed");
            hitStop.SlowFor(0.35f, 0.6f);
            hitStop.enabled = false;
            Check(Time.timeScale == 1f, "Disabling time feedback during slow motion restores the original speed");
            hitStop.enabled = true;
            Object.Destroy(musicObject);
            Object.Destroy(clip);
            return string.Join("\n", results);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            inputSettings.backgroundBehavior = backgroundInput;
        }
    }
}
