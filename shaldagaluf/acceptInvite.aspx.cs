using System;
using System.Web;

public partial class acceptInvite : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;

        string token = Request.QueryString["token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            lblMessage.Text = "קישור הזמנה חסר או לא תקין.";
            lblMessage.CssClass = "auth-error";
            return;
        }

        if (Session["username"] == null || Session["userId"] == null)
        {
            string returnUrl = "acceptInvite.aspx?token=" + HttpUtility.UrlEncode(token);
            Response.Redirect("login.aspx?returnUrl=" + HttpUtility.UrlEncode(returnUrl), false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        try
        {
            int userId = Convert.ToInt32(Session["userId"]);
            int calendarId = CalendarInviteService.RedeemInvite(token, userId);
            lblMessage.Text = "הצטרפת בהצלחה לטבלה המשותפת.";
            lblMessage.CssClass = "auth-success";
            lnkCalendar.Visible = true;
            lnkCalendar.NavigateUrl = "sharedCalendarDetails.aspx?id=" + calendarId;
            Response.AddHeader("Refresh", "1;url=sharedCalendarDetails.aspx?id=" + calendarId);
        }
        catch (Exception ex)
        {
            LoggingService.Log("acceptInvite", "Redeem failed", ex);
            lblMessage.Text = ex.Message;
            lblMessage.CssClass = "auth-error";
        }
    }
}
