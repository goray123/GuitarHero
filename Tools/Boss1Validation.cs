using System;
using System.Collections.Generic;
using System.IO;

public static class Boss1Validation
{
    private static readonly List<string> results = new List<string>();
    private static int failures;
    private static Boss1Input Input(float playerX)
    {
        return new Boss1Input { HasTarget = true, BossX = 0f, PlayerX = playerX, PlayerHeight = 1.31f,
            ArenaLeft = -10f, ArenaRight = 10f, ForwardClearanceH = 8f };
    }
    private static void Check(bool condition, string message)
    {
        string text = (condition ? "PASS: " : "FAIL: ") + message;
        results.Add(text); Console.WriteLine(text);
        if (!condition) failures++;
    }
    private static bool Near(float a, float b, float tolerance) { return Math.Abs(a - b) <= tolerance; }
    private static bool Wait(Boss1CombatModel m, Boss1State state, Boss1Input input, float limit)
    {
        float end = m.Time + limit;
        while (m.State != state && m.Time < end) m.Tick(0.01f, input);
        return m.State == state;
    }
    private static Boss1Missile Guided(Boss1CombatModel m)
    {
        foreach (var missile in m.Missiles) if (missile.Kind == Boss1MissileKind.Guided) return missile;
        return null;
    }
    private static Boss1CombatModel PhaseTwo(out Boss1Input input, Func<Boss1Missile, float, bool> contact = null)
    {
        input = Input(1.2f);
        var m = new Boss1CombatModel(new Boss1Tuning(), contact);
        m.Begin(); m.TakeDamage(m.MaxHealth * 0.5f);
        for (int i = 0; i < 1000 && m.Phase != 2; i++) m.Tick(0.01f, input);
        return m;
    }

    private static void OpeningAndFacing()
    {
        var input = Input(8f);
        var m = new Boss1CombatModel(new Boss1Tuning());
        m.Begin();
        Check(Wait(m, Boss1State.SweepWindup, input, 2f), "First attack is the three-hit combo even at range");
        input.PlayerX = -1.2f;
        Wait(m, Boss1State.PunchWindup, input, 2f);
        Check(m.Facing == 1, "Sweep and punch keep the original facing after player crosses behind");
        Wait(m, Boss1State.SlamWindup, input, 2f);
        m.Tick(0.24f, input);
        Check(m.Facing == -1, "Slam rechecks facing during its first 0.25 seconds");
        m.Tick(0.03f, input); input.PlayerX = 1.2f; m.Tick(0.3f, input);
        Check(m.Facing == -1, "Slam facing stays locked during remaining warning");
        Check(Wait(m, Boss1State.ComboRecovery, input, 2f), "A missed combo still reaches the third hit and recovery");
        int sequence = m.AttackSequence;
        Check(sequence == 3, "Three distinct active windows have three damage identities");
        Check(!m.IsDamageActive, "Recovery carries no melee damage");
    }

    private static void PhasesAndDamage()
    {
        var input = Input(1.2f);
        var m = new Boss1CombatModel(new Boss1Tuning());
        m.Begin(); Wait(m, Boss1State.SweepWindup, input, 2f);
        m.TakeDamage(m.MaxHealth * 0.49f);
        Check(!m.PhasePending && m.Phase == 1, "Health above 50 percent stays in phase one");
        m.TakeDamage(m.MaxHealth * 0.01f);
        Check(m.PhasePending && m.Phase == 1 && m.State == Boss1State.SweepWindup,
            "Exact 50 percent queues a transition without interrupting an attack");
        float before = m.Health;
        Check(!m.TakeDamage(float.NaN) && !m.TakeDamage(float.PositiveInfinity) && !m.TakeDamage(-1f) && m.Health == before,
            "Invalid and negative damage cannot corrupt health");
        for (int i = 0; i < 1000 && m.Phase != 2; i++) m.Tick(0.01f, input);
        float transitioned = m.Time;
        Check(m.Phase == 2 && !m.PhasePending, "Transition completes after current combo and recovery");
        Check(m.BarrageCount == 0, "Entering phase two does not force missile barrage");
        Check(m.BarrageRemaining < 20f - transitioned + 0.1f, "Phase transition does not reset barrage timer");
        while (Guided(m) == null && m.Time < transitioned + 15f) m.Tick(0.01f, input);
        Boss1Missile missile = Guided(m);
        Check(missile != null && missile.CreatedAt >= transitioned + 1.49f, "First guided missile waits at least 1.5 seconds after transition");
        Check(missile != null && Near(missile.LaunchesAt - missile.CreatedAt, 2f, 0.002f) &&
            float.IsPositiveInfinity(missile.ImpactsAt), "Guided missile warns for two seconds and has no preset impact");
        int barrageCount = m.BarrageCount;
        Check(m.TakeDamage(m.MaxHealth * 10f) && m.IsDead && m.Health == 0f && m.Missiles.Count == 0,
            "Lethal damage immediately clears all missiles and transitions");
        m.Tick(60f, input);
        Check(m.Phase <= 2 && m.BarrageCount == barrageCount && !m.IsDamageActive,
            "Death cannot spawn a third phase, barrage or late melee hit");
        Check(!m.TakeDamage(1f), "A corpse rejects later damage");
    }

    private static void GuidedTimingAndBind()
    {
        int collisionChecks = 0;
        Boss1Input input;
        var m = PhaseTwo(out input, (missile, elapsed) =>
        {
            collisionChecks++;
            return elapsed >= 0.3f;
        });
        while (Guided(m) == null && m.Time < 20f) m.Tick(0.01f, input);
        Boss1Missile guided = Guided(m);
        if (guided == null) { Check(false, "Guided missile eventually starts"); return; }
        float launch = guided.LaunchesAt;
        float remaining = m.BarrageRemaining, stateElapsed = m.StateElapsed;
        m.Bind(3f);
        m.Tick(1.95f, input);
        Check(collisionChecks == 0 && m.Missiles.Contains(guided) && guided.LaunchesAt == launch,
            "A locked warning survives bind and cannot collide before launch");
        Check(Near(m.BarrageRemaining, remaining, 0.001f) && Near(m.StateElapsed, stateElapsed, 0.001f),
            "Bind pauses body progress and the barrage clock");
        m.Tick(0.4f, input);
        Check(collisionChecks > 0 && Near(guided.ImpactsAt, launch + 0.3f, 0.02f) &&
            guided.IsExploding(m.Time, 0.2f), "A guided missile can launch and explode on contact during bind");
        m.Tick(0.25f, input);
        Check(!m.Missiles.Contains(guided) && !m.IsDamageActive,
            "The explosion expires while body hitboxes remain off during bind");
        m.Tick(0.65f, input);
        Check(!m.IsDamageActive, "Bind release includes a warning before any body hit resumes");

        m = PhaseTwo(out input);
        while (Guided(m) == null && m.Time < 20f) m.Tick(0.01f, input);
        guided = Guided(m);
        m.Tick(4f, input);
        Check(guided != null && float.IsPositiveInfinity(guided.ImpactsAt) && !m.Missiles.Contains(guided),
            "A guided missile that makes no contact expires without an explosion");
    }

    private static void PeriodicBarrage()
    {
        var m = new Boss1CombatModel(new Boss1Tuning());
        var input = Input(1.2f);
        var ids = new HashSet<int>();
        int previousCount = 0, completed = 0;
        float previousRecoveryEnd = -100f, firstStart = -1f, lastShot = -1f, lastBlastEnd = -1f;
        bool timing = true, shotCadence = true, targetRange = true, fullVolley = true, recovery = true;
        Boss1State previousState = Boss1State.Dormant;
        m.Begin();
        while (m.Time < 105f)
        {
            if (m.IsBarrage)
                input.PlayerX = m.BarrageCount % 2 == 0 ? input.ArenaRight - 0.01f : 1.2f + (float)Math.Sin(m.Time);
            else
                input.PlayerX = 1.2f;
            input.PlayerY = 2.5f + (float)Math.Sin(m.Time * 0.5f);
            m.Tick(0.013f, input);
            if (m.BarrageCount != previousCount)
            {
                if (firstStart < 0f) firstStart = m.Time;
                if (previousCount > 0 && m.Time < previousRecoveryEnd + 19.95f) timing = false;
                ids.Clear(); previousCount = m.BarrageCount; lastShot = -1f;
            }
            foreach (Boss1Missile shot in m.Missiles)
            {
                if (shot.Kind != Boss1MissileKind.Barrage || !ids.Add(shot.Id)) continue;
                if (lastShot >= 0f && !Near(shot.LaunchesAt - lastShot, 0.2f, 0.003f))
                    shotCadence = false;
                float dx = (shot.TargetX - input.PlayerX) * input.PlayerHeight;
                float dy = (shot.TargetY - input.PlayerY) * input.PlayerHeight;
                if (dx * dx + dy * dy > 0.2501f * 0.2501f)
                    targetRange = false;
                lastShot = shot.LaunchesAt;
                lastBlastEnd = shot.ImpactsAt + 0.2f;
            }
            if (previousState == Boss1State.BarrageFiring && m.State == Boss1State.BarrageRecovery)
                recovery &= m.Time >= lastBlastEnd;
            if (previousState == Boss1State.BarrageRecovery && m.State == Boss1State.Idle)
            {
                completed++; previousRecoveryEnd = m.Time;
                if (ids.Count != 30) fullVolley = false;
            }
            previousState = m.State;
        }
        Check(firstStart >= 19.99f, "Barrage never starts before the first 20-second deadline");
        Check(completed >= 3 && m.Phase == 1, "Barrage repeats multiple times entirely within phase one");
        Check(timing, "Repeated barrage waits 20 seconds from prior recovery end");
        Check(fullVolley, "Every completed barrage fires exactly 30 missiles");
        Check(shotCadence, "Barrage launches one missile every 0.2 seconds");
        Check(targetRange, "Every barrage target is within a diameter-0.5 disk around the player's launch-time X and Y");
        Check(recovery, "Barrage recovery starts after the final explosion expires");
        Check(m.BarrageCount <= completed + 1, "Delayed barrage does not accumulate multiple queued uses");
    }

    private static void ImpactSafety(bool withBindings, float step)
    {
        Boss1Input input;
        var m = PhaseTwo(out input, (missile, elapsed) => elapsed >= (missile.Id % 5) * 0.3f);
        var impactTimes = new Dictionary<int, float>();
        var activeTimes = new List<float>();
        int maxGuided = 0;
        float previousGuideStart = -100f;
        bool cadence = true;
        int guidedId = 0;
        for (int tick = 0; tick < 14000; tick++)
        {
            if (withBindings && tick % 917 == 450) m.Bind(0.58f);
            if (withBindings && m.IsDamageActive && tick % 17 == 0 && Guided(m) != null)
                m.Bind(0.26f);
            m.Tick(step, input);
            if (m.IsDamageActive) activeTimes.Add(m.Time);
            int activeGuided = 0;
            foreach (Boss1Missile missile in m.Missiles)
            {
                if (missile.Kind == Boss1MissileKind.Guided)
                {
                    activeGuided++;
                    if (missile.Id != guidedId)
                    {
                        if (missile.CreatedAt - previousGuideStart < 5.98f) cadence = false;
                        previousGuideStart = missile.CreatedAt; guidedId = missile.Id;
                    }
                }
                if (!float.IsPositiveInfinity(missile.ImpactsAt)) impactTimes[missile.Id] = missile.ImpactsAt;
            }
            maxGuided = Math.Max(maxGuided, activeGuided);
        }
        int conflicts = 0;
        foreach (float impact in impactTimes.Values)
            foreach (float active in activeTimes)
                if (active > impact - 0.349f && active < impact + 0.2f + 0.349f) conflicts++;
        string suffix = (withBindings ? " after repeated binds" : "") + " (step " + step + ")";
        Check(conflicts == 0, "Melee and missile explosion windows keep 0.35 seconds separation" + suffix + " (conflicts " + conflicts + ")");
        Check(maxGuided <= 1 && m.GuidedCount >= 5, "Phase two manages only one guided missile at a time" + suffix);
        Check(cadence, "Guided launches are at least six seconds apart" + suffix);
    }

    private static void RearDashAndWalls()
    {
        var input = Input(1.2f);
        var m = new Boss1CombatModel(new Boss1Tuning());
        m.Begin(); Wait(m, Boss1State.ComboRecovery, input, 6f);
        Wait(m, Boss1State.Idle, input, 2f);
        input.PlayerX = -1f;
        Check(Wait(m, Boss1State.RearWindup, input, 2f), "Remaining behind for 0.8 seconds triggers rear missiles between patterns");
        Check(m.Missiles.Count == 2 && m.Facing == 1 && m.Missiles[0].TargetX < 0f && m.Missiles[1].TargetX < 0f,
            "Rear attack keeps facing and warns at two fixed behind positions");
        input.PlayerX = 8f;
        Check(Wait(m, Boss1State.DashWindup, input, 4f), "Far target selects a telegraphed booster dash");
        Wait(m, Boss1State.DashActive, input, 2f);
        input.ForwardClearanceH = 0f; m.Tick(0.01f, input);
        Check(m.State == Boss1State.DashRecovery && !m.IsDamageActive && m.DesiredSpeedH == 0f,
            "Wall contact stops dash damage and movement immediately");
        input.ForwardClearanceH = 8f;
        Wait(m, Boss1State.Approach, input, 3f);
        Check(m.State == Boss1State.Approach, "Dash is not chosen twice consecutively");
        input.HasTarget = false; m.Tick(0.02f, input);
        Check(m.State == Boss1State.Dormant && m.Missiles.Count == 0 && !m.IsDamageActive,
            "Losing or killing the target ends attacks and clears warnings");
    }

    public static int Main(string[] args)
    {
        OpeningAndFacing(); PhasesAndDamage(); GuidedTimingAndBind(); PeriodicBarrage();
        ImpactSafety(false, 0.01f); ImpactSafety(true, 0.01f);
        ImpactSafety(false, 0.02f); ImpactSafety(true, 0.02f); RearDashAndWalls();
        string summary = "RESULT: " + (results.Count - failures) + "/" + results.Count + " passed";
        results.Add(summary); Console.WriteLine(summary);
        if (args.Length > 0) File.WriteAllLines(args[0], results.ToArray());
        return failures == 0 ? 0 : 1;
    }
}

