using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(TrainingDummy))]
public class ZombieMovement : MonoBehaviour
{
    [Tooltip("비워 두면 씬의 플레이어를 자동으로 찾음")]
    [SerializeField] private Transform player;
    [Tooltip("초당 이동 거리")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [Tooltip("이 거리 안에 있는 플레이어를 추적")]
    [SerializeField, Min(0f)] private float detectionDistance = 6f;
    [Tooltip("플레이어와 가까워지면 멈추는 좌우 거리")]
    [SerializeField, Min(0f)] private float stopDistance = 0.85f;

    private Rigidbody2D body;
    private TrainingDummy health;
    private Animator animator;
    private SpriteRenderer visual;
    private Collider2D playerBody;
    private Collider2D groundBody;
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Walk = Animator.StringToHash("Walk");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<TrainingDummy>();
        animator = GetComponentInChildren<Animator>();
        visual = animator.GetComponent<SpriteRenderer>();
        foreach (Collider2D shape in GetComponentsInChildren<Collider2D>())
            if (!shape.isTrigger) groundBody = shape;
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>()?.transform;
        if (player != null) playerBody = player.GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        float speed = 0f;
        // 지면 충돌은 유지하되 플레이어를 몸으로 밀지 않음
        if (groundBody != null && playerBody != null && playerBody.isActiveAndEnabled &&
            !Physics2D.GetIgnoreCollision(groundBody, playerBody))
            Physics2D.IgnoreCollision(groundBody, playerBody);
        // 피격과 사망 모션 중에는 추적 정지
        if (player != null && !health.IsDead && !health.IsPlayingHit)
        {
            Vector2 offset = (Vector2)player.position - body.worldCenterOfMass;
            if (offset.sqrMagnitude <= detectionDistance * detectionDistance)
            {
                if (Mathf.Abs(offset.x) > 0.01f)
                    visual.flipX = offset.x < 0f;
                if (Mathf.Abs(offset.x) > stopDistance)
                    speed = Mathf.Sign(offset.x) * moveSpeed;
            }
        }
        // 점프 없이 가로로만 이동하고 낙하는 중력에 맡김
        body.linearVelocity = new Vector2(speed, body.linearVelocity.y);
    }

    private void LateUpdate()
    {
        if (health.IsDead || health.IsPlayingHit || Time.timeScale <= 0f)
            return;

        // 벽에 막혀 실제로 움직이지 않으면 Idle 유지
        int state = Mathf.Abs(body.linearVelocity.x) > 0.05f ? Walk : Idle;
        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != state)
        {
            animator.Play(state, 0, 0f);
            animator.Update(0f);
        }
    }

    private void OnDisable()
    {
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
}
