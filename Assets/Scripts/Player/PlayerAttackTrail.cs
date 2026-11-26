using UnityEngine;

[DefaultExecutionOrder(150), DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack), typeof(SpriteRenderer))]
public sealed class PlayerAttackTrail : MonoBehaviour
{
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Color basicColor = new Color(1f, 0.66f, 0.3f, 0.75f);
    [SerializeField] private Color thirdColor = new Color(1f, 0.42f, 0.18f, 0.85f);
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.07f;
    [SerializeField, Min(0.01f)] private float hitPulseDuration = 0.045f;
    [SerializeField] private int sortingOrderOffset = 4;

    private const int Segments = 32;
    private static readonly float[] Radii = { 0.2f, 0.55f, 0.82f, 0.96f, 1f };
    private static readonly float[] Opacities = { 0f, 0.18f, 0.5f, 1f, 0f };
    private readonly Vector3[] vertices = new Vector3[(Segments + 1) * Radii.Length];
    private readonly Color[] colors = new Color[(Segments + 1) * Radii.Length];
    private PlayerAttack attack;
    private SpriteRenderer playerSprite;
    private Mesh mesh;
    private MeshRenderer trailRenderer;
    private BoxCollider2D hitbox;
    private Color swingColor;
    private float fadeElapsed;
    private float fadeTime;
    private double pulseStartedAt = double.NegativeInfinity;
    private bool reverse;
    private bool playing;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        playerSprite = GetComponent<SpriteRenderer>();
        var child = new GameObject("Attack Range Trail", typeof(MeshFilter), typeof(MeshRenderer));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Player Attack Range Trail" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        var uv = new Vector2[vertices.Length];
        for (int i = 0; i < uv.Length; i++) uv[i] = Vector2.one * 0.5f;
        mesh.uv = uv;
        var triangles = new int[Segments * (Radii.Length - 1) * 6];
        int index = 0;
        for (int ring = 0; ring < Radii.Length - 1; ring++)
        for (int segment = 0; segment < Segments; segment++)
        {
            int a = ring * (Segments + 1) + segment;
            int b = a + Segments + 1;
            triangles[index++] = a;
            triangles[index++] = b;
            triangles[index++] = a + 1;
            triangles[index++] = a + 1;
            triangles[index++] = b;
            triangles[index++] = b + 1;
        }
        mesh.triangles = triangles;
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        trailRenderer = child.GetComponent<MeshRenderer>();
        trailRenderer.sharedMaterial = trailMaterial;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.enabled = false;
    }

    private void OnEnable()
    {
        attack.SwingStarted += OnSwing;
        attack.HitConfirmed += OnHit;
    }

    private void OnDisable()
    {
        attack.SwingStarted -= OnSwing;
        attack.HitConfirmed -= OnHit;
        Hide();
    }

    private void OnSwing(int comboStep)
    {
        hitbox = attack.CurrentAttackHitbox;
        if (hitbox == null || trailMaterial == null)
        {
            Hide();
            return;
        }
        trailRenderer.sharedMaterial = trailMaterial;
        trailRenderer.transform.SetParent(hitbox.transform, false);
        trailRenderer.transform.localPosition = hitbox.offset;
        Vector2 direction = attack.SwingDirection;
        for (int ring = 0; ring < Radii.Length; ring++)
        for (int segment = 0; segment <= Segments; segment++)
        {
            float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, segment / (float)Segments);
            Vector2 point = new Vector2(-1f + 2f * Radii[ring] * Mathf.Cos(angle), Radii[ring] * Mathf.Sin(angle));
            if (direction.y != 0f) point = new Vector2(-direction.y * point.y, direction.y * point.x);
            vertices[ring * (Segments + 1) + segment] = Vector2.Scale(point, hitbox.size * 0.5f);
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        swingColor = comboStep == 3 ? thirdColor : basicColor;
        reverse = comboStep == 2;
        fadeElapsed = 0f;
        fadeTime = fadeDuration / Mathf.Max(0.01f, attack.CurrentAttackSpeedMultiplier);
        pulseStartedAt = double.NegativeInfinity;
        playing = true;
        LateUpdate();
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep)
    {
        if (playing) pulseStartedAt = Time.unscaledTimeAsDouble;
    }

    private void LateUpdate()
    {
        if (!playing) return;
        if (!attack.isActiveAndEnabled || !attack.IsAttacking || attack.CurrentAttackHitbox != hitbox)
        {
            Hide();
            return;
        }
        float progress = attack.SwingProgress;
        if (progress >= 1f) fadeElapsed += Time.deltaTime;
        float opacity = 1f - Mathf.Clamp01(fadeElapsed / Mathf.Max(0.01f, fadeTime));
        // The sweep freezes with the pose; its brief impact pulse finishes in real time.
        float pulse = 1f - Mathf.Clamp01((float)(Time.unscaledTimeAsDouble - pulseStartedAt) / Mathf.Max(0.01f, hitPulseDuration));
        float head = reverse ? 1f - progress : progress;
        for (int ring = 0; ring < Radii.Length; ring++)
        for (int segment = 0; segment <= Segments; segment++)
        {
            float position = segment / (float)Segments;
            float taper = Mathf.Clamp01(Mathf.Min(position, 1f - position) * 8f);
            float sweep = 0.5f + 0.5f * (1f - Mathf.Clamp01(Mathf.Abs(position - head) * 3f));
            Color color = Color.Lerp(swingColor, new Color(1f, 0.96f, 0.82f, 1f), pulse);
            color.a = Opacities[ring] * taper * Mathf.Lerp(swingColor.a * sweep * opacity, 0.95f, pulse);
            colors[ring * (Segments + 1) + segment] = color;
        }
        mesh.colors = colors;
        trailRenderer.sortingLayerID = playerSprite.sortingLayerID;
        trailRenderer.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
        trailRenderer.enabled = (opacity > 0f || pulse > 0f) && playerSprite.enabled && !playerSprite.forceRenderingOff;
    }

    private void Hide()
    {
        playing = false;
        if (trailRenderer != null) trailRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (trailRenderer != null) Destroy(trailRenderer.gameObject);
        if (mesh != null) Destroy(mesh);
    }
}
