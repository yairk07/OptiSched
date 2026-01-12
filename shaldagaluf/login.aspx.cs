using System;
using System.Data.OleDb;
using System.Web.UI;
using System.Security.Cryptography;
using System.Text;

public partial class login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }
    }

    protected void btnGoogleLogin_Click(object sender, EventArgs e)
    {
        try
        {
            LoggingService.Log("GOOGLE_LOGIN_CLICK", "Google login button clicked");
            
            string clientId = System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientId"];
            int clientIdLength = clientId != null ? clientId.Length : 0;
            LoggingService.Log("GOOGLE_LOGIN_CHECK_CLIENT_ID", string.Format("Checking ClientId - IsEmpty: {0}, Length: {1}", string.IsNullOrWhiteSpace(clientId), clientIdLength));
            
            if (string.IsNullOrWhiteSpace(clientId))
            {
                LoggingService.Log("GOOGLE_LOGIN_NO_CLIENT_ID", "ClientId is empty - cannot proceed with Google login");
                lblError.Text = "Google OAuth לא מוגדר. כדי להפעיל התחברות עם Google:<br/>1. היכנס ל-Google Cloud Console (https://console.cloud.google.com/)<br/>2. צור OAuth 2.0 Client ID<br/>3. הוסף את הערכים ב-Web.config תחת GoogleOAuth:ClientId ו-GoogleOAuth:ClientSecret<br/>4. הוסף Authorized redirect URI: " + Request.Url.Scheme + "://" + Request.Url.Authority + "/google-oauth-callback.aspx";
                lblError.Visible = true;
                return;
            }
            
            string redirectUrl = GoogleOAuthService.GetAuthorizationUrl();
            int redirectUrlLength = redirectUrl != null ? redirectUrl.Length : 0;
            LoggingService.Log("GOOGLE_LOGIN_REDIRECT", string.Format("Redirecting to Google OAuth - URL length: {0}", redirectUrlLength));
            Response.Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_LOGIN_ERROR", string.Format("Error in Google login: {0}, StackTrace: {1}", ex.Message, ex.StackTrace), ex);
            lblError.Text = "שגיאה בהתחברות עם Google: " + ex.Message;
            lblError.Visible = true;
        }
    }

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text.Trim();

        string hashedPassword = HashPassword(password);

        string connStr = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connStr))
        {
            conn.Open();

            string sql = "SELECT id, userName, role, [password] FROM Users WHERE userName=?";
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            cmd.Parameters.AddWithValue("?", username);

            OleDbDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                string dbPassword = dr["password"] != null && dr["password"] != DBNull.Value ? dr["password"].ToString() : "";
                
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
                        OleDbCommand updateCmd = new OleDbCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("?", hashedPassword);
                        updateCmd.Parameters.AddWithValue("?", dr["id"]);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                if (passwordMatch)
                {
                    Session["username"] = dr["userName"].ToString();
                    Session["Role"] = dr["role"] != null && dr["role"] != DBNull.Value ? dr["role"].ToString() : "user";
                    Session["userId"] = dr["id"].ToString();
                    Session["loggedIn"] = true;

                    Response.Redirect("home.aspx");
                    return;
                }
            }
            
            lblError.Text = "שם משתמש או סיסמה שגויים.";
        }
    }
}
