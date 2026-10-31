using System;
using UnityEngine;

public enum RhythmStage { Low, Normal, High, Fever }
public enum RhythmHitKind { Basic, Enhanced, Note }

// 공격 처리보다 먼저 게임 시간 기준 자연 감소를 적용.
[DefaultExecutionOrder(-30)]
[DisallowMultipleComponent]
public class PlayerRhythmGauge : MonoBehaviour
{
    [Serializable]
    private struct StageSettings
    {
        [Min(0.01f)] public float attackSpeed;
        [Min(0f)] public float attackDamage;
        [Min(0f)] public float decayPerSecond;

        public StageSettings(float speed, float damage, float decay)
        {
            attackSpeed = speed;
            attackDamage = damage;
            decayPerSecond = decay;
        }
    }

    public const float MaximumGauge = 1000f;
    public const float StageWidth = 250f;

    [Header("Stage Settings")]
    [SerializeField] private StageSettings low = new StageSettings(1f, 1f, 12.5f);
    [SerializeField] private StageSettings normal = new StageSettings(1.25f, 1f, 25f);
    [SerializeField] private StageSettings high = new StageSettings(1.5f, 1.25f, 50f);
    [SerializeField] private StageSettings fever = new StageSettings(2f, 1.5f, 100f);

    [Header("Confirmed Damage Rewards")]
    [SerializeField, Min(0f)] private float basicHitGain = 30f;
    [SerializeField, Min(0f)] private float enhancedHitMultiplier = 2f;
    [SerializeField, Min(0f)] private float noteHitMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float bossHitLoss = 100f;

    private float currentGauge;

    public event Action Changed;
    public float CurrentGauge => currentGauge;
    public float NormalizedGauge => currentGauge / MaximumGauge;
    public RhythmStage Stage => (RhythmStage)Mathf.Min(3, (int)(currentGauge / StageWidth));
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, Settings(Stage).attackSpeed);
    public float AttackDamageMultiplier => Mathf.Max(0f, Settings(Stage).attackDamage);
    public float DecayPerSecond => Mathf.Max(0f, Settings(Stage).decayPerSecond);
    public uint LifeVersion { get; private set; }

    private void OnEnable() => ResetGauge();
    private void OnDisable() => ResetGauge();

    private void FixedUpdate()
    {
        // FixedUpdate도 히트스탑과 메뉴 정지 중에는 호출되지 않음.
        if (Time.timeScale > 0f) Advance(Time.fixedDeltaTime);
    }

    // 피해 적용에 성공한 호출 지점에서 대상별로 한 번만 호출.
    public void RecordConfirmedDamage(RhythmHitKind kind, float appliedDamage)
    {
        if (!isActiveAndEnabled || !PositiveFinite(appliedDamage)) return;
        float multiplier;
        switch (kind)
        {
            case RhythmHitKind.Basic: multiplier = 1f; break;
            case RhythmHitKind.Enhanced: multiplier = enhancedHitMultiplier; break;
            case RhythmHitKind.Note: multiplier = noteHitMultiplier; break;
            default: return;
        }
        float gain = basicHitGain * multiplier;
        if (PositiveFinite(gain)) SetGauge(currentGauge + gain);
    }

    // 향후 플레이어 체력 시스템이 보스 피해를 실제로 수락한 뒤 1회 호출.
    // 회피·무적으로 거절된 피해에는 호출하지 않음.
    public void NotifyBossDamageTaken(float appliedDamage)
    {
        if (!isActiveAndEnabled || !PositiveFinite(appliedDamage)) return;
        if (PositiveFinite(bossHitLoss)) SetGauge(currentGauge - bossHitLoss);
    }

    // 사망·재시작 경계에서 호출. 이전 생명의 투사체 보상도 무효화.
    public void ResetGauge()
    {
        unchecked { LifeVersion++; }
        SetGauge(0f);
    }

    private StageSettings Settings(RhythmStage stage)
    {
        switch (stage)
        {
            case RhythmStage.Normal: return normal;
            case RhythmStage.High: return high;
            case RhythmStage.Fever: return fever;
            default: return low;
        }
    }

    private void Advance(float seconds)
    {
        if (!PositiveFinite(seconds) || currentGauge <= 0f) return;
        float gauge = currentGauge;
        float remaining = seconds;
        // 경계에 도달한 뒤 남은 시간에는 아래 단계의 감소량을 사용.
        // 최대 네 구간만 통과하므로 큰 프레임 간격에도 반복 횟수는 제한됨.
        while (remaining > 0f && gauge > 0f)
        {
            int index = Mathf.Min(3, (int)(gauge / StageWidth));
            if (index > 0 && gauge <= index * StageWidth) index--;
            float decay = Settings((RhythmStage)index).decayPerSecond;
            if (!PositiveFinite(decay)) break;
            float boundary = index * StageWidth;
            float untilBoundary = (gauge - boundary) / decay;
            if (remaining < untilBoundary)
            {
                gauge -= decay * remaining;
                break;
            }
            gauge = boundary;
            remaining = Mathf.Max(0f, remaining - untilBoundary);
        }
        SetGauge(gauge);
    }

    private void SetGauge(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        float clamped = Mathf.Clamp(value, 0f, MaximumGauge);
        if (clamped == currentGauge) return;
        currentGauge = clamped;
        Changed?.Invoke();
    }

    private static bool PositiveFinite(float value) =>
        value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
}
