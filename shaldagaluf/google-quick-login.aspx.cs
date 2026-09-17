using System;
using System.Threading;
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
            if (!GoogleOAuthService.IsConfigured())
            {
                Response.Redirect("login.aspx?error=google_oauth_config", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string authUrl = GoogleOAuthService.GetAuthorizationUrl();
            Response.Redirect(authUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_QUICK_LOGIN_ERROR", "Error in quick login: " + ex.Message, ex);
            Response.Redirect("login.aspx?error=google_oauth_config", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
