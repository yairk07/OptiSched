using System;
using System.Data;
using System.Data.OleDb;
using System.Web.UI;

public partial class login_with_code : System.Web.UI.Page
{
    private string currentEmail = "";

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.AppendHeader("Content-Type", "text/html; charset=utf-8");
        
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        if (!IsPostBack)
        {
            InitializeUI();
        }
        else
        {
            if (Session["LoginCodeEmail"] != null)
            {
                currentEmail = Session["LoginCodeEmail"].ToString();
            }
        }
    }

    private void InitializeUI()
    {
        h2Title.InnerText = "התחברות ללא סיסמה";
        pDescription.InnerText = "הזן את כתובת האימייל שלך ונשלח לך קוד התחברות";
        h3Title.InnerText = "התחברות עם קוד";
        pSupport.InnerText = "הזן את כתובת האימייל שלך ונשלח לך קוד התחברות חד-פעמי";
        lblEmail.InnerText = "כתובת אימייל";
        btnSendCode.Text = "שלח קוד";
        lblCode.InnerText = "קוד התחברות";
        btnVerifyCode.Text = "אימות קוד";
        pCodeInfo.InnerText = "הזן את הקוד בן 6 הספרות שנשלח לכתובת האימייל שלך";
        lnkResendCode.Text = "שלח קוד מחדש";
        lnkBack.InnerText = "חזור להתחברות רגילה";
        spanNotRegistered.InnerText = "עדיין לא נרשמת?";
        lnkRegister.InnerText = "הירשם עכשיו";
    }

    protected void btnSendCode_Click(object sender, EventArgs e)
    {
        try
        {
            string email = txtEmail.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email))
            {
                lblMessage.Text = "אנא הזן כתובת אימייל";
                lblMessage.Visible = true;
                return;
            }

            if (!IsValidEmail(email))
            {
                lblMessage.Text = "כתובת אימייל לא תקינה";
                lblMessage.Visible = true;
                return;
            }

            LoggingService.Log("LOGIN_CODE_SEND_START", string.Format("Starting send code process - Email: {0}", email));

            UsersService userService = new UsersService();
            LoggingService.Log("LOGIN_CODE_GET_USER", string.Format("Calling GetUserByEmail - Email: {0}", email));
            
            DataRow user = userService.GetUserByEmail(email);

            if (user == null)
            {
                LoggingService.Log("LOGIN_CODE_USER_NOT_FOUND", string.Format("User not found - Email: {0}", email));
                lblMessage.Text = "כתובת אימייל זו לא רשומה במערכת. אנא הירשם תחילה.";
                lblMessage.Visible = true;
                return;
            }

            LoggingService.Log("LOGIN_CODE_USER_FOUND", string.Format("User found - Email: {0}", email));

            LoggingService.Log("LOGIN_CODE_GENERATE", string.Format("Calling GenerateCode - Email: {0}", email));
            string code = LoginCodeService.GenerateCode(email);
            LoggingService.Log("LOGIN_CODE_GENERATED", string.Format("Code generated - Email: {0}, Code: {1}", email, code));

            try
            {
                EmailService.SendLoginCodeEmail(email, code);
                Session["LoginCodeEmail"] = email;
                pnlRequestCode.Visible = false;
                pnlVerifyCode.Visible = true;
                lblCodeMessage.Text = "קוד נשלח בהצלחה לכתובת האימייל שלך. אנא הזן את הקוד.";
                lblCodeMessage.CssClass = "auth-error";
                lblCodeMessage.Style["color"] = "green";
                lblCodeMessage.Visible = true;
            }
            catch (Exception emailEx)
            {
                LoggingService.Log("EMAIL_SEND_FAILED", string.Format("Failed to send login code email - Email: {0}, Error: {1}", email, emailEx.Message), emailEx);
                lblMessage.Text = "שגיאה בשליחת האימייל. אנא נסה שוב מאוחר יותר או השתמש בהתחברות רגילה.";
                lblMessage.Visible = true;
            }
        }
        catch (InvalidOperationException ex)
        {
            lblMessage.Text = ex.Message;
            lblMessage.Visible = true;
        }
        catch (Exception ex)
        {
            LoggingService.Log("LOGIN_CODE_ERROR", string.Format("Error in btnSendCode_Click - {0}", ex.Message), ex);
            lblMessage.Text = "אירעה שגיאה. אנא נסה שוב מאוחר יותר.";
            lblMessage.Visible = true;
        }
    }

    protected void btnVerifyCode_Click(object sender, EventArgs e)
    {
        try
        {
            string email = Session["LoginCodeEmail"]?.ToString() ?? "";
            string code = txtCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                lblCodeMessage.Text = "שגיאה: לא נמצא אימייל. אנא התחל מחדש.";
                lblCodeMessage.Visible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                lblCodeMessage.Text = "אנא הזן את קוד ההתחברות";
                lblCodeMessage.Visible = true;
                return;
            }

            if (code.Length != 6)
            {
                lblCodeMessage.Text = "קוד ההתחברות חייב להיות בן 6 ספרות";
                lblCodeMessage.Visible = true;
                return;
            }

            LoggingService.Log("LOGIN_CODE_VERIFY_START", string.Format("Starting code verification - Email: {0}, Code: {1}", email, code));
            
            bool isValid = LoginCodeService.ValidateCode(email, code);
            LoggingService.Log("LOGIN_CODE_VERIFY_RESULT", string.Format("Code validation result - Email: {0}, Code: {1}, IsValid: {2}", email, code, isValid));

            if (isValid)
            {
                LoggingService.Log("LOGIN_CODE_VERIFY_SUCCESS", string.Format("Code is valid, getting user - Email: {0}", email));
                UsersService userService = new UsersService();
                DataRow user = userService.GetUserByEmail(email);
                LoggingService.Log("LOGIN_CODE_VERIFY_USER", string.Format("User retrieved - Email: {0}, User is null: {1}", email, user == null));

                if (user != null)
                {
                    string userNameCol = user.Table.Columns.Contains("UserName") ? "UserName" : "userName";
                    string roleCol = user.Table.Columns.Contains("Role") ? "Role" : "role";
                    string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";

                    Session["username"] = user[userNameCol]?.ToString() ?? "";
                    Session["Role"] = user[roleCol]?.ToString() ?? "user";
                    Session["userId"] = user[idCol]?.ToString() ?? "";
                    Session["loggedIn"] = true;
                    Session.Remove("LoginCodeEmail");

                    LoggingService.Log("LOGIN_CODE_SUCCESS", string.Format("User logged in with code - Email: {0}, Username: {1}", email, Session["username"]));

                    Response.Redirect("home.aspx");
                    return;
                }
                else
                {
                    lblCodeMessage.Text = "שגיאה: משתמש לא נמצא במערכת";
                    lblCodeMessage.Visible = true;
                }
            }
            else
            {
                lblCodeMessage.Text = "קוד שגוי או שפג תוקפו. אנא נסה שוב או בקש קוד חדש.";
                lblCodeMessage.Visible = true;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("LOGIN_CODE_VERIFY_ERROR", string.Format("Error in btnVerifyCode_Click - {0}", ex.Message), ex);
            lblCodeMessage.Text = "אירעה שגיאה באימות הקוד. אנא נסה שוב.";
            lblCodeMessage.Visible = true;
        }
    }

    protected void lnkResendCode_Click(object sender, EventArgs e)
    {
        try
        {
            string email = Session["LoginCodeEmail"]?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                lblCodeMessage.Text = "שגיאה: לא נמצא אימייל. אנא התחל מחדש.";
                lblCodeMessage.Visible = true;
                return;
            }

            string code = LoginCodeService.GenerateCode(email);

            try
            {
                EmailService.SendLoginCodeEmail(email, code);
                lblCodeMessage.Text = "קוד חדש נשלח בהצלחה לכתובת האימייל שלך.";
                lblCodeMessage.CssClass = "auth-error";
                lblCodeMessage.Style["color"] = "green";
                lblCodeMessage.Visible = true;
                txtCode.Text = "";
            }
            catch (Exception emailEx)
            {
                LoggingService.Log("EMAIL_RESEND_FAILED", string.Format("Failed to resend login code email - Email: {0}, Error: {1}", email, emailEx.Message), emailEx);
                lblCodeMessage.Text = "שגיאה בשליחת האימייל. אנא נסה שוב מאוחר יותר.";
                lblCodeMessage.Visible = true;
            }
        }
        catch (InvalidOperationException ex)
        {
            lblCodeMessage.Text = ex.Message;
            lblCodeMessage.Visible = true;
        }
        catch (Exception ex)
        {
            LoggingService.Log("LOGIN_CODE_RESEND_ERROR", string.Format("Error in lnkResendCode_Click - {0}", ex.Message), ex);
            lblCodeMessage.Text = "אירעה שגיאה. אנא נסה שוב מאוחר יותר.";
            lblCodeMessage.Visible = true;
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
