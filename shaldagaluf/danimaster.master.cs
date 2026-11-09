using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class danimaster : System.Web.UI.MasterPage
{
    // רץ בכל דף כי זה ה-Master Page.
    // מראה או מסתיר קישורי תפריט לפי התחברות ולפי תפקיד owner.
    // למשתמש מחובר גם בודק תזכורות ומעדכן את פעמון ההתראות.
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;

        string pageName = System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath);
        Body.Attributes["class"] = "page-" + pageName.ToLower();

        bool isLoggedIn = Session["username"] != null;
        string role = Convert.ToString(Session["Role"]);
        bool isOwner = isLoggedIn && string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

        if (linkRegister != null)
            linkRegister.Visible = !isLoggedIn;

        if (linkLogin != null)
            linkLogin.Visible = !isLoggedIn;

        if (linkTasks != null)
            linkTasks.Visible = isLoggedIn;

        if (linkSharedCalendars != null)
            linkSharedCalendars.Visible = isLoggedIn;

        if (linkContent != null)
            linkContent.Visible = isLoggedIn;

        if (linkTerms != null)
            linkTerms.Visible = true;

        if (linkUsers != null)
            linkUsers.Visible = isOwner;

        if (linkAllEvents != null)
            linkAllEvents.Visible = isOwner;

        if (linkExportGoogle != null)
            linkExportGoogle.Visible = isLoggedIn;

        if (headerAccount != null)
            headerAccount.Visible = isLoggedIn;

        if (lnkUserName != null)
            lnkUserName.Visible = isLoggedIn;

        if (notifBellWrap != null)
            notifBellWrap.Visible = isLoggedIn;

        if (isLoggedIn && Session["userId"] != null)
        {
            int userId;
            if (int.TryParse(Convert.ToString(Session["userId"]), out userId) && userId > 0)
            {
                try { ReminderService.ProcessDueReminders(userId); }
                catch (Exception ex) { LoggingService.Log("master", "Reminder process failed", ex); }

                try { BindNotificationBell(userId); }
                catch (Exception ex) { LoggingService.Log("master", "Notification bell failed", ex); }
            }
        }
    }

    private void BindNotificationBell(int userId)
    {
        int unread = NotificationService.CountUnread(userId);
        if (lblNotifCount != null)
        {
            lblNotifCount.Visible = unread > 0;
            lblNotifCount.Text = unread > 99 ? "99+" : unread.ToString();
        }

        if (pnlNotifDropdown != null && pnlNotifDropdown.Visible)
        {
            DataTable dt = NotificationService.GetRecent(userId, 20);
            rptNotifications.DataSource = dt;
            rptNotifications.DataBind();
            lblNoNotifs.Visible = dt.Rows.Count == 0;
        }
    }

    protected void btnNotifToggle_Click(object sender, EventArgs e)
    {
        if (pnlNotifDropdown == null) return;
        pnlNotifDropdown.Visible = !pnlNotifDropdown.Visible;
        if (pnlNotifDropdown.Visible && Session["userId"] != null)
        {
            int userId = Convert.ToInt32(Session["userId"]);
            DataTable dt = NotificationService.GetRecent(userId, 20);
            rptNotifications.DataSource = dt;
            rptNotifications.DataBind();
            lblNoNotifs.Visible = dt.Rows.Count == 0;
        }
    }

    protected void btnMarkAllRead_Click(object sender, EventArgs e)
    {
        if (Session["userId"] == null) return;
        int userId = Convert.ToInt32(Session["userId"]);
        NotificationService.MarkAllRead(userId);
        pnlNotifDropdown.Visible = true;
        BindNotificationBell(userId);
        DataTable dt = NotificationService.GetRecent(userId, 20);
        rptNotifications.DataSource = dt;
        rptNotifications.DataBind();
        lblNoNotifs.Visible = dt.Rows.Count == 0;
    }

    protected void rptNotifications_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "Open" || Session["userId"] == null) return;
        int userId = Convert.ToInt32(Session["userId"]);
        string arg = Convert.ToString(e.CommandArgument) ?? "";
        string[] parts = arg.Split(new[] { '|' }, 2);
        int notifId;
        if (parts.Length > 0 && int.TryParse(parts[0], out notifId))
            NotificationService.MarkRead(notifId, userId);

        string link = parts.Length > 1 ? parts[1] : "home.aspx";
        if (string.IsNullOrWhiteSpace(link) || link.IndexOf("..") >= 0 || link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            link = "home.aspx";
        Response.Redirect(link, false);
        Context.ApplicationInstance.CompleteRequest();
    }

    // טיפול בלחיצת קישור UserName
    protected void lnkUserName_Click(object sender, EventArgs e)
    {
        Session.Clear();
        Response.Redirect("login.aspx");
    }
}
