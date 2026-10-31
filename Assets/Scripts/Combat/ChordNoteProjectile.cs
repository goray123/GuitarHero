using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChordNoteProjectile : MonoBehaviour
{
    [Tooltip("음표의 충돌 판정 반지름")]
    [SerializeField, Min(0.01f)] private float hitRadius = 0.1f;

    private const int PathSegments = 48;
    private readonly Vector2[] path = new Vector2[PathSegments + 1];
    private readonly float[] lengths = new float[PathSegments + 1];
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private Transform owner;
    private PlayerRhythmGauge rhythm;
    private uint ownerLifeVersion;
    private ContactFilter2D filter;
    private SpriteRenderer noteSprite;
    private Vector2 origin;
    private float speed;
    private float damage;
    private float travelled;
    private float range;
    private float facing;
    private bool launched;
    private bool spent;
    private int segment;

    public void Launch(Transform shooter, float direction, float lane, float spread, float curve,
        float maxRange, float travelSpeed, float hitDamage, LayerMask layers, int sortingLayer, int sortingOrder)
    {
        owner = shooter;
        rhythm = shooter != null ? shooter.GetComponentInParent<PlayerRhythmGauge>() : null;
        ownerLifeVersion = rhythm != null ? rhythm.LifeVersion : 0;
        facing = direction < 0f ? -1f : 1f;
        origin = transform.position;
        speed = Mathf.Max(0.1f, travelSpeed);
        damage = Mathf.Max(0f, hitDamage);
        range = Mathf.Max(0.1f, maxRange);
        travelled = 0f;
        segment = 0;
        spent = false;
        noteSprite = GetComponent<SpriteRenderer>();
        noteSprite.sortingLayerID = sortingLayer;
        noteSprite.sortingOrder = sortingOrder;
        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(layers);
        float slope = Mathf.Tan(lane * Mathf.Clamp(spread, 0f, 90f) * 0.5f * Mathf.Deg2Rad);
        float bend = (Mathf.Abs(lane) < 0.01f ? 0.35f : lane) * Mathf.Max(0f, curve);
        float total = 0f;
        for (int i = 0; i <= PathSegments; i++)
        {
            float t = i / (float)PathSegments;
            // 퍼짐도 점차 커지고 각 음표가 별도 곡선을 따라 휘어짐
            path[i] = new Vector2(range * t, range * slope * t * t + Mathf.Sin(t * Mathf.PI) * bend);
            if (i > 0) total += Vector2.Distance(path[i - 1], path[i]);
        }
        // 직선 거리가 아닌 실제 곡선 길이를 사거리로 맞춤
        lengths[0] = 0f;
        for (int i = 0; i <= PathSegments; i++)
        {
            path[i] *= range / total;
            path[i].x *= facing;
            if (i > 0) lengths[i] = lengths[i - 1] + Vector2.Distance(path[i - 1], path[i]);
        }
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || spent) return;
        if (owner == null) { Consume(); return; }
        float targetDistance = Mathf.Min(range, travelled + speed * Time.fixedDeltaTime);
        // 빠른 음표도 경로의 작은 구간마다 검사해 적이나 얇은 벽을 통과하지 않음
        while (travelled < targetDistance && !spent)
        {
            int next = Mathf.Min(segment + 1, PathSegments);
            float nextDistance = Mathf.Min(targetDistance, lengths[next]);
            Vector2 to = origin + Vector2.Lerp(path[segment], path[next],
                Mathf.InverseLerp(lengths[segment], lengths[next], nextDistance));
            Vector2 from = transform.position;
            if (CheckCollision(from, to)) return;
            transform.position = to;
            Vector2 delta = to - from;
            if (delta.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x * facing) * Mathf.Rad2Deg * 0.35f);
            travelled = nextDistance;
            if (nextDistance >= lengths[next]) segment = next;
            if (segment == PathSegments) break;
        }
        if (travelled >= range - 0.0001f || segment == PathSegments) Consume();
    }

    private bool CheckCollision(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        Physics2D.OverlapCircle(from, hitRadius, filter, overlaps);
        foreach (Collider2D other in overlaps)
            if (TryContact(other, from, direction)) return true;
        Physics2D.CircleCast(from, hitRadius, direction, filter, hits, Vector2.Distance(from, to));
        foreach (RaycastHit2D hit in hits)
            if (TryContact(hit.collider, hit.point, direction)) return true;
        return false;
    }

    private bool TryContact(Collider2D other, Vector2 point, Vector2 direction)
    {
        if (spent) return true;
        if (other == null || other.transform.IsChildOf(owner)) return false;
        Damageable target = other.GetComponentInParent<Damageable>();
        if (target != null)
        {
            if (!target.isActiveAndEnabled) return false;
            // 음표 하나는 첫 대상에게 한 번만 피해. 다섯 개 전부 맞으면 25
            if (target.TryTakeDamage(new AttackHit(damage, point, direction, 0)) &&
                rhythm != null && rhythm.LifeVersion == ownerLifeVersion)
                rhythm.RecordConfirmedDamage(RhythmHitKind.Note, damage);
            Consume();
            return true;
        }
        if (other.isTrigger) return false;
        Consume();
        return true;
    }

    private void Consume()
    {
        spent = true;
        if (noteSprite != null) noteSprite.enabled = false;
        Destroy(gameObject);
    }
}
