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
    private Dictionary<int, string> memberColors = new Dictionary<int, string>();

    public bool IsAdmin { get { return isAdmin; } }

    // טעינת הדף: בדיקת התחברות, מחיקת אירוע אם נדרש, וטעינת הטבלה המשותפת
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

    // תיקון קידוד עברית (UTF-8 / Windows-1255)
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

    // טעינת פרטי הטבלה, הרשאות המשתמש, והצגת אירועי היום הנבחר
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
            calendarTitle.Text = Utilities.FixEncoding(calendar["CalendarName"].ToString());
        else
            calendarTitle.Text = "";
            
        if (calendar.Table.Columns.Contains("Description") && calendar["Description"] != DBNull.Value && calendar["Description"] != null)
            calendarDescription.Text = Utilities.FixEncoding(calendar["Description"].ToString());
        else
            calendarDescription.Text = "";

        pnlInvite.Visible = isAdmin;
        lnkManage.Visible = isAdmin;
        lnkManage.NavigateUrl = "sharedCalendarManage.aspx?id=" + calendarId;
        pnlEmailSchedule.Visible = isMember || isAdmin;

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

            ApplyVisibleFields();
            BuildCustomFields();
            BindAssigneeList();
            BindCalendar();
            CacheMemberColors();

            if (!IsPostBack)
            {
                calEvents.SelectedDate = DateTime.Today;
                txtEventDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(txtEmailFrom.Text))
                    txtEmailFrom.Text = DateTime.Today.ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(txtEmailToDate.Text))
                    txtEmailToDate.Text = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd");

                int editEventId;
                if (int.TryParse(Request.QueryString["editEvent"], out editEventId) && editEventId > 0)
                    LoadEventIntoForm(editEventId);
                else
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

    private void ApplyVisibleFields()
    {
        HashSet<string> fields = service.GetVisibleFieldSet(calendarId);
        HashSet<string> required = service.GetRequiredFieldSet(calendarId);
        // כותרת ותאריך תמיד נשארים — חובה לשמירה
        pnlFieldTitle.Visible = true;
        pnlFieldDate.Visible = true;
        pnlFieldTime.Visible = fields.Contains("EventTime");
        pnlFieldCategory.Visible = fields.Contains("Category");
        pnlFieldNotes.Visible = fields.Contains("Notes");
        pnlFieldReminder.Visible = fields.Contains("Reminder");
        pnlFieldAssignee.Visible = fields.Contains("Assignee");
        pnlFieldStatus.Visible = fields.Contains("Status");
        ViewState["VisibleFields"] = string.Join(",", new List<string>(fields).ToArray());
        ViewState["RequiredFields"] = string.Join(",", new List<string>(required).ToArray());
        ApplyRequiredFieldMarkers(required);
    }

    private HashSet<string> GetCachedRequiredFields()
    {
        string raw = ViewState["RequiredFields"] as string;
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (string part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string key = part.Trim();
                if (!string.IsNullOrEmpty(key)) set.Add(key);
            }
        }
        foreach (string key in SharedCalendarService.AlwaysRequiredBuiltinFields)
            set.Add(key);
        set.Remove("Creator");
        return set;
    }

    private void ApplyRequiredFieldMarkers(HashSet<string> required)
    {
        if (required == null) required = GetCachedRequiredFields();
        SetFieldLabelRequired(pnlFieldTitle, "\u05DB\u05D5\u05EA\u05E8\u05EA", true);
        SetFieldLabelRequired(pnlFieldDate, "\u05EA\u05D0\u05E8\u05D9\u05DA", true);
        SetFieldLabelRequired(pnlFieldTime, "\u05E9\u05E2\u05D4", required.Contains("EventTime"));
        SetFieldLabelRequired(pnlFieldCategory, "\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4", required.Contains("Category"));
        SetFieldLabelRequired(pnlFieldNotes, "\u05D4\u05E2\u05E8\u05D5\u05EA", required.Contains("Notes"));
        SetFieldLabelRequired(pnlFieldReminder, "\u05EA\u05D6\u05DB\u05D5\u05E8\u05EA", required.Contains("Reminder"));
        SetFieldLabelRequired(pnlFieldAssignee, "\u05D0\u05D7\u05E8\u05D0\u05D9", required.Contains("Assignee"));
        SetFieldLabelRequired(pnlFieldStatus, "\u05E1\u05D8\u05D8\u05D5\u05E1", required.Contains("Status"));
    }

    private static void SetFieldLabelRequired(Panel panel, string labelText, bool isRequired)
    {
        if (panel == null) return;
        foreach (Control child in panel.Controls)
        {
            System.Web.UI.HtmlControls.HtmlGenericControl span = child as System.Web.UI.HtmlControls.HtmlGenericControl;
            if (span == null || span.Attributes["class"] == null) continue;
            if (span.Attributes["class"].IndexOf("meta-label", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            span.InnerHtml = isRequired
                ? labelText + " <span class=\"required\">*</span>"
                : labelText;
            return;
        }
    }

    private DataTable GetCachedCustomFields()
    {
        return service.GetCustomFields(calendarId, true);
    }

    private void BuildCustomFields()
    {
        phCustomFields.Controls.Clear();
        DataTable fields = service.GetCustomFields(calendarId, true);
        if (fields == null || fields.Rows.Count == 0) return;

        // On postback do not preload DB values — posted form values must win until save/clear.
        Dictionary<string, string> existingValues = null;
        if (!IsPostBack)
        {
            int? editingId = ViewState["EditingEventId"] as int?;
            if (editingId.HasValue && editingId.Value > 0)
                existingValues = service.GetEventFieldValues(editingId.Value);
        }

        foreach (DataRow row in fields.Rows)
        {
            string fieldKey = Convert.ToString(row["FieldKey"]);
            string label = Convert.ToString(row["FieldLabel"]);
            string fieldType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
            string options = Convert.ToString(row["FieldOptions"] ?? "");
            bool isRequired = row["IsRequired"] != DBNull.Value && Convert.ToBoolean(row["IsRequired"]);
            string currentVal = "";
            if (existingValues != null && existingValues.ContainsKey(fieldKey))
                currentVal = existingValues[fieldKey];

            Panel line = new Panel();
            line.CssClass = "calendar-meta-line custom-field-line";
            line.ID = "pnlCf_" + fieldKey;

            Literal labelLit = new Literal();
            string reqMark = isRequired && fieldType != "button"
                ? " <span class=\"required\">*</span>"
                : "";
            labelLit.Text = string.Format("<span class=\"meta-label\">{0}{1}</span>",
                System.Web.HttpUtility.HtmlEncode(label), reqMark);
            line.Controls.Add(labelLit);

            if (fieldType == "list")
            {
                DropDownList ddl = new DropDownList();
                ddl.ID = "cf_" + fieldKey;
                ddl.CssClass = "task-input";
                ddl.Items.Add(new ListItem("\u2014 \u05D1\u05D7\u05E8 \u2014", ""));
                foreach (string opt in SharedCalendarService.ParseCustomFieldOptions(options))
                    ddl.Items.Add(new ListItem(opt, opt));
                if (!string.IsNullOrEmpty(currentVal) && ddl.Items.FindByValue(currentVal) != null)
                    ddl.SelectedValue = currentVal;
                line.Controls.Add(ddl);
            }
            else if (fieldType == "button")
            {
                string url = (options ?? "").Trim();
                HyperLink link = new HyperLink();
                link.ID = "cfbtn_" + fieldKey;
                link.CssClass = "task-button custom-field-button";
                link.Text = System.Web.HttpUtility.HtmlEncode(label);
                link.Target = "_blank";
                link.NavigateUrl = string.IsNullOrEmpty(url) ? "#" : url;
                if (!string.IsNullOrEmpty(url) &&
                    !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("/", StringComparison.Ordinal))
                {
                    link.NavigateUrl = "https://" + url;
                }
                link.Attributes["rel"] = "noopener noreferrer";
                line.Controls.Add(link);
            }
            else
            {
                TextBox txt = new TextBox();
                txt.ID = "cf_" + fieldKey;
                txt.CssClass = "task-input";
                txt.Text = currentVal ?? "";
                line.Controls.Add(txt);
            }

            phCustomFields.Controls.Add(line);
        }
    }

    private Dictionary<string, string> CollectCustomFieldValues(out string validationError)
    {
        validationError = null;
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        DataTable fields = GetCachedCustomFields();
        if (fields == null) fields = service.GetCustomFields(calendarId, true);

        foreach (DataRow row in fields.Rows)
        {
            string fieldKey = Convert.ToString(row["FieldKey"]);
            string label = Convert.ToString(row["FieldLabel"]);
            string fieldType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
            bool isRequired = row["IsRequired"] != DBNull.Value && Convert.ToBoolean(row["IsRequired"]);

            if (fieldType == "button")
                continue;

            string val = "";
            Control ctrl = phCustomFields.FindControl("cf_" + fieldKey);
            if (ctrl is TextBox)
                val = ((TextBox)ctrl).Text != null ? ((TextBox)ctrl).Text.Trim() : "";
            else if (ctrl is DropDownList)
                val = ((DropDownList)ctrl).SelectedValue ?? "";

            // Dynamic controls created in Page_Load may miss ViewState; fall back to form post.
            if (string.IsNullOrEmpty(val) && ctrl != null && Request.Form[ctrl.UniqueID] != null)
                val = (Request.Form[ctrl.UniqueID] ?? "").Trim();

            val = Utilities.FixEncoding(val);

            if (isRequired && string.IsNullOrWhiteSpace(val))
            {
                validationError = string.Format(
                    "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05DE\u05DC\u05D0 \u05D0\u05EA \u05D4\u05E9\u05D3\u05D4 \"{0}\"",
                    label);
                return values;
            }

            values[fieldKey] = val;
        }

        return values;
    }

    private void ApplyCustomFieldValuesToForm(Dictionary<string, string> values)
    {
        if (values == null) return;
        DataTable fields = GetCachedCustomFields();
        if (fields == null) return;

        foreach (DataRow row in fields.Rows)
        {
            string fieldKey = Convert.ToString(row["FieldKey"]);
            string fieldType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
            if (fieldType == "button") continue;

            string val;
            if (!values.TryGetValue(fieldKey, out val)) val = "";

            Control ctrl = phCustomFields.FindControl("cf_" + fieldKey);
            if (ctrl is TextBox)
                ((TextBox)ctrl).Text = val;
            else if (ctrl is DropDownList)
            {
                DropDownList ddl = (DropDownList)ctrl;
                if (ddl.Items.FindByValue(val) != null)
                    ddl.SelectedValue = val;
                else if (string.IsNullOrEmpty(val) && ddl.Items.Count > 0)
                    ddl.SelectedIndex = 0;
            }
        }
    }

    private void ClearCustomFieldInputs()
    {
        DataTable fields = GetCachedCustomFields();
        if (fields == null) return;
        foreach (DataRow row in fields.Rows)
        {
            string fieldKey = Convert.ToString(row["FieldKey"]);
            string fieldType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
            if (fieldType == "button") continue;
            Control ctrl = phCustomFields.FindControl("cf_" + fieldKey);
            if (ctrl is TextBox)
                ((TextBox)ctrl).Text = "";
            else if (ctrl is DropDownList)
            {
                DropDownList ddl = (DropDownList)ctrl;
                if (ddl.Items.Count > 0) ddl.SelectedIndex = 0;
            }
        }
    }

    private void LoadEventIntoForm(int eventId)
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool userIsAdmin = isAdmin || isOwner;
        bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);
        if (!canEdit) return;

        DataTable dt = service.GetSharedCalendarEvents(calendarId, currentUserId);
        if (dt == null) return;

        DataRow[] rows = dt.Select(string.Format("Id = {0}", eventId));
        if (rows.Length == 0) return;

        DataRow row = rows[0];
        FillEventFormFromRow(row);
        ViewState["EditingEventId"] = eventId;
        BuildCustomFields();
        ApplyCustomFieldValuesToForm(service.GetEventFieldValues(eventId));

        if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value)
        {
            DateTime eventDate = Convert.ToDateTime(row["EventDate"]);
            calEvents.SelectedDate = eventDate;
            ShowEvents(eventDate);
        }
    }

    private void FillEventFormFromRow(DataRow row)
    {
        string title = "";
        if (row.Table.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
        {
            title = Utilities.FixEncoding(row["Title"].ToString().Trim());
            if (title == "...." || title == "..." || title == "\u05DC\u05DC\u05D0 \u05DB\u05D5\u05EA\u05E8\u05EA")
                title = "";
        }
        txtEventTitle.Text = title;

        if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value && row["EventDate"] != null)
            txtEventDate.Text = Convert.ToDateTime(row["EventDate"]).ToString("yyyy-MM-dd");

        string time = "";
        if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
        {
            time = Utilities.FixEncoding(row["EventTime"].ToString().Trim());
            if (time == "...." || time == "...")
                time = "";
        }
        txtEventTime.Text = time;

        string notes = "";
        if (row.Table.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
            notes = Utilities.FixEncoding(row["Notes"].ToString().Trim());
        txtEventNotes.Text = notes;

        if (row.Table.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
        {
            string category = Utilities.FixEncoding(row["Category"].ToString().Trim());
            if (category != "...." && category != "..." && ddlEventCategory.Items.FindByValue(category) != null)
                ddlEventCategory.SelectedValue = category;
        }

        BindAssigneeList();
        if (row.Table.Columns.Contains("ReminderMinutes") && row["ReminderMinutes"] != DBNull.Value)
        {
            string rm = Convert.ToInt32(row["ReminderMinutes"]).ToString();
            if (ddlEventReminder.Items.FindByValue(rm) != null)
                ddlEventReminder.SelectedValue = rm;
        }
        if (row.Table.Columns.Contains("AssigneeUserId") && row["AssigneeUserId"] != DBNull.Value)
        {
            string aid = Convert.ToInt32(row["AssigneeUserId"]).ToString();
            if (ddlAssignee.Items.FindByValue(aid) != null)
                ddlAssignee.SelectedValue = aid;
        }
        if (row.Table.Columns.Contains("Status") && row["Status"] != DBNull.Value)
        {
            string st = Convert.ToString(row["Status"]);
            if (ddlStatus.Items.FindByValue(st) != null)
                ddlStatus.SelectedValue = st;
        }
    }

    private HashSet<string> GetCachedVisibleFields()
    {
        string raw = ViewState["VisibleFields"] as string;
        if (string.IsNullOrEmpty(raw))
            return service.GetVisibleFieldSet(calendarId);
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string part in raw.Split(','))
            if (!string.IsNullOrWhiteSpace(part)) set.Add(part.Trim());
        return set;
    }

    /// <summary>
    /// Blocks save when a visible built-in field marked required is empty.
    /// Title/EventDate are already validated earlier. Creator is never required on the form.
    /// </summary>
    private string ValidateRequiredBuiltinFields(
        string time, string category, string notes, int reminderMinutes, int assigneeId, string status)
    {
        HashSet<string> required = GetCachedRequiredFields();
        HashSet<string> visible = GetCachedVisibleFields();

        if (required.Contains("EventTime") && visible.Contains("EventTime") && string.IsNullOrWhiteSpace(time))
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05DE\u05DC\u05D0 \u05D0\u05EA \u05D4\u05E9\u05D3\u05D4 \"\u05E9\u05E2\u05D4\"";

        if (required.Contains("Category") && visible.Contains("Category") && string.IsNullOrWhiteSpace(category))
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05DE\u05DC\u05D0 \u05D0\u05EA \u05D4\u05E9\u05D3\u05D4 \"\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4\"";

        if (required.Contains("Notes") && visible.Contains("Notes") && string.IsNullOrWhiteSpace(notes))
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05DE\u05DC\u05D0 \u05D0\u05EA \u05D4\u05E9\u05D3\u05D4 \"\u05D4\u05E2\u05E8\u05D5\u05EA\"";

        if (required.Contains("Assignee") && visible.Contains("Assignee") && assigneeId <= 0)
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05D1\u05D7\u05D5\u05E8 \u05D0\u05D7\u05E8\u05D0\u05D9";

        if (required.Contains("Status") && visible.Contains("Status") && string.IsNullOrWhiteSpace(status))
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05D1\u05D7\u05D5\u05E8 \u05E1\u05D8\u05D8\u05D5\u05E1";

        // "0" = ללא תזכורת — treat as empty when Reminder is required
        if (required.Contains("Reminder") && visible.Contains("Reminder") && reminderMinutes <= 0)
            return "\u05D7\u05D5\u05D1\u05D4 \u05DC\u05D1\u05D7\u05D5\u05E8 \u05EA\u05D6\u05DB\u05D5\u05E8\u05EA";

        return null;
    }

    private void CacheMemberColors()
    {
        memberColors = new Dictionary<int, string>();
        try
        {
            service.EnsureMemberColors(calendarId);
            DataTable members = service.GetCalendarMembers(calendarId);
            foreach (DataRow row in members.Rows)
            {
                int uid = Convert.ToInt32(row["UserId"]);
                string color = Convert.ToString(row["MemberColor"]);
                if (string.IsNullOrWhiteSpace(color))
                    color = SharedCalendarService.GetDefaultColorForUser(uid);
                memberColors[uid] = color;
            }
        }
        catch { }
    }

    private string GetCachedMemberColor(int userId)
    {
        if (memberColors != null && memberColors.ContainsKey(userId))
            return memberColors[userId];
        return service.GetMemberColor(calendarId, userId);
    }

    protected void btnSendSchedule_Click(object sender, EventArgs e)
    {
        lblEmailMsg.Text = "";
        string to = txtEmailTo.Text != null ? txtEmailTo.Text.Trim() : "";
        if (string.IsNullOrWhiteSpace(to) || to.IndexOf('@') < 0)
        {
            lblEmailMsg.Text = "נא להזין כתובת אימייל תקינה.";
            lblEmailMsg.CssClass = "form-message";
            lblEmailMsg.ForeColor = System.Drawing.Color.OrangeRed;
            return;
        }

        try
        {
            DateTime? from = null;
            DateTime? toDate = null;
            DateTime tmp;
            if (DateTime.TryParse(txtEmailFrom.Text, out tmp)) from = tmp;
            if (DateTime.TryParse(txtEmailToDate.Text, out tmp)) toDate = tmp;

            string calendarName = calendarTitle.Text;
            if (string.IsNullOrWhiteSpace(calendarName)) calendarName = "טבלה משותפת";

            string html = service.BuildScheduleEmailHtml(calendarId, from, toDate);
            string subject = "לו\"ז: " + calendarName;
            EmailService.SendEmail(to, subject, html, true);
            lblEmailMsg.Text = "הלו\"ז נשלח בהצלחה ל-" + to;
            lblEmailMsg.ForeColor = System.Drawing.Color.LightGreen;
            lblEmailMsg.CssClass = "form-message auth-success";
        }
        catch (Exception ex)
        {
            LoggingService.Log("sharedCalendarDetails", "Send schedule email failed", ex);
            lblEmailMsg.Text = "שגיאה בשליחה: " + ex.Message;
            lblEmailMsg.ForeColor = System.Drawing.Color.OrangeRed;
        }
    }

    private void BindAssigneeList()
    {
        string selected = ddlAssignee.SelectedValue;
        ddlAssignee.Items.Clear();
        ddlAssignee.Items.Add(new ListItem("ללא אחראי", "0"));
        DataTable members = service.GetCalendarMembers(calendarId);
        foreach (DataRow row in members.Rows)
        {
            string uid = Convert.ToString(row["UserId"]);
            string uname = Utilities.FixEncoding(Convert.ToString(row["UserName"]));
            ddlAssignee.Items.Add(new ListItem(uname, uid));
        }
        if (!string.IsNullOrEmpty(selected) && ddlAssignee.Items.FindByValue(selected) != null)
            ddlAssignee.SelectedValue = selected;
    }

    protected void btnCreateInvite_Click(object sender, EventArgs e)
    {
        if (!isAdmin)
        {
            lblInviteMsg.Text = "רק מנהל יכול ליצור קישור הזמנה.";
            return;
        }
        try
        {
            string perm = ddlInvitePermission.SelectedValue;
            string token = CalendarInviteService.CreateInvite(calendarId, currentUserId, perm, 50, 30);
            string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority) + ResolveUrl("~/acceptInvite.aspx");
            txtInviteLink.Text = baseUrl + "?token=" + token;
            lblInviteMsg.Text = "הקישור נוצר — העתק ושלח לחברים.";
            lblInviteMsg.CssClass = "form-message auth-success";
        }
        catch (Exception ex)
        {
            LoggingService.Log("sharedCalendarDetails", "Create invite failed", ex);
            lblInviteMsg.Text = "שגיאה ביצירת קישור: " + ex.Message;
        }
    }

    // שמירת אירועים שפורסרו מטקסט מודבק (JSON) למסד הנתונים
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
                    string title = eventData.ContainsKey("title") ? Utilities.FixEncoding(eventData["title"].ToString().Trim()) : "";
                    string startTime = eventData.ContainsKey("startTime") ? Utilities.FixEncoding(eventData["startTime"].ToString().Trim()) : "";
                    string endTime = eventData.ContainsKey("endTime") ? Utilities.FixEncoding(eventData["endTime"].ToString().Trim()) : "";
                    string location = eventData.ContainsKey("location") ? Utilities.FixEncoding(eventData["location"].ToString().Trim()) : "";
                    string description = eventData.ContainsKey("description") ? Utilities.FixEncoding(eventData["description"].ToString().Trim()) : "";

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

    // הצגת הודעה כשהטבלה לא נמצאה
    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }

    // שליחת בקשת הצטרפות לטבלה המשותפת
    protected void btnSendJoinRequest_Click(object sender, EventArgs e)
    {
        try
        {
            // Note: New schema doesn't support message yet, so we ignore txtJoinMessage.Text
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
            
            PermissionService.RequestJoin(calendarId, currentUserId);
            
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

    // מעבר ללשונית אירועים ולוח השנה
    protected void btnTabEvents_Click(object sender, EventArgs e)
    {
        pnlEvents.Visible = true;
        pnlRequests.Visible = false;
        btnTabEvents.CssClass = "tab-button active";
        btnTabRequests.CssClass = "tab-button";
        BindCalendar();
    }

    // מעבר ללשונית בקשות הצטרפות ממתינות
    protected void btnTabRequests_Click(object sender, EventArgs e)
    {
        pnlEvents.Visible = false;
        pnlRequests.Visible = true;
        btnTabEvents.CssClass = "tab-button";
        btnTabRequests.CssClass = "tab-button active";
        LoadRequests();
    }

    // בדיקה אם ערך שדה לא תקין / ריק / placeholder
    private bool IsInvalidValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        
        string trimmed = value.Trim();
        return trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." || 
               trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟" ||
               (trimmed.Length <= 1 && (trimmed == "." || trimmed == "؟"));
    }


    // טעינת בקשות הצטרפות ממתינות לטבלה
    private void LoadRequests()
    {
        DataTable dt = PermissionService.GetPendingRequestsForCalendar(calendarId);
        if (dt == null || dt.Rows.Count == 0)
        {
            lblNoRequests.Visible = true;
            dlRequests.Visible = false;
            return;
        }
        
        foreach (DataRow row in dt.Rows)
        {
            if (row.Table.Columns.Contains("UserName") && row["UserName"] != DBNull.Value && row["UserName"] != null)
                row["UserName"] = Utilities.FixEncoding(row["UserName"].ToString());
            if (row.Table.Columns.Contains("firstName") && row["firstName"] != DBNull.Value && row["firstName"] != null)
                row["firstName"] = Utilities.FixEncoding(row["firstName"].ToString());
            if (row.Table.Columns.Contains("FirstName") && row["FirstName"] != DBNull.Value && row["FirstName"] != null)
                row["FirstName"] = Utilities.FixEncoding(row["FirstName"].ToString());
            if (row.Table.Columns.Contains("lastName") && row["lastName"] != DBNull.Value && row["lastName"] != null)
                row["lastName"] = Utilities.FixEncoding(row["lastName"].ToString());
            if (row.Table.Columns.Contains("LastName") && row["LastName"] != DBNull.Value && row["LastName"] != null)
                row["LastName"] = Utilities.FixEncoding(row["LastName"].ToString());
            if (row.Table.Columns.Contains("Message") && row["Message"] != DBNull.Value && row["Message"] != null)
                row["Message"] = Utilities.FixEncoding(row["Message"].ToString());
        }
        
        lblNoRequests.Visible = false;
        dlRequests.Visible = true;
        dlRequests.DataSource = dt;
        dlRequests.DataBind();
    }

    // פתיחת טופס הוספת אירוע חדש לתאריך הנבחר
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

    // שמירת אירוע חדש או עדכון אירוע קיים
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
            string title = Utilities.FixEncoding(titleText);
            string dateStr = txtEventDate.Text;
            string timeText = txtEventTime.Text != null ? txtEventTime.Text.Trim() : "";
            string time = Utilities.FixEncoding(timeText);
            string notesText = txtEventNotes.Text != null ? txtEventNotes.Text.Trim() : "";
            string notes = Utilities.FixEncoding(notesText);
            string categoryValue = ddlEventCategory.SelectedValue != null ? ddlEventCategory.SelectedValue.Trim() : "אחר";
            string category = Utilities.FixEncoding(categoryValue);

            if (string.IsNullOrWhiteSpace(title) || IsInvalidValue(title))
            {
                lblSaveError.Text = "\u05D0\u05E0\u05D0 \u05D4\u05D6\u05DF \u05DB\u05D5\u05EA\u05E8\u05EA \u05DC\u05D0\u05D9\u05E8\u05D5\u05E2";
                lblSaveError.Visible = true;
                return;
            }

            if (string.IsNullOrEmpty(dateStr))
            {
                lblSaveError.Text = "\u05D0\u05E0\u05D0 \u05D1\u05D7\u05E8 \u05EA\u05D0\u05E8\u05D9\u05DA \u05DC\u05D0\u05D9\u05E8\u05D5\u05E2";
                lblSaveError.Visible = true;
                return;
            }

            if (IsInvalidValue(time))
                time = "";

            if (IsInvalidValue(notes))
                notes = "";

            if (IsInvalidValue(category))
                category = "";

            int reminderMinutes = 60;
            int.TryParse(ddlEventReminder.SelectedValue, out reminderMinutes);
            int assigneeId = 0;
            int.TryParse(ddlAssignee.SelectedValue, out assigneeId);
            int? assignee = assigneeId > 0 ? (int?)assigneeId : null;
            string status = ddlStatus.SelectedValue ?? "Pending";

            DateTime eventDate;
            if (!DateTime.TryParse(dateStr, out eventDate))
            {
                lblSaveError.Text = "\u05EA\u05D0\u05E8\u05D9\u05DA \u05DC\u05D0 \u05EA\u05E7\u05D9\u05DF";
                lblSaveError.Visible = true;
                return;
            }

            string builtinError = ValidateRequiredBuiltinFields(time, category, notes, reminderMinutes, assigneeId, status);
            if (!string.IsNullOrEmpty(builtinError))
            {
                lblSaveError.Text = builtinError;
                lblSaveError.Visible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
                category = "\u05D0\u05D7\u05E8";

            string customError;
            Dictionary<string, string> customValues = CollectCustomFieldValues(out customError);
            if (!string.IsNullOrEmpty(customError))
            {
                lblSaveError.Text = customError;
                lblSaveError.Visible = true;
                return;
            }
            
            int? editingId = ViewState["EditingEventId"] as int?;
            int savedEventId = 0;
            
            if (editingId.HasValue)
            {
                service.UpdateSharedCalendarEvent(editingId.Value, title, eventDate, time, notes, category, reminderMinutes, assignee, status);
                savedEventId = editingId.Value;
            }
            else
            {
                savedEventId = service.AddSharedCalendarEvent(calendarId, title, eventDate, time, notes, category, currentUserId, reminderMinutes, assignee, status);
            }

            if (savedEventId > 0)
                service.SetEventFieldValues(savedEventId, customValues);

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

    // טעינת אירוע קיים לטופס העריכה
    protected void lnkEdit_Click(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        if (btn == null) return;
        int eventId;
        if (!int.TryParse(btn.CommandArgument, out eventId)) return;
        LoadEventIntoForm(eventId);
    }

    // מחיקת אירוע מהטבלה המשותפת
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

    // אתחול ברירת מחדל להרשאה בכל שורת בקשת הצטרפות
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

    // אישור בקשת הצטרפות והענקת הרשאה למשתמש
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

        // Map permission to ID (1=Read, 2=ReadWrite)
        int permissionTypeId = 1;
        if (permission == "ReadWrite")
            permissionTypeId = 2;

        try {
            PermissionService.ApproveJoinRequest(requestId, permissionTypeId);
            LoadRequests();
        }
        catch (Exception) {
            // Ignore error
        }
    }

    // דחיית בקשת הצטרפות לטבלה
    protected void btnReject_Click(object sender, EventArgs e)
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            return;

        Button btn = sender as Button;
        int requestId = Convert.ToInt32(btn.CommandArgument);
        
        try {
            PermissionService.RejectJoinRequest(requestId);
            LoadRequests();
        }
        catch (Exception) {
            // Ignore error
        }
    }

    // ניקוי שדות טופס האירוע
    private void ClearEventForm()
    {
        txtEventTitle.Text = "";
        txtEventDate.Text = "";
        txtEventTime.Text = "";
        txtEventNotes.Text = "";
        ddlEventCategory.SelectedIndex = 0;
        if (ddlEventReminder.Items.FindByValue("60") != null)
            ddlEventReminder.SelectedValue = "60";
        if (ddlAssignee.Items.Count > 0)
            ddlAssignee.SelectedIndex = 0;
        ddlStatus.SelectedValue = "Pending";
        ClearCustomFieldInputs();
    }

    // החזרת מחרוזת בטוחה מה-DB (עם תיקון קידוד)
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
            
            return Utilities.FixEncoding(str);
        }
        catch
        {
            return defaultValue;
        }
    }

    // המרת תאריך מה-DB לפורמט תצוגה dd/MM/yyyy
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

    // טעינת כל אירועי הטבלה ל-ViewState (לפני סינון לפי יום)
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

    // מעבר לחודש קודם/הבא בלוח השנה
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

    // רענון הלוח כשמשנים חודש נראה
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

    // סינון והצגת אירועים בתוך תא יום בלוח (לפי תאריך היום)
    protected void calEvents_DayRender(object sender, DayRenderEventArgs e)
    {
        try
        {
            e.Cell.CssClass = "day-cell";

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
                                        title = Utilities.FixEncoding(row["Title"].ToString());
                                    }
                                    
                                    string time = "";
                                    if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                                    {
                                        time = Utilities.FixEncoding(row["EventTime"].ToString());
                                    }
                                    
                                    string eventId = "";
                                    if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                                    {
                                        eventId = row["Id"].ToString();
                                    }
                                    
                                    string createdByName = "";
                                    if (row.Table.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                                    {
                                        createdByName = Utilities.FixEncoding(row["CreatedByName"].ToString());
                                    }

                                    int createdById = 0;
                                    if (row.Table.Columns.Contains("CreatedBy") && row["CreatedBy"] != DBNull.Value)
                                        int.TryParse(Convert.ToString(row["CreatedBy"]), out createdById);
                                    string userColor = GetCachedMemberColor(createdById);
                                    
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
                                    eventLink.Style["background-color"] = userColor;
                                    eventLink.Style["border-color"] = userColor;
                                    eventLink.Style["color"] = "#fff";
                                    
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

    // סינון נתונים לטבלה לפי יום (בחירת תאריך בלוח)
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

    // סינון נתונים לטבלה לפי יום — מציג רק אירועים של התאריך שנבחר
    private void ShowEvents(DateTime date)
    {
        var builder = new StringBuilder();
        int count = 0;
        HashSet<string> fields = GetCachedVisibleFields();
        DataTable customFields = service.GetCustomFields(calendarId, true);
        Dictionary<int, Dictionary<string, string>> allCustomValues = service.GetEventFieldValuesForCalendar(calendarId);

        DataTable eventsData = ViewState["EventsData"] as DataTable;
        if (eventsData != null && eventsData.Rows.Count > 0)
        {
            builder.Append("<div class='events-table-container'>");
            builder.Append("<table class='events-table'>");
            builder.Append("<thead><tr>");
            builder.Append("<th style='width:8px;'></th>");
            if (fields.Contains("Title")) builder.Append("<th>\u05DB\u05D5\u05EA\u05E8\u05EA</th>");
            if (fields.Contains("Category")) builder.Append("<th>\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4</th>");
            if (fields.Contains("EventTime")) builder.Append("<th>\u05E9\u05E2\u05D4</th>");
            if (fields.Contains("Assignee")) builder.Append("<th>\u05D0\u05D7\u05E8\u05D0\u05D9</th>");
            if (fields.Contains("Status")) builder.Append("<th>\u05E1\u05D8\u05D8\u05D5\u05E1</th>");
            if (fields.Contains("Notes")) builder.Append("<th>\u05D4\u05E2\u05E8\u05D5\u05EA</th>");
            if (fields.Contains("Creator")) builder.Append("<th>\u05E0\u05D5\u05E6\u05E8 \u05E2\u05DC \u05D9\u05D3\u05D9</th>");
            if (customFields != null)
            {
                foreach (DataRow cf in customFields.Rows)
                {
                    builder.AppendFormat("<th>{0}</th>",
                        System.Web.HttpUtility.HtmlEncode(Convert.ToString(cf["FieldLabel"])));
                }
            }
            builder.Append("<th>\u05E4\u05E2\u05D5\u05DC\u05D5\u05EA</th>");
            builder.Append("</tr></thead><tbody>");

            DataTable membersLookup = service.GetCalendarMembers(calendarId);
            Dictionary<string, string> memberNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow m in membersLookup.Rows)
                memberNames[Convert.ToString(m["UserId"])] = Utilities.FixEncoding(Convert.ToString(m["UserName"]));

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
                                title = Utilities.FixEncoding(row["Title"].ToString());

                            string category = "";
                            if (row.Table.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
                                category = Utilities.FixEncoding(row["Category"].ToString());

                            string time = "";
                            if (row.Table.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                                time = Utilities.FixEncoding(row["EventTime"].ToString());

                            string notes = "";
                            if (row.Table.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
                                notes = Utilities.FixEncoding(row["Notes"].ToString());

                            string createdByName = "";
                            if (row.Table.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                                createdByName = Utilities.FixEncoding(row["CreatedByName"].ToString());

                            string eventId = "";
                            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                                eventId = row["Id"].ToString();

                            if (string.IsNullOrEmpty(title))
                                title = "ללא כותרת";

                            string role = Session["Role"] != null ? Session["Role"].ToString() : "";
                            bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
                            bool userIsAdmin = isAdmin || isOwner;
                            bool canEdit = userIsAdmin || service.CanUserEdit(calendarId, currentUserId);

                            string assigneeName = "";
                            if (row.Table.Columns.Contains("AssigneeUserId") && row["AssigneeUserId"] != DBNull.Value)
                            {
                                string aid = Convert.ToString(row["AssigneeUserId"]);
                                if (!memberNames.TryGetValue(aid, out assigneeName))
                                    assigneeName = aid;
                            }

                            string statusLabel = "";
                            if (row.Table.Columns.Contains("Status") && row["Status"] != DBNull.Value)
                            {
                                string st = Convert.ToString(row["Status"]);
                                if (st == "Done") statusLabel = "בוצע";
                                else if (st == "InProgress") statusLabel = "בתהליך";
                                else if (st == "Pending") statusLabel = "ממתין";
                                else statusLabel = st;
                            }

                            int createdById = 0;
                            if (row.Table.Columns.Contains("CreatedBy") && row["CreatedBy"] != DBNull.Value)
                                int.TryParse(Convert.ToString(row["CreatedBy"]), out createdById);
                            string userColor = GetCachedMemberColor(createdById);

                            builder.AppendFormat("<tr style='border-right:4px solid {0};'>", userColor);
                            builder.AppendFormat("<td style='background:{0};width:8px;padding:0;'></td>", userColor);
                            if (fields.Contains("Title"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(title));
                            if (fields.Contains("Category"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(category));
                            if (fields.Contains("EventTime"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(time));
                            if (fields.Contains("Assignee"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(assigneeName));
                            if (fields.Contains("Status"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(statusLabel));
                            if (fields.Contains("Notes"))
                                builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(notes));
                            if (fields.Contains("Creator"))
                                builder.AppendFormat("<td><span style='color:{0};font-weight:600;'>{1}</span></td>", userColor, System.Web.HttpUtility.HtmlEncode(createdByName));

                            int eventIdNum = 0;
                            int.TryParse(eventId, out eventIdNum);
                            Dictionary<string, string> eventVals = null;
                            if (eventIdNum > 0)
                                allCustomValues.TryGetValue(eventIdNum, out eventVals);

                            if (customFields != null)
                            {
                                foreach (DataRow cf in customFields.Rows)
                                {
                                    string cfKey = Convert.ToString(cf["FieldKey"]);
                                    string cfType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(cf["FieldType"]));
                                    string cfLabel = Convert.ToString(cf["FieldLabel"]);
                                    string cfOptions = Convert.ToString(cf["FieldOptions"] ?? "");

                                    if (cfType == "button")
                                    {
                                        string url = (cfOptions ?? "").Trim();
                                        if (!string.IsNullOrEmpty(url) &&
                                            !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                                            !url.StartsWith("/", StringComparison.Ordinal))
                                        {
                                            url = "https://" + url;
                                        }
                                        if (string.IsNullOrEmpty(url))
                                            builder.Append("<td></td>");
                                        else
                                            builder.AppendFormat(
                                                "<td><a class='custom-field-btn-link' href='{0}' target='_blank' rel='noopener noreferrer'>{1}</a></td>",
                                                System.Web.HttpUtility.HtmlEncode(url).Replace("\"", "&quot;"),
                                                System.Web.HttpUtility.HtmlEncode(cfLabel));
                                    }
                                    else
                                    {
                                        string v = "";
                                        if (eventVals != null && eventVals.ContainsKey(cfKey))
                                            v = eventVals[cfKey];
                                        builder.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(v ?? ""));
                                    }
                                }
                            }

                            builder.Append("<td>");
                            if (canEdit)
                            {
                                builder.AppendFormat("<a href='sharedCalendarDetails.aspx?id={0}&editEvent={1}' class='edit-link'>\u05E2\u05E8\u05D5\u05DA</a> ", calendarId, eventId);
                                builder.AppendFormat(
                                    "<a href='javascript:void(0)' onclick='if(confirm(\"{2}\")) window.location.href=\"sharedCalendarDetails.aspx?id={0}&deleteEvent={1}\"' class='delete-link'>\u05DE\u05D7\u05E7</a>",
                                    calendarId,
                                    eventId,
                                    "\u05D4\u05D0\u05DD \u05D0\u05EA\u05D4 \u05D1\u05D8\u05D5\u05D7 \u05E9\u05D1\u05E8\u05E6\u05D5\u05E0\u05DA \u05DC\u05DE\u05D7\u05D5\u05E7 \u05D0\u05EA \u05D4\u05D0\u05D9\u05E8\u05D5\u05E2?");
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

            builder.Append("</tbody></table></div>");
        }

        if (count == 0)
        {
            builder.Append("<div class='calendar-event empty'>\u05D0\u05D9\u05DF \u05D0\u05D9\u05E8\u05D5\u05E2\u05D9\u05DD \u05DC\u05EA\u05D0\u05E8\u05D9\u05DA \u05D4\u05D6\u05D4.</div>");
        }

        lblEvents.Text = builder.ToString();
    }
}
