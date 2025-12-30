using System;
using System.Data.OleDb;
using System.Web;
using System.Web.UI;

public partial class google_oauth_callback : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:ENTRY\",\"message\":\"Callback page loaded\",\"data\":{\"hasCode\":\"" + (!string.IsNullOrEmpty(Request.QueryString["code"])).ToString() + "\",\"hasState\":\"" + (!string.IsNullOrEmpty(Request.QueryString["state"])).ToString() + "\",\"hasError\":\"" + (!string.IsNullOrEmpty(Request.QueryString["error"])).ToString() + "\"},\"hypothesisId\":\"1,2,3,4\"}\n"); } catch { }
        // #endregion
        
        string code = Request.QueryString["code"];
        string state = Request.QueryString["state"];
        string error = Request.QueryString["error"];

        if (!string.IsNullOrEmpty(error))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:ERROR_PATH\",\"message\":\"Error in query string\",\"data\":{\"error\":\"" + (error ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"hypothesisId\":\"1\"}\n"); } catch { }
            // #endregion
            Session.Remove("OAuthState");
            Session.Remove("QuickLoginEmail");
            
            if (error == "access_denied")
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:REDIRECT_ACCESS_DENIED\",\"message\":\"Redirecting to login (access denied)\",\"data\":{},\"hypothesisId\":\"1\"}\n"); } catch { }
                // #endregion
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("ההתחברות בוטלה על ידי המשתמש"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:REDIRECT_ERROR\",\"message\":\"Redirecting to login (error)\",\"data\":{},\"hypothesisId\":\"1\"}\n"); } catch { }
            // #endregion
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בהתחברות עם Google: " + error), false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:NO_CODE\",\"message\":\"No code in query string\",\"data\":{},\"hypothesisId\":\"1\"}\n"); } catch { }
            // #endregion
            Session.Remove("OAuthState");
            Session.Remove("QuickLoginEmail");
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא התקבל קוד הרשאה מ-Google"), false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        string sessionState = Session["OAuthState"]?.ToString();
        if (string.IsNullOrEmpty(sessionState) || sessionState != state)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:STATE_MISMATCH\",\"message\":\"State validation failed\",\"data\":{\"sessionState\":\"" + (sessionState ?? "null") + "\",\"queryState\":\"" + (state ?? "null") + "\"},\"hypothesisId\":\"1\"}\n"); } catch { }
            // #endregion
            Session.Remove("OAuthState");
            Session.Remove("QuickLoginEmail");
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאת אבטחה: State parameter לא תואם"), false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        Session.Remove("OAuthState");
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:STATE_VALID\",\"message\":\"State validated, proceeding with OAuth\",\"data\":{},\"hypothesisId\":\"1\"}\n"); } catch { }
        // #endregion

        try
        {
            GoogleUserInfo userInfo = null;
            try
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_GETUSERINFO\",\"message\":\"Before GetUserInfo call\",\"data\":{\"codeLength\":\"" + (code?.Length ?? 0) + "\"},\"hypothesisId\":\"2\"}\n"); } catch { }
                // #endregion
                userInfo = GoogleOAuthService.GetUserInfo(code);
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:AFTER_GETUSERINFO\",\"message\":\"After GetUserInfo call\",\"data\":{\"hasUserInfo\":\"" + (userInfo != null).ToString() + "\",\"hasId\":\"" + (!string.IsNullOrEmpty(userInfo?.Id)).ToString() + "\",\"hasEmail\":\"" + (!string.IsNullOrEmpty(userInfo?.Email)).ToString() + "\"},\"hypothesisId\":\"2\"}\n"); } catch { }
                // #endregion
            }
            catch (Exception getUserInfoEx)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:GETUSERINFO_EXCEPTION\",\"message\":\"GetUserInfo exception\",\"data\":{\"error\":\"" + getUserInfoEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + getUserInfoEx.GetType().Name + "\"},\"hypothesisId\":\"2\"}\n"); } catch { }
                // #endregion
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בקבלת פרטי משתמש מ-Google: " + getUserInfoEx.Message), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            
            if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:INVALID_USERINFO\",\"message\":\"Invalid user info\",\"data\":{},\"hypothesisId\":\"2\"}\n"); } catch { }
                // #endregion
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן לקבל פרטי משתמש מ-Google"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            bool isNewUser = false;
            try
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_CREATEUSER\",\"message\":\"Before CreateOrUpdateUser call\",\"data\":{\"email\":\"" + (userInfo.Email ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"hypothesisId\":\"3\"}\n"); } catch { }
                // #endregion
                isNewUser = GoogleOAuthService.CreateOrUpdateUser(userInfo);
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:AFTER_CREATEUSER\",\"message\":\"After CreateOrUpdateUser call\",\"data\":{\"isNewUser\":\"" + isNewUser.ToString() + "\"},\"hypothesisId\":\"3\"}\n"); } catch { }
                // #endregion
            }
            catch (Exception createEx)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:CREATEUSER_EXCEPTION\",\"message\":\"CreateOrUpdateUser exception\",\"data\":{\"error\":\"" + createEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + createEx.GetType().Name + "\"},\"hypothesisId\":\"3\"}\n"); } catch { }
                // #endregion
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה ביצירת/עדכון משתמש: " + createEx.Message), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_GETUSERID\",\"message\":\"Before GetUserIdByGoogleId call\",\"data\":{},\"hypothesisId\":\"4\"}\n"); } catch { }
            // #endregion
            int? userId = GoogleOAuthService.GetUserIdByGoogleId(userInfo.Id);
            if (!userId.HasValue)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:GETUSERID_BY_EMAIL\",\"message\":\"GetUserIdByGoogleId returned null, trying email\",\"data\":{},\"hypothesisId\":\"4\"}\n"); } catch { }
                // #endregion
                userId = GoogleOAuthService.GetUserIdByEmail(userInfo.Email);
            }

            if (!userId.HasValue)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:NO_USERID\",\"message\":\"No userId found after create\",\"data\":{},\"hypothesisId\":\"4\"}\n"); } catch { }
                // #endregion
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן למצוא את המשתמש במערכת לאחר יצירה"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_DB_QUERY\",\"message\":\"Before database query for user data\",\"data\":{\"userId\":\"" + userId.Value + "\"},\"hypothesisId\":\"4\"}\n"); } catch { }
            // #endregion
            string connectionString = Connect.GetConnectionString();
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Users WHERE Id=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                    idParam.Value = userId.Value;
                    cmd.Parameters.Add(idParam);
                    
                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:USER_DATA_READ\",\"message\":\"User data read from database\",\"data\":{},\"hypothesisId\":\"4\"}\n"); } catch { }
                            // #endregion
                            string userName = "";
                            string role = "";
                            string userIdStr = "";
                            
                            try
                            {
                                userName = dr["UserName"]?.ToString() ?? "";
                            }
                            catch
                            {
                                try
                                {
                                    userName = dr["username"]?.ToString() ?? "";
                                }
                                catch
                                {
                                    userName = userInfo.Email.Split('@')[0];
                                }
                            }
                            
                            try
                            {
                                role = dr["Role"]?.ToString() ?? "user";
                            }
                            catch
                            {
                                try
                                {
                                    role = dr["role"]?.ToString() ?? "user";
                                }
                                catch
                                {
                                    role = "user";
                                }
                            }
                            
                            try
                            {
                                userIdStr = dr["Id"]?.ToString() ?? userId.Value.ToString();
                            }
                            catch
                            {
                                try
                                {
                                    userIdStr = dr["id"]?.ToString() ?? userId.Value.ToString();
                                }
                                catch
                                {
                                    userIdStr = userId.Value.ToString();
                                }
                            }
                            
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_SESSION_SET\",\"message\":\"Before setting session variables\",\"data\":{},\"hypothesisId\":\"3\"}\n"); } catch { }
                            // #endregion
                            Session["username"] = Connect.FixEncoding(userName);
                            Session["Role"] = Connect.FixEncoding(role);
                            Session["userId"] = userIdStr;
                            Session["loggedIn"] = true;
                            
                            Session.Remove("QuickLoginEmail");
                            
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:BEFORE_EMAIL_SEND\",\"message\":\"Before email send (if new user)\",\"data\":{\"isNewUser\":\"" + isNewUser.ToString() + "\"},\"hypothesisId\":\"3\"}\n"); } catch { }
                            // #endregion
                            if (isNewUser)
                            {
                                try
                                {
                                    EmailService.SendRegistrationEmail(userInfo.Email, userInfo.GivenName ?? "");
                                    // #region agent log
                                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:AFTER_EMAIL_SEND\",\"message\":\"After email send\",\"data\":{},\"hypothesisId\":\"3\"}\n"); } catch { }
                                    // #endregion
                                }
                                catch (Exception emailEx)
                                {
                                    // #region agent log
                                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:EMAIL_SEND_EXCEPTION\",\"message\":\"Email send exception (non-fatal)\",\"data\":{\"error\":\"" + emailEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"hypothesisId\":\"3\"}\n"); } catch { }
                                    // #endregion
                                }
                            }
                            
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:REDIRECT_HOME\",\"message\":\"Redirecting to home.aspx\",\"data\":{},\"hypothesisId\":\"1\"}\n"); } catch { }
                            // #endregion
                            Response.Redirect("home.aspx", false);
                            Context.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }
            }

            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:NO_USER_DATA\",\"message\":\"No user data found in database\",\"data\":{},\"hypothesisId\":\"4\"}\n"); } catch { }
            // #endregion
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן לטעון פרטי משתמש מהמסד נתונים"), false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"location\":\"google-oauth-callback.Page_Load:OUTER_EXCEPTION\",\"message\":\"Outer exception caught\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\"},\"hypothesisId\":\"1,2,3,4\"}\n"); } catch { }
            // #endregion
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה כללית: " + ex.Message), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}

