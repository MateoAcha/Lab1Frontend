using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class QuickChatWheel : MonoBehaviour
{
    private const float WheelSize = 320f;
    private const float SlotSize = 94f;
    private const float IconSize = 54f;
    private const float Radius = 100f;
    private const float SelectionDeadZone = 28f;

    private PlayerController _player;
    private GameObject _canvasObject;
    private WheelSlot[] _slots;
    private int _highlightedIndex = -1;
    private bool _wasHeld;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool held = IsQuickChatHeld();
        if (!held)
        {
            if (_wasHeld)
                SubmitHighlighted();
            _wasHeld = false;
            SetVisible(false);
            return;
        }

        _wasHeld = true;
        if (_player == null || _player.IsDowned || OnlineMatchStartGate.IsWaiting ||
            (!MultiplayerState.IsMultiplayer && !MultiplayerState.IsOnline))
        {
            SetHighlighted(-1);
            SetVisible(false);
            return;
        }

        EnsureCanvas();
        SetVisible(true);
        UpdateSelection();
    }

    private bool IsQuickChatHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.isPressed;
#else
        return Input.GetKey(KeyCode.R);
#endif
    }

    private Vector2 ReadPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
        return Input.mousePosition;
#endif
    }

    private void EnsureCanvas()
    {
        if (_canvasObject != null)
            return;

        _canvasObject = new GameObject("QuickChatWheelCanvas");
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2600;

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = CreateUiObject("Wheel", _canvasObject.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(WheelSize, WheelSize);
        rootRect.anchoredPosition = Vector2.zero;

        AddCircle(root.transform, "Background", WheelSize, new Color(0f, 0f, 0f, 0.58f));
        AddCircle(root.transform, "Inner", 78f, new Color(1f, 1f, 1f, 0.08f));

        _slots = new[]
        {
            AddSlot(root.transform, QuickChatEmotes.Greet, new Vector2(0f, Radius)),
            AddSlot(root.transform, QuickChatEmotes.ThumbsUp, new Vector2(Radius, 0f)),
            AddSlot(root.transform, QuickChatEmotes.Danger, new Vector2(0f, -Radius)),
            AddSlot(root.transform, QuickChatEmotes.Angry, new Vector2(-Radius, 0f))
        };

        SetVisible(false);
    }

    private void AddCircle(Transform parent, string name, float size, Color color)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        Image image = obj.AddComponent<Image>();
        image.sprite = SimpleSprite.Circle;
        image.color = color;
        image.raycastTarget = false;
    }

    private WheelSlot AddSlot(Transform parent, string emoteId, Vector2 anchoredPosition)
    {
        QuickChatEmoteInfo info = QuickChatEmotes.GetInfo(emoteId);
        GameObject obj = CreateUiObject("QuickChat_" + info.id, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);
        rect.anchoredPosition = anchoredPosition;

        Image frame = obj.AddComponent<Image>();
        frame.sprite = SimpleSprite.Circle;
        frame.color = new Color(0.03f, 0.035f, 0.045f, 0.92f);
        frame.raycastTarget = false;

        GameObject iconObj = CreateUiObject("Icon", obj.transform);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);
        iconRect.anchoredPosition = Vector2.zero;

        Image iconImage = iconObj.AddComponent<Image>();
        Sprite icon = _player != null ? _player.GetQuickChatIcon(info.id) : null;
        iconImage.sprite = icon != null ? icon : SimpleSprite.Circle;
        iconImage.color = icon != null ? Color.white : info.color;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        if (icon != null)
        {
            return new WheelSlot
            {
                id = info.id,
                info = info,
                root = rect,
                frame = frame
            };
        }

        GameObject textObj = CreateUiObject("Label", obj.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = info.label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return new WheelSlot
        {
            id = info.id,
            info = info,
            root = rect,
            frame = frame
        };
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasObject != null && _canvasObject.activeSelf != visible)
            _canvasObject.SetActive(visible);
        if (!visible)
            SetHighlighted(-1);
    }

    private void UpdateSelection()
    {
        Vector2 pointer = ReadPointerPosition();
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 delta = pointer - center;
        if (delta.magnitude < SelectionDeadZone)
        {
            SetHighlighted(-1);
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            SetHighlighted(delta.x > 0f ? 1 : 3);
        else
            SetHighlighted(delta.y > 0f ? 0 : 2);
    }

    private void SetHighlighted(int index)
    {
        if (_highlightedIndex == index && _slots != null)
            return;

        _highlightedIndex = index;
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            WheelSlot slot = _slots[i];
            if (slot == null || slot.frame == null || slot.root == null)
                continue;

            bool selected = i == _highlightedIndex;
            Color color = slot.info.color;
            slot.frame.color = selected
                ? new Color(color.r, color.g, color.b, 0.98f)
                : new Color(0.03f, 0.035f, 0.045f, 0.92f);
            slot.root.localScale = selected ? Vector3.one * 1.16f : Vector3.one;
        }
    }

    private void SubmitHighlighted()
    {
        if (_highlightedIndex < 0 || _slots == null || _highlightedIndex >= _slots.Length)
            return;
        if (_player == null || _player.IsDowned)
            return;

        WheelSlot slot = _slots[_highlightedIndex];
        if (slot != null)
            _player.TriggerQuickChat(slot.id);
    }

    private void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    private class WheelSlot
    {
        public string id;
        public QuickChatEmoteInfo info;
        public RectTransform root;
        public Image frame;
    }
}
