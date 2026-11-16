using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Boss1AttackHitboxes : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = ~0;
    [Tooltip("체력 시스템 연결 전 임시 최대 체력 기준. 피해는 6%, 6%, 10%.")]
    [SerializeField, Min(1f)] private float referencePlayerMaxHealth = 100f;
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

    // Positive-facing local geometry, measured from feet and the front of the torso.
    public void GetArea(int attack, out Vector2 center, out Vector2 size)
    {
        var owner = controller != null ? controller : GetComponent<Boss1ProximityAttack>();
        float h = owner.Height;
        float reach = (attack == 1 ? 1.5f : 2f) * h;
        float bottom = attack == 2 ? owner.SlideHeight + 0.1f * h : 0f;
        float top = attack == 1 ? 1.2f * h : attack == 2 ? Mathf.Max(h, bottom + 0.25f * h) : 3f * h;
        size = new Vector2(reach, top - bottom);
        center = new Vector2(owner.BodyFront + reach * 0.5f, (bottom + top) * 0.5f);
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
