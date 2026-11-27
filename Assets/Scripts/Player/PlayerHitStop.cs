using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerHitStop : MonoBehaviour
{
    [Tooltip("1, 2타 명중 시 정지 시간. 0이면 사용 안 함")]
    [SerializeField, Min(0f)] private float basicDuration = 0.035f;
    [Tooltip("3타 명중 시 정지 시간. 0이면 사용 안 함")]
    [SerializeField, Min(0f)] private float thirdDuration = 0.085f;
    [Header("Player Damage")]
    [SerializeField, Min(0f)] private float damageDuration = 0.05f;

    private PlayerAttack attack;
    private PlayerHealth health;
    private bool isStopped;
    private bool slowing;
    private float previousTimeScale;
    private double resumeAt;

    public bool IsStopped => isStopped && !slowing;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        attack.HitConfirmed += OnHit;
        if (health != null) health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        attack.HitConfirmed -= OnHit;
        if (health != null) health.Damaged -= OnDamaged;
        RestoreTime();
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep) =>
        StopFor(comboStep == 3 ? thirdDuration : basicDuration);

    private void OnDamaged(AttackHit hit) => StopFor(damageDuration, true);

    public void StopFor(float duration, bool extend = false)
    {
        // 정지 중 추가 명중은 무시해 여러 적을 맞혀도 시간 누적 방지
        if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            return;

        if (isStopped)
        {
            if (slowing)
            {
                slowing = false;
                resumeAt = Time.realtimeSinceStartupAsDouble + duration;
                Time.timeScale = 0f;
                return;
            }
            // Damage can extend an attack's hitstop without taking a second time-scale lock.
            if (extend) resumeAt = System.Math.Max(resumeAt, Time.realtimeSinceStartupAsDouble + duration);
            return;
        }
        if (Time.timeScale <= 0f) return;

        previousTimeScale = Time.timeScale;
        resumeAt = Time.realtimeSinceStartupAsDouble + duration;
        isStopped = true;
        Time.timeScale = 0f;
    }

    public void SlowFor(float duration, float scale)
    {
        if (isStopped || Time.timeScale <= 0f || duration <= 0f || scale <= 0f || scale >= 1f ||
            float.IsNaN(duration) || float.IsInfinity(duration) || float.IsNaN(scale)) return;
        previousTimeScale = Time.timeScale;
        resumeAt = Time.realtimeSinceStartupAsDouble + duration;
        isStopped = slowing = true;
        Time.timeScale = previousTimeScale * scale;
    }

    public void EndSlowMotion()
    {
        if (slowing) RestoreTime();
    }

    private void Update()
    {
        // 게임 시간이 멈춰 있어도 실제 시간으로 복구
        if (isStopped && Time.realtimeSinceStartupAsDouble >= resumeAt)
            RestoreTime();
    }

    private void RestoreTime()
    {
        if (!isStopped) return;
        isStopped = false;
        // 비활성화나 씬 전환 시 정지 상태가 남지 않도록 복구
        if (Time.timeScale == 0f || slowing)
            Time.timeScale = previousTimeScale;
        slowing = false;
    }
}
