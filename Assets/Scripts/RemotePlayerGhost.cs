using UnityEngine;

public class RemotePlayerGhost : MonoBehaviour
{
    public static RemotePlayerGhost Instance { get; private set; }

    private SpriteRenderer _sr;
    private SpriteRenderer _weaponRenderer;
    private Transform _weaponTransform;
    private Health _health;
    private PlayerReviveState _reviveState;
    private int _appliedSkinId = -1;
    private string _appliedSkinColor = "";
    private int _appliedAttackSequence;
    private bool _weaponFacingLeft;
    private float _hideWeaponUntil;
    private GameBootstrap _bootstrap;

    public int CurrentSkinId => OnlinePlayerSync.Instance != null ? OnlinePlayerSync.Instance.RemoteSkinId : 0;
    public Vector3 CurrentVelocity => OnlinePlayerSync.Instance != null ? OnlinePlayerSync.Instance.RemotePlayerVelocity : Vector3.zero;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _health = GetComponent<Health>();
        _reviveState = GetComponent<PlayerReviveState>();
        if (_reviveState == null)
            _reviveState = gameObject.AddComponent<PlayerReviveState>();
        _bootstrap = FindObjectOfType<GameBootstrap>();
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
        if (!visible)
        {
            SetWeaponVisible(false);
            if (_reviveState != null)
                _reviveState.ApplySyncedState(false, 0f);
            return;
        }

        ApplyRemoteSkinIfChanged();
        ApplyRemoteHealth();
        ApplyRemoteDownedState();

        Vector3 target = OnlinePlayerSync.Instance.RemotePlayerPosition
            + OnlinePlayerSync.Instance.RemotePlayerVelocity * 0.08f;

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            Time.deltaTime * 12f);

        if (OnlinePlayerSync.Instance.RemoteDowned)
        {
            SetWeaponVisible(false);
            return;
        }

        ApplyRemoteAttackIfChanged();
        UpdateRemoteWeaponVisual();
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
        _hideWeaponUntil = Time.time + 0.18f;
        _appliedAttackSequence = attackSequence;
    }

    private void UpdateRemoteWeaponVisual()
    {
        if (_sr == null || OnlinePlayerSync.Instance == null || Time.time < _hideWeaponUntil)
        {
            SetWeaponVisible(false);
            return;
        }

        WeaponKind weaponKind = PlayerLoadout.ParseWeaponKind(OnlinePlayerSync.Instance.RemoteWeaponType);
        GameBootstrap bootstrap = ResolveBootstrap();
        if (weaponKind == WeaponKind.Ranged)
        {
            UpdateRemoteRangedOrbVisual(bootstrap);
            return;
        }

        if (!OnlineWeaponVisuals.TryResolveCarriedVisual(
                weaponKind,
                OnlinePlayerSync.Instance.RemoteWeaponItemId,
                bootstrap,
                out OnlineCarriedWeaponVisual visual))
        {
            SetWeaponVisible(false);
            return;
        }

        EnsureWeaponVisual();
        if (_weaponRenderer == null || _weaponTransform == null)
            return;

        Vector3 velocity = CurrentVelocity;
        if (velocity.x < -0.001f)
            _weaponFacingLeft = true;
        else if (velocity.x > 0.001f)
            _weaponFacingLeft = false;

        float facingSign = _weaponFacingLeft ? -1f : 1f;
        _weaponRenderer.sprite = visual.sprite;
        _weaponRenderer.color = Color.white;
        _weaponRenderer.enabled = true;
        _weaponRenderer.flipX = _weaponFacingLeft;
        _weaponRenderer.sortingOrder = _sr.sortingOrder + visual.sortingOrderOffset;
        _weaponTransform.localPosition = new Vector3(visual.offset.x * facingSign, visual.offset.y, 0f);
        _weaponTransform.localRotation = Quaternion.Euler(0f, 0f, visual.rotationOffset * facingSign);
        _weaponTransform.localScale = new Vector3(
            Mathf.Approximately(visual.scale.x, 0f) ? 1f : visual.scale.x,
            Mathf.Approximately(visual.scale.y, 0f) ? 1f : visual.scale.y,
            1f);
    }

    private void UpdateRemoteRangedOrbVisual(GameBootstrap bootstrap)
    {
        EnsureWeaponVisual();
        if (_weaponRenderer == null || _weaponTransform == null)
            return;

        Vector3 velocity = CurrentVelocity;
        if (velocity.x < -0.001f)
            _weaponFacingLeft = true;
        else if (velocity.x > 0.001f)
            _weaponFacingLeft = false;

        Vector2 offset = bootstrap != null ? bootstrap.carriedRangedOrbOffset : new Vector2(0.22f, 0.08f);
        Vector2 scale = bootstrap != null ? bootstrap.carriedRangedOrbScale : Vector2.one;
        float size = ResolveRangedOrbSize();
        int sortingOffset = bootstrap != null ? bootstrap.carriedRangedOrbSortingOrderOffset : 1;
        float facingSign = _weaponFacingLeft ? -1f : 1f;

        Color color = PlayerLoadout.ParseWeaponColor(OnlinePlayerSync.Instance.RemoteWeaponColor, Color.white);
        color.a = 0.95f;

        _weaponRenderer.sprite = SimpleSprite.Circle;
        _weaponRenderer.color = color;
        _weaponRenderer.enabled = true;
        _weaponRenderer.flipX = false;
        _weaponRenderer.sortingOrder = _sr.sortingOrder + sortingOffset;
        _weaponTransform.localPosition = new Vector3(offset.x * facingSign, offset.y, 0f);
        _weaponTransform.localRotation = Quaternion.identity;
        _weaponTransform.localScale = new Vector3(
            size * (Mathf.Approximately(scale.x, 0f) ? 1f : scale.x),
            size * (Mathf.Approximately(scale.y, 0f) ? 1f : scale.y),
            1f);
    }

    private GameBootstrap ResolveBootstrap()
    {
        if (_bootstrap == null)
            _bootstrap = FindObjectOfType<GameBootstrap>();
        return _bootstrap;
    }

    private float ResolveRangedOrbSize()
    {
        PlayerController localPlayer = PlayerController.main != null
            ? PlayerController.main
            : FindObjectOfType<PlayerController>();
        return localPlayer != null
            ? Mathf.Max(0.01f, localPlayer.rangedProjectileSize)
            : 0.35f;
    }

    private void EnsureWeaponVisual()
    {
        if (_weaponRenderer != null && _weaponTransform != null)
            return;

        Transform existing = transform.Find("RemoteCarriedWeaponVisual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("RemoteCarriedWeaponVisual");
        visual.transform.SetParent(transform, false);
        _weaponTransform = visual.transform;
        _weaponRenderer = visual.GetComponent<SpriteRenderer>();
        if (_weaponRenderer == null)
            _weaponRenderer = visual.AddComponent<SpriteRenderer>();
        _weaponRenderer.color = Color.white;
    }

    private void SetWeaponVisible(bool visible)
    {
        if (_weaponRenderer != null)
            _weaponRenderer.enabled = visible;
    }

    private void ApplyRemoteHealth()
    {
        if (_health == null || OnlinePlayerSync.Instance == null) return;

        _health.SetHealthSilently(
            OnlinePlayerSync.Instance.RemoteHp,
            Mathf.Max(0.01f, OnlinePlayerSync.Instance.RemoteMaxHp));
    }

    private void ApplyRemoteDownedState()
    {
        if (_reviveState == null || OnlinePlayerSync.Instance == null) return;

        _reviveState.ApplySyncedState(
            OnlinePlayerSync.Instance.RemoteDowned,
            OnlinePlayerSync.Instance.RemoteReviveProgress);
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
