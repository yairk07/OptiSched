using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using System.Collections.Generic;

public partial class tasks : System.Web.UI.Page
{
    CalendarService calnderService = new CalendarService();
    private DataSet allEvents;

    // Initializes page, handles event deletion, parses events from JSON, loads events, and displays calendar
    protected void Page_Load(object sender, EventArgs e)
    {
        // Set UTF-8 encoding for Hebrew text
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        // Handle event deletion from form post
        string deleteEventId = Request.Form["deleteEventId"];
        int eventId;
        if (!string.IsNullOrEmpty(deleteEventId) && int.TryParse(deleteEventId, out eventId))
        {
            DeleteEvent(eventId);
            Response.Redirect(Request.Url.AbsolutePath, false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        // Handle parsed events JSON from JavaScript text parser
        string parsedEventsJson = Request.Form["parsedEventsJson"];
        if (!string.IsNullOrEmpty(parsedEventsJson))
        {
            // Prevent duplicate saves on postback
            if (!IsPostBack || ViewState["EventsSaved"] == null)
            {
                SaveParsedEvents(parsedEventsJson);
                ViewState["EventsSaved"] = true;
            }
            return;
        }

        // Initial page load (not postback)
        if (!IsPostBack)
        {
            ViewState["EventsSaved"] = null;
            
            // Get user ID for filtering (non-owners only see their own events)
            int? userId = null;
            string role = Session["Role"] != null ? Session["Role"].ToString() : null;
            
            if (role != "owner" && Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            // Load all events and store in ViewState
            allEvents = calnderService.GetAllEvents(userId);
            ViewState["AllEvents"] = allEvents;

            // Initialize calendar to today's date
            if (calendar != null)
            {
                calendar.SelectedDate = DateTime.Today;
            }
            if (lblSelectedDate != null)
            {
                lblSelectedDate.Text = DateTime.Today.ToString("dd/MM/yyyy");
            }
            ShowEvents(DateTime.Today);

            // Show success message if events were saved
            string saved = Request.QueryString["saved"];
            int count;
            if (!string.IsNullOrEmpty(saved) && int.TryParse(saved, out count))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "showSaved", 
                    string.Format("alert('נשמרו {0} אירועים בהצלחה!');", count), true);
            }

            // Show error message if save failed
            string error = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(error))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "showError", 
                    string.Format("alert('שגיאה: {0}');", HttpUtility.JavaScriptStringEncode(error)), true);
            }
        }
        else
        {
            // Restore events from ViewState on postback
            allEvents = (DataSet)ViewState["AllEvents"];
        }
    }

    // Parses JSON events from text parser, saves them to database, and redirects with result
    private void SaveParsedEvents(string json)
    {
        try
        {
            // Deserialize JSON array of events
            var serializer = new JavaScriptSerializer();
            var events = serializer.Deserialize<List<Dictionary<string, object>>>(json);

            // Get user ID from session
            int? userId = null;
            if (Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            // Process each event from JSON
            int savedCount = 0;
            foreach (var eventData in events)
            {
                try
                {
                    // Extract event fields from dictionary
                    string dateStr = eventData.ContainsKey("date") ? eventData["date"].ToString() : "";
                    string title = eventData.ContainsKey("title") ? eventData["title"].ToString().Trim() : "";
                    string startTime = eventData.ContainsKey("startTime") ? eventData["startTime"].ToString().Trim() : "";
                    string endTime = eventData.ContainsKey("endTime") ? eventData["endTime"].ToString().Trim() : "";
                    string location = eventData.ContainsKey("location") ? eventData["location"].ToString().Trim() : "";
                    string description = eventData.ContainsKey("description") ? eventData["description"].ToString().Trim() : "";

                    // Use default title if empty
                    if (string.IsNullOrEmpty(title))
                    {
                        title = "אירוע";
                    }

                    // Parse date and create event if valid
                    DateTime eventDate;
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out eventDate))
                    {
                        // Combine start and end time if both exist
                        string time = "";
                        if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                        {
                            time = string.Format("{0} - {1}", startTime, endTime);
                        }
                        else if (!string.IsNullOrEmpty(startTime))
                        {
                            time = startTime;
                        }

                        // Combine location and description into notes
                        string fullDescription = "";
                        if (!string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(description))
                        {
                            fullDescription = string.Format("מיקום: {0}\n{1}", location, description);
                        }
                        else if (!string.IsNullOrEmpty(location))
                        {
                            fullDescription = string.Format("מיקום: {0}", location);
                        }
                        else if (!string.IsNullOrEmpty(description))
                        {
                            fullDescription = description;
                        }

                        // Insert event into database
                        calnderService.InsertEvent(title, eventDate, time, fullDescription, "אירוע", userId);
                        savedCount++;
                    }
                }
                catch
                {
                    // Skip invalid events and continue
                }
            }

            string role = Session["Role"] != null ? Session["Role"].ToString() : null;
            int? filterUserId = null;
            if (role != "owner" && userId.HasValue)
            {
                filterUserId = userId;
            }
            allEvents = calnderService.GetAllEvents(filterUserId);
            ViewState["AllEvents"] = allEvents;

            string redirectUrl = Request.Url.AbsolutePath + "?saved=" + savedCount;
            Response.Redirect(redirectUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            string redirectUrl = Request.Url.AbsolutePath + "?error=" + HttpUtility.UrlEncode(ex.Message);
            Response.Redirect(redirectUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }

    // Updates selected date label and displays events when calendar date is selected
    protected void calendar_SelectionChanged(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate;
        lblSelectedDate.Text = selectedDate.ToString("dd/MM/yyyy");
        ShowEvents(selectedDate);
    }

    // Deletes an event from database and refreshes the event list
    protected void DeleteEvent(int eventId)
    {
        // Get user ID for permission check (non-owners can only delete their own events)
        int? userId = null;
        string role = Session["Role"] != null ? Session["Role"].ToString() : null;
        
        if (role != "owner" && Session["userId"] != null)
        {
            userId = Convert.ToInt32(Session["userId"]);
        }

        // Delete event (service enforces user permission)
        calnderService.DeleteEvent(eventId, userId);

        // Reload events and update ViewState
        int? filterUserId = null;
        if (role != "owner" && userId.HasValue)
        {
            filterUserId = userId;
        }
        allEvents = calnderService.GetAllEvents(filterUserId);
        ViewState["AllEvents"] = allEvents;

        // Refresh event display for selected date
        ShowEvents(calendar.SelectedDate);
    }

    // Creates a new event from form inputs, handles file/image uploads, and refreshes the display
    protected void AddEvent(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate.Date;
        string title = txtTitle.Text.Trim();
        string time = txtTime.Text.Trim();
        string note = txtNote.Text.Trim();
        string category = ddlCategory.SelectedValue;

        if (!string.IsNullOrEmpty(title))
        {
            int? userId = null;
            if (Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            // Insert event and get the new event ID
            int eventId = calnderService.InsertEvent(title, selectedDate, time, note, category, userId);

            // Handle file upload if file was provided
            if (fileUpload.HasFile)
            {
                try
                {
                    FileService fileService = new FileService();
                    fileService.SaveFile(fileUpload.PostedFile, eventId, userId.Value);
                }
                catch (Exception ex)
                {
                    // Log file upload errors but don't fail the event creation
                    LoggingService.Log("tasks", "Error uploading file", ex);
                }
            }

            // Handle image upload if image was provided
            if (imageUpload.HasFile)
            {
                try
                {
                    ImageService imageService = new ImageService();
                    imageService.SaveImage(imageUpload.PostedFile, eventId, userId.Value);
                }
                catch (Exception ex)
                {
                    LoggingService.Log("tasks", "Error uploading image", ex);
                }
            }

            txtTitle.Text = "";
            txtTime.Text = "";
            txtNote.Text = "";
            ddlCategory.SelectedIndex = 0;

            string role = Session["Role"] != null ? Session["Role"].ToString() : null;
            int? filterUserId = null;
            if (role != "owner" && userId.HasValue)
            {
                filterUserId = userId;
            }
            allEvents = calnderService.GetAllEvents(filterUserId);
            ViewState["AllEvents"] = allEvents;

            ShowEvents(selectedDate);
        }
    }

    // Displays events for a specific date in an HTML table, including edit/delete links
    private void ShowEvents(DateTime date)
    {
        var builder = new StringBuilder();
        int count = 0;

        // Build HTML table structure
        builder.Append("<div class='events-table-container'>");
        builder.Append("<table class='events-table'>");
        builder.Append("<thead>");
        builder.Append("<tr>");
        builder.Append("<th>כותרת</th>");
        builder.Append("<th>קטגוריה</th>");
        builder.Append("<th>שעה</th>");
        builder.Append("<th>הערות</th>");
        builder.Append("<th>סוג</th>");
        builder.Append("<th>פעולות</th>");
        builder.Append("</tr>");
        builder.Append("</thead>");
        builder.Append("<tbody>");

        foreach (DataTable table in allEvents.Tables)
        {
            foreach (DataRow row in table.Rows)
            {
                string dateColumn = row.Table.Columns.Contains("EventDate") ? "EventDate" : (row.Table.Columns.Contains("date") ? "date" : "EventDate");
                
                if (row.IsNull(dateColumn))
                    continue;

                DateTime eventDate;
                if (!row.Table.Columns.Contains(dateColumn) || row[dateColumn] == DBNull.Value || row[dateColumn] == null)
                    continue;
                    
                if (!DateTime.TryParse(row[dateColumn].ToString(), out eventDate))
                    continue;
                if (eventDate.Date == date.Date)
                {
                    string titleColumn = row.Table.Columns.Contains("Title") ? "Title" : (row.Table.Columns.Contains("title") ? "title" : "Title");
                    string timeColumn = row.Table.Columns.Contains("EventTime") ? "EventTime" : (row.Table.Columns.Contains("time") ? "time" : "EventTime");
                    string notesColumn = row.Table.Columns.Contains("Notes") ? "Notes" : (row.Table.Columns.Contains("notes") ? "notes" : "Notes");
                    string categoryColumn = row.Table.Columns.Contains("Category") ? "Category" : (row.Table.Columns.Contains("category") ? "category" : "Category");
                    
                    string title = row.Table.Columns.Contains(titleColumn) && row[titleColumn] != DBNull.Value && row[titleColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[titleColumn].ToString())) : "";
                    string time = row.Table.Columns.Contains(timeColumn) && row[timeColumn] != DBNull.Value && row[timeColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[timeColumn].ToString())) : "";
                    string note = row.Table.Columns.Contains(notesColumn) && row[notesColumn] != DBNull.Value && row[notesColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[notesColumn].ToString())) : "";
                    string category = row.Table.Columns.Contains(categoryColumn) && row[categoryColumn] != DBNull.Value && row[categoryColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[categoryColumn].ToString())) : "אחר";
                    string eventType = table.TableName == "SharedEvents" ? "טבלה משותפת" : "אישי";

                    string eventId = row["Id"] != null && row["Id"] != DBNull.Value ? row["Id"].ToString() : "";
                    bool canDelete = false;

                    if (table.TableName == "PersonalEvents" && !string.IsNullOrEmpty(eventId))
                    {
                        string currentRole = Session["Role"] != null ? Session["Role"].ToString() : null;
                        int? currentUserId = null;
                        if (Session["userId"] != null)
                        {
                            currentUserId = Convert.ToInt32(Session["userId"]);
                        }

                        if (currentRole == "owner")
                        {
                            canDelete = true;
                        }
                        else if (currentUserId.HasValue)
                        {
                            string userIdColumn = row.Table.Columns.Contains("UserId") ? "UserId" : (row.Table.Columns.Contains("Userid") ? "Userid" : "UserId");
                            if (row.Table.Columns.Contains(userIdColumn) && !row.IsNull(userIdColumn) && row[userIdColumn] != DBNull.Value && row[userIdColumn] != null)
                            {
                                int rowUserId = Convert.ToInt32(row[userIdColumn]);
                                canDelete = (rowUserId == currentUserId.Value);
                            }
                        }
                    }

                    builder.Append("<tr>");
                    builder.Append("<td>").Append(title).Append("</td>");
                    builder.Append("<td>").Append(category).Append("</td>");
                    builder.Append("<td>").Append(string.IsNullOrEmpty(time) ? "—" : time).Append("</td>");
                    builder.Append("<td>").Append(string.IsNullOrEmpty(note) ? "—" : note).Append("</td>");
                    builder.Append("<td>").Append(eventType).Append("</td>");
                    builder.Append("<td>");
                    
                    if (canDelete && !string.IsNullOrEmpty(eventId))
                    {
                        builder.Append("<form method='post' style='display: inline;'>");
                        builder.Append("<input type='hidden' name='deleteEventId' value='").Append(eventId).Append("' />");
                        builder.Append("<button type='submit' onclick=\"return confirm('האם אתה בטוח שברצונך למחוק את האירוע הזה?');\" class='delete-link'>מחק</button>");
                        builder.Append("</form>");
                    }
                    else
                    {
                        builder.Append("—");
                    }
                    
                    builder.Append("</td>");
                    builder.Append("</tr>");
                    count++;
                }
            }
        }

        builder.Append("</tbody>");
        builder.Append("</table>");
        builder.Append("</div>");

        if (count == 0)
        {
            builder.Clear();
            builder.Append("<div class='events-table-empty'>אין אירועים לתאריך הזה.</div>");
        }

        lblEvents.Text = builder.ToString();
    }

    // Renders calendar days with event indicators and styling
    protected void calendar_DayRender(object sender, DayRenderEventArgs e)
    {
        DateTime currentDay = e.Day.Date;
        int dayCount = 0;

        foreach (DataTable table in allEvents.Tables)
        {
            foreach (DataRow row in table.Rows)
            {
                string dateColumn = row.Table.Columns.Contains("EventDate") ? "EventDate" : (row.Table.Columns.Contains("date") ? "date" : "EventDate");
                
                if (!row.Table.Columns.Contains(dateColumn) || row.IsNull(dateColumn))
                    continue;

                DateTime eventDate;
                if (row[dateColumn] == DBNull.Value || row[dateColumn] == null)
                    continue;
                    
                if (!DateTime.TryParse(row[dateColumn].ToString(), out eventDate))
                    continue;
                if (eventDate.Date == currentDay.Date)
                {
                    dayCount++;
                }
            }
        }

        if (dayCount > 0)
        {
            e.Cell.Controls.Add(new Literal
            {
                Text = string.Format("<span class='task-day-count'>{0} אירועים</span>", dayCount)
            });
        }
    }
}
