using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Boss1AttackHitboxes : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = ~0;
    [Tooltip("체력 시스템 연결 전 임시 최대 체력 기준. 피해는 6%, 6%, 10%.")]
    [SerializeField, Min(1f)] private float referencePlayerMaxHealth = 100f;
    [Header("Combo hitboxes (player height H)")]
    [Tooltip("X: forward reach from the torso edge. Y: top measured from the feet.")]
    [SerializeField] private Vector2 sweepReachAndTopH = new Vector2(1.5f, 1.2f);
    [Tooltip("X: forward reach from the torso edge. Y: top measured from the feet.")]
    [SerializeField] private Vector2 punchReachAndTopH = new Vector2(2f, 1f);
    [Tooltip("X: forward reach from the torso edge. Y: top measured from the feet.")]
    [SerializeField] private Vector2 slamReachAndTopH = new Vector2(2f, 3f);
    [SerializeField, Min(0f)] private float sweepBottomH;
    [SerializeField, Min(0f)] private float slamBottomH;
    [SerializeField, Min(0f)] private float punchSlideClearanceH = 0.1f;
    [Header("Torso hitbox")]
    [Tooltip("Fraction of the body collider covered from the attack-facing edge toward the back.")]
    [SerializeField, Range(0.5f, 1f)] private float torsoCoverage = 0.5f;
    public event Action<PlayerMovement, AttackHit> PlayerHit;
    public int ActiveAttack { get; private set; }
    private Boss1ProximityAttack controller;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PlayerMovement> hitPlayers = new HashSet<PlayerMovement>();
    private readonly BoxCollider2D[] boxes = new BoxCollider2D[3];
    private readonly List<Collider2D> torsoOverlaps = new List<Collider2D>();
    private BoxCollider2D bodyCollider;
    private int serial = -1;

    private void Awake()
    {
        controller = GetComponent<Boss1ProximityAttack>();
        bodyCollider = GetComponent<BoxCollider2D>();
        for (int i = 0; i < boxes.Length; i++)
        {
            var child = new GameObject(new[] { "Hitbox_Sweep", "Hitbox_Punch", "Hitbox_Slam" }[i]);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            boxes[i] = child.AddComponent<BoxCollider2D>();
            boxes[i].isTrigger = true;
            boxes[i].enabled = false;
        }
    }

    // Positive-facing local geometry, measured from the feet and the torso center.
    public void GetArea(int attack, out Vector2 center, out Vector2 size)
    {
        var owner = controller != null ? controller : GetComponent<Boss1ProximityAttack>();
        float h = owner.Height;
        Vector2 reachAndTop = attack == 1 ? sweepReachAndTopH : attack == 2 ? punchReachAndTopH : slamReachAndTopH;
        float reach = Mathf.Max(0f, reachAndTop.x) * h;
        float bottom = attack == 2 ? owner.SlideHeight + punchSlideClearanceH * h :
            (attack == 1 ? sweepBottomH : slamBottomH) * h;
        float top = Mathf.Max(reachAndTop.y * h, bottom + 0.25f * h);
        size = new Vector2(owner.BodyFront + reach, top - bottom);
        center = new Vector2(size.x * 0.5f, (bottom + top) * 0.5f);
    }

    public void Evaluate()
    {
        Close();
        if (!isActiveAndEnabled || controller == null || !controller.isActiveAndEnabled) return;
        if (serial != controller.AttackSerial) { serial = controller.AttackSerial; hitPlayers.Clear(); }
        if (controller.Timeline.CurrentStage != BossMeleeTimeline.Stage.Active) return;
        ActiveAttack = controller.Timeline.Attack;
        var box = boxes[ActiveAttack - 1];
        GetArea(ActiveAttack, out var center, out var size);
        center.x *= controller.Facing;
        box.offset = center;
        box.size = size;
        box.enabled = true;
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        overlaps.Clear();
        box.Overlap(filter, overlaps);
        foreach (var other in overlaps)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled || player.transform.root == transform.root ||
                !hitPlayers.Add(player)) continue;
            DeliverHit(player, other.ClosestPoint(box.bounds.center), Vector2.right * controller.Facing,
                ActiveAttack == 3 ? 0.10f : 0.06f, ActiveAttack);
        }
        HitTorso(controller.Facing, Vector2.right * controller.Facing,
            ActiveAttack == 3 ? 0.10f : 0.06f, ActiveAttack, hitPlayers);
    }

    public Vector2 TorsoSize(BoxCollider2D body)
    {
        return new Vector2(body.size.x * Mathf.Clamp(torsoCoverage, 0.5f, 1f), body.size.y);
    }

    public void HitTorso(float facing, Vector2 direction, float fraction, int attack,
        HashSet<PlayerMovement> alreadyHit, HashSet<PlayerMovement> attackTorsoHits = null)
    {
        if (!isActiveAndEnabled || bodyCollider == null || !bodyCollider.enabled) return;
        GetTorsoArea(bodyCollider, facing, out var center, out var size);
        Vector2 worldCenter = transform.TransformPoint(center);
        Vector2 worldSize = Vector2.Scale(size, transform.lossyScale);
        worldSize = new Vector2(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y));
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        torsoOverlaps.Clear();
        Physics2D.OverlapBox(worldCenter, worldSize, transform.eulerAngles.z, filter, torsoOverlaps);

        foreach (var other in torsoOverlaps)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled || player.transform.root == transform.root ||
                alreadyHit.Contains(player) || (attackTorsoHits != null && !attackTorsoHits.Add(player))) continue;
            alreadyHit.Add(player);
            DeliverHit(player, other.ClosestPoint(worldCenter), direction, fraction, attack);
        }
    }

    private void GetTorsoArea(BoxCollider2D body, float facing, out Vector2 center, out Vector2 size)
    {
        size = TorsoSize(body);
        center = body.offset + Vector2.right * facing * (body.size.x - size.x) * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        var owner = GetComponent<Boss1ProximityAttack>();
        var body = GetComponent<BoxCollider2D>();
        if (owner == null || body == null) return;

        var oldMatrix = Gizmos.matrix;
        var oldColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        GetTorsoArea(body, owner.Facing, out var torsoCenter, out var torsoSize);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(torsoCenter, torsoSize);
        for (int attack = 1; attack <= 3; attack++)
        {
            GetArea(attack, out var center, out var size);
            center.x *= owner.Facing;
            Gizmos.color = attack == 1 ? Color.yellow : attack == 2 ? Color.cyan : Color.red;
            Gizmos.DrawWireCube(center, size);
        }
        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }

    public void DeliverHit(PlayerMovement player, Vector2 point, Vector2 direction, float fraction, int attack)
    {
        if (!isActiveAndEnabled || (targetLayers.value & (1 << player.gameObject.layer)) == 0) return;
        var hit = new AttackHit(referencePlayerMaxHealth * fraction, point, direction, attack);
        var receiver = player.GetComponent<Damageable>();
        if (receiver != null && receiver.isActiveAndEnabled) receiver.TryTakeDamage(hit);
        PlayerHit?.Invoke(player, hit);
    }

    public void Close()
    {
        ActiveAttack = 0;
        foreach (var box in boxes) if (box != null) box.enabled = false;
    }

    private void OnDisable() { Close(); hitPlayers.Clear(); serial = -1; }
}
