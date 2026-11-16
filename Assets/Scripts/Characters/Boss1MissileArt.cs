using System.Collections.Generic;
using UnityEngine;

// 판정 시간과 독립된 표현 계층. 연기가 남아도 추가 피해는 발생하지 않는다.
[DefaultExecutionOrder(100), DisallowMultipleComponent]
public class Boss1MissileArt : MonoBehaviour
{
    [SerializeField] private Sprite missileSprite;
    [SerializeField] private Sprite[] explosionFrames;
    [SerializeField, Min(0.1f)] private float explosionDuration = 1f;
    [Tooltip("불꽃을 포함한 미사일 전체 길이 (H)")]
    [SerializeField, Min(0.1f)] private float missileLengthH = 0.65f;
    [Tooltip("프레임 안 여백을 제외한 최대 폭발의 지름 비율")]
    [SerializeField, Range(0.1f, 1f)] private float peakFrameCoverage = 0.65f;
    private Boss1ProximityAttack owner;
    private Boss1RearMissiles rear;
    private Boss1AimedMissile aimed;
    private readonly SpriteRenderer[] rockets = new SpriteRenderer[3];
    private readonly List<Burst> bursts = new List<Burst>();
    private bool rearWasExploding, aimedWasExploding;
    public bool HasMissileArt => isActiveAndEnabled && missileSprite != null;
    public bool HasExplosionArt => isActiveAndEnabled && explosionFrames != null && explosionFrames.Length == 16;
    public bool HasArt => HasMissileArt && HasExplosionArt;
    public int ActiveBursts => bursts.Count;
    private sealed class Burst { public SpriteRenderer renderer; public float age; }

    private void Awake() => Initialize();
    private void OnEnable() => Initialize();

    // Play Mode 중 스크립트 재로딩 시 Awake가 다시 호출되지 않아도 아트를 복구한다.
    private void Initialize()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        rear = GetComponent<Boss1RearMissiles>();
        aimed = GetComponent<Boss1AimedMissile>();
        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i] != null) continue;
            var existing = transform.Find("Missile Art " + i);
            rockets[i] = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (rockets[i] == null) rockets[i] = CreateRenderer("Missile Art " + i, 24);
            rockets[i].sprite = missileSprite;
        }
    }

    private SpriteRenderer CreateRenderer(string label, int order)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    private void FixedUpdate()
    {
        if (owner == null || !owner.isActiveAndEnabled) { Clear(); return; }
        AdvanceBursts(Time.fixedDeltaTime);
        bool rearExploding = rear != null && rear.isActiveAndEnabled && rear.Exploding;
        bool aimedExploding = aimed != null && aimed.isActiveAndEnabled && aimed.CurrentPhase == Boss1AimedMissile.Phase.Exploding;
        if (rearExploding && !rearWasExploding)
            for (int i = 0; i < 2; i++) PlayExplosion(rear.Target(i), rear.Radius);
        if (aimedExploding && !aimedWasExploding) PlayExplosion(aimed.ExplosionPoint, aimed.ExplosionRadius);
        rearWasExploding = rearExploding;
        aimedWasExploding = aimedExploding;
    }

    private void LateUpdate()
    {
        if (owner == null || !owner.isActiveAndEnabled) { Clear(); return; }
        if (!HasMissileArt)
        {
            foreach (var rocket in rockets) if (rocket != null) rocket.enabled = false;
            return;
        }
        for (int i = 0; i < 2; i++)
        {
            bool flying = rear != null && rear.isActiveAndEnabled && rear.InFlight;
            rockets[i].enabled = flying;
            if (!flying) continue;
            Vector2 tangent = rear.Target(i) - rear.LaunchPoint + Vector2.up * (4f * rear.Radius * (1f - 2f * rear.FlightProgress));
            PoseRocket(rockets[i], rear.MissilePosition(i), tangent);
        }
        rockets[2].enabled = aimed != null && aimed.isActiveAndEnabled && aimed.CurrentPhase == Boss1AimedMissile.Phase.Flying;
        if (rockets[2].enabled) PoseRocket(rockets[2], aimed.ProjectilePosition, aimed.Direction);
    }

    public void PoseRocket(SpriteRenderer renderer, Vector2 position, Vector2 direction)
    {
        if (missileSprite == null || owner == null) { renderer.enabled = false; return; }
        renderer.sprite = missileSprite;
        renderer.transform.position = position;
        renderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        float length = missileLengthH * owner.Height * Mathf.Abs(transform.lossyScale.y);
        SetWorldScale(renderer.transform, length / Mathf.Max(0.01f, missileSprite.bounds.size.x));
    }

    public void PlayExplosion(Vector2 point, float radius)
    {
        if (!HasExplosionArt) return;
        var renderer = CreateRenderer("Missile Explosion Art", 25);
        // 보스의 이동/뒤집기에 폭발 위치가 끌려가지 않게 분리하고 직접 정리한다.
        renderer.transform.SetParent(null, true);
        renderer.transform.position = point;
        renderer.transform.rotation = Quaternion.identity;
        renderer.sprite = explosionFrames[0];
        float width = explosionFrames[4].rect.width / explosionFrames[4].pixelsPerUnit;
        renderer.transform.localScale = Vector3.one * (radius * 2f / (width * peakFrameCoverage));
        renderer.enabled = true;
        bursts.Add(new Burst { renderer = renderer });
    }

    private void AdvanceBursts(float dt)
    {
        for (int i = bursts.Count - 1; i >= 0; i--)
        {
            var burst = bursts[i];
            burst.age += dt;
            if (burst.age >= explosionDuration)
            {
                Release(burst.renderer.gameObject); bursts.RemoveAt(i); continue;
            }
            int index = Mathf.Min(15, Mathf.FloorToInt(burst.age / explosionDuration * 16f));
            burst.renderer.sprite = explosionFrames[index];
        }
    }

    private void SetWorldScale(Transform target, float scale)
    {
        target.localScale = new Vector3(scale / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y)), 1f);
    }

    private void Clear()
    {
        foreach (var rocket in rockets) if (rocket != null) rocket.enabled = false;
        foreach (var burst in bursts) if (burst.renderer != null) Release(burst.renderer.gameObject);
        bursts.Clear(); rearWasExploding = aimedWasExploding = false;
    }
    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        Clear();
        foreach (var rocket in rockets) if (rocket != null) Release(rocket.gameObject);
    }
    private static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
}
