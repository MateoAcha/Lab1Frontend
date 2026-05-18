using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AuthApiClient
{
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

        yield return PostJson("/users/login", JsonUtility.ToJson(payload), onSuccess, onError);
    }

    public IEnumerator GetUserById(int userId, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        yield return GetJson($"/users/{userId}", onSuccess, onError, requiresAuth: true);
    }

    public IEnumerator GetUserByIdWithoutAuth(int userId, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        yield return GetJson($"/users/{userId}", onSuccess, onError, requiresAuth: false);
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

    public IEnumerator BuyShopItem(int userId, int shopItemId, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new BuyShopItemRequest { shopItemId = shopItemId });
        var request = new UnityWebRequest(_baseUrl + $"/users/{userId}/shop/buy", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

        Debug.Log($"[AuthApi] POST {_baseUrl}/users/{userId}/shop/buy  shopItemId={shopItemId}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
            onSuccess?.Invoke();
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator GetSkins(int userId, Action<UserSkinsData> onSuccess, Action<string> onError)
    {
        var request = UnityWebRequest.Get(_baseUrl + $"/users/{userId}/skins");
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
        bool requiresAuth = false)
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
            const string authError = "Missing access token. Please log in again.";
            Debug.LogError($"[AuthApi] {authError}");
            onError?.Invoke(authError);
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

    private string FormatError(UnityWebRequest request)
    {
        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        long code  = request.responseCode;
        Debug.Log($"[AuthApi] Error body (code={code}): {raw}");

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

    public IEnumerator LobbyStart(Action onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + "/lobby/start", "POST");
        request.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success
            && request.responseCode >= 200 && request.responseCode < 300)
            onSuccess?.Invoke();
        else
            onError?.Invoke(FormatError(request));
    }

    public IEnumerator LobbyPing(string weapon, string armor, string item, float x, float y,
        Action<LobbyPlayerData[], bool> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new LobbyPingRequest
        {
            weapon = weapon ?? "",
            armor  = armor  ?? "",
            item   = item   ?? "",
            x = x, y = y
        });
        var request = new UnityWebRequest(_baseUrl + "/lobby/ping", "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

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

    public IEnumerator LobbyLeave()
    {
        var request = new UnityWebRequest(_baseUrl + "/lobby/leave", "DELETE");
        request.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");
        yield return request.SendWebRequest();
    }

    [Serializable]
    private class AddCoinsRequest
    {
        public int quantity;
    }

    [Serializable]
    private class BuyShopItemRequest
    {
        public int shopItemId;
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
    private class LobbyPingRequest
    {
        public string weapon;
        public string armor;
        public string item;
        public float x;
        public float y;
    }

    [Serializable]
    private class LobbyResponseWrapper
    {
        public LobbyPlayerData[] players;
        public bool started;
    }
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
