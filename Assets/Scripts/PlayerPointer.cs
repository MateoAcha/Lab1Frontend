using UnityEngine;

public class PlayerPointer : MonoBehaviour
{
    private Transform _pivot;
    private static Sprite _triangleSprite;

    private void Start()
    {
        _pivot = new GameObject("PointerPivot").transform;
        _pivot.SetParent(transform, false);
        _pivot.localPosition = Vector3.zero;

        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(_pivot, false);
        arrow.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        arrow.transform.localScale = new Vector3(0.38f, 0.52f, 1f);

        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        sr.sprite = GetTriangleSprite();
        sr.color = new Color(1f, 0.92f, 0.15f, 0.9f);
        sr.sortingOrder = 20;

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

        if (isOnline)
        {
            if (OnlinePlayerSync.Instance == null || !OnlinePlayerSync.Instance.HasRemotePlayer)
            {
                _pivot.gameObject.SetActive(false);
                return;
            }
            otherPos = OnlinePlayerSync.Instance.RemotePlayerPosition;
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
        }

        _pivot.gameObject.SetActive(true);

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
}
