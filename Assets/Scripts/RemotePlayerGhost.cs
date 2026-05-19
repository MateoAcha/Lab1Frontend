using UnityEngine;

public class RemotePlayerGhost : MonoBehaviour
{
    public static RemotePlayerGhost Instance { get; private set; }

    private SpriteRenderer _sr;
    private int _appliedSkinId = -1;
    private string _appliedSkinColor = "";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool visible = OnlinePlayerSync.Instance != null && OnlinePlayerSync.Instance.HasRemotePlayer;

        if (_sr != null) _sr.enabled = visible;
        if (!visible) return;

        ApplyRemoteSkinIfChanged();

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

        PlayerSkinVisuals.Apply(_sr, skinId, skinColor, _sr.sharedMaterial, 0.75f);
        _appliedSkinId = skinId;
        _appliedSkinColor = skinColor;
    }
}
