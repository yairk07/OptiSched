using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.Script.Serialization;

public partial class login_with_code : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A",
                location = "login-with-code.aspx.cs:Page_Load:entry",
                message = "Page_Load entry - checking encoding settings",
                data = new {
                    contentType = Response.ContentType,
                    charset = Response.Charset,
                    contentEncoding = Response.ContentEncoding?.EncodingName,
                    headerEncoding = Response.HeaderEncoding?.EncodingName
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log

        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A",
                location = "login-with-code.aspx.cs:Page_Load:after_encoding",
                message = "After setting encoding - checking actual values",
                data = new {
                    contentType = Response.ContentType,
                    charset = Response.Charset,
                    contentEncoding = Response.ContentEncoding?.EncodingName,
                    headerEncoding = Response.HeaderEncoding?.EncodingName
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log
        
        // #region agent log
        try {
            string testHebrew = "\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05dc\u05dc\u05d0 \u05e1\u05d9\u05e1\u05de\u05d4";
            byte[] testBytes = System.Text.Encoding.UTF8.GetBytes(testHebrew);
            string testDecoded = System.Text.Encoding.UTF8.GetString(testBytes);
            var logData = new {
                sessionId = "debug-session",
                runId = "run2",
                hypothesisId = "E",
                location = "login-with-code.aspx.cs:Page_Load:test_hebrew_unicode",
                message = "Testing Hebrew text with Unicode escape sequences",
                data = new {
                    originalText = testHebrew,
                    utf8Bytes = Convert.ToBase64String(testBytes),
                    decodedText = testDecoded,
                    matches = testHebrew == testDecoded
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log
        
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        OTPLoginService.CleanExpiredCodes();
        
        if (!IsPostBack)
        {
            // #region agent log
            try {
                var logData = new {
                    sessionId = "debug-session",
                    runId = "run2",
                    hypothesisId = "F",
                    location = "login-with-code.aspx.cs:Page_Load:before_SetHebrewText",
                    message = "Before calling SetHebrewText",
                    data = new {
                        isPostBack = IsPostBack
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer = new JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer.Serialize(logData) + "\n");
            } catch (Exception ex) {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
            }
            // #endregion agent log
            
            SetHebrewText();
            
            // #region agent log
            try {
                var h2Title = FindControl("h2Title") as System.Web.UI.HtmlControls.HtmlGenericControl;
                var btnSendCode = FindControl("btnSendCode") as System.Web.UI.WebControls.Button;
                var logData = new {
                    sessionId = "debug-session",
                    runId = "run2",
                    hypothesisId = "F",
                    location = "login-with-code.aspx.cs:Page_Load:after_SetHebrewText",
                    message = "After calling SetHebrewText - checking controls",
                    data = new {
                        h2TitleFound = h2Title != null,
                        h2TitleText = h2Title?.InnerText ?? "null",
                        btnSendCodeFound = btnSendCode != null,
                        btnSendCodeText = btnSendCode?.Text ?? "null"
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer = new JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer.Serialize(logData) + "\n");
            } catch (Exception ex) {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
            }
            // #endregion agent log
        }
        
        // #region agent log
        try {
            string staticTextUnicode = "\u05d4\u05ea\u05d7\u05d1\u05e8\u05d5\u05ea \u05dc\u05dc\u05d0 \u05e1\u05d9\u05e1\u05de\u05d4";
            byte[] staticBytes = System.Text.Encoding.UTF8.GetBytes(staticTextUnicode);
            var logData = new {
                sessionId = "debug-session",
                runId = "run2",
                hypothesisId = "E",
                location = "login-with-code.aspx.cs:Page_Load:static_text_check_unicode",
                message = "Checking static Hebrew text with Unicode escape sequences",
                data = new {
                    staticText = staticTextUnicode,
                    utf8Bytes = Convert.ToBase64String(staticBytes),
                    textLength = staticTextUnicode.Length
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log
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
            string code = OTPLoginService.GenerateLoginCode(email);
            EmailService.SendLoginCodeEmail(email, code);

            ViewState["LoginEmail"] = email;
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
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run5",
                hypothesisId = "I",
                location = "login-with-code.aspx.cs:btnVerifyCode_Click:entry",
                message = "btnVerifyCode_Click entry",
                data = new {
                    emailFromViewState = ViewState["LoginEmail"]?.ToString() ?? "null",
                    codeFromTextBox = txtCode.Text?.Trim() ?? "null"
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch {}
        // #endregion agent log

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

        // #region agent log
        try {
            var logData2 = new {
                sessionId = "debug-session",
                runId = "run5",
                hypothesisId = "I",
                location = "login-with-code.aspx.cs:btnVerifyCode_Click:before_ValidateLoginCode",
                message = "Before calling ValidateLoginCode",
                data = new {
                    email = email,
                    code = code,
                    emailLength = email?.Length ?? 0,
                    codeLength = code?.Length ?? 0
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer2 = new System.Web.Script.Serialization.JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer2.Serialize(logData2) + "\n");
        } catch {}
        // #endregion agent log

        try
        {
            bool isValid = OTPLoginService.ValidateLoginCode(email, code);

            // #region agent log
            try {
                var logData3 = new {
                    sessionId = "debug-session",
                    runId = "run5",
                    hypothesisId = "I",
                    location = "login-with-code.aspx.cs:btnVerifyCode_Click:after_ValidateLoginCode",
                    message = "After calling ValidateLoginCode",
                    data = new {
                        isValid = isValid
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer3 = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer3.Serialize(logData3) + "\n");
            } catch {}
            // #endregion agent log

            if (isValid)
            {
                UsersService us = new UsersService();
                DataRow user = us.GetUserByEmail(email);

                if (user != null)
                {
                    Session["username"] = Connect.FixEncoding(user["userName"].ToString());
                    Session["Role"] = Connect.FixEncoding(user["role"]?.ToString() ?? "user");
                    Session["userId"] = user["id"].ToString();
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
            string code = OTPLoginService.GenerateLoginCode(email);
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

