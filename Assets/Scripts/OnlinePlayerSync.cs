using UnityEngine;

public class OnlinePlayerSync : MonoBehaviour
{
    public static OnlinePlayerSync Instance { get; private set; }
    public Vector3 RemotePlayerPosition { get; private set; }
    public Vector3 RemotePlayerVelocity { get; private set; }
    public string RemoteUsername { get; private set; } = "";
    public int RemoteSkinId { get; private set; }
    public string RemoteSkinColor { get; private set; } = "#FFFFFF";
    public int RemoteAttackSequence { get; private set; }
    public int RemoteQuickChatSequence { get; private set; }
    public string RemoteQuickChatEmote { get; private set; } = "";
    public int RemoteWeaponItemId { get; private set; }
    public string RemoteWeaponType { get; private set; } = "Spear";
    public string RemoteWeaponColor { get; private set; } = "#FFFFFF";
    public float RemoteHp { get; private set; } = 10f;
    public float RemoteMaxHp { get; private set; } = 10f;
    public bool RemoteDowned { get; private set; }
    public float RemoteReviveProgress { get; private set; }
    public bool HasRemotePlayer { get; private set; }

    public void SetRemotePosition(Vector3 pos)
    {
        SetRemoteState(pos, Vector3.zero);
    }

    public void SetRemoteState(Vector3 pos, Vector3 velocity)
    {
        SetRemoteState(pos, velocity, RemoteSkinId, RemoteSkinColor);
    }

    public void SetRemoteState(Vector3 pos, Vector3 velocity, int skinId, string skinColor)
    {
        SetRemoteState(
            pos,
            velocity,
            skinId,
            skinColor,
            RemoteHp,
            RemoteMaxHp,
            RemoteAttackSequence,
            RemoteQuickChatSequence,
            RemoteQuickChatEmote,
            RemoteWeaponItemId,
            RemoteWeaponType,
            RemoteWeaponColor,
            RemoteDowned,
            RemoteReviveProgress,
            RemoteUsername);
    }

    public void SetRemoteState(
        Vector3 pos,
        Vector3 velocity,
        int skinId,
        string skinColor,
        float hp,
        float maxHp,
        int attackSequence = 0,
        int quickChatSequence = 0,
        string quickChatEmote = "",
        int weaponItemId = 0,
        string weaponType = "Spear",
        string weaponColor = "#FFFFFF",
        bool downed = false,
        float reviveProgress = 0f,
        string username = "")
    {
        RemotePlayerPosition = pos;
        RemotePlayerVelocity = downed ? Vector3.zero : velocity;
        RemoteUsername = username?.Trim() ?? "";
        RemoteSkinId = Mathf.Max(0, skinId);
        RemoteSkinColor = string.IsNullOrWhiteSpace(skinColor) ? "#FFFFFF" : skinColor;
        RemoteAttackSequence = Mathf.Max(0, attackSequence);
        RemoteQuickChatSequence = Mathf.Max(0, quickChatSequence);
        RemoteQuickChatEmote = string.IsNullOrWhiteSpace(quickChatEmote) ? "" : QuickChatEmotes.NormalizeId(quickChatEmote);
        RemoteWeaponItemId = Mathf.Max(0, weaponItemId);
        RemoteWeaponType = string.IsNullOrWhiteSpace(weaponType) ? "Spear" : weaponType;
        RemoteWeaponColor = string.IsNullOrWhiteSpace(weaponColor) ? "#FFFFFF" : weaponColor;
        RemoteMaxHp = Mathf.Max(0.01f, maxHp);
        RemoteHp = Mathf.Clamp(hp, 0f, RemoteMaxHp);
        RemoteDowned = downed;
        RemoteReviveProgress = Mathf.Clamp01(reviveProgress);
        HasRemotePlayer = true;
    }

    public void ClearRemotePlayer()
    {
        HasRemotePlayer = false;
        RemoteDowned = false;
        RemoteReviveProgress = 0f;
        RemotePlayerVelocity = Vector3.zero;
        RemoteUsername = "";
        RemoteQuickChatSequence = 0;
        RemoteQuickChatEmote = "";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
