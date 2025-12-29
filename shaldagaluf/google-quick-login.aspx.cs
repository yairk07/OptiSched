using System;
using System.Web;
using System.Web.UI;

public partial class google_quick_login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string email = Request.QueryString["email"];
        
        if (string.IsNullOrEmpty(email))
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("אימייל לא נמצא"));
            return;
        }

        try
        {
            string quickLoginUrl = GoogleOAuthService.GetQuickLoginUrl(email);
            Response.Redirect(quickLoginUrl);
        }
        catch (Exception ex)
        {
            Response.Redirect("login.aspx?error=" + HttpUtility.UrlEncode("שגיאה בהתחברות מהירה: " + ex.Message));
        }
    }

    protected string GetRedirectUrl()
    {
        return GoogleOAuthService.GetQuickLoginUrl(Request.QueryString["email"] ?? "");
    }
}




