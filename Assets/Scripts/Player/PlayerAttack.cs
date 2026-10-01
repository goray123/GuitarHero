using UnityEngine;
using UnityEngine.InputSystem;

// 같은 물리 프레임에서 공격 여부를 먼저 결정
[DefaultExecutionOrder(-10)]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Attack Timing (Seconds)")]
    [Tooltip("1타 시작부터 종료까지의 시간")]
    [SerializeField, Min(0.01f)] private float attack1Duration = 0.375f;
    [Tooltip("2타 시작부터 종료까지의 시간")]
    [SerializeField, Min(0.01f)] private float attack2Duration = 0.625f;
    [Tooltip("3타 시작부터 종료까지의 시간")]
    [SerializeField, Min(0.01f)] private float attack3Duration = 0.25f;
    [Tooltip("각 타격 종료 후 다음 타격을 이어갈 수 있는 시간")]
    [SerializeField, Min(0f)] private float comboResetTime = 1f;

    [Header("Third Attack Fall")]
    [Tooltip("3타 모션 중 하강을 시작할 지점. 0.6이면 60% 재생 후 시작")]
    [SerializeField, Range(0f, 1f)] private float thirdAttackFallStartRatio = 0.6f;
    [Tooltip("공중 3타 중 수직으로 내려가는 속도. 클수록 빠르게 낙하")]
    [SerializeField, Min(0.01f)] private float thirdAttackFallSpeed = 20f;

    [Header("Attack Hitboxes")]
    [Tooltip("바라보는 방향에 맞춰 좌우 반전할 히트박스 부모")]
    [SerializeField] private Transform hitboxRoot;
    [SerializeField] private BoxCollider2D attack1Hitbox;
    [SerializeField] private BoxCollider2D attack2Hitbox;
    [SerializeField] private BoxCollider2D attack3Hitbox;

    private PlayerMovement movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private InputActionMap playerActions;
    private InputAction attackAction;
    private readonly float[] clipDurations = { 0.375f, 0.625f, 0.25f };
    private bool attackRequested;
    private bool attackQueued;
    private bool isAttacking;
    private int comboStep;
    private float attackEndTime;
    private float thirdAttackFallStartTime;
    private float comboResetAt;

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

        // 이동 입력과 별개로 공격 입력만 관리
        playerActions = inputActions.FindActionMap("Player", true).Clone();
        attackAction = playerActions.FindAction("Attack", true);

        // 원본 클립 길이를 기준으로 공격 재생 속도 계산
        if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                for (int i = 0; i < clipDurations.Length; i++)
                {
                    if (clip.name == "PlayerAttack" + (i + 1))
                        clipDurations[i] = clip.length;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (attackAction == null)
            return;

        attackAction.performed += OnAttack;
        attackAction.Enable();
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
            attackAction.Disable();
        }

        attackRequested = false;
        attackQueued = false;
        isAttacking = false;
        comboStep = 0;
        UpdateHitboxes();
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void OnDestroy()
    {
        playerActions?.Dispose();
        playerActions = null;
        attackAction = null;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (IsSliding)
            return;

        // 공격 중 입력은 다음 타격 하나만 예약
        if (isAttacking)
        {
            if (comboStep < 3)
                attackQueued = true;
        }
        else
        {
            attackRequested = true;
        }
    }

    private void FixedUpdate()
    {
        UpdateAttack(Time.time);
    }

    private void UpdateAttack(float currentTime)
    {
        if (isAttacking && currentTime >= attackEndTime)
        {
            isAttacking = false;
            comboResetAt = attackEndTime + Mathf.Max(0f, comboResetTime);

            if (attackQueued && comboStep < 3)
                StartAttack(currentTime);
        }

        // 타격 종료 후 설정 시간이 지나면 연속타 초기화
        if (!isAttacking && currentTime >= comboResetAt)
            comboStep = 0;

        if (attackRequested && !isAttacking && !IsSliding)
            StartAttack(currentTime);

        attackRequested = false;
        animator.SetBool("IsAttacking", isAttacking);
        UpdateHitboxes();
    }

    private void LateUpdate()
    {
        UpdateHitboxDirection();
    }

    // 현재 타격의 판정만 켜고 종료 시 모두 끔
    private void UpdateHitboxes()
    {
        SetHitbox(attack1Hitbox, isAttacking && comboStep == 1);
        SetHitbox(attack2Hitbox, isAttacking && comboStep == 2);
        SetHitbox(attack3Hitbox, isAttacking && comboStep == 3);
        UpdateHitboxDirection();
    }

    private static void SetHitbox(BoxCollider2D hitbox, bool active)
    {
        if (hitbox == null)
            return;

        // 바닥이나 적을 밀지 않는 공격 판정용 트리거
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

    // 휘두르는 모션이 나온 뒤 공중 3타의 수직 낙하 시작
    public bool TryGetFallVelocity(bool isGrounded, float currentTime, out Vector2 velocity)
    {
        velocity = Vector2.zero;
        if (!isAttacking || comboStep != 3 || isGrounded || currentTime < thirdAttackFallStartTime)
            return false;

        velocity = new Vector2(0f, -Mathf.Max(0.01f, thirdAttackFallSpeed));
        return true;
    }

    private void StartAttack(float currentTime)
    {
        comboStep = comboStep >= 3 ? 1 : comboStep + 1;
        isAttacking = true;
        attackQueued = false;
        movement?.ClearSlideRequest();

        float duration = comboStep == 1 ? attack1Duration :
            comboStep == 2 ? attack2Duration : attack3Duration;
        duration = Mathf.Max(0.01f, duration);
        attackEndTime = currentTime + duration;
        // 공격 시간이 바뀌어도 같은 모션 지점에서 하강
        thirdAttackFallStartTime = currentTime + duration * Mathf.Clamp01(thirdAttackFallStartRatio);

        // 공격 시간에 맞춰 해당 공격 애니메이션만 속도 조절
        animator.SetFloat("AttackSpeed", clipDurations[comboStep - 1] / duration);
        animator.SetBool("IsAttacking", true);
        animator.Play("Base Layer.PlayerAttack" + comboStep, 0, 0f);
        UpdateHitboxes();
    }
}
