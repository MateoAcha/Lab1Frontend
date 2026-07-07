using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AuthApiClient
{
    private const string LobbyClientIdHeader = "X-Lobby-Client-Id";
    private const string SessionExpiredMessage = "Session expired or was replaced. Please log in again.";

    private readonly string _baseUrl;

    public AuthApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        Debug.Log($"[AuthApi] Initialized with base URL: {_baseUrl}");
    }

    public IEnumerator Register(string username, string email, string password, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        Debug.Log($"[AuthApi] Register requested for username='{username}', email='{email}'");

        var payload = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        yield return PostJson("/users", JsonUtility.ToJson(payload), onSuccess, onError);
    }

    public IEnumerator Login(string username, string password, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        Debug.Log($"[AuthApi] Login requested for username='{username}'");

        var payload = new LoginRequest
        {
            username = username,
            password = password
        };

        yield return PostJson("/users/login", JsonUtility.ToJson(payload), onSuccess, onError, loginRequest: true);
    }

    public IEnumerator GoogleLogin(
        string idToken,
        string username,
        Action<AuthUserData> onSuccess,
        Action<string, string> onNeedsUsername,
        Action<string> onError)
    {
        Debug.Log("[AuthApi] Google login requested");

        var payload = new GoogleLoginRequest
        {
            idToken = idToken ?? "",
            username = username ?? ""
        };

        yield return PostGoogleLoginPayload(payload, onSuccess, onNeedsUsername, onError);
    }

    public IEnumerator GoogleLoginWithCode(
        string authCode,
        string codeVerifier,
        string redirectUri,
        string username,
        Action<AuthUserData> onSuccess,
        Action<string, string> onNeedsUsername,
        Action<string> onError)
    {
        Debug.Log("[AuthApi] Google browser login requested");

        var payload = new GoogleLoginRequest
        {
            authCode = authCode ?? "",
            codeVerifier = codeVerifier ?? "",
            redirectUri = redirectUri ?? "",
            username = username ?? ""
        };

        yield return PostGoogleLoginPayload(payload, onSuccess, onNeedsUsername, onError);
    }

    public IEnumerator GoogleLoginWithSignupToken(
        string signupToken,
        string username,
        Action<AuthUserData> onSuccess,
        Action<string, string> onNeedsUsername,
        Action<string> onError)
    {
        Debug.Log("[AuthApi] Google signup username requested");

        var payload = new GoogleLoginRequest
        {
            signupToken = signupToken ?? "",
            username = username ?? ""
        };

        yield return PostGoogleLoginPayload(payload, onSuccess, onNeedsUsername, onError);
    }

    private IEnumerator PostGoogleLoginPayload(
        GoogleLoginRequest payload,
        Action<AuthUserData> onSuccess,
        Action<string, string> onNeedsUsername,
        Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + "/users/login/google", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from /users/login/google");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request, loginRequest: true));
            yield break;
        }

        GoogleLoginData loginData;
        try { loginData = JsonUtility.FromJson<GoogleLoginData>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected Google login response."); yield break; }

        if (loginData == null)
        {
            onError?.Invoke("Empty Google login response.");
            yield break;
        }

        if (loginData.requiresUsername)
        {
            Debug.Log($"[AuthApi] Google login needs username. SignupTokenPresent={!string.IsNullOrWhiteSpace(loginData.signupToken)}");
            onNeedsUsername?.Invoke(loginData.email ?? "", loginData.signupToken ?? "");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(loginData.accessToken))
        {
            onError?.Invoke("Google login did not return a session.");
            yield break;
        }

        onSuccess?.Invoke(new AuthUserData
        {
            userId = loginData.userId,
            username = loginData.username,
            email = loginData.email,
            accessToken = loginData.accessToken,
            tokenType = loginData.tokenType
        });
    }

    public IEnumerator GetUserById(int userId, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        yield return GetJson($"/users/{userId}", onSuccess, onError, requiresAuth: true);
    }

    public IEnumerator GetUserByIdWithoutAuth(int userId, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        yield return GetJson($"/users/{userId}", onSuccess, onError, requiresAuth: false);
    }

    public IEnumerator GetProfileByUsername(string username, Action<UserProfileSummaryResponse> onSuccess, Action<string> onError)
    {
        string safeUsername = UnityWebRequest.EscapeURL(username ?? "");
        yield return GetJsonTyped($"/users/profile/{safeUsername}", onSuccess, onError, requiresAuth: true, context: "profile");
    }

    public IEnumerator GetProfileByUserId(int userId, Action<UserProfileSummaryResponse> onSuccess, Action<string> onError)
    {
        yield return GetJsonTyped($"/users/{Mathf.Max(1, userId)}/profile-summary", onSuccess, onError, requiresAuth: true, context: "profile");
    }

    public IEnumerator ValidateCurrentSession(int userId, Action<AuthUserData> onSuccess, Action<long, string> onError)
    {
        if (string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            onError?.Invoke(401, "Missing access token. Please log in again.");
            yield break;
        }

        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}");
        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        request.timeout = 10;

        Debug.Log($"[AuthApi] Validating stored session for userId={userId}");
        yield return request.SendWebRequest();
        Debug.Log($"[AuthApi] Session validation response {(long)request.responseCode} for userId={userId}");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(request.responseCode, FormatError(request));
            yield break;
        }

        AuthUserData userData;
        try
        {
            userData = JsonUtility.FromJson<AuthUserData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke(request.responseCode, "Unexpected session validation response.");
            yield break;
        }

        if (userData == null)
        {
            onError?.Invoke(request.responseCode, "Empty session validation response.");
            yield break;
        }

        onSuccess?.Invoke(userData);
    }

    public IEnumerator GetDailyCoinsStatus(Action<DailyCoinsStatusData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + "/users/me/daily-coins");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/me/daily-coins");
        yield return request.SendWebRequest();
        HandleDailyCoinsResponse(request, onSuccess, onError, "daily coins status");
    }

    public IEnumerator ClaimDailyCoins(Action<DailyCoinsStatusData> onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + "/users/me/daily-coins/claim", "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/me/daily-coins/claim");
        yield return request.SendWebRequest();
        HandleDailyCoinsResponse(request, onSuccess, onError, "daily coins claim");
    }

    public IEnumerator GetSocialSummary(Action<SocialSummaryResponse> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + "/social/summary");
        request.timeout = 10;
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/social/summary");
        yield return request.SendWebRequest();
        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from /social/summary");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.Log($"[AuthApi] Social summary raw: {TruncateForLog(raw, 1800)}");

        SocialSummaryResponse summary = ParseSocialSummary(raw);
        onSuccess?.Invoke(summary ?? new SocialSummaryResponse());
    }

    public IEnumerator SendFriendRequest(string username, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        var payload = new SocialUsernameRequest
        {
            username = username ?? "",
            recipientUsername = username ?? ""
        };

        yield return SendJsonTyped(
            "/friends/requests",
            "POST",
            JsonUtility.ToJson(payload),
            onSuccess,
            onError,
            requiresAuth: true,
            context: "friend request");
    }

    public IEnumerator AcceptFriendRequest(int requestId, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        yield return SendJsonTyped(
            $"/friends/requests/{Mathf.Max(1, requestId)}/accept",
            "POST",
            "",
            onSuccess,
            onError,
            requiresAuth: true,
            context: "friend request accept");
    }

    public IEnumerator DeclineFriendRequest(int requestId, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        yield return SendJsonTyped(
            $"/friends/requests/{Mathf.Max(1, requestId)}/decline",
            "POST",
            "",
            onSuccess,
            onError,
            requiresAuth: true,
            context: "friend request decline");
    }

    public IEnumerator CancelFriendRequest(int requestId, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        yield return SendJsonTyped(
            $"/friends/requests/{Mathf.Max(1, requestId)}",
            "DELETE",
            "",
            onSuccess,
            onError,
            requiresAuth: true,
            context: "friend request cancel");
    }

    public IEnumerator RemoveFriend(int friendUserId, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        string endpoint = $"/friends/{Mathf.Max(1, friendUserId)}";
        var request = new UnityWebRequest(_baseUrl + endpoint, "DELETE");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
            yield break;

        yield return SendAndParseSocialActionOrSummary(request, endpoint, onSuccess, onError, "remove friend");
    }

    public IEnumerator SendLobbyInvite(string username, int roomNumber, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        var payload = new LobbyInviteCreateRequest
        {
            username = username ?? "",
            recipientUsername = username ?? "",
            roomNumber = Mathf.Max(1, roomNumber)
        };

        yield return SendJsonTyped(
            "/lobby/invites",
            "POST",
            JsonUtility.ToJson(payload),
            onSuccess,
            onError,
            requiresAuth: true,
            context: "lobby invite");
    }

    public IEnumerator AcceptLobbyInvite(int inviteId, Action<GameInviteActionResponse> onSuccess, Action<string> onError)
    {
        yield return SendJsonTyped(
            $"/lobby/invites/{Mathf.Max(1, inviteId)}/accept",
            "POST",
            "",
            onSuccess,
            onError,
            requiresAuth: true,
            context: "lobby invite accept");
    }

    public IEnumerator DeclineLobbyInvite(int inviteId, Action<SocialActionResponse> onSuccess, Action<string> onError)
    {
        yield return SendJsonTyped(
            $"/lobby/invites/{Mathf.Max(1, inviteId)}/decline",
            "POST",
            "",
            onSuccess,
            onError,
            requiresAuth: true,
            context: "lobby invite decline");
    }

    public IEnumerator AddCoins(int userId, int quantity, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new AddCoinsRequest { quantity = quantity });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/add-coins", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/inventory/add-coins  qty={quantity}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator SpendCoins(int userId, int quantity, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new AddCoinsRequest { quantity = quantity });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/spend-coins", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator AddEmeralds(int userId, int quantity, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new AddEmeraldsRequest { quantity = quantity });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/add-emeralds", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/inventory/add-emeralds  qty={quantity}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator SpendMaterial(int userId, string materialKey, int quantity, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new SpendMaterialRequest
        {
            materialKey = materialKey ?? "",
            quantity = Mathf.Max(0, quantity)
        });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/spend-material", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/inventory/spend-material  materialKey={materialKey} qty={quantity}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator ConsumeInventoryItem(int userId, int userInventoryId, int quantity, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new ConsumeInventoryItemRequest
        {
            quantity = Mathf.Max(1, quantity)
        });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/{userInventoryId}/consume", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/inventory/{userInventoryId}/consume qty={quantity}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator GetPlayerStats(int userId, Action<PlayerStatsData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/stats");
        request.timeout = 5;
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/{userId}/stats");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        PlayerStatsData stats;
        try
        {
            stats = JsonUtility.FromJson<PlayerStatsData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected stats response.");
            yield break;
        }

        onSuccess?.Invoke(stats ?? new PlayerStatsData { level = 1 });
    }

    public IEnumerator GetSkillTree(int userId, Action<SkillTreeStateData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/skills");
        request.timeout = 5;
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/{userId}/skills");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        SkillTreeStateData state;
        try
        {
            state = JsonUtility.FromJson<SkillTreeStateData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected skill tree response.");
            yield break;
        }

        onSuccess?.Invoke(state ?? new SkillTreeStateData());
    }

    public IEnumerator UnlockSkill(int userId, string skillId, Action<SkillTreeActionData> onSuccess, Action<string> onError)
    {
        yield return PostSkillAction(userId, skillId, "unlock", onSuccess, onError);
    }

    public IEnumerator EquipSkill(int userId, string skillId, Action<SkillTreeActionData> onSuccess, Action<string> onError)
    {
        yield return PostSkillAction(userId, skillId, "equip", onSuccess, onError);
    }

    public IEnumerator LevelUpSkill(int userId, string skillId, Action<SkillTreeActionData> onSuccess, Action<string> onError)
    {
        yield return PostSkillAction(userId, skillId, "level-up", onSuccess, onError);
    }

    public IEnumerator EquipInventoryItem(int userId, int userInventoryId, Action<UserInventoryData> onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/inventory/{userInventoryId}/equip", "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/inventory/{userInventoryId}/equip");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        UserInventoryData inventory;
        try
        {
            inventory = JsonUtility.FromJson<UserInventoryData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected inventory equip response.");
            yield break;
        }

        onSuccess?.Invoke(inventory ?? new UserInventoryData());
    }

    private IEnumerator PostSkillAction(
        int userId,
        string skillId,
        string action,
        Action<SkillTreeActionData> onSuccess,
        Action<string> onError)
    {
        string safeSkillId = UnityWebRequest.EscapeURL(skillId ?? "");
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/skills/{safeSkillId}/{action}", "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/skills/{safeSkillId}/{action}");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        SkillTreeActionData data;
        try
        {
            data = JsonUtility.FromJson<SkillTreeActionData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected skill action response.");
            yield break;
        }

        onSuccess?.Invoke(data ?? new SkillTreeActionData());
    }

    public IEnumerator GetClaimedChallengeRewards(int userId, Action<ChallengeClaimsData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/challenges/claimed");
        request.timeout = 5;
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/{userId}/challenges/claimed");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        if (string.IsNullOrWhiteSpace(request.downloadHandler.text))
        {
            onSuccess?.Invoke(new ChallengeClaimsData());
            yield break;
        }

        ChallengeClaimsData data;
        try
        {
            data = JsonUtility.FromJson<ChallengeClaimsData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected challenge claims response.");
            yield break;
        }

        onSuccess?.Invoke(data ?? new ChallengeClaimsData());
    }

    public IEnumerator SaveChallengeClaim(int userId, int challengeId, string challengeKey, int rewardCoins, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new ChallengeClaimRequest
        {
            challengeId = challengeId,
            challengeKey = challengeKey ?? "",
            rewardCoins = rewardCoins
        });

        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/challenges/claimed", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/challenges/claimed  challengeId={challengeId}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200
            && request.responseCode < 300)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onError?.Invoke(FormatError(request));
        }
    }

    public IEnumerator GetShopItems(Action<ShopCatalogData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + "/shop/items");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        Debug.Log($"[AuthApi] GET {_baseUrl}/shop/items");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        ShopCatalogData catalog;
        try { catalog = JsonUtility.FromJson<ShopCatalogData>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected shop response."); yield break; }

        if (catalog == null) { onError?.Invoke("Empty shop response."); yield break; }
        onSuccess?.Invoke(catalog);
    }

    public IEnumerator BuyShopItem(int userId, int shopItemId, string currency, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new BuyShopItemRequest
        {
            shopItemId = shopItemId,
            currency = currency ?? "COINS"
        });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/shop/buy", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/shop/buy  shopItemId={shopItemId} currency={currency}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
            onSuccess?.Invoke();
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator CreatePaymentPreference(int userId, int emeralds, int pesosPrice,
        Action<PaymentPreferenceData> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new CreatePreferenceRequest
        {
            emeralds = emeralds,
            pesosPrice = pesosPrice
        });
        var request = new UnityWebRequest(_baseUrl + $"/payments/{userId}/create-preference", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            try
            {
                var data = JsonUtility.FromJson<PaymentPreferenceData>(request.downloadHandler.text);
                onSuccess?.Invoke(data);
            }
            catch { onError?.Invoke("Unexpected payment response."); }
        }
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator VerifyPayment(long paymentRecordId, Action<string> onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + $"/payments/verify/{paymentRecordId}", "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 15;
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            try
            {
                var data = JsonUtility.FromJson<PaymentStatusData>(request.downloadHandler.text);
                onSuccess?.Invoke(data?.status ?? "UNKNOWN");
            }
            catch { onError?.Invoke("Unexpected response."); }
        }
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator GetPaymentStatus(long paymentRecordId, Action<string> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/payments/status/{paymentRecordId}");
        request.timeout = 10;
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
        {
            try
            {
                var data = JsonUtility.FromJson<PaymentStatusData>(request.downloadHandler.text);
                onSuccess?.Invoke(data?.status ?? "UNKNOWN");
            }
            catch { onError?.Invoke("Unexpected status response."); }
        }
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator GetSkins(int userId, Action<UserSkinsData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/skins");
        request.timeout = 5;
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/{userId}/skins");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        UserSkinsData data;
        try { data = JsonUtility.FromJson<UserSkinsData>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected skins response."); yield break; }

        if (data == null) { onError?.Invoke("Empty skins response."); yield break; }
        onSuccess?.Invoke(data);
    }

    public IEnumerator EquipSkin(int userId, int skinId, Action onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/skins/{skinId}/equip", "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/skins/{skinId}/equip");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
            onSuccess?.Invoke();
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator GetInventory(int userId, Action<UserInventoryData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/inventory");
        request.timeout = 5;

        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        }

        Debug.Log($"[AuthApi] GET {_baseUrl}/users/{userId}/inventory");

        yield return request.SendWebRequest();

        Debug.Log($"[AuthApi] Inventory response {(long)request.responseCode} for userId={userId}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
            {
                AuthSession.Logout();
                onError?.Invoke("Session expired. Please log in again.");
                yield break;
            }
            string error = FormatError(request);
            Debug.LogError($"[AuthApi] Inventory network error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        if (request.responseCode < 200 || request.responseCode >= 300)
        {
            string error = FormatError(request);
            Debug.LogError($"[AuthApi] Inventory API error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        UserInventoryData inventoryData;
        try
        {
            inventoryData = JsonUtility.FromJson<UserInventoryData>(request.downloadHandler.text);
        }
        catch
        {
            const string parseError = "Unexpected inventory response.";
            Debug.LogError($"[AuthApi] Inventory parse error. Raw response: {request.downloadHandler.text}");
            onError?.Invoke(parseError);
            yield break;
        }

        if (inventoryData == null)
        {
            const string emptyError = "Empty inventory response.";
            Debug.LogError("[AuthApi] Inventory response body was empty after success.");
            onError?.Invoke(emptyError);
            yield break;
        }

        onSuccess?.Invoke(inventoryData);
    }

    private IEnumerator PostJson(
        string endpoint,
        string jsonBody,
        Action<AuthUserData> onSuccess,
        Action<string> onError,
        bool requiresAuth = false,
        bool loginRequest = false)
    {
        var request = new UnityWebRequest(_baseUrl + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (requiresAuth && !TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] POST {_baseUrl + endpoint}");

        yield return request.SendWebRequest();

        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from {endpoint}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = FormatError(request, loginRequest);
            Debug.LogError($"[AuthApi] Network error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        if (request.responseCode < 200 || request.responseCode >= 300)
        {
            string error = FormatError(request, loginRequest);
            Debug.LogError($"[AuthApi] API error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        AuthUserData userData;
        try
        {
            userData = JsonUtility.FromJson<AuthUserData>(request.downloadHandler.text);
        }
        catch
        {
            const string parseError = "Unexpected server response.";
            Debug.LogError($"[AuthApi] Parse error. Raw response: {request.downloadHandler.text}");
            onError?.Invoke(parseError);
            yield break;
        }

        if (userData == null)
        {
            const string emptyError = "Empty server response.";
            Debug.LogError("[AuthApi] Empty response after successful status code.");
            onError?.Invoke(emptyError);
            yield break;
        }

        Debug.Log($"[AuthApi] Success for userId={userData.userId}, username='{userData.username}'");
        onSuccess?.Invoke(userData);
    }

    private IEnumerator GetJson(
        string endpoint,
        Action<AuthUserData> onSuccess,
        Action<string> onError,
        bool requiresAuth = false)
    {
        var request = UnityWebRequest.Get(_baseUrl + endpoint);

        if (requiresAuth && !TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        Debug.Log($"[AuthApi] GET {_baseUrl + endpoint}");

        yield return request.SendWebRequest();

        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from {endpoint}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = FormatError(request);
            Debug.LogError($"[AuthApi] Network error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        if (request.responseCode < 200 || request.responseCode >= 300)
        {
            string error = FormatError(request);
            Debug.LogError($"[AuthApi] API error: {error}");
            onError?.Invoke(error);
            yield break;
        }

        AuthUserData userData;
        try
        {
            userData = JsonUtility.FromJson<AuthUserData>(request.downloadHandler.text);
        }
        catch
        {
            const string parseError = "Unexpected server response.";
            Debug.LogError($"[AuthApi] Parse error. Raw response: {request.downloadHandler.text}");
            onError?.Invoke(parseError);
            yield break;
        }

        if (userData == null)
        {
            const string emptyError = "Empty server response.";
            Debug.LogError("[AuthApi] Empty response after successful status code.");
            onError?.Invoke(emptyError);
            yield break;
        }

        Debug.Log($"[AuthApi] Success for userId={userData.userId}, username='{userData.username}'");
        onSuccess?.Invoke(userData);
    }

    private bool TryAttachAuthorization(UnityWebRequest request, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            if (AuthSession.IsLoggedIn)
                AuthSession.Logout();
            Debug.LogError($"[AuthApi] {SessionExpiredMessage}");
            onError?.Invoke(SessionExpiredMessage);
            return false;
        }

        request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        return true;
    }

    private void HandleDailyCoinsResponse(
        UnityWebRequest request,
        Action<DailyCoinsStatusData> onSuccess,
        Action<string> onError,
        string context)
    {
        Debug.Log($"[AuthApi] Response {(long)request.responseCode} for {context}");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            return;
        }

        DailyCoinsStatusData status;
        try
        {
            status = JsonUtility.FromJson<DailyCoinsStatusData>(request.downloadHandler.text);
        }
        catch
        {
            onError?.Invoke("Unexpected daily coins response.");
            return;
        }

        if (status == null)
        {
            onError?.Invoke("Empty daily coins response.");
            return;
        }

        onSuccess?.Invoke(status);
    }

    private string FormatError(UnityWebRequest request, bool loginRequest = false)
    {
        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        long code  = request.responseCode;
        Debug.Log($"[AuthApi] Error body (code={code}): {raw}");

        if (!loginRequest && (code == 401 || code == 403))
        {
            return SessionExpiredMessage;
        }

        // Try to extract a message from the JSON body (works with both Spring Boot formats).
        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (string field in new[] { "detail", "message", "title" })
            {
                string val = ExtractJsonString(raw, field);
                if (!string.IsNullOrWhiteSpace(val) &&
                    !val.Equals("error", System.StringComparison.OrdinalIgnoreCase))
                    return val;
            }
        }

        // Status-code fallbacks for common cases.
        if (code == 401) return "Incorrect username or password.";
        if (code == 409)
        {
            if (raw.IndexOf("username", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "That username is already taken.";
            if (raw.IndexOf("email", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "That email is already registered.";
            return "Account already exists.";
        }
        if (code == 400) return "Invalid data.";
        if (code == 403) return "Access denied.";
        if (code == 404) return "Not found.";

        return "Invalid data.";
    }

    // Extracts the string value of a JSON key without a full JSON parser.
    private static string ExtractJsonString(string json, string key)
    {
        string search = "\"" + key + "\"";
        int idx = json.IndexOf(search, System.StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += search.Length;
        while (idx < json.Length && (json[idx] == ' ' || json[idx] == ':')) idx++;
        if (idx >= json.Length || json[idx] != '"') return null;
        idx++;
        var sb = new System.Text.StringBuilder();
        while (idx < json.Length && json[idx] != '"')
        {
            if (json[idx] == '\\') idx++;
            if (idx < json.Length) sb.Append(json[idx]);
            idx++;
        }
        return sb.ToString();
    }

    private static SocialSummaryResponse ParseSocialSummary(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new SocialSummaryResponse();

        SocialSummaryResponse direct = null;
        try
        {
            direct = JsonUtility.FromJson<SocialSummaryResponse>(raw);
        }
        catch
        {
            direct = null;
        }

        if (HasAnySocialSection(direct))
            return direct;

        SocialSummaryEnvelope envelope = null;
        try
        {
            envelope = JsonUtility.FromJson<SocialSummaryEnvelope>(raw);
        }
        catch
        {
            envelope = null;
        }

        if (HasAnySocialSection(envelope?.summary))
            return envelope.summary;
        if (HasAnySocialSection(envelope?.data))
            return envelope.data;
        if (HasAnySocialSection(envelope?.social))
            return envelope.social;
        if (HasAnySocialSection(envelope?.result))
            return envelope.result;
        if (HasAnySocialSection(envelope?.payload))
            return envelope.payload;

        string summaryObject = ExtractJsonObject(raw, "summary");
        if (!string.IsNullOrWhiteSpace(summaryObject))
        {
            try
            {
                SocialSummaryResponse nested = JsonUtility.FromJson<SocialSummaryResponse>(summaryObject);
                if (nested != null)
                    return nested;
            }
            catch
            {
                Debug.LogWarning("[AuthApi] Could not parse nested social summary object.");
            }
        }

        return direct ?? new SocialSummaryResponse();
    }

    private static bool HasAnySocialSection(SocialSummaryResponse summary)
    {
        if (summary == null)
            return false;

        return summary.friends != null
            || summary.friendList != null
            || summary.friendships != null
            || summary.incomingFriendRequests != null
            || summary.incomingRequests != null
            || summary.receivedFriendRequests != null
            || summary.friendRequests != null
            || summary.sentFriendRequests != null
            || summary.sentRequests != null
            || summary.outgoingFriendRequests != null
            || summary.gameInvites != null
            || summary.invites != null
            || summary.pendingGameInvites != null
            || summary.lobbyInvites != null;
    }

    private static string ExtractJsonObject(string json, string key)
    {
        string search = "\"" + key + "\"";
        int idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        int colon = json.IndexOf(':', idx + search.Length);
        if (colon < 0)
            return null;

        int start = colon + 1;
        while (start < json.Length && char.IsWhiteSpace(json[start]))
            start++;

        if (start >= json.Length || json[start] != '{')
            return null;

        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return json.Substring(start, i - start + 1);
            }
        }

        return null;
    }

    private static string TruncateForLog(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "";

        return value.Substring(0, Mathf.Max(0, maxLength)) + "...";
    }

    private IEnumerator GetJsonTyped<T>(
        string endpoint,
        Action<T> onSuccess,
        Action<string> onError,
        bool requiresAuth,
        string context) where T : new()
    {
        var request = UnityWebRequest.Get(_baseUrl + endpoint);
        if (requiresAuth && !TryAttachAuthorization(request, onError))
            yield break;

        yield return SendAndParseJson(request, endpoint, onSuccess, onError, context);
    }

    private IEnumerator SendJsonTyped<T>(
        string endpoint,
        string method,
        string jsonBody,
        Action<T> onSuccess,
        Action<string> onError,
        bool requiresAuth,
        string context) where T : new()
    {
        var request = new UnityWebRequest(_baseUrl + endpoint, method);
        string body = jsonBody ?? "";
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (requiresAuth && !TryAttachAuthorization(request, onError))
            yield break;

        yield return SendAndParseJson(request, endpoint, onSuccess, onError, context);
    }

    private IEnumerator SendAndParseJson<T>(
        UnityWebRequest request,
        string endpoint,
        Action<T> onSuccess,
        Action<string> onError,
        string context) where T : new()
    {
        Debug.Log($"[AuthApi] {request.method} {_baseUrl + endpoint}");
        yield return request.SendWebRequest();
        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from {endpoint}");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            onSuccess?.Invoke(new T());
            yield break;
        }

        T data;
        try
        {
            data = JsonUtility.FromJson<T>(raw);
        }
        catch
        {
            Debug.LogError($"[AuthApi] Unexpected {context} response: {raw}");
            onError?.Invoke($"Unexpected {context} response.");
            yield break;
        }

        onSuccess?.Invoke(data == null ? new T() : data);
    }

    private IEnumerator SendAndParseSocialActionOrSummary(
        UnityWebRequest request,
        string endpoint,
        Action<SocialActionResponse> onSuccess,
        Action<string> onError,
        string context)
    {
        Debug.Log($"[AuthApi] {request.method} {_baseUrl + endpoint}");
        yield return request.SendWebRequest();
        Debug.Log($"[AuthApi] Response {(long)request.responseCode} from {endpoint}");

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200
            || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            onSuccess?.Invoke(new SocialActionResponse());
            yield break;
        }

        SocialActionResponse action = null;
        try
        {
            action = JsonUtility.FromJson<SocialActionResponse>(raw);
        }
        catch
        {
            action = null;
        }

        if (action == null)
            action = new SocialActionResponse();

        if (action.summary == null)
        {
            SocialSummaryResponse directSummary = ParseSocialSummary(raw);
            if (HasAnySocialSection(directSummary))
                action.summary = directSummary;
        }

        if (action.summary == null && action.friend == null && action.friendship == null
            && action.request == null && action.friendRequest == null
            && action.invite == null && action.gameInvite == null
            && string.IsNullOrWhiteSpace(action.result))
        {
            Debug.LogWarning($"[AuthApi] Unexpected {context} response: {TruncateForLog(raw, 600)}");
        }

        onSuccess?.Invoke(action);
    }

    public IEnumerator GetLobbyRooms(Action<LobbyRoomListData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + "/lobby/rooms");
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        LobbyRoomListData data;
        try { data = JsonUtility.FromJson<LobbyRoomListData>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected lobby list response."); yield break; }

        onSuccess?.Invoke(data ?? new LobbyRoomListData());
    }

    public IEnumerator LobbyCreate(string lobbyClientId, Action<LobbyRoomData> onSuccess, Action<string> onError)
    {
        yield return LobbyCreate(lobbyClientId, false, "", onSuccess, onError);
    }

    public IEnumerator LobbyCreate(string lobbyClientId, bool privateMatch, string password, Action<LobbyRoomData> onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + "/lobby/create", "POST");
        string json = JsonUtility.ToJson(new LobbyCreateRequest
        {
            privateMatch = privateMatch,
            password = password ?? ""
        });
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AttachLobbyClientId(request, lobbyClientId);
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        LobbyRoomData room;
        try { room = JsonUtility.FromJson<LobbyRoomData>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected lobby create response."); yield break; }

        if (room == null || room.roomNumber <= 0) { onError?.Invoke("Lobby was not created."); yield break; }
        onSuccess?.Invoke(room);
    }

    public IEnumerator LobbyStart(int roomNumber, string lobbyClientId, Action onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + $"/lobby/start?roomNumber={Mathf.Max(1, roomNumber)}", "POST");
        request.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AttachLobbyClientId(request, lobbyClientId);
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
            onSuccess?.Invoke();
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator LobbyPing(int roomNumber, string lobbyClientId, string weapon, string armor, string item, float x, float y,
        Action<LobbyPlayerData[], bool> onSuccess, Action<string> onError)
    {
        yield return LobbyPing(roomNumber, lobbyClientId, weapon, armor, item, x, y, "", false, onSuccess, onError);
    }

    public IEnumerator LobbyPing(int roomNumber, string lobbyClientId, string weapon, string armor, string item, float x, float y,
        string password, bool inviteJoin, Action<LobbyPlayerData[], bool> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new LobbyPingRequest
        {
            roomNumber = Mathf.Max(1, roomNumber),
            weapon = weapon ?? "",
            armor  = armor  ?? "",
            item   = item   ?? "",
            password = password ?? "",
            lobbyPassword = password ?? "",
            inviteJoin = inviteJoin,
            x = x, y = y
        });
        var request = new UnityWebRequest(_baseUrl + "/lobby/ping", "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AttachLobbyClientId(request, lobbyClientId);
        if (!TryAttachAuthorization(request, onError))
        {
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success
            || request.responseCode < 200 || request.responseCode >= 300)
        {
            onError?.Invoke(FormatError(request));
            yield break;
        }

        LobbyResponseWrapper wrapper;
        try { wrapper = JsonUtility.FromJson<LobbyResponseWrapper>(request.downloadHandler.text); }
        catch { onError?.Invoke("Unexpected lobby response."); yield break; }

        onSuccess?.Invoke(wrapper?.players ?? new LobbyPlayerData[0], wrapper?.started ?? false);
    }

    public IEnumerator LobbyLeave(int roomNumber, string lobbyClientId)
    {
        var request = new UnityWebRequest(_baseUrl + $"/lobby/leave?roomNumber={Mathf.Max(1, roomNumber)}", "DELETE");
        request.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        AttachLobbyClientId(request, lobbyClientId);
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return request.SendWebRequest();
    }

    private void AttachLobbyClientId(UnityWebRequest request, string lobbyClientId)
    {
        if (!string.IsNullOrWhiteSpace(lobbyClientId))
            request.SetRequestHeader(LobbyClientIdHeader, lobbyClientId);
    }

    [Serializable]
    private class AddCoinsRequest
    {
        public int quantity;
    }

    [Serializable]
    private class AddEmeraldsRequest
    {
        public int quantity;
    }

    [Serializable]
    private class SpendMaterialRequest
    {
        public string materialKey;
        public int quantity;
    }

    [Serializable]
    private class ConsumeInventoryItemRequest
    {
        public int quantity;
    }

    [Serializable]
    private class ChallengeClaimRequest
    {
        public int challengeId;
        public string challengeKey;
        public int rewardCoins;
    }

    [Serializable]
    private class BuyShopItemRequest
    {
        public int shopItemId;
        public string currency;
    }

    [Serializable]
    private class CreatePreferenceRequest
    {
        public int emeralds;
        public int pesosPrice;
    }

    [Serializable]
    private class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class GoogleLoginRequest
    {
        public string idToken;
        public string authCode;
        public string codeVerifier;
        public string redirectUri;
        public string signupToken;
        public string username;
    }

    [Serializable]
    private class SocialUsernameRequest
    {
        public string username;
        public string recipientUsername;
    }

    [Serializable]
    private class LobbyInviteCreateRequest
    {
        public string username;
        public string recipientUsername;
        public int roomNumber;
    }

    [Serializable]
    private class LobbyCreateRequest
    {
        public bool privateMatch;
        public string password;
    }

    [Serializable]
    private class SocialSummaryEnvelope
    {
        public SocialSummaryResponse summary;
        public SocialSummaryResponse data;
        public SocialSummaryResponse social;
        public SocialSummaryResponse result;
        public SocialSummaryResponse payload;
    }

    [Serializable]
    private class LobbyPingRequest
    {
        public int roomNumber;
        public string weapon;
        public string armor;
        public string item;
        public string password;
        public string lobbyPassword;
        public bool inviteJoin;
        public float x;
        public float y;
    }

    [Serializable]
    private class LobbyResponseWrapper
    {
        public LobbyPlayerData[] players;
        public bool started;
        public int roomNumber;
    }
}

[Serializable]
public class LobbyRoomListData
{
    public LobbyRoomData[] rooms;
}

[Serializable]
public class GoogleLoginData
{
    public bool requiresUsername;
    public string email;
    public string signupToken;
    public string accessToken;
    public string tokenType;
    public int userId;
    public string username;
}

[Serializable]
public class LobbyRoomData
{
    public int roomNumber;
    public LobbyPlayerData[] players;
    public int playerCount;
    public int maxPlayers;
    public bool full;
    public bool privateMatch;
}

[Serializable]
public class LobbyPlayerData
{
    public string username;
    public string weapon;
    public string armor;
    public string item;
    public float x;
    public float y;
}

[Serializable]
public class DailyCoinsStatusData
{
    public bool claimable;
    public long remainingSeconds;
    public int rewardCoins;
    public string lastClaimedAt;
    public int coinTotal;
}

[Serializable]
public class ChallengeClaimsData
{
    public int[] claimedChallengeIds;
    public ChallengeClaimData[] claims;
}

[Serializable]
public class ChallengeClaimData
{
    public int challengeId;
    public string challengeKey;
    public int rewardCoins;
    public string claimedAt;
}

[Serializable]
public class SocialSummaryResponse
{
    public FriendSummaryResponse[] friends;
    public FriendSummaryResponse[] friendList;
    public FriendSummaryResponse[] friendships;
    public FriendRequestResponse[] incomingFriendRequests;
    public FriendRequestResponse[] incomingRequests;
    public FriendRequestResponse[] receivedFriendRequests;
    public FriendRequestResponse[] friendRequests;
    public FriendRequestResponse[] sentFriendRequests;
    public FriendRequestResponse[] sentRequests;
    public FriendRequestResponse[] outgoingFriendRequests;
    public GameInviteResponse[] gameInvites;
    public GameInviteResponse[] invites;
    public GameInviteResponse[] pendingGameInvites;
    public GameInviteResponse[] lobbyInvites;
}

[Serializable]
public class FriendSummaryResponse
{
    public int friendshipId;
    public int userId;
    public int id;
    public int friendUserId;
    public int friendId;
    public string username;
    public string friendUsername;
    public string name;
    public string displayName;
    public int level;
}

[Serializable]
public class FriendRequestResponse
{
    public int requestId;
    public int friendRequestId;
    public int id;
    public int requesterUserId;
    public int senderUserId;
    public int fromUserId;
    public int recipientUserId;
    public int toUserId;
    public string requesterUsername;
    public string requester;
    public string senderUsername;
    public string fromUsername;
    public string recipientUsername;
    public string recipient;
    public string toUsername;
    public string createdAt;
    public string status;
}

[Serializable]
public class GameInviteResponse
{
    public int inviteId;
    public int gameInviteId;
    public int id;
    public int hostUserId;
    public int senderUserId;
    public int recipientUserId;
    public string hostUsername;
    public string host;
    public string senderUsername;
    public string fromUsername;
    public string recipientUsername;
    public string toUsername;
    public int roomNumber;
    public int room;
    public string createdAt;
    public string expiresAt;
    public string status;
}

[Serializable]
public class SocialActionResponse
{
    public string result;
    public SocialSummaryResponse summary;
    public FriendSummaryResponse friend;
    public FriendSummaryResponse friendship;
    public FriendRequestResponse request;
    public FriendRequestResponse friendRequest;
    public GameInviteResponse invite;
    public GameInviteResponse gameInvite;
}

[Serializable]
public class GameInviteActionResponse
{
    public string result;
    public SocialSummaryResponse summary;
    public GameInviteResponse invite;
    public GameInviteResponse gameInvite;
    public int roomNumber;
}

[Serializable]
public class UserProfileSummaryResponse
{
    public int userId;
    public int id;
    public string username;
    public string displayName;
    public int level;
    public SocialStatsSummary stats;
    public SocialStatsSummary playerStats;
    public SocialLoadoutSummary loadout;
    public SocialLoadoutSummary equippedLoadout;
    public SocialLoadoutSummary equipped;
}

[Serializable]
public class SocialStatsSummary
{
    public int gamesPlayed;
    public int runsPlayed;
    public int matchesPlayed;
    public int wins;
    public int gamesWon;
    public int losses;
    public int kills;
    public int totalKills;
    public int enemiesKilled;
    public int meleeKills;
    public int rangedKills;
    public int giantKills;
    public int bossKills;
    public int deaths;
    public int revives;
    public int level;
    public int xp;
    public int experience;
    public int emeralds;
    public float bestTimeSeconds;
    public float bestRunTimeSeconds;
    public float fastestWinSeconds;
    public float totalTimeSeconds;
}

[Serializable]
public class SocialLoadoutSummary
{
    public int skinId;
    public int equippedSkinId;
    public string skinColor;
    public int weaponItemId;
    public int equippedWeaponItemId;
    public int weaponId;
    public string weaponType;
    public string weaponName;
    public string weaponItemName;
    public string weaponColor;
    public string armorName;
    public string armorItemName;
    public string consumableName;
    public string itemName;
}
