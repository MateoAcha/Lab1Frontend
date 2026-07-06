using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    private const int MeleeKillCoins = 1;
    private const int RangedKillCoins = 2;
    private const int GiantKillCoins = 10;
    private TextMeshProUGUI _xpLabel;
    private Image _xpFillImage;

    private void Awake()
    {
        GameStatsTracker.OnPlayerDied += ShowGameOver;
        GameStatsTracker.OnMatchFinished += ShowGameFinished;
        GameStatsTracker.OnPlayerStatsSynced += HandlePlayerStatsSynced;
        GameStatsTracker.OnPvpMatchFinished += ShowPvpResult;
    }

    private void OnDestroy()
    {
        GameStatsTracker.OnPlayerDied -= ShowGameOver;
        GameStatsTracker.OnMatchFinished -= ShowGameFinished;
        GameStatsTracker.OnPlayerStatsSynced -= HandlePlayerStatsSynced;
        GameStatsTracker.OnPvpMatchFinished -= ShowPvpResult;
    }

    private void ShowPvpResult(int loserPlayerIndex)
    {
        Time.timeScale = 0f;

        int winnerIndex = loserPlayerIndex == 0 ? 1 : 0;
        int betCoins = MultiplayerState.PvpBetCoins;

        // In online mode: playerIndex 0 is always the local player
        // Award/deduct coins accordingly — fire-and-forget, don't block the UI
        if (MultiplayerState.IsOnline && AuthSession.IsLoggedIn && betCoins > 0)
        {
            var api = new AuthApiClient(GameStatsTracker.ApiBaseUrl);
            if (loserPlayerIndex == 0) // local player lost
                StartCoroutine(api.SpendCoins(AuthSession.UserId, betCoins, onSuccess: () => { }, onError: err => Debug.LogWarning($"[PvP] SpendCoins failed: {err}")));
            else // local player won
                StartCoroutine(api.AddCoins(AuthSession.UserId, betCoins, onSuccess: () => { }, onError: err => Debug.LogWarning($"[PvP] AddCoins failed: {err}")));
        }

        string winnerName = ResolvePvpPlayerName(winnerIndex);
        string loserName = ResolvePvpPlayerName(loserPlayerIndex);

        int seconds = GameStatsTracker.LastRunTimeSeconds;
        string timeStr = string.Format("{0}:{1:00}", seconds / 60, seconds % 60);

        GameObject canvasObj = new GameObject("PvpResultCanvas");
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
        GameUiThemeRuntime.StylePanel(panel, GameUiThemeRuntime.Current.resultBackground, true);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 10f);
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

        AddLabel(panel.transform, "PvP MATCH OVER", 44, FontStyles.Bold, new Color(0.55f, 0.35f, 1f, 1f));
        AddSpacer(panel.transform, 8f);
        AddLabel(panel.transform, $"🏆  {winnerName} Wins!", 34, FontStyles.Bold, new Color(0.4f, 1f, 0.55f, 1f));
        AddLabel(panel.transform, $"☠  {loserName} was eliminated", 22, FontStyles.Normal, new Color(1f, 0.35f, 0.35f, 1f));
        AddSpacer(panel.transform, 6f);
        AddLabel(panel.transform, $"Match Time:  {timeStr}", 22, FontStyles.Normal, new Color(0.9f, 0.92f, 1f, 1f));
        if (betCoins > 0 && MultiplayerState.IsOnline)
        {
            string betResult = loserPlayerIndex == 0
                ? $"You lost the bet:  -{betCoins} coins"
                : $"You won the bet:  +{betCoins} coins";
            Color betColor = loserPlayerIndex == 0 ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(1f, 0.85f, 0.15f, 1f);
            AddLabel(panel.transform, betResult, 22, FontStyles.Bold, betColor);
        }
        AddSpacer(panel.transform, 10f);

        AddButton(panel.transform, "Main Menu", new Color(0.18f, 0.22f, 0.35f, 1f), () =>
        {
            Time.timeScale = 1f;
            Destroy(canvasObj);
            GameAudio.StopMusic();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        });
    }

    private static string ResolvePvpPlayerName(int playerIndex)
    {
        // NOTE: player.NetworkUsername is intentionally NOT used here because its property
        // always returns a non-empty fallback ("Player"/"Guest"), masking the real username.
        if (MultiplayerState.IsOnline)
        {
            // Index 0 = local player on this machine (host or guest).
            // Index 1 = remote player.
            if (playerIndex == 0)
                return PlayerDisplayNames.LocalUsernameOrFallback(MultiplayerState.IsHost ? "Host" : "Guest");

            string remoteUsername = OnlinePlayerSync.Instance != null
                ? OnlinePlayerSync.Instance.RemoteUsername
                : "";
            return !string.IsNullOrWhiteSpace(remoteUsername)
                ? remoteUsername
                : (MultiplayerState.IsHost ? "Guest" : "Host");
        }

        if (playerIndex == 0) return "Player 1";
        return "Player 2";
    }

    private void ShowGameOver(int meleeKills, int rangedKills, int giantKills, int seconds)
    {
        Show(meleeKills, rangedKills, giantKills, seconds, false);
    }

    private void ShowGameFinished(int meleeKills, int rangedKills, int giantKills, int seconds)
    {
        Show(meleeKills, rangedKills, giantKills, seconds, true);
    }

    private void Show(int meleeKills, int rangedKills, int giantKills, int seconds, bool finished)
    {
        int coins = meleeKills * MeleeKillCoins + rangedKills * RangedKillCoins + giantKills * GiantKillCoins;

        GameObject canvasObj = new GameObject(finished ? "GameFinishedCanvas" : "GameOverCanvas");
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

        GameUiThemeRuntime.StylePanel(panel, GameUiThemeRuntime.Current.resultBackground, true);

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

        AddLabel(
            panel.transform,
            finished ? "GAME FINISHED" : "GAME OVER",
            44,
            FontStyles.Bold,
            finished ? new Color(0.5f, 1f, 0.72f, 1f) : new Color(1f, 0.28f, 0.28f, 1f));
        AddSpacer(panel.transform, 6f);

        string timeStr = string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
        AddLabel(panel.transform, $"{(finished ? "Run Time" : "Time Survived")}:  {timeStr}", 24, FontStyles.Normal, new Color(0.9f, 0.92f, 1f, 1f));
        AddLabel(panel.transform, $"Melee Enemies Killed:  {meleeKills}", 22, FontStyles.Normal, new Color(1f, 0.65f, 0.35f, 1f));
        AddLabel(panel.transform, $"Ranged Enemies Killed:  {rangedKills}", 22, FontStyles.Normal, new Color(0.4f, 0.82f, 1f, 1f));
        AddLabel(panel.transform, $"Giants Killed:  {giantKills}", 22, FontStyles.Normal, new Color(0.78f, 0.55f, 1f, 1f));
        AddLabel(panel.transform, $"Total Kills:  {meleeKills + rangedKills + giantKills}", 22, FontStyles.Bold, new Color(0.95f, 0.95f, 1f, 1f));

        AddSpacer(panel.transform, 4f);

        _xpLabel = AddLabel(
            panel.transform,
            "",
            20,
            FontStyles.Bold,
            new Color(0.54f, 0.78f, 1f, 1f));
        AddXpProgressBar(panel.transform);
        UpdateXpProgress(GameStatsTracker.GetCurrentPlayerStats());

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

        AddButton(panel.transform, "Main Menu", new Color(0.18f, 0.22f, 0.35f, 1f), () =>
        {
            Time.timeScale = 1f;
            Destroy(canvasObj);
            GameAudio.StopMusic();
            SceneManager.LoadScene("Menu");
        });
    }

    private void HandlePlayerStatsSynced(PlayerStatsData stats)
    {
        UpdateXpProgress(stats);
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

    private void AddXpProgressBar(Transform parent)
    {
        GameObject bar = new GameObject("XpProgressBar");
        bar.transform.SetParent(parent, false);
        bar.AddComponent<LayoutElement>().preferredHeight = 18f;

        Image bg = bar.AddComponent<Image>();
        bg.color = GameUiThemeRuntime.Current.surface;
        GameUiThemeRuntime.ApplyBorder(bar);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bar.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        _xpFillImage = fill.AddComponent<Image>();
        _xpFillImage.color = new Color(0.38f, 0.72f, 1f, 1f);
    }

    private void UpdateXpProgress(PlayerStatsData stats)
    {
        if (stats == null || _xpLabel == null)
            return;

        int currentXp = GameStatsTracker.GetXpIntoCurrentLevel(stats);
        int neededXp = Mathf.Max(1, GameStatsTracker.GetXpNeededForNextLevel(stats));
        _xpLabel.text = $"XP Earned:  {GameStatsTracker.LastRunXpEarned}    Level {stats.level}:  {currentXp}/{neededXp}";

        if (_xpFillImage != null)
        {
            RectTransform fillRect = _xpFillImage.rectTransform;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01((float)currentXp / neededXp), 1f);
            fillRect.offsetMax = Vector2.zero;
        }
    }

    private void AddButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject("Button_" + label);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<LayoutElement>().preferredHeight = 52f;

        Image img = obj.AddComponent<Image>();
        Button btn = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, color);
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
        tmp.color = GameUiThemeRuntime.Current.text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
    }
}
