using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.UI;

public partial class forgotPassword : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        VerificationCodeService.CleanExpiredCodes();
        
        if (!IsPostBack)
        {
            SetHebrewText();
        }
    }
    
    private void SetHebrewText()
    {
        string hebrewTitle = "\u05e9\u05db\u05d7\u05ea \u05e1\u05d9\u05e1\u05de\u05d4?";
        string hebrewSubtitle = "\u05d4\u05d6\u05df \u05d0\u05ea \u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da \u05d5\u05e0\u05e9\u05dc\u05d7 \u05dc\u05da \u05e7\u05d5\u05d3 \u05d0\u05de\u05ea \u05dc\u05d0\u05d9\u05e4\u05d5\u05e1 \u05d4\u05e1\u05d9\u05e1\u05de\u05d4";
        string hebrewEmailLabel = "\u05db\u05ea\u05d5\u05d1\u05ea \u05d0\u05d9\u05de\u05d9\u05d9\u05dc <span class=\"required\">*</span>";
        string hebrewSendCode = "\u05e9\u05dc\u05d7 \u05e7\u05d5\u05d3 \u05d0\u05de\u05ea";
        string hebrewCodeLabel = "\u05e7\u05d5\u05d3 \u05d0\u05de\u05ea <span class=\"required\">*</span>";
        string hebrewCodeInfo = "\u05e7\u05d5\u05d3 \u05d4\u05d0\u05de\u05ea \u05e0\u05e9\u05dc\u05d7 \u05dc\u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da. \u05d4\u05e7\u05d5\u05d3 \u05ea\u05e7\u05e3 \u05dc\u05de\u05e9\u05d4 15 \u05d3\u05e7\u05d5\u05ea.";
        string hebrewVerify = "\u05d0\u05de\u05ea \u05e7\u05d5\u05d3";
        string hebrewNewPassword = "\u05e1\u05d9\u05e1\u05de\u05d4 \u05d7\u05d3\u05e9\u05d4 <span class=\"required\">*</span>";
        string hebrewConfirmPassword = "\u05d0\u05d9\u05de\u05d5\u05ea \u05e1\u05d9\u05e1\u05de\u05d4 <span class=\"required\">*</span>";
        string hebrewReset = "\u05d0\u05d9\u05e4\u05d5\u05e1 \u05e1\u05d9\u05e1\u05de\u05d4";
        string hebrewFooter = "\u05d6\u05db\u05e8\u05ea \u05d0\u05ea \u05d4\u05e1\u05d9\u05e1\u05de\u05d4? <a href=\"login.aspx\">\u05d7\u05d6\u05d5\u05e8 \u05dc\u05d3\u05e3 \u05d4\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea</a>";

        var contentPlaceHolder = Master.FindControl("ContentPlaceHolder1") as System.Web.UI.WebControls.ContentPlaceHolder;
        if (contentPlaceHolder == null) return;
        
        var h2Title = contentPlaceHolder.FindControl("h2Title") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pSubtitle = contentPlaceHolder.FindControl("pSubtitle") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblEmail = contentPlaceHolder.FindControl("lblEmail") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblCode = contentPlaceHolder.FindControl("lblCode") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pCodeInfo = contentPlaceHolder.FindControl("pCodeInfo") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblNewPassword = contentPlaceHolder.FindControl("lblNewPassword") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblConfirmPassword = contentPlaceHolder.FindControl("lblConfirmPassword") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pFooter = contentPlaceHolder.FindControl("pFooter") as System.Web.UI.HtmlControls.HtmlGenericControl;

        if (h2Title != null) h2Title.InnerText = hebrewTitle;
        if (pSubtitle != null) pSubtitle.InnerText = hebrewSubtitle;
        if (lblEmail != null) lblEmail.InnerHtml = hebrewEmailLabel;
        if (btnSendReset != null) btnSendReset.Text = hebrewSendCode;
        if (lblCode != null) lblCode.InnerHtml = hebrewCodeLabel;
        if (pCodeInfo != null) pCodeInfo.InnerText = hebrewCodeInfo;
        if (btnVerifyCode != null) btnVerifyCode.Text = hebrewVerify;
        if (lblNewPassword != null) lblNewPassword.InnerHtml = hebrewNewPassword;
        if (lblConfirmPassword != null) lblConfirmPassword.InnerHtml = hebrewConfirmPassword;
        if (btnResetPassword != null) btnResetPassword.Text = hebrewReset;
        if (pFooter != null) pFooter.InnerHtml = hebrewFooter;
    }

    protected void btnSendReset_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "אנא הזן כתובת אימייל.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        UsersService us = new UsersService();
        DataRow user = us.GetUserByEmail(email);

        if (user == null)
        {
            lblMessage.Text = "כתובת האימייל לא נמצאה במערכת.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        try
        {
            string code = VerificationCodeService.GenerateCode(email);
            SendVerificationCodeEmail(email, code);
            
            ViewState["ResetEmail"] = email;
            pnlRequest.Visible = false;
            pnlCode.Visible = true;
            lblCodeMessage.Text = "\u05e7\u05d5\u05d3 \u05d0\u05d9\u05de\u05d5\u05ea \u05e0\u05e9\u05dc\u05d7 \u05dc\u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Green;
        }
        catch (Exception ex)
        {
            lblMessage.Text = ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnVerifyCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["ResetEmail"]?.ToString();
        string code = txtVerificationCode.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            lblCodeMessage.Text = "אנא הזן קוד אימות.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (code.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
        {
            lblCodeMessage.Text = "קוד אימות חייב להיות בן 6 ספרות.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        

        try
        {
            bool isValid = VerificationCodeService.ValidateCode(email, code);

            
        
            if (isValid)
            {
                ViewState["VerifiedEmail"] = email;
                pnlCode.Visible = false;
                pnlReset.Visible = true;
                lblResetMessage.Text = "";
            }
            else
            {
                lblCodeMessage.Text = "קוד אימות לא תקין או שפג תוקפו. אנא נסה שוב.";
                lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            
            
            lblCodeMessage.Text = "שגיאה באימות הקוד: " + ex.Message;
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnResetPassword_Click(object sender, EventArgs e)
    {
        string email = ViewState["VerifiedEmail"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            lblResetMessage.Text = "שגיאה: לא נמצא אימייל מאומת.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            lblResetMessage.Text = "אנא מלא את כל השדות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (newPassword != confirmPassword)
        {
            lblResetMessage.Text = "הסיסמאות אינן תואמות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        UsersService us = new UsersService();
        DataRow user = us.GetUserByEmail(email);
        
        if (user == null)
        {
            lblResetMessage.Text = "שגיאה: משתמש לא נמצא.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        int userId = Convert.ToInt32(user["id"]);
        us.UpdatePassword(userId, newPassword);

        lblResetMessage.Text = "הסיסמה עודכנה בהצלחה! אתה יכול להתחבר עכשיו.";
        lblResetMessage.ForeColor = System.Drawing.Color.Green;
        
        ViewState.Remove("VerifiedEmail");
        ViewState.Remove("ResetEmail");
    }

    private void SendVerificationCodeEmail(string email, string code)
    {
        string smtpServer = "smtp.gmail.com";
        int smtpPort = 587;
        string smtpUsername = "yairk07@gmail.com";
        string smtpPassword = "wdbf swcf qexu qugl";

        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(smtpUsername, "OptiSched", System.Text.Encoding.UTF8);
        mail.To.Add(email);
        mail.SubjectEncoding = System.Text.Encoding.UTF8;
        mail.Subject = "Password Reset Code - OptiSched";
        mail.BodyEncoding = System.Text.Encoding.UTF8;
        mail.Body = $@"
<html dir='ltr'>
<body style='font-family: Arial, sans-serif; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #e50914;'>Password Reset Code</h2>
        <p>We received a request to reset your password.</p>
        <p>Your verification code is:</p>
        <div style='background: #f5f5f5; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: bold; color: #e50914; letter-spacing: 8px; font-family: monospace;'>{code}</span>
        </div>
        <p>Enter this code on the password reset page to continue.</p>
        <p style='color: #666; font-size: 14px;'><strong>This code is valid for 15 minutes only.</strong></p>
        <p>If you did not request a password reset, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px;'>OptiSched - Smart Scheduling for Maximum Efficiency</p>
    </div>
</body>
</html>";
        mail.IsBodyHtml = true;

        SmtpClient smtp = new SmtpClient(smtpServer, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

        smtp.Send(mail);
    }
}

