using UnityEngine;
using UnityEngine.UI;

// 기존 근접 공격과 음표 투사체가 공유하는 보스 체력.
[DefaultExecutionOrder(30), DisallowMultipleComponent]
public class Boss1Health : Damageable
{
    [SerializeField, Min(1f)] private float maxHealth = 200f;
    [Header("Screen Health Bar")]
    [SerializeField] private string bossName = "Boss1";
    [Tooltip("1920 기준 체력바 길이")]
    [SerializeField, Range(48f, 1920f)] private float barLength = 1824f;
    [SerializeField, Min(1f)] private float barHeight = 24f;
    [SerializeField, Min(0f)] private float topMargin = 24f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsPhaseTwo { get; private set; }
    public bool IsPhaseTransitionPending => phaseTransitionRequested;

    private bool phaseTransitionRequested;

    // Run after the body and aimed missile clocks, so their last recovery/explosion
    // frame completes before the new phase changes any timings.
    private void FixedUpdate()
    {
        if (IsDead || IsPhaseTwo || !phaseTransitionRequested) return;
        var controller = GetComponent<Boss1ProximityAttack>();
        if (controller == null || !controller.isActiveAndEnabled || !controller.ReadyForExclusiveAction) return;

        IsPhaseTwo = true;
        phaseTransitionRequested = false;
        controller.PhaseTransitionPending = false;
        var missiles = GetComponent<Boss1AimedMissile>();
        if (missiles != null) missiles.PhaseTwoEnabled = true;
    }

    private GameObject hud;
    private RectTransform panel;
    private RectTransform barSlot;
    private CombatHealthBar healthBar;
    private Vector2 appliedSize;
    private HealthBarUI damageNumbers;
    private RectTransform upperBand;
    private RectTransform lowerBand;
    private float entranceBandHeight;
    private float revealElapsed;
    private float revealDuration;
    private float bandExitDuration;
    private float revealFraction = 1f;

    private void Awake() => CurrentHealth = MaxHealth;

    private void Start()
    {
        EnsureHud();
        RefreshBar();
    }

    private void EnsureHud()
    {
        if (hud != null) return;
        // 화면 크기가 바뀌어도 이름과 체력 바를 상단 중앙에 유지.
        hud = new GameObject("Boss1 Health HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 51;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0f;
        // 기존 허수아비/좀비의 숫자 연출을 화면 HUD에서도 재사용한다.
        damageNumbers = hud.AddComponent<HealthBarUI>();

        panel = Rect("Boss Health", hud.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -topMargin);

        RectTransform title = Rect("Boss Name", panel);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = Vector2.one;
        title.pivot = new Vector2(0.5f, 1f);
        title.sizeDelta = new Vector2(0f, 36f);
        var label = title.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = bossName;
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        barSlot = Rect("Health Gauge", panel);
        barSlot.anchorMin = barSlot.anchorMax = new Vector2(0.5f, 0f);
        barSlot.pivot = new Vector2(0.5f, 0f);
        healthBar = new CombatHealthBar(barSlot, new Color32(250, 158, 62, 255));
        UpdateLayout();
    }

    // The encounter can reveal the HUD while damage reception is still disabled.
    public void ShowEntranceBands(float bandHeight)
    {
        EnsureHud();
        if (upperBand == null)
        {
            upperBand = CreateBand("Upper Letterbox");
            lowerBand = CreateBand("Lower Letterbox");
        }
        entranceBandHeight = bandHeight;
        SetBandHeight(bandHeight);
        panel.gameObject.SetActive(false);
        hud.SetActive(true);
    }

    public void BeginCombatReveal(float duration, float bandDuration)
    {
        if (!isActiveAndEnabled)
        {
            EndEntrancePresentation();
            return;
        }
        revealElapsed = 0f;
        revealDuration = Mathf.Max(0.01f, duration);
        bandExitDuration = bandDuration;
        if (bandExitDuration <= 0f) SetBandHeight(0f);
        revealFraction = 0f;
        panel.gameObject.SetActive(true);
        RefreshBar();
    }

    private void SetBandHeight(float bandHeight)
    {
        upperBand.anchorMin = new Vector2(0f, 1f - bandHeight);
        upperBand.anchorMax = Vector2.one;
        lowerBand.anchorMin = Vector2.zero;
        lowerBand.anchorMax = new Vector2(1f, bandHeight);
    }

    private RectTransform CreateBand(string name)
    {
        RectTransform band = Rect(name, hud.transform);
        band.SetAsFirstSibling();
        band.offsetMin = band.offsetMax = Vector2.zero;
        var image = band.gameObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        return band;
    }

    public void EndEntrancePresentation()
    {
        if (hud == null) return;
        if (upperBand != null) SetBandHeight(0f);
        revealDuration = 0f;
        revealFraction = 1f;
        panel.gameObject.SetActive(true);
        RefreshBar();
        hud.SetActive(isActiveAndEnabled);
    }

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            return false;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        RefreshBar();
        if (IsDead)
        {
            // 체력 0에서는 본체 패턴과 독립 미사일 발사를 정지.
            var controller = GetComponent<Boss1ProximityAttack>();
            phaseTransitionRequested = false;
            if (controller != null)
            {
                controller.PhaseTransitionPending = false;
                controller.BarragePending = false;
                controller.enabled = false;
            }
            var missiles = GetComponent<Boss1AimedMissile>();
            if (missiles != null) missiles.enabled = false;
            var rear = GetComponent<Boss1RearMissiles>();
            if (rear != null) rear.enabled = false;
        }
        else if (!IsPhaseTwo && CurrentHealth <= MaxHealth * 0.5f)
        {
            // Reserve immediately on accepted damage: no new pattern or aimed
            // volley may slip in before the next physics update.
            phaseTransitionRequested = true;
            var controller = GetComponent<Boss1ProximityAttack>();
            if (controller != null) controller.PhaseTransitionPending = true;
        }
        return true;
    }

    public override bool TryTakeDamage(AttackHit hit)
    {
        float healthBeforeHit = CurrentHealth;
        if (!TryTakeDamage(hit.Damage)) return false;
        if (damageNumbers != null)
            damageNumbers.ShowDamage(Mathf.Min(hit.Damage, healthBeforeHit), hit.DisplayPoint);
        return true;
    }

    private void LateUpdate()
    {
        UpdateLayout();
        if (revealDuration <= 0f || Time.timeScale <= 0f) return;
        revealElapsed += Time.unscaledDeltaTime;
        revealFraction = Mathf.SmoothStep(0f, 1f, revealElapsed / revealDuration);
        if (bandExitDuration > 0f)
            SetBandHeight(entranceBandHeight * (1f - Mathf.SmoothStep(0f, 1f, revealElapsed / bandExitDuration)));
        RefreshBar();
        if (revealElapsed >= Mathf.Max(revealDuration, bandExitDuration)) EndEntrancePresentation();
    }

    private void UpdateLayout()
    {
        Vector2 size = new Vector2(barLength, barHeight);
        if (size == appliedSize) return;
        appliedSize = size;
        panel.sizeDelta = new Vector2(size.x, 44f + size.y);
        barSlot.sizeDelta = size;
        healthBar.SetSize(size);
    }

    private void RefreshBar() => healthBar?.SetHealth(revealFraction * CurrentHealth / MaxHealth);

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void OnEnable() { if (hud != null) hud.SetActive(true); }
    private void OnDisable() { if (hud != null) hud.SetActive(false); }
    private void OnDestroy() { if (hud != null) Destroy(hud); }
}
