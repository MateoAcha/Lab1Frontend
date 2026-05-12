using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;

public class AuthMenuController : MonoBehaviour
{
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

    [Header("Play")]
    public GameObject gamePrefab;
    public Transform gameParent;
    public GameObject menuRoot;

    [Header("Config")]
    public string apiBaseUrl = "http://localhost:8080";
    public string gameplaySceneName = "GameScene";

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
    private GameObject _multiplayerPanel;
    private GameObject _onlineLobbyPanel;
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
    private TextMeshProUGUI _lobbyWaitingText;
    private GameObject _joinSessionPanel;
    private TMP_InputField _joinServerUrlInput;
    private bool _launchMultiplayer;

    private enum LoginReturn { MainMenu, OnlineLobby }
    private LoginReturn _loginReturn = LoginReturn.MainMenu;

    private void OnApplicationQuit()
    {
        AuthSession.Logout();
    }

    private void Start()
    {
        _apiClient = new AuthApiClient(apiBaseUrl);
        GameStatsTracker.SetApiBaseUrl(apiBaseUrl);
        AuthSession.LoadFromPrefs();

        if (loginPasswordInput != null)
        {
            loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
            loginPasswordInput.ForceLabelUpdate();
        }

        Debug.Log($"[AuthUI] Menu initialized. LoggedIn={AuthSession.IsLoggedIn}, User='{AuthSession.Username}'");

        RefreshSessionUI();
        ShowOnly(mainMenuPanel);
        EnsureInventoryUI();
        EnsureShopUI();
        EnsureMultiplayerPanel();

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        SetAuthProof("Auth proof pending. Log in to run checks.");
    }

    private void Update()
    {
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

    private IEnumerator FocusNextFrame(TMP_InputField field)
    {
        yield return null;
        field.Select();
        field.ActivateInputField();
    }

    public void OpenRegister()
    {
        if (AuthSession.IsLoggedIn)
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        Debug.Log("[AuthUI] Open Register panel");
        ShowOnly(registerPanel);
        AutoFocusNextFrame(registerUsernameInput);
    }

    public void OpenLogin()
    {
        if (AuthSession.IsLoggedIn)
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        _loginReturn = LoginReturn.MainMenu;
        Debug.Log("[AuthUI] Open Login panel");
        ShowOnly(loginPanel);
        AutoFocusNextFrame(loginUsernameInput);
    }

    public void BackToMain()
    {
        Debug.Log("[AuthUI] Back to Main panel");
        ShowOnly(mainMenuPanel);
    }

    public void OpenProfile()
    {
        if (!AuthSession.IsLoggedIn)
        {
            Debug.Log("[AuthUI] Open Profile clicked while logged out. Redirecting to Login panel.");
            ShowOnly(loginPanel);
            return;
        }

        if (profilePanel == null)
        {
            ShowError("Profile panel is not assigned.");
            return;
        }

        Debug.Log("[AuthUI] Open Profile panel");
        ShowOnly(profilePanel);
    }

    public void OpenStats()
    {
        if (!AuthSession.IsLoggedIn)
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
        if (!AuthSession.IsLoggedIn)
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
        if (!AuthSession.IsLoggedIn)
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

    public void BackToProfile()
    {
        if (profilePanel == null)
        {
            ShowError("Profile panel is not assigned.");
            return;
        }

        Debug.Log("[AuthUI] Back to Profile panel");
        ShowOnly(profilePanel);
    }

    public void OpenMultiplayer()
    {
        EnsureMultiplayerPanel();
        ShowOnly(_multiplayerPanel);
    }

    public void OpenCreateSession()
    {
        ApplyServerUrl(apiBaseUrl);
        _isHostSession = true;
        _loginReturn = LoginReturn.OnlineLobby;
        AuthSession.Logout();
        RefreshSessionUI();
        ShowOnly(loginPanel);
        AutoFocusNextFrame(loginUsernameInput);
    }

    public void OpenJoinSession()
    {
        EnsureJoinSessionPanel();
        if (_joinServerUrlInput != null) _joinServerUrlInput.text = "";
        ShowOnly(_joinSessionPanel);
        AutoFocusNextFrame(_joinServerUrlInput);
    }

    public void SubmitJoinSession()
    {
        string url = _joinServerUrlInput != null ? _joinServerUrlInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(url)) { ShowError("Please enter the host's address."); return; }
        if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "http://" + url;

        ApplyServerUrl(url);
        _isHostSession = false;
        _loginReturn = LoginReturn.OnlineLobby;
        AuthSession.Logout();
        RefreshSessionUI();
        ShowOnly(loginPanel);
        AutoFocusNextFrame(loginUsernameInput);
    }

    private void ApplyServerUrl(string url)
    {
        _apiClient = new AuthApiClient(url);
        GameStatsTracker.SetApiBaseUrl(url);
        if (_inventoryPanelController != null) _inventoryPanelController.Initialize(_apiClient, BackToProfile);
        if (_shopPanelController != null) _shopPanelController.Initialize(_apiClient, BackToProfile);
        Debug.Log($"[AuthUI] Server set to: {url}");
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
        Debug.Log("[AuthUI] Play clicked.");
        StartCoroutine(FetchLoadoutThenPlay());
    }

    public void PlayLocalMultiplayer()
    {
        _launchMultiplayer = true;
        Debug.Log("[AuthUI] Local multiplayer clicked.");
        StartCoroutine(FetchLoadoutThenPlay());
    }

    private IEnumerator FetchLoadoutThenPlay()
    {
        if (AuthSession.IsLoggedIn)
            yield return FetchLoadoutSilently(AuthSession.UserId);

        MultiplayerState.SetMultiplayer(_launchMultiplayer);

        if (gamePrefab != null)
        {
            if (_gameInstance == null)
            {
                _gameInstance = Instantiate(gamePrefab, gameParent);
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

            yield break;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning($"[AuthUI] gamePrefab is not assigned. Falling back to loading scene '{gameplaySceneName}'.");
            SceneManager.LoadScene(gameplaySceneName);
            yield break;
        }

        ShowError("Game prefab is not assigned.");
    }

    public void SubmitRegister()
    {
        if (AuthSession.IsLoggedIn)
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
        if (AuthSession.IsLoggedIn)
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

        UnityEngine.UI.Image cardImg = card.AddComponent<UnityEngine.UI.Image>();
        cardImg.color = new Color(0.12f, 0.08f, 0.08f, 0.97f);

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
        btnImg.color = new Color(0.55f, 0.15f, 0.15f, 1f);

        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = btnImg;
        UnityEngine.UI.ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.55f, 0.15f, 0.15f, 1f);
        cb.highlightedColor = new Color(0.70f, 0.20f, 0.20f, 1f);
        cb.pressedColor     = new Color(0.38f, 0.10f, 0.10f, 1f);
        btn.colors = cb;
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
        btnTmp.color = Color.white;
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
        if (_multiplayerPanel != null) _multiplayerPanel.SetActive(activePanel == _multiplayerPanel);
        if (_onlineLobbyPanel != null) _onlineLobbyPanel.SetActive(activePanel == _onlineLobbyPanel);
        if (_joinSessionPanel != null) _joinSessionPanel.SetActive(activePanel == _joinSessionPanel);
    }

    private void RefreshSessionUI()
    {
        bool loggedIn = AuthSession.IsLoggedIn;

        if (sessionText != null)
        {
            sessionText.text = loggedIn
                ? $"Logged in as {AuthSession.Username}"
                : "Not logged in";
        }

        if (registerButton != null) registerButton.SetActive(!loggedIn);
        if (loginButton != null && loginButton != profileButton) loginButton.SetActive(!loggedIn);
        if (profileButton != null) profileButton.SetActive(true);
        if (logoutButton != null) logoutButton.SetActive(loggedIn);
        RefreshInventoryButtonState(loggedIn);
        RefreshShopButtonState(loggedIn);

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

    private void EnsureInventoryUI()
    {
        EnsureInventoryButton();
        EnsureInventoryPanel();
    }

    private void EnsureInventoryButton()
    {
        if (_inventoryButton != null || profilePanel == null) return;

        Color baseColor = new Color(0.10f, 0.34f, 0.42f, 1f);
        (_inventoryButton, _inventoryButtonImage, _inventoryButtonText) = CreateCardButton(
            "InventoryButton", profilePanel.transform,
            "INVENTORY", "View and equip\nyour items",
            new Vector2(100f, -134f), new Vector2(480f, 760f),
            baseColor, new Color(0.13f, 0.42f, 0.52f, 1f), new Color(0.07f, 0.24f, 0.30f, 1f),
            OpenInventory);

        RefreshInventoryButtonState(AuthSession.IsLoggedIn);
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
        _inventoryPanelController.Initialize(_apiClient, BackToProfile);
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

        Color baseColor = new Color(0.32f, 0.14f, 0.44f, 1f);
        (_shopButton, _shopButtonImage, _shopButtonText) = CreateCardButton(
            "ShopButton", profilePanel.transform,
            "STORE", "Buy new gear\nwith gold coins",
            new Vector2(650f, -134f), new Vector2(480f, 760f),
            baseColor, new Color(0.40f, 0.18f, 0.54f, 1f), new Color(0.22f, 0.10f, 0.30f, 1f),
            OpenStore);

        RefreshShopButtonState(AuthSession.IsLoggedIn);
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
        img.color = baseColor;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = baseColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor     = pressColor;
        cb.selectedColor    = hoverColor;
        cb.disabledColor    = new Color(0.15f, 0.15f, 0.17f, 0.85f);
        btn.colors = cb;
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
        titleTMP.color = Color.white;
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
        subtitleTMP.color = new Color(0.85f, 0.90f, 1f, 0.85f);
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
        _shopPanelController.Initialize(_apiClient, BackToProfile);
        _shopPanel.SetActive(false);
    }

    private void RefreshShopButtonState(bool loggedIn)
    {
        if (_shopButton == null) return;
        _shopButton.gameObject.SetActive(loggedIn);
        _shopButton.interactable = loggedIn;
        if (_shopButtonImage != null)
            _shopButtonImage.color = loggedIn ? new Color(0.32f, 0.14f, 0.44f, 1f) : new Color(0.15f, 0.15f, 0.17f, 0.85f);
        if (_shopButtonText != null)
            _shopButtonText.color = loggedIn ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
    }

    private void RefreshInventoryButtonState(bool loggedIn)
    {
        if (_inventoryButton == null) return;
        _inventoryButton.gameObject.SetActive(loggedIn);
        _inventoryButton.interactable = loggedIn;
        if (_inventoryButtonImage != null)
            _inventoryButtonImage.color = loggedIn ? new Color(0.10f, 0.34f, 0.42f, 1f) : new Color(0.15f, 0.15f, 0.17f, 0.85f);
        if (_inventoryButtonText != null)
            _inventoryButtonText.color = loggedIn ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
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
        _loginReturn = LoginReturn.MainMenu;

        StartCoroutine(_apiClient.GetUserById(AuthSession.UserId,
            onSuccess: profile =>
            {
                AuthSession.UpdateProfile(profile);
                RefreshSessionUI();
                Debug.Log($"[AuthUI] Profile refreshed for '{AuthSession.Username}'.");
                StartCoroutine(RunAuthProofChecks());
                StartCoroutine(FetchLoadoutSilently(AuthSession.UserId));
                if (goToLobby) ShowOnlineLobby();
                else ShowOnly(mainMenuPanel);
            },
            onError: profileError =>
            {
                Debug.LogWarning($"[AuthUI] Logged in, but profile refresh failed: {profileError}");
                StartCoroutine(RunAuthProofChecks());
                StartCoroutine(FetchLoadoutSilently(AuthSession.UserId));
                if (goToLobby) ShowOnlineLobby();
                else ShowOnly(mainMenuPanel);
            }));
    }

    private void ShowOnlineLobby()
    {
        EnsureOnlineLobbyPanel();

        if (_myNameText   != null) _myNameText.text   = AuthSession.Username;
        if (_myWeaponText != null) _myWeaponText.text = "Weapon: " + (PlayerLoadout.EquippedWeapon     != null ? PlayerLoadout.EquippedWeapon.itemName     : "None");
        if (_myArmorText  != null) _myArmorText.text  = "Armor: "  + (PlayerLoadout.EquippedArmor      != null ? PlayerLoadout.EquippedArmor.itemName      : "None");
        if (_myItemText   != null) _myItemText.text   = "Item: "   + (PlayerLoadout.EquippedConsumable != null ? PlayerLoadout.EquippedConsumable.itemName : "None");

        if (_hostAddressText != null)
        {
            if (_isHostSession)
            {
                string ip = GetLocalIP();
                _hostAddressText.text = $"Your address:\nhttp://{ip}:8080";
                _hostAddressText.gameObject.SetActive(true);
            }
            else
            {
                _hostAddressText.gameObject.SetActive(false);
            }
        }

        SetOtherPlayerWaiting();

        if (_lobbyPlayButton  != null) _lobbyPlayButton.gameObject.SetActive(_isHostSession);
        if (_lobbyWaitingText != null) _lobbyWaitingText.gameObject.SetActive(!_isHostSession);

        ShowOnly(_onlineLobbyPanel);

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

    private IEnumerator LobbyPollLoop()
    {
        string weapon = PlayerLoadout.EquippedWeapon     != null ? PlayerLoadout.EquippedWeapon.itemName     : "";
        string armor  = PlayerLoadout.EquippedArmor      != null ? PlayerLoadout.EquippedArmor.itemName      : "";
        string item   = PlayerLoadout.EquippedConsumable != null ? PlayerLoadout.EquippedConsumable.itemName : "";

        while (true)
        {
            bool shouldLaunch = false;
            yield return _apiClient.LobbyPing(weapon, armor, item, 0f, 0f,
                onSuccess: (players, started) =>
                {
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
                    // Guest auto-launches when host starts the game
                    if (started && !_isHostSession) shouldLaunch = true;
                },
                onError: _ => { });

            if (shouldLaunch) { PlayOnlineGame(); yield break; }
            yield return new WaitForSeconds(3f);
        }
    }

    private void PlayOnlineGame()
    {
        if (_lobbyPollRoutine != null) { StopCoroutine(_lobbyPollRoutine); _lobbyPollRoutine = null; }
        StartCoroutine(_apiClient.LobbyLeave());
        MultiplayerState.SetOnline(true);
        _launchMultiplayer = false;
        StartCoroutine(FetchLoadoutThenPlay());
    }

    private IEnumerator HostStartGame()
    {
        yield return _apiClient.LobbyStart(onSuccess: () => { }, onError: _ => { });
        PlayOnlineGame();
    }

    public void BackFromLobby()
    {
        if (_lobbyPollRoutine != null) { StopCoroutine(_lobbyPollRoutine); _lobbyPollRoutine = null; }
        StartCoroutine(_apiClient.LobbyLeave());
        OpenMultiplayer();
    }

    private IEnumerator RunAuthProofChecks()
    {
        if (!AuthSession.IsLoggedIn)
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

    private void EnsureJoinSessionPanel()
    {
        if (_joinSessionPanel != null) return;

        Transform panelParent = mainMenuPanel != null ? mainMenuPanel.transform.parent : transform;

        _joinSessionPanel = new GameObject("JoinSessionPanel");
        _joinSessionPanel.transform.SetParent(panelParent, false);
        RectTransform bg = _joinSessionPanel.AddComponent<RectTransform>();
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        _joinSessionPanel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.97f);

        CreateMenuButton("BackBtn", _joinSessionPanel.transform,
            "Back", new Vector2(110f, -40f), new Vector2(160f, 50f),
            new Color(0.18f, 0.22f, 0.3f, 1f), OpenMultiplayer);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_joinSessionPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f); titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Join Session"; titleTmp.font = TMP_Settings.defaultFontAsset;
        titleTmp.fontSize = 44f; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.color = Color.white;

        // Card
        GameObject card = new GameObject("Card");
        card.transform.SetParent(_joinSessionPanel.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f); cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(680f, 280f);
        cardRect.anchoredPosition = new Vector2(0f, 20f);
        card.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f, 1f);
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
        labelTmp.text = "Host's Server Address";
        labelTmp.font = TMP_Settings.defaultFontAsset; labelTmp.fontSize = 22f;
        labelTmp.color = new Color(0.75f, 0.85f, 1f, 1f);
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.raycastTarget = false;

        // Hint
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(card.transform, false);
        hintObj.AddComponent<LayoutElement>().preferredHeight = 26f;
        TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "Copy it from the host's lobby screen, e.g. http://192.168.1.10:8080";
        hintTmp.font = TMP_Settings.defaultFontAsset; hintTmp.fontSize = 16f;
        hintTmp.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);
        hintTmp.alignment = TextAlignmentOptions.Left; hintTmp.raycastTarget = false;

        // Input field
        GameObject inputObj = new GameObject("ServerUrlInput");
        inputObj.transform.SetParent(card.transform, false);
        inputObj.AddComponent<LayoutElement>().preferredHeight = 56f;
        inputObj.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 1f);
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
        textTmp.color = Color.white; textTmp.enableWordWrapping = false;

        _joinServerUrlInput.textViewport = taRect;
        _joinServerUrlInput.textComponent = textTmp;
        _joinServerUrlInput.fontAsset = TMP_Settings.defaultFontAsset;
        _joinServerUrlInput.pointSize = 22f;

        // Connect button
        GameObject btnObj = new GameObject("ConnectButton");
        btnObj.transform.SetParent(card.transform, false);
        btnObj.AddComponent<LayoutElement>().preferredHeight = 54f;
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.14f, 0.22f, 0.42f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.14f, 0.22f, 0.42f, 1f);
        cb.highlightedColor = new Color(0.18f, 0.28f, 0.54f, 1f);
        cb.pressedColor = new Color(0.10f, 0.15f, 0.30f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(SubmitJoinSession);

        GameObject btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform blr = btnLabel.AddComponent<RectTransform>();
        blr.anchorMin = Vector2.zero; blr.anchorMax = Vector2.one;
        blr.offsetMin = Vector2.zero; blr.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTmp = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "Connect & Log In"; btnTmp.font = TMP_Settings.defaultFontAsset;
        btnTmp.fontSize = 22f; btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center; btnTmp.color = Color.white;
        btnTmp.raycastTarget = false;

        _joinSessionPanel.SetActive(false);
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
        _onlineLobbyPanel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.97f);

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
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.color = Color.white;

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
        playBtnImg.color = new Color(0.15f, 0.42f, 0.25f, 1f);
        _lobbyPlayButton = playBtnObj.AddComponent<Button>();
        _lobbyPlayButton.targetGraphic = playBtnImg;
        ColorBlock cb = _lobbyPlayButton.colors;
        cb.normalColor = new Color(0.15f, 0.42f, 0.25f, 1f);
        cb.highlightedColor = new Color(0.20f, 0.52f, 0.32f, 1f);
        cb.pressedColor = new Color(0.10f, 0.30f, 0.18f, 1f);
        _lobbyPlayButton.colors = cb;
        _lobbyPlayButton.onClick.AddListener(() => StartCoroutine(HostStartGame()));

        GameObject playLabel = new GameObject("Label");
        playLabel.transform.SetParent(playBtnObj.transform, false);
        RectTransform plr = playLabel.AddComponent<RectTransform>();
        plr.anchorMin = Vector2.zero; plr.anchorMax = Vector2.one;
        plr.offsetMin = Vector2.zero; plr.offsetMax = Vector2.zero;
        TextMeshProUGUI playTmp = playLabel.AddComponent<TextMeshProUGUI>();
        playTmp.text = "Start Game"; playTmp.font = TMP_Settings.defaultFontAsset;
        playTmp.fontSize = 26f; playTmp.fontStyle = FontStyles.Bold;
        playTmp.alignment = TextAlignmentOptions.Center; playTmp.color = Color.white;
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
        card.AddComponent<Image>().color = cardColor;

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
            tmp.color = Color.white;
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
        _multiplayerPanel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.97f);

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
        titleTmp.color = Color.white;

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
        CreateModeCard(container.transform, "CREATE\nSESSION",
            "Host a game,\nshare your IP",
            new Color(0.32f, 0.18f, 0.10f, 1f),
            new Color(0.42f, 0.24f, 0.12f, 1f),
            new Color(0.22f, 0.12f, 0.07f, 1f),
            interactable: true, OpenCreateSession);

        // Join online session
        CreateModeCard(container.transform, "JOIN\nSESSION",
            "Enter host's IP\nto connect",
            new Color(0.14f, 0.22f, 0.42f, 1f),
            new Color(0.18f, 0.28f, 0.54f, 1f),
            new Color(0.10f, 0.15f, 0.30f, 1f),
            interactable: true, OpenJoinSession);

        _multiplayerPanel.SetActive(false);
    }

    private void CreateModeCard(Transform parent, string title, string subtitle,
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
        img.color = baseColor;

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = interactable;
        ColorBlock cb = btn.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor = pressColor;
        cb.selectedColor = hoverColor;
        cb.disabledColor = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f, 0.7f);
        btn.colors = cb;
        if (interactable && onClick != null)
            btn.onClick.AddListener(onClick);

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
        titleTmp.color = interactable ? Color.white : new Color(0.5f, 0.5f, 0.55f, 0.8f);
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
        subTmp.color = interactable ? new Color(0.8f, 0.92f, 0.82f, 0.9f) : new Color(0.4f, 0.4f, 0.45f, 0.7f);
        subTmp.enableWordWrapping = true;
        subTmp.raycastTarget = false;
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
        img.color = color;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.1f;
        cb.pressedColor = color * 0.88f;
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
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    [Serializable]
    private class JwtPayload
    {
        public string sub;
        public long exp;
        public int userId;
    }
}
