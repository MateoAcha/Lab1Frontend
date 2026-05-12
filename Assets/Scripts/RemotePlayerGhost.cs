using UnityEngine;

public class RemotePlayerGhost : MonoBehaviour
{
    public static RemotePlayerGhost Instance { get; private set; }

    private SpriteRenderer _sr;

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

        transform.position = Vector3.Lerp(
            transform.position,
            OnlinePlayerSync.Instance.RemotePlayerPosition,
            Time.deltaTime * 8f);
    }
}
