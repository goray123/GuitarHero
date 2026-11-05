using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(BoxCollider2D))]
public sealed class PlayerHealth : Damageable
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float invulnerabilityTime = 0.2f;
    private PlayerRhythmGauge rhythm;
    private SpriteRenderer visual;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerChordStrike skill;
    private Animator animator;
    private Rigidbody2D body;
    private float invulnerableUntil;
    private Color restColor;
    private bool restoreMovement, restoreAttack, restoreSkill, restoreAnimator;

    public event Action Changed;
    public event Action Died;
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get { return Mathf.Max(1f, maxHealth); } }
    public bool IsDead { get { return CurrentHealth <= 0f; } }

    private void Awake()
    {
        rhythm = GetComponent<PlayerRhythmGauge>();
        visual = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        skill = GetComponent<PlayerChordStrike>();
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        restColor = visual != null ? visual.color : Color.white;
    }

    private void OnEnable() { ResetHealth(); }

    public void ResetHealth()
    {
        bool wasDead = IsDead;
        CurrentHealth = MaxHealth;
        invulnerableUntil = float.NegativeInfinity;
        if (visual != null) visual.color = restColor;
        if (wasDead)
        {
            if (movement != null && restoreMovement) movement.enabled = true;
            if (attack != null && restoreAttack) attack.enabled = true;
            if (skill != null && restoreSkill) skill.enabled = true;
            if (animator != null && restoreAnimator) animator.enabled = true;
        }
        if (Changed != null) Changed();
    }

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || Time.time < invulnerableUntil ||
            !Boss1Tuning.Finite(damage) || damage <= 0f) return false;
        float applied = Mathf.Min(CurrentHealth, damage);
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        invulnerableUntil = Time.time + Mathf.Max(0f, invulnerabilityTime);
        // Notify only after accepted damage, never for a rejected overlap.
        if (rhythm != null) rhythm.NotifyBossDamageTaken(applied);
        if (Changed != null) Changed();
        if (IsDead) Die();
        return true;
    }

    private void Die()
    {
        restoreMovement = movement != null && movement.enabled;
        restoreAttack = attack != null && attack.enabled;
        restoreSkill = skill != null && skill.enabled;
        restoreAnimator = animator != null && animator.enabled;
        if (attack != null) attack.enabled = false;
        if (skill != null) skill.enabled = false;
        if (movement != null) movement.enabled = false;
        if (animator != null) animator.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (rhythm != null) rhythm.ResetGauge();
        if (Died != null) Died();
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        Color color = IsDead ? new Color(0.45f, 0.45f, 0.5f, 1f) : restColor;
        if (!IsDead && Time.time < invulnerableUntil)
            color.a *= 0.35f + 0.65f * Mathf.PingPong(Time.time * 20f, 1f);
        visual.color = color;
    }
}

