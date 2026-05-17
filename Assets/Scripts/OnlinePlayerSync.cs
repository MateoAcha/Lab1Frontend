using UnityEngine;

public class OnlinePlayerSync : MonoBehaviour
{
    public static OnlinePlayerSync Instance { get; private set; }
    public Vector3 RemotePlayerPosition { get; private set; }
    public Vector3 RemotePlayerVelocity { get; private set; }
    public bool HasRemotePlayer { get; private set; }

    public void SetRemotePosition(Vector3 pos)
    {
        SetRemoteState(pos, Vector3.zero);
    }

    public void SetRemoteState(Vector3 pos, Vector3 velocity)
    {
        RemotePlayerPosition = pos;
        RemotePlayerVelocity = velocity;
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
