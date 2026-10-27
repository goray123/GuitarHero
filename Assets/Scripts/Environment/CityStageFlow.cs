using UnityEngine;

public class CityStageFlow : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform checkpoint;
    [SerializeField] private CityCameraFollow cameraFollow;
    [SerializeField] private float fallLimit = -7f;
    private bool checkpointReached;

    private void Update()
    {
        if (player == null) return;
        Vector3 position = player.transform.position;
        if (!checkpointReached && position.x >= checkpoint.position.x && position.y > 0f)
        {
            checkpointReached = true;
        }
        if (position.y < fallLimit)
        {
            // 낙하 중 스킬과 콜라이더 상태를 정리한 뒤 안전 지점으로 복귀
            player.SetActive(false);
            Vector3 spawn = (checkpointReached ? checkpoint : startPoint).position;
            player.transform.position = spawn;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            player.SetActive(true);
            cameraFollow.Snap();
        }
    }
}
