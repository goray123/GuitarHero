using UnityEngine;

// 근접 프로토타입의 포즈 계산 뒤에 웅크림과 부스터만 덧입힌다.
[DefaultExecutionOrder(100), DisallowMultipleComponent]
public class Boss1BoostRushVisual : MonoBehaviour
{
    private Boss1BoostRush rush;
    private Boss1ProximityAttack owner;
    private Boss1BoosterFlameVisual boosterFlame;
    private Transform model;
    private SpriteRenderer flame, area;
    private Sprite square;
    private bool posed;

    private void Awake()
    {
        rush = GetComponent<Boss1BoostRush>();
        owner = GetComponent<Boss1ProximityAttack>();
        boosterFlame = GetComponent<Boss1BoosterFlameVisual>();
        square = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            Vector2.one * 0.5f, Texture2D.whiteTexture.width);
        flame = Part("Rush Booster", new Color(1f, 0.5f, 0.05f), 12);
        area = Part("Rush Hitbox Preview", new Color(1f, 0.15f, 0.05f, 0.25f), 11);
    }

    private SpriteRenderer Part(string name, Color color, int order)
    {
        var part = new GameObject(name);
        part.transform.SetParent(transform, false);
        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    private void LateUpdate()
    {
        if (rush == null || owner == null) return;
        bool visible = rush.BlocksActions && rush.Stage != Boss1BoostRush.RushStage.Interval;
        if (boosterFlame == null) boosterFlame = GetComponent<Boss1BoosterFlameVisual>();
        flame.enabled = visible && (boosterFlame == null || !boosterFlame.ReplacesRushFlame);
        area.enabled = rush.HitboxActive;
        if (model == null) model = transform.Find("Prototype Shapes");
        if (model != null)
        {
            if (visible)
            {
                // 머리까지 3.45H인 기존 모델을 실제 돌진 판정 높이에 맞춘다.
                model.localScale = new Vector3(rush.Facing, rush.PoseHeight / (3.45f * owner.Height), 1f);
                posed = true;
            }
            else if (posed) { model.localScale = new Vector3(owner.Facing, 1f, 1f); posed = false; }
        }
        if (!visible) return;
        float h = owner.Height;
        bool moving = rush.Stage == Boss1BoostRush.RushStage.Moving;
        float length = moving ? 1.1f : rush.Stage == Boss1BoostRush.RushStage.Braking ? 0.15f : Mathf.Lerp(0.1f, 0.55f, rush.StageProgress);
        flame.transform.localPosition = new Vector3(-rush.Facing * (owner.BodyFront + length * h * 0.5f), rush.PoseHeight * 0.55f, 0f);
        flame.transform.localScale = new Vector3(length * h, (moving ? 0.3f : 0.15f) * h, 1f);
        flame.color = moving ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.4f, 0.05f);
        if (rush.Hitbox != null)
        {
            area.transform.localPosition = rush.Hitbox.offset;
            area.transform.localScale = new Vector3(rush.Hitbox.size.x, rush.Hitbox.size.y, 1f);
        }
    }

    private void OnDisable()
    {
        if (flame != null) flame.enabled = false;
        if (area != null) area.enabled = false;
        if (posed && model != null) model.localScale = new Vector3(owner.Facing, 1f, 1f);
        posed = false;
    }

    private void OnDestroy()
    {
        if (flame != null) Destroy(flame.gameObject);
        if (area != null) Destroy(area.gameObject);
        if (square != null) Destroy(square);
    }
}
