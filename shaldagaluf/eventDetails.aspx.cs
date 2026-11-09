using System;
using System.Data;
using System.Data.OleDb;
using System.Text;

public partial class eventDetails : System.Web.UI.Page
{
    private int eventId;

    // טעינת הדף והפעלת לוגיקה ראשונית
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!int.TryParse(Request.QueryString["id"], out eventId))
        {
            ShowNotFound();
            return;
        }

        if (!IsPostBack)
        {
            LoadEvent();
        }
    }

    // טעינה וקישור נתונים: LoadEvent
    private void LoadEvent()
    {
        try
        {
            string conStr = Connect.GetConnectionString();
            bool found = false;

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                if (TryLoadEvent(con, "CalendarEvents", false))
                    return;

                if (TryLoadEvent(con, "SharedCalendarEvents", true))
                    return;
            }

            if (!found)
            {
                ShowNotFound();
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("EVENTDETAILS", "Error loading event", ex);
            ShowNotFound();
        }
    }

    // אירוע משותף נשמר בלי עמודת UserId. אם קוראים לעמודה חסרה, הדף זורק שגיאה ומציג "not found".
    private bool TryLoadEvent(OleDbConnection con, string tableName, bool shared)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + tableName + "] WHERE Id = ?", con))
            {
                cmd.Parameters.AddWithValue("?", eventId);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr == null || !dr.Read())
                        return false;
                    if (!CanViewEvent(dr, con, shared))
                        return false;
                    LoadEventData(dr, con);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("EVENTDETAILS", "Skip table " + tableName, ex);
            return false;
        }
    }

    private bool CanViewEvent(OleDbDataReader dr, OleDbConnection con, bool shared)
    {
        int currentUserId = Session["userId"] != null ? Convert.ToInt32(Session["userId"]) : 0;
        string role = Session["Role"] != null ? Session["Role"].ToString() : "user";
        if (string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
            return true;

        object ownerValue = ReadField(dr, "UserId", "CreatedBy");
        int rowUserId = ownerValue != null ? Convert.ToInt32(ownerValue) : 0;
        if (rowUserId == currentUserId && currentUserId > 0)
            return true;

        if (!shared)
            return false;

        object calendarValue = ReadField(dr, "CalendarId");
        if (calendarValue == null)
            return false;

        int calendarId = Convert.ToInt32(calendarValue);
        SharedCalendarService service = new SharedCalendarService();
        return service.IsCalendarMember(calendarId, currentUserId) || service.IsCalendarAdmin(calendarId, currentUserId);
    }

    private static bool HasColumn(OleDbDataReader dr, string name)
    {
        for (int i = 0; i < dr.FieldCount; i++)
        {
            if (string.Equals(dr.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static object ReadField(OleDbDataReader dr, params string[] names)
    {
        foreach (string name in names)
        {
            if (HasColumn(dr, name) && dr[name] != DBNull.Value && dr[name] != null)
                return dr[name];
        }
        return null;
    }

    // טעינה וקישור נתונים: LoadEventData
    private void LoadEventData(OleDbDataReader dr, OleDbConnection con)
    {
        object ownerValue = ReadField(dr, "UserId", "CreatedBy");
        int rowUserId = ownerValue != null ? Convert.ToInt32(ownerValue) : 0;

        pnlContent.Visible = true;
        pnlNotFound.Visible = false;

        object title = ReadField(dr, "Title");
        lblTitle.Text = title != null ? title.ToString() : "(No Title)";

        object time = ReadField(dr, "EventTime", "Time");
        lblTime.Text = time != null ? time.ToString() : "-";

        object category = ReadField(dr, "Category");
        lblCategory.Text = category != null ? category.ToString() : "Other";

        object notes = ReadField(dr, "Notes");
        lblNotes.Text = notes != null ? notes.ToString() : "-";

        object eventDate = ReadField(dr, "EventDate", "Date");
        if (eventDate != null)
        {
            lblDate.Text = Convert.ToDateTime(eventDate).ToString("dd/MM/yyyy");
        }
        else
        {
            lblDate.Text = "-";
        }

        string userName = "-";
        if (rowUserId > 0)
        {
            try
            {
                string userNameCol = "UserName";
                if (!ColumnExists(con, "Users", "UserName"))
                {
                    if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                    else if (ColumnExists(con, "Users", "username"))
                        userNameCol = "username";
                }

                string userSql = "SELECT " + userNameCol + " FROM Users WHERE Id = ?";
                using (OleDbCommand userCmd = new OleDbCommand(userSql, con))
                {
                    userCmd.Parameters.AddWithValue("?", rowUserId);
                    object userNameObj = userCmd.ExecuteScalar();
                    if (userNameObj != null && userNameObj != DBNull.Value)
                    {
                        userName = userNameObj.ToString();
                    }
                }
            }
            catch { }
        }
        lblUserName.Text = userName;

        lnkEdit.NavigateUrl = "editEvent.aspx?id=" + eventId;

        LoadEventFiles();
        LoadEventImages();
    }

    // בדיקה אם עמודה קיימת בטבלה
    private bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        cmd.ExecuteScalar();
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }


    // טעינה וקישור נתונים: LoadEventFiles
    private void LoadEventFiles()
    {
        try
        {
            FileService fs = new FileService();
            DataTable dt = fs.GetFilesByEvent(eventId);
            pnlFiles.Visible = dt != null && dt.Rows.Count > 0;
            rptFiles.DataSource = dt;
            rptFiles.DataBind();
        }
        catch { pnlFiles.Visible = false; }
    }

    // טעינה וקישור נתונים: LoadEventImages
    private void LoadEventImages()
    {
        try
        {
            ImageService ims = new ImageService();
            DataTable dt = ims.GetImagesByEvent(eventId);
            pnlImages.Visible = dt != null && dt.Rows.Count > 0;
            rptImages.DataSource = dt;
            rptImages.DataBind();
        }
        catch { pnlImages.Visible = false; }
    }

    // הצגת הודעה כשהפריט לא נמצא
    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }
}

