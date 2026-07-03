using System.Collections.Generic;
using UnityEngine;

public class NetworkedBloodBurst : MonoBehaviour
{
    public static readonly List<NetworkedBloodBurst> Active = new List<NetworkedBloodBurst>();

    public Vector2 HitPoint { get; private set; }
    public float SizeMultiplier { get; private set; } = 1f;
    public float RemainingLife => Mathf.Max(0f, _destroyAt - Time.time);

    private float _destroyAt;

    public static void Spawn(Vector2 position, Vector2 hitPoint, float sizeMultiplier)
    {
        if (!MultiplayerState.IsOnline || !MultiplayerState.IsHost)
            return;

        GameObject obj = new GameObject("NetworkedBloodBurst");
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        NetworkedBloodBurst burst = obj.AddComponent<NetworkedBloodBurst>();
        burst.Configure(hitPoint, sizeMultiplier);
    }

    private void Awake()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }

    private void Update()
    {
        if (Time.time >= _destroyAt)
            Destroy(gameObject);
    }

    private void Configure(Vector2 hitPoint, float sizeMultiplier)
    {
        HitPoint = hitPoint;
        SizeMultiplier = Mathf.Max(0.2f, sizeMultiplier);
        _destroyAt = Time.time + 0.85f;
    }
}
