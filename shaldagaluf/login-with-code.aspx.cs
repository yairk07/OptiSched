using System;
using System.Web.UI;

public partial class login_with_code : System.Web.UI.Page
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

        if (!IsPostBack)
        {
            InitializeUI();
            pnlRequestCode.Visible = true;
            pnlVerifyCode.Visible = false;
        }
    }

    private void InitializeUI()
    {
        h2Title.InnerText = "התחברות ללא סיסמה";
        pDescription.InnerText = "קבל קוד התחברות באימייל שלך";
        h3Title.InnerText = "התחברות עם קוד";
        pSupport.InnerText = "הזן את כתובת האימייל שלך ונשלח לך קוד התחברות";
        lblEmail.InnerText = "כתובת אימייל";
        btnSendCode.Text = "שלח קוד";
        lblCode.InnerText = "קוד אימות";
        btnVerifyCode.Text = "אמת קוד";
        pCodeInfo.InnerText = "הזן את הקוד בן 6 הספרות שנשלח לאימייל שלך";
        lnkResendCode.Text = "שלח קוד חדש";
        lnkBack.InnerText = "חזור להתחברות רגילה";
        spanNotRegistered.InnerText = "עדיין לא רשום?";
        lnkRegister.InnerText = "הירשם עכשיו";
    }

    protected void btnSendCode_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "אנא הזן כתובת אימייל";
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
            lblCodeMessage.Text = "קוד נשלח לאימייל שלך. אנא בדוק את תיבת הדואר הנכנס.";
            lblCodeMessage.CssClass = "auth-success";
        }
        catch (InvalidOperationException ex)
        {
            lblMessage.Text = ex.Message;
            lblMessage.CssClass = "auth-error";
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error sending code", ex);
            lblMessage.Text = "אירעה שגיאה בשליחת הקוד. אנא נסה שוב מאוחר יותר.";
            lblMessage.CssClass = "auth-error";
        }
    }

    protected void btnVerifyCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["Email"] != null ? ViewState["Email"].ToString() : txtEmail.Text.Trim().ToLower();
        string code = txtCode.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblCodeMessage.Text = "שגיאה: כתובת אימייל לא נמצאה";
            lblCodeMessage.CssClass = "auth-error";
            return;
        }

        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            lblCodeMessage.Text = "אנא הזן קוד בן 6 ספרות";
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

                    Response.Redirect("home.aspx");
                }
                else
                {
                    lblCodeMessage.Text = "משתמש לא נמצא במערכת";
                    lblCodeMessage.CssClass = "auth-error";
                }
            }
            else
            {
                lblCodeMessage.Text = "קוד לא תקין או שפג תוקפו. אנא נסה שוב או בקש קוד חדש.";
                lblCodeMessage.CssClass = "auth-error";
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error verifying code", ex);
            lblCodeMessage.Text = "אירעה שגיאה באימות הקוד. אנא נסה שוב מאוחר יותר.";
            lblCodeMessage.CssClass = "auth-error";
        }
    }

    protected void lnkResendCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["Email"] != null ? ViewState["Email"].ToString() : txtEmail.Text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            lblCodeMessage.Text = "שגיאה: כתובת אימייל לא נמצאה";
            lblCodeMessage.CssClass = "auth-error";
            return;
        }

        try
        {
            string code = LoginCodeService.GenerateCode(email);
            
            EmailService emailService = new EmailService();
            emailService.SendLoginCode(email, code);

            lblCodeMessage.Text = "קוד חדש נשלח לאימייל שלך.";
            lblCodeMessage.CssClass = "auth-success";
            txtCode.Text = "";
        }
        catch (InvalidOperationException ex)
        {
            lblCodeMessage.Text = ex.Message;
            lblCodeMessage.CssClass = "auth-error";
        }
        catch (Exception ex)
        {
            LoggingService.Log("login-with-code", "Error resending code", ex);
            lblCodeMessage.Text = "אירעה שגיאה בשליחת הקוד. אנא נסה שוב מאוחר יותר.";
            lblCodeMessage.CssClass = "auth-error";
        }
    }
}
