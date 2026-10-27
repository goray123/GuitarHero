using UnityEngine;

public readonly struct AttackHit
{
    public float Damage { get; }
    public Vector2 Point { get; }
    public Vector2 DisplayPoint { get; }
    public Vector2 Direction { get; }
    public int ComboStep { get; }

    public AttackHit(float damage, Vector2 point, Vector2 direction, int comboStep)
        : this(damage, point, point, direction, comboStep)
    {
    }

    public AttackHit(float damage, Vector2 point, Vector2 displayPoint, Vector2 direction, int comboStep)
    {
        Damage = damage;
        Point = point;
        DisplayPoint = displayPoint;
        Direction = direction;
        ComboStep = comboStep;
    }
}
