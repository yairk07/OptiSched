using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.OleDb;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

public class GoogleOAuthService
{
    private const string PlaceholderClientId = "YOUR_CLIENT_ID_HERE";
    private const string PlaceholderClientSecret = "YOUR_CLIENT_SECRET_HERE";

    // בדיקה אם הגדרות Google OAuth מוגדרות
    public static bool IsConfigured()
    {
        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        return IsRealCredential(clientId, PlaceholderClientId)
            && IsRealCredential(clientSecret, PlaceholderClientSecret);
    }

    // החזרת Redirect URI מההגדרות
    public static string GetConfiguredRedirectUri()
    {
        return GetRedirectUri();
    }

    // בדיקת תנאי: IsRealCredential
    private static bool IsRealCredential(string value, string placeholder)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (string.Equals(trimmed, placeholder, StringComparison.OrdinalIgnoreCase))
            return false;

        if (trimmed.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    // שליפת נתונים: GetClientId
    private static string GetClientId()
    {
        return (ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "").Trim();
    }

    // שליפת נתונים: GetClientSecret
    private static string GetClientSecret()
    {
        return (ConfigurationManager.AppSettings["GoogleOAuth:ClientSecret"] ?? "").Trim();
    }

    // שליפת נתונים: GetRedirectUri
    private static string GetRedirectUri()
    {
        string configured = (ConfigurationManager.AppSettings["GoogleOAuth:RedirectUri"] ?? "").Trim();
        if (!string.IsNullOrEmpty(configured))
            return configured;

        if (HttpContext.Current == null || HttpContext.Current.Request == null || HttpContext.Current.Request.Url == null)
            throw new InvalidOperationException("HttpContext לא זמין לחישוב כתובת redirect");

        Uri url = HttpContext.Current.Request.Url;
        string appPath = HttpContext.Current.Request.ApplicationPath ?? "/";
        if (appPath == "/")
            appPath = "";
        else
            appPath = appPath.TrimEnd('/');

        return url.Scheme + "://" + url.Authority + appPath + "/google-oauth-callback.aspx";
    }

    // פונקציה: EnsureTls12
    private static void EnsureTls12()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        catch
        {
            // Older runtimes may not expose all flags; continue with defaults.
        }
    }

    // פונקציה: EnsureTls12Public
    public static void EnsureTls12Public()
    {
        EnsureTls12();
    }

    // בניית כתובת התחברות Google OAuth
    public static string GetAuthorizationUrl()
    {
        if (!IsConfigured())
            throw new Exception("Google OAuth לא מוגדר. עדכן GoogleOAuth:ClientId ו-GoogleOAuth:ClientSecret ב-Web.config.");

        string clientId = GetClientId();
        string redirectUri = GetRedirectUri();
        string state = Guid.NewGuid().ToString("N");

        if (HttpContext.Current == null || HttpContext.Current.Session == null)
            throw new Exception("Session לא זמין. לא ניתן להתחיל התחברות מאובטחת עם Google.");

        HttpContext.Current.Session["OAuthState"] = state;
        HttpContext.Current.Session["OAuthStartedUtc"] = DateTime.UtcNow;
        SaveOAuthStateCookie(state);

        string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=" + Uri.EscapeDataString(clientId) + "&" +
            "redirect_uri=" + Uri.EscapeDataString(redirectUri) + "&" +
            "response_type=code&" +
            "scope=" + Uri.EscapeDataString("openid email profile") + "&" +
            "state=" + Uri.EscapeDataString(state) + "&" +
            "access_type=online&" +
            "include_granted_scopes=true&" +
            "prompt=select_account";

        LoggingService.Log("GOOGLE_OAUTH_URL_GENERATED", "Authorization URL generated");
        return authUrl;
    }

    // שמירה/עדכון במסד: SaveOAuthStateCookie
    private static void SaveOAuthStateCookie(string state)
    {
        if (HttpContext.Current == null || HttpContext.Current.Response == null)
            return;

        HttpCookie cookie = new HttpCookie("OptiSchedOAuthState", state);
        cookie.HttpOnly = true;
        cookie.Secure = HttpContext.Current.Request.IsSecureConnection;
        cookie.Path = "/";
        cookie.Expires = DateTime.UtcNow.AddMinutes(10);
        try { cookie.SameSite = SameSiteMode.Lax; } catch { }
        HttpContext.Current.Response.Cookies.Set(cookie);
    }

    // פונקציה: ReadOAuthStateCookie
    private static string ReadOAuthStateCookie()
    {
        if (HttpContext.Current == null || HttpContext.Current.Request == null)
            return null;

        HttpCookie cookie = HttpContext.Current.Request.Cookies["OptiSchedOAuthState"];
        return cookie != null ? cookie.Value : null;
    }

    // אימות state של OAuth למניעת CSRF
    public static bool ValidateOAuthState(string returnedState)
    {
        if (string.IsNullOrEmpty(returnedState) || HttpContext.Current == null)
            return false;

        string expected = null;
        if (HttpContext.Current.Session != null)
            expected = HttpContext.Current.Session["OAuthState"] as string;

        if (string.IsNullOrEmpty(expected))
            expected = ReadOAuthStateCookie();

        if (string.IsNullOrEmpty(expected))
            return false;

        if (HttpContext.Current.Session != null)
        {
            object startedObj = HttpContext.Current.Session["OAuthStartedUtc"];
            if (startedObj is DateTime)
            {
                DateTime started = (DateTime)startedObj;
                if (DateTime.UtcNow - started > TimeSpan.FromMinutes(10))
                    return false;
            }
        }

        return string.Equals(expected, returnedState, StringComparison.Ordinal);
    }

    // פונקציה: ClearOAuthState
    public static void ClearOAuthState()
    {
        if (HttpContext.Current != null && HttpContext.Current.Session != null)
        {
            HttpContext.Current.Session.Remove("OAuthState");
            HttpContext.Current.Session.Remove("OAuthStartedUtc");
        }

        if (HttpContext.Current != null && HttpContext.Current.Response != null)
        {
            HttpCookie expired = new HttpCookie("OptiSchedOAuthState", "");
            expired.HttpOnly = true;
            expired.Path = "/";
            expired.Expires = DateTime.UtcNow.AddDays(-1);
            HttpContext.Current.Response.Cookies.Set(expired);
        }
    }

    // שליפת פרטי משתמש מ-Google
    public static GoogleUserInfo GetUserInfo(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("קוד הרשאה מ-Google חסר");

        if (!IsConfigured())
            throw new Exception("Google OAuth לא מוגדר ב-Web.config");

        EnsureTls12();

        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        string redirectUri = GetRedirectUri();
        string tokenUrl = "https://oauth2.googleapis.com/token";

        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

            NameValueCollection postData = new NameValueCollection();
            postData["code"] = code;
            postData["client_id"] = clientId;
            postData["client_secret"] = clientSecret;
            postData["redirect_uri"] = redirectUri;
            postData["grant_type"] = "authorization_code";

            byte[] responseBytes;
            try
            {
                responseBytes = client.UploadValues(tokenUrl, "POST", postData);
            }
            catch (WebException webEx)
            {
                string body = ReadWebExceptionBody(webEx);
                LoggingService.Log("GOOGLE_OAUTH_TOKEN_ERROR", "Token exchange failed: " + body, webEx);
                throw new Exception("החלפת קוד ההרשאה עם Google נכשלה. ודא ש-Client ID/Secret ו-Redirect URI תואמים ל-Google Cloud Console.");
            }

            if (responseBytes == null || responseBytes.Length == 0)
                throw new Exception("תגובה ריקה מ-Google");

            string response = Encoding.UTF8.GetString(responseBytes);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var tokenResponse = serializer.Deserialize<Dictionary<string, object>>(response);

            string accessToken = tokenResponse.ContainsKey("access_token") ? Convert.ToString(tokenResponse["access_token"]) : "";
            if (string.IsNullOrEmpty(accessToken))
            {
                string error = tokenResponse.ContainsKey("error") ? Convert.ToString(tokenResponse["error"]) : "unknown";
                throw new Exception("לא ניתן לקבל access token מ-Google: " + error);
            }

            using (WebClient userInfoClient = new WebClient())
            {
                userInfoClient.Encoding = Encoding.UTF8;
                userInfoClient.Headers.Clear();
                userInfoClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;

                string userInfoJson;
                try
                {
                    userInfoJson = userInfoClient.DownloadString("https://www.googleapis.com/oauth2/v2/userinfo");
                }
                catch (WebException webEx)
                {
                    LoggingService.Log("GOOGLE_OAUTH_USERINFO_ERROR", "UserInfo request failed", webEx);
                    throw new Exception("לא ניתן לקבל פרטי משתמש מ-Google.");
                }

                if (string.IsNullOrEmpty(userInfoJson))
                    throw new Exception("תגובה ריקה מ-Google UserInfo API");

                var userInfo = serializer.Deserialize<Dictionary<string, object>>(userInfoJson);

                bool verified = true;
                if (userInfo.ContainsKey("verified_email") && userInfo["verified_email"] != null)
                {
                    object verifiedObj = userInfo["verified_email"];
                    if (verifiedObj is bool)
                        verified = (bool)verifiedObj;
                    else
                        bool.TryParse(Convert.ToString(verifiedObj), out verified);
                }

                return new GoogleUserInfo
                {
                    Id = userInfo.ContainsKey("id") ? Convert.ToString(userInfo["id"]) : "",
                    Email = userInfo.ContainsKey("email") ? Convert.ToString(userInfo["email"]) : "",
                    Name = userInfo.ContainsKey("name") ? Convert.ToString(userInfo["name"]) : "",
                    GivenName = userInfo.ContainsKey("given_name") ? Convert.ToString(userInfo["given_name"]) : "",
                    FamilyName = userInfo.ContainsKey("family_name") ? Convert.ToString(userInfo["family_name"]) : "",
                    Picture = userInfo.ContainsKey("picture") ? Convert.ToString(userInfo["picture"]) : "",
                    EmailVerified = verified
                };
            }
        }
    }

    // פונקציה: ReadWebExceptionBody
    private static string ReadWebExceptionBody(WebException webEx)
    {
        try
        {
            if (webEx.Response == null)
                return webEx.Message;

            using (var stream = webEx.Response.GetResponseStream())
            {
                if (stream == null)
                    return webEx.Message;
                using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
        catch
        {
            return webEx.Message;
        }
    }

    // שליפת נתונים: GetUserIdByGoogleId
    public static int? GetUserIdByGoogleId(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
            return null;

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT [Id] FROM [Users] WHERE [GoogleId]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", googleId.Trim());
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
            }
        }
        return null;
    }

    // שליפת נתונים: GetUserIdByEmail
    public static int? GetUserIdByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        string connectionString = Connect.GetConnectionString();
        string emailLower = email.Trim().ToLower();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string[] queries = new string[]
            {
                "SELECT [Id] FROM [Users] WHERE LCase([Email])=?",
                "SELECT [Id] FROM [Users] WHERE LCase([email])=?",
                "SELECT [Id] FROM [Users] WHERE [Email]=?",
                "SELECT [Id] FROM [Users] WHERE [email]=?"
            };

            foreach (string sql in queries)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", emailLower);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt32(result);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        return null;
    }

    // בדיקה אם עמודה קיימת בטבלה
    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations =
            {
                columnName,
                columnName.ToLower(),
                columnName.ToUpper(),
                char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower()
            };

            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        cmd.ExecuteScalar();
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // בדיקה אם שם משתמש כבר קיים
    private static bool UsernameExists(OleDbConnection conn, string userName)
    {
        string[] queries =
        {
            "SELECT COUNT(*) FROM [Users] WHERE LCase([UserName])=?",
            "SELECT COUNT(*) FROM [Users] WHERE LCase([userName])=?"
        };

        string lower = (userName ?? "").Trim().ToLower();
        foreach (string sql in queries)
        {
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", lower);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
                        return true;
                }
            }
            catch
            {
                continue;
            }
        }
        return false;
    }

    // פונקציה: BuildUniqueUserName
    private static string BuildUniqueUserName(string email)
    {
        string baseName;
        if (!string.IsNullOrEmpty(email) && email.Contains("@"))
            baseName = email.Split('@')[0];
        else
            baseName = "user" + DateTime.UtcNow.Ticks;

        baseName = System.Text.RegularExpressions.Regex.Replace(baseName ?? "", @"[^a-zA-Z0-9._-]", "");
        if (string.IsNullOrEmpty(baseName))
            baseName = "user";

        if (baseName.Length > 40)
            baseName = baseName.Substring(0, 40);

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string candidate = baseName;
            int suffix = 1;
            while (UsernameExists(conn, candidate) && suffix < 1000)
            {
                string suffixText = suffix.ToString();
                int maxBase = Math.Max(1, 40 - suffixText.Length);
                candidate = (baseName.Length > maxBase ? baseName.Substring(0, maxBase) : baseName) + suffixText;
                suffix++;
            }
            return candidate;
        }
    }

    // ביצוע פעולה: CreateUnusablePassword
    private static string CreateUnusablePassword()
    {
        // Random value so Google accounts cannot be opened with an empty password.
        return "GOOG$" + Guid.NewGuid().ToString("N") + "$" + Guid.NewGuid().ToString("N");
    }

    // ביצוע פעולה: CreateOrUpdateUser
    public static bool CreateOrUpdateUser(GoogleUserInfo userInfo)
    {
        if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
            throw new ArgumentException("פרטי משתמש Google לא תקינים");

        if (!userInfo.EmailVerified)
            throw new Exception("כתובת האימייל בחשבון Google אינה מאומתת.");

        int? existingUserId = GetUserIdByGoogleId(userInfo.Id);
        bool isNewUser = false;

        if (existingUserId.HasValue)
        {
            string connectionString = Connect.GetConnectionString();
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE [Users] SET [Email]=?, [FirstName]=?, [LastName]=? WHERE [GoogleId]=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", (userInfo.Email ?? "").Trim());
                    cmd.Parameters.AddWithValue("?", (userInfo.GivenName ?? "").Trim());
                    cmd.Parameters.AddWithValue("?", (userInfo.FamilyName ?? "").Trim());
                    cmd.Parameters.AddWithValue("?", (userInfo.Id ?? "").Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }
        else
        {
            int? emailUserId = GetUserIdByEmail(userInfo.Email);
            if (emailUserId.HasValue)
            {
                string connectionString = Connect.GetConnectionString();
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    if (!ColumnExists(conn, "Users", "GoogleId"))
                        throw new Exception("עמודת GoogleId חסרה במסד הנתונים.");

                    // Link only; do not overwrite existing profile names blindly.
                    string sql = "UPDATE [Users] SET [GoogleId]=? WHERE [Id]=?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", (userInfo.Id ?? "").Trim());
                        cmd.Parameters.AddWithValue("?", emailUserId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                isNewUser = true;
                string userName = BuildUniqueUserName(userInfo.Email);
                string firstName = (userInfo.GivenName ?? "").Trim();
                string lastName = (userInfo.FamilyName ?? "").Trim();

                if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(userInfo.Name))
                {
                    string[] nameParts = userInfo.Name.Split(new[] { ' ' }, 2);
                    firstName = nameParts[0] ?? "";
                    if (nameParts.Length > 1)
                        lastName = nameParts[1] ?? "";
                }

                if (string.IsNullOrEmpty(firstName))
                    firstName = userName;
                if (string.IsNullOrEmpty(lastName))
                    lastName = "Google";

                UsersService us = new UsersService();
                int newUserId = us.insertIntoDB(
                    userName,
                    firstName,
                    lastName,
                    userInfo.Email,
                    CreateUnusablePassword(),
                    1,
                    DateTime.Now.Year - 25,
                    "",
                    "",
                    1
                );

                if (newUserId <= 0)
                    throw new Exception("יצירת משתמש Google נכשלה.");

                string connectionString = Connect.GetConnectionString();
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    if (!ColumnExists(conn, "Users", "GoogleId"))
                        throw new Exception("עמודת GoogleId חסרה במסד הנתונים.");

                    string updateSql = "UPDATE [Users] SET [GoogleId]=? WHERE [Id]=?";
                    using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                    {
                        updateCmd.Parameters.AddWithValue("?", (userInfo.Id ?? "").Trim());
                        updateCmd.Parameters.AddWithValue("?", newUserId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        return isNewUser;
    }
}

public class GoogleUserInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Picture { get; set; }
    public bool EmailVerified { get; set; }
}

