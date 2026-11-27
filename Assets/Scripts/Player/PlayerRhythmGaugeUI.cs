using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRhythmGauge), typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerRhythmGaugeUI : MonoBehaviour
{
    [SerializeField] private Sprite guitarArtwork;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite cooldownPixel;
    [Tooltip("대시 쿨타임의 검은 막 투명도 (%). 0이면 불투명, 100이면 완전히 투명합니다.")]
    [SerializeField, Range(0f, 100f)] private float dashCooldownTransparency = 90f;
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, 24f);
    [SerializeField, Min(1f)] private float screenWidthDivisor = 3.36f;
    [SerializeField, Min(1f)] private float screenHeightDivisor = 6.05f;

    private GameObject hudRoot;
    private PlayerRhythmGauge rhythm;
    private PlayerHealth health;
    private PlayerMovement movement;
    private Image dashCooldown;
    private RectTransform hpShade;
    private readonly RectTransform[] rhythmShades = new RectTransform[4];

    private void Awake()
    {
        rhythm = GetComponent<PlayerRhythmGauge>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (hudRoot == null) Build();
        hudRoot.SetActive(true);
        rhythm.Changed += RefreshRhythm;
        health.HealthChanged += RefreshHealth;
        RefreshRhythm();
        RefreshHealth();
        RefreshDash();
    }

    private void LateUpdate() => RefreshDash();

    private void RefreshDash()
    {
        dashCooldown.color = new Color(0f, 0f, 0f, 1f - dashCooldownTransparency / 100f);
        dashCooldown.fillAmount = movement.SlideCooldownFraction;
    }

    private void OnDisable()
    {
        rhythm.Changed -= RefreshRhythm;
        health.HealthChanged -= RefreshHealth;
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hudRoot != null) Destroy(hudRoot);
    }

    private void Build()
    {
        hudRoot = new GameObject("Player Guitar HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = hudRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = hudRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var guitar = CreateImage("Guitar", hudRoot.transform);
        guitar.sprite = guitarArtwork;
        var rect = guitar.rectTransform;
        rect.anchorMin = rect.pivot = Vector2.zero;
        rect.anchorMax = new Vector2(1f / screenWidthDivisor, 1f / screenHeightDivisor);
        rect.offsetMin = rect.offsetMax = screenOffset;

        var icon = CreateImage("Dash", rect);
        icon.sprite = dashIcon;
        icon.preserveAspect = true;
        PlaceOnArtwork(icon.rectTransform, new RectInt(245, 44, 46, 46));

        dashCooldown = CreateImage("Dash Cooldown", rect);
        dashCooldown.sprite = cooldownPixel;
        dashCooldown.type = Image.Type.Filled;
        dashCooldown.fillMethod = Image.FillMethod.Radial360;
        dashCooldown.fillOrigin = (int)Image.Origin360.Top;
        // As the counterclockwise remainder shrinks, the icon is revealed clockwise.
        dashCooldown.fillClockwise = false;
        PlaceOnArtwork(dashCooldown.rectTransform, new RectInt(241, 40, 54, 54));

        // Bounds of the illuminated pixels, in upper-left artwork coordinates.
        hpShade = CreateShade("HP", rect, new RectInt(193, 103, 300, 18));
        for (int i = 0; i < rhythmShades.Length; i++)
            rhythmShades[i] = CreateShade("Rhythm " + (i + 1), rect, new RectInt(58 + 30 * i, 60, 30, 29));
    }

    private static Image CreateImage(string name, Transform parent)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateShade(string name, Transform parent, RectInt pixels)
    {
        var region = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        region.SetParent(parent, false);
        PlaceOnArtwork(region, pixels);

        var shade = CreateImage("Unlit", region);
        shade.color = new Color32(13, 10, 13, 246);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        return shade.rectTransform;
    }

    private static void PlaceOnArtwork(RectTransform region, RectInt pixels)
    {
        region.anchorMin = new Vector2(pixels.xMin / 571f, 1f - pixels.yMax / 179f);
        region.anchorMax = new Vector2(pixels.xMax / 571f, 1f - pixels.yMin / 179f);
        region.offsetMin = region.offsetMax = Vector2.zero;
    }

    private void RefreshHealth() => ShowCharge(hpShade, health.CurrentHealth / health.MaxHealth);

    private void RefreshRhythm()
    {
        float charge = rhythm.CurrentGauge / PlayerRhythmGauge.StageWidth;
        for (int i = 0; i < rhythmShades.Length; i++)
            ShowCharge(rhythmShades[i], charge - i);
    }

    private static void ShowCharge(RectTransform shade, float fraction)
    {
        shade.anchorMin = new Vector2(Mathf.Clamp01(fraction), 0f);
    }
}
