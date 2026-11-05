// Runs only in the isolated copy prepared by ValidateBoss1Unity.ps1.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Boss1UnityValidation
{
    private const string Pending = "Boss1UnityValidation.Pending";
    private static readonly List<string> Results = new List<string>();
    static Boss1UnityValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                CheckPlayMode();
            }
        };
    }
    public static void Run()
    {
        Application.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Scenes/Boss1_Arena.unity", OpenSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
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
            Check(Physics2D.GetIgnoreCollision(arena.Boss.GetComponent<Collider2D>(), arena.Player.GetComponent<Collider2D>()),
                "Boss and player bodies do not block each other");
            Check(arena.Boss.TryTakeDamage(new AttackHit(10f,Vector2.zero,Vector2.right,1)) && arena.Boss.CurrentHealth==1490f,
                "Existing AttackHit damage reaches the new boss Damageable");

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
            Park(player,boss.transform.position.x+1.2f*h,0.02f);
            await State(boss,Boss1State.SweepWindup);
            await State(boss,Boss1State.PunchWindup);
            Check(player.CurrentHealth==94f,"Standing inside the low sweep takes exactly one six-percent hit");
            player.ResetHealth();
            var shape=player.GetComponent<BoxCollider2D>();
            Vector2 originalSize=shape.size, originalOffset=shape.offset;
            shape.size=new Vector2(originalSize.x,player.GetComponent<PlayerMovement>().SlidingColliderHeight);
            shape.offset=originalOffset-Vector2.up*(originalSize.y-shape.size.y)*0.5f;
            Physics2D.SyncTransforms();
            await State(boss,Boss1State.SlamWindup);
            Check(player.CurrentHealth==100f,"Slide-height player collider passes below the middle punch");
            shape.size=originalSize; shape.offset=originalOffset; Physics2D.SyncTransforms();
            await Until(()=>boss.Model.StateElapsed>0.31f,"slam direction lock");
            int facing=boss.Model.Facing;
            Park(player,boss.transform.position.x-facing*1.2f*h,0.02f);
            await State(boss,Boss1State.ComboRecovery);
            Check(boss.Model.Facing==facing && player.CurrentHealth==100f,"Moving behind after slam direction lock evades the third hit");

            arena.RestartEncounter(); await NextFrame();
            player=arena.Player; boss=arena.Boss; ManualPlayer(player); h=boss.PlayerHeight;
            Park(player,boss.transform.position.x+1.2f*h,3f*h);
            boss.TryTakeDamage(boss.MaxHealth*0.5f);
            await Until(()=>boss.Model.Phase==2,"phase two");
            Check(boss.Model.BarrageCount==0,"Phase entry does not launch an extra barrage");
            await Until(()=>Guided(boss)!=null,"first independent guided missile");
            Boss1Missile missile=Guided(boss);
            await Until(()=>missile.Locked,"guided lock");
            GameObject missileRoot=GameObject.Find("Boss1 Missile "+missile.Id);
            Check(missileRoot!=null && Mathf.Abs(missileRoot.transform.position.y)<0.05f,
                "Airborne player targeting is projected onto the ground");
            Check(missileRoot!=null && missileRoot.GetComponentsInChildren<Collider2D>().Length==0,
                "Flying missiles and warning graphics have no contact-damage collider");
            float lockedX=missile.TargetX*h;
            var missileDamageLog=new List<string>();
            player.Changed += () => missileDamageLog.Add(player.CurrentHealth+" hp at boss time "+boss.Model.Time+" / "+boss.Model.State);
            // Stay airborne through the preceding slam. Move into the blast
            // only inside its reserved 0.35 s separation margin so this check
            // measures missile damage independently of a valid melee hit.
            await Until(()=>boss.Model.Time>=missile.ImpactsAt-0.25f,"isolated missile damage window");
            Park(player,lockedX,0.02f); player.ResetHealth();
            await Until(()=>!boss.Model.Missiles.Contains(missile),"guided explosion end");
            Check(player.CurrentHealth==94f,"A missile explosion applies six-percent damage once across its active lifetime (actual "+player.CurrentHealth+"; "+string.Join("; ",missileDamageLog)+")");
            await Until(()=>Guided(boss)!=null,"next guided missile");
            missile=Guided(boss);
            if(!missile.Locked)
            {
                boss.ApplyBind(0.3f);
                Check(Guided(boss)==null,"Binding removes an unfixed guided marker in the live scene");
            }
            boss.TryTakeDamage(boss.MaxHealth*2f);
            Check(boss.IsDead && boss.Model.Missiles.Count==0,"Boss death immediately clears gameplay missiles");
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
            string report=string.Join("\n",Results);
            File.WriteAllText(Path.Combine(Application.dataPath,"../boss1-unity-results.txt"),report);
            Debug.Log(report);
            EditorApplication.Exit(exitCode);
        }
    }
}

