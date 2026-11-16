using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class Boss1BoostStep : MonoBehaviour
{
    public enum Phase { Idle, Preparing, Moving, Resting }
    [SerializeField, Min(0.01f)] private float prepareTime = 0.2f;
    [SerializeField, Min(0.01f)] private float moveTime = 0.2f;
    [SerializeField, Min(0.01f)] private float distanceH = 1.2f;
    [SerializeField, Min(0f)] private float restTime = 0.3f;
    [SerializeField, Min(0f)] private float phaseTwoRestTime = 0.2f;
    public Phase CurrentPhase { get; private set; }
    public bool IsBusy => CurrentPhase != Phase.Idle;
    public float Facing { get; private set; }
    public float DesiredSpeed { get; private set; }
    private float elapsed, remaining, speed, rest;
    private BoxCollider2D box;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Awake() => box = GetComponent<BoxCollider2D>();
    public void Begin(float facing, float worldHeight, bool phaseTwo)
    {
        if (!isActiveAndEnabled || IsBusy) return;
        Facing = Mathf.Sign(facing); remaining = distanceH * worldHeight;
        speed = remaining / moveTime; rest = phaseTwo ? phaseTwoRestTime : restTime;
        Enter(Phase.Preparing);
    }
    public void Tick(float dt, float distanceToReach)
    {
        DesiredSpeed = 0f;
        if (!isActiveAndEnabled || distanceToReach <= 0.001f) { Cancel(); return; }
        if (CurrentPhase == Phase.Idle) return;
        if (CurrentPhase == Phase.Moving)
        {
            float step = Mathf.Min(speed * dt, remaining, distanceToReach);
            Physics2D.SyncTransforms(); hits.Clear();
            // Inset the query so resting floor contact is not reported as an initial overlap.
            var bounds = box.bounds;
            Vector2 size = (Vector2)bounds.size - Vector2.one * 0.04f;
            size = Vector2.Max(size, Vector2.one * 0.01f);
            Physics2D.BoxCast(bounds.center, size, 0f, Vector2.right * Facing,
                new ContactFilter2D { useTriggers = false }, hits, step + 0.04f);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.root == transform.root ||
                    hit.collider.GetComponentInParent<PlayerMovement>() != null || hit.normal.x * Facing > -0.5f) continue;
                step = Mathf.Min(step, Mathf.Max(0f, hit.distance - 0.04f));
            }
            if (step <= 0.001f || remaining <= 0.001f || elapsed + 0.00001f >= moveTime)
            { Enter(Phase.Resting); return; }
            DesiredSpeed = Facing * step / dt; remaining -= step; elapsed += dt;
            return;
        }
        elapsed += dt;
        if (CurrentPhase == Phase.Preparing && elapsed + 0.00001f >= prepareTime) Enter(Phase.Moving);
        else if (CurrentPhase == Phase.Resting && elapsed + 0.00001f >= rest) Enter(Phase.Idle);
    }
    private void Enter(Phase phase) { CurrentPhase = phase; elapsed = 0f; DesiredSpeed = 0f; }
    public void Cancel() => Enter(Phase.Idle);
    private void OnDisable() => Cancel();
}
