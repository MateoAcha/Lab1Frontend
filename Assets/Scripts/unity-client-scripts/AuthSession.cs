using System;
using UnityEngine;

[System.Serializable]
public class AuthUserData
{
    public int userId;
    public string username;
    public string email;
    public string accessToken;
    public string tokenType;
    public bool isPremium;
    public string premiumSince;
    public string premiumUntil;
    public string createdAt;
}

public static class AuthSession
{
    private const string KeyCurrentServerUrl = "auth_current_server_url";
    private const string KeyUserId = "auth_user_id";
    private const string KeyUsername = "auth_username";
    private const string KeyEmail = "auth_email";
    private const string KeyIsPremium = "auth_is_premium";
    private const string KeyAccessToken = "auth_access_token";

    public static bool IsLoggedIn { get; private set; }
    public static int UserId { get; private set; }
    public static string Username { get; private set; } = "";
    public static string Email { get; private set; } = "";
    public static string AccessToken { get; private set; } = "";
    public static bool IsPremium { get; private set; }
    public static string CurrentServerUrl { get; private set; } = "";

    public static void LoadFromPrefs(string defaultServerUrl = "")
    {
        CurrentServerUrl = NormalizeServerUrl(PlayerPrefs.GetString(KeyCurrentServerUrl, defaultServerUrl));
        LoadSessionForCurrentServer();
    }

    public static void SwitchServer(string serverUrl)
    {
        CurrentServerUrl = NormalizeServerUrl(serverUrl);
        PlayerPrefs.SetString(KeyCurrentServerUrl, CurrentServerUrl);
        PlayerPrefs.Save();
        LoadSessionForCurrentServer();
    }

    public static bool IsLoggedInForServer(string serverUrl)
    {
        return IsLoggedIn
            && string.Equals(CurrentServerUrl, NormalizeServerUrl(serverUrl), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeServerUrl(string serverUrl)
    {
        return string.IsNullOrWhiteSpace(serverUrl) ? "" : serverUrl.Trim().TrimEnd('/');
    }

    private static void LoadSessionForCurrentServer()
    {
        bool hasServerSession = HasCurrentServerKey(KeyUserId) && HasCurrentServerKey(KeyAccessToken);
        bool canImportLegacySession = !PlayerPrefs.HasKey(KeyCurrentServerUrl)
            && PlayerPrefs.HasKey(KeyUserId)
            && PlayerPrefs.HasKey(KeyAccessToken);

        IsLoggedIn = hasServerSession || canImportLegacySession;
        if (!IsLoggedIn)
        {
            ClearInMemory();
            return;
        }

        if (hasServerSession)
        {
            UserId = PlayerPrefs.GetInt(ServerKey(KeyUserId));
            Username = PlayerPrefs.GetString(ServerKey(KeyUsername), "");
            Email = PlayerPrefs.GetString(ServerKey(KeyEmail), "");
            AccessToken = PlayerPrefs.GetString(ServerKey(KeyAccessToken), "");
            IsPremium = PlayerPrefs.GetInt(ServerKey(KeyIsPremium), 0) == 1;
        }
        else
        {
            UserId = PlayerPrefs.GetInt(KeyUserId);
            Username = PlayerPrefs.GetString(KeyUsername, "");
            Email = PlayerPrefs.GetString(KeyEmail, "");
            AccessToken = PlayerPrefs.GetString(KeyAccessToken, "");
            IsPremium = PlayerPrefs.GetInt(KeyIsPremium, 0) == 1;
            SaveCurrentSession();
        }
    }

    public static void SetLoggedIn(AuthUserData user)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.accessToken))
        {
            IsLoggedIn = false;
            Debug.LogError("[AuthSession] Missing JWT token. Login response cannot start a session.");
            return;
        }

        IsLoggedIn = true;
        CurrentServerUrl = NormalizeServerUrl(CurrentServerUrl);
        UserId = user.userId;
        Username = user.username ?? "";
        AccessToken = user.accessToken;

        if (!string.IsNullOrWhiteSpace(user.email))
        {
            Email = user.email;
            IsPremium = user.isPremium;
        }

        SaveCurrentSession();
    }

    public static void UpdateProfile(AuthUserData user)
    {
        if (user == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(user.email))
        {
            Email = user.email;
        }

        IsPremium = user.isPremium;

        PlayerPrefs.SetString(ServerKey(KeyEmail), Email);
        PlayerPrefs.SetInt(ServerKey(KeyIsPremium), IsPremium ? 1 : 0);
        SaveLegacyMirror();
        PlayerPrefs.Save();
    }

    public static void Logout()
    {
        DeleteCurrentServerSession();
        ClearInMemory();
        PlayerPrefs.Save();
    }

    private static void SaveCurrentSession()
    {
        PlayerPrefs.SetString(KeyCurrentServerUrl, CurrentServerUrl);
        PlayerPrefs.SetInt(ServerKey(KeyUserId), UserId);
        PlayerPrefs.SetString(ServerKey(KeyUsername), Username);
        PlayerPrefs.SetString(ServerKey(KeyEmail), Email);
        PlayerPrefs.SetString(ServerKey(KeyAccessToken), AccessToken);
        PlayerPrefs.SetInt(ServerKey(KeyIsPremium), IsPremium ? 1 : 0);
        SaveLegacyMirror();
        PlayerPrefs.Save();
    }

    private static void SaveLegacyMirror()
    {
        PlayerPrefs.SetInt(KeyUserId, UserId);
        PlayerPrefs.SetString(KeyUsername, Username);
        PlayerPrefs.SetString(KeyEmail, Email);
        PlayerPrefs.SetString(KeyAccessToken, AccessToken);
        PlayerPrefs.SetInt(KeyIsPremium, IsPremium ? 1 : 0);
    }

    private static void ClearInMemory()
    {
        IsLoggedIn = false;
        UserId = 0;
        Username = "";
        Email = "";
        AccessToken = "";
        IsPremium = false;
    }

    private static void DeleteCurrentServerSession()
    {
        PlayerPrefs.DeleteKey(ServerKey(KeyUserId));
        PlayerPrefs.DeleteKey(ServerKey(KeyUsername));
        PlayerPrefs.DeleteKey(ServerKey(KeyEmail));
        PlayerPrefs.DeleteKey(ServerKey(KeyAccessToken));
        PlayerPrefs.DeleteKey(ServerKey(KeyIsPremium));
        PlayerPrefs.DeleteKey(KeyUserId);
        PlayerPrefs.DeleteKey(KeyUsername);
        PlayerPrefs.DeleteKey(KeyEmail);
        PlayerPrefs.DeleteKey(KeyAccessToken);
        PlayerPrefs.DeleteKey(KeyIsPremium);
    }

    private static bool HasCurrentServerKey(string key)
    {
        return !string.IsNullOrWhiteSpace(CurrentServerUrl) && PlayerPrefs.HasKey(ServerKey(key));
    }

    private static string ServerKey(string key)
    {
        string serverPart = string.IsNullOrWhiteSpace(CurrentServerUrl)
            ? "default"
            : Uri.EscapeDataString(CurrentServerUrl);
        return $"{key}_{serverPart}";
    }
}
