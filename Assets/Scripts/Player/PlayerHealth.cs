using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : Damageable
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [Header("Damage Protection")]
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 1f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsInvulnerable => Time.unscaledTimeAsDouble < invulnerableUntil;

    public event System.Action<AttackHit> Damaged;

    public event System.Action HealthChanged;

    private Collider2D bodyCollider;
    private double invulnerableUntil;

    private void Awake() => bodyCollider = GetComponent<Collider2D>();

    private void OnEnable() => ResetHealth();

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        invulnerableUntil = 0d;
        HealthChanged?.Invoke();
    }

    public override bool TryTakeDamage(float damage)
    {
        Vector2 point = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        return TryTakeDamage(new AttackHit(damage, point, Vector2.zero, 0));
    }

    public override bool TryTakeDamage(AttackHit hit)
    {
        float damage = hit.Damage;
        if (!isActiveAndEnabled || IsDead || IsInvulnerable || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            return false;
        float remainingHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (remainingHealth == CurrentHealth) return false;
        CurrentHealth = remainingHealth;
        // Set protection before publishing the hit so simultaneous contacts cannot deal damage twice.
        invulnerableUntil = Time.unscaledTimeAsDouble + Mathf.Max(0f, invulnerabilityDuration);
        HealthChanged?.Invoke();
        Damaged?.Invoke(hit);
        return true;
    }
}
