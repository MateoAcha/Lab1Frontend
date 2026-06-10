using UnityEngine;

// Plays the 3-frame shockwave animation on a giant's smash projectile.
public class GiantShockwaveAnimator : MonoBehaviour
{
    private const int   FrameCount = 3;
    private const int   FrameW     = 64;
    private const int   FrameH     = 64;
    private const float Fps        = 8f;

    private Sprite[]       _frames;
    private SpriteRenderer _sr;
    private int            _frame;
    private float          _nextAt;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;

        Texture2D tex = Resources.Load<Texture2D>("Sprites/GiantShockwave");
        if (tex == null)
        {
            Debug.LogWarning("GiantShockwaveAnimator: could not load Resources/Sprites/GiantShockwave");
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
    }

    private void Update()
    {
        if (_frames == null || Time.time < _nextAt) return;
        _frame     = (_frame + 1) % FrameCount;
        _sr.sprite = _frames[_frame];
        _nextAt    = Time.time + 1f / Fps;
    }
}
