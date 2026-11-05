using UnityEngine;

// A view has no collider and never owns a damage timer. Locked target positions
// and explosion lifetime are controlled by Boss1CombatModel.
internal sealed class Boss1MissileView
{
    private readonly GameObject root;
    private readonly SpriteRenderer rocket;
    private readonly SpriteRenderer explosion;
    private readonly LineRenderer ring, crossA, crossB;
    private readonly Boss1SpriteSet sprites;
    private readonly float h;
    private Vector3 ground, launchPosition;
    private bool groundLocked, launched, warned;
    public bool DamageApplied;

    public Boss1MissileView(Boss1Missile missile, Boss1Controller owner, Material material)
    {
        h = owner.PlayerHeight;
        sprites = owner.Sprites;
        root = new GameObject("Boss1 Missile " + missile.Id);
        ring = Line("Warning circle", material, 40, true);
        crossA = Line("Locked cross A", material, 41, false);
        crossB = Line("Locked cross B", material, 41, false);
        float radius = owner.Settings.ExplosionRadiusH * h;
        ring.positionCount = 48;
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        crossA.positionCount = crossB.positionCount = 2;
        crossA.SetPosition(0, Vector3.left * radius * 0.3f);
        crossA.SetPosition(1, Vector3.right * radius * 0.3f);
        crossB.SetPosition(0, Vector3.down * radius * 0.3f);
        crossB.SetPosition(1, Vector3.up * radius * 0.3f);
        rocket = Sprite("Flying missile", material, 42);
        explosion = Sprite("Explosion", material, 43);
        rocket.transform.localScale = Vector3.one * (h / 1.31f);
        explosion.transform.localScale = Vector3.one * (radius * 2f / 1.28f);
        rocket.enabled = explosion.enabled = false;
    }

    private LineRenderer Line(string name, Material material, int order, bool loop)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.loop = loop;
        line.widthMultiplier = 0.025f * h;
        line.numCapVertices = 3;
        line.sortingOrder = order;
        return line;
    }

    private SpriteRenderer Sprite(string name, Material material, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        return renderer;
    }

    public Vector2 Ground { get { return ground; } }

    public void Render(Boss1Missile missile, Boss1Controller owner)
    {
        float now = owner.Model.Time;
        if (!groundLocked)
        {
            ground = owner.ProjectGround(missile.TargetX * h);
            groundLocked = missile.Locked;
        }
        root.transform.position = ground;
        bool blasting = missile.IsExploding(now, owner.Settings.ExplosionDuration);
        bool flying = now >= missile.LaunchesAt && !blasting;
        if (missile.Locked && !warned)
        {
            warned = true;
            owner.PlayWarning();
        }
        ring.enabled = !blasting;
        crossA.enabled = crossB.enabled = !blasting && missile.Locked;
        Color color = missile.Locked ? new Color(1f, 0.32f, 0.12f, 1f) :
            new Color(0.1f, 0.95f, 1f, 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(now * 12f)));
        ring.startColor = ring.endColor = color;
        crossA.startColor = crossA.endColor = crossB.startColor = crossB.endColor = color;
        ring.widthMultiplier = h * (missile.Locked ? 0.04f : 0.025f);
        rocket.enabled = flying;
        explosion.enabled = blasting;
        if (flying)
        {
            if (!launched) { launchPosition = owner.Muzzle; launched = true; }
            float t = Mathf.InverseLerp(missile.LaunchesAt, missile.ImpactsAt, now);
            Vector3 control = (launchPosition + ground) * 0.5f + Vector3.up * h * 2f;
            Vector3 a = Vector3.Lerp(launchPosition, control, t), b = Vector3.Lerp(control, ground, t);
            rocket.transform.position = Vector3.Lerp(a, b, t);
            Vector3 tangent = b - a;
            rocket.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f);
            rocket.sprite = sprites.Sample(Boss1Pose.MissileFlight, 0, 7, Mathf.Repeat((now - missile.LaunchesAt) * 1.5f, 1f));
        }
        if (blasting)
            explosion.sprite = sprites.Sample(Boss1Pose.MissileExplosion, 0, 7,
                (now - missile.ImpactsAt) / owner.Settings.ExplosionDuration);
    }

    public void Dispose()
    {
        if (root == null) return;
        root.SetActive(false);
        Object.Destroy(root);
    }
}

