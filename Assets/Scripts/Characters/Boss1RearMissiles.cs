using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Boss1RearMissiles : MonoBehaviour
{
    [SerializeField] private float dwellTime = 0.8f;
    [SerializeField] private float windup = 0.8f;
    [SerializeField] private float flightTime = 0.6f;
    [Tooltip("후딜은 이 값에 1.2배를 적용합니다.")]
    [SerializeField] private float recovery = 0.5f;
    [SerializeField] private float explosionTime = 0.2f;
    [Tooltip("Explosion radius in player heights (H).")]
    [SerializeField, Min(0.01f)] private float radiusH = 0.4f;
    [SerializeField] private float reuseTime = 7f;
    public bool IsBusy => active && !recoveryFinished;
    public bool WarningVisible => active && age < windup + flightTime;
    public bool InFlight => active && age >= windup && age < windup + flightTime;
    public bool Exploding => active && age >= windup + flightTime && age < windup + flightTime + explosionTime;
    public bool HasLiveMissiles => active;
    public float FlightProgress => Mathf.Clamp01((age - windup) / flightTime);
    public float Facing { get; private set; }
    public float Radius { get; private set; }
    public float CooldownRemaining => cooldown;
    public bool IsQueued => queued;
    public bool ReadyToBegin => queued && !active && cooldown <= 0f && !lastWasRear;
    public void NotifyOtherPatternStarted() => lastWasRear = false;
    public Vector2 LaunchPoint { get; private set; }
    private readonly Vector2[] targets = new Vector2[2];
    private readonly HashSet<PlayerMovement>[] hitPlayers = { new HashSet<PlayerMovement>(), new HashSet<PlayerMovement>() };
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();
    private Boss1ProximityAttack owner;
    private Boss1AttackHitboxes damage;
    private BoxCollider2D bodyCollider;
    private AudioSource warningSource;
    private AudioClip warningClip;
    private float age, dwell, cooldown;
    private bool active, recoveryFinished, lastWasRear;
    private bool queued;
    private float queuedFacing;

    private void Awake()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        damage = GetComponent<Boss1AttackHitboxes>();
        bodyCollider = GetComponent<BoxCollider2D>();
        warningSource = gameObject.AddComponent<AudioSource>();
        warningSource.playOnAwake = false; warningSource.spatialBlend = 0f; warningSource.volume = 0.15f;
        const int rate = 22050;
        var samples = new float[(int)(rate * 0.8f)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate, pulse = t % 0.2f;
            samples[i] = pulse < 0.1f ? Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Sin(Mathf.PI * pulse / 0.1f) : 0f;
        }
        warningClip = AudioClip.Create("Rear Missile Warning", samples.Length, 1, rate, false);
        warningClip.SetData(samples, 0);
    }

    public Vector2 Target(int index) => targets[index];
    public Vector2 MissilePosition(int index)
    {
        float t = FlightProgress;
        return Vector2.Lerp(LaunchPoint, targets[index], t) + Vector2.up * (4f * Radius * t * (1f - t));
    }

    public void Observe(Transform player, float facing, float dt, bool otherPatternActive)
    {

        if (active || player == null || !player.gameObject.activeInHierarchy) { ClearRequest(); return; }
        // Slam turns toward the player before this pattern can be selected. Preserve a
        // qualified request on its original side, rather than erasing it on that turn.
        if (queued)
        {
            if (IsInRearSector(player, queuedFacing)) return;
            ClearRequest();
        }
        dwell = IsInRearSector(player, facing) ? dwell + dt : 0f;
        if (dwell + 0.00001f >= dwellTime)
        {
            queued = true;
            queuedFacing = facing < 0f ? -1f : 1f;
        }
    }

    private bool IsInRearSector(Transform player, float facing)
    {
        return (player.position.x - transform.position.x) * facing < 0f;
    }

    private void ClearRequest() { dwell = 0f; queued = false; }

    // The caller waits for current body attacks and any independent aimed missile volley.
    public bool TryBegin(float facing, bool otherMissilesPending = false)
    {
        if (!isActiveAndEnabled || active || cooldown > 0f || lastWasRear || otherMissilesPending || !queued) return false;
        // The owner adopts this facing at warning start, then holds it through firing.
        Facing = queuedFacing;
        float h = owner.Height * Mathf.Abs(transform.lossyScale.y);
        Radius = radiusH * h;
        LaunchPoint = (Vector2)transform.position + new Vector2(-Facing * 0.7f * h, 2.7f * h);
        Physics2D.SyncTransforms();
        for (int i = 0; i < 2; i++)
        {
            float x = transform.position.x - Facing * (i == 0 ? 1f : 2.5f) * h;
            float y = bodyCollider.bounds.min.y;
            groundHits.Clear();
            Physics2D.Raycast(new Vector2(x, y + h), Vector2.down, new ContactFilter2D { useTriggers = false }, groundHits, 8f * h);
            foreach (var hit in groundHits)
            {
                if (hit.normal.y < 0.5f || hit.collider.GetComponentInParent<PlayerMovement>() != null ||
                    hit.collider.GetComponentInParent<Boss1ProximityAttack>() != null) continue;
                y = hit.point.y; break;
            }
            targets[i] = new Vector2(x, y);
            hitPlayers[i].Clear();
        }
        age = 0f; ClearRequest(); active = true; recoveryFinished = false; lastWasRear = true;
        if (Application.isPlaying) warningSource.PlayOneShot(warningClip);
        return true;
    }

    public void Tick(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (!active) return;
        float previousAge = age;
        age += dt;
        if (!recoveryFinished && age + 0.00001f >= windup + recovery * 1.2f)
        {
            recoveryFinished = true;
            cooldown = reuseTime;
        }
        // Check interval overlap so a long step cannot skip the explosion entirely.
        if (age >= windup + flightTime && previousAge < windup + flightTime + explosionTime)
        {
            Physics2D.SyncTransforms();
            for (int i = 0; i < 2; i++)
            {
                overlaps.Clear();
                Physics2D.OverlapCircle(targets[i], Radius, new ContactFilter2D { useTriggers = false }, overlaps);
                foreach (var collider in overlaps)
                {
                    var player = collider.GetComponentInParent<PlayerMovement>();
                    if (player == null || !player.isActiveAndEnabled || !hitPlayers[i].Add(player)) continue;
                    damage.DeliverHit(player, collider.ClosestPoint(targets[i]), Vector2.up, 0.06f, 5);
                }
            }
        }
        if (age + 0.00001f >= windup + flightTime + explosionTime) active = false;
    }

    public void Cancel()
    {
        active = false; ClearRequest();
        foreach (var set in hitPlayers) set.Clear();
        if (warningSource != null) warningSource.Stop();
    }
    private void OnDisable() => Cancel();
    private void OnDestroy()
    {
        if (warningClip != null) Destroy(warningClip);
        if (warningSource != null) Destroy(warningSource);
    }
}

