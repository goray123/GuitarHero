using UnityEngine;
using UnityEngine.UI;

// 기능 확인용 HUD. 화면 좌측 하단에 현재 값·단계·다음 공격 배율을 표시.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRhythmGauge))]
public class PlayerRhythmGaugeUI : MonoBehaviour
{
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, 24f);
    [SerializeField] private Vector2 panelSize = new Vector2(420f, 102f);

    private PlayerRhythmGauge rhythm;
    private GameObject hudRoot;
    private Text stageText;
    private Text valueText;
    private Text statsText;
    private Image fill;

    private static readonly string[] StageNames = { "LOW", "NORMAL", "HIGH", "FEVER" };
    private static readonly Color[] StageColors =
    {
        new Color(0.68f, 0.65f, 1f),
        new Color(0.82f, 0.84f, 0.89f),
        new Color(1f, 0.68f, 0.38f),
        new Color(1f, 0.34f, 0.47f)
    };

    private void Awake() => rhythm = GetComponent<PlayerRhythmGauge>();

    private void OnEnable()
    {
        rhythm.Changed += Refresh;
        if (hudRoot == null) Build();
        hudRoot.SetActive(true);
        Refresh();
    }

    private void OnDisable()
    {
        if (rhythm != null) rhythm.Changed -= Refresh;
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hudRoot != null) Destroy(hudRoot);
    }

    private void Build()
    {
        hudRoot = new GameObject("RhythmHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = hudRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = hudRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image panel = CreateImage("Panel", hudRoot.transform, new Color(0.06f, 0.07f, 0.10f, 0.94f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = screenOffset;
        panelRect.sizeDelta = panelSize;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stageText = CreateText("Stage", panel.transform, font, 20, TextAnchor.MiddleLeft);
        Place(stageText.rectTransform, new Vector2(14f, -8f), new Vector2(170f, 26f));
        valueText = CreateText("Value", panel.transform, font, 18, TextAnchor.MiddleRight);
        Place(valueText.rectTransform, new Vector2(panelSize.x - 184f, -8f), new Vector2(170f, 26f));
        statsText = CreateText("Stats", panel.transform, font, 15, TextAnchor.MiddleLeft);
        Place(statsText.rectTransform, new Vector2(14f, -68f), new Vector2(panelSize.x - 28f, 23f));

        Image track = CreateImage("Track", panel.transform, new Color(0.16f, 0.18f, 0.23f));
        Place(track.rectTransform, new Vector2(14f, -41f), new Vector2(panelSize.x - 28f, 17f));
        fill = CreateImage("Fill", track.transform, StageColors[0]);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        for (int i = 1; i < 4; i++)
        {
            Image marker = CreateImage("Boundary" + i, track.transform, new Color(0.06f, 0.07f, 0.10f));
            marker.rectTransform.anchorMin = new Vector2(i * 0.25f, 0f);
            marker.rectTransform.anchorMax = new Vector2(i * 0.25f, 1f);
            marker.rectTransform.offsetMin = new Vector2(-1f, 0f);
            marker.rectTransform.offsetMax = new Vector2(1f, 0f);
        }
    }

    private void Refresh()
    {
        if (fill == null) return;
        int stage = (int)rhythm.Stage;
        fill.color = StageColors[stage];
        fill.rectTransform.anchorMax = new Vector2(rhythm.NormalizedGauge, 1f);
        stageText.text = "RHYTHM  " + StageNames[stage];
        stageText.color = StageColors[stage];
        valueText.text = Mathf.FloorToInt(rhythm.CurrentGauge) + " / 1000";
        statsText.text = "NEXT ATTACK   SPD x" + rhythm.AttackSpeedMultiplier.ToString("0.00") +
            "    DMG x" + rhythm.AttackDamageMultiplier.ToString("0.00");
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        item.transform.SetParent(parent, false);
        Text text = item.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
