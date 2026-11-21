using System;
using System.Collections.Generic;
using UnityEngine;

// 본체 제어기가 Tick을 호출한다. 별도 FixedUpdate로 이동을 덮어쓰지 않는다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Boss1BoostRush : MonoBehaviour
{
    public enum RushStage { Idle, Windup, Moving, Braking, Interval }

    [Header("Timing (Seconds)")]
    [SerializeField, Min(0.01f)] private float windup = 0.85f;
    [SerializeField, Min(0.01f)] private float travelTime = 0.55f;
    [SerializeField, Min(0.01f)] private float braking = 0.60f;
    [SerializeField, Min(0f)] private float reuseDelay = 5f;
    [SerializeField, Min(0f)] private float patternInterval = 0.4f;
    [Header("Geometry (Player Height H)")]
    [Tooltip("플레이어 높이 H 기준 이동 거리. 일반 이동으로 후퇴해도 닿도록 설정")]
    [SerializeField, Min(0.1f)] private float travelDistance = 12f;
    [Tooltip("점프로 넘을 수 있는 웅크린 몸체와 공격 판정 높이")]
    [SerializeField, Range(0.2f, 1.2f)] private float crouchHeight = 0.9f;
    [Header("Hitbox (player height H)")]
    [Tooltip("Forward distance added beyond the crouched body.")]
    [SerializeField, Min(0f)] private float armReach = 0.25f;
    [SerializeField] private LayerMask terrainLayers = ~0;
    [SerializeField] private LayerMask targetLayers = ~0;
    [Header("Damage")]
    [Tooltip("플레이어 체력 시스템 연결 전 기준 최대 체력")]
    [SerializeField, Min(1f)] private float referencePlayerMaxHealth = 100f;
    [SerializeField, Range(0f, 1f)] private float damageFraction = 0.1f;

    public event Action<PlayerMovement, AttackHit> PlayerHit;
    public RushStage Stage { get; private set; }
    public bool BlocksActions => isActiveAndEnabled && Stage != RushStage.Idle;
    public bool HitboxActive => hitbox != null && hitbox.enabled;
    public float DesiredSpeed { get; private set; }
    public float Facing { get; private set; } = 1f;
    public float CooldownRemaining => cooldown;
    public float PoseHeight => localHeight * crouchHeight;
    public float StageProgress => stageDuration > 0f ? Mathf.Clamp01(elapsed / stageDuration) : 0f;
    public BoxCollider2D Hitbox => hitbox;
    public float RemainingDangerTime => !BlocksActions ? 0f : Stage == RushStage.Windup ?
        Mathf.Max(0f, stageDuration - elapsed) + travelTime : Stage == RushStage.Moving ? Mathf.Max(0f, travelTime - elapsed) : 0f;

    private Rigidbody2D body;
    private Boss1AttackHitboxes attackHitboxes;
    private BoxCollider2D bodyCollider, hitbox;
    private Vector2 standingSize, standingOffset;
    private bool crouched, meleeCompleted, lastWasRush;
    private float elapsed, stageDuration, cooldown, remainingDistance, localHeight, worldHeight;
    private float currentReuse, currentInterval;
    private readonly List<RaycastHit2D> walls = new List<RaycastHit2D>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PlayerMovement> hitPlayers = new HashSet<PlayerMovement>();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        attackHitboxes = GetComponent<Boss1AttackHitboxes>();
        bodyCollider = GetComponent<BoxCollider2D>();
        var child = new GameObject("Hitbox_BoostRush");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        hitbox = child.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    // 첫 패턴은 근접. 돌진을 연속 선택하지 않도록 근접 종료 때 해제한다.
    public void NotifyMeleeCompleted() { meleeCompleted = true; lastWasRush = false; }

    public bool TryBegin(Transform target, float height, float facing, float windupMultiplier, bool fromBackstep = false)
    {
        if (!isActiveAndEnabled || BlocksActions || (!fromBackstep && (cooldown > 0f || !meleeCompleted || lastWasRush)) ||
            target == null || !target.gameObject.activeInHierarchy) return false;
        localHeight = Mathf.Max(0.1f, height);
        worldHeight = localHeight * Mathf.Abs(transform.lossyScale.y);
        Facing = facing < 0f ? -1f : 1f;
        Crouch();
        Physics2D.SyncTransforms();
        // 막힌 방향으로 예고만 반복하지 않도록 최소 전진 공간을 확인한다.
        if (FreeDistance(worldHeight * 0.25f) < worldHeight * 0.2f) { RestoreBody(); return false; }
        hitPlayers.Clear();
        remainingDistance = travelDistance * worldHeight;
        bool phaseTwo = windupMultiplier < 0.999f;
        currentReuse = Mathf.Max(0f, reuseDelay - (phaseTwo ? 1f : 0f));
        currentInterval = phaseTwo ? patternInterval * 0.625f : patternInterval;
        lastWasRush = true;
        Enter(RushStage.Windup, windup * Mathf.Clamp(windupMultiplier, 0.1f, 1f));
        return true;
    }

    // true를 반환한 프레임에는 근접/추적 로직이 이동을 덮어쓰면 안 된다.
    public bool Tick(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        DesiredSpeed = 0f;
        if (!BlocksActions) return false;
        hitbox.enabled = false;
        if (Stage == RushStage.Moving)
        {
            float step = Mathf.Min(remainingDistance, travelDistance * worldHeight / Mathf.Max(0.01f, travelTime) * dt);
            float free = FreeDistance(step);
            if (free <= 0.001f || remainingDistance <= 0.001f || elapsed >= travelTime)
            {
                Enter(RushStage.Braking, braking);
                return true;
            }
            // 벽 앞까지의 실제 이동분만 판정한다. 끝 프레임도 거리를 초과하지 않는다.
            DesiredSpeed = Facing * free / Mathf.Max(0.0001f, dt);
            remainingDistance -= free;
            elapsed += dt;
            EvaluateHitbox(free);
            return true;
        }
        elapsed += dt;
        if (elapsed + 0.00001f < stageDuration) return true;
        if (Stage == RushStage.Windup) Enter(RushStage.Moving, travelTime);
        else if (Stage == RushStage.Braking)
        {
            RestoreBody();
            cooldown = currentReuse;
            Enter(RushStage.Interval, currentInterval);
        }
        else if (Stage == RushStage.Interval) Enter(RushStage.Idle, 0f);
        return true;
    }

    private void Enter(RushStage stage, float duration)
    {
        Stage = stage;
        elapsed = 0f;
        stageDuration = duration;
        DesiredSpeed = 0f;
        hitbox.enabled = false;
    }

    private void Crouch()
    {
        standingSize = bodyCollider.size;
        standingOffset = bodyCollider.offset;
        float height = Mathf.Min(standingSize.y, PoseHeight);
        bodyCollider.size = new Vector2(standingSize.x, height);
        bodyCollider.offset = standingOffset - Vector2.up * (standingSize.y - height) * 0.5f;
        GetHitboxArea(bodyCollider, bodyCollider.offset, height, localHeight, Facing, out var center, out var size);
        hitbox.size = size;
        hitbox.offset = center;
        crouched = true;
    }

    private void GetHitboxArea(BoxCollider2D collider, Vector2 bodyCenter, float height,
        float playerHeight, float facing, out Vector2 center, out Vector2 size)
    {
        var settings = attackHitboxes != null ? attackHitboxes : GetComponent<Boss1AttackHitboxes>();
        float coveredWidth = settings != null ? settings.TorsoSize(collider).x : collider.size.x * 0.5f;
        float extension = armReach * playerHeight;
        size = new Vector2(coveredWidth + extension, height);
        center = bodyCenter + Vector2.right * facing * (collider.size.x - coveredWidth + extension) * 0.5f;
    }

    private void RestoreBody()
    {
        if (!crouched) return;
        bodyCollider.size = standingSize;
        bodyCollider.offset = standingOffset;
        crouched = false;
    }

    private float FreeDistance(float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(terrainLayers);
        walls.Clear();
        bodyCollider.Cast(Vector2.right * Facing, filter, walls, distance + 0.02f);
        float free = distance;
        foreach (var wall in walls)
        {
            if (wall.collider == null || wall.collider.transform.root == transform.root ||
                wall.collider.GetComponentInParent<PlayerMovement>() != null || wall.normal.x * Facing > -0.5f) continue;
            // 접지 중 시작 겹침으로 바닥 법선이 역방향으로 반환되어도 벽으로 취급하지 않는다.
            if (wall.collider.bounds.max.y <= bodyCollider.bounds.min.y + 0.02f) continue;
            free = Mathf.Min(free, Mathf.Max(0f, wall.distance - 0.02f));
        }
        return free;
    }

    private void EvaluateHitbox(float step)
    {
        hitbox.enabled = true;
        Physics2D.SyncTransforms();
        var bounds = hitbox.bounds;
        var center = (Vector2)bounds.center + Vector2.right * Facing * step * 0.5f;
        var size = (Vector2)bounds.size + Vector2.right * step;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        overlaps.Clear();
        // 고속 돌진이 작은 플레이어 콜라이더를 건너뛰지 않도록 이동 구간 전체를 검사한다.
        Physics2D.OverlapBox(center, size, 0f, filter, overlaps);
        foreach (var other in overlaps)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled || player.transform.root == transform.root || !hitPlayers.Add(player)) continue;
            var hit = new AttackHit(referencePlayerMaxHealth * damageFraction,
                other.ClosestPoint(bounds.center), Vector2.right * Facing, 0);
            var receiver = player.GetComponent<Damageable>();
            if (receiver != null && (!receiver.isActiveAndEnabled || !receiver.TryTakeDamage(hit))) continue;
            // 현재 프로토타입에는 플레이어 체력이 없어 이벤트/로그로 피격을 확인한다.
            PlayerHit?.Invoke(player, hit);
            Debug.Log($"[Boss1 BoostRush] {player.name} 피격: {hit.Damage:0.##}", player);
        }
    }

    public void Cancel()
    {
        if (hitbox != null) hitbox.enabled = false;
        RestoreBody();
        Stage = RushStage.Idle;
        DesiredSpeed = 0f;
        hitPlayers.Clear();
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void OnDisable() => Cancel();
    private void OnDestroy()
    {
        if (hitbox == null) return;
        if (Application.isPlaying) Destroy(hitbox.gameObject); else DestroyImmediate(hitbox.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        var owner = GetComponent<Boss1ProximityAttack>();
        var collider = GetComponent<BoxCollider2D>();
        if (owner == null || collider == null) return;

        float height = Mathf.Min(collider.size.y, owner.Height * crouchHeight);
        Vector2 bodyCenter = collider.offset - Vector2.up * (collider.size.y - height) * 0.5f;
        GetHitboxArea(collider, bodyCenter, height, owner.Height, owner.Facing, out var center, out var size);
        var oldMatrix = Gizmos.matrix;
        var oldColor = Gizmos.color;
        Gizmos.color = HitboxActive ? Color.red : Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(HitboxActive ? hitbox.offset : center, HitboxActive ? hitbox.size : size);
        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }
}
