using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class Boss1Controller : Damageable
{
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
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private readonly Dictionary<int, Boss1MissileView> views = new Dictionary<int, Boss1MissileView>();
    private readonly HashSet<int> liveMissiles = new HashSet<int>();
    private readonly List<int> expiredViews = new List<int>();
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
    public Vector3 Muzzle { get { return transform.position + Vector3.up * playerHeight * 1.55f; } }

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
        Model = new Boss1CombatModel(settings);
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
        SyncMissiles();
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
        float direction = Model.Facing;
        if (hasTarget && (Model.State == Boss1State.Idle || Model.State == Boss1State.Approach))
            direction = targetShape.bounds.center.x < body.position.x ? -1f : 1f;
        Boss1Input input = new Boss1Input
        {
            HasTarget = hasTarget,
            BossX = body.position.x / playerHeight,
            PlayerX = hasTarget ? targetShape.bounds.center.x / playerHeight : 0f,
            PlayerWidthH = hasTarget ? targetShape.bounds.size.x / playerHeight : 0.5f,
            ArenaLeft = arenaLeft / playerHeight,
            ArenaRight = arenaRight / playerHeight,
            ForwardClearanceH = ForwardClearance(direction) / playerHeight
        };
        Model.Tick(Time.fixedDeltaTime, input);
        float vx = Model.DesiredSpeedH * playerHeight;
        float minX = arenaLeft + bodyShape.size.x * 0.5f;
        float maxX = arenaRight - bodyShape.size.x * 0.5f;
        float nextX = Mathf.Clamp(body.position.x + vx * Time.fixedDeltaTime, minX, maxX);
        body.linearVelocity = new Vector2((nextX - body.position.x) / Time.fixedDeltaTime, body.linearVelocity.y);
        if (hasTarget && Model.IsDamageActive) DamageMelee();
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
        Vector2 center, size;
        DamageBox(out center, out size);
        Physics2D.OverlapBox(center, size, 0f, hurtFilter, overlaps);
        if (!overlaps.Contains(targetShape)) return;
        Vector2 point = targetShape.ClosestPoint(center);
        if (target.TryTakeDamage(new AttackHit(target.MaxHealth * Model.DamageRatio, point, Vector2.right * Model.Facing, 0)))
            lastDamagedSequence = Model.AttackSequence;
    }

    private void DamageBox(out Vector2 center, out Vector2 size)
    {
        float range = 1.3f * playerHeight, bottom = 0f, top = 0.65f * playerHeight;
        if (Model.State == Boss1State.PunchActive)
        {
            range = 1.6f * playerHeight;
            bottom = (targetMovement != null ? targetMovement.SlidingColliderHeight : playerHeight * 0.46f) + playerHeight * 0.1f;
            top = Mathf.Max(bottom + playerHeight * 0.3f, playerHeight * 1.1f);
        }
        else if (Model.State == Boss1State.SlamActive)
        {
            range = 1.4f * playerHeight;
            top = playerHeight * 2.2f;
        }
        if (Model.State == Boss1State.DashActive)
        {
            size = new Vector2(1.6f, 0.7f) * playerHeight;
            center = body.position + new Vector2(Model.Facing * 0.25f, 0.35f) * playerHeight;
            return;
        }
        size = new Vector2(range, top - bottom);
        center = new Vector2(body.position.x + Model.Facing * (bodyShape.bounds.extents.x + range * 0.5f),
            bodyShape.bounds.min.y + (top + bottom) * 0.5f);
    }

    private void SyncMissiles()
    {
        liveMissiles.Clear();
        foreach (Boss1Missile missile in Model.Missiles)
        {
            liveMissiles.Add(missile.Id);
            Boss1MissileView view;
            if (!views.TryGetValue(missile.Id, out view))
            {
                if (sprites == null || effectsMaterial == null) continue;
                view = new Boss1MissileView(missile, this, effectsMaterial);
                views.Add(missile.Id, view);
            }
            view.Render(missile, this);
            if (!view.DamageApplied && target != null && !target.IsDead &&
                missile.IsExploding(Model.Time, settings.ExplosionDuration))
            {
                Physics2D.OverlapCircle(view.Ground, settings.ExplosionRadiusH * playerHeight, hurtFilter, overlaps);
                if (overlaps.Contains(targetShape) &&
                    target.TryTakeDamage(new AttackHit(target.MaxHealth * 0.06f, view.Ground, Vector2.up, 0)))
                    view.DamageApplied = true;
            }
        }
        expiredViews.Clear();
        foreach (var entry in views) if (!liveMissiles.Contains(entry.Key)) expiredViews.Add(entry.Key);
        foreach (int id in expiredViews) { views[id].Dispose(); views.Remove(id); }
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
            case Boss1State.BarrageFiring: pose = Boss1Pose.MissileBarrage; first = 3; last = 5; progress = Mathf.Repeat(Model.StateElapsed / settings.BarrageGroupInterval, 1f); break;
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
        foreach (var view in views.Values) view.Dispose();
        views.Clear();
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
        if (!drawAttackGizmos || Model == null || !Model.IsDamageActive) return;
        Vector2 center, size;
        DamageBox(out center, out size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}

