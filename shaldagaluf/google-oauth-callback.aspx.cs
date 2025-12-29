using System;
using System.Web;
using System.Web.UI;

public partial class google_oauth_callback : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string code = Request.QueryString["code"];
        string state = Request.QueryString["state"];
        string error = Request.QueryString["error"];

        if (!string.IsNullOrEmpty(error))
        {
            string quickLoginEmail = Session["QuickLoginEmail"]?.ToString();
            Session.Remove("QuickLoginEmail");
            
            if (!string.IsNullOrEmpty(quickLoginEmail) && error == "login_required")
            {
                Response.Redirect(GoogleOAuthService.GetAuthorizationUrl());
                return;
            }
            
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("ההתחברות בוטלה"));
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בהתחברות עם Google"));
            return;
        }

        string sessionState = Session["OAuthState"]?.ToString();
        if (string.IsNullOrEmpty(sessionState) || sessionState != state)
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאת אבטחה בהתחברות"));
            return;
        }

        try
        {
            // #region agent log
            try {
                var logData = new {
                    sessionId = "debug-session",
                    runId = "run7",
                    hypothesisId = "K",
                    location = "google-oauth-callback.aspx.cs:Page_Load:before_GetUserInfo",
                    message = "Before calling GetUserInfo",
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

            GoogleUserInfo userInfo = null;
            try
            {
                userInfo = GoogleOAuthService.GetUserInfo(code);
            }
            catch (Exception getUserInfoEx)
            {
                // #region agent log
                try {
                    var logData2 = new {
                        sessionId = "debug-session",
                        runId = "run7",
                        hypothesisId = "K",
                        location = "google-oauth-callback.aspx.cs:Page_Load:GetUserInfo_exception",
                        message = "Exception in GetUserInfo",
                        data = new {
                            error = getUserInfoEx.Message,
                            errorType = getUserInfoEx.GetType().Name,
                            stackTrace = getUserInfoEx.StackTrace != null ? getUserInfoEx.StackTrace.Substring(0, Math.Min(500, getUserInfoEx.StackTrace.Length)) : null
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer2 = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer2.Serialize(logData2) + "\n");
                } catch {}
                // #endregion agent log
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בקבלת פרטי משתמש מ-Google: " + getUserInfoEx.Message));
                return;
            }
            
            if (userInfo == null || string.IsNullOrEmpty(userInfo.Id))
            {
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן לקבל פרטי משתמש מ-Google"));
                return;
            }

            bool isNewUser = false;
            try
            {
                isNewUser = GoogleOAuthService.CreateOrUpdateUser(userInfo);
            }
            catch (Exception createEx)
            {
                // #region agent log
                try {
                    var logData3 = new {
                        sessionId = "debug-session",
                        runId = "run7",
                        hypothesisId = "K",
                        location = "google-oauth-callback.aspx.cs:Page_Load:CreateOrUpdateUser_exception",
                        message = "Exception in CreateOrUpdateUser",
                        data = new {
                            error = createEx.Message,
                            errorType = createEx.GetType().Name,
                            stackTrace = createEx.StackTrace != null ? createEx.StackTrace.Substring(0, Math.Min(500, createEx.StackTrace.Length)) : null
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer3 = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer3.Serialize(logData3) + "\n");
                } catch {}
                // #endregion agent log
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה ביצירת משתמש: " + createEx.Message));
                return;
            }

            int? userId = GoogleOAuthService.GetUserIdByGoogleId(userInfo.Id);
            if (!userId.HasValue)
            {
                userId = GoogleOAuthService.GetUserIdByEmail(userInfo.Email);
            }

            if (!userId.HasValue)
            {
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן למצוא את המשתמש במערכת"));
                return;
            }

            if (userId.HasValue)
            {
                string connectionString = Connect.GetConnectionString();
                using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT userName, role FROM Users WHERE id=?";
                    using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", userId.Value);
                        using (System.Data.OleDb.OleDbDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                Session["username"] = Connect.FixEncoding(dr["userName"].ToString());
                                Session["Role"] = Connect.FixEncoding(dr["role"]?.ToString() ?? "user");
                                Session["userId"] = userId.Value.ToString();
                                Session["loggedIn"] = true;
                                Session.Remove("OAuthState");

                                string userEmail = userInfo.Email;
                                string quickLoginEmail = Session["QuickLoginEmail"]?.ToString();
                                Session.Remove("QuickLoginEmail");
                                
                                Session["GoogleEmailToStore"] = userEmail;
                                
                                if (isNewUser)
                                {
                                    try
                                    {
                                        EmailService.SendRegistrationEmail(userInfo.Email, userInfo.GivenName ?? "");
                                    }
                                    catch
                                    {
                                    }
                                }
                                
                                Response.Redirect("home.aspx");
                                return;
                            }
                        }
                    }
                }
            }

            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה ביצירת משתמש"));
        }
        catch (Exception ex)
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה: " + ex.Message));
        }
    }
}

