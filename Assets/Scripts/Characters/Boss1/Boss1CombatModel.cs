using System;
using System.Collections.Generic;

public enum Boss1State
{
    Dormant, Idle, Approach, SweepWindup, SweepActive, PunchWindup, PunchActive,
    SlamWindup, SlamActive, ComboRecovery, DashWindup, DashActive, DashRecovery,
    RearWindup, RearRecovery, BarrageWindup, BarrageFiring, BarrageRecovery,
    PhaseTransition, Dead
}

[Serializable]
public sealed class Boss1Tuning
{
    public float MaxHealth = 1500f;
    public float OpeningDelay = 1f;
    public float IdleDelay = 0.6f;
    public float WalkSpeedH = 1.5f;
    public float ComboRangeH = 1.9f;
    public float SweepWindup = 0.65f;
    public float SweepActive = 0.15f;
    public float PunchWindup = 0.75f;
    public float PunchActive = 0.15f;
    public float SlamWindup = 0.85f;
    public float SlamTurnTime = 0.25f;
    public float SlamActive = 0.2f;
    public float ComboRecovery = 1.3f;
    public float ComboCooldown = 2f;
    public float DashThresholdH = 2.5f;
    public float DashDistanceH = 4f;
    public float DashWindup = 0.85f;
    public float DashDuration = 0.55f;
    public float DashRecovery = 1.4f;
    public float DashCooldown = 5f;
    public float RearDistanceH = 2f;
    public float RearResidence = 0.8f;
    public float RearWindup = 0.8f;
    public float RearFlight = 0.6f;
    public float RearRecovery = 1f;
    public float RearCooldown = 7f;
    public float GuidedInterval = 6f;
    public float GuidedTracking = 0.9f;
    public float GuidedFlight = 0.6f;
    public float GuidedResumeDelay = 1.5f;
    public float BarrageInterval = 20f;
    public float BarrageWindup = 1f;
    public float BarrageGroupInterval = 0.6f;
    public float BarrageFlight = 0.8f;
    public float BarrageRecovery = 1.5f;
    public float ExplosionDuration = 0.2f;
    public float ExplosionRadiusH = 0.4f;
    public float ImpactSeparation = 0.35f;
    public float PhaseTransitionTime = 0.4f;
    public float BindReleaseWarning = 0.35f;

    public void Validate()
    {
        MaxHealth = Positive(MaxHealth, 1500f);
        OpeningDelay = Nonnegative(OpeningDelay);
        IdleDelay = Positive(IdleDelay, 0.6f);
        WalkSpeedH = Positive(WalkSpeedH, 1.5f);
        ComboRangeH = Positive(ComboRangeH, 1.9f);
        SweepWindup = Positive(SweepWindup, 0.65f); SweepActive = Positive(SweepActive, 0.15f);
        PunchWindup = Positive(PunchWindup, 0.75f); PunchActive = Positive(PunchActive, 0.15f);
        SlamWindup = Positive(SlamWindup, 0.85f); SlamActive = Positive(SlamActive, 0.2f);
        SlamTurnTime = Math.Max(0f, Math.Min(SlamWindup, Nonnegative(SlamTurnTime)));
        ComboRecovery = Positive(ComboRecovery, 1.3f); ComboCooldown = Nonnegative(ComboCooldown);
        DashThresholdH = Positive(DashThresholdH, 2.5f); DashDistanceH = Positive(DashDistanceH, 4f);
        DashWindup = Positive(DashWindup, 0.85f); DashDuration = Positive(DashDuration, 0.55f);
        DashRecovery = Positive(DashRecovery, 1.4f); DashCooldown = Nonnegative(DashCooldown);
        RearDistanceH = Positive(RearDistanceH, 2f); RearResidence = Positive(RearResidence, 0.8f);
        RearWindup = Positive(RearWindup, 0.8f); RearFlight = Positive(RearFlight, 0.6f);
        RearRecovery = Positive(RearRecovery, 1f); RearCooldown = Nonnegative(RearCooldown);
        GuidedInterval = Positive(GuidedInterval, 6f); GuidedTracking = Positive(GuidedTracking, 0.9f);
        GuidedFlight = Positive(GuidedFlight, 0.6f); GuidedResumeDelay = Nonnegative(GuidedResumeDelay);
        BarrageInterval = Positive(BarrageInterval, 20f); BarrageWindup = Positive(BarrageWindup, 1f);
        BarrageGroupInterval = Positive(BarrageGroupInterval, 0.6f); BarrageFlight = Positive(BarrageFlight, 0.8f);
        BarrageRecovery = Positive(BarrageRecovery, 1.5f); ExplosionDuration = Positive(ExplosionDuration, 0.2f);
        ExplosionRadiusH = Positive(ExplosionRadiusH, 0.4f); ImpactSeparation = Nonnegative(ImpactSeparation);
        PhaseTransitionTime = Positive(PhaseTransitionTime, 0.4f); BindReleaseWarning = Positive(BindReleaseWarning, 0.35f);
    }

    private static float Positive(float v, float fallback) { return Finite(v) && v >= 0.01f ? v : fallback; }
    private static float Nonnegative(float v) { return Finite(v) ? Math.Max(0f, v) : 0f; }
    public static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
}

// Coordinates and speeds are in units of the standing player collider height H.
public struct Boss1Input
{
    public bool HasTarget;
    public float BossX;
    public float PlayerX;
    public float PlayerWidthH;
    public float ArenaLeft;
    public float ArenaRight;
    public float ForwardClearanceH;
}

public enum Boss1MissileKind { Guided, Rear, Barrage }

public sealed class Boss1Missile
{
    public int Id { get; internal set; }
    public Boss1MissileKind Kind { get; internal set; }
    public float TargetX { get; internal set; }
    public float CreatedAt { get; internal set; }
    public float LocksAt { get; internal set; }
    public float LaunchesAt { get; internal set; }
    public float ImpactsAt { get; internal set; }
    public bool Locked { get; internal set; }
    public bool IsExploding(float time, float duration) { return time >= ImpactsAt && time < ImpactsAt + duration; }
}

// Engine-independent scheduler. The Unity adapter supplies physics and renders
// poses; all phase, cooldown, bind and impact-reservation rules live here.
public sealed class Boss1CombatModel
{
    private struct Window
    {
        public float Start, End;
        public Window(float start, float end) { Start = start; End = end; }
    }

    private readonly Boss1Tuning tuning;
    private readonly List<Boss1Missile> missiles = new List<Boss1Missile>();
    private readonly List<Window> bodyWindows = new List<Window>();
    private readonly List<float> recentImpacts = new List<float>();
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<Boss1Missile> readOnlyMissiles;
    private float stateDuration, nextComboAt, nextDashAt, nextRearAt, nextGuidedAt;
    private float rearElapsed, bindRemaining, releaseRemaining, dashStartX;
    private Boss1State lastPattern = Boss1State.Dormant;
    private bool firstCombo = true, dashShown, resumeReservations;
    private int nextMissileId, barrageGroup;

    public Boss1State State { get; private set; }
    public float StateElapsed { get; private set; }
    public float StateProgress { get { return stateDuration > 0f ? Clamp(StateElapsed / stateDuration, 0f, 1f) : 0f; } }
    public float Time { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get { return tuning.MaxHealth; } }
    public int Phase { get; private set; }
    public int Facing { get; private set; }
    public bool PhasePending { get; private set; }
    public bool BarragePending { get; private set; }
    public float BarrageRemaining { get; private set; }
    public int BarrageCount { get; private set; }
    public int GuidedCount { get; private set; }
    public int AttackSequence { get; private set; }
    public bool IsDead { get { return State == Boss1State.Dead; } }
    public bool IsBound { get { return bindRemaining > 0f; } }
    public bool IsSuspended { get { return IsBound || releaseRemaining > 0f || resumeReservations; } }
    public bool IsBarrage { get { return State == Boss1State.BarrageWindup || State == Boss1State.BarrageFiring || State == Boss1State.BarrageRecovery; } }
    public bool IsDamageActive
    {
        get { return !IsSuspended && (State == Boss1State.SweepActive || State == Boss1State.PunchActive ||
            State == Boss1State.SlamActive || State == Boss1State.DashActive); }
    }
    public float DamageRatio { get { return State == Boss1State.SlamActive || State == Boss1State.DashActive ? 0.10f : 0.06f; } }
    public float DesiredSpeedH
    {
        get
        {
            if (IsSuspended || IsDead || (PhasePending || BarragePending) && (State == Boss1State.Idle || State == Boss1State.Approach)) return 0f;
            if (State == Boss1State.Approach) return Facing * tuning.WalkSpeedH;
            if (State == Boss1State.DashActive) return Facing * tuning.DashDistanceH / tuning.DashDuration;
            return 0f;
        }
    }
    public IList<Boss1Missile> Missiles { get { return readOnlyMissiles; } }

    public Boss1CombatModel(Boss1Tuning settings)
    {
        tuning = settings ?? new Boss1Tuning();
        tuning.Validate();
        readOnlyMissiles = missiles.AsReadOnly();
        State = Boss1State.Dormant; Phase = 1; Facing = 1;
        Health = tuning.MaxHealth; BarrageRemaining = tuning.BarrageInterval;
    }

    public void Begin()
    {
        if (State == Boss1State.Dormant) Enter(Boss1State.Idle, tuning.OpeningDelay);
    }

    public void Stop()
    {
        missiles.Clear(); bodyWindows.Clear(); recentImpacts.Clear(); PhasePending = BarragePending = false;
        Enter(Boss1State.Dormant, 0f);
    }

    public bool TakeDamage(float damage)
    {
        if (IsDead || !Boss1Tuning.Finite(damage) || damage <= 0f) return false;
        Health = Math.Max(0f, Health - damage);
        if (Health <= 0f)
        {
            missiles.Clear(); bodyWindows.Clear(); recentImpacts.Clear(); PhasePending = BarragePending = false;
            bindRemaining = releaseRemaining = 0f; resumeReservations = false;
            Enter(Boss1State.Dead, 1.2f);
        }
        else if (Phase == 1 && Health <= tuning.MaxHealth * 0.5f) PhasePending = true;
        return true;
    }

    public void Bind(float seconds)
    {
        if (IsDead || State == Boss1State.Dormant || !Boss1Tuning.Finite(seconds) || seconds <= 0f) return;
        bindRemaining = Math.Max(bindRemaining, seconds);
        releaseRemaining = tuning.BindReleaseWarning;
        resumeReservations = true;
        missiles.RemoveAll(m => m.Kind == Boss1MissileKind.Guided && !m.Locked);
    }

    public void Tick(float seconds, Boss1Input input)
    {
        if (!Boss1Tuning.Finite(seconds) || seconds <= 0f) return;
        // Small deterministic steps preserve window ordering even in editor tests.
        while (seconds > 0.000001f)
        {
            float step = Math.Min(seconds, 0.02f);
            Advance(step, input);
            seconds -= step;
        }
    }

    private void Advance(float dt, Boss1Input input)
    {
        if (State == Boss1State.Dormant) return;
        Time += dt;
        if (IsDead) { StateElapsed += dt; return; }
        UpdateMissiles(input);
        if (!input.HasTarget)
        {
            Stop(); return;
        }
        bool frozen = bindRemaining > 0f || releaseRemaining > 0f;
        if (bindRemaining > 0f) bindRemaining = Math.Max(0f, bindRemaining - dt);
        else if (releaseRemaining > 0f) releaseRemaining = Math.Max(0f, releaseRemaining - dt);
        else if (resumeReservations)
        {
            // Existing locked impacts keep their time. If binding moved a body
            // hit into an impact, keep warning until that impact has cleared.
            frozen = Conflicts(bodyWindows);
            if (!frozen) resumeReservations = false;
        }
        if (frozen)
        {
            ShiftBodyWindows(dt);
            return;
        }

        if (!IsBarrage && !BarragePending)
        {
            BarrageRemaining = Math.Max(0f, BarrageRemaining - dt);
            if (BarrageRemaining <= 0f) BarragePending = true;
        }
        StateElapsed += dt;
        if (State == Boss1State.SlamWindup && StateElapsed < tuning.SlamTurnTime) Face(input);
        if (State == Boss1State.Idle || State == Boss1State.Approach) Choose(input, dt);
        else AdvancePattern(input);

        if (Phase == 2 && !PhasePending && !BarragePending && !IsBarrage &&
            (dashShown || State != Boss1State.DashWindup && State != Boss1State.DashActive && State != Boss1State.DashRecovery) &&
            State != Boss1State.PhaseTransition && State != Boss1State.RearWindup &&
            State != Boss1State.RearRecovery && Time >= nextGuidedAt && !HasGuided())
        {
            float impact = Time + tuning.GuidedTracking + tuning.GuidedFlight;
            if (!ConflictsWithBody(impact))
            {
                AddMissile(Boss1MissileKind.Guided, ClampTarget(input.PlayerX, input), tuning.GuidedTracking, tuning.GuidedTracking, impact);
                nextGuidedAt = Time + tuning.GuidedInterval;
                GuidedCount++;
            }
        }
    }

    private void Choose(Boss1Input input, float dt)
    {
        if (PhasePending)
        {
            if (missiles.Count == 0) Enter(Boss1State.PhaseTransition, tuning.PhaseTransitionTime);
            return;
        }
        if (BarragePending)
        {
            if (missiles.Count == 0)
            {
                BarragePending = false; BarrageCount++; barrageGroup = 0;
                bodyWindows.Clear(); Enter(Boss1State.BarrageWindup, tuning.BarrageWindup);
            }
            return;
        }
        bool behind = (input.PlayerX - input.BossX) * Facing < -0.05f &&
            Math.Abs(input.PlayerX - input.BossX) <= tuning.RearDistanceH;
        rearElapsed = State == Boss1State.Idle && behind ? rearElapsed + dt : 0f;
        if (State == Boss1State.Idle && StateElapsed < stateDuration) return;
        if (firstCombo)
        {
            if (TryReserveCombo()) { firstCombo = false; Face(input); Enter(Boss1State.SweepWindup, tuning.SweepWindup); }
            return;
        }
        if (behind && Time >= nextRearAt && lastPattern != Boss1State.RearWindup)
        {
            // Allow the documented 0.8 s residence even though normal idle is 0.6 s.
            if (rearElapsed < tuning.RearResidence || missiles.Count > 0) return;
            bodyWindows.Clear();
            AddMissile(Boss1MissileKind.Rear, ClampTarget(input.BossX - Facing, input), 0f, tuning.RearWindup,
                Time + tuning.RearWindup + tuning.RearFlight);
            AddMissile(Boss1MissileKind.Rear, ClampTarget(input.BossX - Facing * 2.5f, input), 0f, tuning.RearWindup,
                Time + tuning.RearWindup + tuning.RearFlight);
            Enter(Boss1State.RearWindup, tuning.RearWindup);
            return;
        }
        float distance = Math.Abs(input.PlayerX - input.BossX);
        Face(input);
        if (distance > tuning.DashThresholdH && Time >= nextDashAt && lastPattern != Boss1State.DashWindup &&
            input.ForwardClearanceH > 0.3f && (dashShown || missiles.Count == 0))
        {
            var plan = new List<Window> { new Window(Time + tuning.DashWindup, Time + tuning.DashWindup + tuning.DashDuration) };
            if (!Conflicts(plan))
            {
                SetWindows(plan); Enter(Boss1State.DashWindup, tuning.DashWindup);
                return;
            }
        }
        if (distance <= tuning.ComboRangeH)
        {
            if (Time >= nextComboAt && TryReserveCombo()) Enter(Boss1State.SweepWindup, tuning.SweepWindup);
            else if (State != Boss1State.Idle) Enter(Boss1State.Idle, 0f);
        }
        else if (State != Boss1State.Approach) Enter(Boss1State.Approach, 0f);
    }

    private void AdvancePattern(Boss1Input input)
    {
        if (State == Boss1State.DashActive &&
            (Math.Abs(input.BossX - dashStartX) >= tuning.DashDistanceH || input.ForwardClearanceH <= 0.04f))
        {
            bodyWindows.Clear(); Enter(Boss1State.DashRecovery, tuning.DashRecovery); return;
        }
        if (State == Boss1State.BarrageFiring)
        {
            while (barrageGroup < 3 && StateElapsed + 0.0001f >= barrageGroup * tuning.BarrageGroupInterval)
                FireBarragePair(input);
            if (barrageGroup == 3 && missiles.Count == 0) Enter(Boss1State.BarrageRecovery, tuning.BarrageRecovery);
            return;
        }
        if (StateElapsed + 0.00001f < stateDuration) return;
        switch (State)
        {
            case Boss1State.SweepWindup: Continue(Boss1State.SweepActive, tuning.SweepActive); break;
            case Boss1State.SweepActive: Continue(Boss1State.PunchWindup, tuning.PunchWindup); break;
            case Boss1State.PunchWindup: Continue(Boss1State.PunchActive, tuning.PunchActive); break;
            case Boss1State.PunchActive: Face(input); Continue(Boss1State.SlamWindup, tuning.SlamWindup); break;
            case Boss1State.SlamWindup: Continue(Boss1State.SlamActive, tuning.SlamActive); break;
            case Boss1State.SlamActive: Continue(Boss1State.ComboRecovery, tuning.ComboRecovery); break;
            case Boss1State.ComboRecovery:
                nextComboAt = Time + tuning.ComboCooldown; Finish(Boss1State.SweepWindup); break;
            case Boss1State.DashWindup:
                dashStartX = input.BossX; Continue(Boss1State.DashActive, tuning.DashDuration); break;
            case Boss1State.DashActive: Continue(Boss1State.DashRecovery, tuning.DashRecovery); break;
            case Boss1State.DashRecovery:
                dashShown = true; nextDashAt = Time + tuning.DashCooldown; Finish(Boss1State.DashWindup); break;
            case Boss1State.RearWindup: Continue(Boss1State.RearRecovery, tuning.RearRecovery); break;
            case Boss1State.RearRecovery:
                nextRearAt = Time + tuning.RearCooldown; Finish(Boss1State.RearWindup); break;
            case Boss1State.BarrageWindup:
                Enter(Boss1State.BarrageFiring, 0f); FireBarragePair(input); break;
            case Boss1State.BarrageRecovery:
                BarrageRemaining = tuning.BarrageInterval;
                nextGuidedAt = Math.Max(nextGuidedAt, Time + tuning.GuidedResumeDelay);
                Finish(Boss1State.BarrageWindup); break;
            case Boss1State.PhaseTransition:
                Phase = 2; PhasePending = false; nextGuidedAt = Time + tuning.GuidedResumeDelay;
                Enter(Boss1State.Idle, tuning.IdleDelay); break;
        }
    }

    private bool TryReserveCombo()
    {
        float a = Time + tuning.SweepWindup, b = a + tuning.SweepActive + tuning.PunchWindup;
        float c = b + tuning.PunchActive + tuning.SlamWindup;
        var plan = new List<Window> { new Window(a, a + tuning.SweepActive),
            new Window(b, b + tuning.PunchActive), new Window(c, c + tuning.SlamActive) };
        if (Conflicts(plan)) return false;
        SetWindows(plan);
        return true;
    }

    private void FireBarragePair(Boss1Input input)
    {
        float width = Math.Max(0f, input.ArenaRight - input.ArenaLeft);
        float gap = Math.Max(0.1f, input.PlayerWidthH) + 0.2f;
        float separation = 2f * tuning.ExplosionRadiusH + gap + 0.1f;
        float min = input.ArenaLeft + tuning.ExplosionRadiusH;
        float max = input.ArenaRight - tuning.ExplosionRadiusH;
        // A physically too-narrow arena cannot contain two safe blast zones.
        // Keep this group harmless instead of generating an unavoidable overlap.
        if (max - min >= separation)
        {
            float center = (input.ArenaLeft + input.ArenaRight) * 0.5f;
            float left = barrageGroup == 0 ? input.ArenaLeft + width * 0.2f :
                barrageGroup == 1 ? center - 1.6f : input.PlayerX - separation * 0.5f;
            float right = barrageGroup == 0 ? input.ArenaRight - width * 0.2f :
                barrageGroup == 1 ? center + 1.6f : input.PlayerX + separation * 0.5f;
            left = Clamp(left, min, max - separation);
            right = Clamp(right, left + separation, max);
            AddMissile(Boss1MissileKind.Barrage, left, 0f, 0f, Time + tuning.BarrageFlight);
            AddMissile(Boss1MissileKind.Barrage, right, 0f, 0f, Time + tuning.BarrageFlight);
        }
        barrageGroup++;
    }

    private void AddMissile(Boss1MissileKind kind, float x, float tracking, float launchDelay, float impact)
    {
        missiles.Add(new Boss1Missile { Id = ++nextMissileId, Kind = kind, TargetX = x,
            CreatedAt = Time, LocksAt = Time + tracking, LaunchesAt = Time + launchDelay,
            ImpactsAt = impact, Locked = tracking <= 0f });
    }

    private void UpdateMissiles(Boss1Input input)
    {
        // An explosion's visual may be gone while its separation margin is
        // still required, especially when a bound active hit resumes.
        recentImpacts.RemoveAll(t => Time > t + tuning.ExplosionDuration + tuning.ImpactSeparation + 0.025f);
        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            Boss1Missile missile = missiles[i];
            if (!missile.Locked)
            {
                missile.TargetX = ClampTarget(input.PlayerX, input);
                if (Time >= missile.LocksAt) missile.Locked = true;
            }
            if (Time >= missile.ImpactsAt + tuning.ExplosionDuration)
            {
                recentImpacts.Add(missile.ImpactsAt);
                missiles.RemoveAt(i);
            }
        }
    }

    private bool HasGuided()
    {
        for (int i = 0; i < missiles.Count; i++) if (missiles[i].Kind == Boss1MissileKind.Guided) return true;
        return false;
    }

    private bool Conflicts(IList<Window> plan)
    {
        for (int i = 0; i < plan.Count; i++)
        {
            for (int j = 0; j < missiles.Count; j++)
                if (Overlaps(plan[i], missiles[j].ImpactsAt)) return true;
            for (int j = 0; j < recentImpacts.Count; j++)
                if (Overlaps(plan[i], recentImpacts[j])) return true;
        }
        return false;
    }

    private bool ConflictsWithBody(float impact)
    {
        for (int i = 0; i < bodyWindows.Count; i++) if (Overlaps(bodyWindows[i], impact)) return true;
        return false;
    }

    private bool Overlaps(Window window, float impact)
    {
        // Reserve one fixed-step of tolerance around logical hit windows.
        return window.End + 0.025f >= Time && window.Start - 0.025f < impact + tuning.ExplosionDuration + tuning.ImpactSeparation &&
            window.End + 0.025f > impact - tuning.ImpactSeparation;
    }

    private void SetWindows(List<Window> plan) { bodyWindows.Clear(); bodyWindows.AddRange(plan); }
    private void ShiftBodyWindows(float dt)
    {
        for (int i = 0; i < bodyWindows.Count; i++)
            bodyWindows[i] = new Window(bodyWindows[i].Start + dt, bodyWindows[i].End + dt);
    }
    private void Finish(Boss1State pattern)
    {
        bodyWindows.Clear(); lastPattern = pattern; rearElapsed = 0f;
        Enter(Boss1State.Idle, tuning.IdleDelay);
    }
    private void Face(Boss1Input input) { if (Math.Abs(input.PlayerX - input.BossX) > 0.01f) Facing = input.PlayerX < input.BossX ? -1 : 1; }
    private float ClampTarget(float x, Boss1Input input)
    {
        return Clamp(x, input.ArenaLeft + tuning.ExplosionRadiusH, input.ArenaRight - tuning.ExplosionRadiusH);
    }
    private void Enter(Boss1State state, float duration)
    {
        State = state; StateElapsed = 0f; stateDuration = duration;
        if (state == Boss1State.SweepActive || state == Boss1State.PunchActive ||
            state == Boss1State.SlamActive || state == Boss1State.DashActive) AttackSequence++;
    }
    private void Continue(Boss1State state, float duration)
    {
        // Carry the partial physics step so 0.15/0.65 s phases do not
        // accumulate rounding delays over the three-hit combination.
        float remainder = Math.Max(0f, StateElapsed - stateDuration);
        Enter(state, duration);
        StateElapsed = Math.Min(remainder, duration);
    }
    private static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
}

