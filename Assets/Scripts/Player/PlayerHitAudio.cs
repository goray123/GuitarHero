using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerHitAudio : MonoBehaviour
{
    [Tooltip("1, 2타 명중 시 재생할 원본 타격음")]
    [SerializeField] private AudioClip basicHitClip;
    [Tooltip("1, 2타 타격음 볼륨. 0이면 음소거")]
    [SerializeField, Range(0f, 1f)] private float basicVolume = 1f;
    [Tooltip("3타 명중 시 재생할 가공 타격음")]
    [SerializeField] private AudioClip thirdHitClip;
    [Tooltip("3타 타격음 볼륨. 0이면 음소거")]
    [SerializeField, Range(0f, 1f)] private float thirdVolume = 1f;

    private PlayerAttack attack;
    private AudioSource hitSource;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        // 타격음 전용 2D 음원. 카메라 거리와 무관하게 재생
        hitSource = gameObject.AddComponent<AudioSource>();
        hitSource.playOnAwake = false;
        hitSource.loop = false;
        hitSource.spatialBlend = 0f;
        hitSource.dopplerLevel = 0f;
    }

    private void OnEnable() => attack.HitConfirmed += OnHit;

    private void OnDisable()
    {
        attack.HitConfirmed -= OnHit;
        hitSource.Stop();
    }

    private void OnDestroy()
    {
        if (hitSource != null) Destroy(hitSource);
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep)
    {
        AudioClip clip = comboStep == 3 ? thirdHitClip : basicHitClip;
        float volume = Mathf.Clamp01(comboStep == 3 ? thirdVolume : basicVolume);
        // 실제 명중 시에만 재생. 히트스탑 중에도 소리는 끝까지 유지
        if (clip != null && volume > 0f)
            hitSource.PlayOneShot(clip, volume);
    }
}
