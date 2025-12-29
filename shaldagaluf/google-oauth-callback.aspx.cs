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
            GoogleUserInfo userInfo = null;
            try
            {
                userInfo = GoogleOAuthService.GetUserInfo(code);
            }
            catch (Exception getUserInfoEx)
            {
                
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

