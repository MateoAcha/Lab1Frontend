using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectileAnimator : MonoBehaviour
{
    public Sprite[] frames;
    [Min(0.01f)] public float fps = 10f;

    private static readonly Dictionary<string, Sprite[]> SheetCache = new Dictionary<string, Sprite[]>();

    private SpriteRenderer sr;
    private int frame;
    private float nextFrameAt;

    public static Sprite[] ResolveFrames(Sprite[] assignedFrames, Texture2D texture, int frameCount)
    {
        if (HasSprites(assignedFrames))
        {
            return assignedFrames;
        }

        if (texture == null)
        {
            return null;
        }

        int count = Mathf.Max(1, frameCount);
        string key = texture.GetInstanceID() + ":" + count;
        if (SheetCache.TryGetValue(key, out Sprite[] cached) && HasSprites(cached))
        {
            return cached;
        }

        int frameWidth = Mathf.Max(1, texture.width / count);
        int frameHeight = Mathf.Max(1, texture.height);
        Sprite[] generated = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            generated[i] = Sprite.Create(
                texture,
                new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(frameWidth, frameHeight));
        }

        SheetCache[key] = generated;
        return generated;
    }

    private static bool HasSprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        frame = 0;
        nextFrameAt = Time.time;
    }

    private void Update()
    {
        if (sr == null || !HasSprites(frames) || Time.time < nextFrameAt)
        {
            return;
        }

        sr.sprite = frames[frame];
        frame = (frame + 1) % frames.Length;
        nextFrameAt = Time.time + 1f / Mathf.Max(0.01f, fps);
    }
}
