using UnityEngine;

public class PlayerPointer : MonoBehaviour
{
    private Transform _pivot;
    private SpriteRenderer _arrowRenderer;
    private static Sprite _triangleSprite;
    private static readonly Color NormalColor = new Color(1f, 0.92f, 0.15f, 0.9f);
    private static readonly Color DownedColor = new Color(1f, 0.22f, 0.16f, 0.95f);

    private void Start()
    {
        _pivot = new GameObject("PointerPivot").transform;
        _pivot.SetParent(transform, false);
        _pivot.localPosition = Vector3.zero;

        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(_pivot, false);
        arrow.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        arrow.transform.localScale = new Vector3(0.38f, 0.52f, 1f);

        _arrowRenderer = arrow.AddComponent<SpriteRenderer>();
        _arrowRenderer.sprite = GetTriangleSprite();
        _arrowRenderer.color = NormalColor;
        _arrowRenderer.sortingOrder = 20;

        _pivot.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_pivot == null) return;

        bool isOnline = MultiplayerState.IsOnline;

        if (!MultiplayerState.IsMultiplayer && !isOnline)
        {
            _pivot.gameObject.SetActive(false);
            return;
        }

        Vector2 otherPos;
        bool targetDowned = false;

        if (isOnline)
        {
            if (MultiplayerState.IsHost)
            {
                Transform other = MultiplayerState.GetOtherPlayer(transform);
                if (other == null)
                {
                    _pivot.gameObject.SetActive(false);
                    return;
                }
                otherPos = other.position;
                targetDowned = IsTransformDowned(other);
            }
            else
            {
                bool isRemoteGhost = GetComponent<RemotePlayerGhost>() != null;
                if (isRemoteGhost)
                {
                    if (PlayerController.main == null)
                    {
                        _pivot.gameObject.SetActive(false);
                        return;
                    }
                    otherPos = PlayerController.main.transform.position;
                    targetDowned = PlayerController.main.IsDowned;
                }
                else if (OnlinePlayerSync.Instance == null || !OnlinePlayerSync.Instance.HasRemotePlayer)
                {
                    _pivot.gameObject.SetActive(false);
                    return;
                }
                else
                {
                    otherPos = OnlinePlayerSync.Instance.RemotePlayerPosition;
                    targetDowned = OnlinePlayerSync.Instance.RemoteDowned;
                }
            }
        }
        else
        {
            Transform other = MultiplayerState.GetOtherPlayer(transform);
            if (other == null)
            {
                _pivot.gameObject.SetActive(false);
                return;
            }
            otherPos = other.position;
            targetDowned = IsTransformDowned(other);
        }

        _pivot.gameObject.SetActive(true);
        if (_arrowRenderer != null)
            _arrowRenderer.color = targetDowned ? DownedColor : NormalColor;

        Vector2 dir = (otherPos - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        _pivot.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private static Sprite GetTriangleSprite()
    {
        if (_triangleSprite != null) return _triangleSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            // t=0 at bottom (base), t=1 at top (tip)
            float t = (float)y / (size - 1);
            int halfWidth = Mathf.RoundToInt((1f - t) * (size / 2f - 0.5f));
            int cx = size / 2;
            for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = Color.white;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _triangleSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return _triangleSprite;
    }

    private static bool IsTransformDowned(Transform target)
    {
        if (target == null)
            return false;

        PlayerReviveState reviveState = target.GetComponent<PlayerReviveState>();
        return reviveState != null && reviveState.IsDowned;
    }
}
