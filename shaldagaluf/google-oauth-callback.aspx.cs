using System;
using System.Threading;
using System.Web.UI;

public partial class google_oauth_callback : System.Web.UI.Page
{
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

            if (!string.IsNullOrEmpty(error))
            {
                LoggingService.Log("GOOGLE_OAUTH_ERROR", "Google returned error: " + error);
                GoogleOAuthService.ClearOAuthState();
                SafeRedirect("login.aspx?error=google_denied");
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_CODE", "No code parameter in callback");
                GoogleOAuthService.ClearOAuthState();
                SafeRedirect("login.aspx?error=no_code");
                return;
            }

            if (!GoogleOAuthService.ValidateOAuthState(state))
            {
                LoggingService.Log("GOOGLE_OAUTH_STATE_INVALID", "OAuth state missing, expired, or mismatched");
                GoogleOAuthService.ClearOAuthState();
                SafeRedirect("login.aspx?error=state_mismatch");
                return;
            }

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
                string sql = "SELECT [UserName], [Role] FROM [Users] WHERE [Id]=?";
                using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", userId.Value);
                    using (System.Data.OleDb.OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            Session["username"] = dr["UserName"] != null && dr["UserName"] != DBNull.Value
                                ? dr["UserName"].ToString()
                                : "";
                            Session["Role"] = dr["Role"] != null && dr["Role"] != DBNull.Value
                                ? dr["Role"].ToString()
                                : "user";
                            Session["userId"] = userId.Value.ToString();
                            Session["loggedIn"] = true;
                            GoogleOAuthService.ClearOAuthState();

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
        catch (ThreadAbortException)
        {
            // Expected when Response.Redirect ends the request.
            throw;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_EXCEPTION", "Exception in OAuth callback: " + ex.Message, ex);
            try { GoogleOAuthService.ClearOAuthState(); } catch { }
            SafeRedirect("login.aspx?error=exception");
        }
    }

    private void SafeRedirect(string url)
    {
        Response.Redirect(url, false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
