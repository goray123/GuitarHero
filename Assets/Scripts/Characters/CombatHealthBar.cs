using UnityEngine;
using UnityEngine.UI;

internal sealed class CombatHealthBar
{
    private readonly Image frame;
    private readonly HealthBarUI feedback;

    internal CombatHealthBar(Transform parent, Color color, HealthBarUI feedback,
        Color damageColor, float damageDelay, float damageDuration)
    {
        this.feedback = feedback;
        frame = CreateImage("Amp Frame", parent, "AmpFrame");
        frame.type = Image.Type.Sliced;

        Image damageFill = CreateFill("Damage Trail", damageColor);
        Image fill = CreateFill("Health Signal", color);
        feedback.ConfigureFills(fill, damageFill, damageDelay, damageDuration);
    }

    private Image CreateFill(string name, Color color)
    {
        Image fill = CreateImage(name, frame.transform, "SignalFill");
        fill.color = color;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        var rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 6f);
        rect.offsetMax = new Vector2(-16f, -6f);
        return fill;
    }

    internal void SetSize(Vector2 size)
    {
        // Work in sprite pixels so the end plates keep their proportions in both canvases.
        float scale = Mathf.Min(Mathf.Max(0.001f, size.y) / 24f, Mathf.Max(0.001f, size.x) / 34f);
        frame.rectTransform.localScale = Vector3.one * scale;
        frame.rectTransform.sizeDelta = new Vector2(Mathf.Max(34f, size.x / scale), 24f);
    }

    internal void SetHealth(float ratio) => feedback.SetHealth(ratio, 1f);

    private static Image CreateImage(string name, Transform parent, string spriteName)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = Resources.Load<Sprite>("CombatHud/" + spriteName);
        image.raycastTarget = false;
        return image;
    }
}
