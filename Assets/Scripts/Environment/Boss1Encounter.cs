using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Boss1EntrancePresentation))]
public class Boss1Encounter : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Boss1Health boss;
    [SerializeField] private Transform triggerPoint;
    [SerializeField] private Transform landingPoint;
    [SerializeField] private BoxCollider2D leftGate;
    [SerializeField] private BoxCollider2D rightGate;
    [SerializeField] private CityCameraFollow cameraFollow;
    [Header("Entrance")]
    [Tooltip("착지 지점에서 보스가 떨어질 높이")]
    [SerializeField, Min(1f)] private float dropHeight = 12f;
    [Tooltip("이름 공개 후 검은 띠와 카메라를 복구하는 시간(초)")]
    [SerializeField, Min(0f)] private float landingPause = 0.6f;
    [Tooltip("조작 해제 후 보스 체력바가 차오르는 시간(초)")]
    [SerializeField, Min(0.01f)] private float healthFillDuration = 0.8f;

    public enum Phase { Approach, Entrance, Combat, Cleared }
    public Phase CurrentPhase { get; private set; }

    private Vector2 approachCameraBounds;
    private Boss1EntrancePresentation presentation;
    private Behaviour[] combat, controls;
    private bool[] combatEnabled, controlsEnabled;
    private static readonly HashSet<string> seenEntrances = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSeenEntrances() => seenEntrances.Clear();

    private void Start()
    {
        boss.gameObject.SetActive(false);
        SetGates(false);
        approachCameraBounds = cameraFollow.HorizontalBounds;
        presentation = GetComponent<Boss1EntrancePresentation>();
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

        // 외형만 먼저 활성화하고 착지가 끝난 뒤 기존 전투 컴포넌트를 복구한다.
        combat = new Behaviour[] {
            boss.GetComponent<Boss1ProximityAttack>(), boss.GetComponent<Boss1AimedMissile>(),
            boss.GetComponent<Boss1MissileBarrage>(), boss.GetComponent<Boss1RearMissiles>(), boss
        };
        combatEnabled = Suspend(combat);
        controls = new Behaviour[] {
            player, player.GetComponent<PlayerAttack>(), player.GetComponent<PlayerChordStrike>(),
            player.GetComponent<PlayerGuitarSpin>(),
            player.GetComponent<PlayerRhythmGaugeUI>()
        };
        controlsEnabled = Suspend(controls);
        var playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
        player.GetComponent<Animator>().SetBool("IsMoving", false);
        yield return presentation.Anticipate(player, boss, landingPoint, cameraFollow, seenEntrances.Contains(boss.DisplayName));

        var body = boss.GetComponent<Rigidbody2D>();
        Vector3 dropPosition = landingPoint.position + Vector3.up * dropHeight;
        boss.transform.position = dropPosition;
        boss.gameObject.SetActive(true);
        body.position = dropPosition;
        body.linearVelocity = Vector2.zero;
        Physics2D.IgnoreCollision(boss.GetComponent<BoxCollider2D>(), player.GetComponent<BoxCollider2D>());
        boss.SetEntranceProgress(0f);
        presentation.BeginFall();

        yield return new WaitUntil(() => body.position.y <= landingPoint.position.y + 0.05f &&
            Mathf.Abs(body.linearVelocity.y) < 0.1f);
        yield return presentation.Impact();
        yield return presentation.Release(landingPause);

        // 등장 중 남은 투사체나 허수아비 타격으로 얻은 리듬도 전투에 넘기지 않는다.
        player.GetComponent<PlayerRhythmGauge>().ResetGauge();
        Restore(controls, controlsEnabled);
        Restore(combat, combatEnabled);
        controls = combat = null;
        seenEntrances.Add(boss.DisplayName);
        CurrentPhase = Phase.Combat;
        yield return presentation.FillHealthBar(healthFillDuration);
    }

    private static bool[] Suspend(Behaviour[] components)
    {
        var states = new bool[components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null) continue;
            states[i] = components[i].enabled;
            components[i].enabled = false;
        }
        return states;
    }

    private static void Restore(Behaviour[] components, bool[] states)
    {
        if (components == null) return;
        for (int i = 0; i < components.Length; i++)
            if (components[i] != null) components[i].enabled = states[i];
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (presentation != null) presentation.ResetPresentation();
        if (CurrentPhase != Phase.Entrance) return;
        Restore(controls, controlsEnabled);
        Restore(combat, combatEnabled);
        controls = combat = null;
        if (boss != null) boss.gameObject.SetActive(false);
        SetGates(false);
        if (cameraFollow != null) cameraFollow.SetHorizontalBounds(approachCameraBounds.x, approachCameraBounds.y);
        CurrentPhase = Phase.Approach;
    }

    private void SetGates(bool closed)
    {
        if (leftGate != null) leftGate.enabled = closed;
        if (rightGate != null) rightGate.enabled = closed;
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
