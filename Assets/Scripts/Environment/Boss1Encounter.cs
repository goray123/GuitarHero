using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Boss1Encounter : MonoBehaviour
{
    [Header("맵 연결")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Boss1Health boss;
    [SerializeField] private Transform triggerPoint;
    [SerializeField] private Transform landingPoint;
    [SerializeField] private BoxCollider2D leftGate;
    [SerializeField] private BoxCollider2D rightGate;
    [SerializeField] private CityCameraFollow cameraFollow;
    [Header("카메라 이동과 보스 낙하")]
    [Tooltip("착지 지점에서 보스가 떨어질 높이")]
    [SerializeField, InspectorName("낙하 높이"), Min(1f)] private float dropHeight = 12f;
    [SerializeField, InspectorName("중앙 이동 시간 (초)"), Min(0f)] private float cameraPanDuration = 0.5f;
    [SerializeField, InspectorName("중앙 도착 후 낙하 대기 (초)"), Min(0f)] private float dropDelay = 1.3f;
    [SerializeField, InspectorName("낙하·착지 유지 스프라이트")] private Sprite entrancePose;
    [SerializeField, InspectorName("검은 띠 높이 (화면 비율)"), Range(0f, 0.25f)] private float letterboxHeight = 0.09f;
    [Header("착지 충격")]
    [SerializeField, InspectorName("슬로모션 배속"), Range(0.05f, 1f)] private float slowMotionScale = 0.25f;
    [SerializeField, InspectorName("슬로모션 시간 (초)"), Min(0f)] private float slowMotionDuration = 0.32f;
    [SerializeField, InspectorName("흔들림 강도"), Min(0f)] private float shakeStrength = 0.18f;
    [SerializeField, InspectorName("흔들림 시간 (초)"), Min(0.01f)] private float shakeDuration = 1.5f;
    [Header("조작 복귀와 전투 시작")]
    [SerializeField, InspectorName("검은 띠 퇴장 시간 (초)"), Min(0f)] private float bandExitDuration = 0.16f;
    [SerializeField, InspectorName("체력바 채움 시간 (초)"), Min(0.01f)] private float healthFillDuration = 1.5f;

    public enum Phase { Approach, Entrance, Combat, Cleared }
    public Phase CurrentPhase { get; private set; }

    private Vector2 approachCameraBounds;
    private Behaviour[] combat;
    private bool[] wasEnabled;
    private PlayerHitStop hitStop;
    private float originalScale;
    private float cinematicScale;
    private bool slowingTime;
    private Boss1SlamSpriteVisual bossVisual;
    private Behaviour[] controls;
    private bool[] controlStates;
    private Rigidbody2D playerBody;
    private RigidbodyConstraints2D playerConstraints;

    private void Start()
    {
        boss.gameObject.SetActive(false);
        SetGates(false);
        approachCameraBounds = cameraFollow.HorizontalBounds;
        hitStop = player.GetComponent<PlayerHitStop>();
        bossVisual = boss.GetComponent<Boss1SlamSpriteVisual>();
    }

    private void Update()
    {
        if (CurrentPhase == Phase.Approach && player.transform.position.x >= triggerPoint.position.x)
            StartCoroutine(Enter());
        else if (CurrentPhase == Phase.Combat && boss.IsDead)
        {
            CurrentPhase = Phase.Cleared;
            SetGates(false);
            cameraFollow.SetHorizontalBounds(approachCameraBounds.x, approachCameraBounds.y);
        }
    }

    private IEnumerator Enter()
    {
        CurrentPhase = Phase.Entrance;
        player.GetComponent<PlayerRhythmGauge>().ResetGauge();
        SetGates(true);
        // 셔터의 바깥 면까지 카메라에 담아 닫힌 경계를 볼 수 있게 한다.
        cameraFollow.SetHorizontalBounds(leftGate.bounds.min.x, rightGate.bounds.max.x);
        LockControls();

        // 외형만 먼저 활성화하고 착지가 끝난 뒤 기존 전투 컴포넌트를 복구한다.
        combat = new Behaviour[] {
            boss.GetComponent<Boss1ProximityAttack>(), boss.GetComponent<Boss1AimedMissile>(),
            boss.GetComponent<Boss1MissileBarrage>(), boss.GetComponent<Boss1RearMissiles>(), boss
        };
        wasEnabled = new bool[combat.Length];
        for (int i = 0; i < combat.Length; i++)
        {
            wasEnabled[i] = combat[i].enabled;
            combat[i].enabled = false;
        }

        boss.ShowEntranceBands(letterboxHeight);
        yield return cameraFollow.CenterOnArena(cameraPanDuration);
        yield return new WaitForSeconds(dropDelay);

        var body = boss.GetComponent<Rigidbody2D>();
        Vector3 dropPosition = landingPoint.position + Vector3.up * dropHeight;
        boss.transform.position = dropPosition;
        bossVisual.SetEntrancePose(entrancePose);
        boss.gameObject.SetActive(true);
        body.position = dropPosition;
        body.linearVelocity = Vector2.zero;
        var bossCollider = boss.GetComponent<BoxCollider2D>();
        Physics2D.IgnoreCollision(bossCollider, player.GetComponent<BoxCollider2D>());
        var floor = new ContactFilter2D { useTriggers = false };
        floor.SetNormalAngle(45f, 135f);
        yield return new WaitUntil(() => bossCollider.IsTouching(floor));

        player.GetComponent<PlayerHitCameraShake>()?.Kick(Vector2.down, shakeStrength, shakeDuration);
        originalScale = PlaybackScale;
        cinematicScale = originalScale * slowMotionScale;
        slowingTime = slowMotionDuration > 0f;
        if (slowingTime) PlaybackScale = cinematicScale;

        float duration = Mathf.Max(shakeDuration, slowMotionDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (slowingTime && elapsed >= slowMotionDuration) RestoreSpeed();
            yield return null;
            if (Time.timeScale > 0f) elapsed += Time.unscaledDeltaTime;
        }
        RestoreSpeed();

        // 등장 중 남은 투사체나 허수아비 타격으로 얻은 리듬도 전투에 넘기지 않는다.
        player.GetComponent<PlayerRhythmGauge>().ResetGauge();
        bossVisual.SetEntrancePose(null);
        for (int i = 0; i < combat.Length; i++) combat[i].enabled = wasEnabled[i];
        cameraFollow.ResumeFollow();
        ReleaseControls();
        boss.BeginCombatReveal(healthFillDuration, bandExitDuration);
        CurrentPhase = Phase.Combat;
    }

    private void LockControls()
    {
        controls = new Behaviour[] { player, player.GetComponent<PlayerAttack>(), player.GetComponent<PlayerChordStrike>() };
        controlStates = new bool[controls.Length];
        for (int i = 0; i < controls.Length; i++)
        {
            controlStates[i] = controls[i].enabled;
            controls[i].enabled = false;
        }
        playerBody = player.GetComponent<Rigidbody2D>();
        playerConstraints = playerBody.constraints;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
        playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
        var animator = player.GetComponent<Animator>();
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsSliding", false);
    }

    private void ReleaseControls()
    {
        if (playerBody != null) playerBody.constraints = playerConstraints;
        if (controls == null) return;
        for (int i = 0; i < controls.Length; i++)
            if (controls[i] != null) controls[i].enabled = controlStates[i];
        controls = null;
    }

    private float PlaybackScale
    {
        get => hitStop != null ? hitStop.PlaybackScale : Time.timeScale;
        set
        {
            if (hitStop != null) hitStop.PlaybackScale = value;
            else Time.timeScale = value;
        }
    }

    private void RestoreSpeed()
    {
        if (slowingTime && Mathf.Approximately(PlaybackScale, cinematicScale))
            PlaybackScale = originalScale;
        slowingTime = false;
    }

    private void OnDisable()
    {
        if (CurrentPhase != Phase.Entrance) return;
        StopAllCoroutines();
        RestoreSpeed();
        if (bossVisual != null) bossVisual.SetEntrancePose(null);
        if (boss != null)
        {
            boss.gameObject.SetActive(false);
            boss.EndEntrancePresentation();
        }
        if (combat != null)
            for (int i = 0; i < combat.Length; i++)
                if (combat[i] != null) combat[i].enabled = wasEnabled[i];
        ReleaseControls();
        if (cameraFollow != null)
        {
            cameraFollow.ResumeFollow();
            cameraFollow.SetHorizontalBounds(approachCameraBounds.x, approachCameraBounds.y);
        }
        if (leftGate != null && rightGate != null) SetGates(false);
        CurrentPhase = Phase.Approach;
    }

    private void SetGates(bool closed)
    {
        leftGate.enabled = closed;
        rightGate.enabled = closed;
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(triggerPoint.position, triggerPoint.position + Vector3.up * 8f);
        }
        if (landingPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(landingPoint.position, landingPoint.position + Vector3.up * dropHeight);
            Gizmos.DrawWireSphere(landingPoint.position, 0.3f);
        }
    }
}
