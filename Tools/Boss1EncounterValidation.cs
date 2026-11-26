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
            Check(!boss.gameObject.activeSelf && left.enabled && right.enabled && !player.enabled &&
                !player.GetComponent<PlayerAttack>().enabled && !player.GetComponent<PlayerChordStrike>().enabled,
                "Crossing trigger locks controls and closes gates before the drop");
            Check(rhythm.CurrentGauge == 0f && rhythm.LifeVersion != trainingLife,
                "Boss entrance clears training rhythm and invalidates old projectile rewards");
            var art = boss.GetComponent<Boss1SlamSpriteVisual>();
            await Until(() => boss.gameObject.activeSelf);
            Check(boss.transform.position.y > 5f && !boss.enabled, "Boss enters from above before combat");
            await Until(() => art.BodyRenderer != null && art.BodyRenderer.enabled);
            string[] bodyNames = { "Torso", "Head", "Facing Sensor", "Rear Arm", "Front Arm", "Rear Fist", "Front Fist", "Left Leg", "Right Leg" };
            Check(boss.transform.Find("Prototype Shapes").GetComponentsInChildren<SpriteRenderer>()
                .Where(r => bodyNames.Contains(r.name)).All(r => !r.enabled),
                "Falling boss uses original sprite with all geometric body parts hidden");
            uint entranceLife = rhythm.LifeVersion;
            rhythm.RecordConfirmedDamage(RhythmHitKind.Basic, 1f);
            await Until(() => encounter.CurrentPhase == Boss1Encounter.Phase.Combat);
            Check(player.enabled && boss.enabled && boss.GetComponent<Boss1ProximityAttack>().enabled &&
                Mathf.Abs(boss.GetComponent<Rigidbody2D>().position.y) < 0.1f,
                "Boss lands on floor before combat starts");
            Check(rhythm.CurrentGauge == 0f && rhythm.LifeVersion != entranceLife,
                "Combat start clears rhythm earned during the entrance again");
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
