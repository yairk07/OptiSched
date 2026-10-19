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
            string code = LoginCodeService.GenerateCode(email);
            EmailService emailService = new EmailService();
            emailService.SendLoginCode(email, code);

            ViewState["Email"] = email;
            pnlRequestCode.Visible = false;
            pnlVerifyCode.Visible = true;
            lblCodeMessage.Text = "\u05E7\u05D5\u05D3 \u05E0\u05E9\u05DC\u05D7 \u05DC\u05D0\u05D9\u05DE\u05D9\u05D9\u05DC \u05E9\u05DC\u05DA. \u05D0\u05E0\u05D0 \u05D1\u05D3\u05D5\u05E7 \u05D0\u05EA \u05EA\u05D9\u05D1\u05EA \u05D4\u05D3\u05D5\u05D0\u05E8 \u05D4\u05E0\u05DB\u05E0\u05E1.";
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

        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            lblCodeMessage.Text = "\u05D0\u05E0\u05D0 \u05D4\u05D6\u05DF \u05E7\u05D5\u05D3 \u05D1\u05DF 6 \u05E1\u05E4\u05E8\u05D5\u05EA";
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

                if (user != null)
                {
                    int userId = Convert.ToInt32(user["id"]);
                    string username = user["userName"] != null ? user["userName"].ToString() : email;
                    string role = user["Role"] != null ? user["Role"].ToString() : "user";

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
            string code = LoginCodeService.GenerateCode(email);
            EmailService emailService = new EmailService();
            emailService.SendLoginCode(email, code);

            lblCodeMessage.Text = "\u05E7\u05D5\u05D3 \u05D7\u05D3\u05E9 \u05E0\u05E9\u05DC\u05D7 \u05DC\u05D0\u05D9\u05DE\u05D9\u05D9\u05DC \u05E9\u05DC\u05DA.";
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
}
