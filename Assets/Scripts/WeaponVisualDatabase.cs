using System;
using UnityEngine;

[Serializable]
public class WeaponVisualEntry
{
    public int weaponItemId;
    public Sprite swordSwingSprite;
    public Texture2D swordSwingTexture;
    public Vector2 swordSwingVisualOffset = Vector2.zero;
    public Vector2 swordSwingVisualScale = Vector2.one;
    public float swordSwingVisualRotationOffset;
    public float swordSwingDurationMultiplier = 1.5f;
    [Header("Carried Sword")]
    public Vector2 carriedSwordVisualOffset = new Vector2(-0.18f, 0.18f);
    public Vector2 carriedSwordVisualScale = Vector2.one;
    public float carriedSwordVisualRotationOffset = -35f;
    public int carriedSwordSortingOrderOffset = -1;
    [Header("Spear")]
    public Sprite spearSprite;
    public Texture2D spearTexture;
    public Vector2 spearVisualOffset = Vector2.zero;
    public Vector2 spearVisualScale = Vector2.one;
    public float spearVisualRotationOffset;
    public float spearThrustDistance = 0.35f;
    [Header("Carried Spear")]
    public Vector2 carriedSpearVisualOffset = new Vector2(-0.18f, 0.12f);
    public Vector2 carriedSpearVisualScale = Vector2.one;
    public float carriedSpearVisualRotationOffset = -35f;
    public int carriedSpearSortingOrderOffset = 1;

    private Texture2D _cachedSwordTexture;
    private Sprite _cachedSwordTextureSprite;
    private Texture2D _cachedSpearTexture;
    private Sprite _cachedSpearTextureSprite;

    public Sprite ResolveSwordSwingSprite()
    {
        if (swordSwingSprite != null)
            return swordSwingSprite;

        if (swordSwingTexture == null)
            return null;

        if (_cachedSwordTextureSprite == null || _cachedSwordTexture != swordSwingTexture)
        {
            _cachedSwordTexture = swordSwingTexture;
            _cachedSwordTextureSprite = Sprite.Create(
                swordSwingTexture,
                new Rect(0f, 0f, swordSwingTexture.width, swordSwingTexture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1, swordSwingTexture.width));
        }

        return _cachedSwordTextureSprite;
    }

    public Sprite ResolveSpearSprite()
    {
        if (spearSprite != null)
            return spearSprite;

        if (spearTexture == null)
            return null;

        if (_cachedSpearTextureSprite == null || _cachedSpearTexture != spearTexture)
        {
            _cachedSpearTexture = spearTexture;
            _cachedSpearTextureSprite = Sprite.Create(
                spearTexture,
                new Rect(0f, 0f, spearTexture.width, spearTexture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1, spearTexture.width));
        }

        return _cachedSpearTextureSprite;
    }
}

public class WeaponVisualDatabase : MonoBehaviour
{
    public static WeaponVisualDatabase Instance { get; private set; }

    private static WeaponVisualEntry[] _cachedWeapons;

    public WeaponVisualEntry[] weapons;

    private void OnValidate()
    {
        if (weapons == null)
            return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null && weapons[i].swordSwingDurationMultiplier <= 0f)
                weapons[i].swordSwingDurationMultiplier = 1.5f;
            if (weapons[i] != null && weapons[i].spearThrustDistance < 0f)
                weapons[i].spearThrustDistance = 0f;
        }
    }

    private void Awake()
    {
        Register(this);
    }

    public static void Register(WeaponVisualDatabase database)
    {
        if (database == null)
            return;

        Instance = database;
        _cachedWeapons = database.weapons;
    }

    public bool TryGetSwordVisual(int weaponItemId, out WeaponVisualEntry visual)
    {
        return TryGetWeaponVisual(weaponItemId, weapons, out visual);
    }

    public static bool TryGetSwordVisualGlobal(int weaponItemId, out WeaponVisualEntry visual)
    {
        if (Instance != null && Instance.TryGetSwordVisual(weaponItemId, out visual))
            return true;

        return TryGetWeaponVisual(weaponItemId, _cachedWeapons, out visual);
    }

    public bool TryGetSpearVisual(int weaponItemId, out WeaponVisualEntry visual)
    {
        return TryGetWeaponVisual(weaponItemId, weapons, out visual);
    }

    public static bool TryGetSpearVisualGlobal(int weaponItemId, out WeaponVisualEntry visual)
    {
        if (Instance != null && Instance.TryGetSpearVisual(weaponItemId, out visual))
            return true;

        return TryGetWeaponVisual(weaponItemId, _cachedWeapons, out visual);
    }

    private static bool TryGetWeaponVisual(int weaponItemId, WeaponVisualEntry[] entries, out WeaponVisualEntry visual)
    {
        visual = null;
        if (weaponItemId <= 0 || entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            WeaponVisualEntry entry = entries[i];
            if (entry != null && entry.weaponItemId == weaponItemId)
            {
                visual = entry;
                return true;
            }
        }

        return false;
    }
}
