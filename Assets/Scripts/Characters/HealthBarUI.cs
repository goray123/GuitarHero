using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("앞쪽 초록색 바. 실제 체력을 즉시 표시")]
    [SerializeField] private Image currentFill;
    [Tooltip("뒤쪽 주황색 바. 피해 구간을 남겼다가 따라 감소")]
    [SerializeField] private Image damageFill;
    [SerializeField] private Text healthText;
    [SerializeField] private Text damagePopupTemplate;
    [SerializeField, Min(0f)] private float damageDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float damageDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float damagePopupDuration = 0.65f;
    [SerializeField, Min(0f)] private float damagePopupRise = 28f;

    private readonly List<DamagePopup> damagePopups = new List<DamagePopup>();

    private bool initialized;
    private float currentRatio = 1f;
    private float damageRatio = 1f;
    private float delayRemaining;
    private float tweenElapsed;
    private float tweenStart = 1f;

    public bool IsSettled => Mathf.Approximately(damageRatio, currentRatio);

    private sealed class DamagePopup
    {
        public Text Text;
        public Vector3 StartPosition;
        public Vector3 RiseVector;
        public float Elapsed;
    }

    public void SetHealth(float current, float maximum, bool immediate = false)
    {
        float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        if (float.IsNaN(ratio) || float.IsInfinity(ratio)) return;
        if (healthText != null) healthText.text = $"{current:0.##} / {maximum:0.##}";

        if (!initialized || immediate || ratio > currentRatio)
        {
            // 초기화·회복·재생성 시 이전 피해 잔상을 남기지 않음.
            initialized = true;
            if (immediate) ClearDamagePopups();
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
        if (Time.timeScale > 0f)
        {
            Advance(Time.deltaTime);
            AdvanceDamagePopups(Time.deltaTime);
        }
    }

    public void ShowDamage(float amount, Vector3 worldPosition)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

        Text popupText = damagePopupTemplate != null
            ? Instantiate(damagePopupTemplate, transform, false)
            : CreateDamagePopup();
        popupText.text = $"-{amount:0.##}";
        Color color = popupText.color;
        color.a = 1f;
        popupText.color = color;

        Vector3 startPosition = new Vector3(
            worldPosition.x * 1f,
            worldPosition.y * 1f,
            worldPosition.z * 1f);
        Vector3 riseVector = Vector3.up * damagePopupRise;
        popupText.rectTransform.localPosition = startPosition;
        popupText.gameObject.SetActive(true);
        popupText.transform.SetAsLastSibling();
        damagePopups.Add(new DamagePopup
        {
            Text = popupText,
            StartPosition = startPosition,
            RiseVector = riseVector
        });
    }

    private Text CreateDamagePopup()
    {
        GameObject popupObject = new GameObject("DamagePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        popupObject.transform.SetParent(transform, false);
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.anchorMin = popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(144f, 24f);

        Text popupText = popupObject.GetComponent<Text>();
        popupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupText.fontSize = 18;
        popupText.fontStyle = FontStyle.Bold;
        popupText.alignment = TextAnchor.MiddleCenter;
        popupText.horizontalOverflow = HorizontalWrapMode.Overflow;
        popupText.verticalOverflow = VerticalWrapMode.Overflow;
        popupText.raycastTarget = false;
        popupText.color = new Color(1f, 0.82f, 0.25f, 1f);
        return popupText;
    }

    private void AdvanceDamagePopups(float deltaTime)
    {
        float duration = Mathf.Max(1f, damagePopupDuration);
        for (int i = damagePopups.Count - 1; i >= 0; i--)
        {
            DamagePopup popup = damagePopups[i];
            popup.Elapsed += deltaTime;
            float t = Mathf.Clamp01(popup.Elapsed / duration);
            popup.Text.rectTransform.localPosition = popup.StartPosition + popup.RiseVector * t;
            Color color = popup.Text.color;
            color.a = 1f - t;
            popup.Text.color = color;
            if (t < 1f) continue;

            Destroy(popup.Text.gameObject);
            damagePopups.RemoveAt(i);
        }
    }

    private void ClearDamagePopups()
    {
        foreach (DamagePopup popup in damagePopups)
            if (popup.Text != null) Destroy(popup.Text.gameObject);
        damagePopups.Clear();
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
        float t = Mathf.Clamp01(tweenElapsed / Mathf.Max(1f, damageDuration));
        damageRatio = Mathf.Lerp(tweenStart, currentRatio, Mathf.SmoothStep(0f, 1f, t));
        ApplyFills();
    }

    private void ApplyFills()
    {
        if (currentFill != null) currentFill.fillAmount = currentRatio;
        if (damageFill != null) damageFill.fillAmount = damageRatio;
    }
}
