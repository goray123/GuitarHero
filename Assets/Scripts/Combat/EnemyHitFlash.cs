using System.Collections.Generic;
using UnityEngine;

// Match the animated silhouette without replacing its material or tint.
[DefaultExecutionOrder(200), DisallowMultipleComponent]
public sealed class EnemyHitFlash : MonoBehaviour
{
    private readonly List<SpriteRenderer> sources = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> overlays = new List<SpriteRenderer>();
    private double startedAt;
    private float duration;
    private bool playing;

    public void Play(Material material, float seconds)
    {
        if (!isActiveAndEnabled || material == null || seconds <= 0f) return;
        if (overlays.Count == 0) CreateOverlays(material);
        startedAt = Time.unscaledTimeAsDouble;
        duration = seconds;
        playing = true;
        LateUpdate();
    }

    private void CreateOverlays(Material material)
    {
        var boss = GetComponent<Boss1SlamSpriteVisual>();
        var prototype = GetComponent<Boss1PrototypeModel>();
        if (boss != null && prototype != null)
        {
            // Attack warnings and booster flames keep their own colors.
            if (boss.BodyRenderer != null) sources.Add(boss.BodyRenderer);
            if (prototype.BodyRenderers != null) sources.AddRange(prototype.BodyRenderers);
        }
        else sources.AddRange(GetComponentsInChildren<SpriteRenderer>(true));

        foreach (var source in sources)
        {
            var child = new GameObject("Enemy Hit Flash");
            child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform, false);
            var overlay = child.AddComponent<SpriteRenderer>();
            overlay.sharedMaterial = material;
            overlay.enabled = false;
            overlays.Add(overlay);
        }
    }

    private void LateUpdate()
    {
        if (!playing) return;
        float progress = (float)(Time.unscaledTimeAsDouble - startedAt) / duration;
        if (progress >= 1f)
        {
            OnDisable();
            return;
        }
        float alpha = 0.85f * (1f - Mathf.InverseLerp(0.3f, 1f, progress));
        for (int i = 0; i < overlays.Count; i++)
        {
            var source = sources[i];
            var overlay = overlays[i];
            if (source == null || overlay == null) continue;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.drawMode = source.drawMode;
            overlay.size = source.size;
            overlay.maskInteraction = source.maskInteraction;
            overlay.spriteSortPoint = source.spriteSortPoint;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.color = new Color(1f, 1f, 1f, source.color.a * alpha);
            overlay.enabled = source.enabled && !source.forceRenderingOff && source.sprite != null;
        }
    }

    private void OnDisable()
    {
        playing = false;
        foreach (var overlay in overlays)
            if (overlay != null) overlay.enabled = false;
    }

    private void OnDestroy()
    {
        foreach (var overlay in overlays)
            if (overlay != null) Destroy(overlay.gameObject);
    }
}
