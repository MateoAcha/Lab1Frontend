using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableUI : MonoBehaviour
{
    private PlayerController _player;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        BuildUI();
    }

    private void LateUpdate()
    {
        if (_label == null) return;

        string name = PlayerLoadout.ConsumableName;
        int qty = PlayerLoadout.ConsumableQuantity;

        if (string.IsNullOrWhiteSpace(name) || PlayerLoadout.ConsumableHealAmount <= 0f)
        {
            _label.text = "[Space] No consumable";
            _label.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            return;
        }

        float remaining = _player != null ? _player.ConsumableCooldownRemaining : 0f;
        bool ready = remaining <= 0f && qty > 0;

        if (qty <= 0)
        {
            _label.text = $"[Space] {name} (empty)";
            _label.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
        else if (!ready)
        {
            _label.text = $"[Space] {name} x{qty}  ({remaining:F1}s)";
            _label.color = new Color(0.9f, 0.6f, 0.2f, 0.9f);
        }
        else
        {
            _label.text = $"[Space] {name} x{qty}";
            _label.color = new Color(0.4f, 1f, 0.6f, 1f);
        }
    }

    private void BuildUI()
    {
        Canvas canvas = FindHUDCanvas("RuntimeGameHUD");
        if (canvas == null) return;

        const string labelName = "ConsumableLabel";
        Transform existing = canvas.transform.Find(labelName);
        GameObject labelObj = existing != null ? existing.gameObject : new GameObject(labelName);
        if (existing == null)
            labelObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        if (rect == null) rect = labelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(340f, 36f);
        rect.anchoredPosition = new Vector2(-16f, 16f);

        _label = labelObj.GetComponent<TextMeshProUGUI>();
        if (_label == null) _label = labelObj.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 18f;
        _label.font = TMP_Settings.defaultFontAsset;
        _label.alignment = TextAlignmentOptions.Right;
        _label.raycastTarget = false;
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
}
