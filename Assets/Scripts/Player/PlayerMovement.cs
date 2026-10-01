using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float coyoteTime = 0.1f;

    [SerializeField] private float slideSpeed = 12f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideCooldown = 2f;
    [SerializeField] private float slideColliderHeight = 0.6f;
    [SerializeField] private bool showSlideCooldown = true;

    private GUIStyle cooldownLabelStyle;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private BoxCollider2D bodyCollider;

    private InputActionMap playerActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction slideAction;

    private Vector2 originalSize;
    private Vector2 originalOffset;


    private float moveInput;
    private bool jumpRequested;
    private bool slideRequested;
    private float lastGroundedTime = float.NegativeInfinity;

    private bool isSliding;
    private float slideDirection;
    private float slideEndTime;
    private float nextSlideTime;

    private readonly List<ContactPoint2D> groundContacts = 
        new List<ContactPoint2D>(16);

    private void Awake()
    {
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
    }

    private void OnEnable()
    {
        if (playerActions == null)
            return;

        jumpAction.performed += OnJump;
        slideAction.performed += OnSlide;

        moveAction.Enable();
        jumpAction.Enable();
        slideAction.Enable();
    }

    private void OnDisable()
    {
        if (playerActions != null)
        {
            jumpAction.performed -= OnJump;
            slideAction.performed -= OnSlide;
            playerActions.Disable();
        }

        moveInput = 0f;
        jumpRequested = false;
        slideRequested = false;
        lastGroundedTime = float.NegativeInfinity;

        if (isSliding)
        {
            isSliding = false;
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
        if (!isSliding)
            jumpRequested = true;
    }

    private void OnSlide(InputAction.CallbackContext context)
    {
        // 공중에서 누른 입력은 키를 떼어도 착지까지 보관
        if (!isSliding)
            slideRequested = true;
    }

    private void Update()
    {
        if (playerActions == null)
            return;

        // Input Action으로 좌우 이동 입력 확인
        moveInput = moveAction.ReadValue<Vector2>().x;

        // 슬라이드 중에는 시작 방향 유지
        if (!isSliding && moveInput != 0f)
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

    private void UpdateMovement(float currentTime)
    {
        Vector2 velocity = rb.linearVelocity;
        bool isGrounded = IsGrounded() && velocity.y <= 0.1f;

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
            RestoreCollider();

            // 슬라이딩 쿨타임 동안 사용불가
            nextSlideTime = currentTime + slideCooldown;
        }

        if (!isSliding && !slideEnded && canUseGroundAction)
        {
            // W와 S를 동시에 누르면 점프를 우선
            if (jumpRequested)
            {
                velocity.y = jumpSpeed;
                isGrounded = false;

                // 점프에 사용한 코요테 타임과 동시 슬라이드 입력 소모
                lastGroundedTime = float.NegativeInfinity;
                slideRequested = false;
            }
            else if (slideRequested && currentTime >= nextSlideTime)
            {
                StartSlide(currentTime);
            }
        }

        if (isSliding)
            velocity.x = slideDirection * slideSpeed;
        else if (slideEnded)
            // 종료 순간에는 이동키를 누르고 있어도 가로 속도 초기화
            velocity.x = 0f;
        else
            velocity.x = moveInput * moveSpeed;

        rb.linearVelocity = velocity;

        jumpRequested = false;

        // 착지 시 한 번 처리하고 쿨타임 중 입력은 지움
        if (isGrounded || isSliding)
            slideRequested = false;

        animator.SetBool("IsMoving", Mathf.Abs(velocity.x) > 0.01f);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsSliding", isSliding);
    }

    private void StartSlide(float currentTime)
    {
        isSliding = true;
        slideRequested = false;
        lastGroundedTime = float.NegativeInfinity;

        // S만 눌러도 바라보는 방향으로 슬라이드
        slideDirection = spriteRenderer.flipX ? -1f : 1f;
        slideEndTime = currentTime + slideDuration;

        // 발바닥 위치를 유지하며 콜라이더 높이 변경
        float height = Mathf.Clamp(slideColliderHeight, 0.01f, originalSize.y);
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
