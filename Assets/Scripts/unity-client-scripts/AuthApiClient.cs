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

    private IEnumerator PostJson(string endpoint, string jsonBody, Action<AuthUserData> onSuccess, Action<string> onError)
    {
        var request = new UnityWebRequest(_baseUrl + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

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
