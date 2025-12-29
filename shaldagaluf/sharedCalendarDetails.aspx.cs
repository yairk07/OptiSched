using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

public partial class sharedCalendarDetails : System.Web.UI.Page
{
    SharedCalendarService service = new SharedCalendarService();
    private int calendarId;
    private int currentUserId;
    private bool isAdmin = false;
    private bool isMember = false;

    public bool IsAdmin { get { return isAdmin; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!int.TryParse(Request.QueryString["id"], out calendarId))
        {
            ShowNotFound();
            return;
        }

        currentUserId = Convert.ToInt32(Session["userId"]);

        if (!IsPostBack)
        {
            LoadCalendar();
        }
    }

    private string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        try
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
            string decoded = Encoding.UTF8.GetString(utf8Bytes);
            
            if (decoded == text)
                return text;
            
            byte[] windows1255Bytes = Encoding.GetEncoding("Windows-1255").GetBytes(text);
            return Encoding.UTF8.GetString(windows1255Bytes);
        }
        catch
        {
            return text;
        }
    }

    private void LoadCalendar()
    {
        DataRow calendar = service.GetSharedCalendar(calendarId);
        if (calendar == null)
        {
            ShowNotFound();
            return;
        }

        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        
        isAdmin = service.IsCalendarAdmin(calendarId, currentUserId) || isOwner;
        isMember = service.IsCalendarMember(calendarId, currentUserId) || isAdmin;

        calendarTitle.Text = calendar["CalendarName"].ToString();
        calendarDescription.Text = calendar["Description"]?.ToString() ?? "";

        if (!isMember)
        {
            pnlNotMember.Visible = true;
            pnlMember.Visible = false;
        }
        else
        {
            pnlNotMember.Visible = false;
            pnlMember.Visible = true;
            btnTabRequests.Visible = isAdmin;
            btnAddEvent.Visible = isAdmin;
            LoadEvents();
        }
    }

    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }

    protected void btnSendJoinRequest_Click(object sender, EventArgs e)
    {
        string message = txtJoinMessage.Text.Trim();
        service.CreateJoinRequest(calendarId, currentUserId, message);
        lblJoinMessage.Text = "בקשתך נשלחה בהצלחה! המנהל יקבל התראה.";
        lblJoinMessage.ForeColor = System.Drawing.Color.Green;
        txtJoinMessage.Text = "";
    }

    protected void btnTabEvents_Click(object sender, EventArgs e)
    {
        pnlEvents.Visible = true;
        pnlRequests.Visible = false;
        btnTabEvents.CssClass = "tab-button active";
        btnTabRequests.CssClass = "tab-button";
        LoadEvents();
    }

    protected void btnTabRequests_Click(object sender, EventArgs e)
    {
        pnlEvents.Visible = false;
        pnlRequests.Visible = true;
        btnTabEvents.CssClass = "tab-button";
        btnTabRequests.CssClass = "tab-button active";
        LoadRequests();
    }

    private bool IsInvalidValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        
        string trimmed = value.Trim();
        return trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." || 
               trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟" ||
               (trimmed.Length <= 1 && (trimmed == "." || trimmed == "؟"));
    }

    private void LoadEvents()
    {
        DataTable dt = service.GetSharedCalendarEvents(calendarId, currentUserId);
        
        foreach (DataRow row in dt.Rows)
        {
            if (dt.Columns.Contains("Title"))
            {
                object titleObj = row["Title"];
                string title = titleObj != null && titleObj != DBNull.Value ? Connect.FixEncoding(titleObj.ToString()) : "";
                
                if (IsInvalidValue(title))
                    row["Title"] = "ללא כותרת";
                else
                    row["Title"] = title.Trim();
            }
            
            if (dt.Columns.Contains("Notes"))
            {
                object notesObj = row["Notes"];
                string notes = notesObj != null && notesObj != DBNull.Value ? Connect.FixEncoding(notesObj.ToString()) : "";
                
                if (IsInvalidValue(notes))
                    row["Notes"] = "";
                else
                    row["Notes"] = notes.Trim();
            }
            
            if (dt.Columns.Contains("EventTime"))
            {
                object timeObj = row["EventTime"];
                string time = timeObj != null && timeObj != DBNull.Value ? Connect.FixEncoding(timeObj.ToString()) : "";
                
                if (IsInvalidValue(time))
                    row["EventTime"] = "";
                else
                    row["EventTime"] = time.Trim();
            }
            
            if (dt.Columns.Contains("Category"))
            {
                object categoryObj = row["Category"];
                string category = categoryObj != null && categoryObj != DBNull.Value ? Connect.FixEncoding(categoryObj.ToString()) : "";
                
                if (IsInvalidValue(category))
                    row["Category"] = "אחר";
                else
                    row["Category"] = category.Trim();
            }
            
            if (dt.Columns.Contains("CreatedByName"))
            {
                object createdByObj = row["CreatedByName"];
                string createdBy = createdByObj != null && createdByObj != DBNull.Value ? Connect.FixEncoding(createdByObj.ToString()) : "";
                
                if (IsInvalidValue(createdBy))
                    row["CreatedByName"] = "ללא שם";
                else
                    row["CreatedByName"] = createdBy.Trim();
            }
        }
        
        dlEvents.DataSource = dt;
        dlEvents.DataBind();
        
        foreach (DataListItem item in dlEvents.Items)
        {
            LinkButton lnkEdit = item.FindControl("lnkEdit") as LinkButton;
            LinkButton lnkDelete = item.FindControl("lnkDelete") as LinkButton;
            
            if (lnkEdit != null)
                lnkEdit.Visible = isAdmin;
            if (lnkDelete != null)
                lnkDelete.Visible = isAdmin;
        }
    }

    private void LoadRequests()
    {
        DataTable dt = service.GetJoinRequests(calendarId, currentUserId);
        
        foreach (DataRow row in dt.Rows)
        {
            if (dt.Columns.Contains("UserName") && row["UserName"] != DBNull.Value)
                row["UserName"] = Connect.FixEncoding(row["UserName"].ToString());
            if (dt.Columns.Contains("Message") && row["Message"] != DBNull.Value)
                row["Message"] = Connect.FixEncoding(row["Message"].ToString());
        }
        
        dlRequests.DataSource = dt;
        dlRequests.DataBind();
    }

    protected void btnAddEvent_Click(object sender, EventArgs e)
    {
        pnlAddEvent.Visible = true;
        btnAddEvent.Visible = false;
        ViewState["EditingEventId"] = null;
        ClearEventForm();
    }

    protected void btnCancelEvent_Click(object sender, EventArgs e)
    {
        pnlAddEvent.Visible = false;
        btnAddEvent.Visible = true;
        ClearEventForm();
    }

    protected void btnSaveEvent_Click(object sender, EventArgs e)
    {
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;

        if (!userIsAdmin)
            return;

        try
        {
            string title = txtEventTitle.Text?.Trim() ?? "";
            string dateStr = txtEventDate.Text;
            string time = txtEventTime.Text?.Trim() ?? "";
            string notes = txtEventNotes.Text?.Trim() ?? "";
            string category = ddlEventCategory.SelectedValue?.Trim() ?? "אחר";

            if (string.IsNullOrWhiteSpace(title) || IsInvalidValue(title))
                return;

            if (string.IsNullOrEmpty(dateStr))
                return;

            if (IsInvalidValue(time))
                time = "";

            if (IsInvalidValue(notes))
                notes = "";

            if (IsInvalidValue(category))
                category = "אחר";

            DateTime eventDate = DateTime.Parse(dateStr);
            int? editingId = ViewState["EditingEventId"] as int?;
            
            if (editingId.HasValue)
                service.UpdateSharedCalendarEvent(editingId.Value, title, eventDate, time, notes, category);
            else
                service.AddSharedCalendarEvent(calendarId, title, eventDate, time, notes, category, currentUserId);

            pnlAddEvent.Visible = false;
            btnAddEvent.Visible = true;
            ClearEventForm();
            LoadEvents();
        }
        catch
        {
        }
    }

    protected void lnkEdit_Click(object sender, EventArgs e)
    {
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        LinkButton btn = sender as LinkButton;
        int eventId = Convert.ToInt32(btn.CommandArgument);

        DataTable dt = service.GetSharedCalendarEvents(calendarId, currentUserId);
        DataRow[] rows = dt.Select($"Id = {eventId}");
        if (rows.Length > 0)
        {
            DataRow row = rows[0];
            
            string title = Connect.FixEncoding(row["Title"]?.ToString()?.Trim() ?? "");
            if (title == "...." || title == "..." || title == "ללא כותרת")
                title = "";
            txtEventTitle.Text = title;
            
            txtEventDate.Text = Convert.ToDateTime(row["EventDate"]).ToString("yyyy-MM-dd");
            
            string time = Connect.FixEncoding(row["EventTime"]?.ToString()?.Trim() ?? "");
            if (time == "...." || time == "...")
                time = "";
            txtEventTime.Text = time;
            
            string notes = Connect.FixEncoding(row["Notes"]?.ToString()?.Trim() ?? "");
            if (notes == "...." || notes == "...")
                notes = "";
            txtEventNotes.Text = notes;
            
            if (row["Category"] != DBNull.Value && row["Category"] != null)
            {
                string category = Connect.FixEncoding(row["Category"].ToString().Trim());
                if (category != "...." && category != "..." && ddlEventCategory.Items.FindByValue(category) != null)
                    ddlEventCategory.SelectedValue = category;
            }
            ViewState["EditingEventId"] = eventId;
            pnlAddEvent.Visible = true;
            btnAddEvent.Visible = false;
        }
    }

    protected void lnkDelete_Click(object sender, EventArgs e)
    {
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        LinkButton btn = sender as LinkButton;
        int eventId = Convert.ToInt32(btn.CommandArgument);
        service.DeleteSharedCalendarEvent(eventId);
        LoadEvents();
    }

    protected void btnApprove_Click(object sender, EventArgs e)
    {
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        Button btn = sender as Button;
        int requestId = Convert.ToInt32(btn.CommandArgument);

        DataTable dt = service.GetJoinRequests(calendarId, currentUserId);
        DataRow[] rows = dt.Select($"RequestId = {requestId}");
        if (rows.Length > 0)
        {
            int userId = Convert.ToInt32(rows[0]["UserId"]);
            service.ApproveJoinRequest(requestId, calendarId, userId);
            LoadRequests();
        }
    }

    protected void btnReject_Click(object sender, EventArgs e)
    {
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        Button btn = sender as Button;
        int requestId = Convert.ToInt32(btn.CommandArgument);
        service.RejectJoinRequest(requestId);
        LoadRequests();
    }

    private void ClearEventForm()
    {
        txtEventTitle.Text = "";
        txtEventDate.Text = "";
        txtEventTime.Text = "";
        txtEventNotes.Text = "";
        ddlEventCategory.SelectedIndex = 0;
    }

    protected string GetSafeString(object value, string defaultValue = "")
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;
        
        string str = value.ToString();
        if (string.IsNullOrWhiteSpace(str))
            return defaultValue;
        
        str = str.Trim();
        if (IsInvalidValue(str))
            return defaultValue;
        
        return str;
    }

    protected string GetSafeDate(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";
        
        try
        {
            return Convert.ToDateTime(value).ToString("dd/MM/yyyy");
        }
        catch
        {
            return "";
        }
    }
}
