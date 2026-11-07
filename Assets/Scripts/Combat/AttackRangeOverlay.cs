using System.Collections.Generic;
using UnityEngine;

internal sealed class AttackRangeOverlay
{
    private readonly Transform parent;
    private readonly Material material;
    private readonly List<LineRenderer> outlines = new List<LineRenderer>();
    private int used;

    public AttackRangeOverlay(Transform parent, Material material)
    {
        this.parent = parent;
        this.material = material;
    }

    public void BeginFrame() => used = 0;

    public void EndFrame()
    {
        for (int i = used; i < outlines.Count; i++)
            outlines[i].enabled = false;
    }

    public void Box(BoxCollider2D box, Color color)
    {
        if (box != null && box.gameObject.activeInHierarchy)
            Box(box.offset, box.size, color, box.transform);
    }

    public void Box(Vector2 center, Vector2 size, Color color, Transform space = null)
    {
        Vector2 half = size * 0.5f;
        LineRenderer line = Outline(4, color);
        line.SetPosition(0, WorldPoint(center + new Vector2(-half.x, -half.y), space));
        line.SetPosition(1, WorldPoint(center + new Vector2(half.x, -half.y), space));
        line.SetPosition(2, WorldPoint(center + new Vector2(half.x, half.y), space));
        line.SetPosition(3, WorldPoint(center + new Vector2(-half.x, half.y), space));
    }

    public void Circle(Vector2 center, float radius, Color color)
    {
        const int points = 48;
        LineRenderer line = Outline(points, color);
        for (int i = 0; i < points; i++)
        {
            float angle = i * Mathf.PI * 2f / points;
            line.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private static Vector3 WorldPoint(Vector2 point, Transform space)
    {
        return space != null ? space.TransformPoint(point) : (Vector3)point;
    }

    private LineRenderer Outline(int points, Color color)
    {
        if (used == outlines.Count)
        {
            var obj = new GameObject("Attack range");
            obj.transform.SetParent(parent, false);
            var created = obj.AddComponent<LineRenderer>();
            created.sharedMaterial = material;
            created.useWorldSpace = true;
            created.loop = true;
            created.widthMultiplier = 0.035f;
            created.sortingOrder = 100;
            outlines.Add(created);
        }
        LineRenderer line = outlines[used++];
        line.positionCount = points;
        line.startColor = line.endColor = color;
        line.enabled = true;
        return line;
    }
}
