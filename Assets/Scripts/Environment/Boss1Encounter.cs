using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
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
    [Tooltip("착지 후 공격을 시작하기까지 대기 시간(초)")]
    [SerializeField, Min(0f)] private float landingPause = 0.6f;

    public enum Phase { Approach, Entrance, Combat, Cleared }
    public Phase CurrentPhase { get; private set; }

    private Vector2 approachCameraBounds;

    private void Start()
    {
        boss.gameObject.SetActive(false);
        SetGates(false);
        approachCameraBounds = cameraFollow.HorizontalBounds;
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
        Behaviour[] combat = {
            boss.GetComponent<Boss1ProximityAttack>(), boss.GetComponent<Boss1AimedMissile>(),
            boss.GetComponent<Boss1MissileBarrage>(), boss.GetComponent<Boss1RearMissiles>(), boss
        };
        var wasEnabled = new bool[combat.Length];
        for (int i = 0; i < combat.Length; i++)
        {
            wasEnabled[i] = combat[i].enabled;
            combat[i].enabled = false;
        }

        var body = boss.GetComponent<Rigidbody2D>();
        Vector3 dropPosition = landingPoint.position + Vector3.up * dropHeight;
        boss.transform.position = dropPosition;
        boss.gameObject.SetActive(true);
        body.position = dropPosition;
        body.linearVelocity = Vector2.zero;
        Physics2D.IgnoreCollision(boss.GetComponent<BoxCollider2D>(), player.GetComponent<BoxCollider2D>());

        yield return new WaitUntil(() => body.position.y <= landingPoint.position.y + 0.05f &&
            Mathf.Abs(body.linearVelocity.y) < 0.1f);
        yield return new WaitForSeconds(landingPause);

        // 등장 중 남은 투사체나 허수아비 타격으로 얻은 리듬도 전투에 넘기지 않는다.
        player.GetComponent<PlayerRhythmGauge>().ResetGauge();
        for (int i = 0; i < combat.Length; i++) combat[i].enabled = wasEnabled[i];
        CurrentPhase = Phase.Combat;
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
