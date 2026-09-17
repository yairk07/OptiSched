using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;

public partial class exportGoogle : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = Encoding.UTF8;
        Response.HeaderEncoding = Encoding.UTF8;

        if (Session["username"] == null || Session["userId"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!IsPostBack)
        {
            ShowQueryMessage();
            BindPage();
        }
    }

    private int CurrentUserId()
    {
        return Convert.ToInt32(Session["userId"]);
    }

    private void BindPage()
    {
        int userId = CurrentUserId();
        DataTable events = GoogleCalendarService.GetUserPersonalEvents(userId);
        litStats.Text = "\u05E0\u05DE\u05E6\u05D0\u05D5 " + events.Rows.Count + " \u05D0\u05D9\u05E8\u05D5\u05E2\u05D9\u05DD \u05DC\u05D9\u05D9\u05E6\u05D5\u05D0";

        if (events.Rows.Count == 0)
        {
            lblNoEvents.Visible = true;
            rptEvents.Visible = false;
            txtIcs.Text = "";
        }
        else
        {
            lblNoEvents.Visible = false;
            rptEvents.Visible = true;
            rptEvents.DataSource = events;
            rptEvents.DataBind();
            txtIcs.Text = GoogleCalendarService.BuildIcsForUser(userId);
        }
    }

    private void ShowQueryMessage()
    {
        string gcal = Request.QueryString["gcal"];
        if (string.IsNullOrEmpty(gcal))
            return;

        if (gcal == "ok")
        {
            string count = Request.QueryString["count"] ?? "0";
            string failed = Request.QueryString["failed"] ?? "0";
            SetStatus("\u05D4\u05D9\u05D9\u05E6\u05D5\u05D0 \u05D4\u05D9\u05E9\u05D9\u05E8 \u05DC-Google \u05D4\u05D5\u05E9\u05DC\u05DD. \u05D4\u05E6\u05DC\u05D9\u05D7\u05D5: " + count + ", \u05E0\u05DB\u05E9\u05DC\u05D5: " + failed, "ok");
        }
        else if (gcal == "denied")
            SetStatus("\u05D4\u05D4\u05E8\u05E9\u05D0\u05D4 \u05DC\u05D9\u05D5\u05DE\u05DF Google \u05D1\u05D5\u05D8\u05DC\u05D4.", "err");
        else if (gcal == "error")
        {
            string msg = Request.QueryString["msg"];
            try { if (!string.IsNullOrEmpty(msg)) msg = Uri.UnescapeDataString(msg); } catch { }
            SetStatus(string.IsNullOrEmpty(msg) ? "\u05E9\u05D2\u05D9\u05D0\u05D4 \u05D1\u05D9\u05D9\u05E6\u05D5\u05D0 \u05D4\u05D9\u05E9\u05D9\u05E8 \u05DC-Google." : ("\u05E9\u05D2\u05D9\u05D0\u05D4: " + msg), "err");
        }
        else if (gcal == "needauth")
            SetStatus("\u05E0\u05D3\u05E8\u05E9 \u05D7\u05D9\u05D1\u05D5\u05E8 \u05DE\u05D7\u05D3\u05E9 \u05DC-Google \u05E2\u05D1\u05D5\u05E8 \u05D9\u05D9\u05E6\u05D5\u05D0 \u05D9\u05E9\u05D9\u05E8.", "info");
    }

    private void SetStatus(string text, string kind)
    {
        lblStatus.Text = text;
        lblStatus.CssClass = "export-status " + kind;
    }

    protected string FormatDate(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";
        DateTime dt;
        if (value is DateTime)
            return ((DateTime)value).ToString("dd/MM/yyyy");
        if (DateTime.TryParse(Convert.ToString(value), out dt))
            return dt.ToString("dd/MM/yyyy");
        return Convert.ToString(value);
    }

    protected void btnRefreshIcs_Click(object sender, EventArgs e)
    {
        BindPage();
        SetStatus("\u05E7\u05D5\u05D3 \u05D4-ICS \u05E2\u05D5\u05D3\u05DB\u05DF.", "ok");
    }

    protected void btnDownloadIcs_Click(object sender, EventArgs e)
    {
        int userId = CurrentUserId();
        string ics = GoogleCalendarService.BuildIcsForUser(userId);
        if (string.IsNullOrWhiteSpace(ics) || !ics.Contains("BEGIN:VEVENT"))
        {
            SetStatus("\u05D0\u05D9\u05DF \u05D0\u05D9\u05E8\u05D5\u05E2\u05D9\u05DD \u05DC\u05D4\u05D5\u05E8\u05D3\u05D4.", "err");
            BindPage();
            return;
        }

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] content = Encoding.UTF8.GetBytes(ics);
        byte[] all = new byte[preamble.Length + content.Length];
        System.Buffer.BlockCopy(preamble, 0, all, 0, preamble.Length);
        System.Buffer.BlockCopy(content, 0, all, preamble.Length, content.Length);

        Response.Clear();
        Response.ContentType = "text/calendar; charset=utf-8";
        Response.AddHeader("Content-Disposition", "attachment; filename=optischedule-export.ics");
        Response.BinaryWrite(all);
        Response.Flush();
        HttpContext.Current.ApplicationInstance.CompleteRequest();
    }

    protected void btnExportApi_Click(object sender, EventArgs e)
    {
        try
        {
            int userId = CurrentUserId();
            if (!GoogleCalendarService.HasCalendarAccess(userId))
            {
                Response.Redirect(GoogleCalendarService.GetCalendarAuthorizationUrl(), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            ExportResult result = GoogleCalendarService.ExportUserEventsToGoogle(userId);
            if (result.Exported == 0 && result.Failed > 0)
            {
                SetStatus("\u05D4\u05D9\u05D9\u05E6\u05D5\u05D0 \u05E0\u05DB\u05E9\u05DC \u05DC\u05DB\u05DC \u05D4\u05D0\u05D9\u05E8\u05D5\u05E2\u05D9\u05DD. " + (result.LastError ?? "") + " \u05E0\u05E1\u05D4 \u05D7\u05D9\u05D1\u05D5\u05E8 \u05DE\u05D7\u05D3\u05E9 \u05DC-Google.", "err");
            }
            else
            {
                SetStatus("\u05D4\u05D9\u05D9\u05E6\u05D5\u05D0 \u05D4\u05D9\u05E9\u05D9\u05E8 \u05D4\u05D5\u05E9\u05DC\u05DD. \u05D4\u05E6\u05DC\u05D9\u05D7\u05D5: " + result.Exported + " \u05DE\u05EA\u05D5\u05DA " + result.Total +
                    (result.Failed > 0 ? (" (\u05E0\u05DB\u05E9\u05DC\u05D5: " + result.Failed + ")") : ""), "ok");
            }
            BindPage();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "NO_CALENDAR_TOKEN")
            {
                Response.Redirect(GoogleCalendarService.GetCalendarAuthorizationUrl(), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SetStatus("\u05E9\u05D2\u05D9\u05D0\u05D4: " + ex.Message, "err");
            BindPage();
        }
        catch (Exception ex)
        {
            LoggingService.Log("EXPORT_GOOGLE_PAGE", ex.Message, ex);
                        if ((ex.Message ?? "").IndexOf("Calendar API", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (ex.Message ?? "").IndexOf("Cloud Console", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetStatus("\u05D9\u05E9 \u05DC\u05D4\u05E4\u05E2\u05D9\u05DC Google Calendar API \u05D1-Cloud Console (\u05E8\u05D0\u05D4 \u05E7\u05D9\u05E9\u05D5\u05E8 \u05D1\u05E1\u05E2\u05D9\u05E3 2), \u05DC\u05D7\u05DB\u05D5\u05EA \u05DB\u05DE\u05D4 \u05D3\u05E7\u05D5\u05EA, \u05D5\u05DC\u05E0\u05E1\u05D5\u05EA \u05E9\u05D5\u05D1. \u05D1\u05D9\u05E0\u05EA\u05D9\u05D9\u05DD \u05D4\u05E9\u05EA\u05DE\u05E9 \u05D1\u05D4\u05D5\u05E8\u05D3\u05EA ICS.", "err");
            }
            else
            {
                SetStatus("\u05E9\u05D2\u05D9\u05D0\u05D4 \u05D1\u05D9\u05D9\u05E6\u05D5\u05D0 \u05D9\u05E9\u05D9\u05E8: " + ex.Message, "err");
            }
            BindPage();
        }
    }

    protected void btnReconnect_Click(object sender, EventArgs e)
    {
        try
        {
            int userId = CurrentUserId();
            GoogleCalendarService.ClearTokens(userId);
            Response.Redirect(GoogleCalendarService.GetCalendarAuthorizationUrl(), false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            SetStatus("\u05E9\u05D2\u05D9\u05D0\u05D4 \u05D1\u05D7\u05D9\u05D1\u05D5\u05E8 \u05DE\u05D7\u05D3\u05E9: " + ex.Message, "err");
        }
    }
}