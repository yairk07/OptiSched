using System;
using System.Data;
using System.Web;
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

        // DSD Schema: Use AuthCodeService instead of OTPLoginService
        AuthCodeService.CleanExpiredCodes();
        
        if (!IsPostBack)
        {
            SetHebrewText();
        }
    }

    protected void btnSendCode_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "אנא הזן כתובת אימייל.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            lblMessage.Text = "אנא הזן כתובת אימייל תקינה.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        try
        {
            // DSD Schema: Get UserId first, then generate code using AuthCodeService
            UsersService us = new UsersService();
            DataRow user = us.GetUserByEmail(email);
            
            if (user == null)
            {
                lblMessage.Text = "כתובת האימייל לא נמצאה במערכת.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }
            
            // DSD Schema: Get Id column (handle both old and new column names)
            string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
            int userId = Convert.ToInt32(user[idCol]);
            
            string code = AuthCodeService.GenerateLoginCode(userId);
            EmailService.SendLoginCodeEmail(email, code);

            ViewState["LoginEmail"] = email;
            ViewState["LoginUserId"] = userId;
            pnlRequestCode.Visible = false;
            pnlVerifyCode.Visible = true;
            lblCodeMessage.Text = "\u05e7\u05d5\u05d3 \u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05e0\u05e9\u05dc\u05d7 \u05dc\u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Green;
            txtCode.Text = "";
        }
        catch (Exception ex)
        {
            lblMessage.Text = ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnVerifyCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["LoginEmail"]?.ToString();
        string code = txtCode.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            lblCodeMessage.Text = "אנא הזן קוד התחברות.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (code.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
        {
            lblCodeMessage.Text = "קוד התחברות חייב להיות בן 6 ספרות.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        try
        {
            // DSD Schema: Use UserId for validation with AuthCodeService
            int userId = 0;
            if (ViewState["LoginUserId"] != null)
            {
                userId = Convert.ToInt32(ViewState["LoginUserId"]);
            }
            else
            {
                // Fallback: get user by email
                UsersService us = new UsersService();
                DataRow user = us.GetUserByEmail(email);
                if (user == null)
                {
                    lblCodeMessage.Text = "קוד התחברות לא תקין או שפג תוקפו.";
                    lblCodeMessage.ForeColor = System.Drawing.Color.Red;
                    return;
                }
                string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
                userId = Convert.ToInt32(user[idCol]);
            }
            
            bool isValid = AuthCodeService.ValidateLoginCode(userId, code);

            if (isValid)
            {
                UsersService us = new UsersService();
                DataRow user = us.GetUserByEmail(email);

                if (user != null)
                {
                    // DSD Schema: Use UserName and Role columns (handle both old and new)
                    string userNameCol = user.Table.Columns.Contains("UserName") ? "UserName" : "userName";
                    string roleCol = user.Table.Columns.Contains("Role") ? "Role" : "role";
                    string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
                    
                    Session["username"] = Connect.FixEncoding(user[userNameCol].ToString());
                    Session["Role"] = Connect.FixEncoding(user[roleCol]?.ToString() ?? "user");
                    Session["userId"] = user[idCol].ToString();
                    Session["loggedIn"] = true;

                    Response.Redirect("home.aspx");
                    return;
                }
                else
                {
                    lblCodeMessage.Text = "קוד התחברות לא תקין או שפג תוקפו.";
                    lblCodeMessage.ForeColor = System.Drawing.Color.Red;
                }
            }
            else
            {
                lblCodeMessage.Text = "קוד התחברות לא תקין או שפג תוקפו.";
                lblCodeMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblCodeMessage.Text = "שגיאה באימות הקוד: " + ex.Message;
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void lnkResendCode_Click(object sender, EventArgs e)
    {
        string email = ViewState["LoginEmail"]?.ToString();

        if (string.IsNullOrEmpty(email))
        {
            pnlVerifyCode.Visible = false;
            pnlRequestCode.Visible = true;
            return;
        }

        try
        {
            // DSD Schema: Get UserId and use AuthCodeService
            int userId = 0;
            if (ViewState["LoginUserId"] != null)
            {
                userId = Convert.ToInt32(ViewState["LoginUserId"]);
            }
            else
            {
                UsersService us = new UsersService();
                DataRow user = us.GetUserByEmail(email);
                if (user == null)
                {
                    lblCodeMessage.Text = "שגיאה: משתמש לא נמצא.";
                    lblCodeMessage.ForeColor = System.Drawing.Color.Red;
                    return;
                }
                string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
                userId = Convert.ToInt32(user[idCol]);
            }
            
            string code = AuthCodeService.GenerateLoginCode(userId);
            EmailService.SendLoginCodeEmail(email, code);

            lblCodeMessage.Text = "\u05e7\u05d5\u05d3 \u05d7\u05d3\u05e9 \u05e0\u05e9\u05dc\u05d7 \u05dc\u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da.";
            lblCodeMessage.ForeColor = System.Drawing.Color.Green;
            txtCode.Text = "";
        }
        catch (Exception ex)
        {
            lblCodeMessage.Text = ex.Message;
            lblCodeMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    private void SetHebrewText()
    {
        string hebrewTitle = "\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05dc\u05dc\u05d0 \u05e1\u05d9\u05e1\u05de\u05d4";
        string hebrewDescription = "\u05d4\u05d6\u05df \u05d0\u05ea \u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da \u05d5\u05e0\u05e9\u05dc\u05d7 \u05dc\u05da \u05e7\u05d5\u05d3 \u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05d1\u05df 6 \u05e1\u05e4\u05e8\u05d5\u05ea. \u05d4\u05e7\u05d5\u05d3 \u05ea\u05e7\u05e3 \u05dc\u05de\u05e9\u05d4 15 \u05d3\u05e7\u05d5\u05ea \u05d1\u05dc\u05d1\u05d3.";
        string hebrewFormTitle = "\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05e2\u05dd \u05e7\u05d5\u05d3";
        string hebrewSupport = "\u05d4\u05d6\u05df \u05d0\u05ea \u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da \u05db\u05d3\u05d9 \u05dc\u05e7\u05d1\u05dc \u05e7\u05d5\u05d3 \u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea";
        string hebrewEmailLabel = "\u05db\u05ea\u05d5\u05d1\u05ea \u05d0\u05d9\u05de\u05d9\u05d9\u05dc <span class=\"required\">*</span>";
        string hebrewSendCode = "\u05e9\u05dc\u05d7 \u05e7\u05d5\u05d3 \u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea";
        string hebrewCodeLabel = "\u05e7\u05d5\u05d3 \u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea <span class=\"required\">*</span>";
        string hebrewCodeInfo = "\u05e7\u05d5\u05d3 \u05d4\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05e0\u05e9\u05dc\u05d7 \u05dc\u05db\u05ea\u05d5\u05d1\u05ea \u05d4\u05d0\u05d9\u05de\u05d9\u05d9\u05dc \u05e9\u05dc\u05da. \u05d4\u05e7\u05d5\u05d3 \u05ea\u05e7\u05e3 \u05dc\u05de\u05e9\u05d4 15 \u05d3\u05e7\u05d5\u05ea.";
        string hebrewVerify = "\u05d0\u05d9\u05de\u05d5\u05ea \u05d5\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea";
        string hebrewResend = "\u05e9\u05dc\u05d7 \u05e7\u05d5\u05d3 \u05d7\u05d3\u05e9";
        string hebrewBack = "\u05d7\u05d6\u05d5\u05e8\u05d4 \u05dc\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05e8\u05d2\u05d9\u05dc\u05d4";
        string hebrewNotRegistered = "\u05dc\u05d0 \u05e8\u05e9\u05d5\u05de\u05d9\u05dd \u05e2\u05d3\u05d9\u05d9\u05df?";
        string hebrewCreateUser = "\u05e6\u05e8\u05d5 \u05de\u05e9\u05ea\u05de\u05e9 \u05d7\u05d3\u05e9";

        var contentPlaceHolder = Master.FindControl("ContentPlaceHolder1") as System.Web.UI.WebControls.ContentPlaceHolder;
        if (contentPlaceHolder == null) return;
        
        var h2Title = contentPlaceHolder.FindControl("h2Title") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pDescription = contentPlaceHolder.FindControl("pDescription") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var h3Title = contentPlaceHolder.FindControl("h3Title") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pSupport = contentPlaceHolder.FindControl("pSupport") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblEmail = contentPlaceHolder.FindControl("lblEmail") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lblCode = contentPlaceHolder.FindControl("lblCode") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var pCodeInfo = contentPlaceHolder.FindControl("pCodeInfo") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lnkBack = contentPlaceHolder.FindControl("lnkBack") as System.Web.UI.HtmlControls.HtmlAnchor;
        var spanNotRegistered = contentPlaceHolder.FindControl("spanNotRegistered") as System.Web.UI.HtmlControls.HtmlGenericControl;
        var lnkRegister = contentPlaceHolder.FindControl("lnkRegister") as System.Web.UI.HtmlControls.HtmlAnchor;

        if (h2Title != null) h2Title.InnerText = hebrewTitle;
        if (pDescription != null) pDescription.InnerText = hebrewDescription;
        if (h3Title != null) h3Title.InnerText = hebrewFormTitle;
        if (pSupport != null) pSupport.InnerText = hebrewSupport;
        if (lblEmail != null) lblEmail.InnerHtml = hebrewEmailLabel;
        if (btnSendCode != null) btnSendCode.Text = hebrewSendCode;
        if (lblCode != null) lblCode.InnerHtml = hebrewCodeLabel;
        if (pCodeInfo != null) pCodeInfo.InnerText = hebrewCodeInfo;
        if (btnVerifyCode != null) btnVerifyCode.Text = hebrewVerify;
        if (lnkResendCode != null) lnkResendCode.Text = hebrewResend;
        if (lnkBack != null) lnkBack.InnerText = hebrewBack;
        if (spanNotRegistered != null) spanNotRegistered.InnerText = hebrewNotRegistered;
        if (lnkRegister != null) lnkRegister.InnerText = hebrewCreateUser;
    }
}

