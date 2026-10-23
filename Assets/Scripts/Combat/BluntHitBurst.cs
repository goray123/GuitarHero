using UnityEngine;

// 균열과 파편이 있는 픽셀 프레임으로 둔기 충격 재생
public class BluntHitBurst : MonoBehaviour
{
    [Tooltip("파쇄 이펙트 프레임. 재생 순서대로 연결")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Material effectMaterial;
    [Tooltip("전체 이펙트 지속 시간")]
    [SerializeField, Min(0.05f)] private float duration = 0.28f;
    [Tooltip("기본 크기 배율. 1이면 원본 32 PPU 크기")]
    [SerializeField, Min(0.01f)] private float size = 1f;
    [Tooltip("처음 두 섬광 프레임은 히트스탑 중에도 진행")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.035f;
    private SpriteRenderer effectRenderer;
    private float elapsed;
    private float flashElapsed;
    private float lifetime;
    private bool playing;

    public void Play(Vector2 direction, float scale, int sortingLayer, int sortingOrder, bool groundShock = false)
    {
        if (frames == null || frames.Length == 0 || effectMaterial == null)
        {
            Destroy(gameObject);
            return;
        }
        elapsed = 0f;
        flashElapsed = 0f;
        lifetime = Mathf.Max(0.05f, groundShock ? duration * 0.7f : duration);
        effectRenderer = GetComponent<SpriteRenderer>();
        if (effectRenderer == null) effectRenderer = gameObject.AddComponent<SpriteRenderer>();
        effectRenderer.sharedMaterial = effectMaterial;
        effectRenderer.sortingLayerID = sortingLayer;
        effectRenderer.sortingOrder = sortingOrder;
        // 좌우와 낙하 방향에 맞춰 회전
        float angle = direction.sqrMagnitude > 0f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one * Mathf.Max(.01f, size * scale);
        if (groundShock)
        {
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(1.8f, 0.55f, 1f));
            effectRenderer.color = new Color(0.8f, 0.65f, 0.45f, 0.75f);
        }
        else
            effectRenderer.color = Color.white;
        effectRenderer.sprite = frames[0];
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        int flashFrames = Mathf.Min(2, frames.Length);
        float flashTime = Mathf.Clamp(flashDuration, 0.001f, lifetime * 0.5f);
        if (flashElapsed < flashTime)
        {
            flashElapsed = Mathf.Min(flashTime, flashElapsed + Time.unscaledDeltaTime);
            effectRenderer.sprite = frames[Mathf.Min(flashFrames - 1, (int)(flashElapsed / flashTime * flashFrames))];
            return;
        }
        // 섬광 뒤의 파편은 캐릭터와 같은 게임 시간으로 흩어짐.
        if (Time.timeScale > 0f) elapsed += Time.deltaTime;
        float tailTime = lifetime - flashTime;
        if (elapsed >= tailTime)
        {
            Destroy(gameObject);
            return;
        }
        if (effectRenderer != null)
            effectRenderer.sprite = frames[Mathf.Min(frames.Length - 1,
                flashFrames + (int)(elapsed / tailTime * (frames.Length - flashFrames)))];
    }
}
