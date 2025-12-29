using System;
using System.Data.OleDb;
using System.Web;
using System.Web.UI;

public partial class login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        string action = Request.QueryString["action"];
        
        if (action == "google-login")
        {
            StartGoogleOAuth();
            return;
        }

        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        string error = Request.QueryString["error"];
        if (!string.IsNullOrEmpty(error))
        {
            lblError.Text = HttpUtility.UrlDecode(error);
        }
    }

    private void StartGoogleOAuth()
    {
        try
        {
            Session.Clear();
            Session.Abandon();
            
            string authUrl = GoogleOAuthService.GetAuthorizationUrl();
            
            if (string.IsNullOrEmpty(authUrl))
            {
                Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("לא ניתן ליצור כתובת OAuth"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            
            Response.Redirect(authUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בהתחברות עם Google: " + ex.Message), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }

    protected void btnGoogleLogin_Click(object sender, EventArgs e)
    {
        StartGoogleOAuth();
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text.Trim();

        string hashedPassword = PasswordHelper.HashPassword(password);

        string connStr = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connStr))
        {
            conn.Open();

            string sql = "SELECT id, userName, role, [password] FROM Users WHERE userName=?";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userNameParam = new OleDbParameter("?", OleDbType.WChar);
                userNameParam.Value = username?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        string dbPassword = dr["password"]?.ToString() ?? "";
                        
                        bool passwordMatch = false;
                        
                        if (dbPassword.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(dbPassword, @"^[a-f0-9]{64}$"))
                        {
                            passwordMatch = (dbPassword == hashedPassword);
                        }
                        else
                        {
                            passwordMatch = (dbPassword == password);
                            
                            if (passwordMatch)
                            {
                                string updateSql = "UPDATE Users SET [password]=? WHERE id=?";
                                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                                {
                                    OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                                    passwordParam.Value = hashedPassword ?? "";
                                    updateCmd.Parameters.Add(passwordParam);
                                    
                                    updateCmd.Parameters.AddWithValue("?", dr["id"]);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        if (passwordMatch)
                        {
                            Session["username"] = Connect.FixEncoding(dr["userName"].ToString());
                            Session["Role"] = Connect.FixEncoding(dr["role"]?.ToString() ?? "user");
                            Session["userId"] = dr["id"].ToString();
                            Session["loggedIn"] = true;

                            Response.Redirect("home.aspx");
                            return;
                        }
                    }
                }
            }
            
            lblError.Text = "שם משתמש או סיסמה שגויים.";
        }
    }

    protected string GetGoogleAuthUrl()
    {
        try
        {
            return GoogleOAuthService.GetAuthorizationUrl();
        }
        catch
        {
            return "login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בהתחברות עם Google");
        }
    }
    
    protected string GetGoogleQuickLoginUrl(string email)
    {
        try
        {
            return GoogleOAuthService.GetQuickLoginUrl(email);
        }
        catch
        {
            return GetGoogleAuthUrl();
        }
    }
}
