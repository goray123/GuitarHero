using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(Boss1AttackHitboxes))]
public class Boss1ProximityAttack : MonoBehaviour
{
    private const float GapClosingDistanceH = 5f;

    [SerializeField] private Transform player;

    [Tooltip("플레이어가 없을 때 사용하는 서 있는 높이 H")]
    [SerializeField, Min(0.1f)] private float referenceHeight = 1.31f;
    [SerializeField, Min(0.01f)] private float referenceSlideHeight = 0.6f;
    [Tooltip("1페이즈 1, 2페이즈 0.8. 예고만 단축하고 판정/후딜은 유지")]
    [SerializeField, Range(0.5f, 1f)] private float windupMultiplier = 1f;
    [SerializeField, Min(0f)] private float meleeCooldown = 1f;

    public BossMeleeTimeline Timeline { get; } = new BossMeleeTimeline();
    public float Facing { get; private set; } = -1f;
    public float PreviousFacing { get; private set; } = -1f;
    public float TurnDuration { get; private set; } = 0.25f;
    public int AttackSerial { get; private set; }
    public float Height => referenceHeight;
    public float SlideHeight => referenceSlideHeight;
    public float BodyFront => GetComponent<BoxCollider2D>().size.x * 0.5f;
    public bool IsMoving => Mathf.Abs(desiredSpeed) > 0.01f;

    private Rigidbody2D body;
    private Collider2D playerCollider;
    private BoxCollider2D bodyCollider;
    private Boss1AttackHitboxes hitboxes;
    private Boss1BoostRush boostRush;
    private float desiredSpeed;
    private float cooldown;
    private Boss1LeapSlam leap;
    private bool hasStartedMelee;
    public enum Pattern { None, Melee, Backstep, Rush, Leap, RearMissiles }
    public enum ExclusiveAction { None, PhaseTransition, Barrage }
    public Pattern LastPattern { get; private set; }
    public int PatternSerial { get; private set; }
    public bool PhaseTransitionPending { get; set; }
    public bool BarragePending { get; set; }
    public bool BarrageActive { get; set; }
    public ExclusiveAction NextExclusiveAction => PhaseTransitionPending ? ExclusiveAction.PhaseTransition :
        BarragePending && !ShouldCloseDistance ? ExclusiveAction.Barrage : ExclusiveAction.None;
    public bool ExclusiveActionPending => NextExclusiveAction != ExclusiveAction.None;
    public bool ReadyForExclusiveAction => ExclusiveActionPending && !BodyBusy && (boostStep == null || !boostStep.IsBusy) &&
        (leap == null || !leap.WavesActive) &&
        (rearMissiles == null || !rearMissiles.HasLiveMissiles) && rearSafetyRemaining <= 0f;
    public bool BodyBusy => BarrageActive || Timeline.Attack != 0 || (leap != null && leap.IsBusy) ||
        (boostRush != null && boostRush.BlocksActions) || (backstep != null && backstep.IsBusy) ||
        (rearMissiles != null && rearMissiles.IsBusy);
    private bool firstMeleeFinished, seenRush, seenLeap;
    private float meleeReuseRemaining, rearSafetyRemaining;
    private float PatternInterval => EffectiveWindupMultiplier < 0.999f ? 0.25f : 0.4f;
    private float MissileSafetyGap => aimedMissile != null ? aimedMissile.SafetyGap : 0.35f;
    private bool RearPending => hasStartedMelee && rearMissiles != null &&
        rearMissiles.isActiveAndEnabled && rearMissiles.ReadyToBegin && LastPattern != Pattern.RearMissiles;
    private Boss1Backstep backstep;
    private Boss1RearMissiles rearMissiles;
    private Boss1BoostStep boostStep;
    private Boss1AimedMissile aimedMissile;
    private Boss1Health health;
    private float EffectiveWindupMultiplier => (health != null && health.IsPhaseTwo) ||
        (aimedMissile != null && aimedMissile.isActiveAndEnabled && aimedMissile.PhaseTwoEnabled) ? 0.8f : windupMultiplier;
    public bool HasMissileTarget => HasPlayer;
    public Vector2 MissileTargetPoint => playerCollider != null ? (Vector2)playerCollider.bounds.center : HasPlayer ? (Vector2)player.position : (Vector2)transform.position;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        hitboxes = GetComponent<Boss1AttackHitboxes>();
        leap = GetComponent<Boss1LeapSlam>();
        backstep = GetComponent<Boss1Backstep>();
        rearMissiles = GetComponent<Boss1RearMissiles>();
        aimedMissile = GetComponent<Boss1AimedMissile>();
        health = GetComponent<Boss1Health>();
        boostStep = GetComponent<Boss1BoostStep>();
        if (boostStep == null) boostStep = gameObject.AddComponent<Boss1BoostStep>();
        boostRush = GetComponent<Boss1BoostRush>();
        if (boostRush == null) boostRush = gameObject.AddComponent<Boss1BoostRush>();
        if (GetComponent<Boss1BoostRushVisual>() == null) gameObject.AddComponent<Boss1BoostRushVisual>();
        // Prototype poses and combat share one clock; imported clips cannot override it.
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) { Facing = sprite.flipX ? -1f : 1f; sprite.enabled = false; }
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMovement>()?.transform;
        if (player == null) return;
        playerCollider = player.GetComponent<Collider2D>();
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            float scale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
            referenceHeight = movement.StandingColliderHeight / scale;
            referenceSlideHeight = movement.SlideColliderHeight / scale;
        }
    }

    private bool HasPlayer => player != null && player.gameObject.activeInHierarchy;

    private bool ShouldCloseDistance
    {
        get
        {
            if (!HasPlayer) return false;
            float targetX = playerCollider != null ? playerCollider.bounds.center.x : player.position.x;
            float worldH = Height * Mathf.Abs(transform.lossyScale.y);
            return Mathf.Abs(targetX - transform.position.x) >= GapClosingDistanceH * worldH;
        }
    }

    // Use horizontal reach, not distance to the sprite pivot (jumping changes that pivot).
    private bool InRange(int attack)
    {
        if (!HasPlayer) return false;
        hitboxes.GetArea(attack, out var center, out var size);
        float reach = (center.x + size.x * 0.5f) * Mathf.Abs(transform.lossyScale.x);
        float targetX = playerCollider != null ? playerCollider.bounds.center.x : player.position.x;
        float targetHalfWidth = playerCollider != null ? playerCollider.bounds.extents.x : 0f;
        float feet = playerCollider != null ? playerCollider.bounds.min.y : player.position.y;
        return Mathf.Abs(targetX - transform.position.x) <= reach + targetHalfWidth &&
            Mathf.Abs(feet - bodyCollider.bounds.min.y) <= Height * 3f * Mathf.Abs(transform.lossyScale.y);
    }

    private void FacePlayer()
    {
        if (!HasPlayer) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f) Facing = Mathf.Sign(dx);
    }

    private void BeginAttack(int attack)
    {
        PreviousFacing = Facing;
        // Sweep/punch share a direction. Slam samples once; its model turns for 0.25s.
        if (attack == 1 || attack == 3) FacePlayer();
        TurnDuration = 0.25f * EffectiveWindupMultiplier;
        Timeline.Begin(attack, EffectiveWindupMultiplier);
        AttackSerial++;
        hasStartedMelee = true;
        if (attack == 1) RecordPattern(Pattern.Melee);
        desiredSpeed = 0f;
    }

    private void FixedUpdate()
    {
        if (playerCollider != null && playerCollider.isActiveAndEnabled &&
            !Physics2D.GetIgnoreCollision(bodyCollider, playerCollider))
            Physics2D.IgnoreCollision(bodyCollider, playerCollider);
        Tick(Time.fixedDeltaTime);
        if (leap == null || leap.CurrentPhase != Boss1LeapSlam.Phase.Airborne)
            body.linearVelocity = new Vector2(desiredSpeed, body.linearVelocity.y);
        hitboxes.Evaluate();
    }

    private void Tick(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        meleeReuseRemaining = Mathf.Max(0f, meleeReuseRemaining - dt);
        rearSafetyRemaining = Mathf.Max(0f, rearSafetyRemaining - dt);
        desiredSpeed = 0f;
        if (BarrageActive) return;
        if (rearMissiles != null && rearMissiles.isActiveAndEnabled)
        {
            bool otherPattern = Timeline.Attack != 0 || (leap != null && leap.IsBusy) ||
                (boostRush != null && boostRush.BlocksActions) || (backstep != null && backstep.IsBusy);
            rearMissiles.Observe(player, Facing, dt, otherPattern);
            bool wasBusy = rearMissiles.IsBusy;
            bool hadMissiles = rearMissiles.HasLiveMissiles;
            rearMissiles.Tick(dt);
            if (hadMissiles && !rearMissiles.HasLiveMissiles) rearSafetyRemaining = MissileSafetyGap;
            if (wasBusy)
            {
                leap?.Tick(dt); boostRush?.Tick(dt); backstep?.Tick(dt);
                if (!rearMissiles.IsBusy) cooldown = PatternInterval;
                return;
            }
        }
        if (backstep != null && backstep.isActiveAndEnabled)
        {
            backstep.Observe(player, Height, Facing, dt);
            bool wasBackstepping = backstep.IsBusy;
            backstep.Tick(dt);
            if (wasBackstepping)
            {
                leap?.Tick(dt);
                boostRush?.Tick(dt);
                desiredSpeed = backstep.DesiredSpeed;
                if (backstep.ConsumeFollowup() && !ExclusiveActionPending) BeginBackstepFollowup();
                return;
            }
        }
        if (leap != null && leap.isActiveAndEnabled)
        {
            bool wasBusy = leap.IsBusy;
            leap.Tick(dt);
            if (leap.IsBusy) { boostRush?.Tick(dt); return; }
            if (wasBusy) { cooldown = PatternInterval; return; }
        }
        if (boostRush != null && boostRush.Tick(dt))
        {
            desiredSpeed = boostRush.DesiredSpeed;
            return;
        }
        if (Timeline.Attack != 0)
        {
            if (!Timeline.Advance(dt)) return;
            int completed = Timeline.Attack;
            // Complete this strike's recovery before deciding whether to continue.
            bool continueMelee = completed < 3 && HasPlayer;
            Timeline.Stop();
            if (continueMelee) { BeginAttack(completed + 1); return; }
            firstMeleeFinished = true;
            meleeReuseRemaining = meleeCooldown;
            cooldown = PatternInterval;

            boostRush?.NotifyMeleeCompleted();
        }
        if (!HasPlayer) { boostStep?.Cancel(); return; }
        // Exclusive requests drain already announced actions; never change their timing.
        if (ExclusiveActionPending)
        {
            // Finish a step already in progress, but never begin another one.
            if (boostStep != null && boostStep.IsBusy)
            {
                boostStep.Tick(dt, DistanceToMeleeReach());
                desiredSpeed = boostStep.DesiredSpeed;
            }
            return;
        }
        if (boostStep != null && boostStep.IsBusy)
        {
            if (cooldown <= 0f || InRange(1) || RearPending) boostStep.Cancel();
            else
            {
                boostStep.Tick(dt, DistanceToMeleeReach());
                desiredSpeed = boostStep.DesiredSpeed;
                if (boostStep.IsBusy) return;
            }
        }
        bool missilesBusy = rearMissiles != null && rearMissiles.HasLiveMissiles;
        bool mayBegin = rearSafetyRemaining <= 0f && !missilesBusy;
        // Select only gap closers at long range, including the opening pattern.
        if (ShouldCloseDistance)
        {
            FacePlayer();
            if (mayBegin && cooldown <= 0f) TryApproachPattern(false, missilesBusy);
            return;
        }
        if (RearPending)
        {
            if (cooldown <= 0f && mayBegin && rearMissiles.TryBegin(Facing, missilesBusy))
            {
                PreviousFacing = Facing; Facing = rearMissiles.Facing;
                RecordPattern(Pattern.RearMissiles);
            }
            return;
        }
        if (mayBegin && hasStartedMelee && cooldown <= 0f && backstep != null && backstep.TryBegin(Facing))
        { RecordPattern(Pattern.Backstep); return; }
        float worldH = Height * Mathf.Abs(transform.lossyScale.y);
        if (mayBegin && firstMeleeFinished && cooldown <= 0f)
        {
            FacePlayer();
            if (TryApproachPattern(false, missilesBusy)) return;
        }
        if (mayBegin && cooldown <= 0f && meleeReuseRemaining <= 0f)
        {
            BeginAttack(1);
            return;
        }
        FacePlayer();
        if (!InRange(1)) boostStep?.Begin(Facing, worldH, EffectiveWindupMultiplier < 0.999f);
    }

    private void RecordPattern(Pattern pattern)
    {
        LastPattern = pattern; PatternSerial++;
        if (pattern != Pattern.Rush) boostRush?.NotifyMeleeCompleted();
        if (pattern != Pattern.RearMissiles) rearMissiles?.NotifyOtherPatternStarted();
    }

    private bool TryRush(bool followup, bool missilesBusy)
    {
        if (boostRush == null || (!followup && LastPattern == Pattern.Rush) || (!seenRush && missilesBusy)) return false;
        if (!boostRush.TryBegin(player, Height, Facing, EffectiveWindupMultiplier, followup)) return false;
        seenRush = true; RecordPattern(Pattern.Rush);
        return true;
    }

    private bool TryLeap(bool followup, bool missilesBusy)
    {
        if (leap == null || (!followup && LastPattern == Pattern.Leap) || (!seenLeap && missilesBusy)) return false;
        if (!leap.TryBegin(player, EffectiveWindupMultiplier, followup)) return false;
        seenLeap = true; RecordPattern(Pattern.Leap);
        return true;
    }

    private bool TryApproachPattern(bool followup, bool missilesBusy)
    {
        bool leapFirst = Random.value < 0.5f;
        if (leapFirst && TryLeap(followup, missilesBusy)) return true;
        if (TryRush(followup, missilesBusy)) return true;
        return !leapFirst && TryLeap(followup, missilesBusy);
    }

    private float DistanceToMeleeReach()
    {
        float targetX = playerCollider != null ? playerCollider.bounds.center.x : player.position.x;
        float halfWidth = playerCollider != null ? playerCollider.bounds.extents.x : 0f;
        hitboxes.GetArea(1, out var center, out var size);
        float reach = (center.x + size.x * 0.5f) * Mathf.Abs(transform.lossyScale.x) + halfWidth;
        return Mathf.Max(0f, Mathf.Abs(targetX - transform.position.x) - reach + 0.001f);
    }

    private void BeginBackstepFollowup()
    {
        if (!HasPlayer) return;
        cooldown = 0f;
        FacePlayer();
        if (ShouldCloseDistance)
        {
            TryApproachPattern(true, false);
            return;
        }
        if (Random.Range(0, 3) == 0) { BeginAttack(1); return; }
        if (TryApproachPattern(true, false)) return;
        BeginAttack(1);
    }
    private void OnDisable()
    {
        Timeline.Stop();
        desiredSpeed = 0f;
        cooldown = 0f;
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        if (hitboxes != null) hitboxes.Close();
        if (leap != null) leap.Cancel();
        boostRush?.Cancel();
        backstep?.Cancel();
        rearMissiles?.Cancel();
        aimedMissile?.Cancel();
        boostStep?.Cancel();
    }
}

