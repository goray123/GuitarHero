using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class Boss1Controller : Damageable
{
    private sealed class MissileRuntime
    {
        public Vector2 Position;
        public Vector2 AimOffset;
        public float Travelled;
        public bool DamageApplied;
        public Boss1MissileView View;
    }

    [SerializeField] private Boss1SpriteSet sprites;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private PlayerHealth target;
    [SerializeField] private bool autoBegin = true;
    [SerializeField] private Boss1Tuning settings = new Boss1Tuning();
    [Header("Arena (world X)")]
    [SerializeField] private float arenaLeft = -13f;
    [SerializeField] private float arenaRight = 13f;
    [SerializeField] private bool drawAttackGizmos = true;

    private Rigidbody2D body;
    private BoxCollider2D bodyShape;
    private Collider2D targetShape;
    private PlayerMovement targetMovement;
    private ContactFilter2D solidFilter, hurtFilter;
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(16);
    private readonly List<RaycastHit2D> floorHits = new List<RaycastHit2D>(16);
    private readonly List<RaycastHit2D> missileHits = new List<RaycastHit2D>(16);
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private readonly Dictionary<int, MissileRuntime> missiles = new Dictionary<int, MissileRuntime>();
    private readonly HashSet<int> liveMissiles = new HashSet<int>();
    private readonly List<int> expiredMissiles = new List<int>();
    private Material effectsMaterial;
    private AudioSource warningSource;
    private AudioClip warningClip;
    private Boss1State lastState;
    private int lastDamagedSequence = -1;
    private float playerHeight = 1.31f, hitFlashUntil;
    private bool changedIgnoreCollision;

    public event Action Changed;
    public Boss1CombatModel Model { get; private set; }
    public Boss1Tuning Settings { get { return settings; } }
    public Boss1SpriteSet Sprites { get { return sprites; } }
    public float PlayerHeight { get { return playerHeight; } }
    public float CurrentHealth { get { return Model != null ? Model.Health : settings.MaxHealth; } }
    public float MaxHealth { get { return settings.MaxHealth; } }
    public bool IsDead { get { return Model != null && Model.IsDead; } }
    public Vector3 Muzzle { get { return transform.TransformPoint(Vector3.up * playerHeight * 1.55f); } }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyShape = GetComponent<BoxCollider2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        solidFilter = new ContactFilter2D { useTriggers = false };
        solidFilter.SetLayerMask(~0);
        hurtFilter = new ContactFilter2D { useTriggers = false };
        hurtFilter.SetLayerMask(~0);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null) effectsMaterial = new Material(shader);
        warningSource = gameObject.AddComponent<AudioSource>();
        warningSource.playOnAwake = false;
        warningSource.spatialBlend = 0f;
        warningSource.volume = 0.18f;
        const int count = 2400;
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / 24000f;
            samples[i] = Mathf.Sin(t * 2f * Mathf.PI * 880f) * Mathf.Sin(Mathf.PI * i / count) * 0.4f;
        }
        warningClip = AudioClip.Create("Boss1 lock warning", count, 1, 24000, false);
        warningClip.SetData(samples, 0);
    }

    private void OnEnable()
    {
        settings.Validate();
        Model = new Boss1CombatModel(settings, ResolveGuidedContact);
        lastDamagedSequence = -1;
        lastState = Boss1State.Dormant;
    }

    private void Start()
    {
        if (sprites == null || visual == null)
        {
            Debug.LogError("Boss1 requires its sprite set and a child SpriteRenderer.", this);
            enabled = false;
            return;
        }
        if (target == null) target = FindFirstObjectByType<PlayerHealth>();
        ConfigureTarget(target, arenaLeft, arenaRight);
        if (autoBegin) BeginEncounter();
    }

    public void ConfigureTarget(PlayerHealth player, float left, float right)
    {
        ReleasePlayerCollision();
        target = player;
        arenaLeft = Mathf.Min(left, right);
        arenaRight = Mathf.Max(left, right);
        targetShape = target != null ? target.GetComponent<Collider2D>() : null;
        targetMovement = target != null ? target.GetComponent<PlayerMovement>() : null;
        if (targetMovement != null && targetMovement.StandingColliderHeight > 0f)
            playerHeight = targetMovement.StandingColliderHeight;
        else if (targetShape != null) playerHeight = Mathf.Max(0.1f, targetShape.bounds.size.y);
        // Root is on the floor, and the hurtbox stays independent of the poses.
        bodyShape.size = new Vector2(1.1f, 1.7f) * playerHeight;
        bodyShape.offset = Vector2.up * playerHeight * 0.85f;
        IgnorePlayerCollision();
    }

    public void BeginEncounter()
    {
        if (Model != null && target != null && !target.IsDead) Model.Begin();
    }

    public void ApplyBind(float seconds)
    {
        if (Model == null) return;
        Model.Bind(seconds);
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || Model == null || !Model.TakeDamage(damage)) return false;
        hitFlashUntil = Time.time + 0.12f;
        if (Model.IsDead)
        {
            body.linearVelocity = Vector2.zero;
            ClearMissiles();
        }
        if (Changed != null) Changed();
        return true;
    }

    private void FixedUpdate()
    {
        if (Model == null) return;
        IgnorePlayerCollision();
        bool hasTarget = target != null && target.isActiveAndEnabled && !target.IsDead && targetShape != null;
        Vector2 playerPosition = Vector2.zero;
        if (hasTarget)
        {
            Rigidbody2D playerBody = targetShape.attachedRigidbody;
            playerPosition = playerBody != null ? playerBody.position : (Vector2)target.transform.position;
        }
        float direction = Model.Facing;
        if (hasTarget && (Model.State == Boss1State.Idle || Model.State == Boss1State.Approach))
            direction = playerPosition.x < body.position.x ? -1f : 1f;
        Boss1Input input = new Boss1Input
        {
            HasTarget = hasTarget,
            BossX = body.position.x / playerHeight,
            PlayerX = playerPosition.x / playerHeight,
            PlayerY = playerPosition.y / playerHeight,
            PlayerHeight = playerHeight,
            ArenaLeft = arenaLeft / playerHeight,
            ArenaRight = arenaRight / playerHeight,
            ForwardClearanceH = ForwardClearance(direction) / playerHeight
        };
        Model.Tick(Time.fixedDeltaTime, input);
        float step = Model.DesiredSpeedH * playerHeight * Time.fixedDeltaTime;
        bool dashing = Model.State == Boss1State.DashActive;
        float clearanceH = input.ForwardClearanceH;
        if (dashing)
            clearanceH = Mathf.Min(clearanceH, Model.DashDistanceRemainingH(input.BossX));
        float clearance = clearanceH * playerHeight;
        step = Mathf.Clamp(step, -clearance, clearance);
        float halfWidth = bodyShape.bounds.extents.x;
        float minX = arenaLeft + halfWidth;
        float maxX = arenaRight - halfWidth;
        float nextX = Mathf.Clamp(body.position.x + step, minX, maxX);
        float movement = nextX - body.position.x;
        body.linearVelocity = new Vector2(movement / Time.fixedDeltaTime, body.linearVelocity.y);
        if (hasTarget && Model.IsDamageActive && (!dashing || !Mathf.Approximately(movement, 0f))) DamageMelee();
        SyncMissiles();
        if (lastState != Model.State)
        {
            if (IsWindup(Model.State)) PlayWarning();
            lastState = Model.State;
            if (Changed != null) Changed();
        }
    }

    private float ForwardClearance(float direction)
    {
        float limit = direction > 0f ? arenaRight - bodyShape.bounds.max.x : bodyShape.bounds.min.x - arenaLeft;
        limit = Mathf.Max(0f, limit);
        bodyShape.Cast(Vector2.right * direction, solidFilter, castHits, Mathf.Min(limit, settings.DashDistanceH * playerHeight + 0.1f));
        foreach (RaycastHit2D hit in castHits)
        {
            if (hit.collider == null || Mathf.Abs(hit.normal.x) < 0.5f ||
                hit.collider.GetComponentInParent<Damageable>() != null) continue;
            limit = Mathf.Min(limit, Mathf.Max(0f, hit.distance - 0.02f));
        }
        return limit;
    }

    private void DamageMelee()
    {
        if (lastDamagedSequence == Model.AttackSequence || target == null || target.IsDead) return;
        if (!TryGetDamageBox(out Vector2 center, out Vector2 size)) return;
        Physics2D.OverlapBox(center, size, 0f, hurtFilter, overlaps);
        if (!overlaps.Contains(targetShape)) return;
        Vector2 point = targetShape.ClosestPoint(center);
        if (target.TryTakeDamage(new AttackHit(target.MaxHealth * Model.DamageRatio, point, Vector2.right * Model.Facing, 0)))
            lastDamagedSequence = Model.AttackSequence;
    }

    private bool TryGetDamageBox(out Vector2 center, out Vector2 size)
    {
        float range, top, bottom = 0f;
        switch (Model.State)
        {
            case Boss1State.SweepWindup:
            case Boss1State.SweepActive:
                range = settings.SweepRangeH * playerHeight;
                top = settings.SweepHeightH * playerHeight;
                break;
            case Boss1State.PunchWindup:
            case Boss1State.PunchActive:
                range = settings.PunchRangeH * playerHeight;
                bottom = (targetMovement != null ? targetMovement.SlidingColliderHeight : playerHeight * 0.46f) + playerHeight * 0.1f;
                top = Mathf.Max(bottom + playerHeight * 0.3f, playerHeight * 1.1f);
                break;
            case Boss1State.SlamWindup:
            case Boss1State.SlamActive:
                range = settings.SlamRangeH * playerHeight;
                top = playerHeight * 2.2f;
                break;
            case Boss1State.DashWindup:
            case Boss1State.DashActive:
                size = new Vector2(bodyShape.bounds.size.x + 0.5f * playerHeight, 0.7f * playerHeight);
                center = new Vector2(body.position.x + Model.Facing * 0.25f * playerHeight,
                    bodyShape.bounds.min.y + size.y * 0.5f);
                return true;
            default:
                center = size = Vector2.zero;
                return false;
        }
        size = new Vector2(range, top - bottom);
        center = new Vector2(body.position.x + Model.Facing * (bodyShape.bounds.extents.x + range * 0.5f),
            bodyShape.bounds.min.y + (top + bottom) * 0.5f);
        return true;
    }

    internal void DrawAttackRanges(AttackRangeOverlay overlay)
    {
        if (TryGetDamageBox(out Vector2 center, out Vector2 size))
            overlay.Box(center, size, Model.IsDamageActive ? Color.red : new Color(1f, 0.5f, 0.2f, 0.65f));
        foreach (Boss1Missile missile in Model.Missiles)
            if (missile.IsExploding(Model.Time, settings.ExplosionDuration) &&
                missiles.TryGetValue(missile.Id, out MissileRuntime runtime))
                overlay.Circle(runtime.Position, settings.ExplosionRadiusH * playerHeight, Color.red);
    }

    private MissileRuntime GetMissile(Boss1Missile missile)
    {
        if (missiles.TryGetValue(missile.Id, out MissileRuntime runtime)) return runtime;
        runtime = new MissileRuntime();
        if (missile.Kind == Boss1MissileKind.Guided)
        {
            Vector2 headOffset = new Vector2(-Model.Facing * 0.38f, 1.8f) * playerHeight;
            runtime.Position = body.position + (Vector2)transform.TransformVector(headOffset);
            runtime.AimOffset = (Vector2)targetShape.bounds.center - runtime.Position;
            if (runtime.AimOffset.sqrMagnitude < 0.0001f)
                runtime.AimOffset = Vector2.right * Model.Facing * playerHeight;
        }
        else if (missile.Kind == Boss1MissileKind.Barrage)
            runtime.Position = new Vector2(missile.TargetX, missile.TargetY) * playerHeight;
        else
            runtime.Position = ProjectGround(missile.TargetX * playerHeight);
        if (sprites != null && effectsMaterial != null)
            runtime.View = new Boss1MissileView(missile, this, effectsMaterial, runtime.AimOffset);
        missiles.Add(missile.Id, runtime);
        if (missile.Kind != Boss1MissileKind.Rear) PlayWarning();
        return runtime;
    }

    private bool ResolveGuidedContact(Boss1Missile missile, float flightTime)
    {
        MissileRuntime runtime = GetMissile(missile);
        Vector2 direction = runtime.AimOffset.normalized;
        float travelled = flightTime * settings.GuidedSpeedH * playerHeight;
        float step = travelled - runtime.Travelled;
        if (step <= 0f) return false;
        Physics2D.CircleCast(runtime.Position, settings.GuidedContactRadiusH * playerHeight,
            direction, solidFilter, missileHits, step);
        foreach (RaycastHit2D hit in missileHits)
        {
            if (hit.collider == null ||
                (hit.collider != targetShape && hit.collider.GetComponentInParent<Damageable>() != null)) continue;
            runtime.Position = hit.centroid;
            return true;
        }
        runtime.Position += direction * step;
        runtime.Travelled = travelled;
        return false;
    }

    private void SyncMissiles()
    {
        liveMissiles.Clear();
        foreach (Boss1Missile missile in Model.Missiles)
        {
            liveMissiles.Add(missile.Id);
            MissileRuntime runtime = GetMissile(missile);
            runtime.View?.Render(missile, this, runtime.Position);
            if (!runtime.DamageApplied && target != null && !target.IsDead &&
                missile.IsExploding(Model.Time, settings.ExplosionDuration))
            {
                Physics2D.OverlapCircle(runtime.Position, settings.ExplosionRadiusH * playerHeight, hurtFilter, overlaps);
                if (overlaps.Contains(targetShape) &&
                    target.TryTakeDamage(new AttackHit(target.MaxHealth * 0.06f, runtime.Position, Vector2.up, 0)))
                    runtime.DamageApplied = true;
            }
        }
        expiredMissiles.Clear();
        foreach (var entry in missiles) if (!liveMissiles.Contains(entry.Key)) expiredMissiles.Add(entry.Key);
        foreach (int id in expiredMissiles)
        {
            missiles[id].View?.Dispose();
            missiles.Remove(id);
        }
    }

    public Vector3 ProjectGround(float worldX)
    {
        Vector2 origin = new Vector2(worldX, transform.position.y + playerHeight * 8f);
        Physics2D.Raycast(origin, Vector2.down, solidFilter, floorHits, playerHeight * 24f);
        foreach (RaycastHit2D hit in floorHits)
            if (hit.collider != null && hit.collider.GetComponentInParent<Damageable>() == null && hit.normal.y >= 0.5f)
                return new Vector3(worldX, hit.point.y, 0f);
        return new Vector3(worldX, bodyShape.bounds.min.y, 0f);
    }

    private void LateUpdate()
    {
        if (Model == null || sprites == null || visual == null) return;
        visual.flipX = Model.Facing < 0;
        float progress = Model.StateProgress;
        Boss1Pose pose = Boss1Pose.Idle;
        int first = 0, last = 7;
        switch (Model.State)
        {
            case Boss1State.Approach: pose = Boss1Pose.Walk; progress = Mathf.Repeat(Model.StateElapsed * 1.25f, 1f); break;
            case Boss1State.SweepWindup: pose = Boss1Pose.LowSweep; last = 2; break;
            case Boss1State.SweepActive: pose = Boss1Pose.LowSweep; first = 3; last = 4; break;
            case Boss1State.PunchWindup: pose = Boss1Pose.MidPunch; last = 2; break;
            case Boss1State.PunchActive: pose = Boss1Pose.MidPunch; first = 3; last = 4; break;
            case Boss1State.SlamWindup: pose = Boss1Pose.OverheadSlam; last = 3; break;
            case Boss1State.SlamActive: pose = Boss1Pose.OverheadSlam; first = 4; last = 5; break;
            case Boss1State.ComboRecovery: pose = Boss1Pose.OverheadSlam; first = 6; break;
            case Boss1State.DashWindup: pose = Boss1Pose.BoosterDash; last = 2; break;
            case Boss1State.DashActive: pose = Boss1Pose.BoosterDash; first = 3; last = 5; break;
            case Boss1State.DashRecovery: pose = Boss1Pose.BoosterDash; first = 6; break;
            case Boss1State.RearWindup: pose = Boss1Pose.RearMissile; last = 2; break;
            case Boss1State.RearRecovery: pose = Boss1Pose.RearMissile; first = 3; break;
            case Boss1State.BarrageWindup: pose = Boss1Pose.MissileBarrage; last = 2; break;
            case Boss1State.BarrageFiring: pose = Boss1Pose.MissileBarrage; first = 3; last = 5; progress = Mathf.Repeat(Model.StateElapsed / settings.BarrageShotInterval, 1f); break;
            case Boss1State.BarrageRecovery: pose = Boss1Pose.MissileBarrage; first = 6; break;
            case Boss1State.Dead: pose = Boss1Pose.Death; break;
            default: progress = Mathf.Repeat(Model.StateElapsed, 1f); break;
        }
        if (Model.IsSuspended && (Model.State == Boss1State.SweepActive || Model.State == Boss1State.PunchActive ||
            Model.State == Boss1State.SlamActive || Model.State == Boss1State.DashActive))
            first = last = pose == Boss1Pose.OverheadSlam ? 3 : 2;
        if (!Model.IsDead && Time.time < hitFlashUntil && (Model.State == Boss1State.Idle || Model.State == Boss1State.Approach))
        {
            pose = Boss1Pose.Hit; first = 0; last = 7; progress = 1f - (hitFlashUntil - Time.time) / 0.12f;
        }
        visual.sprite = sprites.Sample(pose, first, last, progress);
        visual.color = Model.IsBound ? new Color(0.45f, 0.85f, 1f, 1f) :
            Time.time < hitFlashUntil && !Model.IsDead ? new Color(1f, 0.6f, 0.6f, 1f) :
            Model.State == Boss1State.PhaseTransition ? new Color(0.5f, 1f, 1f, 1f) : Color.white;
    }

    private static bool IsWindup(Boss1State state)
    {
        return state == Boss1State.SweepWindup || state == Boss1State.PunchWindup ||
            state == Boss1State.SlamWindup || state == Boss1State.DashWindup ||
            state == Boss1State.RearWindup || state == Boss1State.BarrageWindup;
    }

    public void PlayWarning()
    {
        if (warningSource != null && warningClip != null) warningSource.PlayOneShot(warningClip);
    }

    private void IgnorePlayerCollision()
    {
        if (bodyShape == null || targetShape == null || !bodyShape.enabled || !targetShape.isActiveAndEnabled) return;
        if (!Physics2D.GetIgnoreCollision(bodyShape, targetShape))
        {
            Physics2D.IgnoreCollision(bodyShape, targetShape, true);
            changedIgnoreCollision = true;
        }
    }

    private void ReleasePlayerCollision()
    {
        if (changedIgnoreCollision && bodyShape != null && targetShape != null)
            Physics2D.IgnoreCollision(bodyShape, targetShape, false);
        changedIgnoreCollision = false;
    }

    private void ClearMissiles()
    {
        foreach (MissileRuntime runtime in missiles.Values) runtime.View?.Dispose();
        missiles.Clear();
    }

    private void OnDisable()
    {
        ClearMissiles();
        ReleasePlayerCollision();
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        if (Model != null) Model.Stop();
    }

    private void OnDestroy()
    {
        if (effectsMaterial != null) Destroy(effectsMaterial);
        if (warningClip != null) Destroy(warningClip);
    }

    private void OnValidate()
    {
        if (settings == null) settings = new Boss1Tuning();
        settings.Validate();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAttackGizmos || Model == null || !Model.IsDamageActive ||
            !TryGetDamageBox(out Vector2 center, out Vector2 size)) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}

