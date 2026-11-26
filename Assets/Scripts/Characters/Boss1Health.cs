using UnityEngine;
using UnityEngine.UI;

// 기존 근접 공격과 음표 투사체가 공유하는 보스 체력.
[DefaultExecutionOrder(30), DisallowMultipleComponent]
public class Boss1Health : Damageable
{
    [SerializeField, Min(1f)] private float maxHealth = 200f;
    [Header("Screen Health Bar")]
    [SerializeField] private string bossName = "메가 메카트론 JT8-3";
    [SerializeField] private Font nameFont;
    [Tooltip("1920 기준 좌우 여백. 화면 너비에 비례해 조절됨")]
    [SerializeField, Range(0f, 400f)] private float sideMargin = 48f;
    [SerializeField, Min(1f)] private float barHeight = 24f;
    [SerializeField, Min(0f)] private float topMargin = 24f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public string DisplayName => bossName;
    public Font NameFont => nameFont != null ? nameFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    private RectTransform greenBar;
    private HealthBarUI damageNumbers;
    private float entranceProgress = 1f;

    private void Awake() => CurrentHealth = MaxHealth;

    private void Start() => BuildHud();

    private void BuildHud()
    {
        if (hud != null) return;
        // 화면 크기가 바뀌어도 이름과 체력 바를 상단 중앙에 유지.
        hud = new GameObject("Boss1 Health HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0f;
        // 기존 허수아비/좀비의 숫자 연출을 화면 HUD에서도 재사용한다.
        damageNumbers = hud.AddComponent<HealthBarUI>();

        RectTransform panel = Rect("Boss Health", hud.transform);
        // 고정 폭 대신 좌우로 늘려 화면 너비를 채움.
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -topMargin);
        panel.sizeDelta = new Vector2(-2f * sideMargin, 44f + barHeight);

        RectTransform title = Rect("Boss Name", panel);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = Vector2.one;
        title.pivot = new Vector2(0.5f, 1f);
        title.sizeDelta = new Vector2(0f, 36f);
        var label = title.gameObject.AddComponent<Text>();
        label.font = NameFont;
        label.text = bossName;
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        RectTransform redBar = Rect("Lost Health (Red)", panel);
        redBar.anchorMin = Vector2.zero;
        redBar.anchorMax = new Vector2(1f, 0f);
        redBar.pivot = new Vector2(0.5f, 0f);
        redBar.sizeDelta = new Vector2(0f, barHeight);
        Paint(redBar, new Color(0.85f, 0.08f, 0.08f));
        greenBar = Rect("Remaining Health (Green)", redBar);
        greenBar.anchorMin = Vector2.zero;
        greenBar.anchorMax = Vector2.one;
        greenBar.offsetMin = greenBar.offsetMax = Vector2.zero;
        Paint(greenBar, new Color(0.1f, 0.85f, 0.18f));
        RefreshBar();
        hud.SetActive(entranceProgress > 0f);
    }

    public void SetEntranceProgress(float progress)
    {
        entranceProgress = Mathf.Clamp01(progress);
        BuildHud();
        hud.SetActive(entranceProgress > 0f);
        RefreshBar();
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

    private void RefreshBar()
    {
        if (greenBar == null) return;
        // 왼쪽 끝을 고정하고 오른쪽부터 초록색이 줄어 빨간 바가 드러남.
        greenBar.anchorMax = new Vector2(Mathf.Clamp01(CurrentHealth / MaxHealth) * entranceProgress, 1f);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Paint(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void OnEnable() { if (hud != null) hud.SetActive(true); }
    private void OnDisable() { if (hud != null) hud.SetActive(false); }
    private void OnDestroy() { if (hud != null) Destroy(hud); }
}
