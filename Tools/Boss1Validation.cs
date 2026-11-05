using System;
using System.Collections.Generic;
using System.IO;

public static class Boss1Validation
{
    private static readonly List<string> results = new List<string>();
    private static int failures;
    private static Boss1Input Input(float playerX)
    {
        return new Boss1Input { HasTarget = true, BossX = 0f, PlayerX = playerX, PlayerWidthH = 0.47f,
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
    private static Boss1CombatModel PhaseTwo(out Boss1Input input)
    {
        input = Input(1.2f);
        var m = new Boss1CombatModel(new Boss1Tuning());
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
        Check(missile != null && Near(missile.ImpactsAt - missile.CreatedAt, 1.5f, 0.002f),
            "Guided missile has 0.9 second tracking plus 0.6 second locked warning");
        int barrageCount = m.BarrageCount;
        Check(m.TakeDamage(m.MaxHealth * 10f) && m.IsDead && m.Health == 0f && m.Missiles.Count == 0,
            "Lethal damage immediately clears all missiles and transitions");
        m.Tick(60f, input);
        Check(m.Phase <= 2 && m.BarrageCount == barrageCount && !m.IsDamageActive,
            "Death cannot spawn a third phase, barrage or late melee hit");
        Check(!m.TakeDamage(1f), "A corpse rejects later damage");
    }

    private static void GuidedLockAndBind()
    {
        Boss1Input input;
        var m = PhaseTwo(out input);
        while (Guided(m) == null && m.Time < 20f) m.Tick(0.01f, input);
        input.PlayerX = -2f; m.Tick(0.3f, input);
        Boss1Missile missile = Guided(m);
        Check(missile != null && !missile.Locked && Near(missile.TargetX, -2f, 0.001f), "Unfixed guided reticle follows the player");
        m.Bind(0.5f);
        Check(Guided(m) == null, "Bind cancels an unconfirmed guided target");
        m.Tick(1f, input);
        while (Guided(m) == null && m.Time < 30f) m.Tick(0.01f, input);
        missile = Guided(m);
        if (missile == null) { Check(false, "Next guided missile eventually starts"); return; }
        m.Tick(0.92f, input);
        float lockedX = missile.TargetX, impact = missile.ImpactsAt;
        input.PlayerX = 3f; m.Tick(0.1f, input);
        Check(missile.Locked && missile.TargetX == lockedX && missile.ImpactsAt == impact,
            "Locked target position and impact time remain immutable");
        float remaining = m.BarrageRemaining, stateElapsed = m.StateElapsed;
        m.Bind(2f); m.Tick(1f, input);
        Check(Near(m.BarrageRemaining, remaining, 0.001f) && Near(m.StateElapsed, stateElapsed, 0.001f),
            "Bind pauses body progress and the barrage clock");
        Check(missile.ImpactsAt == impact && !m.Missiles.Contains(missile),
            "Already locked missile finishes at its original time during bind");
        Check(!m.IsDamageActive, "Body hitboxes remain off during bind");
        m.Tick(1.1f, input);
        Check(!m.IsDamageActive, "Bind release includes a warning instead of an instant active hit");
    }

    private static void PeriodicBarrage()
    {
        var m = new Boss1CombatModel(new Boss1Tuning());
        var input = Input(1.2f);
        var ids = new HashSet<int>();
        int previousCount = 0, completed = 0;
        float previousRecoveryEnd = -100f, firstStart = -1f, minimumGap = 100f;
        bool timing = true, gapSafe = true, sixPerUse = true;
        Boss1State previousState = Boss1State.Dormant;
        m.Begin();
        while (m.Time < 105f)
        {
            m.Tick(0.01f, input);
            if (m.BarrageCount != previousCount)
            {
                if (firstStart < 0f) firstStart = m.Time;
                if (previousCount > 0 && m.Time < previousRecoveryEnd + 19.95f) timing = false;
                ids.Clear(); previousCount = m.BarrageCount;
            }
            for (int i = 0; i < m.Missiles.Count; i++)
            {
                Boss1Missile a = m.Missiles[i];
                if (a.Kind != Boss1MissileKind.Barrage) continue;
                ids.Add(a.Id);
                for (int j = i + 1; j < m.Missiles.Count; j++)
                {
                    Boss1Missile b = m.Missiles[j];
                    if (!Near(a.CreatedAt, b.CreatedAt, 0.001f)) continue;
                    float gap = Math.Abs(a.TargetX - b.TargetX) - 0.8f;
                    minimumGap = Math.Min(minimumGap, gap);
                    if (gap + 0.001f < input.PlayerWidthH + 0.2f) gapSafe = false;
                }
            }
            if (previousState == Boss1State.BarrageRecovery && m.State == Boss1State.Idle)
            {
                completed++; previousRecoveryEnd = m.Time;
                if (ids.Count != 6) sixPerUse = false;
            }
            previousState = m.State;
        }
        Check(firstStart >= 19.99f, "Barrage never starts before the first 20-second deadline");
        Check(completed >= 3 && m.Phase == 1, "Barrage repeats multiple times entirely within phase one");
        Check(timing, "Repeated barrage waits 20 seconds from prior recovery end");
        Check(sixPerUse, "Every completed barrage fires exactly six missiles");
        Check(gapSafe && minimumGap >= 0.66f, "Each simultaneous pair leaves player width plus 0.2 H safe space");
        Check(m.BarrageCount <= completed + 1, "Delayed barrage does not accumulate multiple queued uses");
    }

    private static void ImpactSafety(bool withBindings, float step)
    {
        Boss1Input input;
        var m = PhaseTwo(out input);
        var impactTimes = new Dictionary<int, float>();
        var activeTimes = new List<float>();
        int maxGuided = 0;
        float previousGuideStart = -100f;
        bool cadence = true;
        int guidedId = 0;
        for (int tick = 0; tick < 14000; tick++)
        {
            if (withBindings && tick % 917 == 450) m.Bind(0.58f);
            if (withBindings && m.IsDamageActive && tick % 17 == 0 && Guided(m) != null && Guided(m).Locked)
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
                if (missile.Locked) impactTimes[missile.Id] = missile.ImpactsAt;
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
        OpeningAndFacing(); PhasesAndDamage(); GuidedLockAndBind(); PeriodicBarrage();
        ImpactSafety(false, 0.01f); ImpactSafety(true, 0.01f);
        ImpactSafety(false, 0.02f); ImpactSafety(true, 0.02f); RearDashAndWalls();
        string summary = "RESULT: " + (results.Count - failures) + "/" + results.Count + " passed";
        results.Add(summary); Console.WriteLine(summary);
        if (args.Length > 0) File.WriteAllLines(args[0], results.ToArray());
        return failures == 0 ? 0 : 1;
    }
}

