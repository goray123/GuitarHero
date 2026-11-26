using System.Collections.Generic;
using UnityEngine;

// Runs after the body controller: one independent aimed missile volley at a time.
[DefaultExecutionOrder(20), DisallowMultipleComponent]
public class Boss1AimedMissile : MonoBehaviour
{
    public enum Phase { Idle, Tracking, Locked, Flying, Exploding }
    [Header("Phase 2")]
    [Tooltip("체력 50% 이하에서 현재 패턴·미사일 종료 후 자동 활성화. 수동 테스트에도 사용 가능.")]
    [SerializeField] private bool phaseTwoEnabled;
    [Tooltip("수동으로 새 조준을 일시 중지합니다. 진행 중인 조준과 발사는 계속됩니다.")]
    [SerializeField] private bool suspendNewVolleys;
    [Header("Timing (Seconds)")]
    [SerializeField, Min(0.1f)] private float aimTime = 2f;
    [SerializeField, Min(0f)] private float lockTime = 0.5f;
    [SerializeField, Min(0.1f)] private float startInterval = 6f;
    [SerializeField, Min(0.01f)] private float explosionTime = 0.2f;
    [SerializeField, Min(0f)] private float safetyGap = 0.35f;
    [Header("Prototype Projectile")]
    [SerializeField, Min(0.1f)] private float speedHPerSecond = 12f;
    [SerializeField, Min(0.01f)] private float projectileRadiusH = 0.09f;
    [Tooltip("Explosion radius in player heights (H).")]
    [SerializeField, Min(0.01f)] private float explosionRadiusH = 0.4f;
    [SerializeField, Min(0.1f)] private float maxFlightTime = 3f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    public bool PhaseTwoEnabled { get => phaseTwoEnabled; set => phaseTwoEnabled = value; }
    public bool SuspendNewVolleys { get => suspendNewVolleys; set => suspendNewVolleys = value; }
    public Phase CurrentPhase { get; private set; }
    public bool HasVolley => CurrentPhase != Phase.Idle;
    public bool AimVisible => CurrentPhase == Phase.Tracking || CurrentPhase == Phase.Locked;
    public Vector2 AimOrigin { get; private set; }
    public Vector2 AimEnd { get; private set; }
    public Vector2 Direction { get; private set; } = Vector2.right;
    public Vector2 ProjectilePosition { get; private set; }
    public Vector2 ExplosionPoint { get; private set; }
    public float ExplosionRadius => explosionRadiusH * WorldHeight;
    public int VolleysStarted { get; private set; }
    public float LastAimStartedAt { get; private set; }
    public float Clock => clock;
    public float SafetyGap => Mathf.Max(0.35f, safetyGap);
    private float WorldHeight => owner.Height * Mathf.Abs(transform.lossyScale.y);
    private Boss1ProximityAttack owner;
    private Boss1AttackHitboxes damage;
    private float clock, nextAimAt, stageTime, aimElapsed;
    private bool wasPhaseTwo;
    private readonly List<RaycastHit2D> casts = new List<RaycastHit2D>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PlayerMovement> hitPlayers = new HashSet<PlayerMovement>();
    private AudioSource warningSource;
    private AudioClip warningClip;

    private void Awake()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        damage = GetComponent<Boss1AttackHitboxes>();
        warningSource = gameObject.AddComponent<AudioSource>();
        warningSource.playOnAwake = false; warningSource.spatialBlend = 0f; warningSource.volume = 0.12f;
        const int rate = 22050;
        var samples = new float[(int)(rate * 0.12f)];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * i / rate) * Mathf.Sin(Mathf.PI * i / samples.Length);
        warningClip = AudioClip.Create("Aim Lock Beep", samples.Length, 1, rate, false);
        warningClip.SetData(samples, 0);
    }

    private void FixedUpdate() => Tick(Time.fixedDeltaTime);

    public void Tick(float dt)
    {
        clock += dt;
        if (!phaseTwoEnabled || owner == null || !owner.isActiveAndEnabled)
        {
            Cancel(); wasPhaseTwo = false; return;
        }
        if (!wasPhaseTwo) { nextAimAt = Mathf.Max(nextAimAt, clock); wasPhaseTwo = true; }
        if (CurrentPhase == Phase.Idle)
        {
            if (suspendNewVolleys || clock < nextAimAt || !owner.HasMissileTarget) return;
            CurrentPhase = Phase.Tracking; aimElapsed = 0f; stageTime = 0f;
            LastAimStartedAt = clock; nextAimAt = clock + startInterval; VolleysStarted++;
            UpdateAim(true); Beep(1f); return;
        }
        stageTime += dt;
        if (AimVisible)
        {
            if (!owner.HasMissileTarget) { Cancel(); return; }
            aimElapsed += dt;
            // Lock angle, not target position: the muzzle can follow the moving boss without homing.
            UpdateAim(CurrentPhase == Phase.Tracking);
            if (CurrentPhase == Phase.Tracking && aimElapsed + 0.00001f >= Mathf.Max(0f, aimTime - lockTime))
            { CurrentPhase = Phase.Locked; Beep(1.5f); }
            if (aimElapsed + 0.00001f >= aimTime)
            {
                ProjectilePosition = AimOrigin;
                CurrentPhase = Phase.Flying; stageTime = 0f; hitPlayers.Clear();
            }
            return;
        }
        if (CurrentPhase == Phase.Flying)
        {
            float distance = speedHPerSecond * WorldHeight * dt;
            if (Cast(ProjectilePosition, Direction, distance, out var hit))
            {
                ProjectilePosition = hit.centroid;
                ExplosionPoint = hit.point;
                CurrentPhase = Phase.Exploding; stageTime = 0f;
                DamageExplosion();
            }
            else
            {
                ProjectilePosition += Direction * distance;
                if (stageTime >= maxFlightTime) CurrentPhase = Phase.Idle;
            }
            return;
        }
        if (CurrentPhase == Phase.Exploding)
        {
            if (stageTime + 0.00001f >= explosionTime) CurrentPhase = Phase.Idle;
            else DamageExplosion();
        }
    }

    private void UpdateAim(bool track)
    {
        AimOrigin = (Vector2)transform.position + new Vector2(owner.Facing * 0.55f * WorldHeight, 3.05f * WorldHeight);
        if (track)
        {
            Vector2 offset = owner.MissileTargetPoint - AimOrigin;
            if (offset.sqrMagnitude > 0.0001f) Direction = offset.normalized;
        }
        float distance = speedHPerSecond * WorldHeight * maxFlightTime;
        AimEnd = Cast(AimOrigin, Direction, distance, out var hit) ? hit.point : AimOrigin + Direction * distance;
    }

    private bool Cast(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D nearest)
    {
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(collisionLayers);
        casts.Clear();
        Physics2D.CircleCast(origin, projectileRadiusH * WorldHeight, direction, filter, casts, distance);
        foreach (var hit in casts)
        {
            if (hit.collider == null || hit.collider.transform.root == transform.root) continue;
            nearest = hit; return true;
        }
        nearest = default; return false;
    }

    private void DamageExplosion()
    {
        Physics2D.SyncTransforms(); overlaps.Clear();
        Physics2D.OverlapCircle(ExplosionPoint, ExplosionRadius, new ContactFilter2D { useTriggers = false }, overlaps);
        foreach (var collider in overlaps)
        {
            var player = collider.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled || !hitPlayers.Add(player)) continue;
            damage.DeliverHit(player, collider.ClosestPoint(ExplosionPoint), Direction, 0.06f, 6);
        }
    }

    private void Beep(float pitch)
    {
        if (!Application.isPlaying) return;
        warningSource.pitch = pitch; warningSource.PlayOneShot(warningClip);
    }
    public void Cancel()
    {
        CurrentPhase = Phase.Idle; hitPlayers.Clear();
        if (warningSource != null) warningSource.Stop();
    }
    private void OnDisable() { Cancel(); wasPhaseTwo = false; }
    private void OnDestroy()
    {
        if (warningClip != null) Destroy(warningClip);
        if (warningSource != null) Destroy(warningSource);
    }
}

