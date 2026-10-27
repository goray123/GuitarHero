using UnityEngine;

[DefaultExecutionOrder(110)]
public class CityParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraRig;
    [Tooltip("클수록 카메라를 따라 움직여 더 먼 배경처럼 보임")]
    [SerializeField, Range(0f, 1f)] private float followRatio = 0.7f;
    private Vector3 origin;
    private float cameraOrigin;
    private void Start()
    {
        origin = transform.position;
        cameraOrigin = cameraRig.position.x;
    }
    private void LateUpdate()
    {
        if (cameraRig != null)
            transform.position = origin + Vector3.right * ((cameraRig.position.x - cameraOrigin) * followRatio);
    }
}
