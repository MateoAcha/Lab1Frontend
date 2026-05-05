using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    private void Awake()
    {
        GameStatsTracker.OnPlayerDied += Show;
    }

    private void OnDestroy()
    {
        GameStatsTracker.OnPlayerDied -= Show;
    }

    private void Show(int meleeKills, int rangedKills, int seconds)
    {
        int coins = meleeKills + rangedKills * 2;

        GameObject canvasObj = new GameObject("GameOverCanvas");
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Image bg = canvasObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.10f, 0.14f, 0.97f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(480f, 10f);
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 36, 36);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        AddLabel(panel.transform, "GAME OVER", 44, FontStyles.Bold, new Color(1f, 0.28f, 0.28f, 1f));
        AddSpacer(panel.transform, 6f);

        string timeStr = string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
        AddLabel(panel.transform, $"Time Survived:  {timeStr}", 24, FontStyles.Normal, new Color(0.9f, 0.92f, 1f, 1f));
        AddLabel(panel.transform, $"Melee Enemies Killed:  {meleeKills}", 22, FontStyles.Normal, new Color(1f, 0.65f, 0.35f, 1f));
        AddLabel(panel.transform, $"Ranged Enemies Killed:  {rangedKills}", 22, FontStyles.Normal, new Color(0.4f, 0.82f, 1f, 1f));
        AddLabel(panel.transform, $"Total Kills:  {meleeKills + rangedKills}", 22, FontStyles.Bold, new Color(0.95f, 0.95f, 1f, 1f));

        AddSpacer(panel.transform, 4f);

        TextMeshProUGUI coinsLabel = AddLabel(
            panel.transform,
            $"Gold Coins Earned:  {coins}",
            26, FontStyles.Bold,
            new Color(1f, 0.85f, 0.15f, 1f));

        if (AuthSession.IsLoggedIn && coins > 0)
        {
            coinsLabel.text = $"Gold Coins Earned:  {coins}  (saving...)";
            StartCoroutine(AwardCoins(coins, coinsLabel));
        }
        else if (!AuthSession.IsLoggedIn)
        {
            AddLabel(panel.transform, "Log in to save coins to your account.", 15, FontStyles.Italic, new Color(0.6f, 0.65f, 0.7f, 0.8f));
        }

        AddSpacer(panel.transform, 8f);

        AddButton(panel.transform, "Play Again", new Color(0.14f, 0.42f, 0.22f, 1f), () =>
        {
            Destroy(canvasObj);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        AddButton(panel.transform, "Main Menu", new Color(0.18f, 0.22f, 0.35f, 1f), () =>
        {
            Destroy(canvasObj);
            SceneManager.LoadScene("Menu");
        });
    }

    private IEnumerator AwardCoins(int coins, TextMeshProUGUI label)
    {
        var api = new AuthApiClient(GameStatsTracker.ApiBaseUrl);
        string error = null;

        yield return api.AddCoins(
            AuthSession.UserId,
            coins,
            onSuccess: () => { },
            onError: err => error = err);

        if (label == null) yield break;

        if (error == null)
        {
            label.text = $"Gold Coins Earned:  {coins}  (saved)";
            label.color = new Color(0.6f, 1f, 0.4f, 1f);
        }
        else
        {
            label.text = $"Gold Coins Earned:  {coins}  (save failed)";
            label.color = new Color(1f, 0.5f, 0.2f, 1f);
            Debug.LogWarning($"[GameOver] Coin save failed: {error}");
        }
    }

    private TextMeshProUGUI AddLabel(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = size + 10f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(row.transform, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void AddSpacer(Transform parent, float height)
    {
        GameObject obj = new GameObject("Spacer");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private void AddButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject("Button_" + label);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<LayoutElement>().preferredHeight = 52f;

        Image img = obj.AddComponent<Image>();
        img.color = color;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.18f;
        cb.pressedColor = color * 0.82f;
        cb.selectedColor = color;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
    }
}
