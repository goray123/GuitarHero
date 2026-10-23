using UnityEngine;

// 공격으로 피해를 받을 수 있는 대상
public abstract class Damageable : MonoBehaviour
{
    public abstract bool TryTakeDamage(float damage);

    // 기존 피해 호출도 유지하면서 피격 대상에 접촉 위치와 공격 강도를 전달.
    public virtual bool TryTakeDamage(AttackHit hit) => TryTakeDamage(hit.Damage);
}
