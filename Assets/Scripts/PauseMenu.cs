using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private GameObject _overlay;
    private bool _paused;

    private void Start()
    {
        BuildUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetPaused(!_paused);
    }

    private void SetPaused(bool paused)
    {
        _paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        _overlay.SetActive(paused);
    }

    private void Resume()
    {
        SetPaused(false);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Destroy(_overlay.transform.root.gameObject);
        SceneManager.LoadScene("Menu");
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        _overlay = new GameObject("PauseOverlay");
        _overlay.transform.SetParent(canvasObj.transform, false);
        RectTransform overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayBg = _overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject card = new GameObject("Card");
        card.transform.SetParent(_overlay.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(360f, 260f);
        cardRect.anchoredPosition = Vector2.zero;
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 36, 36);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddTitle(card.transform);
        AddButton(card.transform, "Resume", new Color(0.14f, 0.48f, 0.28f, 1f), Resume);
        AddButton(card.transform, "Main Menu", new Color(0.28f, 0.16f, 0.16f, 1f), GoToMainMenu);

        _overlay.SetActive(false);
    }

    private void AddTitle(Transform parent)
    {
        GameObject obj = new GameObject("Title");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 52f);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = "Paused";
        text.fontSize = 38;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.95f, 0.96f, 1f, 1f);
        text.font = TMP_Settings.defaultFontAsset;
    }

    private void AddButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(label + "Btn");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 54f);

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 54f;

        Image img = obj.AddComponent<Image>();
        img.color = color;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.12f;
        cb.pressedColor = color * 0.88f;
        cb.selectedColor = color;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.font = TMP_Settings.defaultFontAsset;
    }
}
