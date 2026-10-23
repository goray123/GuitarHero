using UnityEngine;

// 매 프레임 자신의 오프셋을 먼저 제거해 카메라 기준 위치가 누적되지 않게 함.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerHitCameraShake : MonoBehaviour
{
    [SerializeField, Min(0f)] private float basicAmplitude = 0.025f;
    [SerializeField, Min(0f)] private float thirdAmplitude = 0.055f;
    [SerializeField, Min(0f)] private float landingAmplitude = 0.065f;
    [SerializeField, Min(0.01f)] private float duration = 0.14f;

    private PlayerAttack attack;
    private Transform cameraTransform;
    private Vector3 appliedOffset;
    private Vector2 shakeDirection;
    private float amplitude;
    private float elapsed;

    private void Awake() => attack = GetComponent<PlayerAttack>();

    private void OnEnable()
    {
        attack.HitConfirmed += OnHit;
        attack.SlamLanded += OnLanding;
    }

    private void OnDisable()
    {
        attack.HitConfirmed -= OnHit;
        attack.SlamLanded -= OnLanding;
        RemoveOffset();
        amplitude = 0f;
    }

    private void OnHit(Vector2 point, Vector2 direction, int comboStep) =>
        Kick(direction, comboStep == 3 ? thirdAmplitude : basicAmplitude);

    private void OnLanding(Vector2 point) => Kick(Vector2.down, landingAmplitude);

    private void Kick(Vector2 direction, float strength)
    {
        if (strength <= 0f) return;
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            cameraTransform = mainCamera.transform;
        }
        // 다중 명중과 같은 순간의 착지 충격을 더하지 않고 강한 쪽만 사용.
        if (amplitude > 0f && elapsed < 0.05f && strength <= amplitude) return;
        shakeDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        amplitude = strength;
        elapsed = 0f;
    }

    private void Update() => RemoveOffset();

    private void LateUpdate()
    {
        if (cameraTransform == null || amplitude <= 0f) return;
        float lifetime = Mathf.Max(0.01f, duration);
        float t = Mathf.Clamp01(elapsed / lifetime);
        Vector2 perpendicular = new Vector2(-shakeDirection.y, shakeDirection.x);
        Vector2 offset = (shakeDirection * Mathf.Cos(t * Mathf.PI * 5f) +
            perpendicular * (Mathf.Sin(t * Mathf.PI * 7f) * 0.2f)) * (amplitude * (1f - t) * (1f - t));
        appliedOffset = offset;
        cameraTransform.localPosition += appliedOffset;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= lifetime) amplitude = 0f;
    }

    private void RemoveOffset()
    {
        if (cameraTransform != null) cameraTransform.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;
    }
}
