using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    private PlayerAttack owner;
    private BoxCollider2D hitbox;

    private void Awake()
    {
        owner = GetComponentInParent<PlayerAttack>();
        hitbox = GetComponent<BoxCollider2D>();
    }

    // 이미 겹친 상태에서 공격해도 판정
    private void OnTriggerEnter2D(Collider2D other) => Hit(other);
    private void OnTriggerStay2D(Collider2D other) => Hit(other);

    private void Hit(Collider2D other)
    {
        if (isActiveAndEnabled && owner != null)
            owner.TryHit(hitbox, other);
    }
}
