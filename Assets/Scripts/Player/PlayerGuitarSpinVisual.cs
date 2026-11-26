using UnityEngine;

[DefaultExecutionOrder(100), DisallowMultipleComponent]
[RequireComponent(typeof(PlayerGuitarSpin), typeof(SpriteRenderer))]
public class PlayerGuitarSpinVisual : MonoBehaviour
{
    [SerializeField] private Sprite guitarSprite;
    [SerializeField] private Sprite noteSprite;
    [Tooltip("기존 휘두르기 중간 프레임을 반복해 몸체 회전 표현")]
    [SerializeField] private Sprite[] spinFrames;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private BluntHitBurst hitEffect;
    [Tooltip("초당 회전 수. 물리 몸체와 콜라이더는 회전시키지 않음")]
    [SerializeField, Min(0.1f)] private float turnsPerSecond = 2.5f;
    [SerializeField] private Color stormColor = new Color(0.55f, 0.72f, 1f, 0.75f);

    private const int ArcSegments = 32;
    private PlayerGuitarSpin spin;
    private SpriteRenderer character, guitar;
    private SpriteRenderer[] notes;
    private LineRenderer[] arcs;
    private GameObject visualRoot;
    private Sprite previousSprite;
    private bool previousFlip, showing;
    private float elapsed;

    private void Awake()
    {
        spin = GetComponent<PlayerGuitarSpin>();
        character = GetComponent<SpriteRenderer>();
        visualRoot = new GameObject("Guitar Spin Storm");
        visualRoot.transform.SetParent(transform, false);
        guitar = MakeSprite("Spinning Guitar", guitarSprite);
        notes = new SpriteRenderer[6];
        for (int i = 0; i < notes.Length; i++)
        {
            notes[i] = MakeSprite("Orbit Note " + i, noteSprite);
            notes[i].color = stormColor;
        }
        arcs = new LineRenderer[3];
        for (int i = 0; i < arcs.Length; i++)
        {
            var arc = new GameObject("Storm Arc " + i, typeof(LineRenderer)).GetComponent<LineRenderer>();
            arc.transform.SetParent(visualRoot.transform, false);
            arc.sharedMaterial = trailMaterial;
            arc.useWorldSpace = true;
            arc.positionCount = ArcSegments + 1;
            arc.startWidth = 0.065f;
            arc.endWidth = 0.01f;
            arc.startColor = stormColor;
            arc.endColor = new Color(stormColor.r, stormColor.g, stormColor.b, 0f);
            arc.sortingLayerID = character.sortingLayerID;
            arc.sortingOrder = character.sortingOrder + 3;
            arcs[i] = arc;
        }
        visualRoot.SetActive(false);
    }

    private SpriteRenderer MakeSprite(string name, Sprite sprite)
    {
        var result = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        result.transform.SetParent(visualRoot.transform, false);
        result.sprite = sprite;
        result.sharedMaterial = character.sharedMaterial;
        result.sortingLayerID = character.sortingLayerID;
        result.sortingOrder = character.sortingOrder + 5;
        return result;
    }

    private void OnEnable() => spin.HitConfirmed += OnHit;

    private void OnDisable()
    {
        spin.HitConfirmed -= OnHit;
        Hide();
    }

    private void OnHit(Vector2 point, Vector2 direction)
    {
        if (hitEffect == null) return;
        BluntHitBurst burst = Instantiate(hitEffect, point, Quaternion.identity);
        burst.Play(direction, 0.35f, character.sortingLayerID, character.sortingOrder + 12);
    }

    private void LateUpdate()
    {
        if (!spin.IsSpinning) { Hide(); return; }
        if (!showing)
        {
            previousSprite = character.sprite;
            previousFlip = character.flipX;
            showing = true;
            elapsed = 0f;
            visualRoot.SetActive(true);
        }
        elapsed += Time.deltaTime;
        float rotation = elapsed * Mathf.Max(0.1f, turnsPerSecond) * Mathf.PI * 2f;
        Vector2 center = spin.Center;
        float radius = spin.AttackRadius;
        // 시각 프레임과 무기만 돌려 피격 판정·체력바의 위치는 유지한다.
        if (spinFrames != null && spinFrames.Length > 0)
            character.sprite = spinFrames[(int)(elapsed * 20f) % spinFrames.Length];
        character.flipX = Mathf.Cos(rotation) < 0f;

        Vector2 radial = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
        guitar.transform.position = center + radial * (radius * 0.28f);
        guitar.transform.rotation = Quaternion.Euler(0f, 0f, rotation * Mathf.Rad2Deg);
        ScaleToWidth(guitar, radius * 1.3f);
        guitar.sortingOrder = character.sortingOrder + (radial.y > 0f ? -1 : 6);
        for (int i = 0; i < notes.Length; i++)
        {
            float angle = -rotation + i * Mathf.PI * 2f / notes.Length;
            float orbit = radius * (0.65f + 0.3f * (i % 2));
            notes[i].transform.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbit;
            notes[i].transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(angle) * 25f);
            ScaleToWidth(notes[i], 0.18f);
        }
        for (int i = 0; i < arcs.Length; i++)
        {
            arcs[i].enabled = trailMaterial != null;
            for (int j = 0; j <= ArcSegments; j++)
            {
                float angle = rotation + i * Mathf.PI * 2f / arcs.Length - j * 2.4f / ArcSegments;
                float orbit = radius * (0.74f + 0.12f * i);
                arcs[i].SetPosition(j, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbit);
            }
        }
    }

    private static void ScaleToWidth(SpriteRenderer sprite, float width)
    {
        if (sprite.sprite == null) return;
        float size = width / Mathf.Max(0.01f, sprite.sprite.bounds.size.x);
        // 부모 크기와 무관하게 공격 반지름과 같은 월드 크기로 맞춘다.
        Vector3 parentScale = sprite.transform.parent.lossyScale;
        sprite.transform.localScale = new Vector3(size / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            size / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
    }

    private void Hide()
    {
        if (!showing) return;
        showing = false;
        if (character != null)
        {
            character.sprite = previousSprite;
            character.flipX = previousFlip;
        }
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    private void OnDestroy() { if (visualRoot != null) Destroy(visualRoot); }
}
