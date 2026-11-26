using System.Collections;
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
    private bool framingArena;

    public Vector2 HorizontalBounds => new Vector2(leftEdge, rightEdge);

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
        return min > max ? (leftEdge + rightEdge) * .5f : Mathf.Clamp(target.position.x + lookAhead, min, max);
    }
    private void LateUpdate()
    {
        if (framingArena || target == null || viewCamera == null) return;
        float x = Mathf.SmoothDamp(transform.position.x, DesiredX(), ref velocity, smoothTime,
            Mathf.Infinity, Time.deltaTime);
        transform.position = new Vector3(x, height, 0f);
    }
    public IEnumerator CenterOnArena(float duration)
    {
        framingArena = true;
        velocity = 0f;
        Vector3 start = transform.position;
        Vector3 center = new Vector3((leftEdge + rightEdge) * 0.5f, height, start.z);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, center, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        transform.position = center;
    }

    public void ResumeFollow() => framingArena = false;
    public void Snap()
    {
        if (target == null || viewCamera == null) return;
        velocity = 0f;
        transform.position = new Vector3(DesiredX(), height, 0f);
    }
}
