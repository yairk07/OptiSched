using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Linq;

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
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
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

        if (Session["userId"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        currentUserId = Convert.ToInt32(Session["userId"]);

        string parsedEventsJson = Request.Form["parsedEventsJson"];
        if (!string.IsNullOrEmpty(parsedEventsJson))
        {
            if (!IsPostBack || ViewState["EventsSaved"] == null)
            {
                SaveParsedEvents(parsedEventsJson);
                ViewState["EventsSaved"] = true;
            }
        }

        if (!IsPostBack)
        {
            ViewState["EventsSaved"] = null;
            LoadCalendar();
        }
        else
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

        if (calendar.Table.Columns.Contains("CalendarName") && calendar["CalendarName"] != DBNull.Value && calendar["CalendarName"] != null)
            calendarTitle.Text = Connect.FixEncoding(calendar["CalendarName"].ToString());
        else
            calendarTitle.Text = "";
            
        if (calendar.Table.Columns.Contains("Description") && calendar["Description"] != DBNull.Value && calendar["Description"] != null)
            calendarDescription.Text = Connect.FixEncoding(calendar["Description"].ToString());
        else
            calendarDescription.Text = "";

        if (!isMember)
        {
            pnlNotMember.Visible = true;
            pnlMember.Visible = false;
        }
        else
        {
            pnlNotMember.Visible = false;
            pnlMember.Visible = true;
            pnlEvents.Visible = true;
            pnlRequests.Visible = false;
            btnTabRequests.Visible = isAdmin;
            btnAddEvent.Visible = isAdmin;
            if (!IsPostBack)
            {
                pnlAddEvent.Visible = false;
            }
            LoadEvents();
        }
    }

    private void SaveParsedEvents(string json)
    {
        try
        {
            var serializer = new JavaScriptSerializer();
            var events = serializer.Deserialize<List<Dictionary<string, object>>>(json);

            string role = Session["Role"]?.ToString() ?? "";
            bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
            bool userIsAdmin = isAdmin || isOwner;

            if (!userIsAdmin)
            {
                return;
            }

            int savedCount = 0;
            int errorCount = 0;
            foreach (var eventData in events)
            {
                try
                {
                    string dateStr = eventData.ContainsKey("date") ? eventData["date"].ToString() : "";
                    string title = eventData.ContainsKey("title") ? Connect.FixEncoding(eventData["title"].ToString().Trim()) : "";
                    string startTime = eventData.ContainsKey("startTime") ? Connect.FixEncoding(eventData["startTime"].ToString().Trim()) : "";
                    string endTime = eventData.ContainsKey("endTime") ? Connect.FixEncoding(eventData["endTime"].ToString().Trim()) : "";
                    string location = eventData.ContainsKey("location") ? Connect.FixEncoding(eventData["location"].ToString().Trim()) : "";
                    string description = eventData.ContainsKey("description") ? Connect.FixEncoding(eventData["description"].ToString().Trim()) : "";

                    if (string.IsNullOrEmpty(title))
                    {
                        title = "אירוע";
                    }

                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime eventDate))
                    {
                        string time = "";
                        if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                        {
                            time = startTime + " - " + endTime;
                        }
                        else if (!string.IsNullOrEmpty(startTime))
                        {
                            time = startTime;
                        }

                        string notes = "";
                        if (!string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(description))
                        {
                            notes = "מיקום: " + location + "\n" + description;
                        }
                        else if (!string.IsNullOrEmpty(location))
                        {
                            notes = "מיקום: " + location;
                        }
                        else if (!string.IsNullOrEmpty(description))
                        {
                            notes = description;
                        }

                        try
                        {
                            service.AddSharedCalendarEvent(calendarId, title, eventDate, time, notes, "אירוע", currentUserId);
                            savedCount++;
                        }
                        catch
                        {
                            errorCount++;
                        }
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch
                {
                    errorCount++;
                }
            }

            string redirectUrl = Request.Url.AbsolutePath + "?id=" + calendarId + "&saved=" + savedCount;
            Response.Redirect(redirectUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch
        {
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
        try
        {
            DataTable dt = service.GetSharedCalendarEvents(calendarId, currentUserId);
            
            if (dt == null)
                dt = new DataTable();
            
            if (dt.Columns.Count == 0 && dt.Rows.Count == 0)
            {
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Title", typeof(string));
                dt.Columns.Add("EventDate", typeof(DateTime));
                dt.Columns.Add("EventTime", typeof(string));
                dt.Columns.Add("Category", typeof(string));
                dt.Columns.Add("Notes", typeof(string));
                dt.Columns.Add("CreatedByName", typeof(string));
            }
            else
            {
                if (!dt.Columns.Contains("Id"))
                    dt.Columns.Add("Id", typeof(int));
                if (!dt.Columns.Contains("Title"))
                    dt.Columns.Add("Title", typeof(string));
                if (!dt.Columns.Contains("EventDate"))
                    dt.Columns.Add("EventDate", typeof(DateTime));
                if (!dt.Columns.Contains("EventTime"))
                    dt.Columns.Add("EventTime", typeof(string));
                if (!dt.Columns.Contains("Category"))
                    dt.Columns.Add("Category", typeof(string));
                if (!dt.Columns.Contains("Notes"))
                    dt.Columns.Add("Notes", typeof(string));
                if (!dt.Columns.Contains("CreatedByName"))
                    dt.Columns.Add("CreatedByName", typeof(string));
                
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        if (dt.Columns.Contains("Title"))
                        {
                            if (row["Title"] == DBNull.Value || row["Title"] == null || string.IsNullOrWhiteSpace(row["Title"].ToString()))
                            {
                                row["Title"] = "ללא כותרת";
                            }
                            else
                            {
                                row["Title"] = Connect.FixEncoding(row["Title"].ToString());
                            }
                        }
                        if (dt.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null && !string.IsNullOrWhiteSpace(row["EventTime"].ToString()))
                        {
                            row["EventTime"] = Connect.FixEncoding(row["EventTime"].ToString());
                        }
                        if (dt.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null && !string.IsNullOrWhiteSpace(row["Notes"].ToString()))
                        {
                            row["Notes"] = Connect.FixEncoding(row["Notes"].ToString());
                        }
                        if (dt.Columns.Contains("Category"))
                        {
                            if (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString()))
                            {
                                row["Category"] = "אחר";
                            }
                            else
                            {
                                row["Category"] = Connect.FixEncoding(row["Category"].ToString());
                            }
                        }
                        if (dt.Columns.Contains("CreatedByName"))
                        {
                            if (row["CreatedByName"] == DBNull.Value || row["CreatedByName"] == null || string.IsNullOrWhiteSpace(row["CreatedByName"].ToString()))
                            {
                                row["CreatedByName"] = "ללא שם";
                            }
                            else
                            {
                                row["CreatedByName"] = Connect.FixEncoding(row["CreatedByName"].ToString());
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            
            if (dt == null || dt.Rows.Count == 0)
            {
                dlEvents.Visible = false;
                lblNoEvents.Visible = true;
            }
            else
            {
                dlEvents.Visible = true;
                lblNoEvents.Visible = false;
                dlEvents.DataSource = dt;
                dlEvents.DataBind();
            }
            
            foreach (RepeaterItem item in dlEvents.Items)
            {
                try
                {
                    LinkButton lnkEdit = item.FindControl("lnkEdit") as LinkButton;
                    LinkButton lnkDelete = item.FindControl("lnkDelete") as LinkButton;
                    
                    if (lnkEdit != null)
                        lnkEdit.Visible = isAdmin;
                    if (lnkDelete != null)
                        lnkDelete.Visible = isAdmin;
                }
                catch
                {
                    continue;
                }
            }
        }
        catch
        {
        }
    }

    private void LoadRequests()
    {
        DataTable dt = service.GetJoinRequests(calendarId, currentUserId);
        if (dt == null)
            return;
        
        foreach (DataRow row in dt.Rows)
        {
            if (row.Table.Columns.Contains("UserName") && row["UserName"] != DBNull.Value && row["UserName"] != null)
                row["UserName"] = Connect.FixEncoding(row["UserName"].ToString());
            if (row.Table.Columns.Contains("firstName") && row["firstName"] != DBNull.Value && row["firstName"] != null)
                row["firstName"] = Connect.FixEncoding(row["firstName"].ToString());
            if (row.Table.Columns.Contains("FirstName") && row["FirstName"] != DBNull.Value && row["FirstName"] != null)
                row["FirstName"] = Connect.FixEncoding(row["FirstName"].ToString());
            if (row.Table.Columns.Contains("lastName") && row["lastName"] != DBNull.Value && row["lastName"] != null)
                row["lastName"] = Connect.FixEncoding(row["lastName"].ToString());
            if (row.Table.Columns.Contains("LastName") && row["LastName"] != DBNull.Value && row["LastName"] != null)
                row["LastName"] = Connect.FixEncoding(row["LastName"].ToString());
            if (row.Table.Columns.Contains("Message") && row["Message"] != DBNull.Value && row["Message"] != null)
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
        lblSaveError.Visible = false;
        lblSaveError.Text = "";
        
        string role = Session["Role"]?.ToString() ?? "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;

        if (!userIsAdmin)
        {
            lblSaveError.Text = "אין לך הרשאה לשמור אירועים";
            lblSaveError.Visible = true;
            return;
        }

        try
        {
            string title = Connect.FixEncoding(txtEventTitle.Text?.Trim() ?? "");
            string dateStr = txtEventDate.Text;
            string time = Connect.FixEncoding(txtEventTime.Text?.Trim() ?? "");
            string notes = Connect.FixEncoding(txtEventNotes.Text?.Trim() ?? "");
            string category = Connect.FixEncoding(ddlEventCategory.SelectedValue?.Trim() ?? "אחר");

            if (string.IsNullOrWhiteSpace(title) || IsInvalidValue(title))
            {
                lblSaveError.Text = "אנא הזן כותרת לאירוע";
                lblSaveError.Visible = true;
                return;
            }

            if (string.IsNullOrEmpty(dateStr))
            {
                lblSaveError.Text = "אנא בחר תאריך לאירוע";
                lblSaveError.Visible = true;
                return;
            }

            if (IsInvalidValue(time))
                time = "";

            if (IsInvalidValue(notes))
                notes = "";

            if (IsInvalidValue(category))
                category = "אחר";

            DateTime eventDate;
            if (!DateTime.TryParse(dateStr, out eventDate))
            {
                lblSaveError.Text = "תאריך לא תקין";
                lblSaveError.Visible = true;
                return;
            }
            
            int? editingId = ViewState["EditingEventId"] as int?;
            
            if (editingId.HasValue)
            {
                service.UpdateSharedCalendarEvent(editingId.Value, title, eventDate, time, notes, category);
            }
            else
            {
                service.AddSharedCalendarEvent(calendarId, title, eventDate, time, notes, category, currentUserId);
            }

            pnlAddEvent.Visible = false;
            btnAddEvent.Visible = true;
            ViewState["EditingEventId"] = null;
            ClearEventForm();
            LoadEvents();
        }
        catch (Exception ex)
        {
            lblSaveError.Text = "שגיאה בשמירת האירוע: " + ex.Message;
            lblSaveError.Visible = true;
            System.Diagnostics.Debug.WriteLine("Error saving event: " + ex.Message);
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
        if (dt == null)
            return;
            
        DataRow[] rows = dt.Select($"Id = {eventId}");
        if (rows.Length > 0)
        {
            DataRow row = rows[0];
            
            string title = "";
            if (row.Table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
            {
                title = Connect.FixEncoding(row["Title"].ToString().Trim());
                if (title == "...." || title == "..." || title == "ללא כותרת")
                    title = "";
            }
            txtEventTitle.Text = title;
            
            if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
            {
                txtEventDate.Text = Convert.ToDateTime(row["EventDate"]).ToString("yyyy-MM-dd");
            }
            
            string time = "";
            if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
            {
                time = Connect.FixEncoding(row["EventTime"].ToString().Trim());
                if (time == "...." || time == "...")
                    time = "";
            }
            txtEventTime.Text = time;
            
            string notes = "";
            if (row.Table.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
            {
                notes = Connect.FixEncoding(row["Notes"].ToString().Trim());
            }
            txtEventNotes.Text = notes;
            
            if (row.Table.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
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
        if (dt == null)
            return;
            
        DataRow[] rows = dt.Select($"RequestId = {requestId}");
        if (rows.Length > 0)
        {
            DataRow row = rows[0];
            if (row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value && row["UserId"] != null)
            {
                int userId = Convert.ToInt32(row["UserId"]);
                service.ApproveJoinRequest(requestId, calendarId, userId);
                LoadRequests();
            }
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
        try
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;
            
            string str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
                return defaultValue;
            
            str = str.Trim();
            if (IsInvalidValue(str))
                return defaultValue;
            
            return Connect.FixEncoding(str);
        }
        catch
        {
            return defaultValue;
        }
    }

    protected string GetSafeDate(object value)
    {
        try
        {
            if (value == null || value == DBNull.Value)
                return "";
            
            return Convert.ToDateTime(value).ToString("dd/MM/yyyy");
        }
        catch
        {
            return "";
        }
    }
}
