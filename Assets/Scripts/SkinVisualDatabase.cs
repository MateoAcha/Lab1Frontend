using System;
using UnityEngine;

[Serializable]
public class SkinVisualEntry
{
    public int skinId;
    public Sprite sprite;
}

public class SkinVisualDatabase : MonoBehaviour
{
    public static SkinVisualDatabase Instance { get; private set; }

    private static SkinVisualEntry[] _cachedSkins;

    public SkinVisualEntry[] skins;

    private void Awake()
    {
        Instance = this;
        _cachedSkins = skins;
    }

    public bool TryGetSprite(int skinId, out Sprite sprite)
    {
        return TryGetSprite(skinId, skins, out sprite);
    }

    public static bool TryGetSpriteGlobal(int skinId, out Sprite sprite)
    {
        if (Instance != null && Instance.TryGetSprite(skinId, out sprite))
            return true;

        return TryGetSprite(skinId, _cachedSkins, out sprite);
    }

    private static bool TryGetSprite(int skinId, SkinVisualEntry[] entries, out Sprite sprite)
    {
        sprite = null;
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            SkinVisualEntry entry = entries[i];
            if (entry != null && entry.skinId == skinId && entry.sprite != null)
            {
                sprite = entry.sprite;
                return true;
            }
        }

        return false;
    }
}
