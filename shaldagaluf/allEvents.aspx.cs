using System;
using System.Data;
using System.Linq;
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
        
        if (Session["userId"] != null)
            userId = Convert.ToInt32(Session["userId"]);
        
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.LoadEventsData:BEFORE_QUERY\",\"message\":\"Before calling GetAllEvents\",\"data\":{\"userId\":\"" + (userId?.ToString() ?? "null") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        DataTable dt = es.GetAllEvents(userId);
        
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.LoadEventsData:AFTER_QUERY\",\"message\":\"After calling GetAllEvents\",\"data\":{\"rowCount\":\"" + (dt?.Rows.Count ?? 0) + "\",\"columnCount\":\"" + (dt?.Columns.Count ?? 0) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        if (dt == null)
            dt = new DataTable();
        
        if (dt.Rows.Count > 0)
        {
            if (!dt.Columns.Contains("EventDate") && dt.Columns.Contains("date"))
            {
                dt.Columns.Add("EventDate", typeof(DateTime));
                foreach (DataRow row in dt.Rows)
                {
                    if (row.Table.Columns.Contains("date") && row["date"] != DBNull.Value && row["date"] != null)
                    {
                        row["EventDate"] = row["date"];
                    }
                }
            }
            if (!dt.Columns.Contains("Title") && dt.Columns.Contains("title"))
            {
                dt.Columns.Add("Title", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    if (row.Table.Columns.Contains("title") && row["title"] != DBNull.Value && row["title"] != null)
                    {
                        row["Title"] = row["title"];
                    }
                }
            }
            if (!dt.Columns.Contains("EventTime") && dt.Columns.Contains("time"))
            {
                dt.Columns.Add("EventTime", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    if (row.Table.Columns.Contains("time") && row["time"] != DBNull.Value && row["time"] != null)
                    {
                        row["EventTime"] = row["time"];
                    }
                }
            }
            
            if (!dt.Columns.Contains("Category"))
                dt.Columns.Add("Category", typeof(string));
            if (!dt.Columns.Contains("UserName"))
                dt.Columns.Add("UserName", typeof(string));
            if (!dt.Columns.Contains("UserId"))
                dt.Columns.Add("UserId", typeof(int));
            if (!dt.Columns.Contains("Notes"))
                dt.Columns.Add("Notes", typeof(string));
            if (!dt.Columns.Contains("Id"))
                dt.Columns.Add("Id", typeof(int));
            
            EnsureRequiredColumns(dt);
            
            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("Category") && (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString())))
                    row["Category"] = "אחר";
            }
            
            Session[cacheKey] = dt;
        }
        
        return dt;
    }
    
    private void EnsureRequiredColumns(DataTable dt)
    {
        if (dt == null) return;

        string[] requiredColumns = { "Id", "Title", "UserName", "UserId", "EventDate", "EventTime", "Category", "Notes", "EventType" };
        
        foreach (string colName in requiredColumns)
        {
            if (!dt.Columns.Contains(colName))
            {
                Type colType = typeof(string);
                if (colName == "Id" || colName == "UserId")
                    colType = typeof(int);
                else if (colName == "EventDate")
                    colType = typeof(DateTime);
                
                dt.Columns.Add(colName, colType);
                
                foreach (DataRow row in dt.Rows)
                {
                    if (colName == "Id" || colName == "UserId")
                        row[colName] = 0;
                    else if (colName == "EventDate")
                        row[colName] = DateTime.MinValue;
                    else if (colName == "Category")
                        row[colName] = "אחר";
                    else
                        row[colName] = "";
                }
            }
        }
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
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
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
        
        if (dt == null || dt.Rows.Count == 0)
        {
            dlEvents.DataSource = null;
            dlEvents.DataBind();
            return;
        }
        
        EnsureRequiredColumns(dt);
        
        DataTable filteredDt = new DataTable();
        foreach (DataColumn col in dt.Columns)
        {
            filteredDt.Columns.Add(col.ColumnName, col.DataType);
        }
        EnsureRequiredColumns(filteredDt);

        foreach (DataRow row in dt.Rows)
        {
            try
            {
                bool matchesFilter = true;
                bool matchesCategory = true;

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    string title = row.Table.Columns.Contains("Title") ? (row["Title"]?.ToString() ?? "") : "";
                    string userName = row.Table.Columns.Contains("UserName") ? (row["UserName"]?.ToString() ?? "") : "";
                    string notes = row.Table.Columns.Contains("Notes") ? (row["Notes"]?.ToString() ?? "") : "";
                    
                    string searchLower = filter.ToLower();
                    matchesFilter = title.ToLower().Contains(searchLower) ||
                                   userName.ToLower().Contains(searchLower) ||
                                   notes.ToLower().Contains(searchLower);
                }

                if (!string.IsNullOrWhiteSpace(categoryFilter))
                {
                    string category = row.Table.Columns.Contains("Category") ? (row["Category"]?.ToString() ?? "אחר") : "אחר";
                    matchesCategory = category == categoryFilter;
                }

                if (matchesFilter && matchesCategory)
                {
                    DataRow newRow = filteredDt.NewRow();
                    foreach (DataColumn col in filteredDt.Columns)
                    {
                        if (row.Table.Columns.Contains(col.ColumnName))
                        {
                            if (row[col.ColumnName] != DBNull.Value && row[col.ColumnName] != null)
                            {
                                newRow[col.ColumnName] = row[col.ColumnName];
                            }
                            else
                            {
                                if (col.DataType == typeof(int))
                                    newRow[col.ColumnName] = 0;
                                else if (col.DataType == typeof(DateTime))
                                    newRow[col.ColumnName] = DateTime.MinValue;
                                else
                                    newRow[col.ColumnName] = "";
                            }
                        }
                        else
                        {
                            if (col.DataType == typeof(int))
                                newRow[col.ColumnName] = 0;
                            else if (col.DataType == typeof(DateTime))
                                newRow[col.ColumnName] = DateTime.MinValue;
                            else if (col.ColumnName == "Category")
                                newRow[col.ColumnName] = "אחר";
                            else
                                newRow[col.ColumnName] = "";
                        }
                    }
                    filteredDt.Rows.Add(newRow);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"allEvents.BindData:IMPORT_ERROR\",\"message\":\"Error importing row\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }
                continue;
            }
        }

        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.BindData:BEFORE_BIND\",\"message\":\"Before DataBind\",\"data\":{\"rowCount\":\"" + filteredDt.Rows.Count + "\",\"columnCount\":\"" + filteredDt.Columns.Count + "\",\"columns\":\"" + string.Join(",", filteredDt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }

        EnsureRequiredColumns(filteredDt);

        try
        {
            if (filteredDt.Rows.Count > 0)
            {
                try
                {
                    DataRow sampleRow = filteredDt.Rows[0];
                    string sampleData = "";
                    foreach (DataColumn col in filteredDt.Columns)
                    {
                        if (sampleRow[col.ColumnName] != DBNull.Value && sampleRow[col.ColumnName] != null)
                            sampleData += col.ColumnName + "=" + sampleRow[col.ColumnName].ToString().Substring(0, Math.Min(50, sampleRow[col.ColumnName].ToString().Length)) + ";";
                    }
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"allEvents.BindData:SAMPLE_DATA\",\"message\":\"Sample row data\",\"data\":{\"sample\":\"" + sampleData.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }
            }
            
            dlEvents.DataSource = filteredDt;
            dlEvents.DataBind();
            
            try
            {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"allEvents.BindData:AFTER_BIND\",\"message\":\"After DataBind\",\"data\":{\"itemsCount\":\"" + dlEvents.Items.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }
        catch (Exception bindEx)
        {
            try
            {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"allEvents.BindData:BIND_ERROR\",\"message\":\"DataBind error\",\"data\":{\"error\":\"" + bindEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + bindEx.GetType().Name + "\",\"stackTrace\":\"" + bindEx.StackTrace?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

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
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.BindCalendar:ENTRY\",\"message\":\"BindCalendar called\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        DateTime currentDate = calEvents.VisibleDate;
        
        if (currentDate == DateTime.MinValue || currentDate == DateTime.MaxValue || currentDate.Year < 1 || currentDate.Year > 9999)
        {
            currentDate = DateTime.Now;
            calEvents.VisibleDate = currentDate;
        }
        
        lblCurrentMonth.Text = currentDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("he-IL"));
        
        DataTable eventsData = LoadEventsData();
        
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.BindCalendar:LOADED_DATA\",\"message\":\"Events data loaded\",\"data\":{\"rowCount\":\"" + (eventsData?.Rows.Count ?? 0) + "\",\"columnCount\":\"" + (eventsData?.Columns.Count ?? 0) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        if (eventsData == null || eventsData.Rows.Count == 0)
        {
            lblCalendarMessage.Text = "אין אירועים להצגה";
            lblCalendarMessage.ForeColor = System.Drawing.Color.Gray;
        }
        else
        {
            EnsureRequiredColumns(eventsData);
            
            int eventsInMonth = 0;
            string dateColumn = eventsData.Columns.Contains("EventDate") ? "EventDate" : "date";
            
            foreach (DataRow row in eventsData.Rows)
            {
                try
                {
                    if (!row.Table.Columns.Contains(dateColumn))
                        continue;
                        
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
                        catch (Exception dateEx)
                        {
                            try
                            {
                                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                    "{\"location\":\"allEvents.BindCalendar:DATE_ERROR\",\"message\":\"Error parsing date\",\"data\":{\"error\":\"" + dateEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception rowEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.BindCalendar:ROW_ERROR\",\"message\":\"Error processing row\",\"data\":{\"error\":\"" + rowEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + rowEx.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    continue;
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
        try
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
                    
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.calEvents_DayRender:LOADED_DATA\",\"message\":\"Events data loaded\",\"data\":{\"rowCount\":\"" + (eventsData?.Rows.Count ?? 0) + "\",\"columnCount\":\"" + (eventsData?.Columns.Count ?? 0) + "\",\"day\":\"" + e.Day.Date.ToString("yyyy-MM-dd") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                
                if (eventsData != null && eventsData.Rows.Count > 0)
                {
                    EnsureRequiredColumns(eventsData);
                    
                    DateTime targetDate = e.Day.Date.Date;
                    Panel eventsPanel = new Panel();
                    eventsPanel.CssClass = "day-events";
                    
                    string dateColumn = eventsData.Columns.Contains("EventDate") ? "EventDate" : "date";
                    if (!eventsData.Columns.Contains(dateColumn))
                    {
                        try
                        {
                            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                "{\"location\":\"allEvents.calEvents_DayRender:NO_DATE_COLUMN\",\"message\":\"Date column not found\",\"data\":{\"dateColumn\":\"" + dateColumn + "\",\"availableColumns\":\"" + string.Join(",", eventsData.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                        }
                        catch { }
                        return;
                    }
                    
                    foreach (DataRow row in eventsData.Rows)
                    {
                        try
                        {
                            if (!row.Table.Columns.Contains(dateColumn))
                                continue;
                                
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
                                string eventType = "personal";
                                if (eventsData.Columns.Contains("EventType") && row.Table.Columns.Contains("EventType") && row["EventType"] != DBNull.Value && row["EventType"] != null)
                                    eventType = row["EventType"].ToString();
                                
                                string title = "";
                                if (eventsData.Columns.Contains("Title") && row.Table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
                                    title = Connect.FixEncoding(row["Title"].ToString());
                                else if (eventsData.Columns.Contains("title") && row.Table.Columns.Contains("title") && row["title"] != DBNull.Value && row["title"] != null)
                                    title = Connect.FixEncoding(row["title"].ToString());
                                
                                string userName = "";
                                if (eventsData.Columns.Contains("UserName") && row.Table.Columns.Contains("UserName") && row["UserName"] != DBNull.Value && row["UserName"] != null)
                                    userName = Connect.FixEncoding(row["UserName"].ToString());
                                
                                string eventId = "";
                                if (eventsData.Columns.Contains("Id") && row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                                    eventId = row["Id"].ToString();
                                
                                string eventTime = "";
                                if (eventsData.Columns.Contains("EventTime") && row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                                    eventTime = Connect.FixEncoding(row["EventTime"].ToString());
                                else if (eventsData.Columns.Contains("time") && row.Table.Columns.Contains("time") && row["time"] != DBNull.Value && row["time"] != null)
                                    eventTime = Connect.FixEncoding(row["time"].ToString());
                                
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
                        catch (Exception rowEx)
                        {
                            try
                            {
                                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                    "{\"location\":\"allEvents.calEvents_DayRender:ROW_ERROR\",\"message\":\"Error processing row\",\"data\":{\"error\":\"" + rowEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + rowEx.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                            }
                            catch { }
                            continue;
                        }
                    }

                    if (eventsPanel.Controls.Count > 0)
                        e.Cell.Controls.Add(eventsPanel);
                }
            }
                catch (Exception dayEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.calEvents_DayRender:ERROR\",\"message\":\"Error in DayRender\",\"data\":{\"error\":\"" + dayEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + dayEx.GetType().Name + "\",\"stackTrace\":\"" + dayEx.StackTrace?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                }
            }
        }
        catch (Exception outerEx)
        {
            try
            {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"allEvents.calEvents_DayRender:OUTER_ERROR\",\"message\":\"Outer error in DayRender\",\"data\":{\"error\":\"" + outerEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + outerEx.GetType().Name + "\",\"stackTrace\":\"" + outerEx.StackTrace?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }
    }

    protected void dlEvents_ItemDataBound(object sender, DataListItemEventArgs e)
    {
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"allEvents.dlEvents_ItemDataBound:ENTRY\",\"message\":\"ItemDataBound called\",\"data\":{\"itemType\":\"" + e.Item.ItemType.ToString() + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
        {
            try
            {
                DataRowView rowView = e.Item.DataItem as DataRowView;
                if (rowView == null)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:NULL_ROWVIEW\",\"message\":\"DataItem is not DataRowView\",\"data\":{\"dataItemType\":\"" + (e.Item.DataItem?.GetType().Name ?? "null") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    return;
                }
                
                if (rowView.Row == null)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:NULL_ROW\",\"message\":\"rowView.Row is null\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    return;
                }
                
                DataRow row = rowView.Row;
                if (row.Table == null)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:NULL_TABLE\",\"message\":\"row.Table is null\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    return;
                }
                
                DataTable table = row.Table;
                    
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:PROCESSING\",\"message\":\"Processing row\",\"data\":{\"rowColumns\":\"" + string.Join(",", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    
                    Literal litTitle = (Literal)e.Item.FindControl("litTitle");
                    Literal litUserName = (Literal)e.Item.FindControl("litUserName");
                    Literal litEventDate = (Literal)e.Item.FindControl("litEventDate");
                    Literal litEventTime = (Literal)e.Item.FindControl("litEventTime");
                    Literal litCategory = (Literal)e.Item.FindControl("litCategory");
                    Literal litNotes = (Literal)e.Item.FindControl("litNotes");
                    HyperLink lnkEdit = (HyperLink)e.Item.FindControl("lnkEdit");
                    
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:FIND_CONTROLS\",\"message\":\"Controls found\",\"data\":{\"litTitle\":\"" + (litTitle != null ? "found" : "null") + "\",\"litUserName\":\"" + (litUserName != null ? "found" : "null") + "\",\"litEventDate\":\"" + (litEventDate != null ? "found" : "null") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    
                    if (litTitle != null)
                    {
                        if (table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
                            litTitle.Text = row["Title"].ToString();
                        else if (table.Columns.Contains("title") && row["title"] != DBNull.Value && row["title"] != null)
                            litTitle.Text = row["title"].ToString();
                        else
                            litTitle.Text = "";
                    }
                    
                    if (litUserName != null)
                    {
                        string userName = "";
                        string userId = "";
                        if (table.Columns.Contains("UserName") && row["UserName"] != DBNull.Value && row["UserName"] != null)
                            userName = row["UserName"].ToString();
                        if (table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value && row["UserId"] != null)
                            userId = row["UserId"].ToString();
                        litUserName.Text = userName + (string.IsNullOrEmpty(userId) ? "" : " (#" + userId + ")");
                    }
                    
                    if (litEventDate != null)
                    {
                        if (table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
                        {
                            DateTime eventDate;
                            if (row["EventDate"] is DateTime)
                                eventDate = (DateTime)row["EventDate"];
                            else if (DateTime.TryParse(row["EventDate"].ToString(), out eventDate))
                                litEventDate.Text = eventDate.ToString("dd/MM/yyyy");
                            else
                                litEventDate.Text = "";
                        }
                        else if (table.Columns.Contains("date") && row["date"] != DBNull.Value && row["date"] != null)
                        {
                            DateTime eventDate;
                            if (row["date"] is DateTime)
                                eventDate = (DateTime)row["date"];
                            else if (DateTime.TryParse(row["date"].ToString(), out eventDate))
                                litEventDate.Text = eventDate.ToString("dd/MM/yyyy");
                            else
                                litEventDate.Text = "";
                        }
                        else
                            litEventDate.Text = "";
                    }
                    
                    if (litEventTime != null)
                    {
                        if (table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                            litEventTime.Text = row["EventTime"].ToString();
                        else if (table.Columns.Contains("time") && row["time"] != DBNull.Value && row["time"] != null)
                            litEventTime.Text = row["time"].ToString();
                        else
                            litEventTime.Text = "";
                    }
                    
                    if (litCategory != null)
                    {
                        if (table.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null && !string.IsNullOrWhiteSpace(row["Category"].ToString()))
                            litCategory.Text = row["Category"].ToString();
                        else
                            litCategory.Text = "אחר";
                    }
                    
                    if (litNotes != null)
                    {
                        if (table.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
                            litNotes.Text = row["Notes"].ToString();
                        else
                            litNotes.Text = "";
                    }
                    
                    if (lnkEdit != null)
                    {
                        if (table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                            lnkEdit.NavigateUrl = "editEvent.aspx?id=" + row["Id"].ToString();
                        else
                            lnkEdit.Visible = false;
                    }
                    
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"allEvents.dlEvents_ItemDataBound:SUCCESS\",\"message\":\"Row processed successfully\",\"data\":{\"title\":\"" + (litTitle?.Text ?? "null") + "\",\"userName\":\"" + (litUserName?.Text ?? "null") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"allEvents.dlEvents_ItemDataBound:ERROR\",\"message\":\"Error in ItemDataBound\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\",\"stackTrace\":\"" + ex.StackTrace?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }
            }
        }
    }

    public override void VerifyRenderingInServerForm(System.Web.UI.Control control)
    {
    }
}
