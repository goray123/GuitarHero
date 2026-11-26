using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Boss1EntrancePresentation : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float anticipation = 0.35f;
    [SerializeField, Min(0.1f)] private float titleHold = 1.05f;
    [Header("Camera")]
    [SerializeField, Range(0f, 0.15f)] private float barHeight = 0.075f;
    [SerializeField, Range(0f, 0.25f)] private float zoom = 0.1f;
    [SerializeField, Min(0f)] private float impactShake = 0.18f;
    [Header("Existing Feedback Assets")]
    [SerializeField] private AudioClip fallClip;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private AudioClip combatClip;
    [SerializeField] private BluntHitBurst dustPrefab;

    private Boss1Health boss;
    private CityCameraFollow cameraFollow;
    private Transform landingPoint;
    private PlayerHitCameraShake shake;
    private PlayerHitStop hitStop;
    private Boss1SlamSpriteVisual art;
    private AudioSource audioSource;
    private AudioSource[] music;
    private float[] musicVolumes;
    private GameObject hud;
    private RectTransform topBar, bottomBar, titleRect;
    private CanvasGroup titleGroup;
    private Image dim;
    private SpriteRenderer shadow;
    private Texture2D shadowTexture;
    private bool presenting, falling, replay;
    private float dropY;

    public IEnumerator Anticipate(PlayerMovement player, Boss1Health target, Transform landing,
        CityCameraFollow camera, bool isReplay)
    {
        boss = target;
        landingPoint = landing;
        cameraFollow = camera;
        replay = isReplay;
        shake = player.GetComponent<PlayerHitCameraShake>();
        hitStop = player.GetComponent<PlayerHitStop>();
        art = boss.GetComponent<Boss1SlamSpriteVisual>();
        // Only looping music is ducked; footsteps and the entrance cues keep their own levels.
        music = System.Array.FindAll(FindObjectsByType<AudioSource>(FindObjectsSortMode.None),
            source => source.isPlaying && source.loop);
        musicVolumes = new float[music.Length];
        for (int i = 0; i < music.Length; i++) musicVolumes[i] = music[i].volume;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        BuildHud();
        BuildShadow();
        presenting = true;
        hud.SetActive(true);
        shadow.gameObject.SetActive(true);
        float duration = replay ? 0.15f : anticipation;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Ease(elapsed / duration);
            SetFraming(t);
            shadow.color = new Color(0f, 0f, 0f, t * 0.35f);
            yield return null;
        }
        SetFraming(1f);
    }

    public void BeginFall()
    {
        falling = true;
        dropY = boss.transform.position.y;
        if (fallClip != null)
        {
            audioSource.clip = fallClip;
            audioSource.loop = true;
            audioSource.pitch = 0.65f;
            audioSource.volume = 0.24f;
            audioSource.Play();
        }
        var body = boss.GetComponent<Rigidbody2D>();
        float fallTime = Mathf.Sqrt(2f * Mathf.Abs(dropY - landingPoint.position.y) /
            Mathf.Max(0.1f, -Physics2D.gravity.y * body.gravityScale));
        if (shake != null) shake.Kick(Vector2.down, 0.025f, fallTime);
    }

    private void Update()
    {
        if (!falling || boss == null || shadow == null) return;
        float t = Mathf.InverseLerp(dropY, landingPoint.position.y, boss.transform.position.y);
        shadow.transform.localScale = new Vector3(Mathf.Lerp(2.5f, 5f, t), Mathf.Lerp(0.15f, 0.35f, t), 1f);
        shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.35f, 0.75f, t));
    }

    public IEnumerator Impact()
    {
        falling = false;
        shadow.gameObject.SetActive(false);
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.volume = 1f;
        audioSource.pitch = 0.65f;
        if (impactClip != null) audioSource.PlayOneShot(impactClip, 0.85f);
        if (hitStop != null) hitStop.StopFor(0.08f, true);
        if (shake != null) shake.Kick(Vector2.down, impactShake, 0.4f);
        if (dustPrefab != null)
        {
            var renderer = boss.GetComponent<SpriteRenderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                var dust = Instantiate(dustPrefab, landingPoint.position + new Vector3(side * 0.6f, 0.08f), Quaternion.identity);
                dust.Play(Vector2.right * side, 2.2f, renderer.sortingLayerID, renderer.sortingOrder + 8, true);
            }
        }
        float duration = replay ? 0.35f : titleHold;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float pop = Ease(elapsed / 0.18f);
            titleGroup.alpha = pop;
            titleRect.localScale = Vector3.one * Mathf.Lerp(1.08f, 1f, pop);
            titleRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-18f, 0f, pop));
            if (art != null) art.EntranceCompression = 1f - Ease(elapsed / 0.4f);
            yield return null;
        }
        if (art != null) art.EntranceCompression = 0f;
    }

    public IEnumerator Release(float duration)
    {
        duration = Mathf.Max(0.55f, replay ? duration * 0.6f : duration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            titleGroup.alpha = 1f - Ease(t / 0.5f);
            SetFraming(1f - Ease(t));
            boss.SetEntranceProgress(Ease(t));
            yield return null;
        }
        boss.SetEntranceProgress(1f);
        ResetPresentation();
        audioSource.pitch = 0.9f;
        if (combatClip != null) audioSource.PlayOneShot(combatClip, 0.4f);
    }

    private void SetFraming(float amount)
    {
        topBar.anchorMin = new Vector2(0f, 1f - barHeight * amount);
        bottomBar.anchorMax = new Vector2(1f, barHeight * amount);
        dim.color = new Color(0.015f, 0.02f, 0.03f, 0.12f * amount);
        cameraFollow.SetCinematicFocus(landingPoint, amount, zoom);
        for (int i = 0; i < music.Length; i++)
            if (music[i] != null) music[i].volume = musicVolumes[i] * Mathf.Lerp(1f, 0.25f, amount);
    }

    public void ResetPresentation()
    {
        if (!presenting) return;
        falling = false;
        SetFraming(0f);
        cameraFollow.ClearCinematicFocus();
        if (art != null) art.EntranceCompression = 0f;
        if (audioSource != null) audioSource.Stop();
        hud.SetActive(false);
        shadow.gameObject.SetActive(false);
        titleGroup.alpha = 0f;
        presenting = false;
    }

    private void BuildHud()
    {
        if (hud != null) return;
        hud = new GameObject("Boss Entrance HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        dim = Panel("Dim", hud.transform, Vector2.zero, Vector2.one, Color.clear);
        topBar = Panel("Top Letterbox", hud.transform, Vector2.up, Vector2.one, Color.black).rectTransform;
        bottomBar = Panel("Bottom Letterbox", hud.transform, Vector2.zero, Vector2.right, Color.black).rectTransform;
        titleRect = Rect("Boss Title", hud.transform, new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.35f));
        titleGroup = titleRect.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.blocksRaycasts = false;
        titleGroup.interactable = false;
        Label("Epithet", "중장갑 전투 병기", 24, new Color(1f, 0.65f, 0.3f), new Vector2(0f, 0.72f), Vector2.one);
        Text name = Label("Name", boss.DisplayName, 76, new Color(1f, 0.96f, 0.86f), new Vector2(0f, 0.12f), new Vector2(1f, 0.74f));
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 28;
        name.resizeTextMaxSize = 76;
        var outline = name.gameObject.AddComponent<Shadow>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -4f);
        Panel("Title Accent", titleRect, new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.065f), new Color(1f, 0.5f, 0.2f));
    }

    private Text Label(string objectName, string text, int size, Color color, Vector2 min, Vector2 max)
    {
        var label = Rect(objectName, titleRect, min, max).gameObject.AddComponent<Text>();
        label.text = text;
        label.font = boss.NameFont;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private void BuildShadow()
    {
        if (shadow != null) return;
        shadowTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * 32f) / 32f;
                pixels[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - radius) * 3f));
            }
        shadowTexture.SetPixels(pixels);
        shadowTexture.Apply();
        shadow = new GameObject("Boss Landing Shadow", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        shadow.transform.SetParent(transform, false);
        shadow.transform.position = landingPoint.position + Vector3.up * 0.025f;
        shadow.transform.localScale = new Vector3(2.5f, 0.15f, 1f);
        shadow.sprite = Sprite.Create(shadowTexture, new Rect(0f, 0f, 64f, 64f), Vector2.one * 0.5f, 64f);
        var renderer = boss.GetComponent<SpriteRenderer>();
        shadow.sortingLayerID = renderer.sortingLayerID;
        shadow.sortingOrder = renderer.sortingOrder - 1;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    private void OnDisable() => ResetPresentation();

    private void OnDestroy()
    {
        if (shadow != null) Destroy(shadow.sprite);
        if (shadowTexture != null) Destroy(shadowTexture);
    }
}
