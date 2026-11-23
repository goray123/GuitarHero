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
    [Tooltip("Distance extended into the body from its front edge (H).")]
    [SerializeField, Min(0f)] private float sweepBodyReachH;
    [SerializeField, Min(0f)] private float punchBodyReachH;
    [SerializeField, Min(0f)] private float slamBodyReachH;
    [SerializeField, Min(0f)] private float sweepBottomH;
    [SerializeField, Min(0f)] private float slamBottomH;
    [SerializeField, Min(0f)] private float punchSlideClearanceH = 0.1f;
    public event Action<PlayerMovement, AttackHit> PlayerHit;
    public int ActiveAttack { get; private set; }
    private Boss1ProximityAttack controller;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PlayerMovement> hitPlayers = new HashSet<PlayerMovement>();
    private readonly BoxCollider2D[] boxes = new BoxCollider2D[3];
    private int serial = -1;

    private void Awake()
    {
        controller = GetComponent<Boss1ProximityAttack>();
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

    // Positive-facing local geometry; body reach moves only the inner edge.
    public void GetArea(int attack, out Vector2 center, out Vector2 size)
    {
        var owner = controller != null ? controller : GetComponent<Boss1ProximityAttack>();
        float h = owner.Height;
        Vector2 reachAndTop = attack == 1 ? sweepReachAndTopH : attack == 2 ? punchReachAndTopH : slamReachAndTopH;
        float reach = Mathf.Max(0f, reachAndTop.x) * h;
        float bodyReach = Mathf.Max(0f, attack == 1 ? sweepBodyReachH : attack == 2 ? punchBodyReachH : slamBodyReachH) * h;
        float bottom = attack == 2 ? owner.SlideHeight + punchSlideClearanceH * h :
            (attack == 1 ? sweepBottomH : slamBottomH) * h;
        float top = Mathf.Max(reachAndTop.y * h, bottom + 0.25f * h);
        size = new Vector2(reach + bodyReach, top - bottom);
        center = new Vector2(owner.BodyFront + (reach - bodyReach) * 0.5f, (bottom + top) * 0.5f);
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
    }

    private void OnDrawGizmosSelected()
    {
        var owner = GetComponent<Boss1ProximityAttack>();
        var body = GetComponent<BoxCollider2D>();
        if (owner == null || body == null) return;

        var oldMatrix = Gizmos.matrix;
        var oldColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
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
