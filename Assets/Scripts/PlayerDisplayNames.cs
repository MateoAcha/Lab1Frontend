public static class PlayerDisplayNames
{
    public static string LocalUsernameOrFallback(string fallback)
    {
        string username = AuthSession.IsLoggedIn ? AuthSession.Username : "";
        return Normalize(username, fallback);
    }

    public static string Normalize(string username, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(username))
            return username.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? "Player" : fallback.Trim();
    }
}
