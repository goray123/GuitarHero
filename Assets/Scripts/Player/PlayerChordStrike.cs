using UnityEngine;
using UnityEngine.InputSystem;

// 일반 공격과 이동보다 먼저 스킬 시작과 종료 결정
[DefaultExecutionOrder(-20)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement))]
public class PlayerChordStrike : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private ChordNoteProjectile notePrefab;

    [Header("Recoil")]
    [Tooltip("바라보는 방향 반대로 이동할 거리. 벽에 막히면 줄어듦")]
    [SerializeField, Min(0f)] private float recoilDistance = 2f;
    [Tooltip("시작 위치 기준 포물선의 최대 높이")]
    [SerializeField, Min(0f)] private float recoilHeight = 0.65f;
    [Tooltip("반동 이동과 다른 행동 제한 시간")]
    [SerializeField, Min(0.02f)] private float recoilDuration = 0.4f;

    [Header("Notes")]
    [Tooltip("음표 하나의 데미지")]
    [SerializeField, Min(0f)] private float noteDamage = 5f;
    [Tooltip("음표가 곡선을 따라 이동할 최대 거리")]
    [SerializeField, Min(0.1f)] private float noteRange = 3f;
    [SerializeField, Min(0.1f)] private float noteSpeed = 10f;
    [Tooltip("다섯 음표가 퍼지는 전체 각도")]
    [SerializeField, Range(0f, 90f)] private float spreadAngle = 36f;
    [Tooltip("직선 경로에서 추가로 휘어지는 정도")]
    [SerializeField, Min(0f)] private float curveAmount = 0.3f;
    [Tooltip("플레이어 중심 기준 발사 위치. X는 바라보는 방향으로 적용")]
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.35f, 0.15f);
    [Tooltip("음표가 부딪힐 적과 지형 레이어")]
    [SerializeField] private LayerMask collisionLayers = ~0;

    private PlayerMovement movement;
    private PlayerAttack attack;
    private SpriteRenderer playerSprite;
    private Rigidbody2D body;
    private InputActionMap actions;
    private InputAction skillAction;
    private bool requested;
    private bool active;
    private float facing;
    private float startedAt;
    private float duration;
    private float distance;
    private float height;

    public bool BlocksActions => isActiveAndEnabled && (requested || active);

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        playerSprite = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        if (inputActions == null || notePrefab == null || playerSprite == null)
        {
            Debug.LogError("PlayerChordStrike의 Input Actions, Note Prefab, SpriteRenderer를 확인해주세요.", this);
            enabled = false;
            return;
        }
        actions = inputActions.FindActionMap("Player", true).Clone();
        skillAction = actions.FindAction("ChordStrike", true);
    }

    private void OnEnable()
    {
        if (skillAction == null) return;
        skillAction.performed += OnSkill;
        skillAction.Enable();
    }

    private void OnDisable()
    {
        if (skillAction != null)
        {
            skillAction.performed -= OnSkill;
            skillAction.Disable();
        }
        if (active && body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        active = false;
        requested = false;
    }

    private void OnDestroy() => actions?.Dispose();

    private void OnSkill(InputAction.CallbackContext context)
    {
        // 시전 중 누른 키는 저장하지 않음
        if (BlocksActions || Time.timeScale <= 0f || movement.IsSliding ||
            (attack != null && attack.IsAttacking)) return;
        facing = playerSprite.flipX ? -1f : 1f;
        requested = true;
    }

    private void FixedUpdate()
    {
        if (requested)
        {
            requested = false;
            if (movement.IsSliding || (attack != null && attack.IsAttacking)) return;
            active = true;
            startedAt = Time.time;
            duration = Mathf.Max(0.02f, recoilDuration);
            distance = Mathf.Max(0f, recoilDistance);
            height = Mathf.Max(0f, recoilHeight);
            movement.ClearActionRequests();
            FireNotes();
        }
        else if (active && Time.time >= startedAt + duration)
        {
            active = false;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
    }

    private void FireNotes()
    {
        Vector2 origin = body.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
        for (int i = 0; i < 5; i++)
        {
            float lane = (i - 2) / 2f;
            ChordNoteProjectile note = Instantiate(notePrefab, origin, Quaternion.identity);
            note.Launch(transform, facing, lane, spreadAngle, curveAmount,
                noteRange, noteSpeed, noteDamage, collisionLayers,
                playerSprite.sortingLayerID, playerSprite.sortingOrder + 10);
        }
    }

    public Vector2 GetRecoilVelocity(float currentTime, float dt)
    {
        dt = Mathf.Max(0.0001f, dt);
        float t0 = Mathf.Clamp01((currentTime - startedAt) / duration);
        float t1 = Mathf.Clamp01((currentTime + dt - startedAt) / duration);
        // 상승 후 원래 높이로 내려오는 포물선. 위치를 순간이동하지 않아 벽 충돌 유지
        float y0 = 4f * height * t0 * (1f - t0);
        float y1 = 4f * height * t1 * (1f - t1);
        return new Vector2(-facing * distance * (t1 - t0) / dt,
            (y1 - y0) / dt - Physics2D.gravity.y * body.gravityScale * dt);
    }
}
