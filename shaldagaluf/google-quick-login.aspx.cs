using System;
using System.Web.UI;

public partial class google_quick_login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        try
        {
            string authUrl = GoogleOAuthService.GetAuthorizationUrl();
            Response.Redirect(authUrl);
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_QUICK_LOGIN_ERROR", string.Format("Error in quick login - Error: {0}", ex.Message), ex);
            Response.Redirect("login.aspx?error=google_oauth_config");
        }
    }

    protected string GetRedirectUrl()
    {
        try
        {
            return GoogleOAuthService.GetAuthorizationUrl();
        }
        catch
        {
            return "login.aspx?error=google_oauth_config";
        }
    }
}
