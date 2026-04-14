using UnityEngine;

[System.Serializable]
public class AuthUserData
{
    public int userId;
    public string username;
    public string email;
    public bool isPremium;
    public string premiumSince;
    public string premiumUntil;
    public string createdAt;
}

public static class AuthSession
{
    private const string KeyUserId = "auth_user_id";
    private const string KeyUsername = "auth_username";
    private const string KeyEmail = "auth_email";
    private const string KeyIsPremium = "auth_is_premium";

    public static bool IsLoggedIn { get; private set; }
    public static int UserId { get; private set; }
    public static string Username { get; private set; } = "";
    public static string Email { get; private set; } = "";
    public static bool IsPremium { get; private set; }

    public static void LoadFromPrefs()
    {
        IsLoggedIn = PlayerPrefs.HasKey(KeyUserId);
        if (!IsLoggedIn)
        {
            return;
        }

        UserId = PlayerPrefs.GetInt(KeyUserId);
        Username = PlayerPrefs.GetString(KeyUsername, "");
        Email = PlayerPrefs.GetString(KeyEmail, "");
        IsPremium = PlayerPrefs.GetInt(KeyIsPremium, 0) == 1;
    }

    public static void SetLoggedIn(AuthUserData user)
    {
        IsLoggedIn = true;
        UserId = user.userId;
        Username = user.username ?? "";
        Email = user.email ?? "";
        IsPremium = user.isPremium;

        PlayerPrefs.SetInt(KeyUserId, UserId);
        PlayerPrefs.SetString(KeyUsername, Username);
        PlayerPrefs.SetString(KeyEmail, Email);
        PlayerPrefs.SetInt(KeyIsPremium, IsPremium ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void Logout()
    {
        IsLoggedIn = false;
        UserId = 0;
        Username = "";
        Email = "";
        IsPremium = false;

        PlayerPrefs.DeleteKey(KeyUserId);
        PlayerPrefs.DeleteKey(KeyUsername);
        PlayerPrefs.DeleteKey(KeyEmail);
        PlayerPrefs.DeleteKey(KeyIsPremium);
        PlayerPrefs.Save();
    }
}
