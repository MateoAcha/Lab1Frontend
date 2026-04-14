using UnityEngine;
using UnityEngine.UI;

public class ChargeAbilityUI : MonoBehaviour
{
    public Vector2 iconSize = new Vector2(64f, 64f);
    public Vector2 anchoredPosition = new Vector2(-45f, 22f);
    public Color readyColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Range(0f, 1f)] public float cooldownOpacity = 0.25f;

    private PlayerController player;
    private Image dimImage;
    private Image fillImage;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        BuildUI();
    }

    private void LateUpdate()
    {
        if (player == null || dimImage == null || fillImage == null)
        {
            return;
        }

        float progress = player.ChargeCooldownProgress01;
        bool ready = progress >= 0.999f;

        Color dim = readyColor;
        dim.a = ready ? readyColor.a : Mathf.Clamp01(cooldownOpacity);
        dimImage.color = dim;

        fillImage.color = readyColor;
        fillImage.fillAmount = Mathf.Clamp01(progress);
    }

    private void BuildUI()
    {
        const string canvasName = "RuntimeGameHUD";
        const string iconName = "ChargeAbilityIcon";

        Canvas canvas = FindHUDCanvas(canvasName);
        if (canvas == null)
        {
            return;
        }

        Transform existingIcon = canvas.transform.Find(iconName);
        GameObject icon = existingIcon != null ? existingIcon.gameObject : new GameObject(iconName);
        if (existingIcon == null)
        {
            icon.transform.SetParent(canvas.transform, false);
        }

        RectTransform iconRect = icon.GetComponent<RectTransform>();
        if (iconRect == null)
        {
            iconRect = icon.AddComponent<RectTransform>();
        }

        iconRect.anchorMin = new Vector2(0.5f, 0f);
        iconRect.anchorMax = new Vector2(0.5f, 0f);
        iconRect.pivot = new Vector2(0.5f, 0f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = anchoredPosition;

        Image border = GetOrCreateImage(icon.transform, "Border");
        border.sprite = SimpleSprite.Square;
        border.color = new Color(0f, 0f, 0f, 0.65f);

        dimImage = GetOrCreateImage(icon.transform, "Dim");
        dimImage.sprite = SimpleSprite.Square;

        fillImage = GetOrCreateImage(icon.transform, "Fill");
        fillImage.sprite = SimpleSprite.Square;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 1f;
    }

    private Canvas FindHUDCanvas(string canvasName)
    {
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj == null)
        {
            canvasObj = new GameObject(canvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        Canvas existing = canvasObj.GetComponent<Canvas>();
        if (existing == null)
        {
            existing = canvasObj.AddComponent<Canvas>();
            existing.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (canvasObj.GetComponent<CanvasScaler>() == null)
        {
            canvasObj.AddComponent<CanvasScaler>();
        }

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<GraphicRaycaster>();
        }

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
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }

        image.raycastTarget = false;
        return image;
    }
}
