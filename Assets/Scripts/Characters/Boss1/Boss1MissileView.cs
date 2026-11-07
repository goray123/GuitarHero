using UnityEngine;

// The view only renders the model's warnings, flight and explosions.
internal sealed class Boss1MissileView
{
    private readonly GameObject root;
    private readonly SpriteRenderer rocket;
    private readonly SpriteRenderer explosion;
    private readonly LineRenderer aimLine, ring, crossA, crossB;
    private readonly Boss1SpriteSet sprites;
    private readonly float h;
    private Vector3 launchPosition;
    private bool launched;

    public Boss1MissileView(Boss1Missile missile, Boss1Controller owner, Material material, Vector2 aimOffset)
    {
        h = owner.PlayerHeight;
        sprites = owner.Sprites;
        root = new GameObject("Boss1 Missile " + missile.Id);
        rocket = Sprite("Flying missile", material, 42);
        explosion = Sprite("Explosion", material, 43);
        float radius = owner.Settings.ExplosionRadiusH * h;
        rocket.transform.localScale = Vector3.one * (h / 1.31f);
        explosion.transform.localScale = Vector3.one * (radius * 2f / 1.28f);
        rocket.enabled = explosion.enabled = false;

        if (missile.Kind == Boss1MissileKind.Guided)
        {
            aimLine = Line("Guided aim line", material, 41, false);
            aimLine.positionCount = 2;
            aimLine.SetPosition(0, Vector3.zero);
            aimLine.SetPosition(1, aimOffset);
            rocket.transform.localRotation = Rotation(aimOffset);
            return;
        }

        ring = Line("Warning circle", material, 40, true);
        crossA = Line("Locked cross A", material, 41, false);
        crossB = Line("Locked cross B", material, 41, false);
        ring.positionCount = 48;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        crossA.positionCount = crossB.positionCount = 2;
        crossA.SetPosition(0, Vector3.left * radius * 0.3f);
        crossA.SetPosition(1, Vector3.right * radius * 0.3f);
        crossB.SetPosition(0, Vector3.down * radius * 0.3f);
        crossB.SetPosition(1, Vector3.up * radius * 0.3f);
    }

    private LineRenderer Line(string name, Material material, int order, bool loop)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.loop = loop;
        line.widthMultiplier = (loop ? 0.04f : 0.025f) * h;
        line.numCapVertices = 3;
        line.sortingOrder = order;
        line.startColor = line.endColor = new Color(1f, 0.32f, 0.12f, 1f);
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

    public void Render(Boss1Missile missile, Boss1Controller owner, Vector2 position)
    {
        root.transform.position = position;
        float now = owner.Model.Time;
        bool blasting = missile.IsExploding(now, owner.Settings.ExplosionDuration);
        bool flying = now >= missile.LaunchesAt && !blasting;
        if (aimLine != null)
            aimLine.enabled = now < missile.LaunchesAt;
        else
            ring.enabled = crossA.enabled = crossB.enabled = !blasting;
        rocket.enabled = flying;
        explosion.enabled = blasting;
        if (flying)
        {
            if (aimLine == null)
            {
                if (!launched) { launchPosition = owner.Muzzle; launched = true; }
                float t = Mathf.InverseLerp(missile.LaunchesAt, missile.ImpactsAt, now);
                Vector3 control = (launchPosition + (Vector3)position) * 0.5f + Vector3.up * h * 2f;
                Vector3 a = Vector3.Lerp(launchPosition, control, t);
                Vector3 b = Vector3.Lerp(control, position, t);
                rocket.transform.position = Vector3.Lerp(a, b, t);
                rocket.transform.rotation = Rotation(b - a);
            }
            rocket.sprite = sprites.Sample(Boss1Pose.MissileFlight, 0, 7, Mathf.Repeat((now - missile.LaunchesAt) * 1.5f, 1f));
        }
        if (blasting)
            explosion.sprite = sprites.Sample(Boss1Pose.MissileExplosion, 0, 7,
                (now - missile.ImpactsAt) / owner.Settings.ExplosionDuration);
    }

    private static Quaternion Rotation(Vector2 direction)
    {
        return Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    public void Dispose()
    {
        if (root == null) return;
        root.SetActive(false);
        Object.Destroy(root);
    }
}

