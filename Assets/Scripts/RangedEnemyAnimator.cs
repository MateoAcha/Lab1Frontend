using UnityEngine;

// Plays the moving (4-frame) or shooting (1-frame) Piskel animation for ranged enemies.
// spriteScale adjusts visual size independently of the parent's hitbox.
public class RangedEnemyAnimator : MonoBehaviour
{
    public float spriteScale      = 1f;
    public float shootPoseDuration = 0.35f; // seconds the shoot pose is held after firing

    private const int   MoveFrameCount = 4;
    private const int   FrameW         = 32;
    private const int   FrameH         = 32;
    private const float MoveFps        = 3f;

    private Sprite[]              _moveFrames;
    private Sprite                _shootFrame;
    private SpriteRenderer        _sr;
    private RangedEnemyController _controller;
    private int                   _moveFrame;
    private float                 _nextAt;

    private void Start()
    {
        _sr         = GetComponent<SpriteRenderer>();
        _controller = GetComponentInParent<RangedEnemyController>();

        _moveFrames = LoadSheet("Sprites/RangedEnemyMoving", MoveFrameCount);
        Sprite[] shoot = LoadSheet("Sprites/RangedEnemyShooting", 1);
        _shootFrame = shoot != null ? shoot[0] : null;

        ApplyScale();
        if (_sr != null && _moveFrames != null)
            _sr.sprite = _moveFrames[0];
    }

    private void Update()
    {
        ApplyScale();
        if (_sr == null) return;

        bool shooting = _controller != null
            && Time.time - _controller.LastShotTime < shootPoseDuration;

        if (shooting)
        {
            if (_shootFrame != null) _sr.sprite = _shootFrame;
            _nextAt = Time.time + shootPoseDuration; // don't advance move anim during shoot pose
            return;
        }

        if (_moveFrames == null || Time.time < _nextAt) return;
        _moveFrame = (_moveFrame + 1) % _moveFrames.Length;
        _sr.sprite = _moveFrames[_moveFrame];
        _nextAt    = Time.time + 1f / MoveFps;
    }

    private void ApplyScale()
    {
        float s = Mathf.Max(0.01f, spriteScale);
        if (!Mathf.Approximately(transform.localScale.x, s))
            transform.localScale = new Vector3(s, s, 1f);
    }

    private static Sprite[] LoadSheet(string name, int frameCount)
    {
        Texture2D tex = Resources.Load<Texture2D>(name);
        if (tex == null)
        {
            Debug.LogWarning("RangedEnemyAnimator: could not load Resources/" + name);
            return null;
        }
        var frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            Rect rect = new Rect(i * FrameW, 0, FrameW, FrameH);
            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), FrameW);
        }
        return frames;
    }
}
