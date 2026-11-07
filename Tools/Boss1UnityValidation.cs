// Runs only in the isolated copy prepared by ValidateBoss1Unity.ps1.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Boss1UnityValidation
{
    private const string Pending = "Boss1UnityValidation.Pending";
    private static readonly List<string> Results = new List<string>();
    private static bool running;

    static Boss1UnityValidation()
    {
        EditorApplication.update += StartChecks;
    }

    private static void StartChecks()
    {
        if (running || !SessionState.GetBool(Pending, false) ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.EnterPlaymode();
            return;
        }
        running = true;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        Debug.Log("Starting Boss1 play mode validation");
        CheckPlayMode();
    }
    public static void Run()
    {
        Application.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Scenes/Boss1_Arena.unity", OpenSceneMode.Single);
        SessionState.SetBool(Pending, true);
    }
    private static void Check(bool condition, string message)
    {
        string text = (condition ? "PASS: " : "FAIL: ") + message;
        Results.Add(text); Debug.Log(text);
    }
    private static Task NextFrame()
    {
        var completion = new TaskCompletionSource<bool>();
        int frame = Time.frameCount;
        double deadline = EditorApplication.timeSinceStartup + 10;
        void Tick()
        {
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
            if (Time.frameCount != frame)
            {
                EditorApplication.update -= Tick;
                completion.SetResult(true);
            }
            else if (EditorApplication.timeSinceStartup >= deadline)
            {
                EditorApplication.update -= Tick;
                completion.SetException(new TimeoutException("The Unity player loop stopped advancing."));
            }
        }
        EditorApplication.update += Tick;
        EditorApplication.QueuePlayerLoopUpdate();
        return completion.Task;
    }
    private static async Task Until(Func<bool> condition, string description, float timeout = 15f)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out: " + description);
            await NextFrame();
        }
    }
    private static Task State(Boss1Controller boss, Boss1State state)
    {
        return Until(() => boss.Model.State == state, state.ToString());
    }
    private static Boss1Missile Guided(Boss1Controller boss)
    {
        return boss.Model.Missiles.FirstOrDefault(m=>m.Kind == Boss1MissileKind.Guided);
    }
    private static void Park(PlayerHealth player, float x, float footY)
    {
        var body = player.GetComponent<Rigidbody2D>();
        var box = player.GetComponent<BoxCollider2D>();
        body.position = new Vector2(x - box.offset.x, footY + box.size.y * 0.5f - box.offset.y);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    private static void ManualPlayer(PlayerHealth player)
    {
        player.GetComponent<PlayerMovement>().enabled = false;
        player.GetComponent<PlayerAttack>().enabled = false;
        player.GetComponent<PlayerChordStrike>().enabled = false;
        player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    private static LineRenderer[] RangeLines(Boss1Arena arena)
    {
        return arena.GetComponentsInChildren<LineRenderer>()
            .Where(line => line.enabled && line.gameObject.name == "Attack range").ToArray();
    }

    private static async Task PressKey(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await NextFrame();
        await NextFrame();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await NextFrame();
        await NextFrame();
    }

    private static async Task CheckRangeKeys(Boss1Arena arena)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        keyboard.MakeCurrent();
        try
        {
            Check(RangeLines(arena).Length == 0, "Attack ranges start hidden");
            await PressKey(keyboard, Key.F1);
            LineRenderer[] lines = RangeLines(arena);
            Check(lines.Length == 3 && lines.All(line => line.positionCount == 4),
                "F1 shows the three basic attack collider outlines");
            await PressKey(keyboard, Key.A);
            await Until(() => RangeLines(arena).Any(line => line.positionCount > 4), "chord note contact circles", 1f);
            Check(true, "F1 also shows contact circles for live chord notes");
            await Until(() => RangeLines(arena).All(line => line.positionCount == 4), "chord notes end", 1f);

            float startingHealth = arena.Boss.CurrentHealth;
            await PressKey(keyboard, Key.F2);
            await State(arena.Boss, Boss1State.SweepWindup);
            lines = RangeLines(arena);
            float h = arena.Boss.PlayerHeight;
            Check(lines.Length == 4 && lines.Any(line => line.positionCount == 4 &&
                Mathf.Abs(Vector3.Distance(line.GetPosition(0), line.GetPosition(1)) - 1.5f * h) < 0.01f &&
                Mathf.Abs(Vector3.Distance(line.GetPosition(1), line.GetPosition(2)) - 1.2f * h) < 0.01f),
                "F2 adds the boss sweep outline with its configured hitbox dimensions");
            Check(arena.Boss.CurrentHealth == startingHealth && !arena.Boss.Model.PhasePending,
                "F2 leaves the phase and boss health alone");
            await PressKey(keyboard, Key.F1);
            Check(RangeLines(arena).Length == 1, "Player and boss range toggles operate independently");
            await PressKey(keyboard, Key.F2);
            Check(RangeLines(arena).Length == 0, "F2 hides the boss range again");
            await PressKey(keyboard, Key.F7);
            Check(arena.Boss.CurrentHealth == arena.Boss.MaxHealth * 0.5f &&
                (arena.Boss.Model.PhasePending || arena.Boss.Model.Phase == 2), "F7 queues phase two at half health");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
    }

    private static GameObject MissileRoot(Boss1Missile missile)
    {
        return GameObject.Find("Boss1 Missile " + missile.Id);
    }

    private static async Task CheckAirborneBarrage(Boss1Arena arena)
    {
        arena.RestartEncounter();
        await NextFrame();
        var boss = arena.Boss;
        var player = arena.Player;
        ManualPlayer(player);
        float h = boss.PlayerHeight;
        Park(player, 4.5f, 3.1f * h);
        Vector2 playerPosition = player.GetComponent<Rigidbody2D>().position;
        Time.timeScale = 4f;
        await State(boss, Boss1State.BarrageWindup);
        Time.timeScale = 1f;
        await Until(() => boss.Model.Missiles.Any(m => m.Kind == Boss1MissileKind.Barrage), "first barrage shot");
        Boss1Missile missile = boss.Model.Missiles.First(m => m.Kind == Boss1MissileKind.Barrage);
        GameObject root = MissileRoot(missile);
        Vector2 aim = new Vector2(missile.TargetX, missile.TargetY) * h;
        var warning = root.transform.Find("Warning circle").GetComponent<LineRenderer>();
        var explosion = root.transform.Find("Explosion").GetComponent<SpriteRenderer>();
        Check(aim.y > 3f * h && Vector2.Distance(aim, playerPosition) <= 0.2501f &&
            Vector2.Distance(root.transform.position, aim) < 0.001f && warning.enabled,
            "Barrage warning appears at the airborne target inside the player's diameter-0.5 disk");

        Park(player, playerPosition.x + 3f * h, 4.1f * h);
        await Until(() => boss.Model.Time >= missile.ImpactsAt - 0.15f, "barrage flight");
        Check(Vector2.Distance(root.transform.position, aim) < 0.001f && warning.enabled && !explosion.enabled,
            "A launched barrage missile keeps its original target while the player moves");
        var shape = player.GetComponent<BoxCollider2D>();
        Park(player, aim.x, aim.y - shape.bounds.extents.y);
        await Until(() => missile.IsExploding(boss.Model.Time, boss.Settings.ExplosionDuration), "airborne barrage impact");
        Check(explosion.enabled && Vector2.Distance(explosion.transform.position, aim) < 0.001f &&
            player.CurrentHealth == 94f, "The barrage missile explodes and applies damage at the same airborne point");
    }

    private static async void CheckPlayMode()
    {
        int exitCode = 0;
        try
        {
            Time.timeScale = 1f;
            await NextFrame();
            Boss1Arena arena = Object.FindFirstObjectByType<Boss1Arena>();
            Check(arena != null && arena.Player != null && arena.Boss != null, "Arena spawns the wired player and boss prefabs");
            if (arena == null || arena.Player == null || arena.Boss == null) throw new Exception("Arena startup failed");
            Boss1SpriteSet set = arena.Boss.Sprites;
            Check(set != null && Enum.GetValues(typeof(Boss1Pose)).Cast<Boss1Pose>().All(p=>set.Frames(p).Length==8 && set.Frames(p).All(s=>s!=null)),
                "All 12 animations resolve to eight imported sprites each");
            Check(AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/Boss1/Sprites/Boss1_Actions_v1.png").OfType<Sprite>().Count()==64,
                "Unity imports all 64 body sprites from the atlas metadata");
            Check(AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/Boss1/Sprites/Boss1_Support_v1.png").OfType<Sprite>().Count()==32,
                "Unity imports all 32 support sprites from the atlas metadata");
            Check(arena.Boss.Model.Phase==1 && arena.Boss.CurrentHealth==1500f, "Encounter starts at full health in phase one");
            var bossVisual = arena.Boss.GetComponentInChildren<SpriteRenderer>();
            var bossShape = arena.Boss.GetComponent<BoxCollider2D>();
            float initialH = arena.Boss.PlayerHeight;
            Check(Mathf.Approximately(bossVisual.transform.lossyScale.x, 2.7f) &&
                Mathf.Approximately(bossVisual.transform.lossyScale.y, 2.7f) &&
                Mathf.Abs(bossShape.bounds.size.x - 2.2f * initialH) < 0.01f &&
                Mathf.Abs(bossShape.bounds.size.y - 3.4f * initialH) < 0.01f,
                "Boss sprite and body collider are twice their original size in both axes");
            Check(Physics2D.GetIgnoreCollision(arena.Boss.GetComponent<Collider2D>(), arena.Player.GetComponent<Collider2D>()),
                "Boss and player bodies do not block each other");
            Check(arena.Boss.TryTakeDamage(new AttackHit(10f,Vector2.zero,Vector2.right,1)) && arena.Boss.CurrentHealth==1490f,
                "Existing AttackHit damage reaches the new boss Damageable");
            await CheckRangeKeys(arena);
            arena.RestartEncounter(); await NextFrame();

            var player = arena.Player;
            var rhythm = player.GetComponent<PlayerRhythmGauge>();
            for(int i=0;i<10;i++) rhythm.RecordConfirmedDamage(RhythmHitKind.Basic,8f);
            float gauge = rhythm.CurrentGauge;
            Check(player.TryTakeDamage(6f) && player.CurrentHealth==94f && Mathf.Approximately(rhythm.CurrentGauge,gauge-100f),
                "Accepted boss damage removes health and one rhythm penalty");
            float after = rhythm.CurrentGauge;
            Check(!player.TryTakeDamage(6f) && player.CurrentHealth==94f && rhythm.CurrentGauge==after,
                "Invulnerability rejects repeated damage without another rhythm penalty");

            arena.RestartEncounter(); await NextFrame();
            player=arena.Player; var boss=arena.Boss;
            ManualPlayer(player);
            float h=boss.PlayerHeight;
            var bossBody = boss.GetComponent<Rigidbody2D>();
            bossShape = boss.GetComponent<BoxCollider2D>();
            Park(player, bossBody.position.x + bossShape.bounds.extents.x + 0.5f * h, 0.02f);
            await State(boss,Boss1State.SweepWindup);
            float sweepStart = bossBody.position.x;
            await State(boss,Boss1State.PunchWindup);
            float punchStart = bossBody.position.x;
            Check(Mathf.Abs(punchStart - sweepStart - boss.Settings.SweepAdvanceH * h) < 0.08f * h,
                "The low sweep advances the boss during the strike");
            Check(player.CurrentHealth==94f,"Standing inside the low sweep takes exactly one six-percent hit");
            player.ResetHealth();
            var shape=player.GetComponent<BoxCollider2D>();
            Vector2 originalSize=shape.size, originalOffset=shape.offset;
            shape.size=new Vector2(originalSize.x,player.GetComponent<PlayerMovement>().SlidingColliderHeight);
            shape.offset=originalOffset-Vector2.up*(originalSize.y-shape.size.y)*0.5f;
            Park(player, bossBody.position.x + bossShape.bounds.extents.x + 0.5f * h, 0.02f);
            await State(boss,Boss1State.SlamWindup);
            float slamStart = bossBody.position.x;
            Check(Mathf.Abs(slamStart - punchStart - boss.Settings.PunchAdvanceH * h) < 0.08f * h,
                "The middle punch advances the boss during the strike");
            Check(player.CurrentHealth==100f,"Slide-height player collider passes below the moving middle punch");
            shape.size=originalSize; shape.offset=originalOffset; Physics2D.SyncTransforms();
            await Until(()=>boss.Model.StateElapsed>0.31f,"slam direction lock");
            int facing=boss.Model.Facing;
            Park(player,boss.transform.position.x-facing*1.2f*h,0.02f);
            await State(boss,Boss1State.ComboRecovery);
            Check(Mathf.Abs((bossBody.position.x - slamStart) * facing - boss.Settings.SlamAdvanceH * h) < 0.08f * h,
                "The slam advances in the locked direction even after the player crosses behind");
            Check(boss.Model.Facing==facing && player.CurrentHealth==100f,"Moving behind after slam direction lock evades the third hit");

            arena.RestartEncounter(); await NextFrame();
            player = arena.Player; boss = arena.Boss; ManualPlayer(player);
            bossBody = boss.GetComponent<Rigidbody2D>();
            bossShape = boss.GetComponent<BoxCollider2D>();
            float wallX = arena.transform.Find("Right boundary").GetComponent<BoxCollider2D>().bounds.min.x;
            float wallStart = wallX - bossShape.bounds.extents.x - 0.12f;
            bossBody.position = new Vector2(wallStart, bossBody.position.y);
            Park(player, wallX - 0.4f, 0.02f);
            await State(boss, Boss1State.PunchWindup);
            Check(bossBody.position.x > wallStart + 0.04f && bossShape.bounds.max.x <= wallX + 0.005f,
                "Combo movement stops the enlarged body at the arena wall");

            await CheckAirborneBarrage(arena);

            arena.RestartEncounter(); await NextFrame();
            player=arena.Player; boss=arena.Boss; ManualPlayer(player); h=boss.PlayerHeight;
            Park(player, boss.transform.position.x + 3.2f * h, 1.35f * h);
            boss.TryTakeDamage(boss.MaxHealth * 0.5f);
            await Until(() => boss.Model.Phase == 2, "phase two");
            Check(boss.Model.BarrageCount == 0, "Phase entry does not launch an extra barrage");
            await Until(() => Guided(boss) != null, "first independent guided missile");
            Boss1Missile missile = Guided(boss);
            GameObject missileRoot = MissileRoot(missile);
            var line = missileRoot.transform.Find("Guided aim line").GetComponent<LineRenderer>();
            Vector2 origin = line.transform.TransformPoint(line.GetPosition(0));
            Vector2 aim = line.transform.TransformPoint(line.GetPosition(1));
            var playerShape = player.GetComponent<BoxCollider2D>();
            Check(line.enabled && Vector2.Distance(aim, playerShape.bounds.center) < 0.02f &&
                Mathf.Abs(missile.LaunchesAt - missile.CreatedAt - 2f) < 0.001f,
                "Guided warning is a two-second line aimed at the actual airborne player");
            Check(missileRoot.GetComponentsInChildren<Collider2D>().Length == 0,
                "Guided warning and projectile graphics do not create contact-damage colliders");

            Park(player, aim.x, aim.y + h);
            boss.ApplyBind(0.3f);
            await Until(() => boss.Model.Time >= missile.LaunchesAt - 0.12f, "fixed guided warning");
            Check(boss.Model.Missiles.Contains(missile) && line.enabled &&
                Vector2.Distance(origin, line.transform.TransformPoint(line.GetPosition(0))) < 0.001f &&
                Vector2.Distance(aim, line.transform.TransformPoint(line.GetPosition(1))) < 0.001f,
                "The displayed launch line stays fixed through target movement and bind");
            Park(player, aim.x, aim.y - playerShape.bounds.extents.y);
            player.ResetHealth();
            await Until(() => !float.IsPositiveInfinity(missile.ImpactsAt), "guided player contact");
            Vector2 impact = missileRoot.transform.position;
            Check(!line.enabled && Vector2.Distance(impact, playerShape.ClosestPoint(impact)) <= boss.Settings.GuidedContactRadiusH * h + 0.03f,
                "A flying guided missile detonates at contact with the player");
            await Until(() => !boss.Model.Missiles.Contains(missile), "guided explosion end");
            Check(player.CurrentHealth == 94f, "A guided explosion applies six-percent damage once");

            await Until(() => Guided(boss) != null, "next guided missile");
            missile = Guided(boss);
            missileRoot = MissileRoot(missile);
            line = missileRoot.transform.Find("Guided aim line").GetComponent<LineRenderer>();
            origin = line.transform.TransformPoint(line.GetPosition(0));
            aim = line.transform.TransformPoint(line.GetPosition(1));
            var wall = new GameObject("Guided validation wall");
            wall.transform.position = origin + (aim - origin).normalized * h * 1.15f;
            var wallShape = wall.AddComponent<BoxCollider2D>();
            wallShape.size = Vector2.one * h * 0.2f;
            Park(player, aim.x, 5f * h);
            player.ResetHealth();
            await Until(() => !float.IsPositiveInfinity(missile.ImpactsAt), "guided wall contact");
            impact = missileRoot.transform.position;
            Check(Vector2.Distance(impact, wallShape.ClosestPoint(impact)) <= boss.Settings.GuidedContactRadiusH * h + 0.03f &&
                player.CurrentHealth == 100f, "A missed guided shot detonates on a wall without damaging a distant player");
            Object.Destroy(wall);

            boss.TryTakeDamage(boss.MaxHealth * 2f);
            Check(boss.IsDead && boss.Model.Missiles.Count == 0 && MissileRoot(missile) == null,
                "Boss death immediately clears gameplay missiles and their graphics");
            await Until(()=>boss.Model.StateElapsed>=1.25f,"death pose");
            var visual=boss.GetComponentInChildren<SpriteRenderer>();
            Check(visual.sprite!=null && visual.sprite.name=="Boss1_Death_07","Death animation settles on the last generated sprite");
            Check(boss.transform.position.y>-0.1f,"Dead boss retains ground support rather than falling through the floor");

            arena.RestartEncounter(); await NextFrame();
            player=arena.Player; boss=arena.Boss;
            player.TryTakeDamage(player.MaxHealth);
            Check(player.IsDead && !player.GetComponent<PlayerMovement>().enabled &&
                !player.GetComponent<PlayerAttack>().enabled && !player.GetComponent<PlayerChordStrike>().enabled,
                "Player death disables movement, melee and chord-strike input");
            await Until(()=>boss.Model.State==Boss1State.Dormant,"encounter ends after player death");
            Check(boss.Model.Missiles.Count==0,"Player death also removes outstanding boss warnings");
            arena.RestartEncounter(); await NextFrame();
            Check(!arena.Player.IsDead && arena.Player.CurrentHealth==100f && arena.Boss.CurrentHealth==1500f &&
                arena.Boss.Model.Phase==1 && arena.Boss.Model.BarrageCount==0,"Retry creates a clean phase-one encounter");
        }
        catch(Exception exception)
        {
            Results.Add("FAIL: "+exception); Debug.LogException(exception); exitCode=1;
        }
        finally
        {
            if(Results.Any(r=>r.StartsWith("FAIL:"))) exitCode=1;
            Time.timeScale=1f;
            SessionState.SetBool(Pending, false);
            string report=string.Join("\n",Results);
            File.WriteAllText(Path.Combine(Application.dataPath,"../boss1-unity-results.txt"),report);
            Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }
    }
}

