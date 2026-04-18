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

    private string FormatError(UnityWebRequest request)
    {
        string raw = request.downloadHandler != null ? request.downloadHandler.text : "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return $"Request failed ({request.responseCode}).";
        }

        try
        {
            var apiError = JsonUtility.FromJson<ApiError>(raw);
            if (!string.IsNullOrWhiteSpace(apiError.message))
            {
                return apiError.message;
            }
            if (!string.IsNullOrWhiteSpace(apiError.error))
            {
                return apiError.error;
            }
        }
        catch
        {
            // keep raw body fallback
        }

        return raw;
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
    private class ApiError
    {
        public string message;
        public string error;
    }
}
