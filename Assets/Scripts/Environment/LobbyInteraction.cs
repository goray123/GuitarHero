using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class LobbyInteraction : MonoBehaviour
{
    [SerializeField] private string interactionName;
    [Tooltip("비워 두면 클릭 로그만 출력")]
    [SerializeField] private string sceneToLoad;
    [Tooltip("플레이어가 구역 안에서 위 방향키로 상호작용")]
    [SerializeField] private bool allowUpArrow;

    private BoxCollider2D clickArea;
    private PlayerMovement player;
    private bool transitioning;

    private void Awake()
    {
        clickArea = GetComponent<BoxCollider2D>();
        player = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (allowUpArrow && Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame &&
            player != null && clickArea.OverlapPoint(player.transform.position))
        {
            Interact();
            return;
        }

        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null || !viewCamera.pixelRect.Contains(pointer.position.ReadValue())) return;
        Vector2 point = viewCamera.ScreenToWorldPoint(pointer.position.ReadValue());
        if (clickArea.OverlapPoint(point)) Interact();
    }

    public void Interact()
    {
        if (transitioning) return;
        Debug.Log($"[로비] {interactionName} 상호작용", this);
        if (string.IsNullOrEmpty(sceneToLoad)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"[로비] 빌드 씬 목록에서 {sceneToLoad} 씬을 찾을 수 없습니다.", this);
            return;
        }

        transitioning = true;
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}
