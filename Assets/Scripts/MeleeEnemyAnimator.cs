using UnityEngine;

// Plays the 4-frame Piskel sprite sheet for melee enemies.
// spriteScale adjusts visual size independently of the parent's hitbox.
public class MeleeEnemyAnimator : MonoBehaviour
{
    public float spriteScale = 1.25f;

    private const string TextureName = "Sprites/MeleeEnemyAnim";
    private const int    FrameCount  = 4;
    private const int    FrameW      = 32;
    private const int    FrameH      = 32;
    private const float  Fps         = 4f;

    private Sprite[]       _frames;
    private SpriteRenderer _sr;
    private int            _frame;
    private float          _nextAt;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;

        Texture2D tex = Resources.Load<Texture2D>(TextureName);
        if (tex == null)
        {
            Debug.LogWarning("MeleeEnemyAnimator: could not load Resources/" + TextureName);
            return;
        }

        _frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            Rect rect = new Rect(i * FrameW, 0, FrameW, FrameH);
            _frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), FrameW);
        }

        _sr.color  = Color.white;
        _sr.sprite = _frames[0];
        _nextAt    = Time.time + 1f / Fps;
        ApplyScale();
    }

    private void Update()
    {
        ApplyScale();
        if (_frames == null || Time.time < _nextAt) return;
        _frame     = (_frame + 1) % FrameCount;
        _sr.sprite = _frames[_frame];
        _nextAt    = Time.time + 1f / Fps;
    }

    private void ApplyScale()
    {
        float s = Mathf.Max(0.01f, spriteScale);
        if (!Mathf.Approximately(transform.localScale.x, s))
            transform.localScale = new Vector3(s, s, 1f);
    }
}
