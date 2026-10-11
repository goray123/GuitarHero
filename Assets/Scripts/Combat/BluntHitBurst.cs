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
    private SpriteRenderer effectRenderer;
    private float elapsed;

    public void Play(Vector2 direction, float scale, int sortingLayer, int sortingOrder)
    {
        if (frames == null || frames.Length == 0 || effectMaterial == null)
        {
            Destroy(gameObject);
            return;
        }
        elapsed = 0f;
        effectRenderer = GetComponent<SpriteRenderer>();
        if (effectRenderer == null) effectRenderer = gameObject.AddComponent<SpriteRenderer>();
        effectRenderer.sharedMaterial = effectMaterial;
        effectRenderer.sortingLayerID = sortingLayer;
        effectRenderer.sortingOrder = sortingOrder;
        // 좌우와 낙하 방향에 맞춰 회전
        float angle = direction.sqrMagnitude > 0f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one * Mathf.Max(.01f, size * scale);
        effectRenderer.sprite = frames[0];
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float lifetime = Mathf.Max(.05f, duration);
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        if (effectRenderer != null)
            effectRenderer.sprite = frames[Mathf.Min(frames.Length - 1, (int)(elapsed / lifetime * frames.Length))];
    }
}
