using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpSpeed = 10f;

    [SerializeField] private float slideSpeed = 12f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideCooldown = 2f;
    [SerializeField] private float slideColliderHeight = 0.6f;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private BoxCollider2D bodyCollider;

    private Vector2 originalSize;
    private Vector2 originalOffset;


    private float moveInput;
    private bool jumpRequested;
    private bool slideRequested;

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
    }

    private void Update()
    {
        moveInput = 0f;

        Keyboard keyboard = Keyboard.current;
        if(keyboard == null)
            return;

        // 플레이어 좌우 이동
        if (keyboard.aKey.isPressed)
            moveInput -= 1f;

        if (keyboard.dKey.isPressed)
            moveInput += 1f;

        // 슬라이드 중 방향 전환과 행동 입력 방지
        if (!isSliding)
        {
            // 이동 중 방향 전환
            if (moveInput != 0f)
            {
                spriteRenderer.flipX = moveInput < 0f;
            }
            // 플레이어 점프
            if (keyboard.wKey.wasPressedThisFrame)
                jumpRequested = true;
            // 플레이어 슬라이드
            if (keyboard.sKey.wasPressedThisFrame)
                slideRequested = true;
        }
        

        animator.SetBool("IsMoving", moveInput != 0f);
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        bool isGrounded = IsGrounded() && velocity.y <= 0.1f;

        if (isSliding && (Time.time >= slideEndTime || !isGrounded))
        {
            isSliding = false;
            bodyCollider.size = originalSize;
            bodyCollider.offset = originalOffset;

            // 슬라이딩 쿨타임 동안 사용불가
            nextSlideTime = Time.time + slideCooldown;
        }

        if (!isSliding && isGrounded)
        {
            // W와 S를 동시에 누르면 점프를 우선
            if (jumpRequested)
            {
                velocity.y = jumpSpeed;
                isGrounded = false;
            }
            else if (slideRequested && Time.time >= nextSlideTime)
            {
                isSliding = true;

                // 슬라이드 시작할 때 바라보는 방향 저장
                slideDirection = spriteRenderer.flipX ? -1f : 1f;
                slideEndTime = Time.time + slideDuration;

                // 슬라이드 시 콜라이더 크기 변경
                float height = Mathf.Clamp(
                    slideColliderHeight, 0.01f, originalSize.y
                );

                bodyCollider.size = new Vector2(originalSize.x, height);

                bodyCollider.offset = originalOffset
                    - new Vector2(0f, (originalSize.y - height) * 0.5f);
            }
        }

        if (isSliding)
            velocity.x = slideDirection * slideSpeed;
        else
            velocity.x = moveInput * moveSpeed;

        rb.linearVelocity = velocity;

        jumpRequested = false;
        slideRequested = false;

        animator.SetBool("IsMoving", moveInput != 0f);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsSliding", isSliding);
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
