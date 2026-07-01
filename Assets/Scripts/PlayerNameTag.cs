using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    public float y = 0.98f;

    private TextMeshPro _text;
    private MeshRenderer _renderer;
    private PlayerController _player;
    private string _displayName = "";
    private bool _available = true;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        BuildText();
    }

    private void LateUpdate()
    {
        if (_player != null)
            _displayName = _player.DisplayName;

        if (_text != null)
        {
            _text.text = _displayName;
            _text.transform.localPosition = new Vector3(0f, y, 0f);
        }

        bool visible = _available &&
            (MultiplayerState.IsMultiplayer || MultiplayerState.IsOnline) &&
            !string.IsNullOrWhiteSpace(_displayName);
        SetVisible(visible);
    }

    public void SetDisplayName(string displayName)
    {
        _displayName = PlayerDisplayNames.Normalize(displayName, "Player");
        if (_text != null)
            _text.text = _displayName;
    }

    public void SetAvailable(bool available)
    {
        _available = available;
        if (!_available)
            SetVisible(false);
    }

    private void BuildText()
    {
        Transform existing = transform.Find("PlayerNameTag");
        GameObject obj = existing != null ? existing.gameObject : new GameObject("PlayerNameTag");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, y, 0f);

        _text = obj.GetComponent<TextMeshPro>();
        if (_text == null)
            _text = obj.AddComponent<TextMeshPro>();
        _text.font = TMP_Settings.defaultFontAsset;
        _text.fontSize = 2.7f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = new Color(1f, 1f, 1f, 0.96f);
        _text.enableWordWrapping = false;
        _text.raycastTarget = false;

        RectTransform rect = _text.rectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(4.2f, 0.5f);

        _renderer = _text.GetComponent<MeshRenderer>();
        if (_renderer != null)
            _renderer.sortingOrder = 33;

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_renderer != null)
            _renderer.enabled = visible;
        if (_text != null && _text.gameObject.activeSelf != visible)
            _text.gameObject.SetActive(visible);
    }
}
