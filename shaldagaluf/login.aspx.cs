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
            Session.Remove("OAuthState");
            Session.Remove("QuickLoginEmail");
            
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

            // DSD Schema: Use SELECT * to handle both old and new column names during migration
            string sql = "SELECT * FROM Users WHERE (UserName=? OR userName=?)";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userNameParam1 = new OleDbParameter("?", OleDbType.WChar);
                userNameParam1.Value = username?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam1);
                
                OleDbParameter userNameParam2 = new OleDbParameter("?", OleDbType.WChar);
                userNameParam2.Value = username?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam2);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        // DSD Schema: Use PasswordHash column (try new first, fallback to old during migration)
                        string dbPassword = "";
                        try
                        {
                            dbPassword = dr["PasswordHash"]?.ToString() ?? "";
                        }
                        catch
                        {
                            try
                            {
                                dbPassword = dr["password"]?.ToString() ?? "";
                            }
                            catch { }
                        }
                        
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
                                // DSD Schema: Update PasswordHash column
                                int userId = 0;
                                try
                                {
                                    userId = Convert.ToInt32(dr["Id"]);
                                }
                                catch
                                {
                                    userId = Convert.ToInt32(dr["id"]);
                                }
                                
                                string updateSql = "UPDATE Users SET PasswordHash=?, [password]=? WHERE Id=?";
                                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                                {
                                    OleDbParameter passwordHashParam = new OleDbParameter("?", OleDbType.WChar);
                                    passwordHashParam.Value = hashedPassword ?? "";
                                    updateCmd.Parameters.Add(passwordHashParam);
                                    
                                    OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                                    passwordParam.Value = hashedPassword ?? "";
                                    updateCmd.Parameters.Add(passwordParam);
                                    
                                    OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                                    idParam.Value = userId;
                                    updateCmd.Parameters.Add(idParam);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        if (passwordMatch)
                        {
                            // DSD Schema: Use UserName, Role, Id columns (try new first, fallback to old during migration)
                            string userName = "";
                            string role = "";
                            string userIdStr = "";
                            
                            try
                            {
                                userName = dr["UserName"].ToString();
                            }
                            catch
                            {
                                userName = dr["userName"].ToString();
                            }
                            
                            try
                            {
                                role = dr["Role"]?.ToString() ?? "user";
                            }
                            catch
                            {
                                role = dr["role"]?.ToString() ?? "user";
                            }
                            
                            try
                            {
                                userIdStr = dr["Id"].ToString();
                            }
                            catch
                            {
                                userIdStr = dr["id"].ToString();
                            }
                            
                            Session["username"] = Connect.FixEncoding(userName);
                            Session["Role"] = Connect.FixEncoding(role);
                            Session["userId"] = userIdStr;
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
