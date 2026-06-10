using UnityEngine;

// Plays walk / idle sprite-sheet animations for the player.
// Attach to the player's SpriteRenderer object.
// spriteScale controls visual size independently of the parent's hitbox.
public class PlayerAnimator : MonoBehaviour
{
    // Visual size as a LOCAL scale multiplier.
    // 1 = sprite fills the same area as the parent's hitbox.
    // 1.5 = sprite is 50% larger than the hitbox, hitbox unchanged.
    public float spriteScale = 1.25f;

    private const float WalkFps  = 6f;
    private const float IdleFps  = 2f;
    private const float AttackFps = 12f;
    private const float FacingDeadZone = 0.001f;

    private Sprite[]         _walkFrames;
    private Sprite[]         _idleFrames;
    private Sprite[]         _attackFrames;
    private SpriteRenderer   _sr;
    private PlayerController _pc;
    private RemotePlayerGhost _remoteGhost;
    private bool             _walking;
    private int              _frame;
    private int              _attackFrame;
    private float            _nextAt;
    private float            _attackNextAt;
    private float            _attackUntil;
    private int              _appliedSkinId = int.MinValue;
    private bool             _facingLeft;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _pc = GetComponentInParent<PlayerController>();
        _remoteGhost = GetComponentInParent<RemotePlayerGhost>();

        RefreshSkinFrames(true);

        ApplyScale();

        if (SkinSpriteSet.HasSprites(_idleFrames) && _sr != null)
            _sr.sprite = _idleFrames[0];
    }

    private void Update()
    {
        ApplyScale();
        RefreshSkinFrames(false);
        UpdateFacing();

        if (ApplyAttackFrame())
            return;

        bool moving = IsMoving();

        if (moving != _walking)
        {
            _walking = moving;
            _frame   = 0;
            _nextAt  = Time.time; // switch immediately
        }

        if (Time.time < _nextAt) return;

        Sprite[] clip = _walking ? _walkFrames : _idleFrames;
        if (!SkinSpriteSet.HasSprites(clip) || _sr == null) return;

        _frame     = (_frame + 1) % clip.Length;
        _sr.sprite = clip[_frame];
        _sr.color  = PreserveAlpha(Color.white, _sr.color.a);
        _nextAt    = Time.time + 1f / (_walking ? WalkFps : IdleFps);
    }

    public void RefreshSkin()
    {
        RefreshSkinFrames(true);
    }

    public void TriggerAttack(float duration)
    {
        RefreshSkinFrames(false);
        if (!SkinSpriteSet.HasSprites(_attackFrames))
            return;

        _attackFrame = 0;
        _attackNextAt = Time.time;
        _attackUntil = Time.time + Mathf.Max(0.05f, duration);
    }

    private void ApplyScale()
    {
        float s = Mathf.Max(0.01f, spriteScale);
        if (!Mathf.Approximately(transform.localScale.x, s))
            transform.localScale = new Vector3(s, s, 1f);
    }

    private void RefreshSkinFrames(bool force)
    {
        if (_sr == null)
            return;

        int skinId = GetSkinId();
        if (!force && _appliedSkinId == skinId)
            return;

        SkinSpriteSet spriteSet = SkinVisualDatabase.GetSpriteSetOrDefault(skinId);
        _idleFrames = SkinSpriteSet.HasSprites(spriteSet.idleSprites)
            ? spriteSet.idleSprites
            : new[] { spriteSet.PreviewOrFirstSprite };
        _walkFrames = SkinSpriteSet.HasSprites(spriteSet.walkSprites)
            ? spriteSet.walkSprites
            : _idleFrames;
        _attackFrames = SkinSpriteSet.HasSprites(spriteSet.attackSprites)
            ? spriteSet.attackSprites
            : _idleFrames;
        _appliedSkinId = skinId;
        _frame = 0;
        _attackFrame = 0;
        _nextAt = Time.time;
        _attackNextAt = Time.time;

        Sprite firstSprite = SkinSpriteSet.HasSprites(_idleFrames)
            ? _idleFrames[0]
            : spriteSet.PreviewOrFirstSprite;
        if (firstSprite != null)
        {
            _sr.sprite = firstSprite;
            _sr.color = PreserveAlpha(Color.white, _sr.color.a);
        }
    }

    private int GetSkinId()
    {
        if (_pc != null)
            return _pc.NetworkSkinId;
        if (_remoteGhost != null)
            return _remoteGhost.CurrentSkinId;
        return PlayerLoadout.EquippedSkinId;
    }

    private bool IsMoving()
    {
        return GetMoveDirection().sqrMagnitude > 0.001f;
    }

    private Vector2 GetMoveDirection()
    {
        if (_pc != null)
            return _pc.LastMoveInput;
        if (_remoteGhost != null)
            return _remoteGhost.CurrentVelocity;
        return Vector2.zero;
    }

    private void UpdateFacing()
    {
        if (_sr == null)
            return;

        float x = GetMoveDirection().x;
        if (x < -FacingDeadZone)
            _facingLeft = true;
        else if (x > FacingDeadZone)
            _facingLeft = false;

        _sr.flipX = _facingLeft;
    }

    private bool ApplyAttackFrame()
    {
        if (Time.time >= _attackUntil || _sr == null || !SkinSpriteSet.HasSprites(_attackFrames))
            return false;

        if (Time.time >= _attackNextAt)
        {
            _sr.sprite = _attackFrames[_attackFrame];
            _sr.color = PreserveAlpha(Color.white, _sr.color.a);
            _attackFrame = (_attackFrame + 1) % _attackFrames.Length;
            _attackNextAt = Time.time + 1f / AttackFps;
        }

        return true;
    }

    private static Color PreserveAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
