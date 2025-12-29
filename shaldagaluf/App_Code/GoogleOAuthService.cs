using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Data;
using System.Data.OleDb;
using System.Web.Script.Serialization;
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

        string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_type=code&" +
            $"scope={Uri.EscapeDataString(scope)}&" +
            $"state={Uri.EscapeDataString(state)}&" +
            $"access_type=online&" +
            $"prompt=consent";

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

        string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_type=code&" +
            $"scope={Uri.EscapeDataString(scope)}&" +
            $"state={Uri.EscapeDataString(state)}&" +
            $"access_type=online&" +
            $"prompt=none&" +
            $"login_hint={Uri.EscapeDataString(email)}";

        return authUrl;
    }

    public static GoogleUserInfo GetUserInfo(string code)
    {
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run7",
                hypothesisId = "K",
                location = "GoogleOAuthService.cs:GetUserInfo:entry",
                message = "GetUserInfo entry",
                data = new {
                    codeLength = code?.Length ?? 0,
                    hasCode = !string.IsNullOrEmpty(code)
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch {}
        // #endregion agent log

        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        string redirectUri = GetRedirectUri();

        string tokenUrl = "https://oauth2.googleapis.com/token";
        
        try
        {
            // #region agent log
            try {
                var logData2 = new {
                    sessionId = "debug-session",
                    runId = "run7",
                    hypothesisId = "K",
                    location = "GoogleOAuthService.cs:GetUserInfo:before_token_request",
                    message = "Before token request",
                    data = new {
                        tokenUrl = tokenUrl,
                        hasClientId = !string.IsNullOrEmpty(clientId),
                        hasClientSecret = !string.IsNullOrEmpty(clientSecret),
                        redirectUri = redirectUri
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer2 = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer2.Serialize(logData2) + "\n");
            } catch {}
            // #endregion agent log

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
                    // #region agent log
                    try {
                        var logData3 = new {
                            sessionId = "debug-session",
                            runId = "run7",
                            hypothesisId = "K",
                            location = "GoogleOAuthService.cs:GetUserInfo:before_upload",
                            message = "Before UploadValues",
                            data = new {
                                tokenUrl = tokenUrl
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer3 = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer3.Serialize(logData3) + "\n");
                    } catch {}
                    // #endregion agent log

                    System.Threading.Thread.Sleep(100);
                    responseBytes = client.UploadValues(tokenUrl, "POST", postData);

                    // #region agent log
                    try {
                        var logData4 = new {
                            sessionId = "debug-session",
                            runId = "run7",
                            hypothesisId = "K",
                            location = "GoogleOAuthService.cs:GetUserInfo:after_upload",
                            message = "After UploadValues",
                            data = new {
                                responseBytesLength = responseBytes?.Length ?? 0
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer4 = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer4.Serialize(logData4) + "\n");
                    } catch {}
                    // #endregion agent log
                }
                catch (WebException webEx)
                {
                    // #region agent log
                    try {
                        var logData5 = new {
                            sessionId = "debug-session",
                            runId = "run7",
                            hypothesisId = "K",
                            location = "GoogleOAuthService.cs:GetUserInfo:webexception",
                            message = "WebException in UploadValues",
                            data = new {
                                error = webEx.Message,
                                errorType = webEx.GetType().Name,
                                status = webEx.Status.ToString(),
                                stackTrace = webEx.StackTrace != null ? webEx.StackTrace.Substring(0, Math.Min(500, webEx.StackTrace.Length)) : null
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer5 = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer5.Serialize(logData5) + "\n");
                    } catch {}
                    // #endregion agent log
                    throw new Exception("שגיאה בתקשורת עם Google: " + webEx.Message);
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try {
                        var logData6 = new {
                            sessionId = "debug-session",
                            runId = "run7",
                            hypothesisId = "K",
                            location = "GoogleOAuthService.cs:GetUserInfo:exception",
                            message = "Exception in UploadValues",
                            data = new {
                                error = ex.Message,
                                errorType = ex.GetType().Name,
                                stackTrace = ex.StackTrace != null ? ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length)) : null
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer6 = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer6.Serialize(logData6) + "\n");
                    } catch {}
                    // #endregion agent log
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
            // #region agent log
            try {
                var logData7 = new {
                    sessionId = "debug-session",
                    runId = "run7",
                    hypothesisId = "K",
                    location = "GoogleOAuthService.cs:GetUserInfo:outer_exception",
                    message = "Outer exception in GetUserInfo",
                    data = new {
                        error = ex.Message,
                        errorType = ex.GetType().Name,
                        stackTrace = ex.StackTrace != null ? ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length)) : null
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer7 = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer7.Serialize(logData7) + "\n");
            } catch {}
            // #endregion agent log
            throw new Exception("שגיאה ב-GetUserInfo: " + ex.Message);
        }
    }

    public static int? GetUserIdByGoogleId(string googleId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT id FROM Users WHERE GoogleId=?";
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
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run3",
                hypothesisId = "G",
                location = "GoogleOAuthService.cs:GetUserIdByEmail:entry",
                message = "GetUserIdByEmail entry",
                data = new {
                    email = email,
                    emailLength = email?.Length ?? 0
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch {}
        // #endregion agent log

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT id FROM Users WHERE CStr(email)=?";
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
                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "GoogleOAuthService.cs:GetUserIdByEmail:exception",
                        message = "Exception in GetUserIdByEmail query",
                        data = new {
                            error = ex.Message,
                            sql = sql,
                            email = email
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer.Serialize(logData) + "\n");
                } catch {}
                // #endregion agent log
                throw;
            }
        }
        return null;
    }

    public static void EnsureGoogleIdColumn()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            try
            {
                string checkSql = "SELECT GoogleId FROM Users WHERE 1=0";
                using (OleDbCommand checkCmd = new OleDbCommand(checkSql, conn))
                {
                    checkCmd.ExecuteScalar();
                }
            }
            catch
            {
                string alterSql = "ALTER TABLE Users ADD COLUMN GoogleId TEXT";
                using (OleDbCommand alterCmd = new OleDbCommand(alterSql, conn))
                {
                    alterCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public static bool CreateOrUpdateUser(GoogleUserInfo userInfo)
    {
        EnsureGoogleIdColumn();
        
        int? existingUserId = GetUserIdByGoogleId(userInfo.Id);
        bool isNewUser = false;
        
        if (existingUserId.HasValue)
        {
            string connectionString = Connect.GetConnectionString();
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Users SET email=?, firstName=?, lastName=? WHERE GoogleId=?";
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
                    string sql = "UPDATE Users SET GoogleId=?, firstName=?, lastName=? WHERE id=?";
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
                string connectionString = Connect.GetConnectionString();
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
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
                    try {
                        var logData8 = new {
                            sessionId = "debug-session",
                            runId = "run7",
                            hypothesisId = "K",
                            location = "GoogleOAuthService.cs:CreateOrUpdateUser:before_insert",
                            message = "Before INSERT new user",
                            data = new {
                                userName = userName,
                                firstName = firstName,
                                lastName = lastName,
                                email = userInfo.Email,
                                googleId = userInfo.Id
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer8 = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer8.Serialize(logData8) + "\n");
                    } catch {}
                    // #endregion agent log

                    string sql = "INSERT INTO Users (userName, firstName, lastName, email, [password], gender, yearOfBirth, userId, phonenum, city, GoogleId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    try
                    {
                        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                        {
                            OleDbParameter userNameParam = new OleDbParameter("?", OleDbType.WChar);
                            userNameParam.Value = userName?.Trim() ?? "";
                            cmd.Parameters.Add(userNameParam);
                            
                            OleDbParameter firstNameParam = new OleDbParameter("?", OleDbType.WChar);
                            firstNameParam.Value = firstName ?? "";
                            cmd.Parameters.Add(firstNameParam);
                            
                            OleDbParameter lastNameParam = new OleDbParameter("?", OleDbType.WChar);
                            lastNameParam.Value = lastName ?? "";
                            cmd.Parameters.Add(lastNameParam);
                            
                            OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                            emailParam.Value = userInfo.Email?.Trim() ?? "";
                            cmd.Parameters.Add(emailParam);
                            
                            OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                            passwordParam.Value = "";
                            cmd.Parameters.Add(passwordParam);
                            
                            OleDbParameter genderParam = new OleDbParameter("?", OleDbType.Integer);
                            genderParam.Value = 1;
                            cmd.Parameters.Add(genderParam);
                            
                            OleDbParameter yearOfBirthParam = new OleDbParameter("?", OleDbType.Integer);
                            yearOfBirthParam.Value = DateTime.Now.Year - 25;
                            cmd.Parameters.Add(yearOfBirthParam);
                            
                            OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.WChar);
                            userIdParam.Value = "000000000";
                            cmd.Parameters.Add(userIdParam);
                            
                            OleDbParameter phonenumParam = new OleDbParameter("?", OleDbType.WChar);
                            phonenumParam.Value = "";
                            cmd.Parameters.Add(phonenumParam);
                            
                            OleDbParameter cityParam = new OleDbParameter("?", OleDbType.Integer);
                            cityParam.Value = 7;
                            cmd.Parameters.Add(cityParam);
                            
                            OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                            googleIdParam.Value = userInfo.Id?.Trim() ?? "";
                            cmd.Parameters.Add(googleIdParam);
                            
                            // #region agent log
                            try {
                                var logData9 = new {
                                    sessionId = "debug-session",
                                    runId = "run7",
                                    hypothesisId = "K",
                                    location = "GoogleOAuthService.cs:CreateOrUpdateUser:before_execute",
                                    message = "Before ExecuteNonQuery",
                                    data = new {
                                        sql = sql,
                                        paramCount = cmd.Parameters.Count
                                    },
                                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                                };
                                var serializer9 = new System.Web.Script.Serialization.JavaScriptSerializer();
                                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                    serializer9.Serialize(logData9) + "\n");
                            } catch {}
                            // #endregion agent log
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception insertEx)
                    {
                        // #region agent log
                        try {
                            var logData10 = new {
                                sessionId = "debug-session",
                                runId = "run7",
                                hypothesisId = "K",
                                location = "GoogleOAuthService.cs:CreateOrUpdateUser:insert_exception",
                                message = "Exception in INSERT",
                                data = new {
                                    error = insertEx.Message,
                                    errorType = insertEx.GetType().Name,
                                    sql = sql,
                                    stackTrace = insertEx.StackTrace != null ? insertEx.StackTrace.Substring(0, Math.Min(500, insertEx.StackTrace.Length)) : null
                                },
                                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                            };
                            var serializer10 = new System.Web.Script.Serialization.JavaScriptSerializer();
                            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                serializer10.Serialize(logData10) + "\n");
                        } catch {}
                        // #endregion agent log
                        throw;
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

