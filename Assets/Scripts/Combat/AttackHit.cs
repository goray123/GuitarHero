using UnityEngine;

public readonly struct AttackHit
{
    public float Damage { get; }
    public Vector2 Point { get; }
    public Vector2 Direction { get; }
    public int ComboStep { get; }

    public AttackHit(float damage, Vector2 point, Vector2 direction, int comboStep)
    {
        Damage = damage;
        Point = point;
        Direction = direction;
        ComboStep = comboStep;
    }
}
