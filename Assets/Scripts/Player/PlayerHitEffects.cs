using UnityEngine;

[RequireComponent(typeof(PlayerAttack))]
public class PlayerHitEffects : MonoBehaviour
{
    [SerializeField] private BluntHitBurst hitEffectPrefab;
    [Tooltip("1, 2타 이펙트 크기 배율")]
    [SerializeField, Min(0.01f)] private float basicScale = 1f;
    [Tooltip("3타 이펙트 크기 배율")]
    [SerializeField, Min(0.01f)] private float thirdScale = 1.5f;
    [Tooltip("플레이어보다 앞에 표시할 순서 차이")]
    [SerializeField] private int sortingOrderOffset = 8;

    private PlayerAttack attack;
    private SpriteRenderer playerSprite;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        playerSprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => attack.HitConfirmed += OnHit;
    private void OnDisable() => attack.HitConfirmed -= OnHit;

    // 적마다 명중한 위치에 생성하고, 무기나 적의 이동과 분리
    private void OnHit(Vector2 point, Vector2 direction, int comboStep)
    {
        if (hitEffectPrefab == null) return;
        BluntHitBurst effect = Instantiate(hitEffectPrefab, point, Quaternion.identity);
        effect.Play(direction, comboStep == 3 ? thirdScale : basicScale,
            playerSprite != null ? playerSprite.sortingLayerID : 0,
            (playerSprite != null ? playerSprite.sortingOrder : 0) + sortingOrderOffset);
    }
}
