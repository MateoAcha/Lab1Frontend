using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Text;

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

        SetAuthProof("Auth proof pending. Log in to run checks.");
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

    public void PlayGame()
    {
        Debug.Log("[AuthUI] Play clicked.");

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
            {
                Destroy(menuRoot);
            }
            else
            {
                Destroy(gameObject);
            }
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
        if (profilePanel != null) profilePanel.SetActive(activePanel == profilePanel);
        if (statsPanel != null) statsPanel.SetActive(activePanel == statsPanel);
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

        if (!loggedIn)
        {
            SetAuthProof("Auth proof pending. Log in to run checks.");
        }
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

        StartCoroutine(_apiClient.GetUserById(AuthSession.UserId,
            onSuccess: profile =>
            {
                AuthSession.UpdateProfile(profile);
                RefreshSessionUI();
                ShowOnly(mainMenuPanel);
                Debug.Log($"[AuthUI] Profile refreshed for '{AuthSession.Username}'.");
                StartCoroutine(RunAuthProofChecks());
            },
            onError: profileError =>
            {
                Debug.LogWarning($"[AuthUI] Logged in, but profile refresh failed: {profileError}");
                ShowOnly(mainMenuPanel);
                StartCoroutine(RunAuthProofChecks());
            }));
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

    [Serializable]
    private class JwtPayload
    {
        public string sub;
        public long exp;
        public int userId;
    }
}
