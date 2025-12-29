using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class allEvents : System.Web.UI.Page
{
    EventService es = new EventService();

    private DataTable LoadEventsData()
    {
        string cacheKey = "AllEventsData_" + (Session["userId"]?.ToString() ?? "all");
        
        DataTable cachedData = Session[cacheKey] as DataTable;
        if (cachedData != null && cachedData.Rows.Count > 0)
            return cachedData;

        int? userId = null;
        string role = Session["Role"]?.ToString();
        
        if (role != "owner" && Session["userId"] != null)
            userId = Convert.ToInt32(Session["userId"]);
        
        DataTable dt = es.GetAllEvents(userId);
        
        if (dt != null && dt.Rows.Count > 0)
        {
            if (!dt.Columns.Contains("EventDate") && dt.Columns.Contains("date"))
                dt.Columns["date"].ColumnName = "EventDate";
            if (!dt.Columns.Contains("Title") && dt.Columns.Contains("title"))
                dt.Columns["title"].ColumnName = "Title";
            if (!dt.Columns.Contains("EventTime") && dt.Columns.Contains("time"))
                dt.Columns["time"].ColumnName = "EventTime";
            
            if (!dt.Columns.Contains("Category"))
                dt.Columns.Add("Category", typeof(string));
            
            foreach (DataRow row in dt.Rows)
            {
                if (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString()))
                    row["Category"] = "אחר";
            }
            
            Session[cacheKey] = dt;
        }
        else
        {
            dt = new DataTable();
        }
        
        return dt;
    }
    
    private void ClearEventsCache()
    {
        string cacheKey = "AllEventsData_" + (Session["userId"]?.ToString() ?? "all");
        Session.Remove(cacheKey);
    }

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

        // תמיד טוען את הנתונים
        LoadEventsData();

        if (!IsPostBack)
        {
            string viewMode = Request.QueryString["view"] ?? "table";
            SetViewMode(viewMode);
            
            BindData();
            if (viewMode == "calendar")
            {
                BindCalendar();
            }
        }
    }

    private void SetViewMode(string mode)
    {
        if (mode == "calendar")
        {
            pnlTableView.Style["display"] = "none";
            pnlCalendarView.Style["display"] = "block";
            btnTableView.CssClass = "view-btn";
            btnCalendarView.CssClass = "view-btn active";
            
            try
            {
                if (calEvents.VisibleDate == DateTime.MinValue || calEvents.VisibleDate == DateTime.MaxValue)
                    calEvents.VisibleDate = DateTime.Now;
            }
            catch
            {
                calEvents.VisibleDate = DateTime.Now;
            }
        }
        else
        {
            pnlTableView.Style["display"] = "block";
            pnlCalendarView.Style["display"] = "none";
            btnTableView.CssClass = "view-btn active";
            btnCalendarView.CssClass = "view-btn";
        }
    }

    private void BindData(string filter = "", string categoryFilter = "")
    {
        DataTable dt = LoadEventsData();
        DataTable filteredDt = dt.Clone();

        foreach (DataRow row in dt.Rows)
        {
            bool matchesFilter = true;
            bool matchesCategory = true;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                string title = row["Title"]?.ToString() ?? "";
                string userName = row["UserName"]?.ToString() ?? "";
                string notes = row["Notes"]?.ToString() ?? "";
                
                string searchLower = filter.ToLower();
                matchesFilter = title.ToLower().Contains(searchLower) ||
                               userName.ToLower().Contains(searchLower) ||
                               notes.ToLower().Contains(searchLower);
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                string category = row["Category"]?.ToString() ?? "אחר";
                matchesCategory = category == categoryFilter;
            }

            if (matchesFilter && matchesCategory)
            {
                filteredDt.ImportRow(row);
            }
        }

        dlEvents.DataSource = filteredDt;
        dlEvents.DataBind();

        string resultMessage = "";
        if (!string.IsNullOrWhiteSpace(filter) && !string.IsNullOrWhiteSpace(categoryFilter))
        {
            resultMessage = $"נמצאו {filteredDt.Rows.Count} תוצאות עבור: \"{filter}\" בקטגוריה \"{categoryFilter}\"";
        }
        else if (!string.IsNullOrWhiteSpace(filter))
        {
            resultMessage = $"נמצאו {filteredDt.Rows.Count} תוצאות עבור: \"{filter}\"";
        }
        else if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            resultMessage = $"נמצאו {filteredDt.Rows.Count} תוצאות בקטגוריה: \"{categoryFilter}\"";
        }

        lblResult.Text = resultMessage;
    }

    private void BindCalendar()
    {
        DateTime currentDate = calEvents.VisibleDate;
        
        if (currentDate == DateTime.MinValue || currentDate == DateTime.MaxValue || currentDate.Year < 1 || currentDate.Year > 9999)
        {
            currentDate = DateTime.Now;
            calEvents.VisibleDate = currentDate;
        }
        
        lblCurrentMonth.Text = currentDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("he-IL"));
        
        DataTable eventsData = LoadEventsData();
        
        if (eventsData == null || eventsData.Rows.Count == 0)
        {
            lblCalendarMessage.Text = "אין אירועים להצגה";
            lblCalendarMessage.ForeColor = System.Drawing.Color.Gray;
        }
        else
        {
            int eventsInMonth = 0;
            string dateColumn = eventsData.Columns.Contains("EventDate") ? "EventDate" : "date";
            
            foreach (DataRow row in eventsData.Rows)
            {
                if (row[dateColumn] != DBNull.Value && row[dateColumn] != null)
                {
                    try
                    {
                        DateTime eventDate = row[dateColumn] is DateTime 
                            ? ((DateTime)row[dateColumn]).Date 
                            : Convert.ToDateTime(row[dateColumn]).Date;
                        
                        if (eventDate.Year == currentDate.Year && eventDate.Month == currentDate.Month)
                            eventsInMonth++;
                    }
                    catch
                    {
                    }
                }
            }
            
            lblCalendarMessage.Text = $"נמצאו {eventsData.Rows.Count} אירועים בסך הכל ({eventsInMonth} בחודש זה)";
            lblCalendarMessage.ForeColor = System.Drawing.Color.Green;
        }
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        string search = txtSearch.Text.Trim();
        string categoryFilter = ddlCategoryFilter.SelectedValue;
        
        if (string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(categoryFilter))
        {
            ClearEventsCache();
        }
        
        BindData(search, categoryFilter);
    }

    protected void ddlCategoryFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
        string search = txtSearch.Text.Trim();
        string categoryFilter = ddlCategoryFilter.SelectedValue;
        BindData(search, categoryFilter);
    }

    protected void btnViewToggle_Click(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        string mode = btn.CommandArgument;
        ClearEventsCache();
        SetViewMode(mode);
        
        if (mode == "calendar")
        {
            BindCalendar();
        }
        else
        {
            BindData();
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
        e.Cell.CssClass = "day-cell";
        e.Cell.Controls.Clear();

        LiteralControl dayNumber = new LiteralControl($"<div class='day-number'>{e.Day.Date.Day}</div>");
        e.Cell.Controls.Add(dayNumber);

        if (!e.Day.IsOtherMonth)
        {
            try
            {
                DataTable eventsData = LoadEventsData();
                
                if (eventsData != null && eventsData.Rows.Count > 0)
                {
                    DateTime targetDate = e.Day.Date.Date;
                    Panel eventsPanel = new Panel();
                    eventsPanel.CssClass = "day-events";
                    
                    string dateColumn = eventsData.Columns.Contains("EventDate") ? "EventDate" : "date";
                    if (!eventsData.Columns.Contains(dateColumn))
                        return;
                    
                    foreach (DataRow row in eventsData.Rows)
                    {
                        try
                        {
                            object dateObj = row[dateColumn];
                            if (dateObj == DBNull.Value || dateObj == null)
                                continue;

                            DateTime eventDate;
                            
                            if (dateObj is DateTime)
                                eventDate = ((DateTime)dateObj).Date;
                            else
                            {
                                if (!DateTime.TryParse(dateObj.ToString().Trim(), out eventDate))
                                    continue;
                                eventDate = eventDate.Date;
                            }
                            
                            if (eventDate.Year == targetDate.Year && 
                                eventDate.Month == targetDate.Month && 
                                eventDate.Day == targetDate.Day)
                            {
                                string eventType = row["EventType"]?.ToString() ?? "personal";
                                string title = Connect.FixEncoding(row["Title"]?.ToString() ?? row["title"]?.ToString() ?? "");
                                string userName = Connect.FixEncoding(row["UserName"]?.ToString() ?? "");
                                string eventId = row["Id"]?.ToString() ?? "";
                                string eventTime = Connect.FixEncoding(row["EventTime"]?.ToString() ?? row["time"]?.ToString() ?? "");
                                
                                if (string.IsNullOrEmpty(title))
                                    continue;
                                
                                string displayText = title;
                                if (!string.IsNullOrEmpty(userName) && userName.Trim() != "" && userName != "ללא שם")
                                    displayText = $"{title} - {userName}";
                                
                                if (displayText.Length > 18)
                                    displayText = displayText.Substring(0, 18) + "...";
                                
                                HyperLink eventLink = new HyperLink();
                                eventLink.CssClass = $"event-badge {eventType}";
                                eventLink.Text = displayText;
                                eventLink.NavigateUrl = $"editEvent.aspx?id={eventId}";
                                eventLink.ToolTip = $"{title}\nמשתמש: {userName}\n{eventTime}";
                                
                                eventsPanel.Controls.Add(eventLink);
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
            catch
            {
            }
        }
    }

    public override void VerifyRenderingInServerForm(System.Web.UI.Control control)
    {
    }
}
