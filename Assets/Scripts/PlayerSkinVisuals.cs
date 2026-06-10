using UnityEngine;

public static class PlayerSkinVisuals
{
    public static string GetEquippedSkinColorHex()
    {
        return "#FFFFFF";
    }

    public static void ApplyEquipped(SpriteRenderer renderer, Material material = null, float alpha = 1f)
    {
        Apply(renderer, PlayerLoadout.EquippedSkinId, material, alpha);
    }

    public static void Apply(SpriteRenderer renderer, int skinId, Material material = null, float alpha = 1f)
    {
        if (renderer == null) return;

        SkinSpriteSet spriteSet = SkinVisualDatabase.GetSpriteSetOrDefault(skinId);
        Sprite sprite = spriteSet.PreviewOrFirstSprite;
        renderer.sprite = sprite != null ? sprite : SimpleSprite.Square;
        renderer.color = WithAlpha(Color.white, alpha);

        if (material != null)
            renderer.sharedMaterial = material;
    }

    public static void Apply(SpriteRenderer renderer, int skinId, string skinColorHex, Material material = null, float alpha = 1f)
    {
        Apply(renderer, skinId, material, alpha);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}
