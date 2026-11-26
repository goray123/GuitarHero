using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Boss1Backstep : MonoBehaviour
{
    public enum Phase { Idle, Windup, Moving, Settling }
    [SerializeField] private float closeDuration = 1.5f;
    [SerializeField] private float windup = 0.35f;
    [SerializeField] private float distanceH = 3f;
    [SerializeField] private float moveTime = 0.3f;
    [Tooltip("후딜은 이 값에 1.2배를 적용합니다.")]
    [SerializeField] private float settleTime = 0.3f;
    [SerializeField] private float reuseTime = 6f;
    [SerializeField] private LayerMask terrainLayers = ~0;
    public Phase CurrentPhase { get; private set; }
    public bool IsBusy => CurrentPhase != Phase.Idle;
    public float Facing { get; private set; }
    public float DesiredSpeed { get; private set; }
    public float CooldownRemaining => cooldown;
    public float Progress => Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
    public Vector2 StartPoint { get; private set; }
    public Vector2 EndPoint { get; private set; }
    private BoxCollider2D bodyCollider;
    private float dwell, cooldown, elapsed, duration, remaining, worldHeight;
    private bool followup;
    private readonly List<RaycastHit2D> walls = new List<RaycastHit2D>();

    private void Awake() => bodyCollider = GetComponent<BoxCollider2D>();

    // Observe continuously, but the owner only selects this pattern between attacks.
    public void Observe(Transform player, float height, float facing, float dt)
    {
        worldHeight = height * Mathf.Abs(transform.lossyScale.y);
        if (IsBusy || player == null || !player.gameObject.activeInHierarchy) { dwell = 0f; return; }
        float forward = (player.position.x - transform.position.x) * facing;
        dwell = forward >= 0f ? dwell + dt : 0f;
    }

    public bool TryBegin(float facing)
    {
        if (!isActiveAndEnabled || IsBusy || cooldown > 0f || dwell + 0.00001f < closeDuration) return false;
        Facing = facing < 0f ? -1f : 1f;
        Physics2D.SyncTransforms();
        remaining = FreeDistance(distanceH * worldHeight);
        StartPoint = new Vector2(transform.position.x, bodyCollider.bounds.min.y);
        EndPoint = StartPoint + Vector2.left * Facing * remaining;
        dwell = 0f;
        followup = false;
        Enter(Phase.Windup, windup);
        return true;
    }

    public void Tick(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        DesiredSpeed = 0f;
        if (!IsBusy) return;
        if (CurrentPhase == Phase.Moving)
        {
            float step = Mathf.Min(remaining, distanceH * worldHeight / Mathf.Max(0.001f, moveTime) * dt);
            float free = FreeDistance(step);
            if (free <= 0.001f || remaining <= 0.001f || elapsed + 0.00001f >= duration)
            {
                Enter(Phase.Settling, settleTime * 1.2f);
                return;
            }
            DesiredSpeed = -Facing * free / Mathf.Max(0.0001f, dt);
            remaining -= free;
            elapsed += dt;
            return;
        }
        elapsed += dt;
        if (elapsed + 0.00001f < duration) return;
        if (CurrentPhase == Phase.Windup) Enter(Phase.Moving, moveTime);
        else
        {
            Enter(Phase.Idle, 0f);
            cooldown = reuseTime;
            followup = true;
        }
    }

    public bool ConsumeFollowup()
    {
        bool pending = followup;
        followup = false;
        return pending;
    }

    private void Enter(Phase phase, float seconds)
    {
        CurrentPhase = phase; elapsed = 0f; duration = seconds; DesiredSpeed = 0f;
    }

    private float FreeDistance(float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(terrainLayers);
        walls.Clear();
        bodyCollider.Cast(Vector2.left * Facing, filter, walls, distance + 0.02f);
        float free = distance;
        foreach (var wall in walls)
        {
            if (wall.collider == null || wall.collider.transform.root == transform.root ||
                wall.collider.GetComponentInParent<PlayerMovement>() != null || wall.normal.x * Facing < 0.5f) continue;
            free = Mathf.Min(free, Mathf.Max(0f, wall.distance - 0.02f));
        }
        return free;
    }

    public void Cancel()
    {
        Enter(Phase.Idle, 0f); followup = false; dwell = 0f;
    }
    private void OnDisable() => Cancel();
}
