using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableUI : MonoBehaviour
{
    public Vector2 iconSize = new Vector2(76f, 76f);
    public Vector2 anchoredPosition = new Vector2(-24f, 24f);
    public Color readyColor = new Color(0.35f, 1f, 0.62f, 1f);
    public Color speedBoostColor = new Color(0.35f, 0.78f, 1f, 1f);
    public Color emptyColor = new Color(0.45f, 0.45f, 0.45f, 0.72f);
    [Range(0f, 1f)] public float cooldownOpacity = 0.25f;

    private PlayerController _player;
    private Image _dimImage;
    private Image _fillImage;
    private Image _iconImage;
    private TextMeshProUGUI _quantityText;
    private Sprite _generatedHealthTextureIcon;
    private Sprite _generatedSpeedTextureIcon;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        BuildUI();
    }

    private void LateUpdate()
    {
        if (_dimImage == null || _fillImage == null || _iconImage == null || _quantityText == null)
        {
            return;
        }

        bool speedBoost = PlayerLoadout.ConsumableIsSpeedBoost;
        bool hasConsumable = !string.IsNullOrWhiteSpace(PlayerLoadout.ConsumableName)
            && (speedBoost || PlayerLoadout.ConsumableHealAmount > 0f);
        int quantity = hasConsumable ? Mathf.Max(0, PlayerLoadout.ConsumableQuantity) : 0;
        float cooldown = Mathf.Max(0f, PlayerLoadout.ConsumableCooldown);
        float remaining = _player != null ? _player.ConsumableCooldownRemaining : 0f;
        float progress = cooldown > 0f ? 1f - Mathf.Clamp01(remaining / cooldown) : 1f;
        bool ready = hasConsumable && quantity > 0 && remaining <= 0f;

        Color activeColor = speedBoost ? speedBoostColor : readyColor;
        Color dim = hasConsumable && quantity > 0 ? activeColor : emptyColor;
        dim.a = ready ? activeColor.a : Mathf.Clamp01(cooldownOpacity);
        _dimImage.color = dim;

        _fillImage.color = activeColor;
        _fillImage.fillAmount = hasConsumable && quantity > 0 ? Mathf.Clamp01(progress) : 0f;

        Sprite icon = ResolveIcon();
        _iconImage.sprite = icon;
        _iconImage.color = hasConsumable && quantity > 0
            ? Color.white
            : new Color(1f, 1f, 1f, 0.34f);

        _quantityText.text = quantity.ToString();
        _quantityText.color = quantity > 0 ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.9f);
    }

    private void BuildUI()
    {
        Canvas canvas = FindHUDCanvas("RuntimeGameHUD");
        if (canvas == null) return;

        const string oldLabelName = "ConsumableLabel";
        Transform oldLabel = canvas.transform.Find(oldLabelName);
        if (oldLabel != null)
        {
            Destroy(oldLabel.gameObject);
        }

        const string iconName = "ConsumableIcon";
        Transform existing = canvas.transform.Find(iconName);
        GameObject root = existing != null ? existing.gameObject : new GameObject(iconName);
        if (existing == null)
        {
            root.transform.SetParent(canvas.transform, false);
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null) rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = iconSize;
        rect.anchoredPosition = anchoredPosition;

        Image border = GetOrCreateImage(root.transform, "Border");
        border.sprite = SimpleSprite.Square;
        border.color = new Color(0f, 0f, 0f, 0.68f);

        _dimImage = GetOrCreateImage(root.transform, "Dim");
        _dimImage.sprite = SimpleSprite.Square;

        _fillImage = GetOrCreateImage(root.transform, "Fill");
        _fillImage.sprite = SimpleSprite.Square;
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Vertical;
        _fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        _fillImage.fillAmount = 1f;

        _iconImage = GetOrCreateImage(root.transform, "Icon");
        _iconImage.sprite = ResolveIcon();
        SetOffsets(_iconImage.rectTransform, new Vector2(14f, 14f), new Vector2(-14f, -18f));
        _iconImage.preserveAspect = true;

        _quantityText = GetOrCreateText(root.transform, "Quantity");
        RectTransform qtyRect = _quantityText.rectTransform;
        qtyRect.anchorMin = new Vector2(0f, 0f);
        qtyRect.anchorMax = new Vector2(1f, 0f);
        qtyRect.pivot = new Vector2(0.5f, 0f);
        qtyRect.sizeDelta = new Vector2(0f, 34f);
        qtyRect.anchoredPosition = new Vector2(0f, 2f);
        _quantityText.fontSize = 32f;
        _quantityText.fontStyle = FontStyles.Bold;
        _quantityText.alignment = TextAlignmentOptions.Center;
        _quantityText.raycastTarget = false;
    }

    private Sprite ResolveIcon()
    {
        bool speedBoost = PlayerLoadout.ConsumableIsSpeedBoost;
        if (_player == null)
        {
            return SimpleSprite.Circle;
        }

        Sprite assignedIcon = speedBoost ? _player.speedConsumableIcon : _player.healthConsumableIcon;
        if (assignedIcon != null)
        {
            return assignedIcon;
        }

        Texture2D texture = speedBoost ? _player.speedConsumableIconTexture : _player.healthConsumableIconTexture;
        if (texture != null)
        {
            Sprite generated = speedBoost ? _generatedSpeedTextureIcon : _generatedHealthTextureIcon;
            if (generated == null || generated.texture != texture)
            {
                generated = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(texture.width, texture.height));

                if (speedBoost)
                {
                    _generatedSpeedTextureIcon = generated;
                }
                else
                {
                    _generatedHealthTextureIcon = generated;
                }
            }

            return generated;
        }

        return SimpleSprite.Circle;
    }

    private Canvas FindHUDCanvas(string canvasName)
    {
        GameObject obj = GameObject.Find(canvasName);
        if (obj == null)
        {
            obj = new GameObject(canvasName);
            Canvas c = obj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            obj.AddComponent<CanvasScaler>();
            obj.AddComponent<GraphicRaycaster>();
            return c;
        }

        Canvas existing = obj.GetComponent<Canvas>();
        if (existing == null)
        {
            existing = obj.AddComponent<Canvas>();
            existing.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        if (obj.GetComponent<CanvasScaler>() == null) obj.AddComponent<CanvasScaler>();
        if (obj.GetComponent<GraphicRaycaster>() == null) obj.AddComponent<GraphicRaycaster>();
        return existing;
    }

    private Image GetOrCreateImage(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null ? child.gameObject : new GameObject(name);
        if (child == null)
        {
            obj.transform.SetParent(parent, false);
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        SetOffsets(rect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        Image image = obj.GetComponent<Image>();
        if (image == null) image = obj.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI GetOrCreateText(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null ? child.gameObject : new GameObject(name);
        if (child == null)
        {
            obj.transform.SetParent(parent, false);
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text == null) text = obj.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) return;

        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
