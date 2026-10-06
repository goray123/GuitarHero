using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class TrainingDummy : Damageable
{
    [Header("Health")]
    [Tooltip("더미 최대 체력")]
    [SerializeField, Min(1f)] private float maxHealth = 50f;
    [Tooltip("체력이 0이 된 뒤 재생성까지 걸리는 시간")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBar;
    [Tooltip("빨간 배경 위에서 남은 체력만큼 표시할 초록 바")]
    [SerializeField] private Transform healthFill;

    [Header("Animation")]
    [SerializeField] private AnimationClip hitAnimation;
    [SerializeField] private AnimationClip deathAnimation;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D hurtbox;
    private Vector3 fullBarScale;
    private Vector3 spawnPosition;
    private float hitEndsAt;
    private float deathEndsAt;
    private float respawnAt;
    private bool playingHit;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        hurtbox = GetComponent<BoxCollider2D>();
        fullBarScale = healthFill != null ? healthFill.localScale : Vector3.one;
        spawnPosition = transform.position;
    }

    private void OnEnable() => Respawn();

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0f || float.IsNaN(damage))
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

    private void Update() => UpdateState(Time.time);

    private void UpdateState(float currentTime)
    {
        if (IsDead)
        {
            if (currentTime >= respawnAt)
                Respawn();
            else if (currentTime >= deathEndsAt)
            {
                spriteRenderer.enabled = false;
                if (healthBar != null) healthBar.SetActive(false);
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
        CurrentHealth = MaxHealth;
        IsDead = false;
        playingHit = false;
        spriteRenderer.enabled = true;
        hurtbox.enabled = true;
        if (healthBar != null) healthBar.SetActive(true);
        UpdateHealthBar();
        animator.Play("Idle", 0, 0f);
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null) return;
        Vector3 scale = fullBarScale;
        scale.x *= Mathf.Clamp01(CurrentHealth / MaxHealth);
        healthFill.localScale = scale;
    }
}
