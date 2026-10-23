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
    [Header("Swing / Guitar Resonance / Landing")]
    [SerializeField] private AudioClip swingClip;
    [SerializeField, Range(0f, 1f)] private float swingVolume = 0.12f;
    [SerializeField] private AudioClip resonanceClip;
    [SerializeField, Range(0f, 1f)] private float resonanceVolume = 0.18f;
    [SerializeField] private AudioClip landingClip;
    [SerializeField, Range(0f, 1f)] private float landingVolume = 0.3f;

    private PlayerAttack attack;
    private AudioSource hitSource;
    private AudioSource swingSource;
    private AudioSource resonanceSource;
    private AudioSource landingSource;
    private double lastHitAt = double.NegativeInfinity;
    private bool landingPending;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        hitSource = CreateSource();
        swingSource = CreateSource();
        resonanceSource = CreateSource();
        landingSource = CreateSource();
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void OnEnable()
    {
        attack.HitConfirmed += OnHit;
        attack.SwingStarted += OnSwing;
        attack.SlamLanded += OnLanding;
    }

    private void OnDisable()
    {
        attack.HitConfirmed -= OnHit;
        attack.SwingStarted -= OnSwing;
        attack.SlamLanded -= OnLanding;
        hitSource.Stop();
        swingSource.Stop();
        resonanceSource.Stop();
        landingSource.Stop();
        lastHitAt = double.NegativeInfinity;
        landingPending = false;
    }

    private void OnDestroy()
    {
        if (hitSource != null) Destroy(hitSource);
        if (swingSource != null) Destroy(swingSource);
        if (resonanceSource != null) Destroy(resonanceSource);
        if (landingSource != null) Destroy(landingSource);
    }

    private void OnSwing(int comboStep)
    {
        swingSource.pitch = comboStep == 3 ? 0.85f : 1f;
        if (swingClip != null && swingVolume > 0f)
            swingSource.PlayOneShot(swingClip, swingVolume);
    }

    private void OnLanding(Vector2 point)
    {
        landingPending = true;
    }

    private void LateUpdate()
    {
        if (!landingPending) return;
        landingPending = false;
        // 명중과 착지가 겹쳐도 타격음 위에 큰 소리를 중복하지 않음.
        float volume = landingVolume * (Time.realtimeSinceStartupAsDouble - lastHitAt < 0.12 ? 0.4f : 1f);
        if (landingClip != null && volume > 0f)
            landingSource.PlayOneShot(landingClip, volume);
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep)
    {
        double now = Time.realtimeSinceStartupAsDouble;
        // 한 번의 휘두르기로 여러 대상을 맞혀도 소리가 겹쳐 폭증하지 않음.
        if (now - lastHitAt < 0.025) return;
        lastHitAt = now;
        AudioClip clip = comboStep == 3 ? thirdHitClip : basicHitClip;
        float volume = Mathf.Clamp01(comboStep == 3 ? thirdVolume : basicVolume);
        // 실제 명중 시에만 재생. 히트스탑 중에도 소리는 끝까지 유지
        if (clip != null && volume > 0f)
            hitSource.PlayOneShot(clip, volume);
        if (comboStep == 3 && resonanceClip != null && resonanceVolume > 0f)
            resonanceSource.PlayOneShot(resonanceClip, resonanceVolume);
    }
}
