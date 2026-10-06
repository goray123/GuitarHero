using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 같은 물리 프레임에서 공격 여부를 먼저 결정
[DefaultExecutionOrder(-10)]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Attack 1 and 2 Timing (Seconds)")]
    [Tooltip("1, 2타 공통 선딜레이")]
    [SerializeField, Min(0.01f)] private float basicWindupTime = 0.125f;
    [Tooltip("1, 2타 공통 히트박스 활성 시간")]
    [SerializeField, Min(0.01f)] private float basicActiveTime = 0.125f;
    [Tooltip("1, 2타 공통 후딜레이. 이 구간부터 슬라이드로 취소 가능")]
    [SerializeField, Min(0f)] private float basicRecoveryTime = 0.125f;
    [Tooltip("방향키를 누르며 1, 2타를 시작할 때 선딜레이 동안 이동할 거리")]
    [SerializeField, Min(0f)] private float basicAdvanceDistance = 0.6f;

    [Header("Attack 3 Timing (Seconds)")]
    [SerializeField, Min(0.01f)] private float thirdWindupTime = 0.375f;
    [SerializeField, Min(0.01f)] private float thirdActiveTime = 0.125f;
    [Tooltip("공중 낙하 공격이 착지한 뒤 히트박스를 유지할 시간")]
    [SerializeField, Min(0f)] private float thirdLandingActiveTime = 0.125f;
    [Tooltip("3타 후딜레이. 이 구간부터 슬라이드로 취소 가능")]
    [SerializeField, Min(0f)] private float thirdRecoveryTime = 0.125f;

    [Header("Attack 3 Movement")]
    [Tooltip("방향키와 함께 3타를 시작할 때 정점까지 이동할 가로 거리")]
    [SerializeField, Min(0f)] private float thirdAdvanceDistance = 1.2f;
    [Tooltip("3타 선딜레이 끝의 정점 높이. 공격 시작 위치 기준")]
    [SerializeField, Min(0f)] private float thirdHopHeight = 0.6f;
    [Tooltip("3타 히트박스 활성 시간 동안 공중에서 수직으로 내려가는 속도")]
    [SerializeField, Min(0.01f)] private float thirdAttackFallSpeed = 20f;

    [Header("Combo")]
    [Tooltip("1, 2타 후딜레이 종료 후 다음 타격을 이어갈 수 있는 시간")]
    [SerializeField, Min(0f)] private float comboResetTime = 1f;

    [Header("Attack Hitboxes")]
    [Tooltip("바라보는 방향에 맞춰 좌우 반전할 히트박스 부모")]
    [SerializeField] private Transform hitboxRoot;
    [SerializeField] private BoxCollider2D attack1Hitbox;
    [SerializeField] private BoxCollider2D attack2Hitbox;
    [SerializeField] private BoxCollider2D attack3Hitbox;

    [Header("Attack Damage")]
    [Tooltip("1타 데미지")]
    [SerializeField, Min(0f)] private float attack1Damage = 8f;
    [Tooltip("2타 데미지")]
    [SerializeField, Min(0f)] private float attack2Damage = 8f;
    [Tooltip("3타 데미지")]
    [SerializeField, Min(0f)] private float attack3Damage = 25f;

    private readonly HashSet<Damageable> hitTargets = new HashSet<Damageable>();

    private PlayerMovement movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private InputActionMap playerActions;
    private InputAction attackAction;
    private InputAction moveAction;
    private bool attackRequested;
    private float requestedDirection;
    private float attackDirection;
    private bool isAttacking;
    private int comboStep;
    private float attackStartTime;
    private float windupEndTime;
    private float activeEndTime;
    private float attackEndTime;
    private float comboResetAt;
    private bool comboTimerPaused;
    private float windupDuration;
    private float advanceDistance;
    private float hopHeight;
    private bool isDamageActive;
    private bool thirdActiveStarted;
    private bool waitingForLanding;
    private bool isDiveAttack;

    // 원본 스프라이트의 준비 / 휘두르기 / 마무리 구간 경계
    private static readonly float[] SwingStarts = { 11f / 21f, 8f / 15f, 14f / 24f };
    private static readonly float[] SwingEnds = { 15f / 21f, 11f / 15f, 20f / 24f };

    public bool IsAttacking => isAttacking;
    private bool IsSliding => movement != null && movement.IsSliding;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateHitboxes();
        if (inputActions == null || animator == null)
        {
            Debug.LogError("PlayerAttack의 Input Actions와 Animator를 연결해주세요.", this);
            enabled = false;
            return;
        }

        playerActions = inputActions.FindActionMap("Player", true).Clone();
        attackAction = playerActions.FindAction("Attack", true);
        moveAction = playerActions.FindAction("Move", true);
    }

    private void OnEnable()
    {
        if (attackAction == null)
            return;
        attackAction.performed += OnAttack;
        moveAction.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
            playerActions.Disable();
        }
        StopAttack();
        comboStep = 0;
        comboTimerPaused = false;
    }

    private void OnDestroy()
    {
        playerActions?.Dispose();
        playerActions = null;
        attackAction = null;
        moveAction = null;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        // 공격 중 누른 입력은 예약하지 않고 무시
        if (IsSliding || isAttacking || attackRequested)
            return;

        // 입력 순간의 방향을 저장해 시전 중 방향 변경 방지
        float input = moveAction.ReadValue<Vector2>().x;
        attackRequested = true;
        requestedDirection = Mathf.Abs(input) > 0.01f ? Mathf.Sign(input) : 0f;
    }

    private void FixedUpdate()
    {
        UpdateAttack(Time.time);
    }

    private void Update()
    {
        UpdateAnimation(Time.time);
    }

    private void LateUpdate()
    {
        UpdateHitboxDirection();
    }

    private void UpdateAttack(float currentTime)
    {
        UpdateDiveTiming(currentTime);
        if (isAttacking && currentTime >= attackEndTime)
        {
            isAttacking = false;
            if (comboStep < 3)
            {
                // 후딜레이가 끝난 시각부터 연속타 유지 시간 계산
                comboResetAt = attackEndTime + Mathf.Max(0f, comboResetTime);
            }
            else
            {
                comboStep = 0;
            }
        }

        if (!isAttacking && !comboTimerPaused && currentTime >= comboResetAt)
            comboStep = 0;
        if (attackRequested && !isAttacking && !IsSliding)
            StartAttack(currentTime, requestedDirection);

        attackRequested = false;
        isDamageActive = isAttacking && currentTime >= windupEndTime && currentTime < activeEndTime;
        animator.SetBool("IsAttacking", isAttacking);
        UpdateHitboxes();
        UpdateAnimation(currentTime);
    }

    // 공중 3타는 착지까지 유지한 뒤 착지 판정 시간부터 계산
    private void UpdateDiveTiming(float currentTime)
    {
        if (!isAttacking || comboStep != 3 || currentTime < windupEndTime)
            return;

        if (!thirdActiveStarted)
        {
            thirdActiveStarted = true;
            waitingForLanding = movement != null && !movement.IsOnGround;
            isDiveAttack = waitingForLanding;
            if (waitingForLanding)
            {
                activeEndTime = float.PositiveInfinity;
                attackEndTime = float.PositiveInfinity;
            }
        }

        if (waitingForLanding && movement.IsOnGround)
        {
            waitingForLanding = false;
            activeEndTime = currentTime + Mathf.Max(0f, thirdLandingActiveTime);
            attackEndTime = activeEndTime + Mathf.Max(0f, thirdRecoveryTime);
        }
    }

    public bool CanCancelWithSlide(float currentTime)
    {
        return isAttacking && currentTime >= activeEndTime && currentTime < attackEndTime;
    }

    // 슬라이드 캔슬은 타격 순서를 유지하고 초기화 타이머만 정지
    public void CancelForSlide()
    {
        StopAttack();
        comboTimerPaused = comboStep > 0;
    }

    public void ResumeComboAfterSlide(float currentTime)
    {
        if (!comboTimerPaused)
            return;

        // 캔슬 슬라이드가 끝난 시각부터 연속타 입력 시간 계산
        comboResetAt = currentTime + Mathf.Max(0f, comboResetTime);
        comboTimerPaused = false;
    }

    private void StopAttack()
    {
        isAttacking = false;
        isDamageActive = false;
        waitingForLanding = false;
        thirdActiveStarted = false;
        isDiveAttack = false;
        attackRequested = false;
        UpdateHitboxes();
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    // 선딜레이에서만 지정 거리만큼 전진. 벽 충돌은 물리 엔진이 처리
    public Vector2 GetAttackVelocity(Vector2 velocity, bool isGrounded, float currentTime,
        float fixedDeltaTime, float gravityScale)
    {
        if (!isAttacking)
            return velocity;

        velocity.x = 0f;
        if (currentTime < windupEndTime && attackDirection != 0f)
        {
            float dt = Mathf.Max(0.0001f, fixedDeltaTime);
            float t0 = Mathf.Clamp01((currentTime - attackStartTime) / windupDuration);
            float t1 = Mathf.Clamp01((currentTime + dt - attackStartTime) / windupDuration);
            velocity.x = attackDirection * advanceDistance * (t1 - t0) / dt;
            if (comboStep == 3)
            {
                // 선딜레이 끝에서 정점에 도달하는 포물선의 상승 구간
                float y0 = hopHeight * (2f * t0 - t0 * t0);
                float y1 = hopHeight * (2f * t1 - t1 * t1);
                velocity.y = (y1 - y0) / dt - Physics2D.gravity.y * gravityScale * dt;
            }
        }
        else if (comboStep == 3 && currentTime >= windupEndTime && currentTime < activeEndTime && !isGrounded)
        {
            velocity.y = -Mathf.Max(0.01f, thirdAttackFallSpeed);
        }
        return velocity;
    }

    private void StartAttack(float currentTime, float direction)
    {
        hitTargets.Clear();
        waitingForLanding = false;
        thirdActiveStarted = false;
        isDiveAttack = false;
        comboStep = comboStep >= 3 ? 1 : comboStep + 1;
        comboTimerPaused = false;
        isAttacking = true;
        isDamageActive = false;
        attackDirection = direction;
        movement?.ClearSlideRequest();
        if (spriteRenderer != null && direction != 0f)
            spriteRenderer.flipX = direction < 0f;

        bool third = comboStep == 3;
        windupDuration = Mathf.Max(0.01f, third ? thirdWindupTime : basicWindupTime);
        float active = Mathf.Max(0.01f, third ? thirdActiveTime : basicActiveTime);
        float recovery = Mathf.Max(0f, third ? thirdRecoveryTime : basicRecoveryTime);
        advanceDistance = Mathf.Max(0f, third ? thirdAdvanceDistance : basicAdvanceDistance);
        hopHeight = Mathf.Max(0f, thirdHopHeight);
        attackStartTime = currentTime;
        windupEndTime = currentTime + windupDuration;
        activeEndTime = windupEndTime + active;
        attackEndTime = activeEndTime + recovery;

        animator.SetBool("IsAttacking", true);
        animator.SetFloat("AttackProgress", 0f);
        animator.Play("Base Layer.PlayerAttack" + comboStep, 0, 0f);
        UpdateHitboxes();
    }

    private void UpdateAnimation(float currentTime)
    {
        if (!isAttacking || animator == null)
            return;
        float swingStart = SwingStarts[comboStep - 1];
        float swingEnd = SwingEnds[comboStep - 1];
        float progress;
        if (currentTime < windupEndTime)
            progress = Mathf.Lerp(0f, swingStart, Mathf.InverseLerp(attackStartTime, windupEndTime, currentTime));
        else if (isDiveAttack && currentTime < activeEndTime)
            // 휘두르기를 재생한 뒤 착지 판정이 끝날 때까지 마지막 자세 유지
            progress = Mathf.Lerp(swingStart, swingEnd,
                Mathf.InverseLerp(windupEndTime, windupEndTime + Mathf.Max(0.01f, thirdActiveTime), currentTime));
        else if (currentTime < activeEndTime)
            progress = Mathf.Lerp(swingStart, swingEnd, Mathf.InverseLerp(windupEndTime, activeEndTime, currentTime));
        else
            progress = Mathf.Lerp(swingEnd, 1f, Mathf.InverseLerp(activeEndTime, attackEndTime, currentTime));
        // Inspector의 각 구간 시간에 애니메이션도 맞춤
        animator.SetFloat("AttackProgress", progress);
    }

    private void UpdateHitboxes()
    {
        SetHitbox(attack1Hitbox, isDamageActive && comboStep == 1);
        SetHitbox(attack2Hitbox, isDamageActive && comboStep == 2);
        SetHitbox(attack3Hitbox, isDamageActive && comboStep == 3);
        UpdateHitboxDirection();
    }

    // 활성 히트박스에 닿은 대상은 타격당 한 번만 피해 적용
    public void TryHit(Collider2D hitbox, Collider2D other)
    {
        if (!isActiveAndEnabled || !isDamageActive || !isAttacking)
            return;
        Collider2D activeHitbox = comboStep == 1 ? attack1Hitbox :
            comboStep == 2 ? attack2Hitbox : attack3Hitbox;
        if (hitbox == null || hitbox != activeHitbox || !hitbox.enabled || other == null)
            return;
        Damageable target = other.GetComponentInParent<Damageable>();
        if (target == null || !target.isActiveAndEnabled ||
            target.transform.root == transform.root || hitTargets.Contains(target))
            return;
        float damage = comboStep == 1 ? attack1Damage : comboStep == 2 ? attack2Damage : attack3Damage;
        if (target.TryTakeDamage(Mathf.Max(0f, damage)))
            hitTargets.Add(target);
    }

    private static void SetHitbox(BoxCollider2D hitbox, bool active)
    {
        if (hitbox == null)
            return;
        hitbox.isTrigger = true;
        hitbox.enabled = active;
    }

    private void UpdateHitboxDirection()
    {
        if (hitboxRoot == null || spriteRenderer == null)
            return;
        Vector3 scale = hitboxRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (spriteRenderer.flipX ? -1f : 1f);
        hitboxRoot.localScale = scale;
    }
}
