using System;
using UnityEngine;

// Presentation only: the combat components continue to own movement, timing and hitboxes.
[DisallowMultipleComponent, DefaultExecutionOrder(110)]
[RequireComponent(typeof(Boss1ProximityAttack))]
public sealed class Boss1BoosterFlameVisual : MonoBehaviour
{
    public enum FlameState
    {
        Hidden, RushWindup, RushMoving, BackstepWindup, BackstepMoving,
        StepPreparing, StepMoving, LeapWindup, LeapAirborne, Pursuit
    }

    [Flags]
    public enum NozzleUse { None = 0, Step = 1, Rush = 2, Backstep = 4, Leap = 8, Pursuit = 16 }

    [Serializable]
    public sealed class NozzleBinding
    {
        public Sprite sprite;
        [Tooltip("Nozzle mouth center in the full native image, measured from its top-left corner")]
        public Vector2 nozzlePixelTopLeft;
        [Tooltip("Exhaust axis in the authored sprite: +X right, +Y up; facing is applied by the body renderer")]
        public Vector2 exhaustDirectionLocal = Vector2.left;
        public NozzleUse allowedUses;
    }

    [Tooltip("Transparent sprite: nozzle at its pivot on the left, exhaust extending right")]
    [SerializeField] private Sprite smallFlame;
    [SerializeField] private Sprite rushFlame;
    [SerializeField, Min(.01f)] private float smallLengthH = 1f;
    [SerializeField, Min(.01f)] private float rushLengthH = 2.4f;
    [Tooltip("Nozzle locations in player-height H, for the body facing right")]
    [SerializeField] private Vector2 rushNozzleH = new Vector2(-.65f, .55f);
    [SerializeField] private Vector2 stepNozzleH = new Vector2(-1.2f, 1.7f);
    [SerializeField] private Vector2 backstepNozzleH = new Vector2(.65f, 1.55f);
    [SerializeField] private Vector2 leapNozzleH = new Vector2(-.45f, 1.55f);
    [Tooltip("Exact sprite-to-nozzle mapping. An empty array preserves the legacy geometric anchors.")]
    [SerializeField] private NozzleBinding[] nozzleBindings = new NozzleBinding[0];

    public FlameState CurrentState { get; private set; }
    public bool IsVisible => flame != null && flame.enabled && flame.gameObject.activeInHierarchy;
    public SpriteRenderer FlameRenderer => flame;
    public Vector2 ExhaustDirection { get; private set; }
    public float NominalLength { get; private set; }
    public float NominalThickness { get; private set; }
    public bool UsesPoseNozzles => nozzleBindings != null && nozzleBindings.Length > 0;
    public Sprite CurrentBodySprite { get; private set; }
    public bool HasCurrentNozzleBinding { get; private set; }
    public Vector2 NozzleWorldPosition { get; private set; }
    public Vector2 BoundExhaustDirection { get; private set; }
    public NozzleUse CurrentNozzleUses { get; private set; }
    public bool ReplacesRushFlame => isActiveAndEnabled && rushFlame != null;
    public bool ReplacesPrototypeFlame => isActiveAndEnabled && smallFlame != null;

    private Boss1ProximityAttack owner;
    private Boss1BoostRush rush;
    private Boss1Backstep backstep;
    private Boss1BoostStep step;
    private Boss1LeapSlam leap;
    private Rigidbody2D body;
    private SpriteRenderer flame;
    private Boss1SlamSpriteVisual bodyVisual;
    private Vector3 nozzleWorldPoint;

    private void OnEnable()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        rush = GetComponent<Boss1BoostRush>();
        backstep = GetComponent<Boss1Backstep>();
        step = GetComponent<Boss1BoostStep>();
        leap = GetComponent<Boss1LeapSlam>();
        body = GetComponent<Rigidbody2D>();
        bodyVisual = GetComponent<Boss1SlamSpriteVisual>();
    }

    private void LateUpdate()
    {
        RefreshNozzleBinding();
        if (owner == null || !owner.isActiveAndEnabled || owner.Timeline.Attack != 0)
        {
            Hide();
            return;
        }

        if (rush != null && rush.BlocksActions)
        {
            if (rush.Stage == Boss1BoostRush.RushStage.Windup)
                Show(rushFlame, FlameState.RushWindup, rush.Facing, rushNozzleH,
                    Vector2.left * rush.Facing, RushLengthH() * Mathf.Lerp(.2f, .45f, rush.StageProgress));
            else if (rush.Stage == Boss1BoostRush.RushStage.Moving)
                Show(rushFlame, FlameState.RushMoving, rush.Facing, rushNozzleH,
                    Vector2.left * MovementFacing(rush.DesiredSpeed, rush.Facing), RushLengthH());
            else Hide();
            return;
        }

        if (backstep != null && backstep.isActiveAndEnabled && backstep.IsBusy)
        {
            if (backstep.CurrentPhase == Boss1Backstep.Phase.Windup)
                Show(smallFlame, FlameState.BackstepWindup, backstep.Facing, backstepNozzleH,
                    Vector2.right * backstep.Facing, smallLengthH * Mathf.Lerp(.2f, .45f, backstep.Progress));
            else if (backstep.CurrentPhase == Boss1Backstep.Phase.Moving)
                Show(smallFlame, FlameState.BackstepMoving, backstep.Facing, backstepNozzleH,
                    Vector2.left * MovementFacing(backstep.DesiredSpeed, -backstep.Facing), smallLengthH);
            else Hide();
            return;
        }

        if (leap != null && leap.isActiveAndEnabled && leap.IsBusy)
        {
            if (leap.CurrentPhase == Boss1LeapSlam.Phase.Windup)
                Show(smallFlame, FlameState.LeapWindup, owner.Facing, leapNozzleH,
                    Vector2.down, smallLengthH * Mathf.Lerp(.2f, .45f, leap.Progress));
            else if (leap.CurrentPhase == Boss1LeapSlam.Phase.Airborne)
                Show(smallFlame, FlameState.LeapAirborne, owner.Facing, leapNozzleH, Vector2.down, smallLengthH);
            else Hide();
            return;
        }

        if (step != null && step.isActiveAndEnabled && step.IsBusy)
        {
            if (step.CurrentPhase == Boss1BoostStep.Phase.Preparing)
                Show(smallFlame, FlameState.StepPreparing, step.Facing, stepNozzleH,
                    Vector2.left * step.Facing, smallLengthH * .25f);
            else if (step.CurrentPhase == Boss1BoostStep.Phase.Moving)
                Show(smallFlame, FlameState.StepMoving, step.Facing, stepNozzleH,
                    Vector2.left * MovementFacing(step.DesiredSpeed, step.Facing), smallLengthH);
            else Hide();
            return;
        }

        if (!owner.BodyBusy && owner.IsMoving && body != null && Mathf.Abs(body.linearVelocity.x) > .01f)
        {
            float facing = Mathf.Sign(body.linearVelocity.x);
            Show(smallFlame, FlameState.Pursuit, facing, stepNozzleH, Vector2.left * facing, smallLengthH);
            return;
        }
        Hide();
    }

    private static float MovementFacing(float speed, float fallback)
    {
        // At the first moving sample the controller has entered the phase but has not stepped yet.
        return Mathf.Abs(speed) > .01f ? Mathf.Sign(speed) : (fallback < 0f ? -1f : 1f);
    }

    private void RefreshNozzleBinding()
    {
        ClearNozzleBinding();
        if (bodyVisual == null) bodyVisual = GetComponent<Boss1SlamSpriteVisual>();
        if (bodyVisual == null || !bodyVisual.isActiveAndEnabled) return;
        SpriteRenderer renderer = bodyVisual.BodyRenderer;
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null) return;
        CurrentBodySprite = renderer.sprite;
        if (!UsesPoseNozzles) return;

        NozzleBinding binding = null;
        foreach (var candidate in nozzleBindings)
        {
            if (candidate == null || candidate.sprite != CurrentBodySprite) continue;
            // Ambiguous metadata must not emit a jet from an arbitrary attachment.
            if (binding != null) return;
            binding = candidate;
        }
        if (binding == null || !Finite(binding.nozzlePixelTopLeft) || !Finite(binding.exhaustDirectionLocal) ||
            binding.exhaustDirectionLocal.sqrMagnitude < .000001f) return;

        Sprite sprite = CurrentBodySprite;
        Vector2 pixel = binding.nozzlePixelTopLeft;
        if (pixel.x < 0f || pixel.x > sprite.rect.width || pixel.y < 0f || pixel.y > sprite.rect.height ||
            sprite.pixelsPerUnit <= .0001f) return;
        pixel.y = sprite.rect.height - pixel.y;
        Vector2 localPoint = (pixel - sprite.pivot) / sprite.pixelsPerUnit;
        Vector2 localDirection = binding.exhaustDirectionLocal.normalized;
        if (renderer.flipX) { localPoint.x = -localPoint.x; localDirection.x = -localDirection.x; }
        if (renderer.flipY) { localPoint.y = -localPoint.y; localDirection.y = -localDirection.y; }
        Vector2 direction = renderer.transform.TransformVector(localDirection);
        if (!Finite(direction) || direction.sqrMagnitude < .000001f) return;

        nozzleWorldPoint = renderer.transform.TransformPoint(localPoint);
        NozzleWorldPosition = nozzleWorldPoint;
        BoundExhaustDirection = direction.normalized;
        CurrentNozzleUses = binding.allowedUses;
        HasCurrentNozzleBinding = true;
    }

    private void ClearNozzleBinding()
    {
        CurrentBodySprite = null;
        HasCurrentNozzleBinding = false;
        NozzleWorldPosition = Vector2.zero;
        BoundExhaustDirection = Vector2.zero;
        CurrentNozzleUses = NozzleUse.None;
        nozzleWorldPoint = Vector3.zero;
    }

    private static bool Finite(Vector2 value) =>
        !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);

    private static NozzleUse UseForState(FlameState state)
    {
        switch (state)
        {
            case FlameState.RushWindup:
            case FlameState.RushMoving: return NozzleUse.Rush;
            case FlameState.BackstepWindup:
            case FlameState.BackstepMoving: return NozzleUse.Backstep;
            case FlameState.StepPreparing:
            case FlameState.StepMoving: return NozzleUse.Step;
            case FlameState.LeapWindup:
            case FlameState.LeapAirborne: return NozzleUse.Leap;
            case FlameState.Pursuit: return NozzleUse.Pursuit;
            default: return NozzleUse.None;
        }
    }

    private float RushLengthH()
    {
        float length = Mathf.Max(.01f, rushLengthH);
        if (smallFlame == null || rushFlame == null) return length;
        // Keep the authored aspect ratios while making rush visibly larger in both dimensions.
        const float minimumRatio = 1.75f;
        float smallAspect = smallFlame.bounds.size.x / Mathf.Max(.0001f, smallFlame.bounds.size.y);
        float rushAspect = rushFlame.bounds.size.x / Mathf.Max(.0001f, rushFlame.bounds.size.y);
        return Mathf.Max(length, smallLengthH * minimumRatio,
            smallLengthH * minimumRatio * rushAspect / Mathf.Max(.0001f, smallAspect));
    }

    private void Show(Sprite sprite, FlameState state, float facing, Vector2 legacyNozzleH,
        Vector2 localExhaust, float lengthH)
    {
        if (sprite == null || sprite.bounds.size.x <= .0001f ||
            (UsesPoseNozzles && (!HasCurrentNozzleBinding || (CurrentNozzleUses & UseForState(state)) == NozzleUse.None)))
        {
            Hide();
            return;
        }
        CreateVisual();
        float length = Mathf.Max(.001f, lengthH) * owner.Height;
        float scale = length / sprite.bounds.size.x;
        if (UsesPoseNozzles)
        {
            // TransformVector includes the sampled body's negative X scale when it faces left.
            localExhaust = transform.InverseTransformVector(BoundExhaustDirection);
            flame.transform.localPosition = transform.InverseTransformPoint(nozzleWorldPoint);
        }
        else
            flame.transform.localPosition = new Vector3(legacyNozzleH.x * facing, legacyNozzleH.y, 0f) * owner.Height;
        localExhaust.Normalize();
        flame.sprite = sprite;
        flame.color = Color.white;
        flame.flipX = false;
        flame.flipY = false;
        // The imported pivot is the nozzle; no per-frame crop or stretch is involved.
        flame.transform.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(localExhaust.y, localExhaust.x) * Mathf.Rad2Deg);
        flame.transform.localScale = new Vector3(scale, scale, 1f);
        flame.enabled = true;
        CurrentState = state;
        Vector3 lengthVector = flame.transform.TransformVector(Vector3.right * sprite.bounds.size.x);
        ExhaustDirection = ((Vector2)lengthVector).normalized;
        NominalLength = lengthVector.magnitude;
        NominalThickness = flame.transform.TransformVector(Vector3.up * sprite.bounds.size.y).magnitude;
    }

    private void CreateVisual()
    {
        if (flame != null) return;
        var child = new GameObject("Booster Flame");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        flame = child.AddComponent<SpriteRenderer>();
        var original = GetComponent<SpriteRenderer>();
        if (original != null)
        {
            if (original.sharedMaterial != null) flame.sharedMaterial = original.sharedMaterial;
            flame.sortingLayerID = original.sortingLayerID;
            flame.sortingOrder = original.sortingOrder - 1;
        }
        flame.color = Color.white;
        flame.enabled = false;
    }

    private void Hide()
    {
        if (flame != null) flame.enabled = false;
        CurrentState = FlameState.Hidden;
        ExhaustDirection = Vector2.zero;
        NominalLength = 0f;
        NominalThickness = 0f;
    }

    private void ReleaseVisual()
    {
        Hide();
        ClearNozzleBinding();
        if (flame == null) return;
        if (Application.isPlaying) Destroy(flame.gameObject);
        else DestroyImmediate(flame.gameObject);
        flame = null;
    }

    private void OnDisable() => ReleaseVisual();
    private void OnDestroy() => ReleaseVisual();
}
