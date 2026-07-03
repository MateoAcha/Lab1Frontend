using TMPro;
using UnityEngine;

public class PlayerQuickChatBubble : MonoBehaviour
{
    public float y = 1.42f;
    public float visibleSeconds = 2f;

    private SpriteRenderer _iconRenderer;
    private TextMeshPro _label;
    private float _hideAt;

    private void Awake()
    {
        BuildBubble();
        SetVisible(false);
    }

    private void Update()
    {
        if (_hideAt > 0f && Time.time >= _hideAt)
            SetVisible(false);
    }

    public void Show(string emoteId, Sprite icon, bool playSound = true)
    {
        if (_iconRenderer == null || _label == null)
            BuildBubble();

        QuickChatEmoteInfo info = QuickChatEmotes.GetInfo(emoteId);
        _iconRenderer.sprite = icon != null ? icon : SimpleSprite.Circle;
        _iconRenderer.color = icon != null ? Color.white : info.color;
        _iconRenderer.enabled = true;
        ApplyIconScale(icon != null ? 0.5f : 0.42f);
        _label.text = icon != null ? "" : info.label;
        MeshRenderer labelRenderer = _label.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
            labelRenderer.enabled = true;
        _hideAt = Time.time + Mathf.Max(0.1f, visibleSeconds);
        SetVisible(true);

        if (playSound)
            GameAudio.PlayQuickChat();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void BuildBubble()
    {
        Transform existing = transform.Find("QuickChatBubble");
        GameObject root = existing != null ? existing.gameObject : new GameObject("QuickChatBubble");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, y, 0f);
        root.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

        _iconRenderer = root.GetComponent<SpriteRenderer>();
        if (_iconRenderer == null)
            _iconRenderer = root.AddComponent<SpriteRenderer>();
        _iconRenderer.sprite = SimpleSprite.Circle;
        _iconRenderer.sortingOrder = 42;
        ApplyIconScale(0.42f);

        Transform labelTransform = root.transform.Find("Label");
        GameObject labelObj = labelTransform != null ? labelTransform.gameObject : new GameObject("Label");
        labelObj.transform.SetParent(root.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, -0.03f, 0f);
        labelObj.transform.localScale = new Vector3(2.25f, 2.25f, 1f);

        _label = labelObj.GetComponent<TextMeshPro>();
        if (_label == null)
            _label = labelObj.AddComponent<TextMeshPro>();
        _label.font = TMP_Settings.defaultFontAsset;
        _label.fontSize = 2.5f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = Color.white;
        _label.enableWordWrapping = false;
        _label.raycastTarget = false;
        MeshRenderer labelRenderer = _label.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
            labelRenderer.sortingOrder = 43;

        RectTransform rect = _label.rectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(1.4f, 0.8f);
    }

    private void SetVisible(bool visible)
    {
        Transform root = transform.Find("QuickChatBubble");
        if (root != null && root.gameObject.activeSelf != visible)
            root.gameObject.SetActive(visible);
        if (!visible)
            _hideAt = 0f;
    }

    private void ApplyIconScale(float targetDiameter)
    {
        if (_iconRenderer == null || _iconRenderer.sprite == null)
            return;

        Vector2 size = _iconRenderer.sprite.bounds.size;
        float maxDimension = Mathf.Max(0.001f, Mathf.Max(size.x, size.y));
        float scale = Mathf.Max(0.01f, targetDiameter) / maxDimension;
        _iconRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}

public struct QuickChatEmoteInfo
{
    public string id;
    public string label;
    public Color color;
}

public static class QuickChatEmotes
{
    public const string Greet = "greet";
    public const string ThumbsUp = "thumbs_up";
    public const string Danger = "danger";
    public const string Angry = "angry";

    public static readonly string[] Ids = { Greet, ThumbsUp, Danger, Angry };

    public static QuickChatEmoteInfo GetInfo(string emoteId)
    {
        switch (emoteId)
        {
            case ThumbsUp:
                return new QuickChatEmoteInfo { id = ThumbsUp, label = "OK", color = new Color(0.17f, 0.58f, 1f, 0.96f) };
            case Danger:
                return new QuickChatEmoteInfo { id = Danger, label = "!", color = new Color(1f, 0.18f, 0.12f, 0.96f) };
            case Angry:
                return new QuickChatEmoteInfo { id = Angry, label = "NO", color = new Color(0.58f, 0.08f, 0.08f, 0.96f) };
            case Greet:
            default:
                return new QuickChatEmoteInfo { id = Greet, label = "HI", color = new Color(0.22f, 0.72f, 0.35f, 0.96f) };
        }
    }

    public static string NormalizeId(string emoteId)
    {
        QuickChatEmoteInfo info = GetInfo(emoteId);
        return info.id;
    }
}
