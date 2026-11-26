using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A·S와 평타보다 먼저 키다운의 시작·종료를 결정한다.
[DefaultExecutionOrder(-25), DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(SpriteRenderer))]
public class PlayerGuitarSpin : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [Header("Channel (Seconds)")]
    [Tooltip("키를 누르고 있어도 이 시간이 지나면 종료")]
    [SerializeField, Min(0.02f)] private float maximumDuration = 4f;
    [Tooltip("키를 떼거나 최대 시간이 끝난 순간부터 계산")]
    [SerializeField, Min(0f)] private float cooldownTime = 6f;
    [Header("Damage")]
    [Tooltip("플레이어 몸체 중심에서 양방향으로 공격하는 반지름(월드 유닛)")]
    [SerializeField, Min(0.1f)] private float attackRadius = 1.6f;
    [Tooltip("연속 타격 간격. 리듬 공격속도와 무관")]
    [SerializeField, Min(0.02f)] private float hitInterval = 0.15f;
    [Tooltip("타격당 기본 피해. 현재 리듬의 공격력 배율 적용")]
    [SerializeField, Min(0f)] private float hitDamage = 3f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [Header("Channel Protection")]
    [Tooltip("회전 중 받는 피해 감소 비율")]
    [SerializeField, Range(0f, 1f)] private float damageReduction = 0.3f;
    [Tooltip("회전 중 좌우 이동 속도 배율")]
    [SerializeField, Range(0f, 1f)] private float moveSpeedMultiplier = 0.4f;
    [SerializeField] private bool showCooldown = true;

    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerChordStrike chordStrike;
    private PlayerRhythmGauge rhythm;
    private PlayerHealth health;
    private Collider2D bodyCollider;
    private InputActionMap actions;
    private InputAction spinAction;
    private bool requested, active;
    private float endTime, nextHitTime, nextUseTime;
    private GUIStyle statusStyle;
    private readonly List<Collider2D> overlaps = new List<Collider2D>(32);
    private readonly HashSet<Damageable> hitTargets = new HashSet<Damageable>();

    public event System.Action<Vector2, Vector2> HitConfirmed;
    public bool IsSpinning => isActiveAndEnabled && active;
    public bool BlocksActions => isActiveAndEnabled && (requested || active);
    public float MoveSpeedMultiplier => Mathf.Clamp01(moveSpeedMultiplier);
    public float DamageTakenMultiplier => IsSpinning ? 1f - Mathf.Clamp01(damageReduction) : 1f;
    public float AttackRadius => Mathf.Max(0.1f, attackRadius);
    public Vector2 Center => bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        chordStrike = GetComponent<PlayerChordStrike>();
        rhythm = GetComponent<PlayerRhythmGauge>();
        health = GetComponent<PlayerHealth>();
        bodyCollider = GetComponent<Collider2D>();
        if (inputActions == null)
        {
            Debug.LogError("PlayerGuitarSpin의 Input Actions를 연결해주세요.", this);
            enabled = false;
            return;
        }
        actions = inputActions.FindActionMap("Player", true).Clone();
        spinAction = actions.FindAction("GuitarSpin", true);
    }

    private void OnEnable()
    {
        if (spinAction == null) return;
        spinAction.performed += OnPressed;
        spinAction.canceled += OnReleased;
        spinAction.Enable();
        if (health != null) health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
        if (spinAction != null)
        {
            spinAction.performed -= OnPressed;
            spinAction.canceled -= OnReleased;
            spinAction.Disable();
        }
        StopSpin(Time.time);
    }

    private void OnDestroy() => actions?.Dispose();
    private void OnPressed(InputAction.CallbackContext context) => RequestSpin();
    private void OnReleased(InputAction.CallbackContext context) => StopSpin(Time.time);
    private void OnApplicationFocus(bool focused) { if (!focused) StopSpin(Time.time); }
    private void OnDamaged(AttackHit hit) { if (health.IsDead) StopSpin(Time.time); }

    private bool CanBegin(float currentTime) => !BlocksActions && currentTime >= nextUseTime &&
        Time.timeScale > 0f && movement.isActiveAndEnabled && movement.IsOnGround &&
        !movement.IsSliding && !movement.IsCrouching &&
        (health == null || !health.IsDead) && (attack == null || !attack.IsAttacking) &&
        (chordStrike == null || !chordStrike.BlocksActions);

    private void RequestSpin()
    {
        if (CanBegin(Time.time)) requested = true;
    }

    private void FixedUpdate() => UpdateSpin(Time.time);

    private void UpdateSpin(float currentTime)
    {
        if (requested)
        {
            requested = false;
            if (!spinAction.IsPressed() || !CanBegin(currentTime)) return;
            active = true;
            endTime = currentTime + Mathf.Max(0.02f, maximumDuration);
            nextHitTime = currentTime;
            movement.ClearActionRequests();
        }
        if (!active) return;
        if (!spinAction.IsPressed() || currentTime >= endTime || !movement.isActiveAndEnabled ||
            (health != null && health.IsDead))
        {
            StopSpin(currentTime);
            return;
        }
        if (currentTime + 0.00001f < nextHitTime) return;
        nextHitTime += Mathf.Max(0.02f, hitInterval);
        DealDamage();
    }

    private void StopSpin(float currentTime)
    {
        requested = false;
        if (!active) return;
        active = false;
        nextUseTime = currentTime + Mathf.Max(0f, cooldownTime);
        movement.ClearActionRequests();
    }

    private void DealDamage()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(targetLayers);
        Vector2 center = Center;
        Physics2D.OverlapCircle(center, AttackRadius, filter, overlaps);
        hitTargets.Clear();
        float damage = Mathf.Max(0f, hitDamage) * (rhythm != null ? rhythm.AttackDamageMultiplier : 1f);
        foreach (Collider2D other in overlaps)
        {
            Damageable target = other.GetComponentInParent<Damageable>();
            if (target == null || !target.isActiveAndEnabled || target.transform.root == transform.root ||
                !hitTargets.Add(target)) continue;
            Vector2 direction = ((Vector2)other.bounds.center - center).normalized;
            Vector2 point = other.ClosestPoint(center);
            if (!target.TryTakeDamage(new AttackHit(damage, point, direction, 0))) continue;
            rhythm?.RecordConfirmedDamage(RhythmHitKind.Skill, damage);
            HitConfirmed?.Invoke(point, direction);
        }
    }

    private void OnGUI()
    {
        if (!showCooldown) return;
        if (statusStyle == null)
            statusStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        string status = IsSpinning ? $"{Mathf.Max(0f, endTime - Time.time):F1}s  /  DR {damageReduction:P0}" :
            Time.time < nextUseTime ? $"Cooldown {nextUseTime - Time.time:F1}s" : "Hold to spin";
        GUI.Box(new Rect(Screen.width - 276f, 68f, 260f, 44f), $"[D] Guitar Spin: {status}", statusStyle);
    }
}
