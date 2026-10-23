using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerHitStop : MonoBehaviour
{
    [Tooltip("1, 2타 명중 시 정지 시간. 0이면 사용 안 함")]
    [SerializeField, Min(0f)] private float basicDuration = 0.035f;
    [Tooltip("3타 명중 시 정지 시간. 0이면 사용 안 함")]
    [SerializeField, Min(0f)] private float thirdDuration = 0.085f;

    private PlayerAttack attack;
    private bool isStopped;
    private float previousTimeScale;
    private double resumeAt;

    public bool IsStopped => isStopped;

    private void Awake() => attack = GetComponent<PlayerAttack>();
    private void OnEnable() => attack.HitConfirmed += OnHit;

    private void OnDisable()
    {
        attack.HitConfirmed -= OnHit;
        RestoreTime();
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep)
    {
        float duration = comboStep == 3 ? thirdDuration : basicDuration;
        // 정지 중 추가 명중은 무시해 여러 적을 맞혀도 시간 누적 방지
        if (isStopped || Time.timeScale <= 0f || duration <= 0f ||
            float.IsNaN(duration) || float.IsInfinity(duration))
            return;

        previousTimeScale = Time.timeScale;
        resumeAt = Time.realtimeSinceStartupAsDouble + duration;
        isStopped = true;
        Time.timeScale = 0f;
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
        if (Time.timeScale == 0f)
            Time.timeScale = previousTimeScale;
    }
}
