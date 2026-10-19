using System;
using System.Threading;
using System.Web.UI;

public partial class google_oauth_callback : System.Web.UI.Page
{
    // טעינת הדף והפעלת לוגיקה ראשונית
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;

        try
        {
            string code = Request.QueryString["code"];
            string state = Request.QueryString["state"];
            string error = Request.QueryString["error"];
            string purpose = Session["OAuthPurpose"] != null ? Session["OAuthPurpose"].ToString() : "login";

            if (!string.IsNullOrEmpty(error))
            {
                LoggingService.Log("GOOGLE_OAUTH_ERROR", "Google returned error: " + error);
                GoogleOAuthService.ClearOAuthState();
                Session.Remove("OAuthPurpose");
                SafeRedirect(purpose == "calendar_export"
                    ? "exportGoogle.aspx?gcal=denied"
                    : "login.aspx?error=google_denied");
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_CODE", "No code parameter in callback");
                GoogleOAuthService.ClearOAuthState();
                Session.Remove("OAuthPurpose");
                SafeRedirect(purpose == "calendar_export"
                    ? "exportGoogle.aspx?gcal=error&msg=" + Uri.EscapeDataString("לא התקבל קוד מ-Google")
                    : "login.aspx?error=no_code");
                return;
            }

            if (!GoogleOAuthService.ValidateOAuthState(state))
            {
                LoggingService.Log("GOOGLE_OAUTH_STATE_INVALID", "OAuth state missing, expired, or mismatched");
                GoogleOAuthService.ClearOAuthState();
                Session.Remove("OAuthPurpose");
                SafeRedirect(purpose == "calendar_export"
                    ? "exportGoogle.aspx?gcal=error&msg=" + Uri.EscapeDataString("state לא תקין")
                    : "login.aspx?error=state_mismatch");
                return;
            }

            if (purpose == "calendar_export")
            {
                HandleCalendarExport(code);
                return;
            }

            HandleLogin(code);
        }
        catch (ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_EXCEPTION", "Exception in OAuth callback: " + ex.Message, ex);
            try { GoogleOAuthService.ClearOAuthState(); } catch { }
            string purpose = Session["OAuthPurpose"] != null ? Session["OAuthPurpose"].ToString() : "login";
            Session.Remove("OAuthPurpose");
            string detail = Uri.EscapeDataString((ex.Message ?? "").Length > 120 ? (ex.Message ?? "").Substring(0, 120) : (ex.Message ?? ""));
            SafeRedirect(purpose == "calendar_export"
                ? "exportGoogle.aspx?gcal=error&msg=" + detail
                : "login.aspx?error=exception");
        }
    }

    // פונקציה: HandleCalendarExport
    private void HandleCalendarExport(string code)
    {
        if (Session["userId"] == null)
        {
            Session.Remove("OAuthPurpose");
            GoogleOAuthService.ClearOAuthState();
            SafeRedirect("login.aspx?error=exception");
            return;
        }

        int userId = Convert.ToInt32(Session["userId"]);
        GoogleTokenSet tokens = GoogleCalendarService.ExchangeCodeForTokens(code);
        GoogleCalendarService.SaveTokens(userId, tokens);
        GoogleOAuthService.ClearOAuthState();
        Session.Remove("OAuthPurpose");

        if (Session["PendingExportEventId"] != null)
        {
            int pendingId;
            if (int.TryParse(Convert.ToString(Session["PendingExportEventId"]), out pendingId))
            {
                bool ok = GoogleCalendarService.ExportSingleEventToGoogle(userId, pendingId);
                Session.Remove("PendingExportEventId");
                SafeRedirect(ok ? "exportGoogle.aspx?gcal=ok&count=1&failed=0" : "exportGoogle.aspx?gcal=error&msg=" + Uri.EscapeDataString("ייצוא אירוע בודד נכשל"));
                return;
            }
            Session.Remove("PendingExportEventId");
        }

        ExportResult result = GoogleCalendarService.ExportUserEventsToGoogle(userId);
        LoggingService.Log("GOOGLE_CALENDAR_EXPORT_DONE", "Exported events: " + result.Exported);
        SafeRedirect("exportGoogle.aspx?gcal=ok&count=" + result.Exported + "&failed=" + result.Failed);
    }

    // פונקציה: HandleLogin
    private void HandleLogin(string code)
    {
        LoggingService.Log("GOOGLE_OAUTH_CALLBACK", "Processing OAuth callback");

        GoogleUserInfo userInfo = GoogleOAuthService.GetUserInfo(code);

        if (userInfo == null || string.IsNullOrEmpty(userInfo.Email) || string.IsNullOrEmpty(userInfo.Id))
        {
            LoggingService.Log("GOOGLE_OAUTH_NO_USERINFO", "Failed to get user info from Google");
            GoogleOAuthService.ClearOAuthState();
            SafeRedirect("login.aspx?error=no_userinfo");
            return;
        }

        bool isNewUser = GoogleOAuthService.CreateOrUpdateUser(userInfo);

        int? userId = GoogleOAuthService.GetUserIdByGoogleId(userInfo.Id);
        if (!userId.HasValue)
            userId = GoogleOAuthService.GetUserIdByEmail(userInfo.Email);

        if (!userId.HasValue)
        {
            LoggingService.Log("GOOGLE_OAUTH_NO_USERID", "Failed to resolve user id after Google login");
            GoogleOAuthService.ClearOAuthState();
            SafeRedirect("login.aspx?error=no_userid");
            return;
        }

        string connectionString = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT * FROM [Users] WHERE [Id]=?";
            using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", userId.Value);
                using (System.Data.OleDb.OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        string userName = ReadColumn(dr, "UserName", "username");
                        string role = ReadColumn(dr, "Role", "role");
                        if (string.IsNullOrEmpty(role))
                            role = "user";

                        Session["username"] = userName;
                        Session["Role"] = role;
                        Session["userId"] = userId.Value.ToString();
                        Session["loggedIn"] = true;
                        GoogleOAuthService.ClearOAuthState();
                        Session.Remove("OAuthPurpose");

                        LoggingService.Log(
                            "GOOGLE_OAUTH_SUCCESS",
                            string.Format("User logged in via Google. IsNewUser={0}", isNewUser));

                        SafeRedirect(isNewUser ? "home.aspx?newuser=true" : "home.aspx");
                        return;
                    }
                }
            }
        }

        LoggingService.Log("GOOGLE_OAUTH_USER_NOT_FOUND", "User row missing after Google login");
        GoogleOAuthService.ClearOAuthState();
        SafeRedirect("login.aspx?error=user_not_found");
    }

    // פונקציה: ReadColumn
    private static string ReadColumn(System.Data.OleDb.OleDbDataReader dr, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            try
            {
                object value = dr[names[i]];
                if (value != null && value != DBNull.Value)
                    return value.ToString();
            }
            catch (IndexOutOfRangeException)
            {
            }
        }
        return "";
    }

    // פונקציה: SafeRedirect
    private void SafeRedirect(string url)
    {
        Response.Redirect(url, false);
        Context.ApplicationInstance.CompleteRequest();
    }
}

