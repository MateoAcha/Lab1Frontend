using UnityEngine;

// Plays the walk/idle (5-frame) or attack (1-frame) animation for giant enemies.
// spriteScale adjusts visual size independently of the parent's hitbox.
public class GiantEnemyAnimator : MonoBehaviour
{
    public float spriteScale       = 1f;

    private const int   WalkFrameCount = 5;
    private const int   FrameW         = 64;
    private const int   FrameH         = 64;
    private const float WalkFps        = 5f;
    private const float FacingDeadZone = 0.001f;

    private Sprite[]            _walkFrames;
    private Sprite              _attackFrame;
    private SpriteRenderer      _sr;
    private GiantEnemyController _controller;
    private int                 _walkFrame;
    private float               _nextAt;
    private bool                _facingLeft = true;
    private Vector3             _lastRootPos;
    private bool                _hasLastRootPos;

    private void Start()
    {
        _sr         = GetComponent<SpriteRenderer>();
        _controller = GetComponentInParent<GiantEnemyController>();

        _walkFrames  = LoadSheet("Sprites/GiantWalkIdle", WalkFrameCount);
        Sprite[] atk = LoadSheet("Sprites/GiantShooting", 1);
        _attackFrame = atk != null ? atk[0] : null;

        CacheRootPos();
        ApplyScale();

        if (_sr != null && _walkFrames != null)
            _sr.sprite = _walkFrames[0];
    }

    private void Update()
    {
        ApplyScale();
        if (_sr == null) return;

        UpdateFacing();

        bool attacking = _controller != null && _controller.IsAttacking;

        if (attacking)
        {
            if (_attackFrame != null) _sr.sprite = _attackFrame;
            _nextAt = Time.time + 1f / WalkFps; // pause walk cycle during attack
            return;
        }

        if (_walkFrames == null || Time.time < _nextAt) return;
        _walkFrame = (_walkFrame + 1) % _walkFrames.Length;
        _sr.sprite = _walkFrames[_walkFrame];
        _nextAt    = Time.time + 1f / WalkFps;
    }

    private void UpdateFacing()
    {
        float x = 0f;

        if (_controller != null)
        {
            x = _controller.FacingDirection.x;
        }
        else
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            Vector3 current = root.position;
            if (_hasLastRootPos)
                x = current.x - _lastRootPos.x;
            _lastRootPos = current;
            _hasLastRootPos = true;
        }

        if (x < -FacingDeadZone)       _facingLeft = true;
        else if (x > FacingDeadZone)   _facingLeft = false;

        _sr.flipX = !_facingLeft;
    }

    private void CacheRootPos()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        _lastRootPos    = root.position;
        _hasLastRootPos = true;
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
            Debug.LogWarning("GiantEnemyAnimator: could not load Resources/" + name);
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
