using UnityEngine;

// Plays the 2-frame move sprite sheet for player minions.
// spriteScale adjusts visual size independently of the parent's hitbox.
public class PlayerMinionAnimator : MonoBehaviour
{
    public Sprite[] moveSprites;
    public Texture2D moveSheet;
    public string resourceSheetName = "Sprites/Minion";
    public int frameWidth = 32;
    public int frameHeight = 32;
    public int frameCount = 2;
    public float fps = 4f;
    public float spriteScale = 1f;

    private Sprite[] _frames;
    private SpriteRenderer _sr;
    private int _frame;
    private float _nextAt;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;

        _frames = LoadFrames();
        if (_frames == null || _frames.Length <= 0)
        {
            Debug.LogWarning("PlayerMinionAnimator: no minion move frames found.");
            _sr.sprite = SimpleSprite.Circle;
            _sr.color = new Color(1f, 0.88f, 0.12f, 1f);
            ApplyScale();
            return;
        }

        _sr.color = Color.white;
        _sr.sprite = _frames[0];
        _nextAt = Time.time + 1f / Mathf.Max(0.01f, fps);
        ApplyScale();
    }

    private void Update()
    {
        ApplyScale();
        if (_frames == null || _frames.Length <= 1 || Time.time < _nextAt)
            return;

        _frame = (_frame + 1) % _frames.Length;
        _sr.sprite = _frames[_frame];
        _nextAt = Time.time + 1f / Mathf.Max(0.01f, fps);
    }

    private Sprite[] LoadFrames()
    {
        Sprite[] assigned = LoadAssignedSprites();
        if (assigned != null)
            return assigned;

        Texture2D texture = moveSheet;
        if (texture == null && !string.IsNullOrWhiteSpace(resourceSheetName))
            texture = Resources.Load<Texture2D>(resourceSheetName);
        if (texture == null)
            texture = Resources.Load<Texture2D>("Sprites/Minion");

        if (texture == null)
            return null;

        return SliceTexture(texture);
    }

    private Sprite[] LoadAssignedSprites()
    {
        if (moveSprites == null || moveSprites.Length <= 0)
            return null;

        int count = 0;
        for (int i = 0; i < moveSprites.Length; i++)
        {
            if (moveSprites[i] != null)
                count++;
        }

        if (count <= 1)
            return null;

        Sprite[] assigned = new Sprite[count];
        int index = 0;
        for (int i = 0; i < moveSprites.Length; i++)
        {
            if (moveSprites[i] != null)
                assigned[index++] = moveSprites[i];
        }
        return assigned;
    }

    private Sprite[] SliceTexture(Texture2D texture)
    {
        int width = Mathf.Clamp(frameWidth, 1, Mathf.Max(1, texture.width));
        int height = Mathf.Clamp(frameHeight, 1, Mathf.Max(1, texture.height));
        int count = Mathf.Max(1, frameCount);

        bool vertical = texture.height >= height * count && texture.width < width * count;
        int availableFrames = vertical
            ? Mathf.Max(1, texture.height / height)
            : Mathf.Max(1, texture.width / width);
        count = Mathf.Min(count, availableFrames);

        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            Rect rect = vertical
                ? new Rect(0f, texture.height - height * (i + 1), width, height)
                : new Rect(width * i, 0f, width, height);

            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), width);
        }

        return frames;
    }

    private void ApplyScale()
    {
        float s = Mathf.Max(0.01f, spriteScale);
        if (!Mathf.Approximately(transform.localScale.x, s))
            transform.localScale = new Vector3(s, s, 1f);
    }
}
