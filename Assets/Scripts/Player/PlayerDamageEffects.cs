using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(SpriteRenderer))]
public class PlayerDamageEffects : MonoBehaviour
{
    [Header("Character Flash")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0f)] private float flashDuration = 0.05f;
    [Header("Invulnerability Blink")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;
    [Header("Screen Edges")]
    [SerializeField] private Color overlayColor = new Color(0.85f, 0.02f, 0.04f, 0.45f);
    [SerializeField, Min(0f)] private float overlayDuration = 1f;
    [Header("Damage Spark")]
    [SerializeField] private BluntHitBurst sparkPrefab;
    [SerializeField] private Color sparkColor = new Color(1f, 0.18f, 0.22f, 1f);
    [SerializeField, Min(0.01f)] private float sparkScale = 0.45f;
    [SerializeField] private int sparkSortingOrderOffset = 8;

    private PlayerHealth health;
    private SpriteRenderer characterSprite;
    private SpriteRenderer flashSprite;
    private GameObject overlayRoot;
    private RawImage overlayImage;
    private Texture2D overlayTexture;
    private double feedbackStartedAt;
    private bool flashing;
    private bool blinking;
    private bool originalForceRenderingOff;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        characterSprite = GetComponent<SpriteRenderer>();
        var overlay = new GameObject("Damage Flash", typeof(SpriteRenderer));
        overlay.layer = gameObject.layer;
        overlay.transform.SetParent(characterSprite.transform, false);
        flashSprite = overlay.GetComponent<SpriteRenderer>();
        flashSprite.sharedMaterial = flashMaterial;
        flashSprite.enabled = false;
        BuildOverlay();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        if (overlayRoot != null) overlayRoot.SetActive(true);
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        StopFlash();
        StopBlink();
        SetOverlayStrength(0f);
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void OnDamaged(AttackHit hit)
    {
        feedbackStartedAt = Time.unscaledTimeAsDouble;
        if (!blinking) originalForceRenderingOff = characterSprite.forceRenderingOff;
        blinking = health.IsInvulnerable;
        UpdateBlink(0f);
        SetOverlayStrength(overlayDuration > 0f ? 1f : 0f);
        if (flashMaterial != null && flashDuration > 0f)
        {
            flashing = true;
            UpdateFlash(0f);
        }

        if (sparkPrefab == null) return;
        BluntHitBurst spark = Instantiate(sparkPrefab, hit.Point, Quaternion.identity);
        spark.Play(hit.Direction, sparkScale, characterSprite.sortingLayerID,
            characterSprite.sortingOrder + sparkSortingOrderOffset);
        if (spark.TryGetComponent<SpriteRenderer>(out var sparkRenderer))
            sparkRenderer.color = sparkColor;
    }

    private void LateUpdate()
    {
        // Finish the flash even when a simultaneous attack has frozen game time.
        float elapsed = (float)(Time.unscaledTimeAsDouble - feedbackStartedAt);
        if (flashing)
        {
            if (elapsed >= flashDuration || flashDuration <= 0f) StopFlash();
            else UpdateFlash(elapsed / flashDuration);
        }
        UpdateBlink(elapsed);
        if (overlayImage != null && overlayImage.enabled)
            SetOverlayStrength(overlayDuration > 0f ? 1f - Mathf.Clamp01(elapsed / overlayDuration) : 0f);
    }

    private void UpdateBlink(float elapsed)
    {
        if (!blinking) return;
        if (!health.IsInvulnerable)
        {
            StopBlink();
            return;
        }
        // Keep the impact flash visible, then blink for exactly as long as health grants protection.
        bool hidden = !flashing && ((int)(elapsed / Mathf.Max(0.01f, blinkInterval)) & 1) != 0;
        characterSprite.forceRenderingOff = originalForceRenderingOff || hidden;
    }

    private void StopBlink()
    {
        if (!blinking) return;
        blinking = false;
        if (characterSprite != null) characterSprite.forceRenderingOff = originalForceRenderingOff;
    }

    private void BuildOverlay()
    {
        const int size = 128;
        overlayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Player Damage Edges",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float edge = Mathf.Max(Mathf.Abs(x * 2f / (size - 1) - 1f), Mathf.Abs(y * 2f / (size - 1) - 1f));
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, edge));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
        }
        overlayTexture.SetPixels32(pixels);
        overlayTexture.Apply(false, true);

        overlayRoot = new GameObject("Player Damage Overlay", typeof(RectTransform), typeof(Canvas));
        var canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        overlayImage = new GameObject("Red Edges", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        overlayImage.transform.SetParent(overlayRoot.transform, false);
        overlayImage.texture = overlayTexture;
        overlayImage.raycastTarget = false;
        overlayImage.rectTransform.anchorMin = Vector2.zero;
        overlayImage.rectTransform.anchorMax = Vector2.one;
        overlayImage.rectTransform.offsetMin = overlayImage.rectTransform.offsetMax = Vector2.zero;
        SetOverlayStrength(0f);
    }

    private void SetOverlayStrength(float strength)
    {
        if (overlayImage == null) return;
        Color color = overlayColor;
        color.a *= strength;
        overlayImage.color = color;
        overlayImage.enabled = strength > 0f;
    }

    private void UpdateFlash(float progress)
    {
        // Follow the current animation and facing without replacing its material or tint.
        flashSprite.sprite = characterSprite.sprite;
        flashSprite.flipX = characterSprite.flipX;
        flashSprite.flipY = characterSprite.flipY;
        flashSprite.drawMode = characterSprite.drawMode;
        flashSprite.size = characterSprite.size;
        flashSprite.maskInteraction = characterSprite.maskInteraction;
        flashSprite.spriteSortPoint = characterSprite.spriteSortPoint;
        flashSprite.sortingLayerID = characterSprite.sortingLayerID;
        flashSprite.sortingOrder = characterSprite.sortingOrder + 1;
        // Hold the initial flash briefly before fading it out.
        float fade = 1f - Mathf.InverseLerp(0.3f, 1f, progress);
        Color color = flashColor;
        color.a *= characterSprite.color.a * fade;
        flashSprite.color = color;
        flashSprite.enabled = characterSprite.enabled && !characterSprite.forceRenderingOff && characterSprite.sprite != null;
    }

    private void StopFlash()
    {
        flashing = false;
        if (flashSprite != null) flashSprite.enabled = false;
    }

    private void OnDestroy()
    {
        if (flashSprite != null) Destroy(flashSprite.gameObject);
        if (overlayRoot != null) Destroy(overlayRoot);
        if (overlayTexture != null) Destroy(overlayTexture);
    }
}
