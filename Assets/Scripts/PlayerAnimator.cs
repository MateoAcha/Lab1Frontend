using UnityEngine;

// Plays walk / idle sprite-sheet animations for the player.
// Attach to the "Sprite" child of the Player GameObject.
// spriteScale controls visual size independently of the parent's hitbox.
public class PlayerAnimator : MonoBehaviour
{
    // Visual size as a LOCAL scale multiplier.
    // 1 = sprite fills the same area as the parent's hitbox.
    // 1.5 = sprite is 50% larger than the hitbox, hitbox unchanged.
    public float spriteScale = 1.25f;

    private const int   FrameW   = 32;
    private const int   FrameH   = 32;
    private const float WalkFps  = 6f;
    private const float IdleFps  = 2f;

    private Sprite[]         _walkFrames;
    private Sprite[]         _idleFrames;
    private SpriteRenderer   _sr;
    private PlayerController _pc;
    private bool             _walking;
    private int              _frame;
    private float            _nextAt;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _pc = GetComponentInParent<PlayerController>();

        _walkFrames = LoadSheet("Sprites/PlayerWalk", 6);
        _idleFrames = LoadSheet("Sprites/PlayerIdle", 2);

        ApplyScale();

        if (_idleFrames != null && _sr != null)
            _sr.sprite = _idleFrames[0];
    }

    private void Update()
    {
        ApplyScale();

        bool moving = _pc != null && _pc.LastMoveInput.sqrMagnitude > 0.001f;

        if (moving != _walking)
        {
            _walking = moving;
            _frame   = 0;
            _nextAt  = Time.time; // switch immediately
        }

        if (Time.time < _nextAt) return;

        Sprite[] clip = _walking ? _walkFrames : _idleFrames;
        if (clip == null || _sr == null) return;

        _frame     = (_frame + 1) % clip.Length;
        _sr.sprite = clip[_frame];
        _nextAt    = Time.time + 1f / (_walking ? WalkFps : IdleFps);
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
            Debug.LogWarning("PlayerAnimator: could not load Resources/" + name);
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
