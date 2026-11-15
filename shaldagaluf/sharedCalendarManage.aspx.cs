using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class sharedCalendarManage : System.Web.UI.Page
{
    SharedCalendarService service = new SharedCalendarService();
    private int calendarId;
    private int currentUserId;

    protected int CalendarId { get { return calendarId; } }

    // Hebrew UI labels via \u escapes so compilation is encoding-safe even without BOM.
    private static readonly Dictionary<string, string> FieldLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Title", "\u05DB\u05D5\u05EA\u05E8\u05EA" },
        { "EventDate", "\u05EA\u05D0\u05E8\u05D9\u05DA" },
        { "EventTime", "\u05E9\u05E2\u05D4" },
        { "Category", "\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4" },
        { "Notes", "\u05D4\u05E2\u05E8\u05D5\u05EA" },
        { "Assignee", "\u05D0\u05D7\u05E8\u05D0\u05D9" },
        { "Status", "\u05E1\u05D8\u05D8\u05D5\u05E1" },
        { "Creator", "\u05E0\u05D5\u05E6\u05E8 \u05E2\u05DC \u05D9\u05D3\u05D9" },
        { "Reminder", "\u05EA\u05D6\u05DB\u05D5\u05E8\u05EA" }
    };

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;

        if (Session["username"] == null || Session["userId"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!int.TryParse(Request.QueryString["id"], out calendarId) || calendarId <= 0)
        {
            ShowDenied();
            return;
        }

        currentUserId = Convert.ToInt32(Session["userId"]);
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
        bool isAdmin = service.IsCalendarAdmin(calendarId, currentUserId) || isOwner;

        if (!isAdmin)
        {
            ShowDenied();
            return;
        }

        lnkBack.NavigateUrl = "sharedCalendarDetails.aspx?id=" + calendarId;

        if (!IsPostBack)
            LoadAll();
    }

    private void ShowDenied()
    {
        pnlContent.Visible = false;
        pnlDenied.Visible = true;
    }

    private void LoadAll()
    {
        DataRow calendar = service.GetSharedCalendar(calendarId);
        if (calendar == null)
        {
            ShowDenied();
            return;
        }

        // DB text may need FixEncoding; do not apply to static source literals.
        if (calendar.Table.Columns.Contains("CalendarName") && calendar["CalendarName"] != DBNull.Value)
            txtName.Text = Utilities.FixEncoding(Convert.ToString(calendar["CalendarName"]));
        else if (calendar.Table.Columns.Contains("Name") && calendar["Name"] != DBNull.Value)
            txtName.Text = Utilities.FixEncoding(Convert.ToString(calendar["Name"]));

        if (calendar.Table.Columns.Contains("Description") && calendar["Description"] != DBNull.Value)
            txtDescription.Text = Utilities.FixEncoding(Convert.ToString(calendar["Description"]));

        BindVisibleFields();
        BindCustomFields();
        UpdateNewFieldOptionsVisibility();
        BindMembers();
        BindEvents();
    }

    private static bool IsAlwaysRequiredBuiltin(string key)
    {
        foreach (string always in SharedCalendarService.AlwaysRequiredBuiltinFields)
        {
            if (string.Equals(always, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void BindVisibleFields()
    {
        HashSet<string> visible = service.GetVisibleFieldSet(calendarId);
        HashSet<string> required = service.GetRequiredFieldSet(calendarId);

        DataTable dt = new DataTable();
        dt.Columns.Add("Key", typeof(string));
        dt.Columns.Add("Label", typeof(string));
        dt.Columns.Add("IsVisible", typeof(bool));
        dt.Columns.Add("IsRequired", typeof(bool));
        dt.Columns.Add("AlwaysRequired", typeof(bool));
        dt.Columns.Add("AllowRequired", typeof(bool));

        foreach (var kv in FieldLabels)
        {
            bool always = IsAlwaysRequiredBuiltin(kv.Key);
            bool allowRequired = !string.Equals(kv.Key, "Creator", StringComparison.OrdinalIgnoreCase);
            DataRow row = dt.NewRow();
            row["Key"] = kv.Key;
            row["Label"] = kv.Value;
            row["IsVisible"] = always || visible.Contains(kv.Key);
            row["IsRequired"] = always || (allowRequired && required.Contains(kv.Key));
            row["AlwaysRequired"] = always;
            row["AllowRequired"] = allowRequired;
            dt.Rows.Add(row);
        }

        rptBuiltinFields.DataSource = dt;
        rptBuiltinFields.DataBind();
    }

    protected void rptBuiltinFields_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            return;

        DataRowView row = e.Item.DataItem as DataRowView;
        if (row == null) return;

        bool alwaysRequired = Convert.ToBoolean(row["AlwaysRequired"]);
        bool allowRequired = Convert.ToBoolean(row["AllowRequired"]);
        bool isVisible = Convert.ToBoolean(row["IsVisible"]);
        bool isRequired = Convert.ToBoolean(row["IsRequired"]);

        CheckBox chkVisible = e.Item.FindControl("chkVisible") as CheckBox;
        CheckBox chkRequired = e.Item.FindControl("chkRequired") as CheckBox;
        Literal litAlwaysRequired = e.Item.FindControl("litAlwaysRequired") as Literal;

        if (chkVisible != null)
        {
            chkVisible.Checked = isVisible;
            chkVisible.Enabled = !alwaysRequired;
            if (alwaysRequired)
                chkVisible.CssClass = (chkVisible.CssClass + " bf-locked").Trim();
        }

        if (chkRequired != null)
        {
            chkRequired.Checked = isRequired;
            chkRequired.Enabled = allowRequired && !alwaysRequired;
            if (!chkRequired.Enabled)
                chkRequired.CssClass = (chkRequired.CssClass + " bf-locked").Trim();
            if (!allowRequired)
                chkRequired.Visible = false;
        }

        if (litAlwaysRequired != null)
        {
            litAlwaysRequired.Text = alwaysRequired
                ? "<span class='bf-always-badge'>\u05D7\u05D5\u05D1\u05D4</span>"
                : "";
        }
    }

    private int? EditingCustomFieldId
    {
        get { return ViewState["EditingCustomFieldId"] as int?; }
        set { ViewState["EditingCustomFieldId"] = value; }
    }

    private void BindCustomFields()
    {
        DataTable fields = service.GetCustomFields(calendarId, true);
        if (fields == null || fields.Rows.Count == 0)
        {
            rptCustomFields.Visible = false;
            lblNoCustomFields.Visible = true;
            return;
        }

        lblNoCustomFields.Visible = false;
        rptCustomFields.Visible = true;
        rptCustomFields.DataSource = fields;
        rptCustomFields.DataBind();
    }

    private void UpdateNewFieldOptionsVisibility()
    {
        string type = SharedCalendarService.NormalizeCustomFieldType(ddlNewFieldType.SelectedValue);
        bool show = type == "list" || type == "button";
        pnlNewFieldOptions.Visible = show;
        if (type == "button")
        {
            lblNewFieldOptions.Text = "\u05DB\u05EA\u05D5\u05D1\u05EA URL \u05DC\u05DB\u05E4\u05EA\u05D5\u05E8";
            litNewOptionsHint.Text = "<span class='cf-options-hint'>\u05DC\u05D3\u05D5\u05D2\u05DE\u05D4: https://maps.google.com/...</span>";
        }
        else if (type == "list")
        {
            lblNewFieldOptions.Text = "\u05D0\u05E4\u05E9\u05E8\u05D5\u05D9\u05D5\u05EA \u05DC\u05E8\u05E9\u05D9\u05DE\u05D4 (\u05E9\u05D5\u05E8\u05D4 \u05DC\u05DB\u05DC \u05D0\u05E4\u05E9\u05E8\u05D5\u05EA)";
            litNewOptionsHint.Text = "<span class='cf-options-hint'>\u05DB\u05DC \u05D0\u05E4\u05E9\u05E8\u05D5\u05EA \u05D1\u05E9\u05D5\u05E8\u05D4 \u05E0\u05E4\u05E8\u05D3\u05EA, \u05D0\u05D5 \u05DE\u05D5\u05E4\u05E8\u05D3\u05D5\u05EA \u05D1\u05E4\u05E1\u05D9\u05E7\u05D9\u05DD</span>";
        }
        else
        {
            litNewOptionsHint.Text = "";
        }
    }

    protected void ddlNewFieldType_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateNewFieldOptionsVisibility();
    }

    protected void ddlEditType_SelectedIndexChanged(object sender, EventArgs e)
    {
        DropDownList ddl = sender as DropDownList;
        if (ddl == null) return;
        RepeaterItem item = ddl.NamingContainer as RepeaterItem;
        if (item == null) return;
        UpdateEditOptionsVisibility(item);
    }

    private void UpdateEditOptionsVisibility(RepeaterItem item)
    {
        DropDownList ddlEditType = item.FindControl("ddlEditType") as DropDownList;
        TextBox txtEditOptions = item.FindControl("txtEditOptions") as TextBox;
        Label lblEditOptions = item.FindControl("lblEditOptions") as Label;
        if (ddlEditType == null || txtEditOptions == null || lblEditOptions == null) return;

        string type = SharedCalendarService.NormalizeCustomFieldType(ddlEditType.SelectedValue);
        bool show = type == "list" || type == "button";
        txtEditOptions.Visible = show;
        lblEditOptions.Visible = show;
        if (type == "button")
            lblEditOptions.Text = "\u05DB\u05EA\u05D5\u05D1\u05EA URL \u05DC\u05DB\u05E4\u05EA\u05D5\u05E8";
        else
            lblEditOptions.Text = "\u05D0\u05E4\u05E9\u05E8\u05D5\u05D9\u05D5\u05EA \u05DC\u05E8\u05E9\u05D9\u05DE\u05D4";
    }

    protected void rptCustomFields_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            return;

        DataRowView row = e.Item.DataItem as DataRowView;
        if (row == null) return;

        int fieldId = Convert.ToInt32(row["Id"]);
        bool isRequired = row["IsRequired"] != DBNull.Value && Convert.ToBoolean(row["IsRequired"]);
        string fieldType = SharedCalendarService.NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
        string options = Convert.ToString(row["FieldOptions"] ?? "");

        Literal litRequiredBadge = e.Item.FindControl("litRequiredBadge") as Literal;
        if (litRequiredBadge != null)
        {
            litRequiredBadge.Text = isRequired
                ? "<span class='cf-required-badge'>\u05D7\u05D5\u05D1\u05D4</span>"
                : "";
        }

        Panel pnlViewOptions = e.Item.FindControl("pnlViewOptions") as Panel;
        Literal litOptionsPreview = e.Item.FindControl("litOptionsPreview") as Literal;
        if (pnlViewOptions != null && litOptionsPreview != null)
        {
            if (fieldType == "list" && !string.IsNullOrWhiteSpace(options))
            {
                pnlViewOptions.Visible = true;
                string[] opts = SharedCalendarService.ParseCustomFieldOptions(options);
                litOptionsPreview.Text = "\u05D0\u05E4\u05E9\u05E8\u05D5\u05D9\u05D5\u05EA: " +
                    System.Web.HttpUtility.HtmlEncode(string.Join(" \u00B7 ", opts));
            }
            else if (fieldType == "button" && !string.IsNullOrWhiteSpace(options))
            {
                pnlViewOptions.Visible = true;
                litOptionsPreview.Text = "URL: " + System.Web.HttpUtility.HtmlEncode(options.Trim());
            }
            else
            {
                pnlViewOptions.Visible = false;
            }
        }

        Panel pnlEditField = e.Item.FindControl("pnlEditField") as Panel;
        bool editing = EditingCustomFieldId.HasValue && EditingCustomFieldId.Value == fieldId;
        if (pnlEditField != null)
            pnlEditField.Visible = editing;

        if (!editing) return;

        TextBox txtEditLabel = e.Item.FindControl("txtEditLabel") as TextBox;
        DropDownList ddlEditType = e.Item.FindControl("ddlEditType") as DropDownList;
        CheckBox chkEditRequired = e.Item.FindControl("chkEditRequired") as CheckBox;
        TextBox txtEditOptions = e.Item.FindControl("txtEditOptions") as TextBox;

        if (txtEditLabel != null)
            txtEditLabel.Text = Convert.ToString(row["FieldLabel"]);
        if (ddlEditType != null && ddlEditType.Items.FindByValue(fieldType) != null)
            ddlEditType.SelectedValue = fieldType;
        if (chkEditRequired != null)
            chkEditRequired.Checked = isRequired;
        if (txtEditOptions != null)
            txtEditOptions.Text = options;

        UpdateEditOptionsVisibility(e.Item);
    }

    protected void rptCustomFields_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int fieldId;
        if (e.CommandName == "EditField")
        {
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out fieldId)) return;
            EditingCustomFieldId = fieldId;
            BindCustomFields();
            return;
        }

        if (e.CommandName == "CancelEdit")
        {
            EditingCustomFieldId = null;
            BindCustomFields();
            return;
        }

        if (e.CommandName == "DeleteField")
        {
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out fieldId)) return;
            try
            {
                service.DeleteCustomField(fieldId);
                if (EditingCustomFieldId.HasValue && EditingCustomFieldId.Value == fieldId)
                    EditingCustomFieldId = null;
                ShowMsg("\u05D4\u05E9\u05D3\u05D4 \u05D4\u05DE\u05D5\u05EA\u05D0\u05DD \u05E0\u05DE\u05D7\u05E7.");
                BindCustomFields();
            }
            catch (Exception ex)
            {
                ShowMsg("\u05E9\u05D2\u05D9\u05D0\u05D4: " + ex.Message, true);
            }
            return;
        }

        if (e.CommandName == "UpdateField")
        {
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out fieldId)) return;
            TextBox txtEditLabel = e.Item.FindControl("txtEditLabel") as TextBox;
            DropDownList ddlEditType = e.Item.FindControl("ddlEditType") as DropDownList;
            CheckBox chkEditRequired = e.Item.FindControl("chkEditRequired") as CheckBox;
            TextBox txtEditOptions = e.Item.FindControl("txtEditOptions") as TextBox;
            try
            {
                service.UpdateCustomField(
                    fieldId,
                    txtEditLabel != null ? txtEditLabel.Text : "",
                    ddlEditType != null ? ddlEditType.SelectedValue : "text",
                    txtEditOptions != null ? txtEditOptions.Text : "",
                    chkEditRequired != null && chkEditRequired.Checked);
                EditingCustomFieldId = null;
                ShowMsg("\u05D4\u05E9\u05D3\u05D4 \u05D4\u05DE\u05D5\u05EA\u05D0\u05DD \u05E2\u05D5\u05D3\u05DB\u05DF.");
                BindCustomFields();
            }
            catch (Exception ex)
            {
                ShowMsg("\u05E9\u05D2\u05D9\u05D0\u05D4: " + ex.Message, true);
                BindCustomFields();
            }
        }
    }

    protected void btnAddCustomField_Click(object sender, EventArgs e)
    {
        try
        {
            service.AddCustomField(
                calendarId,
                txtNewFieldLabel.Text,
                ddlNewFieldType.SelectedValue,
                txtNewFieldOptions.Text,
                chkNewFieldRequired.Checked);
            txtNewFieldLabel.Text = "";
            txtNewFieldOptions.Text = "";
            chkNewFieldRequired.Checked = false;
            ddlNewFieldType.SelectedValue = "text";
            UpdateNewFieldOptionsVisibility();
            ShowMsg("\u05D4\u05E9\u05D3\u05D4 \u05D4\u05DE\u05D5\u05EA\u05D0\u05DD \u05E0\u05D5\u05E1\u05E3.");
            BindCustomFields();
        }
        catch (Exception ex)
        {
            ShowMsg("\u05E9\u05D2\u05D9\u05D0\u05D4: " + ex.Message, true);
        }
    }

    private void BindMembers()
    {
        DataTable members = service.GetCalendarMembers(calendarId);
        if (members == null || members.Rows.Count == 0)
        {
            rptMembers.Visible = false;
            lblNoMembers.Visible = true;
            return;
        }

        // SelectedValue on DropDownList in ItemTemplate can fail if Permission empty
        foreach (DataRow row in members.Rows)
        {
            string perm = Convert.ToString(row["Permission"]);
            if (perm != "ReadWrite") row["Permission"] = "Read";
        }

        lblNoMembers.Visible = false;
        rptMembers.Visible = true;
        rptMembers.DataSource = members;
        rptMembers.DataBind();
    }

    private void BindEvents()
    {
        DataTable events = service.GetSharedCalendarEvents(calendarId);
        DataTable view = new DataTable();
        view.Columns.Add("Id", typeof(int));
        view.Columns.Add("Title", typeof(string));
        view.Columns.Add("EventDateDisplay", typeof(string));
        view.Columns.Add("EventTime", typeof(string));
        view.Columns.Add("Category", typeof(string));
        view.Columns.Add("CreatedByName", typeof(string));
        view.Columns.Add("RowColor", typeof(string));

        if (events != null)
        {
            foreach (DataRow row in events.Rows)
            {
                DataRow nr = view.NewRow();
                nr["Id"] = Convert.ToInt32(row["Id"]);
                nr["Title"] = Utilities.FixEncoding(Convert.ToString(row["Title"]));
                if (row["EventDate"] != DBNull.Value)
                    nr["EventDateDisplay"] = Convert.ToDateTime(row["EventDate"]).ToString("dd/MM/yyyy");
                else
                    nr["EventDateDisplay"] = "";
                nr["EventTime"] = Utilities.FixEncoding(Convert.ToString(row["EventTime"]));
                nr["Category"] = Utilities.FixEncoding(Convert.ToString(row["Category"]));
                nr["CreatedByName"] = Utilities.FixEncoding(Convert.ToString(row["CreatedByName"]));
                int createdBy = 0;
                if (row.Table.Columns.Contains("CreatedBy") && row["CreatedBy"] != DBNull.Value)
                    int.TryParse(Convert.ToString(row["CreatedBy"]), out createdBy);
                nr["RowColor"] = service.GetMemberColor(calendarId, createdBy);
                view.Rows.Add(nr);
            }
        }

        if (view.Rows.Count == 0)
        {
            rptEvents.Visible = false;
            lblNoEvents.Visible = true;
        }
        else
        {
            lblNoEvents.Visible = false;
            rptEvents.Visible = true;
            rptEvents.DataSource = view;
            rptEvents.DataBind();
        }
    }

    private void ShowMsg(string text, bool error = false)
    {
        lblMessage.Text = text;
        lblMessage.Visible = true;
        lblMessage.CssClass = error ? "form-message error" : "form-message";
    }

    protected void btnSaveInfo_Click(object sender, EventArgs e)
    {
        try
        {
            service.UpdateCalendarInfo(calendarId, txtName.Text, txtDescription.Text);
            ShowMsg("\u05E4\u05E8\u05D8\u05D9 \u05D4\u05D8\u05D1\u05DC\u05D4 \u05E0\u05E9\u05DE\u05E8\u05D5.");
        }
        catch (Exception ex)
        {
            ShowMsg("\u05E9\u05D2\u05D9\u05D0\u05D4: " + ex.Message, true);
        }
    }

    protected void btnSaveFields_Click(object sender, EventArgs e)
    {
        List<string> visible = new List<string>();
        List<string> required = new List<string>();

        foreach (RepeaterItem item in rptBuiltinFields.Items)
        {
            if (item.ItemType != ListItemType.Item && item.ItemType != ListItemType.AlternatingItem)
                continue;

            HiddenField hfKey = item.FindControl("hfFieldKey") as HiddenField;
            CheckBox chkVisible = item.FindControl("chkVisible") as CheckBox;
            CheckBox chkRequired = item.FindControl("chkRequired") as CheckBox;
            if (hfKey == null) continue;

            string key = hfKey.Value;
            if (string.IsNullOrWhiteSpace(key)) continue;

            bool always = IsAlwaysRequiredBuiltin(key);
            bool allowRequired = !string.Equals(key, "Creator", StringComparison.OrdinalIgnoreCase);
            bool show = always || (chkVisible != null && chkVisible.Checked);
            bool req = always || (allowRequired && chkRequired != null && chkRequired.Checked);

            if (req) show = true;
            if (show) visible.Add(key);
            if (req) required.Add(key);
        }

        service.SetBuiltinFieldSettings(
            calendarId,
            string.Join(",", visible.ToArray()),
            string.Join(",", required.ToArray()));
        BindVisibleFields();
        ShowMsg("\u05E9\u05D3\u05D5\u05EA \u05D4\u05EA\u05E6\u05D5\u05D2\u05D4 \u05E0\u05E9\u05DE\u05E8\u05D5.");
    }

    protected void rptMembers_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            return;
        DropDownList ddlPerm = e.Item.FindControl("ddlPerm") as DropDownList;
        HiddenField hfPerm = e.Item.FindControl("hfPerm") as HiddenField;
        if (ddlPerm == null || hfPerm == null) return;
        string perm = hfPerm.Value == "ReadWrite" ? "ReadWrite" : "Read";
        if (ddlPerm.Items.FindByValue(perm) != null)
            ddlPerm.SelectedValue = perm;
    }

    protected void rptMembers_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int userId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out userId))
            return;

        if (e.CommandName == "RemoveMember")
        {
            if (service.IsCalendarAdmin(calendarId, userId))
            {
                ShowMsg("\u05DC\u05D0 \u05E0\u05D9\u05EA\u05DF \u05DC\u05D4\u05E1\u05D9\u05E8 \u05D0\u05EA \u05DE\u05E0\u05D4\u05DC \u05D4\u05D8\u05D1\u05DC\u05D4.", true);
                BindMembers();
                return;
            }
            service.RemoveMember(calendarId, userId);
            ShowMsg("\u05D4\u05D7\u05D1\u05E8 \u05D4\u05D5\u05E1\u05E8.");
            BindMembers();
            return;
        }

        if (e.CommandName == "SaveMember")
        {
            TextBox txtColor = e.Item.FindControl("txtColor") as TextBox;
            DropDownList ddlPerm = e.Item.FindControl("ddlPerm") as DropDownList;
            string color = txtColor != null ? txtColor.Text : "";
            string perm = ddlPerm != null ? ddlPerm.SelectedValue : "Read";
            service.SetMemberColor(calendarId, userId, color);
            service.UpdateMemberPermission(calendarId, userId, perm);
            ShowMsg("\u05E4\u05E8\u05D8\u05D9 \u05D4\u05D7\u05D1\u05E8 \u05E2\u05D5\u05D3\u05DB\u05E0\u05D5.");
            BindMembers();
        }
    }

    protected void rptEvents_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "DeleteEvent") return;
        int eventId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out eventId))
            return;
        service.DeleteSharedCalendarEvent(eventId);
        ShowMsg("\u05D4\u05D0\u05D9\u05E8\u05D5\u05E2 \u05E0\u05DE\u05D7\u05E7.");
        BindEvents();
    }
}
