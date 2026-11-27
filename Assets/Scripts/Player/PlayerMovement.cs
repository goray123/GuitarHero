using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float moveSpeed = 4.8f;
    [Tooltip("기본 공격 1, 2타 중 좌우 이동 속도 배율")]
    [SerializeField, Range(0f, 1f)] private float basicAttackMoveSpeedMultiplier = 0.7f;
    [SerializeField, Min(0.01f)] private float minimumJumpHeight = 1.4f;
    [SerializeField, Min(0.01f)] private float maximumJumpHeight = 2.4f;
    private const float MinimumHoldTime = 0.05f;
    private const float MaximumHoldTime = 0.25f;
    [Tooltip("같은 높이에서 상승 속도 대비 하강 속도 비율")]
    [SerializeField, Min(1f)] private float fallSpeedMultiplier = 1.3f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("슬라이드 한 번에 이동할 거리. 벽에 막히면 실제 이동 거리는 줄어듦")]
    [SerializeField, Min(0f)] private float slideDistance = 6f;
    [Tooltip("슬라이드 지속 시간. 거리와 시간에 맞춰 속도 자동 계산")]
    [SerializeField, Min(0.01f)] private float slideDuration = 0.5f;
    [SerializeField] private float slideCooldown = 2f;
    [SerializeField] private float slideColliderHeight = 0.6f;
    [SerializeField] private bool showSlideCooldown = true;

    [Tooltip("앉았을 때 몸체 콜라이더 높이")]
    [SerializeField, Min(0.01f)] private float crouchColliderHeight = 0.8f;

    private GUIStyle cooldownLabelStyle;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private BoxCollider2D bodyCollider;

    private InputActionMap playerActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private InputAction crouchAction;
    private PlayerAttack attack;
    private PlayerChordStrike chordStrike;
    private PlayerGuitarSpin guitarSpin;

    public bool IsSliding => isSliding;
    // Match the input gate: the cooldown countdown begins when the slide ends.
    public float SlideCooldownFraction => isSliding ? 1f :
        slideCooldown > 0f ? Mathf.Clamp01((nextSlideTime - Time.time) / slideCooldown) : 0f;
    public bool IsCrouching => isCrouching;
    public bool IsOnGround => rb != null && IsGrounded() && rb.linearVelocity.y <= 0.1f;
    private bool IsAttacking => attack != null && attack.IsAttacking;
    private bool IsUsingSkill => (chordStrike != null && chordStrike.BlocksActions) || IsSpinning;
    private bool IsSpinning => guitarSpin != null && guitarSpin.BlocksActions;

    private Vector2 originalSize;
    private Vector2 originalOffset;
    public float StandingColliderHeight => (originalSize.y > 0f ? originalSize.y : GetComponent<BoxCollider2D>().size.y) * Mathf.Abs(transform.lossyScale.y);
    public float SlideColliderHeight => Mathf.Min(slideColliderHeight * Mathf.Abs(transform.lossyScale.y), StandingColliderHeight);


    private float moveInput;
    private bool jumpRequested;
    private bool jumpCutRequested;
    private bool isJumping;
    private float releasedHoldTime, jumpStartY;
    private bool slideRequested;
    private float lastGroundedTime = float.NegativeInfinity;

    private bool isSliding;
    private bool isCrouching;
    private readonly RaycastHit2D[] standingHits = new RaycastHit2D[8];
    private float slideDirection;
    private float currentSlideSpeed;
    private float slideEndTime;
    private float nextSlideTime;


    private readonly List<ContactPoint2D> groundContacts = 
        new List<ContactPoint2D>(16);

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        chordStrike = GetComponent<PlayerChordStrike>();
        guitarSpin = GetComponent<PlayerGuitarSpin>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<BoxCollider2D>();
        originalSize = bodyCollider.size;
        originalOffset = bodyCollider.offset;

        if (inputActions == null)
        {
            Debug.LogError("PlayerMovement의 Input Actions를 연결해주세요.", this);
            enabled = false;
            return;
        }

        // 다른 오브젝트의 입력 상태에 영향을 주지 않도록 복제
        playerActions = inputActions.FindActionMap("Player", true).Clone();
        moveAction = playerActions.FindAction("Move", true);
        jumpAction = playerActions.FindAction("Jump", true);
        slideAction = playerActions.FindAction("Slide", true);
        crouchAction = playerActions.FindAction("Crouch", true);
    }

    private void OnEnable()
    {
        if (playerActions == null)
            return;

        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJumpReleased;
        slideAction.performed += OnSlide;
        slideAction.canceled += OnSlideReleased;

        moveAction.Enable();
        jumpAction.Enable();
        slideAction.Enable();
        crouchAction.Enable();
    }

    private void OnDisable()
    {
        if (playerActions != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.canceled -= OnJumpReleased;
            slideAction.performed -= OnSlide;
            slideAction.canceled -= OnSlideReleased;
            playerActions.Disable();
        }

        moveInput = 0f;
        jumpRequested = false;
        jumpCutRequested = false;
        isJumping = false;
        slideRequested = false;
        lastGroundedTime = float.NegativeInfinity;

        if (isCrouching)
        {
            isCrouching = false;
            RestoreCollider();
            animator.SetBool("IsCrouching", false);
        }

        if (isSliding)
        {
            isSliding = false;
            attack?.ResumeComboAfterSlide(Time.time);
            RestoreCollider();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void OnDestroy()
    {
        playerActions?.Dispose();
        playerActions = null;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!isSliding && !IsAttacking && !IsUsingSkill)
            jumpRequested = true;
    }

    private void OnSlide(InputAction.CallbackContext context)
    {
        // 공중에서 누른 입력은 키를 누르고 있는 동안만 보관
        if (!isSliding && !IsUsingSkill)
            slideRequested = true;
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        if (!jumpCutRequested) releasedHoldTime = (float)context.duration;
        jumpCutRequested = true;
    }

    private void OnSlideReleased(InputAction.CallbackContext context)
    {
        // 착지 전에 키를 떼면 예약 취소, 이미 시작한 슬라이드는 유지
        slideRequested = false;
    }

    private void Update()
    {
        if (playerActions == null)
            return;

        // Input Action으로 좌우 이동 입력 확인
        moveInput = moveAction.ReadValue<Vector2>().x;

        // 공격 중에는 바라보는 방향 고정
        if (!isSliding && !IsAttacking && !IsUsingSkill && moveInput != 0f)
            spriteRenderer.flipX = moveInput < 0f;
    }

    private void FixedUpdate()
    {
        UpdateMovement(Time.time);
    }

    private void OnGUI()
    {
        if (!showSlideCooldown)
            return;

        if (cooldownLabelStyle == null)
        {
            cooldownLabelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            cooldownLabelStyle.normal.textColor = Color.white;
        }

        // 화면 오른쪽 위에 남은 쿨타임을 소수점 한 자리로 표시
        float remainingCooldown = Mathf.Max(0f, nextSlideTime - Time.time);
        Rect labelRect = new Rect(Screen.width - 276f, 16f, 260f, 44f);
        GUI.Box(labelRect, $"Slide Cooldown: {remainingCooldown:F1}s", cooldownLabelStyle);
    }

    private float SpeedForHeight(float height)
    {
        if (height <= 0f) return 0f;
        float gravity = -Physics2D.gravity.y * rb.gravityScale;
        // Box2D applies gravity before moving each fixed step.
        return Mathf.Sqrt(2f * gravity * height) + 0.5f * gravity * Time.fixedDeltaTime;
    }

    private void UpdateMovement(float currentTime)
    {
        // 음표 스킬 중에는 입력 대신 스킬의 반동 또는 제자리 상승 적용
        if (chordStrike != null && chordStrike.BlocksActions)
        {
            ClearActionRequests();
            UpdateCrouch(false);
            lastGroundedTime = float.NegativeInfinity;
            rb.linearVelocity = chordStrike.GetRecoilVelocity(currentTime, Time.fixedDeltaTime);
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsGrounded", IsOnGround);
            animator.SetBool("IsSliding", false);
            return;
        }
        if (IsSpinning)
        {
            ClearActionRequests();
            UpdateCrouch(false);
            lastGroundedTime = float.NegativeInfinity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed * guitarSpin.MoveSpeedMultiplier, rb.linearVelocity.y);
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsGrounded", IsOnGround);
            animator.SetBool("IsSliding", false);
            return;
        }
        Vector2 velocity = rb.linearVelocity;
        bool isGrounded = IsGrounded() && velocity.y <= 0.1f;
        if (isGrounded || velocity.y <= 0f)
            isJumping = false;

        // 마지막 접지 시각으로 절벽 이탈 후 0.1초 판정
        if (isGrounded)
            lastGroundedTime = currentTime;

        bool canUseGroundAction = isGrounded ||
            currentTime - lastGroundedTime <= coyoteTime;

        // 공중에서도 시전 시간이 끝날 때까지 슬라이드 유지
        bool slideEnded = isSliding && currentTime >= slideEndTime;
        if (slideEnded)
        {
            isSliding = false;
            attack?.ResumeComboAfterSlide(currentTime);
            RestoreCollider();

            // 슬라이딩 쿨타임 동안 사용불가
            nextSlideTime = currentTime + slideCooldown;
        }

        if (!isSliding && !slideEnded && canUseGroundAction)
        {
            // 점프와 슬라이드가 동시에 들어오면 점프를 우선
            if (!IsAttacking && jumpRequested)
            {
                jumpStartY = rb.position.y;
                velocity.y = SpeedForHeight(Mathf.Max(minimumJumpHeight, maximumJumpHeight));
                isJumping = true;
                isGrounded = false;

                // 점프에 사용한 코요테 타임과 동시 슬라이드 입력 소모
                lastGroundedTime = float.NegativeInfinity;
                slideRequested = false;
            }
            else if (slideRequested && currentTime >= nextSlideTime &&
                (!IsAttacking || attack.CanCancelWithSlide(currentTime)))
            {
                if (IsAttacking)
                    attack.CancelForSlide();
                StartSlide(currentTime);
            }
        }

        if (isJumping && (jumpCutRequested || !jumpAction.IsPressed()))
        {
            float hold = Mathf.InverseLerp(MinimumHoldTime, MaximumHoldTime, releasedHoldTime);
            float height = Mathf.Lerp(minimumJumpHeight, Mathf.Max(minimumJumpHeight, maximumJumpHeight), hold);
            float remaining = height - (rb.position.y - jumpStartY);
            velocity.y = Mathf.Min(velocity.y, SpeedForHeight(remaining));
            isJumping = false;
        }

        if (!isGrounded && velocity.y < 0f)
            velocity.y += Physics2D.gravity.y * rb.gravityScale *
                (fallSpeedMultiplier * fallSpeedMultiplier - 1f) * Time.fixedDeltaTime;

        UpdateCrouch(isGrounded && !isSliding && !IsAttacking && crouchAction.IsPressed());

        if (isSliding)
            // 마지막 물리 프레임에서는 남은 시간만큼만 이동
            velocity.x = slideDirection * currentSlideSpeed *
                Mathf.Clamp01((slideEndTime - currentTime) / Time.fixedDeltaTime);
        else if (slideEnded || isCrouching)
            velocity.x = 0f;
        else
        {
            float speedMultiplier = attack != null && attack.IsBasicComboAttack
                ? basicAttackMoveSpeedMultiplier : 1f;
            velocity.x = moveInput * moveSpeed * speedMultiplier;
        }

        // 공격의 전진과 포물선 이동을 일반 조작보다 우선 적용
        if (IsAttacking)
        {
            velocity = attack.GetAttackVelocity(velocity, isGrounded, currentTime, Time.fixedDeltaTime, rb.gravityScale);
            if (velocity.y > 0.1f)
            {
                isGrounded = false;
                lastGroundedTime = float.NegativeInfinity;
            }
        }

        rb.linearVelocity = velocity;

        jumpRequested = false;
        jumpCutRequested = false;

        // 타격 활성 중 누르고 있는 슬라이드는 후딜레이까지 대기
        if (isSliding || (isGrounded && (!IsAttacking || attack.CanCancelWithSlide(currentTime))))
            slideRequested = false;

        animator.SetBool("IsMoving", Mathf.Abs(velocity.x) > 0.01f);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsSliding", isSliding);
    }

    // 공격 시작 시 대기 중인 슬라이드 입력 소모
    public void ClearSlideRequest()
    {
        slideRequested = false;
    }

    // 스킬 중 점프와 슬라이드 입력이 종료 뒤에 발동하지 않도록 소모
    public void ClearActionRequests()
    {
        jumpRequested = false;
        jumpCutRequested = false;
        isJumping = false;
        slideRequested = false;
    }

    private void StartSlide(float currentTime)
    {
        isSliding = true;
        slideRequested = false;
        lastGroundedTime = float.NegativeInfinity;

        // 슬라이드 키를 누르면 바라보는 방향으로 이동
        slideDirection = spriteRenderer.flipX ? -1f : 1f;
        float duration = Mathf.Max(0.01f, slideDuration);
        // 시작 시 거리와 시간을 기준으로 이번 슬라이드 속도 확정
        currentSlideSpeed = Mathf.Max(0f, slideDistance) / duration;
        slideEndTime = currentTime + duration;

        isCrouching = false;
        animator.SetBool("IsCrouching", false);
        SetColliderHeight(slideColliderHeight);
    }

    private void UpdateCrouch(bool crouching)
    {
        if (isCrouching == crouching)
            return;
        if (!crouching && !isSliding)
        {
            float distance = (originalSize.y - bodyCollider.size.y) * Mathf.Abs(transform.lossyScale.y);
            int count = bodyCollider.Cast(Vector2.up, standingHits, distance);
            for (int i = 0; i < count; i++)
                if (!standingHits[i].collider.isTrigger &&
                    !Physics2D.GetIgnoreCollision(bodyCollider, standingHits[i].collider) &&
                    standingHits[i].normal.y < -0.1f &&
                    standingHits[i].collider.bounds.max.y > bodyCollider.bounds.center.y)
                    return;
        }
        isCrouching = crouching;
        if (crouching)
            SetColliderHeight(crouchColliderHeight);
        else if (!isSliding)
            RestoreCollider();
        animator.SetBool("IsCrouching", isCrouching);
    }

    private void SetColliderHeight(float requestedHeight)
    {
        // 발바닥 위치를 유지하며 콜라이더 높이 변경
        float height = Mathf.Clamp(requestedHeight, 0.01f, originalSize.y);
        bodyCollider.size = new Vector2(originalSize.x, height);
        bodyCollider.offset = originalOffset
            - new Vector2(0f, (originalSize.y - height) * 0.5f);
    }

    private void RestoreCollider()
    {
        bodyCollider.size = originalSize;
        bodyCollider.offset = originalOffset;
    }

    private bool IsGrounded()
    {
        int contactCount = rb.GetContacts(groundContacts);

        for (int i = 0;i < contactCount;i++)
        {
            if(groundContacts[i].normal.y >= 0.7f)
            {
                return true;
            }
        }

        return false;
    }
}
