using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class OnlineMatchStartGate
{
    public static bool IsWaiting { get; private set; }

    private static GameObject _canvasObj;
    private static TextMeshProUGUI _messageText;

    public static void Show(string message)
    {
        IsWaiting = true;
        Time.timeScale = 0f;
        EnsureOverlay();
        SetMessage(message);
    }

    public static void SetMessage(string message)
    {
        EnsureOverlay();
        if (_messageText != null)
            _messageText.text = string.IsNullOrWhiteSpace(message) ? "Syncing online match..." : message;
    }

    public static void Hide()
    {
        IsWaiting = false;
        if (!PauseMenu.IsPaused)
            Time.timeScale = 1f;
        DestroyOverlay();
    }

    public static void Reset()
    {
        IsWaiting = false;
        Time.timeScale = 1f;
        DestroyOverlay();
    }

    private static void EnsureOverlay()
    {
        if (_canvasObj != null)
        {
            _canvasObj.SetActive(true);
            return;
        }

        _canvasObj = new GameObject("OnlineMatchLoadingCanvas");
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasObj.AddComponent<GraphicRaycaster>();

        GameObject background = new GameObject("Background");
        background.transform.SetParent(_canvasObj.transform, false);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.015f, 0.018f, 0.024f, 0.96f);

        GameObject textObj = new GameObject("Message");
        textObj.transform.SetParent(background.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(760f, 140f);
        textRect.anchoredPosition = Vector2.zero;

        _messageText = textObj.AddComponent<TextMeshProUGUI>();
        _messageText.font = TMP_Settings.defaultFontAsset;
        _messageText.fontSize = 34f;
        _messageText.fontStyle = FontStyles.Bold;
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.color = new Color(0.86f, 0.94f, 1f, 1f);
        _messageText.enableWordWrapping = true;
        _messageText.raycastTarget = false;
    }

    private static void DestroyOverlay()
    {
        if (_canvasObj == null)
            return;

        Object.Destroy(_canvasObj);
        _canvasObj = null;
        _messageText = null;
    }
}
