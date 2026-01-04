using System;
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
                LoggingService.Log("GOOGLE_OAUTH_ERROR", string.Format("Google OAuth error: {0}", error));
                Response.Redirect("login.aspx?error=google_oauth_failed");
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_CODE", "No code parameter in callback");
                Response.Redirect("login.aspx?error=no_code");
                return;
            }

            string sessionState = Session["OAuthState"]?.ToString();
            if (!string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(sessionState) && state != sessionState)
            {
                LoggingService.Log("GOOGLE_OAUTH_STATE_MISMATCH", string.Format("State mismatch - Expected: {0}, Got: {1}", sessionState, state));
                Response.Redirect("login.aspx?error=state_mismatch");
                return;
            }

            LoggingService.Log("GOOGLE_OAUTH_CALLBACK", string.Format("Processing OAuth callback - Code: {0}, State: {1}", code, state));

            GoogleUserInfo userInfo = GoogleOAuthService.GetUserInfo(code);
            
            if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_USERINFO", "Failed to get user info from Google");
                Response.Redirect("login.aspx?error=no_userinfo");
                return;
            }

            LoggingService.Log("GOOGLE_OAUTH_USERINFO", string.Format("Got user info - Email: {0}, Name: {1}", userInfo.Email, userInfo.Name));

            bool isNewUser = GoogleOAuthService.CreateOrUpdateUser(userInfo);
            
            int? userId = GoogleOAuthService.GetUserIdByGoogleId(userInfo.Id);
            if (!userId.HasValue)
            {
                userId = GoogleOAuthService.GetUserIdByEmail(userInfo.Email);
            }

            if (!userId.HasValue)
            {
                LoggingService.Log("GOOGLE_OAUTH_NO_USERID", string.Format("Failed to get user ID - Email: {0}", userInfo.Email));
                Response.Redirect("login.aspx?error=no_userid");
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
                            Session["username"] = dr["UserName"]?.ToString() ?? "";
                            Session["Role"] = dr["Role"]?.ToString() ?? "user";
                            Session["userId"] = userId.Value.ToString();
                            Session["loggedIn"] = true;
                            Session.Remove("OAuthState");

                            LoggingService.Log("GOOGLE_OAUTH_SUCCESS", string.Format("User logged in - Email: {0}, Username: {1}, IsNewUser: {2}", userInfo.Email, Session["username"], isNewUser));

                            Response.Redirect("home.aspx");
                            return;
                        }
                    }
                }
            }

            LoggingService.Log("GOOGLE_OAUTH_USER_NOT_FOUND", string.Format("User not found after creation - Email: {0}, UserId: {1}", userInfo.Email, userId));
            Response.Redirect("login.aspx?error=user_not_found");
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_OAUTH_EXCEPTION", string.Format("Exception in OAuth callback - Error: {0}", ex.Message), ex);
            Response.Redirect("login.aspx?error=exception");
        }
    }
}
