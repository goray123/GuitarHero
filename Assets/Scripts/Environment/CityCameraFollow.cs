using UnityEngine;

// 카메라 부모를 움직여 기존 자식 카메라의 타격 진동과 분리
[DefaultExecutionOrder(100)]
public class CityCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float leftEdge = -4f;
    [SerializeField] private float rightEdge = 85f;
    [SerializeField] private float height = 4f;
    [SerializeField] private float lookAhead = 2.5f;
    [SerializeField, Min(0f)] private float smoothTime = 0.16f;
    private float velocity;
    private Transform cinematicTarget;
    private float cinematicWeight;
    private float originalSize;

    public Vector2 HorizontalBounds => new Vector2(leftEdge, rightEdge);
    public Camera ViewCamera => viewCamera;

    public void SetCinematicFocus(Transform focus, float weight, float zoom)
    {
        if (viewCamera == null) return;
        if (cinematicTarget == null) originalSize = viewCamera.orthographicSize;
        cinematicTarget = focus;
        cinematicWeight = Mathf.Clamp01(weight);
        viewCamera.orthographicSize = originalSize * (1f - Mathf.Clamp(zoom, 0f, 0.25f) * cinematicWeight);
    }

    public void ClearCinematicFocus()
    {
        if (cinematicTarget != null && viewCamera != null) viewCamera.orthographicSize = originalSize;
        cinematicTarget = null;
        cinematicWeight = 0f;
    }

    public void SetHorizontalBounds(float left, float right)
    {
        leftEdge = left;
        rightEdge = right;
    }

    private void Start() => Snap();
    private float DesiredX()
    {
        float halfWidth = viewCamera.orthographicSize * viewCamera.aspect;
        float min = leftEdge + halfWidth;
        float max = rightEdge - halfWidth;
        float focus = target.position.x + lookAhead;
        if (cinematicTarget != null) focus = Mathf.Lerp(focus, cinematicTarget.position.x, cinematicWeight);
        return min > max ? (leftEdge + rightEdge) * .5f : Mathf.Clamp(focus, min, max);
    }
    private void LateUpdate()
    {
        if (target == null || viewCamera == null) return;
        float x = Mathf.SmoothDamp(transform.position.x, DesiredX(), ref velocity, smoothTime,
            Mathf.Infinity, cinematicTarget != null ? Time.unscaledDeltaTime : Time.deltaTime);
        transform.position = new Vector3(x, height, 0f);
    }
    public void Snap()
    {
        if (target == null || viewCamera == null) return;
        velocity = 0f;
        transform.position = new Vector3(DesiredX(), height, 0f);
    }

    private void OnDisable() => ClearCinematicFocus();
}
