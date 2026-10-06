using UnityEngine;

// 공격으로 피해를 받을 수 있는 대상
public abstract class Damageable : MonoBehaviour
{
    public abstract bool TryTakeDamage(float damage);
}
