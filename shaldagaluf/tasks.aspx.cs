using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using System.Collections.Generic;

public partial class tasks : System.Web.UI.Page
{
    calnderservice calnderService = new calnderservice();
    private DataSet allEvents;

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        string deleteEventId = Request.Form["deleteEventId"];
        if (!string.IsNullOrEmpty(deleteEventId) && int.TryParse(deleteEventId, out int eventId))
        {
            DeleteEvent(eventId);
            Response.Redirect(Request.Url.AbsolutePath, false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        string parsedEventsJson = Request.Form["parsedEventsJson"];
        if (!string.IsNullOrEmpty(parsedEventsJson))
        {
            if (!IsPostBack || ViewState["EventsSaved"] == null)
            {
                SaveParsedEvents(parsedEventsJson);
                ViewState["EventsSaved"] = true;
            }
            return;
        }

        if (!IsPostBack)
        {
            ViewState["EventsSaved"] = null;
            
            int? userId = null;
            string role = Session["Role"]?.ToString();
            
            if (role != "owner" && Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            allEvents = calnderService.GetAllEvents(userId);
            ViewState["AllEvents"] = allEvents;

            if (calendar != null)
            {
                calendar.SelectedDate = DateTime.Today;
            }
            if (lblSelectedDate != null)
            {
                lblSelectedDate.Text = DateTime.Today.ToString("dd/MM/yyyy");
            }
            ShowEvents(DateTime.Today);

            string saved = Request.QueryString["saved"];
            if (!string.IsNullOrEmpty(saved) && int.TryParse(saved, out int count))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "showSaved", 
                    $"alert('נשמרו {count} אירועים בהצלחה!');", true);
            }

            string error = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(error))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "showError", 
                    $"alert('שגיאה: {HttpUtility.JavaScriptStringEncode(error)}');", true);
            }
        }
        else
        {
            allEvents = (DataSet)ViewState["AllEvents"];
        }
    }

    private void SaveParsedEvents(string json)
    {
        try
        {
            var serializer = new JavaScriptSerializer();
            var events = serializer.Deserialize<List<Dictionary<string, object>>>(json);

            int? userId = null;
            if (Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            int savedCount = 0;
            foreach (var eventData in events)
            {
                try
                {
                    string dateStr = eventData.ContainsKey("date") ? eventData["date"].ToString() : "";
                    string title = eventData.ContainsKey("title") ? eventData["title"].ToString().Trim() : "";
                    string startTime = eventData.ContainsKey("startTime") ? eventData["startTime"].ToString().Trim() : "";
                    string endTime = eventData.ContainsKey("endTime") ? eventData["endTime"].ToString().Trim() : "";
                    string location = eventData.ContainsKey("location") ? eventData["location"].ToString().Trim() : "";
                    string description = eventData.ContainsKey("description") ? eventData["description"].ToString().Trim() : "";

                    if (string.IsNullOrEmpty(title))
                    {
                        title = "אירוע";
                    }

                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime eventDate))
                    {
                        string time = "";
                        if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                        {
                            time = $"{startTime} - {endTime}";
                        }
                        else if (!string.IsNullOrEmpty(startTime))
                        {
                            time = startTime;
                        }

                        string fullDescription = "";
                        if (!string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(description))
                        {
                            fullDescription = $"מיקום: {location}\n{description}";
                        }
                        else if (!string.IsNullOrEmpty(location))
                        {
                            fullDescription = $"מיקום: {location}";
                        }
                        else if (!string.IsNullOrEmpty(description))
                        {
                            fullDescription = description;
                        }

                        calnderService.InsertEvent(title, eventDate, time, fullDescription, "אירוע", userId);
                        savedCount++;
                    }
                }
                catch
                {
                }
            }

            string role = Session["Role"]?.ToString();
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

    protected void calendar_SelectionChanged(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate;
        lblSelectedDate.Text = selectedDate.ToString("dd/MM/yyyy");
        ShowEvents(selectedDate);
    }

    protected void DeleteEvent(int eventId)
    {
        int? userId = null;
        string role = Session["Role"]?.ToString();
        
        if (role != "owner" && Session["userId"] != null)
        {
            userId = Convert.ToInt32(Session["userId"]);
        }

        calnderService.DeleteEvent(eventId, userId);

        int? filterUserId = null;
        if (role != "owner" && userId.HasValue)
        {
            filterUserId = userId;
        }
        allEvents = calnderService.GetAllEvents(filterUserId);
        ViewState["AllEvents"] = allEvents;

        ShowEvents(calendar.SelectedDate);
    }

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

            calnderService.InsertEvent(title, selectedDate, time, note, category, userId);

            txtTitle.Text = "";
            txtTime.Text = "";
            txtNote.Text = "";
            ddlCategory.SelectedIndex = 0;

            string role = Session["Role"]?.ToString();
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

    private void ShowEvents(DateTime date)
    {
        var builder = new StringBuilder();
        int count = 0;

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
                if (row.IsNull("date"))
                    continue;

                DateTime eventDate;
                if (!DateTime.TryParse(row["date"].ToString(), out eventDate))
                    continue;
                if (eventDate.Date == date.Date)
                {
                    string title = HttpUtility.HtmlEncode(Connect.FixEncoding(row["title"].ToString()));
                    string time = HttpUtility.HtmlEncode(Connect.FixEncoding(row["time"]?.ToString() ?? ""));
                    string note = HttpUtility.HtmlEncode(Connect.FixEncoding(row["notes"]?.ToString() ?? ""));
                    string category = HttpUtility.HtmlEncode(Connect.FixEncoding(row["category"]?.ToString() ?? "אחר"));
                    string eventType = table.TableName == "SharedEvents" ? "טבלה משותפת" : "אישי";

                    string eventId = row["Id"]?.ToString() ?? "";
                    bool canDelete = false;

                    if (table.TableName == "PersonalEvents" && !string.IsNullOrEmpty(eventId))
                    {
                        string currentRole = Session["Role"]?.ToString();
                        int? currentUserId = null;
                        if (Session["userId"] != null)
                        {
                            currentUserId = Convert.ToInt32(Session["userId"]);
                        }

                        if (currentRole == "owner")
                        {
                            canDelete = true;
                        }
                        else if (currentUserId.HasValue && !row.IsNull("Userid"))
                        {
                            int rowUserId = Convert.ToInt32(row["Userid"]);
                            canDelete = (rowUserId == currentUserId.Value);
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

    protected void calendar_DayRender(object sender, DayRenderEventArgs e)
    {
        DateTime currentDay = e.Day.Date;
        int dayCount = 0;

        foreach (DataTable table in allEvents.Tables)
        {
            foreach (DataRow row in table.Rows)
            {
                if (row.IsNull("date"))
                    continue;

                DateTime eventDate;
                if (!DateTime.TryParse(row["date"].ToString(), out eventDate))
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
                Text = $"<span class='task-day-count'>{dayCount} אירועים</span>"
            });
        }
    }
}
