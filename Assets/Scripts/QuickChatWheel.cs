using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class QuickChatWheel : MonoBehaviour
{
    private const float ButtonSize = 92f;
    private const float Radius = 86f;

    private PlayerController _player;
    private GameObject _canvasObject;
    private bool _suppressUntilRelease;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool held = IsQuickChatHeld();
        if (!held)
        {
            _suppressUntilRelease = false;
            SetVisible(false);
            return;
        }

        if (_suppressUntilRelease || _player == null || _player.IsDowned || OnlineMatchStartGate.IsWaiting ||
            (!MultiplayerState.IsMultiplayer && !MultiplayerState.IsOnline))
        {
            SetVisible(false);
            return;
        }

        EnsureCanvas();
        SetVisible(true);
    }

    private bool IsQuickChatHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.isPressed;
#else
        return Input.GetKey(KeyCode.R);
#endif
    }

    private void EnsureCanvas()
    {
        if (_canvasObject != null)
            return;

        EnsureEventSystem();

        _canvasObject = new GameObject("QuickChatWheelCanvas");
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2600;

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = CreateUiObject("Wheel", _canvasObject.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(300f, 300f);
        rootRect.anchoredPosition = Vector2.zero;

        AddButton(root.transform, QuickChatEmotes.Greet, new Vector2(0f, Radius));
        AddButton(root.transform, QuickChatEmotes.ThumbsUp, new Vector2(Radius, 0f));
        AddButton(root.transform, QuickChatEmotes.Danger, new Vector2(0f, -Radius));
        AddButton(root.transform, QuickChatEmotes.Angry, new Vector2(-Radius, 0f));

        SetVisible(false);
    }

    private void AddButton(Transform parent, string emoteId, Vector2 anchoredPosition)
    {
        QuickChatEmoteInfo info = QuickChatEmotes.GetInfo(emoteId);
        GameObject obj = CreateUiObject("QuickChat_" + info.id, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        rect.anchoredPosition = anchoredPosition;

        Image image = obj.AddComponent<Image>();
        Sprite icon = _player != null ? _player.GetQuickChatIcon(info.id) : null;
        image.sprite = icon != null ? icon : SimpleSprite.Circle;
        image.color = icon != null ? Color.white : new Color(info.color.r, info.color.g, info.color.b, 0.9f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        string selectedId = info.id;
        button.onClick.AddListener(() =>
        {
            if (_player != null)
                _player.TriggerQuickChat(selectedId);
            _suppressUntilRelease = true;
            SetVisible(false);
        });

        if (icon != null)
            return;

        GameObject textObj = CreateUiObject("Label", obj.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = info.label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
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
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject obj = new GameObject("EventSystem");
            eventSystem = obj.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }
}
