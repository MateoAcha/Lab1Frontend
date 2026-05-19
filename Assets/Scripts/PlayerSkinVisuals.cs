using UnityEngine;

public static class PlayerSkinVisuals
{
    public static string GetEquippedSkinColorHex()
    {
        return ToHex(PlayerLoadout.GetSkinColor());
    }

    public static void ApplyEquipped(SpriteRenderer renderer, Material material = null, float alpha = 1f)
    {
        Apply(renderer, PlayerLoadout.EquippedSkinId, GetEquippedSkinColorHex(), material, alpha);
    }

    public static void Apply(SpriteRenderer renderer, int skinId, string skinColorHex, Material material = null, float alpha = 1f)
    {
        if (renderer == null) return;

        if (skinId > 0 && SkinVisualDatabase.TryGetSpriteGlobal(skinId, out Sprite skinSprite))
        {
            renderer.sprite = skinSprite;
            renderer.color = WithAlpha(Color.white, alpha);
        }
        else
        {
            renderer.sprite = SimpleSprite.Square;
            Color fallback = skinId > 0
                ? PlayerLoadout.GetSkinColor(skinId)
                : new Color(0.3f, 0.75f, 1f, 1f);
            renderer.color = WithAlpha(PlayerLoadout.ParseWeaponColor(skinColorHex, fallback), alpha);
        }

        if (material != null)
            renderer.sharedMaterial = material;
    }

    private static string ToHex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}
