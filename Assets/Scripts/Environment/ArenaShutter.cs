using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public class ArenaShutter : MonoBehaviour
{
    [SerializeField] private Transform panel;
    [Tooltip("셔터가 천장 안으로 올라가는 거리")]
    [SerializeField, Min(0.1f)] private float openDistance = 13f;
    [Tooltip("셔터가 열리고 닫히는 속도(유닛/초)")]
    [SerializeField, Min(0.1f)] private float travelSpeed = 26f;

    private BoxCollider2D gate;
    private Vector3 closedPosition;

    private void Awake()
    {
        gate = GetComponent<BoxCollider2D>();
        closedPosition = panel.localPosition;
        if (!gate.enabled)
        {
            panel.localPosition += Vector3.up * openDistance;
            panel.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        Vector3 target = closedPosition + (gate.enabled ? Vector3.zero : Vector3.up * openDistance);
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, travelSpeed * Time.deltaTime);
        panel.gameObject.SetActive(gate.enabled || (panel.localPosition - target).sqrMagnitude > 0.0001f);
    }
}
