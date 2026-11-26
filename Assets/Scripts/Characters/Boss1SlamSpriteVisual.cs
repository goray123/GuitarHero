using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(100)]
[RequireComponent(typeof(Boss1ProximityAttack), typeof(Boss1PrototypeModel))]
public sealed class Boss1SlamSpriteVisual : MonoBehaviour
{
    [SerializeField] private AnimationClip sweepAnimation;
    [SerializeField] private AnimationClip punchAnimation;
    [SerializeField] private AnimationClip slamAnimation;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private AnimationClip rushAnimation;
    [SerializeField] private AnimationClip leapAnimation;
    [SerializeField] private AnimationClip backstepAnimation;
    [SerializeField] private float idleReferenceSpriteHeight = 7.8625f;
    [Tooltip("클립에서 양 주먹이 처음 바닥에 닿는 시간")]
    [SerializeField, Min(0f)] private float contactTime = 0.82f;
    [Tooltip("클립에서 충격 흡수가 끝나고 회복하는 시간")]
    [SerializeField, Min(0f)] private float recoveryStartTime = 1.02f;
    [Tooltip("기본 자세의 투명 여백을 제외한 그림 높이 / 모든 프레임에 공통인 PPU")]
    [SerializeField, Min(0.01f)] private float referenceSpriteHeight = 4.86875f;
    [Tooltip("1·2타 공통 기본 자세 높이 / PPU. 프레임마다 배율을 바꾸지 않음")]
    [SerializeField, Min(0.01f)] private float meleeReferenceSpriteHeight = 4.71875f;
    [Tooltip("이동 프레임의 기본 자세 높이 / PPU. 근접 원화 배율과 별개로 유지")]
    [SerializeField, Min(0.01f)] private float movementReferenceSpriteHeight = 4.71875f;

    private Boss1ProximityAttack owner;
    private Boss1PrototypeModel prototype;
    private SpriteRenderer visual;
    private Boss1BoostRush rush;
    private Boss1LeapSlam leap;
    private Boss1Backstep backstep;
    private Boss1BoostStep boostStep;
    private Boss1RearMissiles rearMissiles;

    // Other presentation components attach to the sampled sprite, including its pivot and facing.
    public SpriteRenderer BodyRenderer => visual;
    public float EntranceCompression { get; set; }

    private void OnEnable()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        prototype = GetComponent<Boss1PrototypeModel>();
        rush = GetComponent<Boss1BoostRush>();
        leap = GetComponent<Boss1LeapSlam>();
        backstep = GetComponent<Boss1Backstep>();
        boostStep = GetComponent<Boss1BoostStep>();
        rearMissiles = GetComponent<Boss1RearMissiles>();
    }

    private void CreateVisual()
    {
        if (visual != null) return;
        var child = new GameObject("Slam Sprite");
        child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(transform, false);
        visual = child.AddComponent<SpriteRenderer>();
        child.AddComponent<Animator>().enabled = false;
        var original = GetComponent<SpriteRenderer>();
        if (original != null)
        {
            if (original.sharedMaterial != null) visual.sharedMaterial = original.sharedMaterial;
            visual.sortingLayerID = original.sortingLayerID;
            visual.sortingOrder = original.sortingOrder;
        }
        visual.enabled = false;
    }

    private void LateUpdate()
    {
        // 등장 연출처럼 전투 제어가 꺼져 있어도 실제 원화를 유지한다.
        if (owner != null && idleSprite != null && (!Application.isPlaying || !owner.isActiveAndEnabled))
        {
            CreateVisual();
            visual.sprite = idleSprite;
            float idleScale = 3.45f * owner.Height / Mathf.Max(.01f, idleReferenceSpriteHeight);
            float compression = Mathf.Clamp01(EntranceCompression);
            visual.transform.localScale = new Vector3(idleScale * (1f + compression * 0.04f),
                idleScale * (1f - compression * 0.1f), 1f);
            var original = GetComponent<SpriteRenderer>();
            visual.flipX = Application.isPlaying ? owner.Facing < 0f : original != null && original.flipX;
            visual.enabled = true;
            prototype.SetBodyVisible(false);
            return;
        }
        int attack = owner != null ? owner.Timeline.Attack : 0;
        if (owner != null && owner.isActiveAndEnabled && attack == 0 && ShowMovement()) return;
        AnimationClip clip = attack == 1 ? sweepAnimation : attack == 2 ? punchAnimation : attack == 3 ? slamAnimation : null;
        bool show = owner != null && owner.isActiveAndEnabled && clip != null;
        if (!show)
        {
            if (visual != null) visual.enabled = false;
            if (prototype != null) prototype.SetBodyVisible(true);
            return;
        }

        CreateVisual();
        var clock = owner.Timeline;
        float progress = Mathf.Clamp01(clock.StageProgress);
        float contact = Mathf.Clamp(attack == 3 ? contactTime : 0.65f, 0f, clip.length);
        float recovery = Mathf.Clamp(attack == 3 ? recoveryStartTime : 0.8f, contact, clip.length);
        // 전투 시간은 유지하고 접촉 그림을 3타 판정 시작에 맞춤.
        float sampleTime = clock.CurrentStage == BossMeleeTimeline.Stage.Windup ? Mathf.Lerp(0f, contact, progress) :
            clock.CurrentStage == BossMeleeTimeline.Stage.Active ? Mathf.Lerp(contact, recovery, progress) :
            Mathf.Lerp(recovery, clip.length, progress);
        clip.SampleAnimation(visual.gameObject, sampleTime);
        // 예고 시작 때 정한 방향으로만 회전. 도형 모델과 같은 회전 시간을 사용.
        float facing = owner.Facing;
        if (attack == 3 && clock.Elapsed < owner.TurnDuration && owner.TurnDuration > 0f)
            facing = Mathf.Lerp(owner.PreviousFacing, owner.Facing,
                Mathf.SmoothStep(0f, 1f, clock.Elapsed / owner.TurnDuration));
        visual.flipX = false;
        // 기존 임시 보스 높이(플레이어 기준 높이의 3.45배)에 맞춤.
        float referenceHeight = attack == 3 ? referenceSpriteHeight : meleeReferenceSpriteHeight;
        float scale = 3.45f * owner.Height / Mathf.Max(0.01f, referenceHeight);
        visual.transform.localScale = new Vector3(scale * facing, scale, 1f);
        visual.enabled = visual.sprite != null;
        if (prototype != null) prototype.SetBodyVisible(!visual.enabled);
    }

    // Sample presentation from the combat phase, including shortened warnings and early wall stops.
    private bool ShowMovement()
    {
        AnimationClip clip = null;
        float time = 0f;
        float facing = owner.Facing;
        float height = movementReferenceSpriteHeight;
        if (rush != null && rush.BlocksActions && rushAnimation != null)
        {
            clip = rushAnimation;
            facing = rush.Facing;
            float p = Mathf.Clamp01(rush.StageProgress);
            switch (rush.Stage)
            {
                case Boss1BoostRush.RushStage.Windup: time = Mathf.Lerp(0f, .849f, p); break;
                case Boss1BoostRush.RushStage.Moving: time = Mathf.Lerp(.85f, 1.399f, p); break;
                case Boss1BoostRush.RushStage.Braking: time = Mathf.Lerp(1.4f, 2f, p); break;
                default: time = 2f; break;
            }
        }
        else if (leap != null && leap.IsBusy && leapAnimation != null)
        {
            clip = leapAnimation;
            float p = Mathf.Clamp01(leap.Progress);
            switch (leap.CurrentPhase)
            {
                case Boss1LeapSlam.Phase.Windup: time = Mathf.Lerp(0f, .799f, p); break;
                case Boss1LeapSlam.Phase.Airborne: time = Mathf.Lerp(.8f, 1.499f, p); break;
                case Boss1LeapSlam.Phase.Impact: time = Mathf.Lerp(1.5f, 1.649f, p); break;
                default: time = Mathf.Lerp(1.65f, 2.2f, p); break;
            }
        }
        else if (backstep != null && backstep.IsBusy && backstepAnimation != null)
        {
            clip = backstepAnimation;
            facing = backstep.Facing;
            float p = Mathf.Clamp01(backstep.Progress);
            switch (backstep.CurrentPhase)
            {
                case Boss1Backstep.Phase.Windup: time = Mathf.Lerp(0f, .349f, p); break;
                case Boss1Backstep.Phase.Moving: time = Mathf.Lerp(.35f, .649f, p); break;
                default: time = Mathf.Lerp(.65f, .95f, p); break;
            }
        }
        else if (idleSprite != null && rearMissiles != null && rearMissiles.isActiveAndEnabled && rearMissiles.HasLiveMissiles)
        {
            // 후방 미사일은 추진기가 달린 Idle 원본 자세와 크기를 유지.
            height = idleReferenceSpriteHeight;
            facing = rearMissiles.Facing;
        }
        else if (idleSprite != null && !owner.BodyBusy)
        {
            height = idleReferenceSpriteHeight;
            // Boost steps use the original body while their movement and exhaust remain phase driven.
            if (boostStep != null && boostStep.IsBusy) facing = boostStep.Facing;
        }
        else return false;

        CreateVisual();
        if (clip != null) clip.SampleAnimation(visual.gameObject, time);
        else visual.sprite = idleSprite;
        float scale = 3.45f * owner.Height / Mathf.Max(.01f, height);
        visual.transform.localScale = new Vector3(scale * facing, scale, 1f);
        visual.flipX = false;
        visual.enabled = visual.sprite != null;
        prototype.SetBodyVisible(!visual.enabled);
        return true;
    }

    private void OnDisable()
    {
        if (prototype != null) prototype.SetBodyVisible(true);
        if (visual == null) return;
        visual.enabled = false;
        if (Application.isPlaying) Destroy(visual.gameObject);
        else DestroyImmediate(visual.gameObject);
        visual = null;
    }
}
