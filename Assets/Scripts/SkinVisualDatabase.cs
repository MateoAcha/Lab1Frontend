using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SkinVisualEntry
{
    public int skinId;
    [FormerlySerializedAs("sprite")]
    public Sprite previewSprite;
    [Header("Quick Assign Sprite Sheets")]
    public Texture2D idleSheet;
    public Texture2D walkSheet;
    public Texture2D attackSheet;
    public int frameWidth = 32;
    public int frameHeight = 32;
    [Header("Optional Per-Frame Overrides")]
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] attackSprites;

    public void EnsureDefaults()
    {
        if (frameWidth <= 0)
            frameWidth = 32;
        if (frameHeight <= 0)
            frameHeight = 32;
    }

    public SkinSpriteSet ToSpriteSet()
    {
        EnsureDefaults();
        int width = Mathf.Max(1, frameWidth);
        int height = Mathf.Max(1, frameHeight);

        return new SkinSpriteSet(
            previewSprite,
            ResolveFrames(idleSprites, idleSheet, width, height),
            ResolveFrames(walkSprites, walkSheet, width, height),
            ResolveFrames(attackSprites, attackSheet, width, height));
    }

    private static Sprite[] ResolveFrames(Sprite[] spriteFrames, Texture2D sheet, int frameWidth, int frameHeight)
    {
        Sprite[] frames = NormalizeSprites(spriteFrames);
        if (frames != null)
            return frames;

        return SkinVisualDatabase.CreateSpritesFromSheet(sheet, frameWidth, frameHeight);
    }

    private static Sprite[] NormalizeSprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        int count = 0;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                count++;
        }

        if (count == 0)
            return null;

        Sprite[] normalized = new Sprite[count];
        int write = 0;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                normalized[write++] = sprites[i];
        }

        return normalized;
    }
}

public struct SkinSpriteSet
{
    public Sprite previewSprite;
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] attackSprites;

    public SkinSpriteSet(Sprite previewSprite, Sprite[] idleSprites, Sprite[] walkSprites, Sprite[] attackSprites)
    {
        this.previewSprite = previewSprite;
        this.idleSprites = idleSprites;
        this.walkSprites = walkSprites;
        this.attackSprites = attackSprites;
    }

    public bool HasAnySprite
    {
        get
        {
            return previewSprite != null
                || HasSprites(idleSprites)
                || HasSprites(walkSprites)
                || HasSprites(attackSprites);
        }
    }

    public Sprite PreviewOrFirstSprite
    {
        get
        {
            if (previewSprite != null)
                return previewSprite;
            if (HasSprites(idleSprites))
                return idleSprites[0];
            if (HasSprites(walkSprites))
                return walkSprites[0];
            if (HasSprites(attackSprites))
                return attackSprites[0];
            return null;
        }
    }

    public SkinSpriteSet WithFallback(SkinSpriteSet fallback)
    {
        Sprite preview = PreviewOrFirstSprite;
        Sprite[] previewClip = preview != null ? new[] { preview } : null;
        Sprite[] idle = HasSprites(idleSprites)
            ? idleSprites
            : (HasSprites(previewClip) ? previewClip : fallback.idleSprites);
        Sprite[] walk = HasSprites(walkSprites)
            ? walkSprites
            : (HasSprites(idle) ? idle : fallback.walkSprites);
        Sprite[] attack = HasSprites(attackSprites)
            ? attackSprites
            : (HasSprites(previewClip) ? previewClip : fallback.attackSprites);

        return new SkinSpriteSet(
            preview != null ? preview : fallback.PreviewOrFirstSprite,
            idle,
            walk,
            attack);
    }

    public static bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0 && sprites[0] != null;
    }
}

public class SkinVisualDatabase : MonoBehaviour
{
    public static SkinVisualDatabase Instance { get; private set; }

    private const int DefaultFrameWidth = 32;
    private const int DefaultFrameHeight = 32;
    private const string DefaultIdleSheet = "Sprites/PlayerIdle";
    private const string DefaultWalkSheet = "Sprites/PlayerWalk";
    private const string DefaultAttackSheet = "Sprites/PlayerAttack";

    private static SkinVisualEntry[] _cachedSkins;
    private static SkinSpriteSet _defaultSpriteSet;
    private static bool _defaultSpriteSetLoaded;

    public SkinVisualEntry[] skins;

    private void OnValidate()
    {
        if (skins == null)
            return;

        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null)
                skins[i].EnsureDefaults();
        }
    }

    private void Awake()
    {
        Register(this);
    }

    public static void Register(SkinVisualDatabase database)
    {
        if (database == null)
            return;

        Instance = database;
        _cachedSkins = database.skins;
    }

    public bool TryGetSpriteSet(int skinId, out SkinSpriteSet spriteSet)
    {
        return TryGetSpriteSet(skinId, skins, out spriteSet);
    }

    public bool TryGetSprite(int skinId, out Sprite sprite)
    {
        if (TryGetSpriteSet(skinId, out SkinSpriteSet spriteSet))
        {
            sprite = spriteSet.PreviewOrFirstSprite;
            return sprite != null;
        }

        sprite = null;
        return false;
    }

    public static bool TryGetSpriteSetGlobal(int skinId, out SkinSpriteSet spriteSet)
    {
        if (Instance != null && Instance.TryGetSpriteSet(skinId, out spriteSet))
            return true;

        return TryGetSpriteSet(skinId, _cachedSkins, out spriteSet);
    }

    public static bool TryGetSpriteGlobal(int skinId, out Sprite sprite)
    {
        if (TryGetSpriteSetGlobal(skinId, out SkinSpriteSet spriteSet))
        {
            sprite = spriteSet.PreviewOrFirstSprite;
            return sprite != null;
        }

        sprite = null;
        return false;
    }

    public static SkinSpriteSet GetSpriteSetOrDefault(int skinId)
    {
        SkinSpriteSet fallback = GetDefaultSpriteSet();
        if (TryGetSpriteSetGlobal(skinId, out SkinSpriteSet spriteSet))
            return spriteSet.WithFallback(fallback);

        return fallback;
    }

    public static SkinSpriteSet GetDefaultSpriteSet()
    {
        if (_defaultSpriteSetLoaded)
            return _defaultSpriteSet;

        _defaultSpriteSetLoaded = true;
        Sprite[] idleSprites = LoadSheet(DefaultIdleSheet, DefaultFrameWidth, DefaultFrameHeight);
        Sprite[] walkSprites = LoadSheet(DefaultWalkSheet, DefaultFrameWidth, DefaultFrameHeight);
        Sprite[] attackSprites = LoadSheet(DefaultAttackSheet, DefaultFrameWidth, DefaultFrameHeight);
        Sprite preview = SkinSpriteSet.HasSprites(idleSprites)
            ? idleSprites[0]
            : (SkinSpriteSet.HasSprites(walkSprites)
                ? walkSprites[0]
                : (SkinSpriteSet.HasSprites(attackSprites) ? attackSprites[0] : null));

        _defaultSpriteSet = new SkinSpriteSet(preview, idleSprites, walkSprites, attackSprites);
        return _defaultSpriteSet;
    }

    private static bool TryGetSpriteSet(int skinId, SkinVisualEntry[] entries, out SkinSpriteSet spriteSet)
    {
        spriteSet = new SkinSpriteSet();
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            SkinVisualEntry entry = entries[i];
            if (entry != null && entry.skinId == skinId)
            {
                spriteSet = entry.ToSpriteSet();
                return spriteSet.HasAnySprite;
            }
        }

        return false;
    }

    public static Sprite[] CreateSpritesFromSheet(Texture2D texture, int frameWidth, int frameHeight)
    {
        if (texture == null)
            return null;

        int columns = Mathf.Max(1, texture.width / Mathf.Max(1, frameWidth));
        int rows = Mathf.Max(1, texture.height / Mathf.Max(1, frameHeight));
        int frameCount = Mathf.Max(1, columns * rows);
        Sprite[] sprites = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            int column = i % columns;
            int rowFromTop = i / columns;
            int rowFromBottom = rows - rowFromTop - 1;
            Rect rect = new Rect(
                column * frameWidth,
                rowFromBottom * frameHeight,
                frameWidth,
                frameHeight);
            sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), frameWidth);
        }

        return sprites;
    }

    private static Sprite[] LoadSheet(string resourceName, int frameWidth, int frameHeight)
    {
        return CreateSpritesFromSheet(Resources.Load<Texture2D>(resourceName), frameWidth, frameHeight);
    }
}
