using UnityEngine;
using UnityEngine.InputSystem;

// Dedicated, repeatable encounter scene. Existing city scenes are left alone.
public sealed class Boss1Arena : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Boss1Controller bossPrefab;
    [SerializeField] private Sprite skyline;
    [SerializeField] private float leftEdge = -13f;
    [SerializeField] private float rightEdge = 13f;
    [SerializeField] private bool enableTestKeys = true;
    private GameObject playerObject;
    private PlayerHealth player;
    private Boss1Controller boss;
    private Sprite white;
    private Material unlit;
    private GUIStyle title, label, small;
    private bool resultShown;
    private float resultAt;

    public Boss1Controller Boss { get { return boss; } }
    public PlayerHealth Player { get { return player; } }

    private void Awake()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 3.3f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.8f;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.075f);
        }
        white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) unlit = new Material(shader);
        Stage();
        RestartEncounter();
    }

    private void Stage()
    {
        float width = rightEdge - leftEdge;
        if (skyline != null)
        {
            var background = new GameObject("City skyline");
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = skyline;
            if (unlit != null) renderer.sharedMaterial = unlit;
            renderer.color = new Color(0.5f, 0.6f, 0.78f, 1f);
            renderer.sortingOrder = -30;
            background.transform.SetParent(transform, false);
            background.transform.position = new Vector3(0f, 4f, 0f);
            background.transform.localScale = Vector3.one * Mathf.Max(width / skyline.bounds.size.x, 9f / skyline.bounds.size.y);
        }
        Block("Floor", new Vector2(0f, -0.4f), new Vector2(width + 2f, 0.8f), new Color(0.065f, 0.08f, 0.12f), true, -1);
        Block("Left boundary", new Vector2(leftEdge - 0.5f, 3f), new Vector2(1f, 7f), new Color(0.08f, 0.1f, 0.15f), true, 4);
        Block("Right boundary", new Vector2(rightEdge + 0.5f, 3f), new Vector2(1f, 7f), new Color(0.08f, 0.1f, 0.15f), true, 4);
        Block("Floor light", new Vector2(0f, -0.04f), new Vector2(width, 0.04f), new Color(0.1f, 0.7f, 0.8f), false, 1);
        for (int i = 0; i < 9; i++)
            Block("Rear column", new Vector2(Mathf.Lerp(leftEdge, rightEdge, i / 8f), 3f),
                new Vector2(0.14f, 6f), new Color(0.08f, 0.16f, 0.22f), false, -20);
    }

    private void Block(string name, Vector2 position, Vector2 size, Color color, bool collision, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = white;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (unlit != null) renderer.sharedMaterial = unlit;
        if (collision) obj.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    public void RestartEncounter()
    {
        if (playerPrefab == null || bossPrefab == null)
        {
            Debug.LogError("Boss1_Arena requires the Player and Boss1 prefabs.", this);
            return;
        }
        if (boss != null) { boss.gameObject.SetActive(false); Destroy(boss.gameObject); }
        if (playerObject != null) { playerObject.SetActive(false); Destroy(playerObject); }
        playerObject = Instantiate(playerPrefab, new Vector3(-2f, 0.74f, 0f), Quaternion.identity);
        player = playerObject.GetComponent<PlayerHealth>();
        if (player == null) player = playerObject.AddComponent<PlayerHealth>();
        boss = Instantiate(bossPrefab, new Vector3(1f, 0.025f, 0f), Quaternion.identity);
        boss.ConfigureTarget(player, leftEdge, rightEdge);
        boss.BeginEncounter();
        resultShown = false;
    }

    private void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys != null && keys.rKey.wasPressedThisFrame) RestartEncounter();
        if (enableTestKeys && keys != null && boss != null && !boss.IsDead && player != null && !player.IsDead)
        {
            if (keys.bKey.wasPressedThisFrame) boss.ApplyBind(3f);
            if (keys.f2Key.wasPressedThisFrame && boss.Model.Phase == 1)
                boss.TryTakeDamage(Mathf.Max(0f, boss.CurrentHealth - boss.MaxHealth * 0.5f));
        }
        if (!resultShown && (boss != null && boss.IsDead || player != null && player.IsDead))
        {
            resultShown = true; resultAt = Time.unscaledTime;
        }
        if (playerObject != null && playerObject.transform.position.y < -6f) RestartEncounter();
    }

    private void OnGUI()
    {
        if (player == null || boss == null || boss.Model == null) return;
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = Color.white;
            label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            label.normal.textColor = Color.white;
            small = new GUIStyle(label) { fontSize = 13 };
        }
        float width = Mathf.Min(700f, Screen.width * 0.62f), x = (Screen.width - width) * 0.5f;
        GUI.Label(new Rect(x, 16f, width, 28f), "ARMORED MECH  /  PHASE " + boss.Model.Phase + " OF 2", title);
        Bar(new Rect(x, 49f, width, 15f), boss.CurrentHealth / boss.MaxHealth, new Color(0.95f, 0.2f, 0.3f));
        GUI.Label(new Rect(x, 67f, width, 23f), Mathf.CeilToInt(boss.CurrentHealth) + " / " + boss.MaxHealth.ToString("0"), small);
        GUI.Label(new Rect(20f, 82f, 200f, 24f), "HEALTH  " + player.CurrentHealth.ToString("0") + " / " + player.MaxHealth.ToString("0"), label);
        Bar(new Rect(20f, 109f, 190f, 12f), player.CurrentHealth / player.MaxHealth, new Color(0.2f, 0.9f, 0.6f));
        string burst = boss.Model.IsBarrage ? "MISSILE BARRAGE" : boss.Model.BarragePending ? "MISSILES READY" :
            "MISSILES  " + boss.Model.BarrageRemaining.ToString("0.0") + "s";
        GUI.Label(new Rect(x + width - 230f, 67f, 230f, 23f), burst, small);
        float helpX = Screen.width * 0.35f;
        GUI.Label(new Rect(helpX, Screen.height - 72f, Screen.width - helpX - 20f, 22f),
            "ARROWS MOVE  |  UP JUMP  |  DOWN SLIDE", small);
        GUI.Label(new Rect(helpX, Screen.height - 50f, Screen.width - helpX - 20f, 22f),
            "Z ATTACK  |  A CHORD STRIKE  |  R RETRY", small);
        if (enableTestKeys)
            GUI.Label(new Rect(helpX, Screen.height - 28f, Screen.width - helpX - 20f, 22f), "TEST: B BIND 3s  |  F2 PHASE 2", small);
        if (resultShown && Time.unscaledTime - resultAt > 0.5f)
        {
            Rect panel = new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 65f, 340f, 130f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 40f), player.IsDead ? "TRY AGAIN" : "BOSS DEFEATED", title);
            if (GUI.Button(new Rect(panel.x + 80f, panel.y + 78f, 180f, 30f), "RETRY  [R]")) RestartEncounter();
        }
    }

    private static void Bar(Rect rect, float ratio, Color color)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.17f, 0.95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        rect.width *= Mathf.Clamp01(ratio);
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void OnDestroy()
    {
        if (boss != null) Destroy(boss.gameObject);
        if (playerObject != null) Destroy(playerObject);
        if (white != null) Destroy(white);
        if (unlit != null) Destroy(unlit);
    }
}

