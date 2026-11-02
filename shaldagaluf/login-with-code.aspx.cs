using System;
using System.Web.UI;

public partial class login_with_code : System.Web.UI.Page
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
            Response.Redirect("home.aspx");
            return;
        }

        if (!IsPostBack)
        {
            pnlRequestCode.Visible = true;
            pnlVerifyCode.Visible = false;
        }
    }

    // פונקציה: FriendlyEmailError
    private string FriendlyEmailError(Exception ex)
    {
        string msg = ex != null ? (ex.Message ?? "") : "";
        if (msg.IndexOf("SMTP", StringComparison.OrdinalIgnoreCase) >= 0 ||
            msg.IndexOf("Web.config", StringComparison.OrdinalIgnoreCase) >= 0 ||
            msg.IndexOf("mail", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "\u05E9\u05DC\u05D9\u05D7\u05EA \u05D4\u05DE\u05D9\u05D9\u05DC \u05DC\u05D0 \u05D4\u05E6\u05DC\u05D9\u05D7\u05D4. \u05D1\u05D3\u05D5\u05E7 \u05D4\u05D2\u05D3\u05E8\u05D5\u05EA SMTP \u05D1-Web.config \u05D0\u05D5 \u05E0\u05E1\u05D4 \u05D4\u05EA\u05D7\u05D1\u05E8\u05D5\u05EA \u05E2\u05DD \u05E1\u05D9\u05E1\u05DE\u05D4.";
        }
        return msg;
    }

    // התאמת ממשק הזנת הקוד: מפתח אדום (4) ל-yair, 6 ספרות לאחרים
    private void ApplyCodeUiForAccount(string email)
    {
        bool vip = LoginCodeService.IsPrivilegedShortCodeAccount(email);
        ViewState["IsVipShortCode"] = vip;

        if (vip)
        {
            txtCode.MaxLength = LoginCodeService.PrivilegedCodeLength;
            txtCode.Attributes["placeholder"] = "0000";
            lblCodeHint.Text = "הזן את המפתח האדום (4 ספרות) — שמור לבעלים בלבד";
            lblCodeHint.CssClass = "code-hint vip";
        }
        else
        {
            txtCode.MaxLength = LoginCodeService.StandardCodeLength;
            txtCode.Attributes["placeholder"] = "000000";
            lblCodeHint.Text = "הזן את הקוד בן 6 הספרות שנשלח לאימייל שלך";
            lblCodeHint.CssClass = "code-hint";
        }
    }

    // שליחת הודעה/בקשה
    protected void btnSendCode_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "\u05D0\u05E0\u05D0 \u05D4\u05D6\u05DF \u05DB\u05EA\u05D5\u05D1\u05EA \u05D0\u05D9\u05DE\u05D9\u05D9\u05DC";
            lblMessage.CssClass = "auth-error";
            return;
        }

        try
        {
            bool vip = LoginCodeService.IsPrivilegedShortCodeAccount(email);
            string code;

            if (vip)
            {
                // ל-yair: בלי SMTP בכלל — מיד למסך המפתח האדום
                try { code = LoginCodeService.GenerateCode(email); }
                catch (Exception genEx)
                {
                    LoggingService.Log("login-with-code", "VIP GenerateCode skipped/failed; red-key still allowed", genEx);
                    code = LoginCodeService.PrivilegedFixedCode;
                }

                ViewState["Email"] = email;
                ApplyCodeUiForAccount(email);
                pnlRequestCode.Visible = false;
                pnlVerifyCode.Visible = true;
                lblCodeMessage.Text = "המפתח האדום מוכן — הזן 4 ספרות.";
                lblCodeMessage.CssClass = "auth-success";
                return;
            }

            code = LoginCodeService.GenerateCode(email);
            EmailService emailService = new EmailService();
            emailService.SendLoginCode(email, code);

            ViewState["Email"] = email;
            ApplyCodeUiForAccount(email);
            pnlRequestCode.Visible = false;
            pnlVerifyCode.Visible = true;
            lblCodeMessage.Text = "קוד נשלח לאימייל שלך. אנא בדוק את תיבת הדואר הנכנס.";
            lblCodeMessage.CssClass = "auth-success";
        }
        catch (InvalidOperationException ex)
        {
            lblMessage.Text = FriendlyEmailError(ex);
            lblMessage.CssClass = "auth-error";
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error sending code", ex);
            lblMessage.Text = FriendlyEmailError(ex);
            lblMessage.CssClass = "auth-error";
        }
    }

    // טיפול בלחיצת כפתור VerifyCode
    protected void btnVerifyCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["Email"] != null ? ViewState["Email"].ToString() : txtEmail.Text.Trim().ToLower();
        string code = txtCode.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblCodeMessage.Text = "\u05E9\u05D2\u05D9\u05D0\u05D4: \u05DB\u05EA\u05D5\u05D1\u05EA \u05D0\u05D9\u05DE\u05D9\u05D9\u05DC \u05DC\u05D0 \u05E0\u05DE\u05E6\u05D0\u05D4";
            lblCodeMessage.CssClass = "auth-error";
            return;
        }

        if (!LoginCodeService.IsCodeFormatValidForAccount(email, code))
        {
            int expected = LoginCodeService.GetExpectedCodeLength(email);
            lblCodeMessage.Text = expected == LoginCodeService.PrivilegedCodeLength
                ? "לא ניתן לאמת: יש להזין את המפתח האדום (4 ספרות) בדיוק כפי שנשמר לבעלים."
                : "לא ניתן לאמת: יש להזין קוד בן 6 ספרות (קוד קצר שמור למשתמש מורשה בלבד).";
            lblCodeMessage.CssClass = "auth-error";
            return;
        }

        try
        {
            bool isValid = LoginCodeService.ValidateCode(email, code);

            if (isValid)
            {
                UsersService us = new UsersService();
                System.Data.DataRow user = us.GetUserByEmail(email);

                // גיבוי: אם חיפוש לפי אימייל נכשל — נסה לפי שם yair למפתח האדום
                if (user == null && LoginCodeService.IsPrivilegedMasterKey(email, code))
                    user = us.GetUserByUsername(LoginCodeService.PrivilegedUsername);

                if (user != null)
                {
                    int userId = Convert.ToInt32(user["id"]);
                    string username = GetRowString(user, "username", "userName", "UserName");
                    if (string.IsNullOrWhiteSpace(username)) username = email;
                    string role = GetRowString(user, "Role", "role");
                    if (string.IsNullOrWhiteSpace(role)) role = "user";

                    Session["username"] = username;
                    Session["userId"] = userId;
                    Session["Role"] = role;
                    Session["loggedIn"] = true;

                    Response.Redirect("home.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    lblCodeMessage.Text = "\u05DE\u05E9\u05EA\u05DE\u05E9 \u05DC\u05D0 \u05E0\u05DE\u05E6\u05D0 \u05D1\u05DE\u05E2\u05E8\u05DB\u05EA";
                    lblCodeMessage.CssClass = "auth-error";
                }
            }
            else
            {
                lblCodeMessage.Text = "\u05E7\u05D5\u05D3 \u05DC\u05D0 \u05EA\u05E7\u05D9\u05DF \u05D0\u05D5 \u05E9\u05E4\u05D2 \u05EA\u05D5\u05E7\u05E4\u05D5. \u05D0\u05E0\u05D0 \u05E0\u05E1\u05D4 \u05E9\u05D5\u05D1 \u05D0\u05D5 \u05D1\u05E7\u05E9 \u05E7\u05D5\u05D3 \u05D7\u05D3\u05E9.";
                lblCodeMessage.CssClass = "auth-error";
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error verifying code", ex);
            lblCodeMessage.Text = "\u05D0\u05D9\u05E8\u05E2\u05D4 \u05E9\u05D2\u05D9\u05D0\u05D4 \u05D1\u05D0\u05D9\u05DE\u05D5\u05EA \u05D4\u05E7\u05D5\u05D3. \u05D0\u05E0\u05D0 \u05E0\u05E1\u05D4 \u05E9\u05D5\u05D1 \u05DE\u05D0\u05D5\u05D7\u05E8 \u05D9\u05D5\u05EA\u05E8.";
            lblCodeMessage.CssClass = "auth-error";
        }
    }

    // טיפול בלחיצת קישור ResendCode
    protected void lnkResendCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["Email"] != null ? ViewState["Email"].ToString() : txtEmail.Text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            lblCodeMessage.Text = "\u05E9\u05D2\u05D9\u05D0\u05D4: \u05DB\u05EA\u05D5\u05D1\u05EA \u05D0\u05D9\u05DE\u05D9\u05D9\u05DC \u05DC\u05D0 \u05E0\u05DE\u05E6\u05D0\u05D4";
            lblCodeMessage.CssClass = "auth-error";
            return;
        }

        try
        {
            bool vip = LoginCodeService.IsPrivilegedShortCodeAccount(email);
            if (vip)
            {
                try { LoginCodeService.GenerateCode(email); } catch { /* המפתח הקבוע עדיין תקף */ }
                ApplyCodeUiForAccount(email);
                lblCodeMessage.Text = "המפתח האדום מוכן — הזן 4 ספרות.";
                lblCodeMessage.CssClass = "auth-success";
                txtCode.Text = "";
                return;
            }

            string code = LoginCodeService.GenerateCode(email);
            EmailService emailService = new EmailService();
            emailService.SendLoginCode(email, code);

            ApplyCodeUiForAccount(email);
            lblCodeMessage.Text = "קוד חדש נשלח לאימייל שלך.";
            lblCodeMessage.CssClass = "auth-success";
            txtCode.Text = "";
        }
        catch (InvalidOperationException ex)
        {
            lblCodeMessage.Text = FriendlyEmailError(ex);
            lblCodeMessage.CssClass = "auth-error";
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error resending code", ex);
            lblCodeMessage.Text = FriendlyEmailError(ex);
            lblCodeMessage.CssClass = "auth-error";
        }
    }

    private static string GetRowString(System.Data.DataRow row, params string[] names)
    {
        if (row == null || row.Table == null) return null;
        foreach (string name in names)
        {
            foreach (System.Data.DataColumn col in row.Table.Columns)
            {
                if (string.Equals(col.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                {
                    object v = row[col];
                    if (v == null || v == DBNull.Value) return null;
                    return Convert.ToString(v);
                }
            }
        }
        return null;
    }
}
