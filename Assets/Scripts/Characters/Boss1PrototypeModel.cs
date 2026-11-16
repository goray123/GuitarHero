using UnityEngine;

// Disposable geometric preview, visible in both Scene and Game views.
[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(Boss1ProximityAttack))]
public class Boss1PrototypeModel : MonoBehaviour
{
    private Transform root, torso, head, eye, leftArm, rightArm, leftFist, rightFist, zone;
    private SpriteRenderer zoneRenderer;
    private SpriteRenderer[] bodyRenderers;
    private Sprite square, circle;
    private Texture2D circleTexture;
    private Boss1ProximityAttack owner;
    private Boss1AttackHitboxes hitboxes;
    private Boss1LeapSlam leap;
    private Transform landingMarker, waveLeft, waveRight, booster;
    private Boss1BoostStep boostStep;
    private Boss1Backstep backstep;
    private Boss1BoosterFlameVisual boosterFlame;
    private Transform backstepPath, backstepEnd;
    private Boss1RearMissiles rearMissiles;
    private Transform rearLauncher;
    private readonly Transform[] rearRockets = new Transform[2];
    private readonly Transform[] rearTargets = new Transform[2];
    private readonly Transform[] rearCrossX = new Transform[2];
    private readonly Transform[] rearCrossY = new Transform[2];
    private Boss1AimedMissile aimedMissile;
    private Boss1MissileArt missileArt;
    private Transform aimedLine, aimedRocket, aimedBlast, aimedCrossX, aimedCrossY, aimedMuzzle;

    private void OnEnable()
    {
        owner = GetComponent<Boss1ProximityAttack>();
        hitboxes = GetComponent<Boss1AttackHitboxes>();
        leap = GetComponent<Boss1LeapSlam>();
        backstep = GetComponent<Boss1Backstep>();
        boostStep = GetComponent<Boss1BoostStep>();
        boosterFlame = GetComponent<Boss1BoosterFlameVisual>();
        rearMissiles = GetComponent<Boss1RearMissiles>();
        aimedMissile = GetComponent<Boss1AimedMissile>();
        missileArt = GetComponent<Boss1MissileArt>();
        var oldSprite = GetComponent<SpriteRenderer>();
        if (oldSprite != null) oldSprite.enabled = false;
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        Build();
        Pose();
    }

    private void Build()
    {
        if (root != null) return;
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            Vector2.one * 0.5f, Texture2D.whiteTexture.width);
        square.hideFlags = HideFlags.HideAndDontSave;
        circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        circleTexture.hideFlags = HideFlags.HideAndDontSave;
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * 32f)));
        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        circle = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64);
        circle.hideFlags = HideFlags.HideAndDontSave;
        root = new GameObject("Prototype Shapes").transform;
        root.gameObject.hideFlags = HideFlags.HideAndDontSave;
        root.SetParent(transform, false);
        Color steel = new Color(0.25f, 0.34f, 0.43f);
        torso = Part("Torso", false, steel, 6);
        head = Part("Head", true, new Color(0.4f, 0.5f, 0.58f), 7);
        eye = Part("Facing Sensor", true, Color.cyan, 8);
        leftArm = Part("Rear Arm", false, steel, 5);
        rightArm = Part("Front Arm", false, steel * 1.3f, 8);
        leftFist = Part("Rear Fist", true, new Color(0.9f, 0.48f, 0.12f), 7);
        rightFist = Part("Front Fist", true, new Color(1f, 0.65f, 0.18f), 9);
        var leftLeg = Part("Left Leg", false, steel, 5);
        var rightLeg = Part("Right Leg", false, steel, 5);
        Set(leftLeg, new Vector2(-0.38f, 0.42f), new Vector2(0.5f, 0.84f));
        Set(rightLeg, new Vector2(0.38f, 0.42f), new Vector2(0.5f, 0.84f));
        var bodyParts = new[] { torso, head, eye, leftArm, rightArm, leftFist, rightFist, leftLeg, rightLeg };
        bodyRenderers = new SpriteRenderer[bodyParts.Length];
        for (int i = 0; i < bodyParts.Length; i++) bodyRenderers[i] = bodyParts[i].GetComponent<SpriteRenderer>();
        zone = Part("Attack Area", false, Color.clear, 4);
        zoneRenderer = zone.GetComponent<SpriteRenderer>();
        booster = Part("Booster", true, Color.cyan, 4);
        landingMarker = Part("Leap Landing Circle", true, new Color(1, 0.75f, 0.1f, 0.35f), 4);
        waveLeft = Part("Left Shockwave", false, new Color(1, 0.3f, 0.1f, 0.65f), 8);
        waveRight = Part("Right Shockwave", false, new Color(1, 0.3f, 0.1f, 0.65f), 8);
        // Ground markers must neither follow the jumping body nor inherit its turning pose.
        landingMarker.SetParent(transform, false);
        waveLeft.SetParent(transform, false);
        waveRight.SetParent(transform, false);
        backstepPath = Part("Backstep Path (No Damage)", false, new Color(0.1f, 0.8f, 1f, 0.4f), 4);
        backstepEnd = Part("Backstep Stop", true, new Color(0.1f, 0.8f, 1f, 0.65f), 4);
        backstepPath.SetParent(transform, false);
        backstepEnd.SetParent(transform, false);
        rearLauncher = Part("Rear Missile Launcher", false, new Color(0.5f, 0.16f, 0.12f), 7);
        for (int i = 0; i < 2; i++)
        {
            rearRockets[i] = Part("Rear Missile " + i, false, new Color(1f, 0.5f, 0.12f), 12);
            rearTargets[i] = Part("Rear Blast Radius " + i, true, Color.yellow, 10);
            rearCrossX[i] = Part("Rear Aim Horizontal " + i, false, Color.white, 11);
            rearCrossY[i] = Part("Rear Aim Vertical " + i, false, Color.white, 11);
            rearRockets[i].SetParent(transform, false); rearTargets[i].SetParent(transform, false);
            rearCrossX[i].SetParent(transform, false); rearCrossY[i].SetParent(transform, false);
        }
        aimedLine = Part("Aimed Missile Line", false, Color.yellow, 13);
        aimedRocket = Part("Aimed Missile Rectangle", false, new Color(1f, 0.45f, 0.1f), 14);
        aimedBlast = Part("Aimed Explosion Radius", true, new Color(1f, 0.15f, 0.05f, 0.5f), 12);
        aimedCrossX = Part("Aimed Cross Horizontal", false, Color.white, 14);
        aimedCrossY = Part("Aimed Cross Vertical", false, Color.white, 14);
        aimedMuzzle = Part("Aimed Launcher", false, new Color(0.75f, 0.2f, 0.1f), 12);
        foreach (var part in new[] { aimedLine, aimedRocket, aimedBlast, aimedCrossX, aimedCrossY, aimedMuzzle })
            part.SetParent(transform, false);
    }

    private Transform Part(string label, bool round, Color color, int order)
    {
        var part = new GameObject(label);
        part.hideFlags = HideFlags.HideAndDontSave;
        part.transform.SetParent(root, false);
        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = round ? circle : square;
        renderer.color = color;
        renderer.sortingOrder = order;
        return part.transform;
    }

    private static void Set(Transform part, Vector2 position, Vector2 size)
    {
        part.localPosition = position;
        part.localScale = new Vector3(size.x, size.y, 1f);
    }

    private static void Arm(Transform part, Vector2 shoulder, Vector2 fist, float thickness)
    {
        Set(part, (shoulder + fist) * 0.5f, new Vector2(Vector2.Distance(shoulder, fist), thickness));
        Vector2 delta = fist - shoulder;
        part.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private void LateUpdate() { if (root == null) Build(); Pose(); }

    // 내려찍기 그림을 표시할 때 몸체만 숨기고 공격 범위와 미사일 표시는 유지.
    public void SetBodyVisible(bool visible)
    {
        if (bodyRenderers == null) return;
        foreach (var renderer in bodyRenderers)
            if (renderer != null) renderer.enabled = visible;
    }

    private void Pose()
    {
        if (owner == null || root == null) return;
        float h = owner.Height;
        var clock = owner.Timeline;
        float facing = owner.Facing;
        if (clock.Attack == 3 && clock.Elapsed < owner.TurnDuration)
            facing = Mathf.Lerp(owner.PreviousFacing, owner.Facing, Mathf.SmoothStep(0, 1, clock.Elapsed / owner.TurnDuration));
        root.localScale = new Vector3(facing, 1, 1);
        booster.gameObject.SetActive(false);
        Set(torso, new Vector2(0, 1.75f * h), new Vector2(1.55f * h, 2.1f * h));
        Set(head, new Vector2(0, 3.05f * h), Vector2.one * 0.8f * h);
        Set(eye, new Vector2(0.24f * h, 3.08f * h), Vector2.one * 0.18f * h);
        Vector2 resting = new Vector2(0.85f * h, 1.55f * h);
        Vector2 fist = resting;
        Vector2 otherFist = new Vector2(-0.85f * h, 1.55f * h);
        zoneRenderer.enabled = clock.Attack != 0;
        if (clock.Attack != 0)
        {
            hitboxes.GetArea(clock.Attack, out var center, out var size);
            Set(zone, center, size);
            var stage = clock.CurrentStage;
            float p = Mathf.Clamp01(clock.StageProgress);
            Vector2 ready = clock.Attack == 1 ? new Vector2(-0.65f * h, 0.65f * h) :
                clock.Attack == 2 ? new Vector2(-0.6f * h, center.y) : new Vector2(0.45f * h, 3.8f * h);
            Vector2 impact = clock.Attack == 3 ? new Vector2(center.x, 0.35f * h) :
                new Vector2(center.x + size.x * 0.35f, center.y);
            fist = stage == BossMeleeTimeline.Stage.Windup ? Vector2.Lerp(resting, ready, Mathf.Min(1f, p * 2f)) :
                stage == BossMeleeTimeline.Stage.Active ? Vector2.Lerp(ready, impact, Mathf.Min(1f, p * 4f)) :
                Vector2.Lerp(impact, resting, p);
            if (clock.Attack == 3) otherFist = fist + new Vector2(-0.65f * h, 0);
            zoneRenderer.color = stage == BossMeleeTimeline.Stage.Windup ? new Color(1f, 0.75f, 0.1f, 0.18f) :
                stage == BossMeleeTimeline.Stage.Active ? new Color(1f, 0.18f, 0.12f, 0.5f) : new Color(0.1f, 0.85f, 1f, 0.12f);
        }
        if (leap != null && leap.IsBusy)
        {
            bool crouching = leap.CurrentPhase == Boss1LeapSlam.Phase.Windup;
            bool flying = leap.CurrentPhase == Boss1LeapSlam.Phase.Airborne;
            root.localScale = new Vector3(facing, crouching ? Mathf.Lerp(1f, 0.7f, leap.Progress) : 1f, 1);
            booster.gameObject.SetActive(crouching || flying);
            Set(booster, new Vector2(-0.7f * h, 1.7f * h), new Vector2(0.5f, flying ? 1.2f : 0.5f) * h);
            fist = flying ? new Vector2(0.6f * h, 3.7f * h) :
                crouching ? new Vector2(0.9f * h, 1f * h) : new Vector2(0.7f * h, 0.35f * h);
            otherFist = new Vector2(-fist.x, fist.y);
        }
        if (boostStep != null && boostStep.IsBusy)
        {
            bool moving = boostStep.CurrentPhase == Boss1BoostStep.Phase.Moving;
            root.localScale = new Vector3(boostStep.Facing, moving ? 0.9f : 1f, 1f);
            booster.gameObject.SetActive(boostStep.CurrentPhase != Boss1BoostStep.Phase.Resting);
            Set(booster, new Vector2(-1.05f * h, 1.9f * h), new Vector2(moving ? 1.1f : 0.25f, moving ? 0.4f : 0.2f) * h);
            booster.GetComponent<SpriteRenderer>().color = Color.cyan;
        }
        bool retreating = backstep != null && backstep.IsBusy;
        backstepPath.gameObject.SetActive(retreating);
        backstepEnd.gameObject.SetActive(retreating);
        if (retreating)
        {
            bool moving = backstep.CurrentPhase == Boss1Backstep.Phase.Moving;
            root.localScale = new Vector3(backstep.Facing, moving ? 0.85f : 0.95f, 1f);
            booster.gameObject.SetActive(backstep.CurrentPhase != Boss1Backstep.Phase.Settling);
            Set(booster, new Vector2(0.9f * h, 1.65f * h), new Vector2(moving ? 1.2f : 0.5f, 0.4f) * h);
            fist = new Vector2(0.8f * h, 2.1f * h);
            otherFist = new Vector2(-0.6f * h, 2.1f * h);
            backstepPath.position = (backstep.StartPoint + backstep.EndPoint) * 0.5f + Vector2.up * 0.05f;
            backstepPath.localScale = new Vector3(Mathf.Abs(backstep.EndPoint.x - backstep.StartPoint.x) / Mathf.Abs(transform.lossyScale.x), 0.08f * h, 1f);
            backstepEnd.position = backstep.EndPoint;
            backstepEnd.localScale = new Vector3(0.35f * h, 0.15f * h, 1f);
        }
        if (boosterFlame == null) boosterFlame = GetComponent<Boss1BoosterFlameVisual>();
        if (boosterFlame != null && boosterFlame.ReplacesPrototypeFlame)
            booster.gameObject.SetActive(false);
        UpdateLeapMarkers();
        UpdateRearMissiles(h);
        UpdateAimedMissile(h);
        Set(rightFist, fist, Vector2.one * (clock.Attack == 2 ? 0.4f : 0.65f) * h);
        Set(leftFist, otherFist, Vector2.one * 0.65f * h);
        Arm(rightArm, new Vector2(0.6f * h, 2.4f * h), fist, 0.35f * h);
        Arm(leftArm, new Vector2(-0.6f * h, 2.4f * h), otherFist, 0.35f * h);
    }

    private void UpdateAimedMissile(float h)
    {
        bool aiming = aimedMissile != null && aimedMissile.AimVisible;
        bool flying = aimedMissile != null && aimedMissile.CurrentPhase == Boss1AimedMissile.Phase.Flying;
        bool exploding = aimedMissile != null && aimedMissile.CurrentPhase == Boss1AimedMissile.Phase.Exploding;
        aimedLine.gameObject.SetActive(aiming);
        aimedCrossX.gameObject.SetActive(aiming); aimedCrossY.gameObject.SetActive(aiming);
        aimedMuzzle.gameObject.SetActive(aiming);
        if (missileArt == null) missileArt = GetComponent<Boss1MissileArt>();
        aimedRocket.gameObject.SetActive(flying && !(missileArt != null && missileArt.HasMissileArt));
        aimedBlast.gameObject.SetActive(exploding && !(missileArt != null && missileArt.HasExplosionArt));
        if (aimedMissile == null) return;
        Vector2 direction = aimedMissile.Direction;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (aiming)
        {
            bool locked = aimedMissile.CurrentPhase == Boss1AimedMissile.Phase.Locked;
            Vector2 a = aimedMissile.AimOrigin, b = aimedMissile.AimEnd;
            aimedMuzzle.position = a; aimedMuzzle.localScale = new Vector3(0.25f * h, 0.4f * h, 1);
            aimedMuzzle.rotation = Quaternion.Euler(0, 0, angle - 90f);
            aimedLine.position = (a + b) * 0.5f;
            aimedLine.localScale = new Vector3(Vector2.Distance(a, b) / Mathf.Abs(transform.lossyScale.x), (locked ? 0.045f : 0.025f) * h, 1);
            aimedLine.rotation = Quaternion.Euler(0, 0, angle);
            aimedLine.GetComponent<SpriteRenderer>().color = locked ? new Color(1f, 0.25f, 0.1f) :
                new Color(1, 0.85f, 0.1f, 0.35f + 0.5f * Mathf.Abs(Mathf.Sin(aimedMissile.Clock * 12f)));
            aimedCrossX.position = aimedCrossY.position = b;
            aimedCrossX.localScale = new Vector3((locked ? 0.35f : 0.2f) * h, 0.04f * h, 1);
            aimedCrossY.localScale = new Vector3(0.04f * h, (locked ? 0.35f : 0.2f) * h, 1);
        }
        if (flying)
        {
            aimedRocket.position = aimedMissile.ProjectilePosition;
            aimedRocket.localScale = new Vector3(0.18f * h, 0.45f * h, 1);
            aimedRocket.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
        if (exploding)
        {
            aimedBlast.position = aimedMissile.ExplosionPoint;
            float diameter = aimedMissile.ExplosionRadius * 2f;
            aimedBlast.localScale = new Vector3(diameter / Mathf.Abs(transform.lossyScale.x), diameter / Mathf.Abs(transform.lossyScale.y), 1);
        }
    }

    private void UpdateRearMissiles(float h)
    {
        bool active = rearMissiles != null && rearMissiles.HasLiveMissiles;
        rearLauncher.gameObject.SetActive(active);
        Set(rearLauncher, new Vector2(-0.7f * h, 2.7f * h), new Vector2(0.55f, 0.9f) * h);
        rearLauncher.localRotation = Quaternion.Euler(0, 0, 35f);
        for (int i = 0; i < 2; i++)
        {
            if (missileArt == null) missileArt = GetComponent<Boss1MissileArt>();
            bool useMissileArt = missileArt != null && missileArt.HasMissileArt;
            bool useExplosionArt = missileArt != null && missileArt.HasExplosionArt;
            bool marker = active && (rearMissiles.WarningVisible || (rearMissiles.Exploding && !useExplosionArt));
            rearTargets[i].gameObject.SetActive(marker);
            rearCrossX[i].gameObject.SetActive(active && rearMissiles.WarningVisible);
            rearCrossY[i].gameObject.SetActive(active && rearMissiles.WarningVisible);
            rearRockets[i].gameObject.SetActive(active && rearMissiles.InFlight && !useMissileArt);
            if (!active) continue;
            Vector2 point = rearMissiles.Target(i);
            float diameter = rearMissiles.Radius * 2f;
            rearTargets[i].position = point;
            rearTargets[i].localScale = new Vector3(diameter / Mathf.Abs(transform.lossyScale.x), diameter / Mathf.Abs(transform.lossyScale.y), 1f);
            rearTargets[i].GetComponent<SpriteRenderer>().color = rearMissiles.Exploding ?
                new Color(1, 0.15f, 0.05f, 0.65f) : new Color(1, 0.75f, 0.1f, 0.28f);
            rearCrossX[i].position = rearCrossY[i].position = point;
            rearCrossX[i].localScale = new Vector3(0.25f * h, 0.04f * h, 1);
            rearCrossY[i].localScale = new Vector3(0.04f * h, 0.25f * h, 1);
            rearRockets[i].position = rearMissiles.MissilePosition(i);
            rearRockets[i].localScale = new Vector3(0.18f * h, 0.45f * h, 1);
            Vector2 tangent = point - rearMissiles.LaunchPoint + Vector2.up * (4f * rearMissiles.Radius * (1f - 2f * rearMissiles.FlightProgress));
            rearRockets[i].rotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f);
        }
    }

    private void UpdateLeapMarkers()
    {
        bool marker = leap != null && leap.MarkerVisible;
        landingMarker.gameObject.SetActive(marker);
        if (marker)
        {
            landingMarker.position = leap.LandingPoint;
            landingMarker.localScale = new Vector3(2f * leap.Radius / Mathf.Abs(transform.lossyScale.x),
                2f * leap.Radius / Mathf.Abs(transform.lossyScale.y), 1);
            landingMarker.GetComponent<SpriteRenderer>().color = leap.CurrentPhase == Boss1LeapSlam.Phase.Impact ?
                new Color(1, 0.15f, 0.1f, 0.5f) : new Color(1, 0.75f, 0.1f, 0.3f);
        }
        bool waves = leap != null && leap.WavesActive;
        waveLeft.gameObject.SetActive(waves);
        waveRight.gameObject.SetActive(waves);
        if (!waves) return;
        waveLeft.position = leap.WaveCenter(-1);
        waveRight.position = leap.WaveCenter(1);
        Vector3 size = new Vector3(leap.WaveWidth / Mathf.Abs(transform.lossyScale.x),
            leap.WaveHeight / Mathf.Abs(transform.lossyScale.y), 1);
        waveLeft.localScale = waveRight.localScale = size;
    }

    private void OnDisable()
    {
        Release(root != null ? root.gameObject : null);
        Release(landingMarker != null ? landingMarker.gameObject : null);
        Release(waveLeft != null ? waveLeft.gameObject : null);
        Release(waveRight != null ? waveRight.gameObject : null);
        Release(backstepPath != null ? backstepPath.gameObject : null);
        Release(backstepEnd != null ? backstepEnd.gameObject : null);
        foreach (var part in new[] { aimedLine, aimedRocket, aimedBlast, aimedCrossX, aimedCrossY, aimedMuzzle })
            Release(part != null ? part.gameObject : null);
        for (int i = 0; i < 2; i++)
        {
            Release(rearRockets[i] != null ? rearRockets[i].gameObject : null);
            Release(rearTargets[i] != null ? rearTargets[i].gameObject : null);
            Release(rearCrossX[i] != null ? rearCrossX[i].gameObject : null);
            Release(rearCrossY[i] != null ? rearCrossY[i].gameObject : null);
        }
        Release(square); Release(circle); Release(circleTexture);
        root = null;
    }

    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
