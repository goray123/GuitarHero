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

    [Header("Cooldown (Seconds)")]
    [Tooltip("각 스킬 발동 순간부터 다시 사용할 수 있을 때까지의 시간. A와 S는 별도 계산")]
    [SerializeField, Min(0f)] private float cooldownTime = 5f;

    [Header("A Skill: Recoil")]
    [Tooltip("바라보는 방향 반대로 이동할 거리. 벽에 막히면 줄어듦")]
    [SerializeField, Min(0f)] private float recoilDistance = 2f;
    [Tooltip("시작 위치 기준 포물선의 최대 높이")]
    [SerializeField, Min(0f)] private float recoilHeight = 0.65f;
    [Tooltip("반동 이동과 다른 행동 제한 시간")]
    [SerializeField, Min(0.02f)] private float recoilDuration = 0.4f;

    [Header("S Skill: Downward Chord")]
    [Tooltip("S스킬 시작 위치에서 제자리로 상승하는 높이. 보스1 부스터 돌진을 넘을 여유 포함")]
    [SerializeField, Min(0f)] private float downwardJumpHeight = 1.6f;
    [Tooltip("S스킬 최고점까지 걸리는 시간. 발사 직후 조작 가능")]
    [SerializeField, Min(0.02f)] private float downwardRiseTime = 0.26f;
    [Tooltip("바라보는 방향의 수평선에서 아래로 조준하는 각도")]
    [SerializeField, Range(0f, 90f)] private float downwardAimAngle = 60f;

    [Header("Notes")]
    [Tooltip("음표 하나의 데미지")]
    [SerializeField, Min(0f)] private float noteDamage = 5f;
    [Tooltip("음표가 곡선을 따라 이동할 최대 거리")]
    [SerializeField, Min(0.1f)] private float noteRange = 3f;
    [SerializeField, Min(0.1f)] private float noteSpeed = 10f;
    [Tooltip("음표가 퍼지는 전체 각도")]
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
    private InputAction downwardSkillAction;
    private bool requested;
    private bool active;
    private float facing;
    private float startedAt;
    private float duration;
    private float distance;
    private float height;
    private float nextUseTime;
    private float nextDownwardUseTime;
    private bool downward;
    private SpriteRenderer aimSprite;
    private MaterialPropertyBlock aimProperties;
    private bool aiming;
    private bool previousForceRenderingOff;
    private float aimEndTime;
    private const float AimRecoveryTime = 0.1f;

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
        downwardSkillAction = actions.FindAction("DownwardChord", true);
    }

    private void OnEnable()
    {
        if (skillAction == null) return;
        skillAction.performed += OnSkill;
        skillAction.Enable();
        downwardSkillAction.performed += OnDownwardSkill;
        downwardSkillAction.Enable();
    }

    private void OnDisable()
    {
        if (skillAction != null)
        {
            skillAction.performed -= OnSkill;
            skillAction.Disable();
            downwardSkillAction.performed -= OnDownwardSkill;
            downwardSkillAction.Disable();
        }
        if (active && body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        active = false;
        requested = false;
        EndAim();
    }

    private void OnDestroy() => actions?.Dispose();

    private void OnSkill(InputAction.CallbackContext context) => RequestSkill(false);

    private void OnDownwardSkill(InputAction.CallbackContext context) => RequestSkill(true);

    private void RequestSkill(bool useDownward)
    {
        // 시전 중 누른 키는 저장하지 않음
        if (BlocksActions || Time.time < (useDownward ? nextDownwardUseTime : nextUseTime) ||
            Time.timeScale <= 0f || movement.IsSliding ||
            (attack != null && attack.IsAttacking)) return;
        facing = playerSprite.flipX ? -1f : 1f;
        downward = useDownward;
        requested = true;
    }

    private void FixedUpdate() => UpdateSkill(Time.time);

    private void UpdateSkill(float currentTime)
    {
        if (requested)
        {
            requested = false;
            if (currentTime < (downward ? nextDownwardUseTime : nextUseTime) ||
                movement.IsSliding || (attack != null && attack.IsAttacking)) return;
            active = true;
            startedAt = currentTime;
            if (downward)
                nextDownwardUseTime = startedAt + Mathf.Max(0f, cooldownTime);
            else
                nextUseTime = startedAt + Mathf.Max(0f, cooldownTime);
            duration = downward
                ? Mathf.Ceil(Mathf.Max(Time.fixedDeltaTime, downwardRiseTime) / Time.fixedDeltaTime) * Time.fixedDeltaTime
                : Mathf.Max(0.02f, recoilDuration);
            distance = downward ? 0f : Mathf.Max(0f, recoilDistance);
            height = Mathf.Max(0f, downward ? downwardJumpHeight : recoilHeight);
            movement.ClearActionRequests();
            if (downward)
                BeginAim();
            else
                FireNotes();
        }
        else if (active && currentTime + 0.00001f >= startedAt + duration)
        {
            active = false;
            body.linearVelocity = new Vector2(0f, downward ? 0f : body.linearVelocity.y);
            if (downward)
            {
                FireNotes();
                aimEndTime = currentTime + AimRecoveryTime;
            }
        }
    }

    private void FireNotes()
    {
        float aim = downward ? -downwardAimAngle * Mathf.Deg2Rad : 0f;
        Vector2 offset = new Vector2(
            muzzleOffset.x * Mathf.Cos(aim) - muzzleOffset.y * Mathf.Sin(aim),
            muzzleOffset.x * Mathf.Sin(aim) + muzzleOffset.y * Mathf.Cos(aim));
        Vector2 origin = body.position + new Vector2(offset.x * facing, offset.y);
        int count = downward ? 3 : 5;
        for (int i = 0; i < count; i++)
        {
            float lane = (i - (count - 1) * 0.5f) / ((count - 1) * 0.5f);
            ChordNoteProjectile note = Instantiate(notePrefab, origin, Quaternion.identity);
            note.Launch(transform, facing, downward ? 0f : lane, downward ? 0f : spreadAngle,
                downward ? 0f : curveAmount,
                noteRange, noteSpeed, noteDamage, collisionLayers,
                playerSprite.sortingLayerID, playerSprite.sortingOrder + 10,
                downward ? downwardAimAngle + lane * spreadAngle * 0.5f : 0f);
        }
    }

    public Vector2 GetRecoilVelocity(float currentTime, float dt)
    {
        dt = Mathf.Max(0.0001f, dt);
        float t0 = Mathf.Clamp01((currentTime - startedAt) / duration);
        float t1 = Mathf.Clamp01((currentTime + dt - startedAt) / duration);
        // A는 왕복 포물선, S는 최고점까지만 상승. 속도로 이동해 벽 충돌 유지.
        float y0 = downward ? height * (2f * t0 - t0 * t0) : 4f * height * t0 * (1f - t0);
        float y1 = downward ? height * (2f * t1 - t1 * t1) : 4f * height * t1 * (1f - t1);
        return new Vector2(-facing * distance * (t1 - t0) / dt,
            (y1 - y0) / dt - Physics2D.gravity.y * body.gravityScale * dt);
    }

    private void BeginAim()
    {
        if (aimSprite == null)
        {
            var visual = new GameObject("S Skill Aim", typeof(SpriteRenderer));
            visual.transform.SetParent(transform, false);
            aimSprite = visual.GetComponent<SpriteRenderer>();
            aimProperties = new MaterialPropertyBlock();
        }
        if (!aiming) previousForceRenderingOff = playerSprite.forceRenderingOff;
        aiming = true;
        playerSprite.forceRenderingOff = true;
    }

    private void LateUpdate()
    {
        if (!aiming) return;
        if (!downward || (!active && Time.time >= aimEndTime)) { EndAim(); return; }
        // 시각만 기울여 몸체 콜라이더와 물리 회전은 유지.
        float tilt = active ? Mathf.SmoothStep(0f, 1f, (Time.time - startedAt) / duration)
            : Mathf.Clamp01((aimEndTime - Time.time) / AimRecoveryTime);
        aimSprite.transform.localRotation = Quaternion.Euler(0f, 0f, -facing * downwardAimAngle * tilt);
        aimSprite.sprite = playerSprite.sprite;
        aimSprite.flipX = facing < 0f;
        aimSprite.flipY = playerSprite.flipY;
        aimSprite.color = playerSprite.color;
        aimSprite.sharedMaterial = playerSprite.sharedMaterial;
        aimSprite.sortingLayerID = playerSprite.sortingLayerID;
        aimSprite.sortingOrder = playerSprite.sortingOrder;
        aimSprite.enabled = playerSprite.enabled && !previousForceRenderingOff;
        playerSprite.GetPropertyBlock(aimProperties);
        aimSprite.SetPropertyBlock(aimProperties);
    }

    private void EndAim()
    {
        if (!aiming) return;
        aiming = false;
        if (playerSprite != null) playerSprite.forceRenderingOff = previousForceRenderingOff;
        if (aimSprite != null) aimSprite.enabled = false;
    }
}
