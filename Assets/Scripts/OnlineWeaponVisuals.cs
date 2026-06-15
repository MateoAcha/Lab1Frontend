using System.Collections.Generic;
using UnityEngine;

public struct OnlineCarriedWeaponVisual
{
    public Sprite sprite;
    public Vector2 offset;
    public Vector2 scale;
    public float rotationOffset;
    public int sortingOrderOffset;
}

public static class OnlineWeaponVisuals
{
    private static readonly Dictionary<Texture2D, Sprite> TextureSprites = new Dictionary<Texture2D, Sprite>();

    public static bool TryResolveCarriedVisual(
        WeaponKind weaponKind,
        int weaponItemId,
        GameBootstrap bootstrap,
        out OnlineCarriedWeaponVisual visual)
    {
        visual = new OnlineCarriedWeaponVisual();

        if (weaponKind == WeaponKind.Sword)
        {
            if (WeaponVisualDatabase.TryGetSwordVisualGlobal(weaponItemId, out WeaponVisualEntry entry))
            {
                Sprite sprite = entry.ResolveSwordSwingSprite();
                if (sprite != null)
                {
                    visual = new OnlineCarriedWeaponVisual
                    {
                        sprite = sprite,
                        offset = entry.carriedSwordVisualOffset,
                        scale = entry.carriedSwordVisualScale,
                        rotationOffset = entry.carriedSwordVisualRotationOffset,
                        sortingOrderOffset = entry.carriedSwordSortingOrderOffset
                    };
                    return true;
                }
            }

            if (bootstrap != null)
            {
                Sprite sprite = ResolveSprite(bootstrap.swordSwingSprite, bootstrap.swordSwingTexture);
                if (sprite != null)
                {
                    visual = new OnlineCarriedWeaponVisual
                    {
                        sprite = sprite,
                        offset = bootstrap.carriedSwordVisualOffset,
                        scale = bootstrap.carriedSwordVisualScale,
                        rotationOffset = bootstrap.carriedSwordVisualRotationOffset,
                        sortingOrderOffset = bootstrap.carriedSwordSortingOrderOffset
                    };
                    return true;
                }
            }
        }

        if (weaponKind == WeaponKind.Spear)
        {
            if (WeaponVisualDatabase.TryGetSpearVisualGlobal(weaponItemId, out WeaponVisualEntry entry))
            {
                Sprite sprite = entry.ResolveSpearSprite();
                if (sprite != null)
                {
                    visual = new OnlineCarriedWeaponVisual
                    {
                        sprite = sprite,
                        offset = entry.carriedSpearVisualOffset,
                        scale = entry.carriedSpearVisualScale,
                        rotationOffset = entry.carriedSpearVisualRotationOffset,
                        sortingOrderOffset = Mathf.Max(1, entry.carriedSpearSortingOrderOffset)
                    };
                    return true;
                }
            }

            if (bootstrap != null)
            {
                Sprite sprite = ResolveSprite(bootstrap.spearSprite, bootstrap.spearTexture);
                if (sprite != null)
                {
                    visual = new OnlineCarriedWeaponVisual
                    {
                        sprite = sprite,
                        offset = bootstrap.carriedSpearVisualOffset,
                        scale = bootstrap.carriedSpearVisualScale,
                        rotationOffset = bootstrap.carriedSpearVisualRotationOffset,
                        sortingOrderOffset = Mathf.Max(1, bootstrap.carriedSpearSortingOrderOffset)
                    };
                    return true;
                }
            }
        }

        return false;
    }

    public static Sprite ResolveAttackSprite(WeaponKind weaponKind, int weaponItemId, GameBootstrap bootstrap)
    {
        if (weaponKind == WeaponKind.Sword)
        {
            if (WeaponVisualDatabase.TryGetSwordVisualGlobal(weaponItemId, out WeaponVisualEntry entry))
            {
                Sprite sprite = entry.ResolveSwordSwingSprite();
                if (sprite != null)
                    return sprite;
            }

            return bootstrap != null
                ? ResolveSprite(bootstrap.swordSwingSprite, bootstrap.swordSwingTexture)
                : null;
        }

        if (weaponKind == WeaponKind.Spear)
        {
            if (WeaponVisualDatabase.TryGetSpearVisualGlobal(weaponItemId, out WeaponVisualEntry entry))
            {
                Sprite sprite = entry.ResolveSpearSprite();
                if (sprite != null)
                    return sprite;
            }

            return bootstrap != null
                ? ResolveSprite(bootstrap.spearSprite, bootstrap.spearTexture)
                : null;
        }

        return null;
    }

    private static Sprite ResolveSprite(Sprite sprite, Texture2D texture)
    {
        if (sprite != null)
            return sprite;

        if (texture == null)
            return null;

        if (!TextureSprites.TryGetValue(texture, out Sprite cached) || cached == null)
        {
            cached = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1, texture.width));
            TextureSprites[texture] = cached;
        }

        return cached;
    }
}
