using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class AuthMenuController : MonoBehaviour
{
    private const string LastOnlineServerPrefsKey = "auth_last_online_server_url";

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject registerPanel;
    public GameObject loginPanel;
    public GameObject profilePanel;
    public GameObject statsPanel;
    public GameObject errorPanel;

    [Header("Register Inputs")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;

    [Header("Login Inputs")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Error UI")]
    public TextMeshProUGUI errorText;

    [Header("Session UI")]
    public TextMeshProUGUI sessionText;
    public TextMeshProUGUI authProofText;
    public GameObject registerButton;
    public GameObject loginButton;
    public GameObject profileButton;
    public GameObject logoutButton;

    [Header("Daily Coins")]
    public Button dailyCoinsButton;
    public TextMeshProUGUI dailyCoinsTimerText;
    public Color dailyCoinsClaimableColor = new Color(0.14f, 0.42f, 0.22f, 1f);
    public Color dailyCoinsClaimedColor = new Color(0.20f, 0.23f, 0.29f, 1f);
    public string dailyCoinsClaimableText = "Claim Daily Coins";
    public string dailyCoinsClaimedText = "Already Claimed";
    public string dailyCoinsLoadingText = "Checking...";
    public string dailyCoinsClaimingText = "Claiming...";

    [Header("UI Theme")]
    public GameUiTheme uiTheme = new GameUiTheme();

    [Header("Skin Visuals")]
    public SkinVisualDatabase skinVisualDatabase;

    [Header("Weapon Visuals")]
    public WeaponVisualDatabase weaponVisualDatabase;

    [Header("Play")]
    public GameObject gamePrefab;
    public Transform gameParent;
    public GameObject menuRoot;

    [Header("Maps")]
    public MapSelectOption[] mapSelectOptions = GameMapSelection.CreateDefaultMapSelectOptions();

    [Header("Config")]
    public string apiBaseUrl = "http://localhost:8080";
    public string gameplaySceneName = "GameScene";
    public string googleClientId = "";

    private AuthApiClient _apiClient;
    private GameObject _gameInstance;
    private GameObject _inventoryPanel;
    private InventoryPanelController _inventoryPanelController;
    private Button _inventoryButton;
    private Image _inventoryButtonImage;
    private TextMeshProUGUI _inventoryButtonText;
    private GameObject _shopPanel;
    private ShopPanelController _shopPanelController;
    private Button _shopButton;
    private Image _shopButtonImage;
    private TextMeshProUGUI _shopButtonText;
    private GameObject _skillTreePanel;
    private SkillTreePanelController _skillTreePanelController;
    private Button _skillTreeButton;
    private Image _skillTreeButtonImage;
    private TextMeshProUGUI _skillTreeButtonText;
    private Button _statsCardButton;
    private Image _statsCardImage;
    private TextMeshProUGUI _statsCardText;
    private Button _profileBackButton;
    private Image _profileBackButtonImage;
    private Button _profileLogoutButton;
    private Image _profileLogoutButtonImage;
    private bool _profileUiStyled;
    private bool _loginUiStyled;
    private bool _registerUiStyled;
    private string _currentServerUrl;
    private string _lastOnlineServerUrl;
    private Button _loginLocalServerButton;
    private Image _loginLocalServerImage;
    private Button _loginOnlineServerButton;
    private Image _loginOnlineServerImage;
    private TextMeshProUGUI _loginServerInfoText;
    private Button _registerLocalServerButton;
    private Image _registerLocalServerImage;
    private Button _registerOnlineServerButton;
    private Image _registerOnlineServerImage;
    private TextMeshProUGUI _registerServerInfoText;
    private TextMeshProUGUI _dailyCoinsButtonText;
    private Coroutine _dailyCoinsStatusRoutine;
    private DailyCoinsStatusData _dailyCoinsStatus;
    private string _dailyCoinsStatusServerUrl = "";
    private float _dailyCoinsStatusReceivedAt;
    private bool _dailyCoinsUiHooked;
    private bool _dailyCoinsClaimInFlight;
    private GameObject _multiplayerPanel;
    private GameObject _onlineLobbyPanel;
    private SocialPanelController _mainSocialPanel;
    private SocialPanelController _lobbySocialPanel;
    private TextMeshProUGUI _myNameText;
    private TextMeshProUGUI _myWeaponText;
    private TextMeshProUGUI _myArmorText;
    private TextMeshProUGUI _myItemText;
    private TextMeshProUGUI _otherNameText;
    private TextMeshProUGUI _otherWeaponText;
    private TextMeshProUGUI _otherArmorText;
    private TextMeshProUGUI _otherItemText;
    private TextMeshProUGUI _hostAddressText;
    private Coroutine _lobbyPollRoutine;
    private bool _isHostSession;
    private Button _lobbyPlayButton;
    private Image _lobbyPlayButtonImage;
    private TextMeshProUGUI _lobbyPlayButtonText;
    private TextMeshProUGUI _lobbyWaitingText;
    private int _currentLobbyRoomNumber;
    private int _lobbyPlayerCount;
    private GameObject _roomListPanel;
    private Transform _roomListContent;
    private TextMeshProUGUI _roomListStatusText;
    private Button _createSessionButton;
    private TextMeshProUGUI _createSessionTitleText;
    private TextMeshProUGUI _createSessionSubtitleText;
    private GameObject _joinSessionPanel;
    private TMP_InputField _joinServerUrlInput;
    private TextMeshProUGUI _joinSessionTitleText;
    private TextMeshProUGUI _joinSessionAddressLabelText;
    private TextMeshProUGUI _joinSessionHintText;
    private TextMeshProUGUI _joinSessionSubmitText;
    private GameObject _googleLoginPanel;
    private GameObject _googleUsernameInputObj;
    private TMP_InputField _googleIdTokenInput;
    private TMP_InputField _googleUsernameInput;
    private TextMeshProUGUI _googleStatusText;
    private TextMeshProUGUI _googleSubmitText;
    private Button _googleSubmitButton;
    private Coroutine _googleOAuthRoutine;
    private string _pendingGoogleAuthCode = "";
    private string _pendingGoogleCodeVerifier = "";
    private string _pendingGoogleRedirectUri = "";
    private string _pendingGoogleSignupToken = "";
    private GameObject _googleReturnPanel;
    private GameObject _mapSelectPanel;
    private GameObject _mapSelectReturnPanel;
    private Action<int> _pendingMapSelection;
    private bool _launchMultiplayer;
    private bool _mainMenuButtonsBuilt;
    private bool _sessionValidationInProgress;
    private string _sessionValidationMessage = "";
    private readonly string _lobbyClientId = Guid.NewGuid().ToString("N");

    private enum LoginReturn { MainMenu, OnlineLobby, OnlineRoomList }
    private enum ServerAddressMode { JoinSession, AuthServer }
    private LoginReturn _loginReturn = LoginReturn.MainMenu;
    private ServerAddressMode _serverAddressMode = ServerAddressMode.JoinSession;
    private GameObject _serverAddressReturnPanel;

    private void OnApplicationQuit()
    {
    }

    private void Start()
    {
        GameAudio.EnsureMenuMusic();
        GameUiThemeRuntime.SetTheme(uiTheme);
        SkinVisualDatabase.Register(skinVisualDatabase);
        WeaponVisualDatabase.Register(weaponVisualDatabase);
        _lastOnlineServerUrl = PlayerPrefs.GetString(LastOnlineServerPrefsKey, "");
        AuthSession.LoadFromPrefs(apiBaseUrl);
        ApplyServerUrl(string.IsNullOrWhiteSpace(AuthSession.CurrentServerUrl)
            ? apiBaseUrl
            : AuthSession.CurrentServerUrl);
        bool validateRestoredSession = AuthSession.IsLoggedIn;
        if (validateRestoredSession)
        {
            _sessionValidationInProgress = true;
            _sessionValidationMessage = "Checking saved session...";
        }

        if (loginPasswordInput != null)
        {
            loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
            loginPasswordInput.ForceLabelUpdate();
        }

        Debug.Log($"[AuthUI] Menu initialized. LoggedIn={AuthSession.IsLoggedIn}, User='{AuthSession.Username}'");

        EnsureMainMenuButtons();
        EnsureAuthScreensUI();
        RefreshSessionUI();
        ShowOnly(mainMenuPanel);
        EnsureProfileUI();
        EnsureMultiplayerPanel();
        EnsureMainSocialPanel();
        RefreshSocialPanelVisibility();

        if (validateRestoredSession)
        {
            StartCoroutine(ValidateRestoredSession());
        }

        if (MultiplayerState.ConsumeReturnToOnlineMenu())
        {
            if (CanUseAuthenticatedSession() && !IsUsingLocalServer())
                ShowRoomList();
            else
                OpenMultiplayer();
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        SetAuthProof("Auth proof pending. Log in to run checks.");
    }

    private void Update()
    {
        UpdateDailyCoinsCountdown();

        TMP_InputField[] fields = GetActivePanelFields();
        if (fields == null) return;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        bool up    = kb.upArrowKey.wasPressedThisFrame;
        bool down  = kb.downArrowKey.wasPressedThisFrame;
        bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
#else
        bool up    = Input.GetKeyDown(KeyCode.UpArrow);
        bool down  = Input.GetKeyDown(KeyCode.DownArrow);
        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif

        if (up)   NavigateFields(fields, -1);
        if (down)  NavigateFields(fields,  1);
        if (enter) SubmitActivePanel();
    }

    private TMP_InputField[] GetActivePanelFields()
    {
        if (loginPanel != null && loginPanel.activeSelf)
            return new[] { loginUsernameInput, loginPasswordInput };
        if (registerPanel != null && registerPanel.activeSelf)
            return new[] { registerUsernameInput, registerEmailInput, registerPasswordInput };
        return null;
    }

    private void NavigateFields(TMP_InputField[] fields, int direction)
    {
        int current = -1;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null && fields[i].isFocused) { current = i; break; }
        }
        int next = ((current + direction + fields.Length) % fields.Length);
        fields[next]?.Select();
        fields[next]?.ActivateInputField();
    }

    private void SubmitActivePanel()
    {
        if (loginPanel != null && loginPanel.activeSelf) SubmitLogin();
        else if (registerPanel != null && registerPanel.activeSelf) SubmitRegister();
    }

    private void AutoFocusNextFrame(TMP_InputField field)
    {
        if (field != null) StartCoroutine(FocusNextFrame(field));
    }

    private void EnsureMainMenuButtons()
    {
        if (_mainMenuButtonsBuilt || mainMenuPanel == null)
            return;

        _mainMenuButtonsBuilt = true;
        GameUiThemeRuntime.StylePanel(mainMenuPanel, GameUiThemeRuntime.Current.mainMenuBackground, true);

        CreateMainMenuButtonFromTemplate(
            "PlaySPButton", "Play (Singleplayer)",
            new Vector2(0f, -64f), new Vector2(465f, 80f),
            GameUiThemeRuntime.Current.primaryButton,
            PlayGame);

        CreateMainMenuButtonFromTemplate(
            "PlayMPButton", "Play (Multiplayer)",
            new Vector2(0f, -182f), new Vector2(465f, 80f),
            GameUiThemeRuntime.Current.primaryButton,
            OpenMultiplayer);

        CreateMainMenuButtonFromTemplate(
            "QuitButton", "Exit to desktop",
            new Vector2(0f, -300f), new Vector2(465f, 80f),
            GameUiThemeRuntime.Current.dangerButton,
            QuitGame);

        Button profileMainButton = CreateMainMenuButtonFromTemplate(
            "ProfileButton", "Player",
            new Vector2(-976f, -626f), new Vector2(465f, 80f),
            GameUiThemeRuntime.Current.secondaryButton,
            OpenProfile);
        profileButton = profileMainButton.gameObject;
        loginButton = profileButton;

        Button dailyButton = CreateMainMenuButtonFromTemplate(
            "dailyCoinsButton", dailyCoinsLoadingText,
            new Vector2(976f, -626f), new Vector2(465f, 80f),
            GameUiThemeRuntime.Current.successButton,
            null);
        dailyCoinsButton = dailyButton;
        _dailyCoinsButtonText = null;
        _dailyCoinsUiHooked = false;
    }

    private Button CreateMainMenuButtonFromTemplate(
        string name, string fallbackLabel,
        Vector2 fallbackAnchoredPosition, Vector2 fallbackSize,
        Color buttonColor,
        UnityEngine.Events.UnityAction onClick)
    {
        Transform parent = mainMenuPanel.transform;
        Transform template = parent.Find(name);
        RectTransform templateRect = template != null ? template.GetComponent<RectTransform>() : null;
        Image templateImage = template != null ? template.GetComponent<Image>() : null;
        TextMeshProUGUI templateText = template != null ? template.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        int siblingIndex = template != null ? template.GetSiblingIndex() : parent.childCount;
        string label = templateText != null && !string.IsNullOrWhiteSpace(templateText.text)
            ? templateText.text
            : fallbackLabel;

        if (template != null)
            template.gameObject.SetActive(false);

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

        RectTransform rect = obj.AddComponent<RectTransform>();
        if (templateRect != null)
        {
            rect.anchorMin = templateRect.anchorMin;
            rect.anchorMax = templateRect.anchorMax;
            rect.pivot = templateRect.pivot;
            rect.anchoredPosition = templateRect.anchoredPosition;
            rect.sizeDelta = templateRect.sizeDelta;
            rect.localRotation = templateRect.localRotation;
            rect.localScale = templateRect.localScale;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = fallbackAnchoredPosition;
            rect.sizeDelta = fallbackSize;
        }

        Image image = obj.AddComponent<Image>();
        if (templateImage != null)
        {
            image.sprite = templateImage.sprite;
            image.type = templateImage.type;
            image.material = templateImage.material;
            image.preserveAspect = templateImage.preserveAspect;
            image.fillCenter = templateImage.fillCenter;
            image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
        }

        Button button = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, buttonColor);
        button.interactable = true;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI labelText = GetOrCreateButtonLabel(obj.transform);
        labelText.text = label;
        labelText.raycastTarget = false;
        labelText.alignment = TextAlignmentOptions.Center;
        if (templateText != null)
        {
            labelText.font = templateText.font;
            labelText.fontSize = templateText.fontSize;
            labelText.fontStyle = templateText.fontStyle;
            labelText.enableWordWrapping = templateText.enableWordWrapping;
            labelText.overflowMode = templateText.overflowMode;
        }
        else
        {
            labelText.font = TMP_Settings.defaultFontAsset;
            labelText.fontSize = 35f;
            labelText.fontStyle = FontStyles.Normal;
        }
        labelText.color = GameUiThemeRuntime.Current.text;

        return button;
    }

    private IEnumerator FocusNextFrame(TMP_InputField field)
    {
        yield return null;
        field.Select();
        field.ActivateInputField();
    }

    public void OpenRegister()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        Debug.Log("[AuthUI] Open Register panel");
        EnsureAuthScreensUI();
        ShowOnly(registerPanel);
        AutoFocusNextFrame(registerUsernameInput);
    }

    public void OpenLogin()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        _loginReturn = LoginReturn.MainMenu;
        Debug.Log("[AuthUI] Open Login panel");
        EnsureAuthScreensUI();
        ShowOnly(loginPanel);
        AutoFocusNextFrame(loginUsernameInput);
    }

    public void BackToMain()
    {
        Debug.Log("[AuthUI] Back to Main panel");
        _loginReturn = LoginReturn.MainMenu;
        ShowOnly(mainMenuPanel);
    }

    public void OpenProfile()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!CanUseAuthenticatedSession())
        {
            Debug.Log("[AuthUI] Open Profile clicked while logged out. Redirecting to Login panel.");
            _loginReturn = LoginReturn.MainMenu;
            EnsureAuthScreensUI();
            ShowOnly(loginPanel);
            return;
        }

        if (profilePanel == null)
        {
            ShowError("Profile panel is not assigned.");
            return;
        }

        Debug.Log("[AuthUI] Open Profile panel");
        EnsureProfileUI();
        ShowOnly(profilePanel);
    }

    public void OpenStats()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!CanUseAuthenticatedSession())
        {
            ShowError("Please log in first to open stats.");
            return;
        }

        if (statsPanel == null)
        {
            ShowError("Stats panel is not assigned.");
            return;
        }

        Debug.Log("[AuthUI] Open Stats panel");
        ShowOnly(statsPanel);
    }

    public void OpenInventory()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!CanUseAuthenticatedSession())
        {
            ShowError("Please log in first to open inventory.");
            return;
        }

        EnsureInventoryUI();

        if (_inventoryPanel == null || _inventoryPanelController == null)
        {
            ShowError("Inventory panel is not available.");
            return;
        }

        _inventoryPanelController.SetApiClient(_apiClient);
        ShowOnly(_inventoryPanel);
        _inventoryPanelController.LoadInventory(AuthSession.UserId);
        Debug.Log("[AuthUI] Open Inventory panel");
    }

    public void OpenStore()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!CanUseAuthenticatedSession())
        {
            ShowError("Please log in first to open the store.");
            return;
        }

        EnsureShopUI();

        if (_shopPanel == null || _shopPanelController == null)
        {
            ShowError("Shop panel is not available.");
            return;
        }

        _shopPanelController.SetApiClient(_apiClient);
        ShowOnly(_shopPanel);
        _shopPanelController.Open(AuthSession.UserId);
        Debug.Log("[AuthUI] Open Shop panel");
    }

    public void OpenSkillTree()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!CanUseAuthenticatedSession())
        {
            ShowError("Please log in first to open the skill tree.");
            return;
        }

        EnsureSkillTreeUI();

        if (_skillTreePanel == null || _skillTreePanelController == null)
        {
            ShowError("Skill tree panel is not available.");
            return;
        }

        ShowOnly(_skillTreePanel);
        _skillTreePanelController.Open();
        Debug.Log("[AuthUI] Open Skill Tree panel");
    }

    public void BackToProfile()
    {
        if (profilePanel == null)
        {
            ShowError("Profile panel is not assigned.");
            return;
        }

        Debug.Log("[AuthUI] Back to Profile panel");
        EnsureProfileUI();
        ShowOnly(profilePanel);
    }

    public void OpenMultiplayer()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        EnsureMultiplayerPanel();
        RefreshCreateSessionButtonState();
        ShowOnly(_multiplayerPanel);
    }

    public void OpenCreateSession()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (!IsUsingLocalServer() || !CanUseAuthenticatedSession())
        {
            ShowError("Create Session requires a logged-in Local DB session. Use Local DB to log in, or Join Session to connect to a host.");
            return;
        }

        _isHostSession = true;
        _currentLobbyRoomNumber = 0;
        _loginReturn = LoginReturn.OnlineLobby;
        RefreshSessionUI();
        ContinueToLobbyOrLogin();
    }

    public void OpenJoinSession()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession() && !IsUsingLocalServer())
        {
            _isHostSession = false;
            _loginReturn = LoginReturn.OnlineRoomList;
            ShowRoomList();
            return;
        }

        OpenServerAddressPanel(ServerAddressMode.JoinSession);
    }

    public void SubmitJoinSession()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        string url = _joinServerUrlInput != null ? _joinServerUrlInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowError(_serverAddressMode == ServerAddressMode.AuthServer
                ? "Please enter the database server address."
                : "Please enter the host's address.");
            return;
        }
        if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "http://" + url;

        ApplyServerUrl(url);
        _lastOnlineServerUrl = AuthSession.NormalizeServerUrl(url);
        PlayerPrefs.SetString(LastOnlineServerPrefsKey, _lastOnlineServerUrl);
        PlayerPrefs.Save();

        if (_serverAddressMode == ServerAddressMode.AuthServer)
        {
            RefreshSessionUI();

            if (CanUseAuthenticatedSession())
            {
                if (_loginReturn == LoginReturn.OnlineLobby)
                    StartCoroutine(FetchLoadoutAndShowOnlineLobby());
                else if (_loginReturn == LoginReturn.OnlineRoomList)
                    ShowRoomList();
                else
                    ShowOnly(mainMenuPanel);
                return;
            }

            GameObject returnPanel = _serverAddressReturnPanel != null
                ? _serverAddressReturnPanel
                : loginPanel;
            ShowOnly(returnPanel);
            if (returnPanel == loginPanel)
                AutoFocusNextFrame(loginUsernameInput);
            else if (returnPanel == registerPanel)
                AutoFocusNextFrame(registerUsernameInput);
            return;
        }

        _isHostSession = false;
        _currentLobbyRoomNumber = 0;
        _loginReturn = LoginReturn.OnlineRoomList;
        RefreshSessionUI();
        ContinueToLobbyOrLogin();
    }

    private void ContinueToLobbyOrLogin()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            if (_loginReturn == LoginReturn.OnlineRoomList)
                ShowRoomList();
            else
                StartCoroutine(FetchLoadoutAndShowOnlineLobby());
            return;
        }

        EnsureAuthScreensUI();
        ShowOnly(loginPanel);
        AutoFocusNextFrame(loginUsernameInput);
    }

    private void ApplyServerUrl(string url)
    {
        _currentServerUrl = AuthSession.NormalizeServerUrl(url);
        AuthSession.SwitchServer(_currentServerUrl);
        _apiClient = new AuthApiClient(_currentServerUrl);
        GameStatsTracker.SetApiBaseUrl(_currentServerUrl);
        if (_inventoryPanelController != null) _inventoryPanelController.Initialize(_apiClient, BackToProfile);
        if (_shopPanelController != null) _shopPanelController.Initialize(_apiClient, BackToProfile);
        if (_skillTreePanelController != null) _skillTreePanelController.Initialize(_apiClient, BackToProfile);
        if (_mainSocialPanel != null) _mainSocialPanel.SetApiClient(_apiClient);
        if (_lobbySocialPanel != null) _lobbySocialPanel.SetApiClient(_apiClient);
        RefreshAuthServerIndicators();
        Debug.Log($"[AuthUI] Server set to: {_currentServerUrl}");
    }

    private bool IsUsingLocalServer()
    {
        return string.Equals(
            AuthSession.NormalizeServerUrl(_currentServerUrl),
            AuthSession.NormalizeServerUrl(apiBaseUrl),
            StringComparison.OrdinalIgnoreCase);
    }

    private string GetCompactServerUrl(string url)
    {
        string normalized = AuthSession.NormalizeServerUrl(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "unknown";
        }

        const string http = "http://";
        const string https = "https://";
        if (normalized.StartsWith(http, StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(http.Length);
        }
        if (normalized.StartsWith(https, StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(https.Length);
        }
        return normalized;
    }

    private string GetLocalIP()
    {
        try
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
        }
        catch { return "Unknown"; }
    }

    public void PlayGame()
    {
        _launchMultiplayer = false;
        Time.timeScale = 1f;
        MultiplayerState.SetMultiplayer(false);
        MultiplayerState.SetOnline(false);
        MultiplayerState.SetHost(false);
        Debug.Log("[AuthUI] Play clicked.");
        OpenMapSelect(mainMenuPanel, _ => StartCoroutine(FetchLoadoutThenPlay()));
    }

    public void PlayLocalMultiplayer()
    {
        _launchMultiplayer = true;
        Time.timeScale = 1f;
        MultiplayerState.SetOnline(false);
        MultiplayerState.SetHost(false);
        Debug.Log("[AuthUI] Local multiplayer clicked.");
        OpenMapSelect(_multiplayerPanel, _ => StartCoroutine(FetchLoadoutThenPlay()));
    }

    private IEnumerator FetchLoadoutThenPlay(bool waitForLoadout = true)
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            yield break;
        }

        if (waitForLoadout && CanUseAuthenticatedSession())
            yield return FetchLoadoutSilently(AuthSession.UserId);

        LaunchGame();
    }

    private bool CanUseAuthenticatedSession()
    {
        return AuthSession.IsLoggedIn && !_sessionValidationInProgress;
    }

    private IEnumerator ValidateRestoredSession()
    {
        int userId = AuthSession.UserId;
        bool success = false;
        long statusCode = 0;
        string error = "";
        AuthUserData user = null;

        yield return _apiClient.ValidateCurrentSession(
            userId,
            onSuccess: data =>
            {
                success = true;
                user = data;
            },
            onError: (code, message) =>
            {
                statusCode = code;
                error = message;
            });

        _sessionValidationInProgress = false;

        if (success)
        {
            if (user != null)
                AuthSession.UpdateProfile(user);

            _sessionValidationMessage = "";
            Debug.Log($"[AuthUI] Restored session validated for userId={userId}.");
            RefreshSessionUI();
            StartCoroutine(FetchLoadoutSilently(AuthSession.UserId));
            yield break;
        }

        Debug.LogWarning($"[AuthUI] Restored session validation failed. status={statusCode}, error={error}");
        AuthSession.Logout();
        PlayerLoadout.Apply(null, null, null);
        PlayerSkillLoadout.ApplyServerState(null);
        _sessionValidationMessage = "";
        RefreshSessionUI();
        ShowOnly(mainMenuPanel);

        if (statusCode == 401 || statusCode == 403)
        {
            ShowError("Your saved login expired or was replaced. Please log in again.");
            yield break;
        }

        ShowError("Could not verify your saved login. Please log in again.");
    }

    private void LaunchGame()
    {
        MultiplayerState.SetMultiplayer(_launchMultiplayer);

        if (gamePrefab != null)
        {
            if (_gameInstance == null)
            {
                _gameInstance = Instantiate(gamePrefab, gameParent);
                _gameInstance.SetActive(true);
                Debug.Log("[AuthUI] Game prefab instantiated.");
            }
            else
            {
                Debug.Log("[AuthUI] Game prefab already instantiated. Skipping duplicate.");
            }

            if (menuRoot != null)
                Destroy(menuRoot);
            else
                Destroy(gameObject);

            return;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning($"[AuthUI] gamePrefab is not assigned. Falling back to loading scene '{gameplaySceneName}'.");
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        ShowError("Game prefab is not assigned.");
    }

    public void SubmitRegister()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        string username = registerUsernameInput != null ? registerUsernameInput.text.Trim() : "";
        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please complete username, email and password.");
            return;
        }

        if (!Regex.IsMatch(email, @"^[\w\-\.]+@([\w\-]+\.)+[\w\-]{2,4}$"))
        {
            ShowError("Please enter a valid email address.");
            return;
        }

        Debug.Log($"[AuthUI] Submitting register for '{username}' / '{email}'");

        StartCoroutine(_apiClient.Register(username, email, password,
            onSuccess: user =>
            {
                Debug.Log($"[AuthUI] Register success for '{user.username}'. Requesting JWT via login.");

                StartCoroutine(_apiClient.Login(username, password,
                    onSuccess: loginData =>
                    {
                        HandleLoginSuccess(loginData, "Register + login success");
                    },
                    onError: loginError =>
                    {
                        Debug.LogError($"[AuthUI] Auto-login after register failed: {loginError}");
                        ShowError("Registered successfully, but automatic login failed. Please log in manually.");
                        ShowOnly(mainMenuPanel);
                    }));
            },
            onError: error =>
            {
                Debug.LogError($"[AuthUI] Register failed: {error}");
                ShowError(error);
            }));
    }

    public void SubmitLogin()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        string username = loginUsernameInput != null ? loginUsernameInput.text.Trim() : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please complete username and password.");
            return;
        }

        Debug.Log($"[AuthUI] Submitting login for '{username}'");

        StartCoroutine(_apiClient.Login(username, password,
            onSuccess: loginData =>
            {
                HandleLoginSuccess(loginData, "Login success");
            },
            onError: error =>
            {
                Debug.LogError($"[AuthUI] Login failed: {error}");
                ShowError(error);
            }));
    }

    public void SubmitGoogleLogin()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            return;
        }

        if (CanUseAuthenticatedSession())
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        _googleReturnPanel = registerPanel != null && registerPanel.activeSelf
            ? registerPanel
            : loginPanel;
        ClearPendingGoogleLogin();
        EnsureGoogleLoginPanel();

        if (_googleIdTokenInput != null) _googleIdTokenInput.text = "";
        if (_googleUsernameInput != null) _googleUsernameInput.text = "";
        SetGoogleUsernameStep(false, "Opening Google sign-in in your browser...");
        ShowOnly(_googleLoginPanel);

        if (_googleOAuthRoutine != null)
        {
            StopCoroutine(_googleOAuthRoutine);
        }
        _googleOAuthRoutine = StartCoroutine(RunGoogleBrowserLogin());
    }

    private void SubmitGooglePanel()
    {
        bool askingUsername = _googleUsernameInputObj != null && _googleUsernameInputObj.activeSelf;
        string username = askingUsername && _googleUsernameInput != null ? _googleUsernameInput.text.Trim() : "";

        if (!askingUsername)
        {
            SetGoogleStatus("Google sign-in is already open in your browser.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_pendingGoogleSignupToken))
        {
            SetGoogleStatus("Google sign-in expired. Go back and try again.");
            return;
        }

        if (!Regex.IsMatch(username, @"^[A-Za-z0-9_]{3,20}$"))
        {
            SetGoogleStatus("Username must be 3-20 letters, numbers or underscores.");
            AutoFocusNextFrame(_googleUsernameInput);
            return;
        }

        SetGoogleStatus("Creating Google user...");
        StartCoroutine(_apiClient.GoogleLoginWithSignupToken(
            _pendingGoogleSignupToken,
            username,
            onSuccess: loginData =>
            {
                ClearPendingGoogleLogin();
                HandleLoginSuccess(loginData, "Google login success");
            },
            onNeedsUsername: (email, signupToken) =>
            {
                HandleGoogleUsernameRequired(email, signupToken);
            },
            onError: error =>
            {
                SetGoogleStatus(error);
                AutoFocusNextFrame(_googleUsernameInput);
            }));
    }

    private void BackFromGoogleLogin()
    {
        if (_googleOAuthRoutine != null)
        {
            StopCoroutine(_googleOAuthRoutine);
            _googleOAuthRoutine = null;
        }
        ClearPendingGoogleLogin();
        ShowOnly(_googleReturnPanel != null ? _googleReturnPanel : loginPanel);
    }

    private IEnumerator RunGoogleBrowserLogin()
    {
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            SetGoogleStatus("Google client id is not configured in the menu.");
            yield break;
        }

        int port = GetAvailableLoopbackPort();
        string redirectUri = $"http://127.0.0.1:{port}/google-login/";
        string state = Guid.NewGuid().ToString("N");
        string codeVerifier = CreateRandomUrlSafeString(64);
        string codeChallenge = CreatePkceChallenge(codeVerifier);

        TcpListener listener = new TcpListener(IPAddress.Loopback, port);
        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthUI] Could not start Google callback listener: {ex}");
            SetGoogleStatus("Could not listen for the Google browser callback.");
            yield break;
        }

        Task<GoogleOAuthCallback> callbackTask = WaitForGoogleCallbackAsync(listener, state);
        Application.OpenURL(BuildGoogleAuthUrl(redirectUri, state, codeChallenge));
        SetGoogleStatus("Complete Google sign-in in the browser window.");

        float deadline = Time.realtimeSinceStartup + 180f;
        while (!callbackTask.IsCompleted && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (!callbackTask.IsCompleted)
        {
            StopGoogleListener(listener);
            SetGoogleStatus("Google sign-in timed out. Go back and try again.");
            _googleOAuthRoutine = null;
            yield break;
        }

        GoogleOAuthCallback callback;
        try
        {
            callback = callbackTask.Result;
        }
        catch (Exception ex)
        {
            StopGoogleListener(listener);
            Debug.LogError($"[AuthUI] Google callback failed: {ex}");
            SetGoogleStatus("Google sign-in callback failed.");
            _googleOAuthRoutine = null;
            yield break;
        }

        StopGoogleListener(listener);

        if (!string.IsNullOrWhiteSpace(callback.Error))
        {
            SetGoogleStatus("Google sign-in was cancelled.");
            _googleOAuthRoutine = null;
            yield break;
        }

        if (!callback.StateMatches)
        {
            SetGoogleStatus("Google sign-in returned an invalid state. Try again.");
            _googleOAuthRoutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(callback.AuthCode))
        {
            SetGoogleStatus("Google did not return a login code. Try again.");
            _googleOAuthRoutine = null;
            yield break;
        }

        _pendingGoogleAuthCode = callback.AuthCode;
        _pendingGoogleCodeVerifier = codeVerifier;
        _pendingGoogleRedirectUri = redirectUri;
        SetGoogleStatus("Finishing Google login...");

        yield return _apiClient.GoogleLoginWithCode(
            _pendingGoogleAuthCode,
            _pendingGoogleCodeVerifier,
            _pendingGoogleRedirectUri,
            "",
            onSuccess: loginData =>
            {
                ClearPendingGoogleLogin();
                HandleLoginSuccess(loginData, "Google login success");
            },
            onNeedsUsername: (email, signupToken) =>
            {
                HandleGoogleUsernameRequired(email, signupToken);
            },
            onError: loginError =>
            {
                SetGoogleStatus(loginError);
            });

        _googleOAuthRoutine = null;
    }

    private void HandleGoogleUsernameRequired(string email, string signupToken)
    {
        _pendingGoogleAuthCode = "";
        _pendingGoogleCodeVerifier = "";
        _pendingGoogleRedirectUri = "";

        if (string.IsNullOrWhiteSpace(signupToken))
        {
            _pendingGoogleSignupToken = "";
            SetGoogleUsernameStep(false, "Google signup session was not returned. Restart the backend and try again.");
            return;
        }

        _pendingGoogleSignupToken = signupToken;
        SetGoogleUsernameStep(true, string.IsNullOrWhiteSpace(email)
            ? "Choose a username for this Google account."
            : $"Choose a username for {email}.");
        AutoFocusNextFrame(_googleUsernameInput);
    }

    private string BuildGoogleAuthUrl(string redirectUri, string state, string codeChallenge)
    {
        return "https://accounts.google.com/o/oauth2/v2/auth"
            + "?client_id=" + Uri.EscapeDataString(googleClientId.Trim())
            + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
            + "&response_type=code"
            + "&scope=" + Uri.EscapeDataString("openid email profile")
            + "&state=" + Uri.EscapeDataString(state)
            + "&code_challenge=" + Uri.EscapeDataString(codeChallenge)
            + "&code_challenge_method=S256"
            + "&prompt=select_account";
    }

    private int GetAvailableLoopbackPort()
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();
        return port;
    }

    private string CreatePkceChallenge(string codeVerifier)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64Url(hash);
        }
    }

    private string CreateRandomUrlSafeString(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Base64Url(bytes);
    }

    private string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async Task<GoogleOAuthCallback> WaitForGoogleCallbackAsync(TcpListener listener, string expectedState)
    {
        while (true)
        {
            using (TcpClient client = await listener.AcceptTcpClientAsync())
            using (NetworkStream stream = client.GetStream())
            {
                string request = await ReadHttpRequestAsync(stream);
                string target = ExtractHttpRequestTarget(request);
                string error = ExtractQueryValue(target, "error");
                string returnedState = ExtractQueryValue(target, "state");
                string authCode = ExtractQueryValue(target, "code");

                bool hasOAuthResult = !string.IsNullOrWhiteSpace(error)
                    || !string.IsNullOrWhiteSpace(authCode)
                    || !string.IsNullOrWhiteSpace(returnedState);

                if (!hasOAuthResult)
                {
                    Debug.LogWarning($"[AuthUI] Ignoring non-OAuth Google callback request: {target} "
                        + $"parsedState='{returnedState}' parsedCodeLength={authCode.Length} parsedError='{error}'");
                    WriteGoogleBrowserResponse(stream, false, "Still waiting for Google sign-in.");
                    continue;
                }

                bool stateMatches = string.Equals(
                    returnedState.Trim(),
                    expectedState,
                    StringComparison.Ordinal);
                bool success = string.IsNullOrWhiteSpace(error)
                    && stateMatches
                    && !string.IsNullOrWhiteSpace(authCode);

                WriteGoogleBrowserResponse(stream, success, success
                    ? "Google login complete."
                    : "Google login failed.");
                return new GoogleOAuthCallback(authCode, error, stateMatches);
            }
        }
    }

    private async Task<string> ReadHttpRequestAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[4096];
        StringBuilder request = new StringBuilder();

        while (request.Length < 32768)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
                break;

            request.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            string text = request.ToString();
            if (text.Contains("\r\n\r\n") || text.Contains("\n\n"))
                break;
        }

        return request.ToString();
    }

    private string ExtractHttpRequestTarget(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            return "";

        string firstLine = request.Split('\n')[0].Trim();
        string[] parts = firstLine.Split(' ');
        if (parts.Length < 2)
            return "";

        return parts[1];
    }

    private string ExtractQueryValue(string requestTarget, string key)
    {
        if (string.IsNullOrWhiteSpace(requestTarget) || string.IsNullOrWhiteSpace(key))
            return "";

        string markerAfterQuestion = "?" + key + "=";
        string markerAfterAmpersand = "&" + key + "=";
        int start = requestTarget.IndexOf(markerAfterQuestion, StringComparison.Ordinal);
        int valueStartOffset = markerAfterQuestion.Length;

        if (start < 0)
        {
            start = requestTarget.IndexOf(markerAfterAmpersand, StringComparison.Ordinal);
            valueStartOffset = markerAfterAmpersand.Length;
        }

        if (start < 0)
            return "";

        int valueStart = start + valueStartOffset;
        int valueEnd = requestTarget.IndexOf('&', valueStart);
        if (valueEnd < 0)
            valueEnd = requestTarget.IndexOf(' ', valueStart);
        if (valueEnd < 0)
            valueEnd = requestTarget.Length;

        string rawValue = requestTarget.Substring(valueStart, Math.Max(0, valueEnd - valueStart));
        return UrlDecode(rawValue);
    }

    private string UrlDecode(string value)
    {
        return Uri.UnescapeDataString((value ?? "").Replace("+", " "));
    }

    private void WriteGoogleBrowserResponse(NetworkStream stream, bool success, string title)
    {
        string body = success
            ? "<html><body><h2>" + title + "</h2><p>You can return to the game.</p></body></html>"
            : "<html><body><h2>" + title + "</h2><p>Return to the game and try again.</p></body></html>";
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        string headers = "HTTP/1.1 200 OK\r\n"
            + "Content-Type: text/html; charset=utf-8\r\n"
            + "Content-Length: " + bytes.Length + "\r\n"
            + "Connection: close\r\n\r\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void StopGoogleListener(TcpListener listener)
    {
        try
        {
            listener.Stop();
        }
        catch
        {
            // The listener is best-effort; shutting down twice is harmless.
        }
    }

    private void ClearPendingGoogleLogin()
    {
        _pendingGoogleAuthCode = "";
        _pendingGoogleCodeVerifier = "";
        _pendingGoogleRedirectUri = "";
        _pendingGoogleSignupToken = "";
    }

    private struct GoogleOAuthCallback
    {
        public readonly string AuthCode;
        public readonly string Error;
        public readonly bool StateMatches;

        public GoogleOAuthCallback(string authCode, string error, bool stateMatches)
        {
            AuthCode = authCode;
            Error = error;
            StateMatches = stateMatches;
        }
    }

    public void Logout()
    {
        if (!AuthSession.IsLoggedIn)
        {
            Debug.Log("[AuthUI] Logout clicked but no user is logged in.");
            return;
        }

        Debug.Log($"[AuthUI] Logout clicked by '{AuthSession.Username}'");
        AuthSession.Logout();
        RefreshSessionUI();

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        ShowOnly(mainMenuPanel);
    }

    public void QuitGame()
    {
        Debug.Log("[AuthUI] Quit clicked.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CloseError()
    {
        Debug.Log("[AuthUI] Close error panel");
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    private IEnumerator FetchLoadoutSilently(int userId)
    {
        yield return _apiClient.GetInventory(userId,
            onSuccess: data =>
            {
                PlayerLoadout.ApplyFromItems(data?.items);
                Debug.Log("[AuthUI] Loadout applied from inventory.");
            },
            onError: err =>
            {
                Debug.LogWarning($"[AuthUI] Silent inventory fetch failed: {err}");
            });

        yield return _apiClient.GetSkins(userId,
            onSuccess: data =>
            {
                PlayerLoadout.ApplySkin(data?.skins);
                Debug.Log("[AuthUI] Skin applied from profile.");
            },
            onError: err =>
            {
                Debug.LogWarning($"[AuthUI] Silent skin fetch failed: {err}");
            });

        yield return _apiClient.GetSkillTree(userId,
            onSuccess: data =>
            {
                PlayerSkillLoadout.ApplyServerState(data);
                Debug.Log("[AuthUI] Skill loadout applied from profile.");
            },
            onError: err =>
            {
                Debug.LogWarning($"[AuthUI] Silent skill fetch failed: {err}");
            });
    }

    private void ShowError(string message)
    {
        Debug.LogError($"[AuthUI] Error shown to user: {message}");
        EnsureErrorUI();
        if (errorText != null) errorText.text = message;
        if (errorPanel != null) errorPanel.SetActive(true);
    }

    private void EnsureErrorUI()
    {
        if (errorPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        errorPanel = new GameObject("ErrorPanel");
        errorPanel.transform.SetParent(panelParent, false);
        errorPanel.transform.SetAsLastSibling();

        RectTransform bg = errorPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;

        UnityEngine.UI.Image overlay = errorPanel.AddComponent<UnityEngine.UI.Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.55f);
        overlay.raycastTarget = true;

        GameObject card = new GameObject("Card");
        card.transform.SetParent(errorPanel.transform, false);

        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot     = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(500f, 220f);
        cardRect.anchoredPosition = Vector2.zero;

        GameUiThemeRuntime.StylePanel(card, GameUiThemeRuntime.Current.panelBackground, true);

        UnityEngine.UI.VerticalLayoutGroup vg = card.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vg.padding = new RectOffset(30, 30, 28, 28);
        vg.spacing = 18f;
        vg.childAlignment = TextAnchor.UpperCenter;
        vg.childControlWidth = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        titleObj.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 36f;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "Error";
        title.font = TMP_Settings.defaultFontAsset;
        title.fontSize = 26f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.35f, 0.35f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(card.transform, false);
        msgObj.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 60f;
        errorText = msgObj.AddComponent<TextMeshProUGUI>();
        errorText.font = TMP_Settings.defaultFontAsset;
        errorText.fontSize = 20f;
        errorText.color = new Color(0.95f, 0.88f, 0.88f, 1f);
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.enableWordWrapping = true;
        errorText.raycastTarget = false;

        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(card.transform, false);
        btnObj.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 44f;

        UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        GameUiThemeRuntime.StyleButton(btn, btnImg, GameUiThemeRuntime.Current.dangerButton);
        btn.onClick.AddListener(CloseError);

        GameObject btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform lr = btnLabel.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTmp = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "Close";
        btnTmp.font = TMP_Settings.defaultFontAsset;
        btnTmp.fontSize = 20f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = GameUiThemeRuntime.Current.text;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.raycastTarget = false;

        errorPanel.SetActive(false);
    }

    private void ShowOnly(GameObject activePanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        if (registerPanel != null) registerPanel.SetActive(activePanel == registerPanel);
        if (loginPanel != null) loginPanel.SetActive(activePanel == loginPanel);
        if (profilePanel != null) profilePanel.SetActive(activePanel == profilePanel);
        if (statsPanel != null) statsPanel.SetActive(activePanel == statsPanel);
        if (_inventoryPanel != null) _inventoryPanel.SetActive(activePanel == _inventoryPanel);
        if (_shopPanel != null) _shopPanel.SetActive(activePanel == _shopPanel);
        if (_skillTreePanel != null) _skillTreePanel.SetActive(activePanel == _skillTreePanel);
        if (_multiplayerPanel != null) _multiplayerPanel.SetActive(activePanel == _multiplayerPanel);
        if (_onlineLobbyPanel != null) _onlineLobbyPanel.SetActive(activePanel == _onlineLobbyPanel);
        if (_roomListPanel != null) _roomListPanel.SetActive(activePanel == _roomListPanel);
        if (_joinSessionPanel != null) _joinSessionPanel.SetActive(activePanel == _joinSessionPanel);
        if (_googleLoginPanel != null) _googleLoginPanel.SetActive(activePanel == _googleLoginPanel);
        if (_mapSelectPanel != null) _mapSelectPanel.SetActive(activePanel == _mapSelectPanel);
        RefreshSocialPanelVisibility();
    }

    private void EnsureMainSocialPanel()
    {
        if (_mainSocialPanel != null || mainMenuPanel == null)
            return;

        GameObject panel = new GameObject("SocialPanel");
        panel.transform.SetParent(mainMenuPanel.transform, false);
        _mainSocialPanel = panel.AddComponent<SocialPanelController>();
        _mainSocialPanel.Initialize(_apiClient, this, false, 0);
        panel.SetActive(false);
    }

    private void EnsureLobbySocialPanel()
    {
        EnsureOnlineLobbyPanel();
        if (_lobbySocialPanel != null || _onlineLobbyPanel == null)
            return;

        GameObject panel = new GameObject("HostSocialPanel");
        panel.transform.SetParent(_onlineLobbyPanel.transform, false);
        _lobbySocialPanel = panel.AddComponent<SocialPanelController>();
        _lobbySocialPanel.Initialize(_apiClient, this, _isHostSession, _currentLobbyRoomNumber);
        panel.SetActive(false);
    }

    private void RefreshSocialPanelVisibility()
    {
        bool loggedIn = CanUseAuthenticatedSession();

        if (_mainSocialPanel != null)
        {
            _mainSocialPanel.SetApiClient(_apiClient);
            _mainSocialPanel.SetContext(false, 0);
            bool showMainSocial = loggedIn && mainMenuPanel != null && mainMenuPanel.activeSelf;
            _mainSocialPanel.gameObject.SetActive(showMainSocial);
            if (showMainSocial)
                _mainSocialPanel.RefreshNow();
        }

        if (_lobbySocialPanel != null)
        {
            _lobbySocialPanel.SetApiClient(_apiClient);
            bool showLobbySocial = loggedIn
                && _isHostSession
                && _onlineLobbyPanel != null
                && _onlineLobbyPanel.activeSelf;
            _lobbySocialPanel.SetContext(showLobbySocial, _currentLobbyRoomNumber);
            _lobbySocialPanel.gameObject.SetActive(showLobbySocial);
            if (showLobbySocial)
                _lobbySocialPanel.RefreshNow();
        }
    }

    public void JoinInvitedOnlineLobby(int roomNumber)
    {
        if (roomNumber <= 0)
        {
            ShowError("Invite did not include a valid room.");
            return;
        }

        if (_lobbyPollRoutine != null)
        {
            StopCoroutine(_lobbyPollRoutine);
            _lobbyPollRoutine = null;
        }

        _isHostSession = false;
        _currentLobbyRoomNumber = roomNumber;
        _loginReturn = LoginReturn.OnlineLobby;
        ContinueToLobbyOrLogin();
    }

    private void RefreshSessionUI()
    {
        bool loggedIn = CanUseAuthenticatedSession();

        if (sessionText != null)
        {
            string serverLabel = IsUsingLocalServer()
                ? "Local DB"
                : $"Online DB: {GetCompactServerUrl(_currentServerUrl)}";

            if (_sessionValidationInProgress)
                sessionText.text = $"{_sessionValidationMessage}  ·  {serverLabel}";
            else
                sessionText.text = loggedIn
                    ? $"Logged in as {AuthSession.Username}  ·  {serverLabel}"
                    : $"Not logged in  ·  {serverLabel}";
        }

        if (registerButton != null) registerButton.SetActive(!loggedIn && !_sessionValidationInProgress);
        if (loginButton != null && loginButton != profileButton) loginButton.SetActive(!loggedIn && !_sessionValidationInProgress);
        if (profileButton != null) profileButton.SetActive(true);
        if (logoutButton != null) logoutButton.SetActive(loggedIn);
        RefreshInventoryButtonState(loggedIn);
        RefreshShopButtonState(loggedIn);
        RefreshStatsCardState(loggedIn);
        RefreshProfileTopButtonState(loggedIn);
        RefreshAuthServerIndicators();
        RefreshDailyCoinsUI();
        RefreshCreateSessionButtonState();
        RefreshSocialPanelVisibility();

        if (!loggedIn)
        {
            SetAuthProof("Auth proof pending. Log in to run checks.");

            if ((_inventoryPanel != null && _inventoryPanel.activeSelf) ||
                (_shopPanel != null && _shopPanel.activeSelf))
            {
                ShowOnly(mainMenuPanel);
            }
        }
    }

    private void RefreshDailyCoinsUI()
    {
        EnsureDailyCoinsUI();

        if (dailyCoinsButton == null)
        {
            return;
        }

        bool loggedIn = CanUseAuthenticatedSession();
        dailyCoinsButton.gameObject.SetActive(loggedIn);
        if (dailyCoinsTimerText != null)
        {
            dailyCoinsTimerText.gameObject.SetActive(false);
        }

        if (!loggedIn)
        {
            _dailyCoinsStatus = null;
            _dailyCoinsStatusServerUrl = "";
            _dailyCoinsClaimInFlight = false;
            dailyCoinsButton.interactable = false;
            return;
        }

        bool currentStatus = _dailyCoinsStatus != null
            && string.Equals(_dailyCoinsStatusServerUrl, _currentServerUrl, StringComparison.OrdinalIgnoreCase);

        if (!currentStatus)
        {
            SetDailyCoinsButtonState(dailyCoinsLoadingText, dailyCoinsClaimedColor, false);
            RequestDailyCoinsStatus();
            return;
        }

        ApplyDailyCoinsStatusVisual();
    }

    private void EnsureDailyCoinsUI()
    {
        if (dailyCoinsButton == null)
        {
            if (dailyCoinsTimerText != null)
            {
                dailyCoinsTimerText.gameObject.SetActive(false);
            }
            return;
        }

        if (_dailyCoinsButtonText == null)
        {
            _dailyCoinsButtonText = dailyCoinsButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (!_dailyCoinsUiHooked)
        {
            _dailyCoinsUiHooked = true;
        dailyCoinsButton.onClick.AddListener(ClaimDailyCoins);
        }
    }

    private void RequestDailyCoinsStatus()
    {
        if (_dailyCoinsStatusRoutine != null || _apiClient == null || !CanUseAuthenticatedSession())
        {
            return;
        }

        _dailyCoinsStatusRoutine = StartCoroutine(LoadDailyCoinsStatus());
    }

    private IEnumerator LoadDailyCoinsStatus()
    {
        DailyCoinsStatusData status = null;
        string error = null;
        yield return _apiClient.GetDailyCoinsStatus(
            data => status = data,
            err => error = err);

        _dailyCoinsStatusRoutine = null;

        if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"[AuthUI] Daily coins status failed: {error}");
            SetDailyCoinsButtonState("Daily unavailable", dailyCoinsClaimedColor, false);
            yield break;
        }

        SetDailyCoinsStatus(status);
    }

    private void ClaimDailyCoins()
    {
        if (_dailyCoinsClaimInFlight || !CanUseAuthenticatedSession() || _apiClient == null)
        {
            return;
        }

        if (_dailyCoinsStatus != null && !_dailyCoinsStatus.claimable && GetDailyCoinsRemainingSeconds() > 0)
        {
            return;
        }

        _dailyCoinsClaimInFlight = true;
        SetDailyCoinsButtonState(dailyCoinsClaimingText, dailyCoinsClaimedColor, false);
        StartCoroutine(ClaimDailyCoinsRoutine());
    }

    private IEnumerator ClaimDailyCoinsRoutine()
    {
        DailyCoinsStatusData status = null;
        string error = null;
        yield return _apiClient.ClaimDailyCoins(
            data => status = data,
            err => error = err);

        _dailyCoinsClaimInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"[AuthUI] Daily coins claim failed: {error}");
            ShowError(error);
            RequestDailyCoinsStatus();
            yield break;
        }

        SetDailyCoinsStatus(status);
    }

    private void SetDailyCoinsStatus(DailyCoinsStatusData status)
    {
        _dailyCoinsStatus = status ?? new DailyCoinsStatusData();
        _dailyCoinsStatusServerUrl = _currentServerUrl;
        _dailyCoinsStatusReceivedAt = Time.realtimeSinceStartup;
        ApplyDailyCoinsStatusVisual();
    }

    private void ApplyDailyCoinsStatusVisual()
    {
        if (dailyCoinsButton == null || _dailyCoinsStatus == null || !CanUseAuthenticatedSession())
        {
            return;
        }

        long remaining = GetDailyCoinsRemainingSeconds();
        bool claimable = _dailyCoinsStatus.claimable || remaining <= 0;
        if (claimable)
        {
            SetDailyCoinsButtonState(dailyCoinsClaimableText, dailyCoinsClaimableColor, true);
            if (dailyCoinsTimerText != null)
            {
                dailyCoinsTimerText.gameObject.SetActive(false);
            }
            return;
        }

        SetDailyCoinsButtonState(dailyCoinsClaimedText, dailyCoinsClaimedColor, false);
        if (dailyCoinsTimerText != null)
        {
            dailyCoinsTimerText.gameObject.SetActive(true);
            dailyCoinsTimerText.text = FormatDailyCoinsTime(remaining);
        }
    }

    private void UpdateDailyCoinsCountdown()
    {
        if (!CanUseAuthenticatedSession() || _dailyCoinsStatus == null || dailyCoinsTimerText == null)
        {
            return;
        }

        if (!string.Equals(_dailyCoinsStatusServerUrl, _currentServerUrl, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_dailyCoinsStatus.claimable)
        {
            return;
        }

        long remaining = GetDailyCoinsRemainingSeconds();
        if (remaining <= 0)
        {
            _dailyCoinsStatus.claimable = true;
            _dailyCoinsStatus.remainingSeconds = 0;
            ApplyDailyCoinsStatusVisual();
            return;
        }

        if (dailyCoinsTimerText.gameObject.activeSelf)
        {
            dailyCoinsTimerText.text = FormatDailyCoinsTime(remaining);
        }
    }

    private long GetDailyCoinsRemainingSeconds()
    {
        if (_dailyCoinsStatus == null)
        {
            return 0;
        }

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _dailyCoinsStatusReceivedAt);
        return Math.Max(0L, _dailyCoinsStatus.remainingSeconds - Mathf.FloorToInt(elapsed));
    }

    private void SetDailyCoinsButtonState(string label, Color color, bool interactable)
    {
        if (dailyCoinsButton == null)
        {
            return;
        }

        dailyCoinsButton.interactable = interactable;

        Image image = dailyCoinsButton.targetGraphic as Image;
        if (image == null)
        {
            image = dailyCoinsButton.GetComponent<Image>();
        }

        if (image != null)
        {
            GameUiThemeRuntime.StyleButton(dailyCoinsButton, image, color);
        }

        if (_dailyCoinsButtonText != null)
        {
            _dailyCoinsButtonText.text = label;
            _dailyCoinsButtonText.color = GameUiThemeRuntime.Current.text;
        }
    }

    private string FormatDailyCoinsTime(long totalSeconds)
    {
        long safeSeconds = Math.Max(0L, totalSeconds);
        long hours = safeSeconds / 3600;
        long minutes = (safeSeconds % 3600) / 60;
        long seconds = safeSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    private void EnsureAuthScreensUI()
    {
        StyleLoginPanel();
        StyleRegisterPanel();
    }

    private void StyleLoginPanel()
    {
        if (_loginUiStyled || loginPanel == null) return;
        _loginUiStyled = true;

        StyleAuthPanelRoot(loginPanel);
        StyleAuthTitle(loginPanel, "LoginText", "LOGIN", new Vector2(0f, -128f));
        EnsureAuthServerChoice(loginPanel, true, -206f);

        StyleAuthInput(loginUsernameInput, "Username", new Vector2(0f, 82f));
        StyleAuthInput(loginPasswordInput, "Password", new Vector2(0f, -20f));

        StyleAuthButton(loginPanel, "LoginSubmitButton", "Login",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -150f), new Vector2(620f, 64f),
            GameUiThemeRuntime.Current.primaryButton,
            SubmitLogin);

        StyleAuthButton(loginPanel, "GoogleLoginButton", "G  Continue with Google",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -226f), new Vector2(620f, 54f),
            GameUiThemeRuntime.Current.secondaryButton,
            SubmitGoogleLogin);

        StyleAuthButton(loginPanel, "BackButton", "Back",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -16f), new Vector2(130f, 46f),
            GameUiThemeRuntime.Current.secondaryButton,
            BackToMain);
    }

    private void StyleRegisterPanel()
    {
        if (_registerUiStyled || registerPanel == null) return;
        _registerUiStyled = true;

        StyleAuthPanelRoot(registerPanel);
        StyleAuthTitle(registerPanel, "RegisterText", "REGISTER", new Vector2(0f, -104f));
        EnsureAuthServerChoice(registerPanel, false, -184f);

        StyleAuthInput(registerUsernameInput, "Username", new Vector2(0f, 126f));
        StyleAuthInput(registerEmailInput, "Email", new Vector2(0f, 28f));
        StyleAuthInput(registerPasswordInput, "Password", new Vector2(0f, -70f));

        StyleAuthButton(registerPanel, "RegisterSubmitButton", "Register",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -200f), new Vector2(620f, 64f),
            GameUiThemeRuntime.Current.primaryButton,
            SubmitRegister);

        StyleAuthButton(registerPanel, "GoogleRegisterButton", "G  Continue with Google",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -276f), new Vector2(620f, 54f),
            GameUiThemeRuntime.Current.secondaryButton,
            SubmitGoogleLogin);

        StyleAuthButton(registerPanel, "BackButton", "Back",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -16f), new Vector2(130f, 46f),
            GameUiThemeRuntime.Current.secondaryButton,
            OpenLogin);
    }

    private void StyleAuthPanelRoot(GameObject panel)
    {
        RectTransform rect = GetOrAddComponent<RectTransform>(panel);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameUiThemeRuntime.StylePanel(panel, GameUiThemeRuntime.Current.authBackground, true);
    }

    private void StyleAuthTitle(GameObject panel, string objectName, string title, Vector2 position)
    {
        Transform titleTransform = panel.transform.Find(objectName);
        GameObject titleObj;
        if (titleTransform != null)
        {
            titleObj = titleTransform.gameObject;
        }
        else
        {
            titleObj = new GameObject(objectName);
            titleObj.transform.SetParent(panel.transform, false);
        }

        RectTransform rect = GetOrAddComponent<RectTransform>(titleObj);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(620f, 72f);
        rect.anchoredPosition = position;

        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(titleObj);
        text.text = title;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 46f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = GameUiThemeRuntime.Current.text;
        text.raycastTarget = false;
    }

    private void EnsureAuthServerChoice(GameObject panel, bool forLogin, float y)
    {
        (Button localButton, Image localImage) = CreateAuthServerChoiceButton(
            panel, forLogin ? "LoginLocalServerButton" : "RegisterLocalServerButton",
            "Local DB", new Vector2(-142f, y), UseLocalAuthServer);

        (Button onlineButton, Image onlineImage) = CreateAuthServerChoiceButton(
            panel, forLogin ? "LoginOnlineServerButton" : "RegisterOnlineServerButton",
            "Online DB", new Vector2(142f, y), UseOnlineAuthServer);

        TextMeshProUGUI infoText = CreateAuthServerInfo(panel,
            forLogin ? "LoginServerInfo" : "RegisterServerInfo",
            new Vector2(0f, y - 40f));

        if (forLogin)
        {
            _loginLocalServerButton = localButton;
            _loginLocalServerImage = localImage;
            _loginOnlineServerButton = onlineButton;
            _loginOnlineServerImage = onlineImage;
            _loginServerInfoText = infoText;
        }
        else
        {
            _registerLocalServerButton = localButton;
            _registerLocalServerImage = localImage;
            _registerOnlineServerButton = onlineButton;
            _registerOnlineServerImage = onlineImage;
            _registerServerInfoText = infoText;
        }

        RefreshAuthServerIndicators();
    }

    private (Button, Image) CreateAuthServerChoiceButton(
        GameObject panel, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        Transform existing = panel.transform.Find(name);
        GameObject obj;
        bool created = false;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject(name);
            obj.transform.SetParent(panel.transform, false);
            created = true;
        }

        RectTransform rect = GetOrAddComponent<RectTransform>(obj);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(260f, 38f);
        rect.anchoredPosition = position;

        Image image = GetOrAddComponent<Image>(obj);
        Button button = GetOrAddComponent<Button>(obj);
        button.targetGraphic = image;
        if (created)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI text = GetOrCreateButtonLabel(obj.transform);
        text.text = label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return (button, image);
    }

    private TextMeshProUGUI CreateAuthServerInfo(GameObject panel, string name, Vector2 position)
    {
        Transform existing = panel.transform.Find(name);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            obj.transform.SetParent(panel.transform, false);
        }

        RectTransform rect = GetOrAddComponent<RectTransform>(obj);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(620f, 28f);
        rect.anchoredPosition = position;

        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(obj);
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 15f;
        text.fontStyle = FontStyles.Italic;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void UseLocalAuthServer()
    {
        ApplyServerUrl(apiBaseUrl);
        RefreshSessionUI();
    }

    private void UseOnlineAuthServer()
    {
        _serverAddressReturnPanel = registerPanel != null && registerPanel.activeSelf
            ? registerPanel
            : loginPanel;
        OpenServerAddressPanel(ServerAddressMode.AuthServer);
    }

    private void RefreshAuthServerIndicators()
    {
        bool local = IsUsingLocalServer();
        string modeText = local ? "Using local database" : $"Using online database: {GetCompactServerUrl(_currentServerUrl)}";

        RefreshAuthChoiceVisual(_loginLocalServerButton, _loginLocalServerImage, local);
        RefreshAuthChoiceVisual(_loginOnlineServerButton, _loginOnlineServerImage, !local);
        RefreshAuthChoiceVisual(_registerLocalServerButton, _registerLocalServerImage, local);
        RefreshAuthChoiceVisual(_registerOnlineServerButton, _registerOnlineServerImage, !local);

        if (_loginServerInfoText != null)
        {
            _loginServerInfoText.text = modeText;
            _loginServerInfoText.color = local
                ? new Color(0.66f, 0.78f, 0.84f, 0.86f)
                : new Color(0.74f, 0.82f, 1f, 0.90f);
        }

        if (_registerServerInfoText != null)
        {
            _registerServerInfoText.text = modeText;
            _registerServerInfoText.color = local
                ? new Color(0.66f, 0.78f, 0.84f, 0.86f)
                : new Color(0.74f, 0.82f, 1f, 0.90f);
        }
    }

    private void RefreshAuthChoiceVisual(Button button, Image image, bool active)
    {
        if (button == null || image == null) return;

        Color color = active
            ? GameUiThemeRuntime.Current.primaryButton
            : GameUiThemeRuntime.Current.secondaryButton;
        GameUiThemeRuntime.StyleButton(button, image, color);
    }

    private void StyleAuthInput(TMP_InputField input, string placeholder, Vector2 position)
    {
        if (input == null) return;

        RectTransform rect = GetOrAddComponent<RectTransform>(input.gameObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 74f);
        rect.anchoredPosition = position;

        Image background = GetOrAddComponent<Image>(input.gameObject);
        background.color = GameUiThemeRuntime.Current.surface;
        GameUiThemeRuntime.ApplyBorder(input.gameObject);
        input.targetGraphic = background;

        ColorBlock colors = input.colors;
        colors.normalColor = GameUiThemeRuntime.Current.surface;
        colors.highlightedColor = GameUiThemeRuntime.Current.Hover(GameUiThemeRuntime.Current.surface);
        colors.selectedColor = GameUiThemeRuntime.Current.Hover(GameUiThemeRuntime.Current.surface);
        colors.pressedColor = GameUiThemeRuntime.Current.Pressed(GameUiThemeRuntime.Current.surface);
        colors.disabledColor = GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.surface, 0.7f);
        input.colors = colors;
        input.caretColor = new Color(0.82f, 0.92f, 1f, 1f);
        input.selectionColor = new Color(0.20f, 0.50f, 0.62f, 0.55f);

        if (input.textViewport != null)
        {
            input.textViewport.offsetMin = new Vector2(22f, 8f);
            input.textViewport.offsetMax = new Vector2(-22f, -8f);
        }

        if (input.textComponent != null)
        {
            input.textComponent.font = TMP_Settings.defaultFontAsset;
            input.textComponent.fontSize = 28f;
            input.textComponent.color = GameUiThemeRuntime.Current.text;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent.enableWordWrapping = false;
            input.textComponent.raycastTarget = false;
        }

        if (input.placeholder is TextMeshProUGUI placeholderText)
        {
            placeholderText.text = placeholder;
            placeholderText.font = TMP_Settings.defaultFontAsset;
            placeholderText.fontSize = 26f;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = GameUiThemeRuntime.Current.MutedText(0.68f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.raycastTarget = false;
        }
    }

    private void StyleAuthButton(
        GameObject panel, string objectName, string label,
        Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, Color color,
        UnityEngine.Events.UnityAction fallbackAction)
    {
        Transform buttonTransform = panel.transform.Find(objectName);
        GameObject buttonObj;
        bool created = false;
        if (buttonTransform != null)
        {
            buttonObj = buttonTransform.gameObject;
        }
        else
        {
            buttonObj = new GameObject(objectName);
            buttonObj.transform.SetParent(panel.transform, false);
            created = true;
        }

        RectTransform rect = GetOrAddComponent<RectTransform>(buttonObj);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = GetOrAddComponent<Image>(buttonObj);
        Button button = GetOrAddComponent<Button>(buttonObj);
        GameUiThemeRuntime.StyleButton(button, image, color);
        if (created && fallbackAction != null)
        {
            button.onClick.AddListener(fallbackAction);
        }

        TextMeshProUGUI text = GetOrCreateButtonLabel(buttonObj.transform);
        text.text = label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = GameUiThemeRuntime.Current.text;
        text.raycastTarget = false;
    }

    private void EnsureInventoryUI()
    {
        EnsureInventoryButton();
        EnsureInventoryPanel();
    }

    private void EnsureProfileUI()
    {
        if (profilePanel == null) return;

        StyleProfilePanel();
        EnsureSkillTreeUI();
        EnsureStatsCardButton();
        EnsureInventoryUI();
        EnsureShopUI();

        bool loggedIn = CanUseAuthenticatedSession();
        RefreshSkillTreeButtonState(loggedIn);
        RefreshStatsCardState(loggedIn);
        RefreshInventoryButtonState(loggedIn);
        RefreshShopButtonState(loggedIn);
        RefreshProfileTopButtonState(loggedIn);
    }

    private void StyleProfilePanel()
    {
        if (_profileUiStyled || profilePanel == null) return;
        _profileUiStyled = true;

        RectTransform rect = GetOrAddComponent<RectTransform>(profilePanel);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameUiThemeRuntime.StylePanel(profilePanel, GameUiThemeRuntime.Current.profileBackground, true);

        StyleProfileTitle();
        EnsureProfileBackButton();
        EnsureProfileLogoutButton();
    }

    private void StyleProfileTitle()
    {
        Transform titleTransform = profilePanel.transform.Find("ProfileText");
        GameObject titleObj;
        if (titleTransform != null)
        {
            titleObj = titleTransform.gameObject;
        }
        else
        {
            titleObj = new GameObject("ProfileText");
            titleObj.transform.SetParent(profilePanel.transform, false);
        }

        RectTransform titleRect = GetOrAddComponent<RectTransform>(titleObj);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(500f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);

        TextMeshProUGUI titleText = GetOrAddComponent<TextMeshProUGUI>(titleObj);
        titleText.text = "PROFILE";
        titleText.font = TMP_Settings.defaultFontAsset;
        titleText.fontSize = 36f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = GameUiThemeRuntime.Current.text;
        titleText.raycastTarget = false;
    }

    private void EnsureProfileBackButton()
    {
        Transform backTransform = profilePanel.transform.Find("BackButton");
        if (backTransform != null)
        {
            (_profileBackButton, _profileBackButtonImage) = StyleProfileSmallButton(
                backTransform.gameObject, "Back",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(130f, 46f),
                GameUiThemeRuntime.Current.secondaryButton);
            return;
        }

        GameObject backObj = CreateProfileSmallButton(
            "BackButton", "Back",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -16f), new Vector2(130f, 46f),
            GameUiThemeRuntime.Current.secondaryButton,
            BackToMain);
        (_profileBackButton, _profileBackButtonImage) = (backObj.GetComponent<Button>(), backObj.GetComponent<Image>());
    }

    private void EnsureProfileLogoutButton()
    {
        Transform logoutTransform = logoutButton != null
            ? logoutButton.transform
            : profilePanel.transform.Find("MainLogoutButton");

        if (logoutTransform != null)
        {
            logoutButton = logoutTransform.gameObject;
            (_profileLogoutButton, _profileLogoutButtonImage) = StyleProfileSmallButton(
                logoutTransform.gameObject, "Logout",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -16f), new Vector2(150f, 46f),
                GameUiThemeRuntime.Current.dangerButton);
            return;
        }

        GameObject logoutObj = CreateProfileSmallButton(
            "MainLogoutButton", "Logout",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(150f, 46f),
            GameUiThemeRuntime.Current.dangerButton,
            Logout);
        logoutButton = logoutObj;
        (_profileLogoutButton, _profileLogoutButtonImage) = (logoutObj.GetComponent<Button>(), logoutObj.GetComponent<Image>());
    }

    private void EnsureStatsCardButton()
    {
        if (_statsCardButton != null || profilePanel == null) return;

        Transform statsTransform = profilePanel.transform.Find("StatsButton");
        if (statsTransform != null)
        {
            (_statsCardButton, _statsCardImage, _statsCardText) = StyleProfileCardButton(
                statsTransform.gameObject,
                "STATS", "Review your\nmatch records",
                new Vector2(-550f, -134f), new Vector2(480f, 760f),
                GameUiThemeRuntime.Current.primaryButton,
                GameUiThemeRuntime.Current.Hover(GameUiThemeRuntime.Current.primaryButton),
                GameUiThemeRuntime.Current.Pressed(GameUiThemeRuntime.Current.primaryButton));
        }
        else
        {
            (_statsCardButton, _statsCardImage, _statsCardText) = CreateCardButton(
                "StatsButton", profilePanel.transform,
                "STATS", "Review your\nmatch records",
                new Vector2(-550f, -134f), new Vector2(480f, 760f),
                GameUiThemeRuntime.Current.primaryButton,
                GameUiThemeRuntime.Current.Hover(GameUiThemeRuntime.Current.primaryButton),
                GameUiThemeRuntime.Current.Pressed(GameUiThemeRuntime.Current.primaryButton),
                OpenStats);
        }
    }

    private void EnsureSkillTreeUI()
    {
        EnsureSkillTreeButton();
        EnsureSkillTreePanel();
    }

    private void EnsureSkillTreeButton()
    {
        if (_skillTreeButton != null || profilePanel == null) return;

        Color baseColor = GameUiThemeRuntime.Current.secondaryButton;
        Transform skillTreeTransform = profilePanel.transform.Find("SkillTreeButton");
        if (skillTreeTransform != null)
        {
            (_skillTreeButton, _skillTreeButtonImage, _skillTreeButtonText) = StyleProfileWideButton(
                skillTreeTransform.gameObject,
                "SKILL TREE", "Unlock and equip placeholder powers",
                new Vector2(0f, 334f), new Vector2(1580f, 92f),
                baseColor, GameUiThemeRuntime.Current.Hover(baseColor), GameUiThemeRuntime.Current.Pressed(baseColor));
        }
        else
        {
            (_skillTreeButton, _skillTreeButtonImage, _skillTreeButtonText) = CreateWideButton(
                "SkillTreeButton", profilePanel.transform,
                "SKILL TREE", "Unlock and equip placeholder powers",
                new Vector2(0f, 334f), new Vector2(1580f, 92f),
                baseColor, GameUiThemeRuntime.Current.Hover(baseColor), GameUiThemeRuntime.Current.Pressed(baseColor),
                OpenSkillTree);
        }

        RefreshSkillTreeButtonState(CanUseAuthenticatedSession());
    }

    private (Button btn, Image img, TextMeshProUGUI label) StyleProfileWideButton(
        GameObject obj,
        string title, string subtitle,
        Vector2 position, Vector2 size,
        Color baseColor, Color hoverColor, Color pressColor)
    {
        RectTransform rect = GetOrAddComponent<RectTransform>(obj);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image img = GetOrAddComponent<Image>(obj);

        Button btn = GetOrAddComponent<Button>(obj);
        GameUiThemeRuntime.StyleButton(btn, img, baseColor);

        DisableExistingChildren(obj.transform);

        GameObject content = new GameObject("StyledContent");
        content.transform.SetParent(obj.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(32f, 0f);
        contentRect.offsetMax = new Vector2(-32f, 0f);

        HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 360f;
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.font = TMP_Settings.defaultFontAsset;
        titleText.fontSize = 40f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = GameUiThemeRuntime.Current.text;
        titleText.raycastTarget = false;

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(content.transform, false);
        LayoutElement subtitleLayout = subtitleObj.AddComponent<LayoutElement>();
        subtitleLayout.flexibleWidth = 1f;
        TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = subtitle;
        subtitleText.font = TMP_Settings.defaultFontAsset;
        subtitleText.fontSize = 22f;
        subtitleText.fontStyle = FontStyles.Normal;
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.color = GameUiThemeRuntime.Current.MutedText(0.9f);
        subtitleText.enableWordWrapping = false;
        subtitleText.overflowMode = TextOverflowModes.Ellipsis;
        subtitleText.raycastTarget = false;

        return (btn, img, titleText);
    }

    private (Button btn, Image img, TextMeshProUGUI label) CreateWideButton(
        string name, Transform parent,
        string title, string subtitle,
        Vector2 position, Vector2 size,
        Color baseColor, Color hoverColor, Color pressColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        (Button btn, Image img, TextMeshProUGUI label) = StyleProfileWideButton(
            obj, title, subtitle, position, size, baseColor, hoverColor, pressColor);
        btn.onClick.AddListener(onClick);
        return (btn, img, label);
    }

    private (Button btn, Image img, TextMeshProUGUI label) StyleProfileCardButton(
        GameObject obj,
        string title, string subtitle,
        Vector2 position, Vector2 size,
        Color baseColor, Color hoverColor, Color pressColor)
    {
        RectTransform rect = GetOrAddComponent<RectTransform>(obj);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image img = GetOrAddComponent<Image>(obj);

        Button btn = GetOrAddComponent<Button>(obj);
        GameUiThemeRuntime.StyleButton(btn, img, baseColor);

        DisableExistingChildren(obj.transform);

        GameObject content = new GameObject("StyledContent");
        content.transform.SetParent(obj.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 40, 40);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        titleObj.AddComponent<LayoutElement>().preferredHeight = 80f;
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.font = TMP_Settings.defaultFontAsset;
        titleText.fontSize = 52f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = GameUiThemeRuntime.Current.text;
        titleText.raycastTarget = false;

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(content.transform, false);
        subtitleObj.AddComponent<LayoutElement>().preferredHeight = 60f;
        TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = subtitle;
        subtitleText.font = TMP_Settings.defaultFontAsset;
        subtitleText.fontSize = 26f;
        subtitleText.fontStyle = FontStyles.Normal;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = GameUiThemeRuntime.Current.MutedText(0.85f);
        subtitleText.enableWordWrapping = true;
        subtitleText.raycastTarget = false;

        return (btn, img, titleText);
    }

    private (Button btn, Image img) StyleProfileSmallButton(
        GameObject obj, string label,
        Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = GetOrAddComponent<RectTransform>(obj);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = GetOrAddComponent<Image>(obj);
        Button button = GetOrAddComponent<Button>(obj);
        GameUiThemeRuntime.StyleButton(button, image, color);

        TextMeshProUGUI text = GetOrCreateButtonLabel(obj.transform);
        text.text = label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = GameUiThemeRuntime.Current.text;
        text.raycastTarget = false;

        return (button, image);
    }

    private GameObject CreateProfileSmallButton(
        string name, string label,
        Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(profilePanel.transform, false);
        (Button button, _) = StyleProfileSmallButton(obj, label, anchor, pivot, position, size, color);
        button.onClick.AddListener(onClick);
        return obj;
    }

    private TextMeshProUGUI GetOrCreateButtonLabel(Transform buttonTransform)
    {
        TextMeshProUGUI text = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        GameObject labelObj;
        if (text != null)
        {
            labelObj = text.gameObject;
            labelObj.SetActive(true);
        }
        else
        {
            labelObj = new GameObject("Label");
            labelObj.transform.SetParent(buttonTransform, false);
            text = labelObj.AddComponent<TextMeshProUGUI>();
        }

        RectTransform labelRect = GetOrAddComponent<RectTransform>(labelObj);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return text;
    }

    private void DisableExistingChildren(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void RefreshStatsCardState(bool loggedIn)
    {
        if (_statsCardButton == null) return;
        _statsCardButton.gameObject.SetActive(loggedIn);
        _statsCardButton.interactable = loggedIn;
        if (_statsCardImage != null)
            _statsCardImage.color = loggedIn ? GameUiThemeRuntime.Current.primaryButton : GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.primaryButton, 0.85f);
        if (_statsCardText != null)
            _statsCardText.color = loggedIn ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.55f);
    }

    private void RefreshProfileTopButtonState(bool loggedIn)
    {
        if (_profileBackButton != null)
        {
            _profileBackButton.interactable = true;
            if (_profileBackButtonImage != null)
            {
                _profileBackButtonImage.color = GameUiThemeRuntime.Current.secondaryButton;
            }
        }

        if (_profileLogoutButton != null)
        {
            _profileLogoutButton.gameObject.SetActive(loggedIn);
            _profileLogoutButton.interactable = loggedIn;
            if (_profileLogoutButtonImage != null)
            {
                _profileLogoutButtonImage.color = loggedIn
                    ? GameUiThemeRuntime.Current.dangerButton
                    : GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.dangerButton, 0.85f);
            }
        }
    }

    private void EnsureInventoryButton()
    {
        if (_inventoryButton != null || profilePanel == null) return;

        Color baseColor = GameUiThemeRuntime.Current.primaryButton;
        (_inventoryButton, _inventoryButtonImage, _inventoryButtonText) = CreateCardButton(
            "InventoryButton", profilePanel.transform,
            "INVENTORY", "View and equip\nyour items",
            new Vector2(0f, -134f), new Vector2(480f, 760f),
            baseColor, GameUiThemeRuntime.Current.Hover(baseColor), GameUiThemeRuntime.Current.Pressed(baseColor),
            OpenInventory);

        RefreshInventoryButtonState(CanUseAuthenticatedSession());
    }

    private void EnsureInventoryPanel()
    {
        if (_inventoryPanel != null)
        {
            return;
        }

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _inventoryPanel = new GameObject("InventoryPanel");
        _inventoryPanel.transform.SetParent(panelParent, false);

        RectTransform rect = _inventoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _inventoryPanelController = _inventoryPanel.AddComponent<InventoryPanelController>();
        _inventoryPanelController.Initialize(_apiClient, BackToProfile, skinVisualDatabase);
        _inventoryPanel.SetActive(false);
    }

    private void EnsureShopUI()
    {
        EnsureShopButton();
        EnsureShopPanel();
    }

    private void EnsureShopButton()
    {
        if (_shopButton != null || profilePanel == null) return;

        Color baseColor = GameUiThemeRuntime.Current.primaryButton;
        (_shopButton, _shopButtonImage, _shopButtonText) = CreateCardButton(
            "ShopButton", profilePanel.transform,
            "STORE", "Buy new gear\nwith gold coins",
            new Vector2(550f, -134f), new Vector2(480f, 760f),
            baseColor, GameUiThemeRuntime.Current.Hover(baseColor), GameUiThemeRuntime.Current.Pressed(baseColor),
            OpenStore);

        RefreshShopButtonState(CanUseAuthenticatedSession());
    }

    private (Button btn, Image img, TextMeshProUGUI label) CreateCardButton(
        string name, Transform parent,
        string title, string subtitle,
        Vector2 position, Vector2 size,
        Color baseColor, Color hoverColor, Color pressColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image img = obj.AddComponent<Image>();
        Button btn = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, baseColor);
        btn.onClick.AddListener(onClick);

        // Inner layout
        VerticalLayoutGroup vg = obj.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(28, 28, 40, 40);
        vg.spacing = 20f;
        vg.childAlignment       = TextAnchor.MiddleCenter;
        vg.childControlWidth    = true;
        vg.childControlHeight   = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(obj.transform, false);
        titleObj.AddComponent<LayoutElement>().preferredHeight = 80f;
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = title;
        titleTMP.font = TMP_Settings.defaultFontAsset;
        titleTMP.fontSize = 52f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = GameUiThemeRuntime.Current.text;
        titleTMP.raycastTarget = false;

        // Subtitle
        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(obj.transform, false);
        subtitleObj.AddComponent<LayoutElement>().preferredHeight = 60f;
        var subtitleTMP = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleTMP.text = subtitle;
        subtitleTMP.font = TMP_Settings.defaultFontAsset;
        subtitleTMP.fontSize = 26f;
        subtitleTMP.fontStyle = FontStyles.Normal;
        subtitleTMP.alignment = TextAlignmentOptions.Center;
        subtitleTMP.color = GameUiThemeRuntime.Current.MutedText(0.85f);
        subtitleTMP.enableWordWrapping = true;
        subtitleTMP.raycastTarget = false;

        return (btn, img, titleTMP);
    }

    private void EnsureShopPanel()
    {
        if (_shopPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _shopPanel = new GameObject("ShopPanel");
        _shopPanel.transform.SetParent(panelParent, false);

        RectTransform rect = _shopPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _shopPanelController = _shopPanel.AddComponent<ShopPanelController>();
        _shopPanelController.Initialize(_apiClient, BackToProfile, skinVisualDatabase);
        _shopPanel.SetActive(false);
    }

    private void EnsureSkillTreePanel()
    {
        if (_skillTreePanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _skillTreePanel = new GameObject("SkillTreePanel");
        _skillTreePanel.transform.SetParent(panelParent, false);

        RectTransform rect = _skillTreePanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _skillTreePanelController = _skillTreePanel.AddComponent<SkillTreePanelController>();
        _skillTreePanelController.Initialize(_apiClient, BackToProfile);
        _skillTreePanel.SetActive(false);
    }

    private void RefreshShopButtonState(bool loggedIn)
    {
        if (_shopButton == null) return;
        _shopButton.gameObject.SetActive(loggedIn);
        _shopButton.interactable = loggedIn;
        if (_shopButtonImage != null)
            _shopButtonImage.color = loggedIn ? GameUiThemeRuntime.Current.primaryButton : GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.primaryButton, 0.85f);
        if (_shopButtonText != null)
            _shopButtonText.color = loggedIn ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.55f);
    }

    private void RefreshSkillTreeButtonState(bool loggedIn)
    {
        if (_skillTreeButton == null) return;
        _skillTreeButton.gameObject.SetActive(loggedIn);
        _skillTreeButton.interactable = loggedIn;
        if (_skillTreeButtonImage != null)
            _skillTreeButtonImage.color = loggedIn ? GameUiThemeRuntime.Current.secondaryButton : GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.secondaryButton, 0.85f);
        if (_skillTreeButtonText != null)
            _skillTreeButtonText.color = loggedIn ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.55f);
    }

    private void RefreshInventoryButtonState(bool loggedIn)
    {
        if (_inventoryButton == null) return;
        _inventoryButton.gameObject.SetActive(loggedIn);
        _inventoryButton.interactable = loggedIn;
        if (_inventoryButtonImage != null)
            _inventoryButtonImage.color = loggedIn ? GameUiThemeRuntime.Current.primaryButton : GameUiThemeRuntime.Current.Disabled(GameUiThemeRuntime.Current.primaryButton, 0.85f);
        if (_inventoryButtonText != null)
            _inventoryButtonText.color = loggedIn ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.55f);
    }

    private void HandleLoginSuccess(AuthUserData loginData, string source)
    {
        AuthSession.SetLoggedIn(loginData);

        if (!AuthSession.IsLoggedIn)
        {
            ShowError("Login failed: token was not returned by server.");
            return;
        }

        RefreshSessionUI();
        Debug.Log($"[AuthUI] {source}. Logged as '{AuthSession.Username}' (id={AuthSession.UserId})");

        bool goToLobby = _loginReturn == LoginReturn.OnlineLobby;
        bool goToRoomList = _loginReturn == LoginReturn.OnlineRoomList;
        _loginReturn = LoginReturn.MainMenu;

        StartCoroutine(_apiClient.GetUserById(AuthSession.UserId,
            onSuccess: profile =>
            {
                AuthSession.UpdateProfile(profile);
                RefreshSessionUI();
                Debug.Log($"[AuthUI] Profile refreshed for '{AuthSession.Username}'.");
                StartCoroutine(RunAuthProofChecks());
                if (goToLobby)
                    StartCoroutine(FetchLoadoutAndShowOnlineLobby());
                else if (goToRoomList)
                    ShowRoomList();
                else
                {
                    StartCoroutine(FetchLoadoutSilently(AuthSession.UserId));
                    ShowOnly(mainMenuPanel);
                }
            },
            onError: profileError =>
            {
                Debug.LogWarning($"[AuthUI] Logged in, but profile refresh failed: {profileError}");
                StartCoroutine(RunAuthProofChecks());
                if (goToLobby)
                    StartCoroutine(FetchLoadoutAndShowOnlineLobby());
                else if (goToRoomList)
                    ShowRoomList();
                else
                {
                    StartCoroutine(FetchLoadoutSilently(AuthSession.UserId));
                    ShowOnly(mainMenuPanel);
                }
            }));
    }

    private IEnumerator FetchLoadoutAndShowOnlineLobby()
    {
        if (_sessionValidationInProgress)
        {
            ShowError("Still checking your saved login. Please try again in a moment.");
            yield break;
        }

        if (_isHostSession && _currentLobbyRoomNumber <= 0)
        {
            bool created = false;
            string createError = null;
            yield return _apiClient.LobbyCreate(
                _lobbyClientId,
                onSuccess: room =>
                {
                    _currentLobbyRoomNumber = room != null ? room.roomNumber : 0;
                    created = _currentLobbyRoomNumber > 0;
                },
                onError: error => createError = error);

            if (!created)
            {
                RefreshSessionUI();
                ShowError(string.IsNullOrWhiteSpace(createError) ? "Could not create lobby room." : createError);
                OpenMultiplayer();
                yield break;
            }
        }

        if (!_isHostSession && _currentLobbyRoomNumber <= 0)
        {
            ShowRoomList();
            yield break;
        }

        if (CanUseAuthenticatedSession())
            yield return FetchLoadoutSilently(AuthSession.UserId);

        ShowOnlineLobby();
    }

    private void ShowOnlineLobby()
    {
        EnsureOnlineLobbyPanel();

        RefreshMyLobbyLoadoutDisplay();

        if (_hostAddressText != null)
        {
            if (_isHostSession)
            {
                string ip = GetLocalIP();
                _hostAddressText.text = $"Room #{_currentLobbyRoomNumber}\nYour address:\nhttp://{ip}:8080";
                _hostAddressText.gameObject.SetActive(true);
            }
            else
            {
                _hostAddressText.text = $"Room #{_currentLobbyRoomNumber}";
                _hostAddressText.gameObject.SetActive(true);
            }
        }

        SetOtherPlayerWaiting();

        _lobbyPlayerCount = 1;
        RefreshLobbyStartButtonState();
        if (_lobbyWaitingText != null) _lobbyWaitingText.gameObject.SetActive(!_isHostSession);

        ShowOnly(_onlineLobbyPanel);
        EnsureLobbySocialPanel();
        RefreshSocialPanelVisibility();

        if (_lobbyPollRoutine != null) StopCoroutine(_lobbyPollRoutine);
        _lobbyPollRoutine = StartCoroutine(LobbyPollLoop());
    }

    private void SetOtherPlayerWaiting()
    {
        if (_otherNameText   != null) _otherNameText.text   = "Waiting...";
        if (_otherWeaponText != null) _otherWeaponText.text = "-";
        if (_otherArmorText  != null) _otherArmorText.text  = "-";
        if (_otherItemText   != null) _otherItemText.text   = "-";
    }

    private void RefreshMyLobbyLoadoutDisplay()
    {
        if (_myNameText   != null) _myNameText.text   = AuthSession.Username;
        if (_myWeaponText != null) _myWeaponText.text = "Weapon: " + GetLobbyItemLabel(PlayerLoadout.EquippedWeapon);
        if (_myArmorText  != null) _myArmorText.text  = "Armor: "  + GetLobbyItemLabel(PlayerLoadout.EquippedArmor);
        if (_myItemText   != null) _myItemText.text   = "Item: "   + GetLobbyItemLabel(PlayerLoadout.EquippedConsumable);
    }

    private string GetLobbyItemLabel(InventoryItemData item)
    {
        string itemName = GetLobbyItemName(item);
        return string.IsNullOrWhiteSpace(itemName) ? "None" : itemName;
    }

    private string GetLobbyItemName(InventoryItemData item)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.itemName)
            ? item.itemName
            : "";
    }

    private IEnumerator LobbyPollLoop()
    {
        while (true)
        {
            RefreshMyLobbyLoadoutDisplay();
            string weapon = GetLobbyItemName(PlayerLoadout.EquippedWeapon);
            string armor  = GetLobbyItemName(PlayerLoadout.EquippedArmor);
            string item   = GetLobbyItemName(PlayerLoadout.EquippedConsumable);

            bool shouldLaunch = false;
            string pingError = null;
            yield return _apiClient.LobbyPing(_currentLobbyRoomNumber, _lobbyClientId, weapon, armor, item, 0f, 0f,
                onSuccess: (players, started) =>
                {
                    _lobbyPlayerCount = CountLobbyPlayers(players);
                    LobbyPlayerData other = null;
                    foreach (LobbyPlayerData p in players)
                    {
                        if (p != null && p.username != AuthSession.Username) { other = p; break; }
                    }
                    if (other != null)
                    {
                        if (_otherNameText   != null) _otherNameText.text   = other.username;
                        if (_otherWeaponText != null) _otherWeaponText.text = "Weapon: " + (string.IsNullOrWhiteSpace(other.weapon) ? "None" : other.weapon);
                        if (_otherArmorText  != null) _otherArmorText.text  = "Armor: "  + (string.IsNullOrWhiteSpace(other.armor)  ? "None" : other.armor);
                        if (_otherItemText   != null) _otherItemText.text   = "Item: "   + (string.IsNullOrWhiteSpace(other.item)   ? "None" : other.item);
                    }
                    else
                    {
                        SetOtherPlayerWaiting();
                    }
                    RefreshLobbyStartButtonState();
                    // Guest auto-launches when host starts the game
                    if (started && !_isHostSession) shouldLaunch = true;
                },
                onError: error => pingError = error);

            if (!string.IsNullOrWhiteSpace(pingError))
            {
                Debug.LogWarning($"[AuthUI] Lobby ping failed: {pingError}");
                RefreshSessionUI();
                ShowError(pingError);
                if (!_isHostSession)
                {
                    _currentLobbyRoomNumber = 0;
                    ShowRoomList();
                    yield break;
                }

                _currentLobbyRoomNumber = 0;
                OpenMultiplayer();
                yield break;
            }
            if (shouldLaunch) { PlayOnlineGame(); yield break; }
            yield return new WaitForSeconds(3f);
        }
    }

    private void PlayOnlineGame()
    {
        if (_lobbyPollRoutine != null) { StopCoroutine(_lobbyPollRoutine); _lobbyPollRoutine = null; }
        MultiplayerState.SetOnline(true);
        MultiplayerState.SetHost(_isHostSession);
        MultiplayerState.SetOnlineRoomNumber(_currentLobbyRoomNumber);
        _launchMultiplayer = true;
        StartCoroutine(FetchLoadoutThenPlay());
    }

    private IEnumerator HostStartGame()
    {
        if (_lobbyPlayerCount < 2)
        {
            RefreshLobbyStartButtonState();
            yield break;
        }

        OpenMapSelect(_onlineLobbyPanel, _ => StartCoroutine(HostStartGameAfterMapSelection()));
        yield break;
    }

    private IEnumerator HostStartGameAfterMapSelection()
    {
        bool started = false;
        string startError = null;
        yield return _apiClient.LobbyStart(
            _currentLobbyRoomNumber,
            _lobbyClientId,
            onSuccess: () => started = true,
            onError: error => startError = error);

        if (!started)
        {
            RefreshSessionUI();
            ShowError(string.IsNullOrWhiteSpace(startError) ? "Could not start lobby." : startError);
            yield break;
        }

        PlayOnlineGame();
    }

    private int CountLobbyPlayers(LobbyPlayerData[] players)
    {
        if (players == null) return 0;

        int count = 0;
        foreach (LobbyPlayerData player in players)
        {
            if (player != null && !string.IsNullOrWhiteSpace(player.username))
                count++;
        }
        return count;
    }

    private void RefreshLobbyStartButtonState()
    {
        if (_lobbyPlayButton == null) return;

        bool visible = _isHostSession;
        bool canStart = visible && _lobbyPlayerCount >= 2;
        _lobbyPlayButton.gameObject.SetActive(visible);
        _lobbyPlayButton.interactable = canStart;

        if (_lobbyPlayButtonText != null)
            _lobbyPlayButtonText.text = canStart ? "Start Game" : "Waiting for Player";

        if (_lobbyPlayButtonImage != null)
            _lobbyPlayButtonImage.color = canStart
                ? new Color(0.15f, 0.42f, 0.25f, 1f)
                : new Color(0.20f, 0.23f, 0.28f, 0.95f);
    }

    public void BackFromLobby()
    {
        if (_lobbyPollRoutine != null) { StopCoroutine(_lobbyPollRoutine); _lobbyPollRoutine = null; }
        if (_currentLobbyRoomNumber > 0) StartCoroutine(_apiClient.LobbyLeave(_currentLobbyRoomNumber, _lobbyClientId));
        _currentLobbyRoomNumber = 0;
        OpenMultiplayer();
    }

    private IEnumerator RunAuthProofChecks()
    {
        if (!CanUseAuthenticatedSession())
        {
            SetAuthProof("Auth proof unavailable: no active session.");
            yield break;
        }

        SetAuthProof("Running auth proof checks...");

        string withTokenResult = "Not run";
        string withoutTokenResult = "Not run";
        string wrongPasswordResult = "Not run";

        yield return _apiClient.GetUserById(AuthSession.UserId,
            onSuccess: _ =>
            {
                withTokenResult = "OK (protected endpoint accepted Bearer token)";
            },
            onError: error =>
            {
                withTokenResult = $"Failed ({error})";
            });

        yield return _apiClient.GetUserByIdWithoutAuth(AuthSession.UserId,
            onSuccess: _ =>
            {
                withoutTokenResult = "Unexpectedly allowed (check backend security rules)";
            },
            onError: error =>
            {
                withoutTokenResult = $"Blocked as expected ({error})";
            });

        yield return _apiClient.Login(AuthSession.Username, "__wrong_password_probe__",
            onSuccess: _ =>
            {
                wrongPasswordResult = "Unexpectedly accepted wrong password";
            },
            onError: error =>
            {
                wrongPasswordResult = $"Rejected as expected ({error})";
            });

        string jwtSummary = BuildJwtSummary(AuthSession.AccessToken);

        SetAuthProof(
            "Authentication proof:\n" +
            $"- With token: {withTokenResult}\n" +
            $"- Without token: {withoutTokenResult}\n" +
            $"- Wrong password login: {wrongPasswordResult}\n" +
            $"{jwtSummary}\n" +
            "- Hashing note: frontend never receives a password field; hashing itself is verified on backend/DB.");
    }

    private string BuildJwtSummary(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "- JWT: missing";
        }

        string[] parts = token.Split('.');
        if (parts.Length < 2)
        {
            return "- JWT: malformed";
        }

        try
        {
            string payloadJson = DecodeBase64Url(parts[1]);
            JwtPayload payload = JsonUtility.FromJson<JwtPayload>(payloadJson);
            if (payload == null)
            {
                return "- JWT payload could not be parsed";
            }

            string expText = payload.exp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(payload.exp).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "unknown";

            string tokenPreview = token.Length > 20
                ? $"{token.Substring(0, 12)}...{token.Substring(token.Length - 8)}"
                : token;

            return $"- JWT: sub={payload.sub}, userId={payload.userId}, exp={expText}, token={tokenPreview}";
        }
        catch (Exception ex)
        {
            return $"- JWT decode failed: {ex.Message}";
        }
    }

    private string DecodeBase64Url(string base64Url)
    {
        string base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private void SetAuthProof(string message)
    {
        if (authProofText != null)
        {
            authProofText.text = message;
        }
        Debug.Log($"[AuthUI] {message.Replace('\n', ' ')}");
    }

    private void ShowRoomList()
    {
        EnsureRoomListPanel();
        ShowOnly(_roomListPanel);
        StartCoroutine(LoadRoomList());
    }

    private IEnumerator LoadRoomList()
    {
        ClearRoomRows();
        SetRoomListStatus($"Loading rooms on {GetCompactServerUrl(_currentServerUrl)}...");

        LobbyRoomData[] rooms = null;
        string error = null;
        yield return _apiClient.GetLobbyRooms(
            onSuccess: data => rooms = data != null ? data.rooms : null,
            onError: err => error = err);

        ClearRoomRows();

        if (!string.IsNullOrWhiteSpace(error))
        {
            RefreshSessionUI();
            SetRoomListStatus(error);
            yield break;
        }

        if (rooms == null || rooms.Length == 0)
        {
            SetRoomListStatus("No active lobbies on this server.");
            yield break;
        }

        SetRoomListStatus($"Active lobbies on {GetCompactServerUrl(_currentServerUrl)}");
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null)
                CreateRoomRow(rooms[i]);
        }
    }

    private void SelectRoom(LobbyRoomData room)
    {
        if (room == null) return;
        if (RoomHasCurrentUser(room))
        {
            ShowError($"This account is already in Room #{room.roomNumber}.");
            return;
        }

        if (room.full || room.playerCount >= Mathf.Max(1, room.maxPlayers))
        {
            ShowError($"Room #{room.roomNumber} is full.");
            return;
        }

        _currentLobbyRoomNumber = room.roomNumber;
        _isHostSession = false;
        _loginReturn = LoginReturn.OnlineLobby;
        ContinueToLobbyOrLogin();
    }

    private bool RoomHasCurrentUser(LobbyRoomData room)
    {
        if (room == null || room.players == null || string.IsNullOrWhiteSpace(AuthSession.Username))
            return false;

        for (int i = 0; i < room.players.Length; i++)
        {
            LobbyPlayerData player = room.players[i];
            if (player != null && string.Equals(player.username, AuthSession.Username, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private void SetRoomListStatus(string text)
    {
        if (_roomListStatusText != null)
            _roomListStatusText.text = text;
    }

    private void ClearRoomRows()
    {
        if (_roomListContent == null) return;
        for (int i = _roomListContent.childCount - 1; i >= 0; i--)
            Destroy(_roomListContent.GetChild(i).gameObject);
    }

    private void EnsureRoomListPanel()
    {
        if (_roomListPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;
        _roomListPanel = new GameObject("RoomListPanel");
        _roomListPanel.transform.SetParent(panelParent, false);

        RectTransform bg = _roomListPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_roomListPanel, GameUiThemeRuntime.Current.multiplayerBackground, true);

        CreateMenuButton("BackBtn", _roomListPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), OpenMultiplayer);

        CreateMenuButton("RefreshBtn", _roomListPanel.transform,
            "Refresh", new Vector2(290f, -40f), new Vector2(170f, 50f),
            new Color(0.14f, 0.22f, 0.42f, 1f), () => StartCoroutine(LoadRoomList()));

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_roomListPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(760f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Active Lobbies";
        titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GameUiThemeRuntime.Current.text;

        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(_roomListPanel.transform, false);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.sizeDelta = new Vector2(900f, 36f);
        statusRect.anchoredPosition = new Vector2(0f, -120f);
        _roomListStatusText = statusObj.AddComponent<TextMeshProUGUI>();
        _roomListStatusText.font = TMP_Settings.defaultFontAsset;
        _roomListStatusText.fontSize = 20f;
        _roomListStatusText.alignment = TextAlignmentOptions.Center;
        _roomListStatusText.color = new Color(0.75f, 0.85f, 1f, 0.9f);

        GameObject listObj = new GameObject("RoomRows");
        listObj.transform.SetParent(_roomListPanel.transform, false);
        RectTransform listRect = listObj.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 1f);
        listRect.anchorMax = new Vector2(0.5f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.sizeDelta = new Vector2(900f, 520f);
        listRect.anchoredPosition = new Vector2(0f, -170f);
        VerticalLayoutGroup layout = listObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        _roomListContent = listObj.transform;

        _roomListPanel.SetActive(false);
    }

    private void CreateRoomRow(LobbyRoomData room)
    {
        GameObject row = new GameObject("RoomRow");
        row.transform.SetParent(_roomListContent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 86f);
        row.AddComponent<LayoutElement>().preferredHeight = 86f;

        Image image = row.AddComponent<Image>();
        bool full = room.full || room.playerCount >= Mathf.Max(1, room.maxPlayers);
        bool currentUserAlreadyInRoom = RoomHasCurrentUser(room);
        bool unavailable = full || currentUserAlreadyInRoom;
        image.color = unavailable
            ? new Color(0.20f, 0.13f, 0.14f, 0.96f)
            : new Color(0.10f, 0.18f, 0.27f, 0.96f);
        GameUiThemeRuntime.ApplyBorder(row);

        Button button = row.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = !unavailable;
        LobbyRoomData captured = room;
        button.onClick.AddListener(() => SelectRoom(captured));

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.color = unavailable ? new Color(1f, 0.72f, 0.72f, 1f) : Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        string status = currentUserAlreadyInRoom ? "   ACCOUNT IN ROOM" : full ? "   FULL" : "";
        label.text = $"Room #{room.roomNumber}   {room.playerCount}/{Mathf.Max(1, room.maxPlayers)}{status}\n{BuildRoomPlayerNames(room)}";
    }

    private string BuildRoomPlayerNames(LobbyRoomData room)
    {
        if (room == null || room.players == null || room.players.Length == 0)
            return "No players";

        var sb = new StringBuilder();
        for (int i = 0; i < room.players.Length; i++)
        {
            LobbyPlayerData player = room.players[i];
            if (player == null || string.IsNullOrWhiteSpace(player.username)) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(player.username);
        }
        return sb.Length > 0 ? sb.ToString() : "No players";
    }

    private void OpenServerAddressPanel(ServerAddressMode mode)
    {
        _serverAddressMode = mode;
        EnsureJoinSessionPanel();
        RefreshServerAddressPanelText();
        if (_joinServerUrlInput != null) _joinServerUrlInput.text = "";
        ShowOnly(_joinSessionPanel);
        AutoFocusNextFrame(_joinServerUrlInput);
    }

    private void RefreshServerAddressPanelText()
    {
        bool authMode = _serverAddressMode == ServerAddressMode.AuthServer;

        if (_joinSessionTitleText != null)
            _joinSessionTitleText.text = authMode ? "Online Database" : "Join Session";
        if (_joinSessionAddressLabelText != null)
            _joinSessionAddressLabelText.text = authMode ? "Database Server Address" : "Host's Server Address";
        if (_joinSessionHintText != null)
            _joinSessionHintText.text = authMode
                ? "Enter the database server address, e.g. http://192.168.1.10:8080"
                : "Copy it from the host's lobby screen, e.g. http://192.168.1.10:8080";
        if (_joinSessionSubmitText != null)
            _joinSessionSubmitText.text = authMode ? "Use DB & Log In" : "Connect & Log In";
    }

    private void BackFromServerAddressPanel()
    {
        if (_serverAddressMode == ServerAddressMode.AuthServer)
        {
            GameObject returnPanel = _serverAddressReturnPanel != null
                ? _serverAddressReturnPanel
                : loginPanel;
            ShowOnly(returnPanel);
            return;
        }

        OpenMultiplayer();
    }

    private void EnsureJoinSessionPanel()
    {
        if (_joinSessionPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _joinSessionPanel = new GameObject("JoinSessionPanel");
        _joinSessionPanel.transform.SetParent(panelParent, false);
        RectTransform bg = _joinSessionPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_joinSessionPanel, GameUiThemeRuntime.Current.multiplayerBackground, true);

        CreateMenuButton("BackBtn", _joinSessionPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), BackFromServerAddressPanel);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_joinSessionPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f); titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        _joinSessionTitleText = titleTmp;
        titleTmp.text = "Join Session"; titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.color = GameUiThemeRuntime.Current.text;

        // Card
        GameObject card = new GameObject("Card");
        card.transform.SetParent(_joinSessionPanel.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f); cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(680f, 280f);
        cardRect.anchoredPosition = new Vector2(0f, 20f);
        GameUiThemeRuntime.StyleSurface(card);
        VerticalLayoutGroup vg = card.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(40, 40, 36, 36); vg.spacing = 20f;
        vg.childAlignment = TextAnchor.UpperCenter;
        vg.childControlWidth = true; vg.childControlHeight = true;
        vg.childForceExpandWidth = true; vg.childForceExpandHeight = false;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(card.transform, false);
        labelObj.AddComponent<LayoutElement>().preferredHeight = 30f;
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        _joinSessionAddressLabelText = labelTmp;
        labelTmp.text = "Host's Server Address";
        labelTmp.font = TMP_Settings.defaultFontAsset; labelTmp.fontSize = 22f;
        labelTmp.color = new Color(0.75f, 0.85f, 1f, 1f);
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.raycastTarget = false;

        // Hint
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(card.transform, false);
        hintObj.AddComponent<LayoutElement>().preferredHeight = 26f;
        TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
        _joinSessionHintText = hintTmp;
        hintTmp.text = "Copy it from the host's lobby screen, e.g. http://192.168.1.10:8080";
        hintTmp.font = TMP_Settings.defaultFontAsset; hintTmp.fontSize = 16f;
        hintTmp.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);
        hintTmp.alignment = TextAlignmentOptions.Left; hintTmp.raycastTarget = false;

        // Input field
        GameObject inputObj = new GameObject("ServerUrlInput");
        inputObj.transform.SetParent(card.transform, false);
        inputObj.AddComponent<LayoutElement>().preferredHeight = 56f;
        GameUiThemeRuntime.StyleSurface(inputObj);
        _joinServerUrlInput = inputObj.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(12f, 4f); taRect.offsetMax = new Vector2(-12f, -4f);
        textArea.AddComponent<RectMask2D>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.font = TMP_Settings.defaultFontAsset; textTmp.fontSize = 22f;
        textTmp.color = GameUiThemeRuntime.Current.text; textTmp.enableWordWrapping = false;

        _joinServerUrlInput.textViewport = taRect;
        _joinServerUrlInput.textComponent = textTmp;
        _joinServerUrlInput.fontAsset = TMP_Settings.defaultFontAsset;
        _joinServerUrlInput.pointSize = 22f;

        // Connect button
        GameObject btnObj = new GameObject("ConnectButton");
        btnObj.transform.SetParent(card.transform, false);
        btnObj.AddComponent<LayoutElement>().preferredHeight = 54f;
        Image btnImg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, btnImg, GameUiThemeRuntime.Current.primaryButton);
        btn.onClick.AddListener(SubmitJoinSession);

        GameObject btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform blr = btnLabel.AddComponent<RectTransform>();
        blr.anchorMin = Vector2.zero; blr.anchorMax = Vector2.one;
        blr.offsetMin = Vector2.zero; blr.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTmp = btnLabel.AddComponent<TextMeshProUGUI>();
        _joinSessionSubmitText = btnTmp;
        btnTmp.text = "Connect & Log In"; btnTmp.font = TMP_Settings.defaultFontAsset;
        btnTmp.fontSize = 22f; btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center; btnTmp.color = GameUiThemeRuntime.Current.text;
        btnTmp.raycastTarget = false;

        _joinSessionPanel.SetActive(false);
    }

    private void EnsureGoogleLoginPanel()
    {
        if (_googleLoginPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _googleLoginPanel = new GameObject("GoogleLoginPanel");
        _googleLoginPanel.transform.SetParent(panelParent, false);
        RectTransform bg = _googleLoginPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_googleLoginPanel, GameUiThemeRuntime.Current.authBackground, true);

        CreateMenuButton("BackBtn", _googleLoginPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), BackFromGoogleLogin);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_googleLoginPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(760f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Google Login";
        titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GameUiThemeRuntime.Current.text;

        GameObject card = new GameObject("Card");
        card.transform.SetParent(_googleLoginPanel.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(720f, 380f);
        cardRect.anchoredPosition = new Vector2(0f, 10f);
        GameUiThemeRuntime.StyleSurface(card);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 34, 34);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(card.transform, false);
        statusObj.AddComponent<LayoutElement>().preferredHeight = 58f;
        _googleStatusText = statusObj.AddComponent<TextMeshProUGUI>();
        _googleStatusText.font = TMP_Settings.defaultFontAsset;
        _googleStatusText.fontSize = 19f;
        _googleStatusText.alignment = TextAlignmentOptions.Center;
        _googleStatusText.color = new Color(0.75f, 0.85f, 1f, 0.92f);
        _googleStatusText.enableWordWrapping = true;
        _googleStatusText.raycastTarget = false;

        _googleIdTokenInput = CreateGoogleInput(card.transform, "GoogleIdTokenInput", "Google ID token");
        _googleUsernameInput = CreateGoogleInput(card.transform, "GoogleUsernameInput", "Choose username");
        _googleUsernameInputObj = _googleUsernameInput != null ? _googleUsernameInput.gameObject : null;

        GameObject btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(card.transform, false);
        btnObj.AddComponent<LayoutElement>().preferredHeight = 56f;
        Image btnImg = btnObj.AddComponent<Image>();
        _googleSubmitButton = btnObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(_googleSubmitButton, btnImg, GameUiThemeRuntime.Current.primaryButton);
        _googleSubmitButton.onClick.AddListener(SubmitGooglePanel);

        GameObject btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform blr = btnLabel.AddComponent<RectTransform>();
        blr.anchorMin = Vector2.zero;
        blr.anchorMax = Vector2.one;
        blr.offsetMin = Vector2.zero;
        blr.offsetMax = Vector2.zero;
        _googleSubmitText = btnLabel.AddComponent<TextMeshProUGUI>();
        _googleSubmitText.text = "Continue";
        _googleSubmitText.font = TMP_Settings.defaultFontAsset;
        _googleSubmitText.fontSize = 23f;
        _googleSubmitText.fontStyle = FontStyles.Bold;
        _googleSubmitText.alignment = TextAlignmentOptions.Center;
        _googleSubmitText.color = GameUiThemeRuntime.Current.text;
        _googleSubmitText.raycastTarget = false;

        _googleLoginPanel.SetActive(false);
        SetGoogleUsernameStep(false, "Sign in with Google to continue.");
    }

    private TMP_InputField CreateGoogleInput(Transform parent, string name, string placeholder)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        inputObj.AddComponent<LayoutElement>().preferredHeight = 58f;
        GameUiThemeRuntime.StyleSurface(inputObj);
        TMP_InputField input = inputObj.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(14f, 5f);
        taRect.offsetMax = new Vector2(-14f, -5f);
        textArea.AddComponent<RectMask2D>();

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.font = TMP_Settings.defaultFontAsset;
        placeholderText.fontSize = 22f;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.62f, 0.70f, 0.78f, 0.82f);
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderText.raycastTarget = false;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.font = TMP_Settings.defaultFontAsset;
        textTmp.fontSize = 22f;
        textTmp.color = GameUiThemeRuntime.Current.text;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.enableWordWrapping = false;
        textTmp.raycastTarget = false;

        input.textViewport = taRect;
        input.textComponent = textTmp;
        input.placeholder = placeholderText;
        input.fontAsset = TMP_Settings.defaultFontAsset;
        input.pointSize = 22f;
        return input;
    }

    private void SetGoogleUsernameStep(bool usernameStep, string status)
    {
        if (_googleIdTokenInput != null)
            _googleIdTokenInput.gameObject.SetActive(false);
        if (_googleUsernameInputObj != null)
            _googleUsernameInputObj.SetActive(usernameStep);
        if (_googleSubmitButton != null)
            _googleSubmitButton.gameObject.SetActive(usernameStep);
        if (_googleSubmitText != null)
            _googleSubmitText.text = usernameStep ? "Create Google User" : "Continue";
        SetGoogleStatus(status);
    }

    private void SetGoogleStatus(string status)
    {
        if (_googleStatusText != null)
            _googleStatusText.text = status ?? "";
    }

    private void OpenMapSelect(GameObject returnPanel, Action<int> onSelected)
    {
        EnsureMapSelectPanel();
        _mapSelectReturnPanel = returnPanel != null ? returnPanel : mainMenuPanel;
        _pendingMapSelection = onSelected;
        ShowOnly(_mapSelectPanel);
    }

    private void BackFromMapSelect()
    {
        Action<int> pending = _pendingMapSelection;
        _pendingMapSelection = null;
        ShowOnly(_mapSelectReturnPanel != null ? _mapSelectReturnPanel : mainMenuPanel);
    }

    private void SelectMap(int index)
    {
        GameMapSelection.Select(index);
        Action<int> pending = _pendingMapSelection;
        _pendingMapSelection = null;
        pending?.Invoke(index);
    }

    private void EnsureMapSelectPanel()
    {
        if (_mapSelectPanel != null) return;

        if (mapSelectOptions == null || mapSelectOptions.Length < 3)
            mapSelectOptions = GameMapSelection.CreateDefaultMapSelectOptions();

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _mapSelectPanel = new GameObject("MapSelectPanel");
        _mapSelectPanel.transform.SetParent(panelParent, false);
        RectTransform bg = _mapSelectPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_mapSelectPanel, GameUiThemeRuntime.Current.mapSelectBackground, true);

        CreateMenuButton("BackBtn", _mapSelectPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), BackFromMapSelect);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_mapSelectPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(760f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -54f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Select Map";
        titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GameUiThemeRuntime.Current.text;
        titleTmp.raycastTarget = false;

        GameObject row = new GameObject("MapRow");
        row.transform.SetParent(_mapSelectPanel.transform, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(980f, 360f);
        rowRect.anchoredPosition = new Vector2(0f, -10f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        int count = Mathf.Min(3, mapSelectOptions.Length);
        for (int i = 0; i < count; i++)
        {
            CreateMapCard(row.transform, mapSelectOptions[i], i);
        }

        _mapSelectPanel.SetActive(false);
    }

    private void CreateMapCard(Transform parent, MapSelectOption option, int index)
    {
        GameObject card = new GameObject("MapCard_" + index);
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(290f, 330f);
        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 290f;
        layoutElement.preferredHeight = 330f;

        Button button = card.AddComponent<Button>();
        Image bg = card.AddComponent<Image>();
        GameUiThemeRuntime.StyleButton(button, bg, GameUiThemeRuntime.Current.surface);
        int capturedIndex = index;
        button.onClick.AddListener(() => SelectMap(capturedIndex));

        GameObject imageObj = new GameObject("Preview");
        imageObj.transform.SetParent(card.transform, false);
        RectTransform imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.pivot = new Vector2(0.5f, 1f);
        imageRect.sizeDelta = new Vector2(238f, 238f);
        imageRect.anchoredPosition = new Vector2(0f, -24f);
        Image preview = imageObj.AddComponent<Image>();
        preview.color = option != null ? option.previewColor : Color.gray;
        preview.sprite = option != null && option.previewTexture != null
            ? Sprite.Create(option.previewTexture, new Rect(0f, 0f, option.previewTexture.width, option.previewTexture.height), new Vector2(0.5f, 0.5f), Mathf.Max(option.previewTexture.width, option.previewTexture.height))
            : SimpleSprite.Square;
        preview.raycastTarget = false;

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.offsetMin = new Vector2(18f, 20f);
        nameRect.offsetMax = new Vector2(-18f, 78f);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = option != null && !string.IsNullOrWhiteSpace(option.mapName)
            ? option.mapName
            : "Map " + (index + 1);
        nameText.font = TMP_Settings.defaultFontAsset;
        nameText.fontSize = 24f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = GameUiThemeRuntime.Current.text;
        nameText.enableWordWrapping = true;
        nameText.raycastTarget = false;
    }

    private void EnsureOnlineLobbyPanel()
    {
        if (_onlineLobbyPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _onlineLobbyPanel = new GameObject("OnlineLobbyPanel");
        _onlineLobbyPanel.transform.SetParent(panelParent, false);
        RectTransform bg = _onlineLobbyPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_onlineLobbyPanel, GameUiThemeRuntime.Current.multiplayerBackground, true);

        // Back button
        CreateMenuButton("BackBtn", _onlineLobbyPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), BackFromLobby);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_onlineLobbyPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f); titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Online Lobby"; titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.color = GameUiThemeRuntime.Current.text;

        // Host address (shown only when hosting)
        GameObject addrObj = new GameObject("HostAddress");
        addrObj.transform.SetParent(_onlineLobbyPanel.transform, false);
        RectTransform addrRect = addrObj.AddComponent<RectTransform>();
        addrRect.anchorMin = new Vector2(0.5f, 1f); addrRect.anchorMax = new Vector2(0.5f, 1f);
        addrRect.pivot = new Vector2(0.5f, 1f);
        addrRect.sizeDelta = new Vector2(700f, 70f);
        addrRect.anchoredPosition = new Vector2(0f, -135f);
        _hostAddressText = addrObj.AddComponent<TextMeshProUGUI>();
        _hostAddressText.font = TMP_Settings.defaultFontAsset;
        _hostAddressText.fontSize = 22f;
        _hostAddressText.alignment = TextAlignmentOptions.Center;
        _hostAddressText.color = new Color(0.6f, 1f, 0.7f, 1f);
        _hostAddressText.enableWordWrapping = true;
        _hostAddressText.raycastTarget = false;
        addrObj.SetActive(false);

        // Two-player card row
        GameObject row = new GameObject("PlayerRow");
        row.transform.SetParent(_onlineLobbyPanel.transform, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f); rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(900f, 340f);
        rowRect.anchoredPosition = new Vector2(0f, 30f);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        // Build left (you) and right (other) cards
        (_myNameText, _myWeaponText, _myArmorText, _myItemText) =
            BuildPlayerCard(row.transform, "YOU", new Color(0.10f, 0.28f, 0.18f, 1f));
        (_otherNameText, _otherWeaponText, _otherArmorText, _otherItemText) =
            BuildPlayerCard(row.transform, "OTHER PLAYER", new Color(0.12f, 0.20f, 0.36f, 1f));

        // Play button (host only)
        GameObject playBtnObj = new GameObject("PlayButton");
        playBtnObj.transform.SetParent(_onlineLobbyPanel.transform, false);
        RectTransform playRect = playBtnObj.AddComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0f); playRect.anchorMax = new Vector2(0.5f, 0f);
        playRect.pivot = new Vector2(0.5f, 0f);
        playRect.sizeDelta = new Vector2(300f, 60f);
        playRect.anchoredPosition = new Vector2(0f, 60f);
        Image playBtnImg = playBtnObj.AddComponent<Image>();
        _lobbyPlayButtonImage = playBtnImg;
        _lobbyPlayButton = playBtnObj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(_lobbyPlayButton, playBtnImg, GameUiThemeRuntime.Current.successButton);
        _lobbyPlayButton.onClick.AddListener(() => StartCoroutine(HostStartGame()));

        GameObject playLabel = new GameObject("Label");
        playLabel.transform.SetParent(playBtnObj.transform, false);
        RectTransform plr = playLabel.AddComponent<RectTransform>();
        plr.anchorMin = Vector2.zero; plr.anchorMax = Vector2.one;
        plr.offsetMin = Vector2.zero; plr.offsetMax = Vector2.zero;
        TextMeshProUGUI playTmp = playLabel.AddComponent<TextMeshProUGUI>();
        _lobbyPlayButtonText = playTmp;
        playTmp.text = "Start Game"; playTmp.font = TMP_Settings.defaultFontAsset;
        playTmp.fontSize = 26f; playTmp.fontStyle = FontStyles.Bold;
        playTmp.alignment = TextAlignmentOptions.Center; playTmp.color = GameUiThemeRuntime.Current.text;
        playTmp.raycastTarget = false;

        // Waiting label (guest only)
        GameObject waitObj = new GameObject("WaitingText");
        waitObj.transform.SetParent(_onlineLobbyPanel.transform, false);
        RectTransform waitRect = waitObj.AddComponent<RectTransform>();
        waitRect.anchorMin = new Vector2(0.5f, 0f); waitRect.anchorMax = new Vector2(0.5f, 0f);
        waitRect.pivot = new Vector2(0.5f, 0f);
        waitRect.sizeDelta = new Vector2(500f, 50f);
        waitRect.anchoredPosition = new Vector2(0f, 68f);
        _lobbyWaitingText = waitObj.AddComponent<TextMeshProUGUI>();
        _lobbyWaitingText.text = "Waiting for host to start...";
        _lobbyWaitingText.font = TMP_Settings.defaultFontAsset;
        _lobbyWaitingText.fontSize = 22f;
        _lobbyWaitingText.alignment = TextAlignmentOptions.Center;
        _lobbyWaitingText.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        _lobbyWaitingText.raycastTarget = false;

        _onlineLobbyPanel.SetActive(false);
    }

    private (TextMeshProUGUI name, TextMeshProUGUI weapon, TextMeshProUGUI armor, TextMeshProUGUI item)
        BuildPlayerCard(Transform parent, string header, Color cardColor)
    {
        GameObject card = new GameObject(header + "Card");
        card.transform.SetParent(parent, false);
        RectTransform cr = card.AddComponent<RectTransform>();
        cr.sizeDelta = new Vector2(420f, 320f);
        card.AddComponent<LayoutElement>().preferredWidth = 420f;
        GameUiThemeRuntime.StylePanel(card, cardColor, true);

        VerticalLayoutGroup vg = card.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(28, 28, 24, 24);
        vg.spacing = 14f;
        vg.childAlignment = TextAnchor.UpperCenter;
        vg.childControlWidth = true; vg.childControlHeight = true;
        vg.childForceExpandWidth = true; vg.childForceExpandHeight = false;

        TextMeshProUGUI MakeLine(string label, float size = 22f, bool bold = false)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(card.transform, false);
            obj.AddComponent<LayoutElement>().preferredHeight = size + 10f;
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = size;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = GameUiThemeRuntime.Current.text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        // Header label (not a data field)
        TextMeshProUGUI hdr = MakeLine(header, 18f);
        hdr.text = header;
        hdr.color = new Color(0.8f, 0.9f, 1f, 0.7f);

        TextMeshProUGUI nameText   = MakeLine("Name",   28f, true);
        TextMeshProUGUI weaponText = MakeLine("Weapon", 20f);
        TextMeshProUGUI armorText  = MakeLine("Armor",  20f);
        TextMeshProUGUI itemText   = MakeLine("Item",   20f);

        // Prefix each line with a label
        weaponText.text = "Weapon: -";
        armorText.text  = "Armor: -";
        itemText.text   = "Item: -";

        return (nameText, weaponText, armorText, itemText);
    }

    private void EnsureMultiplayerPanel()
    {
        if (_multiplayerPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _multiplayerPanel = new GameObject("MultiplayerPanel");
        _multiplayerPanel.transform.SetParent(panelParent, false);

        RectTransform bg = _multiplayerPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        GameUiThemeRuntime.StylePanel(_multiplayerPanel, GameUiThemeRuntime.Current.multiplayerBackground, true);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_multiplayerPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Multiplayer";
        titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 48f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GameUiThemeRuntime.Current.text;

        // Back button
        CreateMenuButton("BackBtn", _multiplayerPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), BackToMain);

        // Button container
        GameObject container = new GameObject("ButtonContainer");
        container.transform.SetParent(_multiplayerPanel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(1260f, 340f);
        containerRect.anchoredPosition = new Vector2(0f, 20f);
        HorizontalLayoutGroup hg = container.AddComponent<HorizontalLayoutGroup>();
        hg.spacing = 40f;
        hg.childAlignment = TextAnchor.MiddleCenter;
        hg.childControlWidth = false;
        hg.childControlHeight = false;
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight = false;

        // Local Multiplayer
        CreateModeCard(container.transform, "LOCAL\nMULTIPLAYER",
            "Same screen,\ntwo controllers",
            new Color(0.12f, 0.38f, 0.22f, 1f),
            new Color(0.15f, 0.48f, 0.28f, 1f),
            new Color(0.08f, 0.26f, 0.15f, 1f),
            interactable: true, PlayLocalMultiplayer);

        // Create online session
        _createSessionButton = CreateModeCard(container.transform, "CREATE\nSESSION",
            "Host a game,\nshare your IP",
            new Color(0.32f, 0.18f, 0.10f, 1f),
            new Color(0.42f, 0.24f, 0.12f, 1f),
            new Color(0.22f, 0.12f, 0.07f, 1f),
            interactable: true, OpenCreateSession);
        if (_createSessionButton != null)
        {
            Transform createCard = _createSessionButton.transform;
            _createSessionTitleText = createCard.Find("Title")?.GetComponent<TextMeshProUGUI>();
            _createSessionSubtitleText = createCard.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        }

        // Join online session
        CreateModeCard(container.transform, "JOIN\nSESSION",
            "Enter host's IP\nto connect",
            new Color(0.14f, 0.22f, 0.42f, 1f),
            new Color(0.18f, 0.28f, 0.54f, 1f),
            new Color(0.10f, 0.15f, 0.30f, 1f),
            interactable: true, OpenJoinSession);

        _multiplayerPanel.SetActive(false);
        RefreshCreateSessionButtonState();
    }

    private Button CreateModeCard(Transform parent, string title, string subtitle,
        Color baseColor, Color hoverColor, Color pressColor,
        bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        GameObject card = new GameObject(title.Replace("\n", "") + "Card");
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380f, 300f);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 380f;
        le.preferredHeight = 300f;

        Image img = card.AddComponent<Image>();
        Button btn = card.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(btn, img, baseColor);
        btn.interactable = interactable;
        if (interactable && onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        VerticalLayoutGroup vg = card.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(24, 24, 36, 36);
        vg.spacing = 16f;
        vg.childAlignment = TextAnchor.MiddleCenter;
        vg.childControlWidth = true;
        vg.childControlHeight = true;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        titleObj.AddComponent<LayoutElement>().preferredHeight = 110f;
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 34f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = interactable ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.55f);
        titleTmp.enableWordWrapping = true;
        titleTmp.raycastTarget = false;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(card.transform, false);
        subObj.AddComponent<LayoutElement>().preferredHeight = 60f;
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.font = TMP_Settings.defaultFontAsset;
        subTmp.fontSize = 22f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = interactable ? GameUiThemeRuntime.Current.MutedText(0.9f) : GameUiThemeRuntime.Current.MutedText(0.45f);
        subTmp.enableWordWrapping = true;
        subTmp.raycastTarget = false;

        return btn;
    }

    private void RefreshCreateSessionButtonState()
    {
        if (_createSessionButton == null) return;
        bool canCreate = CanUseAuthenticatedSession() && IsUsingLocalServer();
        _createSessionButton.interactable = canCreate;
        if (_createSessionTitleText != null)
            _createSessionTitleText.color = canCreate ? GameUiThemeRuntime.Current.text : GameUiThemeRuntime.Current.MutedText(0.6f);
        if (_createSessionSubtitleText != null)
            _createSessionSubtitleText.color = canCreate
                ? GameUiThemeRuntime.Current.MutedText(0.9f)
                : GameUiThemeRuntime.Current.MutedText(0.5f);
    }

    private void CreateMenuButton(string name, Transform parent, string label,
        Vector2 anchoredPos, Vector2 size, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

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
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GameUiThemeRuntime.Current.text;
        tmp.raycastTarget = false;
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    [Serializable]
    private class JwtPayload
    {
        public string sub;
        public long exp;
        public int userId;
    }
}
