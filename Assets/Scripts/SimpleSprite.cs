using UnityEngine;

public static class SimpleSprite
{
    private static Sprite square;
    private static Sprite circle;

    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();

                square = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            return square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                const int size = 64;
                Texture2D tex = new Texture2D(size, size);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                float center = (size - 1) * 0.5f;
                float radius = size * 0.5f - 1f;
                float radiusSq = radius * radius;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distSq = dx * dx + dy * dy;
                        tex.SetPixel(x, y, distSq <= radiusSq ? Color.white : Color.clear);
                    }
                }

                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return circle;
        }
    }
}
