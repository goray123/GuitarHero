using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHealth : Damageable
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [Header("Overhead Health Bar")]
    [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.12f);
    [SerializeField, Min(0f)] private float heightOffset = 0.2f;
    [SerializeField] private Color fillColor = new Color(0.12f, 0.9f, 0.25f, 1f);

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead => CurrentHealth <= 0f;

    private RectTransform barRoot;
    private RectTransform fill;
    private Collider2D bodyCollider;
    private SpriteRenderer characterSprite;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        characterSprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (barRoot == null) BuildBar();
        barRoot.gameObject.SetActive(true);
        ResetHealth();
        LateUpdate();
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        RefreshBar();
    }

    public override bool TryTakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            return false;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        RefreshBar();
        return true;
    }

    private void BuildBar()
    {
        barRoot = new GameObject("Player Health Bar", typeof(RectTransform), typeof(Canvas))
            .GetComponent<RectTransform>();
        barRoot.SetParent(transform, false);
        barRoot.sizeDelta = barSize;
        var canvas = barRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(barRoot, false);
        background.color = new Color(0.04f, 0.06f, 0.04f, 0.85f);
        background.raycastTarget = false;
        Stretch(background.rectTransform);

        var green = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        green.transform.SetParent(background.transform, false);
        green.color = fillColor;
        green.raycastTarget = false;
        fill = green.rectTransform;
        Stretch(fill);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void RefreshBar()
    {
        if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(CurrentHealth / MaxHealth), 1f);
    }

    private void LateUpdate()
    {
        if (barRoot == null) return;
        float x = transform.position.x;
        float top = transform.position.y;
        if (bodyCollider != null && bodyCollider.enabled)
        {
            x = bodyCollider.bounds.center.x;
            top = bodyCollider.bounds.max.y;
        }
        // Keep the bar above the visible character even when sliding shrinks its collider.
        if (characterSprite != null && characterSprite.enabled && characterSprite.sprite != null)
            top = Mathf.Max(top, characterSprite.bounds.max.y);
        barRoot.position = new Vector3(x, top + heightOffset + barSize.y * 0.5f, transform.position.z);
        barRoot.rotation = Quaternion.identity;
        // World-sized bar must not mirror or stretch with the character's transform.
        var scale = transform.lossyScale;
        barRoot.localScale = new Vector3(InverseScale(scale.x), InverseScale(scale.y), InverseScale(scale.z));
    }

    private static float InverseScale(float value) => Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;

    private void OnDisable() { if (barRoot != null) barRoot.gameObject.SetActive(false); }
    private void OnDestroy() { if (barRoot != null) Destroy(barRoot.gameObject); }
}
