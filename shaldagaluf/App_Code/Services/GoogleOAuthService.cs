using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.OleDb;
using System.Configuration;

public class GoogleOAuthService
{
    private static string GetClientId()
    {
        try
        {
            string clientId = ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "";
            int length = clientId != null ? clientId.Length : 0;
            LoggingService.Log("GOOGLE_OAUTH_GET_CLIENT_ID", string.Format("ClientId from config - Length: {0}, IsEmpty: {1}", length, string.IsNullOrEmpty(clientId)));
            return clientId;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_GET_CLIENT_ID_ERROR", string.Format("Error getting ClientId: {0}", ex.Message), ex);
            return "";
        }
    }

    private static string GetClientSecret()
    {
        try
        {
            string clientSecret = ConfigurationManager.AppSettings["GoogleOAuth:ClientSecret"] ?? "";
            int length = clientSecret != null ? clientSecret.Length : 0;
            LoggingService.Log("GOOGLE_OAUTH_GET_CLIENT_SECRET", string.Format("ClientSecret from config - Length: {0}, IsEmpty: {1}", length, string.IsNullOrEmpty(clientSecret)));
            return clientSecret;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_GET_CLIENT_SECRET_ERROR", string.Format("Error getting ClientSecret: {0}", ex.Message), ex);
            return "";
        }
    }

    private static string GetRedirectUri()
    {
        try
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null || HttpContext.Current.Request.Url == null)
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_HTTPCONTEXT", "HttpContext is not available");
                throw new InvalidOperationException("HttpContext לא זמין");
            }
            string baseUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority;
            string redirectUri = baseUrl + "/google-oauth-callback.aspx";
            LoggingService.Log("GOOGLE_OAUTH_REDIRECT_URI_BUILT", string.Format("RedirectUri built: {0}", redirectUri));
            return redirectUri;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_GET_REDIRECT_URI_ERROR", string.Format("Error getting RedirectUri: {0}", ex.Message), ex);
            throw;
        }
    }

    public static string GetAuthorizationUrl()
    {
        try
        {
            LoggingService.Log("GOOGLE_OAUTH_GET_AUTH_URL_START", "Starting GetAuthorizationUrl");
            
            string clientId = GetClientId();
            int clientIdLength = clientId != null ? clientId.Length : 0;
            string clientIdPreview = string.IsNullOrEmpty(clientId) ? "EMPTY" : clientId.Substring(0, Math.Min(10, clientId.Length)) + "...";
            LoggingService.Log("GOOGLE_OAUTH_CLIENT_ID", string.Format("ClientId retrieved - Length: {0}, IsEmpty: {1}, Value: {2}", clientIdLength, string.IsNullOrEmpty(clientId), clientIdPreview));
            
            string redirectUri = GetRedirectUri();
            LoggingService.Log("GOOGLE_OAUTH_REDIRECT_URI", string.Format("RedirectUri: {0}", redirectUri));
            
            if (string.IsNullOrEmpty(clientId))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_CLIENT_ID", "Client ID is empty or null - checking all AppSettings keys");
                try
                {
                    var allKeys = System.Configuration.ConfigurationManager.AppSettings.AllKeys;
                    LoggingService.Log("GOOGLE_OAUTH_APP_SETTINGS_KEYS", string.Format("All AppSettings keys: {0}", string.Join(", ", allKeys)));
                    foreach (string key in allKeys)
                    {
                        if (key.Contains("Google") || key.Contains("OAuth"))
                        {
                            string value = System.Configuration.ConfigurationManager.AppSettings[key];
                            int valueLength = value != null ? value.Length : 0;
                            LoggingService.Log("GOOGLE_OAUTH_KEY_FOUND", string.Format("Found key: {0}, Value length: {1}", key, valueLength));
                        }
                    }
                }
                catch (Exception ex2)
                {
                    LoggingService.Log("GOOGLE_OAUTH_CHECK_KEYS_ERROR", string.Format("Error checking AppSettings keys: {0}", ex2.Message), ex2);
                }
                throw new Exception("Google OAuth Client ID לא מוגדר ב-Web.config");
            }
            
            if (string.IsNullOrEmpty(redirectUri))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_REDIRECT_URI", "Redirect URI is empty or null");
                throw new Exception("לא ניתן לקבוע כתובת redirect");
            }
            
            string scope = "openid email profile";
            string state = Guid.NewGuid().ToString();
            LoggingService.Log("GOOGLE_OAUTH_STATE", string.Format("Generated state: {0}", state));
            
            if (HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session["OAuthState"] = state;
                LoggingService.Log("GOOGLE_OAUTH_STATE_SAVED", "State saved to session");
            }
            else
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_SESSION", "Session is not available");
            }

            string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                "client_id=" + Uri.EscapeDataString(clientId) + "&" +
                "redirect_uri=" + Uri.EscapeDataString(redirectUri) + "&" +
                "response_type=code&" +
                "scope=" + Uri.EscapeDataString(scope) + "&" +
                "state=" + Uri.EscapeDataString(state) + "&" +
                "access_type=online&" +
                "prompt=select_account";

            LoggingService.Log("GOOGLE_OAUTH_URL_GENERATED", string.Format("Authorization URL generated successfully - Length: {0}", authUrl.Length));
            return authUrl;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_GET_AUTH_URL_ERROR", string.Format("Error in GetAuthorizationUrl: {0}, StackTrace: {1}", ex.Message, ex.StackTrace), ex);
            throw;
        }
    }

    public static GoogleUserInfo GetUserInfo(string code)
    {
        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        string redirectUri = GetRedirectUri();

        string tokenUrl = "https://oauth2.googleapis.com/token";
        
        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            client.Proxy = null;
            
            NameValueCollection postData = new NameValueCollection();
            postData["code"] = code;
            postData["client_id"] = clientId;
            postData["client_secret"] = clientSecret;
            postData["redirect_uri"] = redirectUri;
            postData["grant_type"] = "authorization_code";

            byte[] responseBytes = client.UploadValues(tokenUrl, "POST", postData);

            if (responseBytes == null || responseBytes.Length == 0)
            {
                throw new Exception("תגובה ריקה מ-Google");
            }

            string response = Encoding.UTF8.GetString(responseBytes);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var tokenResponse = serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(response);
            
            string accessToken = tokenResponse.ContainsKey("access_token") ? tokenResponse["access_token"].ToString() : "";

            if (string.IsNullOrEmpty(accessToken))
            {
                string error = tokenResponse.ContainsKey("error") ? tokenResponse["error"].ToString() : "Unknown error";
                string errorDescription = tokenResponse.ContainsKey("error_description") ? tokenResponse["error_description"].ToString() : "";
                throw new Exception("לא ניתן לקבל access token מ-Google: " + error + (string.IsNullOrEmpty(errorDescription) ? "" : " - " + errorDescription));
            }

            string userInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
            
            using (WebClient userInfoClient = new WebClient())
            {
                userInfoClient.Encoding = Encoding.UTF8;
                userInfoClient.Proxy = null;
                userInfoClient.Headers.Clear();
                userInfoClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;
                
                string userInfoJson = userInfoClient.DownloadString(userInfoUrl);

                if (string.IsNullOrEmpty(userInfoJson))
                {
                    throw new Exception("תגובה ריקה מ-Google UserInfo API");
                }

                var userInfo = serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(userInfoJson);

                return new GoogleUserInfo
                {
                    Id = userInfo.ContainsKey("id") ? userInfo["id"].ToString() : "",
                    Email = userInfo.ContainsKey("email") ? userInfo["email"].ToString() : "",
                    Name = userInfo.ContainsKey("name") ? userInfo["name"].ToString() : "",
                    GivenName = userInfo.ContainsKey("given_name") ? userInfo["given_name"].ToString() : "",
                    FamilyName = userInfo.ContainsKey("family_name") ? userInfo["family_name"].ToString() : "",
                    Picture = userInfo.ContainsKey("picture") ? userInfo["picture"].ToString() : ""
                };
            }
        }
    }

    public static int? GetUserIdByGoogleId(string googleId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT [Id] FROM [Users] WHERE [GoogleId]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                string googleIdValue = googleId != null ? googleId.Trim() : "";
                cmd.Parameters.AddWithValue("?", googleIdValue);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
        }
        return null;
    }

    public static int? GetUserIdByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

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
                        {
                            return Convert.ToInt32(result);
                        }
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

    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
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

    public static bool CreateOrUpdateUser(GoogleUserInfo userInfo)
    {
        if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
        {
            throw new ArgumentException("פרטי משתמש Google לא תקינים");
        }
        
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
                    cmd.Parameters.AddWithValue("?", userInfo.Email != null ? userInfo.Email.Trim() : "");
                    cmd.Parameters.AddWithValue("?", userInfo.GivenName != null ? userInfo.GivenName.Trim() : "");
                    cmd.Parameters.AddWithValue("?", userInfo.FamilyName != null ? userInfo.FamilyName.Trim() : "");
                    cmd.Parameters.AddWithValue("?", userInfo.Id != null ? userInfo.Id.Trim() : "");
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
                    bool hasGoogleIdColumn = ColumnExists(conn, "Users", "GoogleId");
                    if (hasGoogleIdColumn)
                    {
                        string sql = "UPDATE [Users] SET [GoogleId]=?, [FirstName]=?, [LastName]=? WHERE [Id]=?";
                        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", userInfo.Id != null ? userInfo.Id.Trim() : "");
                            cmd.Parameters.AddWithValue("?", userInfo.GivenName != null ? userInfo.GivenName.Trim() : "");
                            cmd.Parameters.AddWithValue("?", userInfo.FamilyName != null ? userInfo.FamilyName.Trim() : "");
                            cmd.Parameters.AddWithValue("?", emailUserId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            else
            {
                isNewUser = true;
                string userName = "";
                if (!string.IsNullOrEmpty(userInfo.Email) && userInfo.Email.Contains("@"))
                {
                    userName = userInfo.Email.Split('@')[0];
                }
                else
                {
                    userName = "user" + DateTime.Now.Ticks;
                }
                
                string firstName = (userInfo.GivenName ?? "").Trim();
                string lastName = (userInfo.FamilyName ?? "").Trim();
                
                if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(userInfo.Name))
                {
                    string[] nameParts = userInfo.Name.Split(new[] { ' ' }, 2);
                    firstName = nameParts[0] ?? "";
                    if (nameParts.Length > 1)
                    {
                        lastName = nameParts[1] ?? "";
                    }
                }
                
                if (string.IsNullOrEmpty(firstName))
                {
                    firstName = userName;
                }
                
                if (string.IsNullOrEmpty(lastName))
                {
                    lastName = "";
                }
                
                UsersService us = new UsersService();
                int newUserId = us.insertIntoDB(
                    userName,
                    firstName,
                    lastName,
                    userInfo.Email,
                    "",
                    1,
                    DateTime.Now.Year - 25,
                    "000000000",
                    "",
                    7
                );
                
                if (newUserId > 0)
                {
                    string connectionString = Connect.GetConnectionString();
                    using (OleDbConnection conn = new OleDbConnection(connectionString))
                    {
                        conn.Open();
                        bool hasGoogleIdColumn = ColumnExists(conn, "Users", "GoogleId");
                        string userInfoIdTrimmed = userInfo.Id != null ? userInfo.Id.Trim() : "";
                        if (hasGoogleIdColumn && !string.IsNullOrEmpty(userInfoIdTrimmed))
                        {
                            string updateSql = "UPDATE [Users] SET [GoogleId]=? WHERE [Id]=?";
                            using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("?", userInfoIdTrimmed);
                                updateCmd.Parameters.AddWithValue("?", newUserId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
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
}
