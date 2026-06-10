using UnityEngine;

public class RemotePlayerGhost : MonoBehaviour
{
    public static RemotePlayerGhost Instance { get; private set; }

    private SpriteRenderer _sr;
    private Health _health;
    private int _appliedSkinId = -1;
    private string _appliedSkinColor = "";
    private int _appliedAttackSequence;

    public int CurrentSkinId => OnlinePlayerSync.Instance != null ? OnlinePlayerSync.Instance.RemoteSkinId : 0;
    public Vector3 CurrentVelocity => OnlinePlayerSync.Instance != null ? OnlinePlayerSync.Instance.RemotePlayerVelocity : Vector3.zero;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _health = GetComponent<Health>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool visible = OnlinePlayerSync.Instance != null && OnlinePlayerSync.Instance.HasRemotePlayer;

        if (_sr != null) _sr.enabled = visible;
        SetHealthBarVisible(visible);
        if (_health == null) _health = GetComponent<Health>();
        if (!visible) return;

        ApplyRemoteSkinIfChanged();
        ApplyRemoteAttackIfChanged();
        ApplyRemoteHealth();

        Vector3 target = OnlinePlayerSync.Instance.RemotePlayerPosition
            + OnlinePlayerSync.Instance.RemotePlayerVelocity * 0.08f;

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            Time.deltaTime * 12f);
    }

    private void ApplyRemoteSkinIfChanged()
    {
        if (_sr == null || OnlinePlayerSync.Instance == null) return;

        int skinId = OnlinePlayerSync.Instance.RemoteSkinId;
        string skinColor = OnlinePlayerSync.Instance.RemoteSkinColor;
        if (_appliedSkinId == skinId && string.Equals(_appliedSkinColor, skinColor))
            return;

        PlayerSkinVisuals.Apply(_sr, skinId, skinColor, _sr.sharedMaterial);
        PlayerAnimator animator = _sr.GetComponent<PlayerAnimator>();
        if (animator != null)
            animator.RefreshSkin();
        _appliedSkinId = skinId;
        _appliedSkinColor = skinColor;
    }

    private void ApplyRemoteAttackIfChanged()
    {
        if (_sr == null || OnlinePlayerSync.Instance == null)
            return;

        int attackSequence = OnlinePlayerSync.Instance.RemoteAttackSequence;
        if (attackSequence <= 0 || attackSequence == _appliedAttackSequence)
            return;

        PlayerAnimator animator = _sr.GetComponent<PlayerAnimator>();
        if (animator != null)
            animator.TriggerAttack(0.14f);
        _appliedAttackSequence = attackSequence;
    }

    private void ApplyRemoteHealth()
    {
        if (_health == null || OnlinePlayerSync.Instance == null) return;

        _health.maxHp = Mathf.Max(0.01f, OnlinePlayerSync.Instance.RemoteMaxHp);
        _health.hp = Mathf.Clamp(OnlinePlayerSync.Instance.RemoteHp, 0f, _health.maxHp);
    }

    private void SetHealthBarVisible(bool visible)
    {
        SetChildRendererVisible("HpBack", visible);
        SetChildRendererVisible("HpFill", visible);
    }

    private void SetChildRendererVisible(string childName, bool visible)
    {
        Transform child = transform.Find(childName);
        if (child == null) return;

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = visible;
    }
}
