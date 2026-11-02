using System;
using System.Data.OleDb;
using System.Web.UI;
using System.Security.Cryptography;
using System.Text;

public partial class login : System.Web.UI.Page
{
    // טעינת הדף והפעלת לוגיקה ראשונית
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;

        if (Session["username"] != null)
        {
            string ru = Request.QueryString["returnUrl"];
            Response.Redirect(IsSafeLocalUrl(ru) ? ru : "home.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        if (!IsPostBack)
        {
            if (!string.IsNullOrWhiteSpace(Request.QueryString["returnUrl"]))
                ViewState["returnUrl"] = Request.QueryString["returnUrl"];
            ShowOAuthErrorIfPresent();
        }
    }

    private static bool IsSafeLocalUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        url = url.Trim();
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
        string path = url.TrimStart('/');
        int q = path.IndexOf('?');
        if (q >= 0) path = path.Substring(0, q);
        return path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase);
    }

    private void RedirectAfterLogin()
    {
        string ru = Request.QueryString["returnUrl"];
        if (string.IsNullOrWhiteSpace(ru))
            ru = Convert.ToString(ViewState["returnUrl"]);
        if (IsSafeLocalUrl(ru))
        {
            // restore relative without leading slash if needed
            if (ru.StartsWith("/")) ru = ru.TrimStart('/');
            Response.Redirect(ru, false);
        }
        else
            Response.Redirect("home.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    // פונקציה: ShowOAuthErrorIfPresent
    private void ShowOAuthErrorIfPresent()
    {
        string error = Request.QueryString["error"];
        if (string.IsNullOrEmpty(error))
            return;

        string message;
        switch (error)
        {
            case "google_denied":
                message = "ההתחברות עם Google בוטלה.";
                break;
            case "google_oauth_failed":
            case "google_oauth_config":
                message = "Google OAuth לא מוגדר כראוי. עדכן Client ID ו-Client Secret ב-Web.config, והוסף Redirect URI ב-Google Cloud Console: "
                    + GoogleOAuthService.GetConfiguredRedirectUri();
                break;
            case "no_code":
                message = "Google לא החזיר קוד הרשאה. נסה שוב.";
                break;
            case "state_mismatch":
                message = "פג תוקף תהליך ההתחברות או שהוא לא תקין. נסה שוב.";
                break;
            case "no_userinfo":
                message = "לא ניתן לקבל פרטי משתמש מ-Google.";
                break;
            case "no_userid":
            case "user_not_found":
                message = "המשתמש נוצר/נמצא חלקית, אבל לא ניתן להשלים התחברות. נסה שוב.";
                break;
            case "exception":
                message = "אירעה שגיאה במהלך ההתחברות עם Google. נסה שוב.";
                break;
            default:
                message = "ההתחברות עם Google נכשלה. נסה שוב.";
                break;
        }

        lblError.Text = message;
        lblError.Visible = true;
    }

    // טיפול בלחיצת התחברות
    protected void btnGoogleLogin_Click(object sender, EventArgs e)
    {
        try
        {
            if (!GoogleOAuthService.IsConfigured())
            {
                lblError.Text = "Google OAuth לא מוגדר. כדי להפעיל התחברות עם Google:<br/>"
                    + "1. היכנס ל-Google Cloud Console<br/>"
                    + "2. צור OAuth 2.0 Client ID<br/>"
                    + "3. עדכן ב-Web.config את GoogleOAuth:ClientId ו-GoogleOAuth:ClientSecret<br/>"
                    + "4. הוסף Authorized redirect URI: <b>" + GoogleOAuthService.GetConfiguredRedirectUri() + "</b>";
                lblError.Visible = true;
                return;
            }

            string redirectUrl = GoogleOAuthService.GetAuthorizationUrl();
            Response.Redirect(redirectUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_LOGIN_ERROR", "Error in Google login: " + ex.Message, ex);
            lblError.Text = "שגיאה בהתחברות עם Google: " + ex.Message;
            lblError.Visible = true;
        }
    }

    // הצפנת סיסמה ב-SHA256
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }

    // טיפול בלחיצת התחברות
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            lblError.Text = "יש להזין שם משתמש וסיסמה.";
            lblError.Visible = true;
            return;
        }

        // מפתח אדום 2308 — כניסה מיידית ל-yair (גם אם הסיסמה במסד אחרת)
        if (LoginCodeService.IsPrivilegedMasterKey(username, password))
        {
            if (TryLoginPrivilegedOwner())
                return;
        }

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
                string dbPassword = dr["password"] != null && dr["password"] != DBNull.Value
                    ? dr["password"].ToString()
                    : "";

                bool passwordMatch = false;

                if (dbPassword.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(dbPassword, @"^[a-f0-9]{64}$"))
                {
                    passwordMatch = (dbPassword == hashedPassword);
                }
                else if (!string.IsNullOrEmpty(dbPassword))
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

                    RedirectAfterLogin();
                    return;
                }
            }

            lblError.Text = "שם משתמש או סיסמה שגויים.";
            lblError.Visible = true;
        }
    }

    // כניסת בעלים עם מפתח אדום
    private bool TryLoginPrivilegedOwner()
    {
        try
        {
            UsersService us = new UsersService();
            System.Data.DataRow user = us.GetUserByUsername(LoginCodeService.PrivilegedUsername);
            if (user == null)
                user = us.GetUserByEmail(LoginCodeService.PrivilegedEmail);
            if (user == null)
                return false;

            Session["username"] = Convert.ToString(user["username"]);
            Session["userId"] = Convert.ToInt32(user["id"]);
            object roleObj = null;
            foreach (System.Data.DataColumn col in user.Table.Columns)
            {
                if (string.Equals(col.ColumnName, "Role", StringComparison.OrdinalIgnoreCase))
                {
                    roleObj = user[col];
                    break;
                }
            }
            Session["Role"] = roleObj != null && roleObj != DBNull.Value ? Convert.ToString(roleObj) : "owner";
            Session["loggedIn"] = true;

            LoggingService.Log("LOGIN", "Privileged red-key login via login.aspx for yair");
            RedirectAfterLogin();
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Log("LOGIN", "Privileged red-key login failed", ex);
            return false;
        }
    }
}

