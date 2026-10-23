using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("앞쪽 초록색 바. 실제 체력을 즉시 표시")]
    [SerializeField] private Image currentFill;
    [Tooltip("뒤쪽 주황색 바. 피해 구간을 남겼다가 따라 감소")]
    [SerializeField] private Image damageFill;
    [SerializeField, Min(0f)] private float damageDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float damageDuration = 0.4f;

    private bool initialized;
    private float currentRatio = 1f;
    private float damageRatio = 1f;
    private float delayRemaining;
    private float tweenElapsed;
    private float tweenStart = 1f;

    public bool IsSettled => Mathf.Approximately(damageRatio, currentRatio);

    public void SetHealth(float current, float maximum, bool immediate = false)
    {
        float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        if (float.IsNaN(ratio) || float.IsInfinity(ratio)) return;

        if (!initialized || immediate || ratio > currentRatio)
        {
            // 초기화·회복·재생성 시 이전 피해 잔상을 남기지 않음.
            initialized = true;
            currentRatio = damageRatio = tweenStart = ratio;
            delayRemaining = tweenElapsed = 0f;
            ApplyFills();
            return;
        }
        if (Mathf.Approximately(ratio, currentRatio)) return;

        currentRatio = ratio;
        // 연속 피격 시 주황색 바가 위로 튀지 않고 현재 표시량에서 재출발.
        tweenStart = damageRatio;
        delayRemaining = Mathf.Max(0f, damageDelay);
        tweenElapsed = 0f;
        ApplyFills();
    }

    private void Update()
    {
        // 세계에 붙은 UI도 히트스탑과 메뉴 일시정지에 맞춰 정지.
        if (Time.timeScale > 0f) Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!initialized || IsSettled || deltaTime <= 0f) return;
        if (delayRemaining > 0f)
        {
            float heldTime = Mathf.Min(delayRemaining, deltaTime);
            delayRemaining -= heldTime;
            deltaTime -= heldTime;
        }
        if (deltaTime <= 0f) return;

        tweenElapsed += deltaTime;
        float t = Mathf.Clamp01(tweenElapsed / Mathf.Max(0.01f, damageDuration));
        damageRatio = Mathf.Lerp(tweenStart, currentRatio, Mathf.SmoothStep(0f, 1f, t));
        ApplyFills();
    }

    private void ApplyFills()
    {
        if (currentFill != null) currentFill.fillAmount = currentRatio;
        if (damageFill != null) damageFill.fillAmount = damageRatio;
    }
}
