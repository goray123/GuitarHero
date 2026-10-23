using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrainingDummy : Damageable
{
    [Header("Health")]
    [Tooltip("더미 최대 체력")]
    [SerializeField, Min(1f)] private float maxHealth = 50f;
    [Tooltip("체력이 0이 된 뒤 재생성까지 걸리는 시간")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private HealthBarUI healthBarUI;

    [Header("Animation")]
    [SerializeField] private AnimationClip hitAnimation;
    [SerializeField] private AnimationClip deathAnimation;

    [Header("Anchored Hit Reaction")]
    [Tooltip("발밑 피벗. 판정과 체력바는 이 피벗 밖에 유지")]
    [SerializeField] private Transform reactionPivot;
    [SerializeField, Min(0f)] private float basicRecoilDistance = 0.08f;
    [SerializeField, Min(0f)] private float thirdRecoilDistance = 0.2f;
    [SerializeField, Range(0f, 30f)] private float basicBendAngle = 5f;
    [SerializeField, Range(0f, 30f)] private float thirdBendAngle = 13f;
    [SerializeField, Min(0.01f)] private float basicReactionTime = 0.22f;
    [SerializeField, Min(0.01f)] private float thirdReactionTime = 0.34f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D hurtbox;
    private Vector3 spawnPosition;
    private float hitEndsAt;
    private float deathEndsAt;
    private float respawnAt;
    private bool playingHit;
    private Vector3 restPivotPosition;
    private Quaternion restPivotRotation;
    private Vector3 restPivotScale;
    private float reactionElapsed;
    private float reactionDuration;
    private float recoilDistance;
    private float bendAngle;
    private Vector2 reactionDirection;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead { get; private set; }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = animator.GetComponent<SpriteRenderer>();
        hurtbox = GetComponent<BoxCollider2D>();
        spawnPosition = transform.position;
        if (reactionPivot != null)
        {
            restPivotPosition = reactionPivot.localPosition;
            restPivotRotation = reactionPivot.localRotation;
            restPivotScale = reactionPivot.localScale;
        }
    }

    private void OnEnable() => Respawn();

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        UpdateHealthBar();
        if (CurrentHealth <= 0f)
        {
            // 파괴 중에는 추가 피격 차단
            IsDead = true;
            playingHit = false;
            hurtbox.enabled = false;
            deathEndsAt = Time.time + (deathAnimation != null ? deathAnimation.length : 0f);
            respawnAt = Time.time + Mathf.Max(0f, respawnDelay);
            animator.Play("Death", 0, 0f);
        }
        else
        {
            playingHit = true;
            hitEndsAt = Time.time + (hitAnimation != null ? hitAnimation.length : 0f);
            animator.Play("Hit", 0, 0f);
        }
        return true;
    }

    public override bool TryTakeDamage(AttackHit hit)
    {
        if (!TryTakeDamage(hit.Damage)) return false;
        bool heavy = hit.ComboStep == 3;
        reactionDirection = hit.Direction;
        recoilDistance = heavy ? thirdRecoilDistance : basicRecoilDistance;
        bendAngle = heavy ? thirdBendAngle : basicBendAngle;
        reactionDuration = Mathf.Max(0.01f, heavy ? thirdReactionTime : basicReactionTime);
        reactionElapsed = 0f;
        // 충격 자세를 먼저 확정해 히트스탑 동안 그대로 유지.
        ApplyReaction();
        animator.Update(0f);
        return true;
    }

    private void Update()
    {
        UpdateState(Time.time);
        if (reactionDuration <= 0f || Time.timeScale <= 0f) return;
        reactionElapsed = Mathf.Min(reactionDuration, reactionElapsed + Time.deltaTime);
        ApplyReaction();
        if (reactionElapsed >= reactionDuration) reactionDuration = 0f;
    }

    private void ApplyReaction()
    {
        if (reactionPivot == null) return;
        float t = Mathf.Clamp01(reactionElapsed / Mathf.Max(0.01f, reactionDuration));
        // 첫 자세에서 최대 충격, 이후 작은 반동을 거쳐 원위치로 감쇠.
        float recoil = Mathf.Cos(t * Mathf.PI * 2.5f) * (1f - t) * (1f - t);
        bool downward = reactionDirection.y < -0.5f;
        float horizontal = downward ? 0f : Mathf.Sign(reactionDirection.x);
        reactionPivot.localPosition = restPivotPosition + Vector3.right * (horizontal * recoilDistance * recoil);
        reactionPivot.localRotation = restPivotRotation * Quaternion.Euler(0f, 0f, -horizontal * bendAngle * recoil);
        float compression = downward ? recoilDistance * recoil : 0f;
        reactionPivot.localScale = Vector3.Scale(restPivotScale, new Vector3(1f + compression * 0.45f, 1f - compression, 1f));
    }

    private void OnDisable() => ResetReaction();

    private void ResetReaction()
    {
        reactionDuration = 0f;
        if (reactionPivot == null) return;
        reactionPivot.localPosition = restPivotPosition;
        reactionPivot.localRotation = restPivotRotation;
        reactionPivot.localScale = restPivotScale;
    }

    private void UpdateState(float currentTime)
    {
        if (IsDead)
        {
            if (currentTime >= respawnAt)
                Respawn();
            else if (currentTime >= deathEndsAt)
            {
                spriteRenderer.enabled = false;
                // 치명타의 주황색 피해 바가 다 줄어든 뒤 숨김.
                if (healthBar != null && (healthBarUI == null || healthBarUI.IsSettled))
                    healthBar.SetActive(false);
            }
        }
        else if (playingHit && currentTime >= hitEndsAt)
        {
            playingHit = false;
            animator.Play("Idle", 0, 0f);
        }
    }

    // 원래 위치에서 체력과 표시 상태 복구
    private void Respawn()
    {
        transform.position = spawnPosition;
        ResetReaction();
        CurrentHealth = MaxHealth;
        IsDead = false;
        playingHit = false;
        spriteRenderer.enabled = true;
        hurtbox.enabled = true;
        if (healthBar != null) healthBar.SetActive(true);
        UpdateHealthBar(true);
        animator.Play("Idle", 0, 0f);
    }

    private void UpdateHealthBar(bool immediate = false)
    {
        if (healthBarUI != null) healthBarUI.SetHealth(CurrentHealth, MaxHealth, immediate);
    }
}
