using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AuthMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject registerPanel;
    public GameObject loginPanel;
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
    public GameObject registerButton;
    public GameObject loginButton;
    public GameObject logoutButton;

    [Header("Config")]
    public string apiBaseUrl = "http://localhost:8080";
    public string gameplaySceneName = "GameScene";

    private AuthApiClient _apiClient;

    private void Start()
    {
        _apiClient = new AuthApiClient(apiBaseUrl);
        AuthSession.LoadFromPrefs();

        Debug.Log($"[AuthUI] Menu initialized. LoggedIn={AuthSession.IsLoggedIn}, User='{AuthSession.Username}'");

        RefreshSessionUI();
        ShowOnly(mainMenuPanel);

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
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
    }

    public void OpenLogin()
    {
        if (AuthSession.IsLoggedIn)
        {
            ShowError("You are already logged in. Please log out first.");
            return;
        }

        Debug.Log("[AuthUI] Open Login panel");
        ShowOnly(loginPanel);
    }

    public void BackToMain()
    {
        Debug.Log("[AuthUI] Back to Main panel");
        ShowOnly(mainMenuPanel);
    }

    public void PlayGame()
    {
        Debug.Log($"[AuthUI] Play clicked. Loading scene '{gameplaySceneName}'");
        SceneManager.LoadScene(gameplaySceneName);
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

        Debug.Log($"[AuthUI] Submitting register for '{username}' / '{email}'");

        StartCoroutine(_apiClient.Register(username, email, password,
            onSuccess: user =>
            {
                AuthSession.SetLoggedIn(user);
                RefreshSessionUI();
                Debug.Log($"[AuthUI] Register success. Logged as '{AuthSession.Username}' (id={AuthSession.UserId})");
                ShowOnly(mainMenuPanel);
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
            onSuccess: user =>
            {
                AuthSession.SetLoggedIn(user);
                RefreshSessionUI();
                Debug.Log($"[AuthUI] Login success. Logged as '{AuthSession.Username}' (id={AuthSession.UserId})");
                ShowOnly(mainMenuPanel);
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

    public void CloseError()
    {
        Debug.Log("[AuthUI] Close error panel");
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }

        Debug.LogError($"[AuthUI] Error shown to user: {message}");
    }

    private void ShowOnly(GameObject activePanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        if (registerPanel != null) registerPanel.SetActive(activePanel == registerPanel);
        if (loginPanel != null) loginPanel.SetActive(activePanel == loginPanel);
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
        if (loginButton != null) loginButton.SetActive(!loggedIn);
        if (logoutButton != null) logoutButton.SetActive(loggedIn);
    }
}
