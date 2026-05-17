using UnityEngine;

public class OnlinePlayerSync : MonoBehaviour
{
    public static OnlinePlayerSync Instance { get; private set; }
    public Vector3 RemotePlayerPosition { get; private set; }
    public bool HasRemotePlayer { get; private set; }

    public void SetRemotePosition(Vector3 pos)
    {
        RemotePlayerPosition = pos;
        HasRemotePlayer = true;
    }

    public void ClearRemotePlayer()
    {
        HasRemotePlayer = false;
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
