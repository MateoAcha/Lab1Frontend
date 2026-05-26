using UnityEngine;

public class OnlinePlayerSync : MonoBehaviour
{
    public static OnlinePlayerSync Instance { get; private set; }
    public Vector3 RemotePlayerPosition { get; private set; }
    public Vector3 RemotePlayerVelocity { get; private set; }
    public int RemoteSkinId { get; private set; }
    public string RemoteSkinColor { get; private set; } = "#4DBFFF";
    public float RemoteHp { get; private set; } = 10f;
    public float RemoteMaxHp { get; private set; } = 10f;
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
        SetRemoteState(pos, velocity, skinId, skinColor, RemoteHp, RemoteMaxHp);
    }

    public void SetRemoteState(Vector3 pos, Vector3 velocity, int skinId, string skinColor, float hp, float maxHp)
    {
        RemotePlayerPosition = pos;
        RemotePlayerVelocity = velocity;
        RemoteSkinId = Mathf.Max(0, skinId);
        RemoteSkinColor = string.IsNullOrWhiteSpace(skinColor) ? "#4DBFFF" : skinColor;
        RemoteMaxHp = Mathf.Max(0.01f, maxHp);
        RemoteHp = Mathf.Clamp(hp, 0f, RemoteMaxHp);
        HasRemotePlayer = true;
    }

    public void ClearRemotePlayer()
    {
        HasRemotePlayer = false;
        RemotePlayerVelocity = Vector3.zero;
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
