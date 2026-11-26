using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Boss1LeapSlam : MonoBehaviour
{
    public enum Phase { Idle, Windup, Airborne, Impact, Recovery }
    [SerializeField] private float arenaMinX = -40f;
    [SerializeField] private float arenaMaxX = 40f;
    [SerializeField, Min(0.01f)] private float windup = 0.8f;
    [SerializeField, Min(0.01f)] private float flightTime = 0.7f;
    [SerializeField, Min(0f)] private float apexHeightH = 2.5f;
    [SerializeField, Min(0.01f)] private float impactTime = 0.15f;
    [Tooltip("후딜은 이 값에 1.2배를 적용합니다.")]
    [SerializeField, Min(0f)] private float recoveryTime = 0.55f;
    [SerializeField, Min(0f)] private float reuseTime = 6f;
    public Phase CurrentPhase { get; private set; }
    public bool IsBusy => CurrentPhase != Phase.Idle;
    public bool Ready => !IsBusy && cooldown <= 0f;
    public float Progress => Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
    public Vector2 LandingPoint { get; private set; }
    [Header("Hitboxes (player height H)")]
    [SerializeField, Min(0.01f)] private float landingRadiusH = 1f;
    [SerializeField, Min(0.01f)] private float waveHeightH = 0.75f;
    [SerializeField] private Sprite waveSprite;
    public Sprite WaveSprite => waveSprite;
    [SerializeField, Min(0.01f)] private float waveWidthH = 0.35f;
    [SerializeField, Min(0.01f)] private float waveRangeH = 4f;

    public float Radius => landingRadiusH * H;
    public float WaveHeight => waveHeightH * H;
    public float WaveWidth => waveWidthH * H;
    public float WaveDistance { get; private set; }
    public bool WavesActive { get; private set; }
    public bool MarkerVisible => CurrentPhase == Phase.Airborne || CurrentPhase == Phase.Impact;
    public float RemainingDangerTime => CurrentPhase == Phase.Windup ? Mathf.Max(0f, duration - elapsed) + flightTime + 0.5f :
        CurrentPhase == Phase.Airborne ? Mathf.Max(0f, flightTime - elapsed) + 0.5f :
        Mathf.Max(CurrentPhase == Phase.Impact ? Mathf.Max(0f, impactTime - elapsed) : 0f,
            WavesActive ? Mathf.Max(0f, (waveRangeH * H - WaveDistance) / (8f * H)) : 0f);
    private float H => owner.Height * Mathf.Abs(transform.lossyScale.y);
    private Boss1ProximityAttack owner;
    private Boss1AttackHitboxes damage;
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Transform target;
    private Vector2 startPosition, landingBody;
    private float elapsed, duration, cooldown, savedGravity;
    private float currentReuse;
    private bool gravityOverridden;
    private readonly List<Collider2D> results = new List<Collider2D>();
    private readonly HashSet<PlayerMovement> hitPlayers = new HashSet<PlayerMovement>();
    private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();

    private void Awake()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        damage = GetComponent<Boss1AttackHitboxes>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    public bool TryBegin(Transform player, float windupMultiplier, bool fromBackstep = false)
    {
        if (!isActiveAndEnabled || IsBusy || (!fromBackstep && !Ready) || player == null || !player.gameObject.activeInHierarchy) return false;
        target = player;
        currentReuse = Mathf.Max(0f, reuseTime - (windupMultiplier < 0.999f ? 1f : 0f));
        hitPlayers.Clear();
        WavesActive = false;
        WaveDistance = 0f;
        Enter(Phase.Windup, windup * windupMultiplier);
        return true;
    }

    private void Enter(Phase phase, float seconds)
    {
        CurrentPhase = phase;
        elapsed = 0f;
        duration = seconds;
    }

    // Called by the boss's fixed-step controller, so melee and leap never run together.
    public void Tick(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (WavesActive) AdvanceWaves(dt);
        if (!IsBusy) return;
        elapsed += dt;
        if (CurrentPhase == Phase.Windup)
        {
            if (elapsed + 0.00001f >= duration) TakeOff();
        }
        else if (CurrentPhase == Phase.Airborne)
        {
            float t = Mathf.Clamp01(elapsed / flightTime);
            Vector2 position = Vector2.Lerp(startPosition, landingBody, t);
            position.y += 4f * apexHeightH * H * t * (1f - t);
            body.MovePosition(position);
            if (t >= 1f)
            {
                body.position = landingBody;
                RestoreGravity();
                Enter(Phase.Impact, impactTime);
                WavesActive = true;
                WaveDistance = 0f;
                HitLanding();
            }
        }
        else if (CurrentPhase == Phase.Impact)
        {
            if (elapsed + 0.00001f >= duration) Enter(Phase.Recovery, recoveryTime * 1.2f);
            else HitLanding();
        }
        else if (elapsed + 0.00001f >= duration)
        {
            Enter(Phase.Idle, 0f);
            cooldown = currentReuse;
        }
    }

    private void TakeOff()
    {
        startPosition = body.position;
        float feetY = bodyCollider.bounds.min.y;
        float halfWidth = bodyCollider.bounds.extents.x;
        float minimum = arenaMinX + halfWidth + 0.05f;
        float maximum = arenaMaxX - halfWidth - 0.05f;
        float x = Mathf.Clamp(target != null ? target.position.x : startPosition.x,
            Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));
        var filter = new ContactFilter2D { useTriggers = false };
        groundHits.Clear();
        Physics2D.Raycast(new Vector2(x, feetY + H), Vector2.down, filter, groundHits, 8f * H);
        foreach (var hit in groundHits)
        {
            if (hit.normal.y < 0.5f || hit.collider.GetComponentInParent<PlayerMovement>() != null ||
                hit.collider.GetComponentInParent<Boss1ProximityAttack>() != null) continue;
            feetY = hit.point.y;
            break;
        }
        LandingPoint = new Vector2(x, feetY);
        landingBody = new Vector2(x, feetY + (body.position.y - bodyCollider.bounds.min.y));
        savedGravity = body.gravityScale;
        gravityOverridden = true;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        Enter(Phase.Airborne, flightTime);
    }

    private void HitLanding()
    {
        results.Clear();
        Physics2D.SyncTransforms();
        Physics2D.OverlapCircle(LandingPoint, Radius, new ContactFilter2D { useTriggers = false }, results);
        HitResults(0.10f, Vector2.down);
    }

    public Vector2 WaveCenter(int direction) => LandingPoint + new Vector2(direction * WaveDistance, WaveHeight * 0.5f);

    private void AdvanceWaves(float dt)
    {
        float previous = WaveDistance;
        WaveDistance = Mathf.Min(waveRangeH * H, WaveDistance + 8f * H * dt);
        // Sweep the travelled segment to avoid tunnelling through thin player colliders.
        foreach (int direction in new[] { -1, 1 })
        {
            Vector2 center = LandingPoint + new Vector2(direction * (previous + WaveDistance) * 0.5f, WaveHeight * 0.5f);
            results.Clear();
            Physics2D.OverlapBox(center, new Vector2(WaveWidth + WaveDistance - previous, WaveHeight), 0,
                new ContactFilter2D { useTriggers = false }, results);
            HitResults(0.06f, Vector2.right * direction);
        }
        if (WaveDistance >= waveRangeH * H) WavesActive = false;
    }

    private void HitResults(float fraction, Vector2 direction)
    {
        foreach (var collider in results)
        {
            var player = collider.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled || !hitPlayers.Add(player)) continue;
            // One hit per leap prevents simultaneous landing + two waves triple-hitting a player.
            damage.DeliverHit(player, collider.ClosestPoint(LandingPoint), direction, fraction, 4);
        }
    }

    private void RestoreGravity()
    {
        if (!gravityOverridden) return;
        body.gravityScale = savedGravity;
        body.linearVelocity = Vector2.zero;
        gravityOverridden = false;
    }

    public void Cancel()
    {
        RestoreGravity();
        CurrentPhase = Phase.Idle;
        WavesActive = false;
        hitPlayers.Clear();
        target = null;
    }
    private void OnDisable() => Cancel();
}
