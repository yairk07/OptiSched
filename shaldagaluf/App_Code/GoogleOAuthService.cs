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
        return ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "";
    }

    private static string GetClientSecret()
    {
        return ConfigurationManager.AppSettings["GoogleOAuth:ClientSecret"] ?? "";
    }

    private static string GetRedirectUri()
    {
        string baseUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority;
        return baseUrl + "/google-oauth-callback.aspx";
    }

    public static string GetAuthorizationUrl()
    {
        string clientId = GetClientId();
        string redirectUri = GetRedirectUri();
        
        if (string.IsNullOrEmpty(clientId))
        {
            throw new Exception("Google OAuth Client ID לא מוגדר ב-Web.config");
        }
        
        if (string.IsNullOrEmpty(redirectUri))
        {
            throw new Exception("לא ניתן לקבוע כתובת redirect");
        }
        
        string scope = "openid email profile";
        string state = Guid.NewGuid().ToString();
        HttpContext.Current.Session["OAuthState"] = state;

        string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=" + Uri.EscapeDataString(clientId) + "&" +
            "redirect_uri=" + Uri.EscapeDataString(redirectUri) + "&" +
            "response_type=code&" +
            "scope=" + Uri.EscapeDataString(scope) + "&" +
            "state=" + Uri.EscapeDataString(state) + "&" +
            "access_type=online&" +
            "prompt=select_account";

        return authUrl;
    }

    public static string GetQuickLoginUrl(string email)
    {
        string clientId = GetClientId();
        string redirectUri = GetRedirectUri();
        
        string scope = "openid email profile";
        string state = Guid.NewGuid().ToString();
        HttpContext.Current.Session["OAuthState"] = state;
        HttpContext.Current.Session["QuickLoginEmail"] = email;

        string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=" + Uri.EscapeDataString(clientId) + "&" +
            "redirect_uri=" + Uri.EscapeDataString(redirectUri) + "&" +
            "response_type=code&" +
            "scope=" + Uri.EscapeDataString(scope) + "&" +
            "state=" + Uri.EscapeDataString(state) + "&" +
            "access_type=online&" +
            "prompt=select_account&" +
            "login_hint=" + Uri.EscapeDataString(email);

        return authUrl;
    }

    public static GoogleUserInfo GetUserInfo(string code)
    {
        

        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        string redirectUri = GetRedirectUri();

        string tokenUrl = "https://oauth2.googleapis.com/token";
        
        try
        {
            

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

                byte[] responseBytes = null;
                try
                {
                    

                    System.Threading.Thread.Sleep(100);
                    responseBytes = client.UploadValues(tokenUrl, "POST", postData);

                    
                }
                catch (WebException webEx)
                {
                    
                    throw new Exception("שגיאה בתקשורת עם Google: " + webEx.Message);
                }
                catch (Exception ex)
                {
                    
                    throw new Exception("שגיאה בקבלת token: " + ex.Message);
                }

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
                    
                    string userInfoJson = null;
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        userInfoJson = userInfoClient.DownloadString(userInfoUrl);
                    }
                    catch (WebException webEx)
                    {
                        throw new Exception("שגיאה בקבלת פרטי משתמש מ-Google: " + webEx.Message);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("שגיאה בקבלת פרטי משתמש: " + ex.Message);
                    }

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
        catch (Exception ex)
        {
            
            throw new Exception("שגיאה ב-GetUserInfo: " + ex.Message);
        }
    }

    public static int? GetUserIdByGoogleId(string googleId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT Id FROM Users WHERE GoogleId=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                googleIdParam.Value = googleId?.Trim() ?? "";
                cmd.Parameters.Add(googleIdParam);
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
        

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            // DSD Schema: Use Id and Email columns
            string sql = "SELECT Id FROM Users WHERE CStr(Email)=?";
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                    emailParam.Value = email?.Trim() ?? "";
                    cmd.Parameters.Add(emailParam);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }
        return null;
    }

    // DSD Schema: GoogleId is a standard column, no dynamic creation needed
    // Removed EnsureGoogleIdColumn() - column must exist in database schema

    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            // Try multiple variations of column name (case-insensitive check)
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        cmd.ExecuteScalar();
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.ColumnExists\",\"message\":\"Column found\",\"data\":{\"tableName\":\"" + tableName + "\",\"columnName\":\"" + columnName + "\",\"variant\":\"" + variant + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                        // #endregion
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.ColumnExists\",\"message\":\"Column not found\",\"data\":{\"tableName\":\"" + tableName + "\",\"columnName\":\"" + columnName + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            return false;
        }
        catch (Exception ex)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.ColumnExists\",\"message\":\"ColumnExists exception\",\"data\":{\"tableName\":\"" + tableName + "\",\"columnName\":\"" + columnName + "\",\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            return false;
        }
    }

    public static bool CreateOrUpdateUser(GoogleUserInfo userInfo)
    {
        // DSD Schema: GoogleId column is predefined, no need to ensure it exists
        
        int? existingUserId = GetUserIdByGoogleId(userInfo.Id);
        bool isNewUser = false;
        
        if (existingUserId.HasValue)
        {
            string connectionString = Connect.GetConnectionString();
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Users SET Email=?, FirstName=?, LastName=? WHERE GoogleId=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                    emailParam.Value = userInfo.Email?.Trim() ?? "";
                    cmd.Parameters.Add(emailParam);
                    
                    OleDbParameter firstNameParam = new OleDbParameter("?", OleDbType.WChar);
                    firstNameParam.Value = (userInfo.GivenName ?? "").Trim();
                    cmd.Parameters.Add(firstNameParam);
                    
                    OleDbParameter lastNameParam = new OleDbParameter("?", OleDbType.WChar);
                    lastNameParam.Value = (userInfo.FamilyName ?? "").Trim();
                    cmd.Parameters.Add(lastNameParam);
                    
                    OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                    googleIdParam.Value = userInfo.Id?.Trim() ?? "";
                    cmd.Parameters.Add(googleIdParam);
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
                    // DSD Schema: Use FirstName, LastName, Id columns
                    string sql = "UPDATE Users SET GoogleId=?, FirstName=?, LastName=? WHERE Id=?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                        googleIdParam.Value = userInfo.Id?.Trim() ?? "";
                        cmd.Parameters.Add(googleIdParam);
                        
                        OleDbParameter firstNameParam = new OleDbParameter("?", OleDbType.WChar);
                        firstNameParam.Value = (userInfo.GivenName ?? "").Trim();
                        cmd.Parameters.Add(firstNameParam);
                        
                        OleDbParameter lastNameParam = new OleDbParameter("?", OleDbType.WChar);
                        lastNameParam.Value = (userInfo.FamilyName ?? "").Trim();
                        cmd.Parameters.Add(lastNameParam);
                        
                        OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                        idParam.Value = emailUserId.Value;
                        cmd.Parameters.Add(idParam);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                    isNewUser = true;
                    string userName = userInfo.Email.Split('@')[0];
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
                    
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.CreateOrUpdateUser:USING_USERSERVICE\",\"message\":\"Using UsersService.insertIntoDB instead of manual INSERT\",\"data\":{\"email\":\"" + (userInfo.Email ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                    
                    try
                    {
                        UsersService us = new UsersService();
                        us.insertIntoDB(
                            userName,
                            firstName,
                            lastName,
                            userInfo.Email,
                            "", // Empty password for Google users
                            1, // Default gender
                            DateTime.Now.Year - 25, // Default year of birth
                            "000000000", // Default userId
                            "", // Empty phone
                            7 // Default city (Tel Aviv)
                        );
                        
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.CreateOrUpdateUser:USERSERVICE_SUCCESS\",\"message\":\"UsersService.insertIntoDB success, now updating GoogleId\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                        // #endregion
                        
                        string connectionString = Connect.GetConnectionString();
                        using (OleDbConnection conn = new OleDbConnection(connectionString))
                        {
                            conn.Open();
                            bool hasGoogleIdColumn = ColumnExists(conn, "Users", "GoogleId");
                            if (hasGoogleIdColumn && !string.IsNullOrEmpty(userInfo.Id?.Trim()))
                            {
                                int? newUserId = GetUserIdByEmail(userInfo.Email);
                                if (newUserId.HasValue)
                                {
                                    string updateSql = "UPDATE Users SET GoogleId=? WHERE Id=?";
                                    using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                                    {
                                        OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                                        googleIdParam.Value = userInfo.Id.Trim();
                                        updateCmd.Parameters.Add(googleIdParam);
                                        
                                        OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                                        idParam.Value = newUserId.Value;
                                        updateCmd.Parameters.Add(idParam);
                                        
                                        updateCmd.ExecuteNonQuery();
                                        // #region agent log
                                        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.CreateOrUpdateUser:UPDATE_GOOGLEID\",\"message\":\"GoogleId updated successfully\",\"data\":{\"userId\":\"" + newUserId.Value + "\",\"googleId\":\"" + userInfo.Id.Trim() + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                                        // #endregion
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception insertEx)
                    {
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"GoogleOAuthService.CreateOrUpdateUser:USERSERVICE_EXCEPTION\",\"message\":\"UsersService.insertIntoDB exception\",\"data\":{\"error\":\"" + insertEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + insertEx.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                        // #endregion
                        throw;
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

