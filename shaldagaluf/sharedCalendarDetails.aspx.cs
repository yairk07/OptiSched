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

        string deleteEventId = Request.QueryString["deleteEvent"];
        if (!string.IsNullOrEmpty(deleteEventId))
        {
            int eventId;
            if (int.TryParse(deleteEventId, out eventId))
            {
                string role = Session["Role"] != null ? Session["Role"].ToString() : "";
                bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
                bool userIsAdmin = isAdmin || isOwner;
                bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);
                
                if (canEdit)
                {
                    service.DeleteSharedCalendarEvent(eventId);
                }
                Response.Redirect("sharedCalendarDetails.aspx?id=" + calendarId);
                return;
            }
        }

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

        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
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
            
            string userRole = Session["Role"] != null ? Session["Role"].ToString() : "";
            bool userIsOwner = string.Equals(userRole, "owner", StringComparison.OrdinalIgnoreCase);
            bool canEdit = isAdmin || userIsOwner || service.CanUserEdit(calendarId, currentUserId);
            
            BindCalendar();
            
            if (!IsPostBack)
            {
                calEvents.SelectedDate = DateTime.Today;
                txtEventDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                ShowEvents(DateTime.Today);
            }
            else
            {
                if (calEvents.SelectedDate != DateTime.MinValue)
                {
                    ShowEvents(calEvents.SelectedDate);
                }
            }
        }
    }

    private void SaveParsedEvents(string json)
    {
        try
        {
            var serializer = new JavaScriptSerializer();
            var events = serializer.Deserialize<List<Dictionary<string, object>>>(json);

            string role = Session["Role"] != null ? Session["Role"].ToString() : "";
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

                    DateTime eventDate;
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out eventDate))
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
        try
        {
            string message = txtJoinMessage.Text.Trim();
            
            if (calendarId <= 0)
            {
                lblJoinMessage.Text = "שגיאה: לא ניתן לזהות את הטבלה.";
                lblJoinMessage.ForeColor = System.Drawing.Color.Red;
                lblJoinMessage.Visible = true;
                return;
            }
            
            if (currentUserId <= 0)
            {
                lblJoinMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש.";
                lblJoinMessage.ForeColor = System.Drawing.Color.Red;
                lblJoinMessage.Visible = true;
                return;
            }
            
            service.CreateJoinRequest(calendarId, currentUserId, message);
            lblJoinMessage.Text = "בקשתך נשלחה בהצלחה! המנהל יקבל התראה.";
            lblJoinMessage.ForeColor = System.Drawing.Color.Green;
            lblJoinMessage.Visible = true;
            txtJoinMessage.Text = "";
            
            Response.Redirect(Request.Url.AbsolutePath + "?id=" + calendarId, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            lblJoinMessage.Text = "שגיאה בשליחת הבקשה: " + ex.Message;
            lblJoinMessage.ForeColor = System.Drawing.Color.Red;
            lblJoinMessage.Visible = true;
            System.Diagnostics.Debug.WriteLine("Error sending join request: " + ex.Message);
        }
    }

    protected void btnTabEvents_Click(object sender, EventArgs e)
    {
        pnlEvents.Visible = true;
        pnlRequests.Visible = false;
        btnTabEvents.CssClass = "tab-button active";
        btnTabRequests.CssClass = "tab-button";
        BindCalendar();
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


    private void LoadRequests()
    {
        DataTable dt = service.GetJoinRequests(calendarId, currentUserId);
        if (dt == null || dt.Rows.Count == 0)
        {
            lblNoRequests.Visible = true;
            dlRequests.Visible = false;
            return;
        }
        
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
        
        lblNoRequests.Visible = false;
        dlRequests.Visible = true;
        dlRequests.DataSource = dt;
        dlRequests.DataBind();
    }

    protected void btnAddEvent_Click(object sender, EventArgs e)
    {
        ViewState["EditingEventId"] = null;
        ClearEventForm();
        if (calEvents.SelectedDate != DateTime.MinValue)
        {
            txtEventDate.Text = calEvents.SelectedDate.ToString("yyyy-MM-dd");
        }
        else
        {
            txtEventDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
            calEvents.SelectedDate = DateTime.Today;
        }
        ShowEvents(calEvents.SelectedDate);
    }

    protected void btnSaveEvent_Click(object sender, EventArgs e)
    {
        lblSaveError.Visible = false;
        lblSaveError.Text = "";
        
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;
        bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);

        if (!canEdit)
        {
            lblSaveError.Text = "אין לך הרשאה לשמור אירועים";
            lblSaveError.Visible = true;
            return;
        }

        try
        {
            string titleText = txtEventTitle.Text != null ? txtEventTitle.Text.Trim() : "";
            string title = Connect.FixEncoding(titleText);
            string dateStr = txtEventDate.Text;
            string timeText = txtEventTime.Text != null ? txtEventTime.Text.Trim() : "";
            string time = Connect.FixEncoding(timeText);
            string notesText = txtEventNotes.Text != null ? txtEventNotes.Text.Trim() : "";
            string notes = Connect.FixEncoding(notesText);
            string categoryValue = ddlEventCategory.SelectedValue != null ? ddlEventCategory.SelectedValue.Trim() : "אחר";
            string category = Connect.FixEncoding(categoryValue);

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

            ViewState["EditingEventId"] = null;
            ClearEventForm();
            BindCalendar();
            ShowEvents(calEvents.SelectedDate != DateTime.MinValue ? calEvents.SelectedDate : DateTime.Today);
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
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;
        bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);
        
        if (!canEdit)
            return;

        LinkButton btn = sender as LinkButton;
        int eventId = Convert.ToInt32(btn.CommandArgument);

        DataTable dt = service.GetSharedCalendarEvents(calendarId, currentUserId);
        if (dt == null)
            return;
            
        DataRow[] rows = dt.Select(string.Format("Id = {0}", eventId));
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
            if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
            {
                DateTime eventDate = Convert.ToDateTime(row["EventDate"]);
                calEvents.SelectedDate = eventDate;
                txtEventDate.Text = eventDate.ToString("yyyy-MM-dd");
                ShowEvents(eventDate);
            }
        }
    }

    protected void lnkDelete_Click(object sender, EventArgs e)
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;
        bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);
        
        if (!canEdit)
            return;

        LinkButton btn = sender as LinkButton;
        int eventId = Convert.ToInt32(btn.CommandArgument);
        service.DeleteSharedCalendarEvent(eventId);
        BindCalendar();
    }

    protected void dlRequests_ItemDataBound(object sender, DataListItemEventArgs e)
    {
        if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
        {
            DropDownList ddlPermission = (DropDownList)e.Item.FindControl("ddlPermission");
            if (ddlPermission != null)
            {
                ddlPermission.SelectedValue = "Read";
            }
        }
    }

    protected void btnApprove_Click(object sender, EventArgs e)
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        Button btn = sender as Button;
        int requestId = Convert.ToInt32(btn.CommandArgument);

        DataListItem item = (DataListItem)btn.NamingContainer;
        DropDownList ddlPermission = (DropDownList)item.FindControl("ddlPermission");
        string permission = "Read";
        if (ddlPermission != null)
        {
            permission = ddlPermission.SelectedValue;
        }

        DataTable dt = service.GetJoinRequests(calendarId, currentUserId);
        if (dt == null)
            return;
            
        DataRow[] rows = dt.Select(string.Format("RequestId = {0}", requestId));
        if (rows.Length > 0)
        {
            DataRow row = rows[0];
            if (row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value && row["UserId"] != null)
            {
                int userId = Convert.ToInt32(row["UserId"]);
                service.ApproveJoinRequest(requestId, calendarId, userId, permission);
                LoadRequests();
            }
        }
    }

    protected void btnReject_Click(object sender, EventArgs e)
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
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

    private void BindCalendar()
    {
        try
        {
            DateTime currentDate = calEvents.VisibleDate;
            if (currentDate == DateTime.MinValue || currentDate == DateTime.MaxValue || currentDate.Year < 1 || currentDate.Year > 9999)
            {
                currentDate = DateTime.Now;
                calEvents.VisibleDate = currentDate;
            }
            
            lblCurrentMonth.Text = currentDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("he-IL"));
            
            DataTable eventsData = service.GetSharedCalendarEvents(calendarId, currentUserId);
            if (eventsData == null || eventsData.Rows.Count == 0)
            {
                ViewState["EventsData"] = new DataTable();
            }
            else
            {
                ViewState["EventsData"] = eventsData;
            }
        }
        catch
        {
        }
    }

    protected void btnMonthChange_Click(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        DateTime currentDate = calEvents.VisibleDate;
        
        if (currentDate == DateTime.MinValue || currentDate == DateTime.MaxValue)
        {
            currentDate = DateTime.Now;
        }
        
        try
        {
            if (btn.CommandArgument == "prev")
            {
                DateTime newDate = currentDate.AddMonths(-1);
                if (newDate.Year >= 1 && newDate.Year <= 9999)
                {
                    calEvents.VisibleDate = newDate;
                }
            }
            else
            {
                DateTime newDate = currentDate.AddMonths(1);
                if (newDate.Year >= 1 && newDate.Year <= 9999)
                {
                    calEvents.VisibleDate = newDate;
                }
            }
        }
        catch
        {
            calEvents.VisibleDate = DateTime.Now;
        }
        
        BindCalendar();
    }

    protected void calEvents_VisibleMonthChanged(object sender, MonthChangedEventArgs e)
    {
        try
        {
            if (e.NewDate.Year >= 1 && e.NewDate.Year <= 9999)
            {
                calEvents.VisibleDate = e.NewDate;
            }
            else
            {
                calEvents.VisibleDate = DateTime.Now;
            }
        }
        catch
        {
            calEvents.VisibleDate = DateTime.Now;
        }
        
        BindCalendar();
    }

    protected void calEvents_DayRender(object sender, DayRenderEventArgs e)
    {
        try
        {
            e.Cell.CssClass = "day-cell";
            e.Cell.Controls.Clear();

            LiteralControl dayNumber = new LiteralControl(string.Format("<div class='day-number'>{0}</div>", e.Day.Date.Day));
            e.Cell.Controls.Add(dayNumber);

            if (!e.Day.IsOtherMonth)
            {
                DataTable eventsData = ViewState["EventsData"] as DataTable;
                if (eventsData != null && eventsData.Rows.Count > 0)
                {
                    Panel eventsPanel = new Panel();
                    eventsPanel.CssClass = "day-events";
                    
                    DateTime targetDate = e.Day.Date.Date;
                    
                    foreach (DataRow row in eventsData.Rows)
                    {
                        try
                        {
                            if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
                            {
                                DateTime eventDate = Convert.ToDateTime(row["EventDate"]).Date;
                                if (eventDate == targetDate)
                                {
                                    string title = "";
                                    if (row.Table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
                                    {
                                        title = Connect.FixEncoding(row["Title"].ToString());
                                    }
                                    
                                    string time = "";
                                    if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                                    {
                                        time = Connect.FixEncoding(row["EventTime"].ToString());
                                    }
                                    
                                    string eventId = "";
                                    if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                                    {
                                        eventId = row["Id"].ToString();
                                    }
                                    
                                    string createdByName = "";
                                    if (row.Table.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                                    {
                                        createdByName = Connect.FixEncoding(row["CreatedByName"].ToString());
                                    }
                                    
                                    if (string.IsNullOrEmpty(title))
                                        continue;
                                    
                                    string displayText = title;
                                    if (displayText.Length > 18)
                                        displayText = displayText.Substring(0, 18) + "...";
                                    
                                    HyperLink eventLink = new HyperLink();
                                    eventLink.CssClass = "event-badge";
                                    eventLink.Text = displayText;
                                    eventLink.NavigateUrl = string.Format("eventDetails.aspx?id={0}", eventId);
                                    eventLink.ToolTip = string.Format("{0}\nמשתמש: {1}\n{2}", title, createdByName, time);
                                    
                                    eventsPanel.Controls.Add(eventLink);
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    if (eventsPanel.Controls.Count > 0)
                        e.Cell.Controls.Add(eventsPanel);
                }
            }
        }
        catch
        {
        }
    }

    protected void calEvents_SelectionChanged(object sender, EventArgs e)
    {
        DateTime selectedDate = calEvents.SelectedDate;
        if (selectedDate != DateTime.MinValue)
        {
            txtEventDate.Text = selectedDate.ToString("yyyy-MM-dd");
            ViewState["EditingEventId"] = null;
            ClearEventForm();
            txtEventDate.Text = selectedDate.ToString("yyyy-MM-dd");
            ShowEvents(selectedDate);
        }
    }

    private void ShowEvents(DateTime date)
    {
        var builder = new StringBuilder();
        int count = 0;

        DataTable eventsData = ViewState["EventsData"] as DataTable;
        if (eventsData != null && eventsData.Rows.Count > 0)
        {
            builder.Append("<div class='events-table-container'>");
            builder.Append("<table class='events-table'>");
            builder.Append("<thead>");
            builder.Append("<tr>");
            builder.Append("<th>כותרת</th>");
            builder.Append("<th>קטגוריה</th>");
            builder.Append("<th>שעה</th>");
            builder.Append("<th>הערות</th>");
            builder.Append("<th>נוצר על ידי</th>");
            builder.Append("<th>פעולות</th>");
            builder.Append("</tr>");
            builder.Append("</thead>");
            builder.Append("<tbody>");

            foreach (DataRow row in eventsData.Rows)
            {
                try
                {
                    if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
                    {
                        DateTime eventDate = Convert.ToDateTime(row["EventDate"]).Date;
                        if (eventDate == date.Date)
                        {
                            string title = "";
                            if (row.Table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
                                title = Connect.FixEncoding(row["Title"].ToString());

                            string category = "";
                            if (row.Table.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
                                category = Connect.FixEncoding(row["Category"].ToString());

                            string time = "";
                            if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                                time = Connect.FixEncoding(row["EventTime"].ToString());

                            string notes = "";
                            if (row.Table.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
                                notes = Connect.FixEncoding(row["Notes"].ToString());

                            string createdByName = "";
                            if (row.Table.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                                createdByName = Connect.FixEncoding(row["CreatedByName"].ToString());

                            string eventId = "";
                            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                                eventId = row["Id"].ToString();

                            if (string.IsNullOrEmpty(title))
                                title = "ללא כותרת";

                            string role = Session["Role"] != null ? Session["Role"].ToString() : "";
                            bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
                            bool userIsAdmin = isAdmin || isOwner;
                            bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);

                            builder.Append("<tr>");
                            builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(title));
                            builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(category));
                            builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(time));
                            builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(notes));
                            builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(createdByName));
                            
                            builder.Append("<td>");
                            if (canEdit)
                            {
                                builder.AppendFormat("<a href='editEvent.aspx?id={0}' class='edit-link'>ערוך</a> ", eventId);
                                builder.AppendFormat("<a href='javascript:void(0)' onclick='if(confirm(\"האם אתה בטוח שברצונך למחוק את האירוע?\")) window.location.href=\"sharedCalendarDetails.aspx?id={0}&deleteEvent={1}\"' class='delete-link'>מחק</a>", calendarId, eventId);
                            }
                            builder.Append("</td>");
                            builder.Append("</tr>");
                            count++;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            builder.Append("</tbody>");
            builder.Append("</table>");
            builder.Append("</div>");
        }

        if (count == 0)
        {
            builder.Append("<div class='calendar-event empty'>אין אירועים לתאריך הזה.</div>");
        }

        lblEvents.Text = builder.ToString();
    }
}
