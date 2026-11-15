using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;

/// <summary>
/// SharedCalendarService - DSD Schema: Static tables, INTEGER types, EventDate/EventTime columns
/// No dynamic table creation - tables must exist in database
/// </summary>
public class SharedCalendarService
{
    // DSD Schema: No dynamic table creation - tables are predefined
    // Constructor no longer creates tables
    // Constructor - tables must exist in database, no dynamic creation
    public SharedCalendarService()
    {
        // Tables must exist in database - no dynamic creation
    }

    // Loads all shared calendars with membership status, admin status, and request status for a user
    public DataTable GetAllSharedCalendars(int? userId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                bool sharedCalendarsTableExists = TableExists(con, "SharedCalendars");
                if (!sharedCalendarsTableExists)
                {
                    return dt;
                }

                if (userId.HasValue)
                {
                    // Check if Permission column exists once at the beginning (for backward compatibility)
                    bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");

                    // Determine correct username column name (handles case variations)
                    string userNameCol = "username";
                    if (!ColumnExists(con, "Users", "username"))
                    {
                        if (ColumnExists(con, "Users", "UserName"))
                            userNameCol = "UserName";
                        else if (ColumnExists(con, "Users", "userName"))
                            userNameCol = "userName";
                    }

                    // Get ALL calendars first (before filtering by membership)
                    string sqlAll = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U." + userNameCol + @" AS CreatorName,
    SC.CreatedDate
FROM SharedCalendars SC
LEFT JOIN Users U ON CInt(SC.CreatedBy) = CInt(U.id)
ORDER BY SC.CreatedDate DESC";

                    using (OleDbCommand cmdAll = new OleDbCommand(sqlAll, con))
                    {
                        using (OleDbDataAdapter daAll = new OleDbDataAdapter(cmdAll))
                        {
                            daAll.Fill(dt);
                        }
                    }

                    // Add all required columns
                    if (!dt.Columns.Contains("IsMember"))
                    {
                        dt.Columns.Add("IsMember", typeof(int));
                    }
                    if (!dt.Columns.Contains("IsAdmin"))
                    {
                        dt.Columns.Add("IsAdmin", typeof(int));
                    }
                    if (!dt.Columns.Contains("RequestStatus"))
                    {
                        dt.Columns.Add("RequestStatus", typeof(string));
                    }
                    if (!dt.Columns.Contains("HasRequestedAccess"))
                    {
                        dt.Columns.Add("HasRequestedAccess", typeof(int));
                    }
                    // Always expose Permission for UI binding (Eval), even if DB column is missing
                    if (!dt.Columns.Contains("Permission"))
                    {
                        dt.Columns.Add("Permission", typeof(string));
                    }

                    // Check membership and admin status for each calendar
                    foreach (DataRow row in dt.Rows)
                    {
                        int calendarId = Convert.ToInt32(row["CalendarId"]);
                        int createdBy = Convert.ToInt32(row["CreatedBy"]);

                        // Check if user is admin (creator of calendar)
                        if (createdBy == userId.Value)
                        {
                            row["IsAdmin"] = 1;
                            row["IsMember"] = 0;
                            row["Permission"] = "ReadWrite";
                        }
                        else
                        {
                            // Check if user is a member of this calendar
                            string checkMemberSql = "SELECT COUNT(*) FROM SharedCalendarMembers WHERE CalendarId = ? AND CInt(UserId) = ?";
                            using (OleDbCommand checkMemberCmd = new OleDbCommand(checkMemberSql, con))
                            {
                                checkMemberCmd.Parameters.AddWithValue("?", calendarId);
                                checkMemberCmd.Parameters.AddWithValue("?", userId.Value);
                                object memberResult = checkMemberCmd.ExecuteScalar();
                                int isMemberCount = (memberResult != null && memberResult != DBNull.Value) ? Convert.ToInt32(memberResult) : 0;

                                if (isMemberCount > 0)
                                {
                                    row["IsAdmin"] = 0;
                                    row["IsMember"] = 1;
                                    // From DB if column exists; otherwise default Read
                                    string permission = hasPermissionColumn
                                        ? GetMemberPermission(con, calendarId, userId.Value)
                                        : "Read";
                                    row["Permission"] = permission ?? "Read";
                                }
                                else
                                {
                                    row["IsAdmin"] = 0;
                                    row["IsMember"] = 0;
                                    row["Permission"] = "";
                                }
                            }
                        }

                        // Get join request status for this calendar
                        string requestStatus = GetRequestStatus(calendarId, userId.Value);
                        row["RequestStatus"] = requestStatus ?? "";
                        row["HasRequestedAccess"] = string.IsNullOrEmpty(requestStatus) ? 0 : 1;
                    }

                    DataView dv = dt.DefaultView;
                    dv.Sort = "CreatedDate DESC";
                    dt = dv.ToTable();
                }
                else
                {
                    string userNameCol = "username";
                    if (!ColumnExists(con, "Users", "username"))
                    {
                        if (ColumnExists(con, "Users", "UserName"))
                            userNameCol = "UserName";
                        else if (ColumnExists(con, "Users", "userName"))
                            userNameCol = "userName";
                    }

                    string sql = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U." + userNameCol + @" AS CreatorName,
    SC.CreatedDate,
    0 AS IsMember,
    0 AS IsAdmin
FROM SharedCalendars SC
LEFT JOIN Users U ON CInt(SC.CreatedBy) = CInt(U.id)
ORDER BY SC.CreatedDate DESC";

                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (dt.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value && row["CalendarName"] != null)
                        row["CalendarName"] = Utilities.FixEncoding(row["CalendarName"].ToString());
                    if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value && row["Description"] != null)
                        row["Description"] = Utilities.FixEncoding(row["Description"].ToString());
                    if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value && row["CreatorName"] != null)
                        row["CreatorName"] = Utilities.FixEncoding(row["CreatorName"].ToString());
                }
            }
        }
        catch
        {
        }

        return dt;
    }

    // Creates a new shared calendar and automatically adds creator as admin member
    public int CreateSharedCalendar(string name, string description, int createdBy)
    {
        string conStr = Connect.GetConnectionString();
        int calendarId = 0;

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // Insert new calendar record (DSD Schema: INTEGER types)
                string sql = "INSERT INTO SharedCalendars (Name, Description, CreatedBy, CreatedDate) VALUES (?, ?, ?, ?)";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    OleDbParameter nameParam = new OleDbParameter("?", OleDbType.WChar);
                    nameParam.Value = (name ?? "").Trim();
                    cmd.Parameters.Add(nameParam);

                    OleDbParameter descriptionParam = new OleDbParameter("?", OleDbType.WChar);
                    descriptionParam.Value = (description ?? "").Trim();
                    cmd.Parameters.Add(descriptionParam);

                    // DSD Schema: INTEGER type (not LONG)
                    OleDbParameter createdByParam = new OleDbParameter("?", OleDbType.Integer);
                    createdByParam.Value = createdBy;
                    cmd.Parameters.Add(createdByParam);

                    OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                    dateParam.Value = DateTime.Now;
                    cmd.Parameters.Add(dateParam);

                    cmd.ExecuteNonQuery();

                    // Get the inserted calendar ID
                    cmd.CommandText = "SELECT @@IDENTITY";
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        calendarId = Convert.ToInt32(result);

                        // Automatically add creator as admin member (DSD Schema requirement)
                        sql = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, JoinedDate) VALUES (?, ?, ?, ?)";
                        cmd.CommandText = sql;
                        cmd.Parameters.Clear();

                        // DSD Schema: INTEGER types
                        OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                        calendarIdParam.Value = calendarId;
                        cmd.Parameters.Add(calendarIdParam);

                        OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                        userIdParam.Value = createdBy;
                        cmd.Parameters.Add(userIdParam);

                        OleDbParameter roleParam = new OleDbParameter("?", OleDbType.WChar);
                        roleParam.Value = "admin";
                        cmd.Parameters.Add(roleParam);

                        OleDbParameter joinedDateParam = new OleDbParameter("?", OleDbType.Date);
                        joinedDateParam.Value = DateTime.Now;
                        cmd.Parameters.Add(joinedDateParam);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        catch
        {
            throw;
        }

        return calendarId;
    }

    // Adds a user as a member to a shared calendar with specified role and permission level
    public void AddMemberToCalendar(int calendarId, int userId, string role = "member", string permission = "Read")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");

                if (hasPermissionColumn)
                {
                    if (permission != "Read" && permission != "ReadWrite")
                        permission = "Read";

                    string sql = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, Permission, JoinedDate) VALUES (?, ?, ?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                        calendarIdParam.Value = calendarId;
                        cmd.Parameters.Add(calendarIdParam);

                        OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                        userIdParam.Value = userId;
                        cmd.Parameters.Add(userIdParam);

                        OleDbParameter roleParam = new OleDbParameter("?", OleDbType.WChar);
                        roleParam.Value = role != null ? role.Trim() : "member";
                        cmd.Parameters.Add(roleParam);

                        OleDbParameter permissionParam = new OleDbParameter("?", OleDbType.WChar);
                        permissionParam.Value = permission;
                        cmd.Parameters.Add(permissionParam);

                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string sql = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, JoinedDate) VALUES (?, ?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                        calendarIdParam.Value = calendarId;
                        cmd.Parameters.Add(calendarIdParam);

                        OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                        userIdParam.Value = userId;
                        cmd.Parameters.Add(userIdParam);

                        OleDbParameter roleParam = new OleDbParameter("?", OleDbType.WChar);
                        roleParam.Value = role != null ? role.Trim() : "member";
                        cmd.Parameters.Add(roleParam);

                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        catch
        {
            throw;
        }
    }

    // Creates a join request for a user to join a shared calendar
    public void CreateJoinRequest(int calendarId, int userId, string message = "")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "INSERT INTO JoinRequests (CalendarId, UserId, Status, RequestDate, Message) VALUES (?, ?, ?, ?, ?)";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);

                    OleDbParameter statusParam = new OleDbParameter("?", OleDbType.WChar);
                    statusParam.Value = "pending";
                    cmd.Parameters.Add(statusParam);

                    cmd.Parameters.AddWithValue("?", DateTime.Now);

                    OleDbParameter messageParam = new OleDbParameter("?", OleDbType.WChar);
                    messageParam.Value = (message ?? "").Trim();
                    cmd.Parameters.Add(messageParam);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch
        {
            throw;
        }
    }

    // שליפת נתונים: GetJoinRequests
    public DataTable GetJoinRequests(int calendarId, int? adminUserId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string userNameCol = "username";
                if (!ColumnExists(con, "Users", "username"))
                {
                    if (ColumnExists(con, "Users", "UserName"))
                        userNameCol = "UserName";
                    else if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                }

                string firstNameCol = "firstName";
                if (!ColumnExists(con, "Users", "firstName"))
                {
                    if (ColumnExists(con, "Users", "FirstName"))
                        firstNameCol = "FirstName";
                }

                string lastNameCol = "lastName";
                if (!ColumnExists(con, "Users", "lastName"))
                {
                    if (ColumnExists(con, "Users", "LastName"))
                        lastNameCol = "LastName";
                }

                string sql = @"
SELECT 
    JR.Id AS RequestId,
    JR.CalendarId,
    JR.UserId,
    U." + userNameCol + @" AS UserName,
    U." + firstNameCol + @" AS firstName,
    U." + lastNameCol + @" AS lastName,
    JR.Status,
    JR.RequestDate,
    JR.Message
FROM JoinRequests JR
LEFT JOIN Users U ON CInt(JR.UserId) = CInt(U.id)
WHERE JR.CalendarId = ? AND JR.Status = 'pending'";

                if (adminUserId.HasValue)
                {
                    sql += " AND EXISTS (SELECT 1 FROM SharedCalendars SC WHERE SC.Id = ? AND CInt(SC.CreatedBy) = ?)";
                }

                sql += " ORDER BY JR.RequestDate DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    if (adminUserId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("?", calendarId);
                        cmd.Parameters.AddWithValue("?", adminUserId.Value);
                    }

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (dt.Columns.Contains("UserName") && row["UserName"] != DBNull.Value && row["UserName"] != null)
                        row["UserName"] = Utilities.FixEncoding(row["UserName"].ToString());
                    if (dt.Columns.Contains("firstName") && row["firstName"] != DBNull.Value && row["firstName"] != null)
                        row["firstName"] = Utilities.FixEncoding(row["firstName"].ToString());
                    if (dt.Columns.Contains("FirstName") && row["FirstName"] != DBNull.Value && row["FirstName"] != null)
                        row["FirstName"] = Utilities.FixEncoding(row["FirstName"].ToString());
                    if (dt.Columns.Contains("lastName") && row["lastName"] != DBNull.Value && row["lastName"] != null)
                        row["lastName"] = Utilities.FixEncoding(row["lastName"].ToString());
                    if (dt.Columns.Contains("LastName") && row["LastName"] != DBNull.Value && row["LastName"] != null)
                        row["LastName"] = Utilities.FixEncoding(row["LastName"].ToString());
                    if (dt.Columns.Contains("Message") && row["Message"] != DBNull.Value && row["Message"] != null)
                        row["Message"] = Utilities.FixEncoding(row["Message"].ToString());
                }
            }
        }
        catch
        {
        }

        return dt;
    }

    // Approves a join request and adds the user to the calendar with specified permission level
    public void ApproveJoinRequest(int requestId, int calendarId, int userId, string permission = "Read")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "UPDATE JoinRequests SET Status = 'approved' WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", requestId);
                    cmd.ExecuteNonQuery();
                }

                AddMemberToCalendar(calendarId, userId, "member", permission);
            }
        }
        catch
        {
            throw;
        }
    }

    // דחיית בקשת הצטרפות
    public void RejectJoinRequest(int requestId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "UPDATE JoinRequests SET Status = 'rejected' WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", requestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch
        {
            throw;
        }
    }

    // שליפת כל אירועי טבלה משותפת
    public DataTable GetSharedCalendarEvents(int calendarId, int? userId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                bool tableExists = TableExists(con, "SharedCalendarEvents");
                if (!tableExists)
                {
                    dt.Columns.Add("Id", typeof(int));
                    dt.Columns.Add("Title", typeof(string));
                    dt.Columns.Add("EventDate", typeof(DateTime));
                    dt.Columns.Add("EventTime", typeof(string));
                    dt.Columns.Add("Category", typeof(string));
                    dt.Columns.Add("Notes", typeof(string));
                    dt.Columns.Add("CreatedByName", typeof(string));
                    return dt;
                }

                string userNameCol = "UserName";
                if (!ColumnExists(con, "Users", "UserName"))
                {
                    if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                    else if (ColumnExists(con, "Users", "username"))
                        userNameCol = "username";
                }

                string[] dateColumnNames = { "EventDate", "Date", "eventDate", "date" };
                string[] timeColumnNames = { "EventTime", "Time", "eventTime", "time" };

                string dateColumnName = null;
                string timeColumnName = null;

                foreach (string colName in dateColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        dateColumnName = colName;
                        break;
                    }
                }

                foreach (string colName in timeColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        timeColumnName = colName;
                        break;
                    }
                }

                string dateSelect = dateColumnName != null ? "SCE.[" + dateColumnName + "] AS EventDate" : "NULL AS EventDate";
                string timeSelect = timeColumnName != null ? "SCE.[" + timeColumnName + "] AS EventTime" : "NULL AS EventTime";
                string orderBy = dateColumnName != null ? "ORDER BY SCE.[" + dateColumnName + "] DESC" : "ORDER BY SCE.Id DESC";
                if (timeColumnName != null && dateColumnName != null)
                {
                    orderBy = "ORDER BY SCE.[" + dateColumnName + "] DESC, SCE.[" + timeColumnName + "] DESC";
                }

                bool hasAssignee = ColumnExists(con, "SharedCalendarEvents", "AssigneeUserId");
                bool hasStatus = ColumnExists(con, "SharedCalendarEvents", "Status");
                bool hasReminder = ColumnExists(con, "SharedCalendarEvents", "ReminderMinutes");
                string extrasSelect = "";
                if (hasAssignee) extrasSelect += ", SCE.AssigneeUserId";
                if (hasStatus) extrasSelect += ", SCE.Status";
                if (hasReminder) extrasSelect += ", SCE.ReminderMinutes";

                string sql = @"
SELECT
    SCE.Id,
    SCE.CalendarId,
    SCE.Title,
    " + dateSelect + @",
    " + timeSelect + @",
    SCE.Notes,
    SCE.Category,
    SCE.CreatedBy,
    U." + userNameCol + @" AS CreatedByName,
    SCE.CreatedDate
    " + extrasSelect + @"
FROM SharedCalendarEvents SCE
LEFT JOIN Users U ON CInt(SCE.CreatedBy) = CInt(U.id)
WHERE SCE.CalendarId = ?
" + orderBy;

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                    calendarIdParam.Value = calendarId;
                    cmd.Parameters.Add(calendarIdParam);

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                if (dt.Columns.Count == 0)
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
                    foreach (DataRow row in dt.Rows)
                    {
                        if (dt.Columns.Contains("Title") && row["Title"] != DBNull.Value && row["Title"] != null)
                            row["Title"] = Utilities.FixEncoding(row["Title"].ToString());
                        if (dt.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                            row["EventTime"] = Utilities.FixEncoding(row["EventTime"].ToString());
                        if (dt.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
                            row["Notes"] = Utilities.FixEncoding(row["Notes"].ToString());
                        if (dt.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
                            row["Category"] = Utilities.FixEncoding(row["Category"].ToString());
                        if (dt.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                            row["CreatedByName"] = Utilities.FixEncoding(row["CreatedByName"].ToString());
                    }
                }
            }
        }
        catch
        {
            if (dt.Columns.Count == 0)
            {
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Title", typeof(string));
                dt.Columns.Add("EventDate", typeof(DateTime));
                dt.Columns.Add("EventTime", typeof(string));
                dt.Columns.Add("Category", typeof(string));
                dt.Columns.Add("Notes", typeof(string));
                dt.Columns.Add("CreatedByName", typeof(string));
            }
        }

        return dt;
    }

    // הוספת אירוע לטבלה משותפת — מחזיר Id של האירוע החדש (או 0)
    public int AddSharedCalendarEvent(int calendarId, string title, DateTime date, string time, string notes, string category, int createdBy, int reminderMinutes = 60, int? assigneeUserId = null, string status = "Pending")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }

            string cleanTitle = Utilities.FixEncoding((title ?? "").Trim());
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = Utilities.FixEncoding((time ?? "").Trim());
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = Utilities.FixEncoding((notes ?? "").Trim());
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = Utilities.FixEncoding((category ?? "אחר").Trim());
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

            if (string.IsNullOrWhiteSpace(status)) status = "Pending";

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string[] dateColumnNames = { "EventDate", "Date", "eventDate", "date" };
                string[] timeColumnNames = { "EventTime", "Time", "eventTime", "time" };

                string dateColumnName = "EventDate";
                string timeColumnName = "EventTime";

                foreach (string colName in dateColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        dateColumnName = colName;
                        break;
                    }
                }

                foreach (string colName in timeColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        timeColumnName = colName;
                        break;
                    }
                }

                int newEventId = 0;
                try
                {
                    string sql = "INSERT INTO SharedCalendarEvents (CalendarId, Title, [" + dateColumnName + "], [" + timeColumnName + "], Notes, Category, CreatedBy, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                        calendarIdParam.Value = calendarId;
                        cmd.Parameters.Add(calendarIdParam);

                        OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
                        titleParam.Value = cleanTitle;
                        cmd.Parameters.Add(titleParam);

                        OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                        dateParam.Value = date;
                        cmd.Parameters.Add(dateParam);

                        OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                        timeParam.Value = cleanTime;
                        cmd.Parameters.Add(timeParam);

                        OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                        notesParam.Value = cleanNotes;
                        cmd.Parameters.Add(notesParam);

                        OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                        categoryParam.Value = cleanCategory;
                        cmd.Parameters.Add(categoryParam);

                        OleDbParameter createdByParam = new OleDbParameter("?", OleDbType.Integer);
                        createdByParam.Value = createdBy;
                        cmd.Parameters.Add(createdByParam);

                        OleDbParameter createdDateParam = new OleDbParameter("?", OleDbType.Date);
                        createdDateParam.Value = DateTime.Now;
                        cmd.Parameters.Add(createdDateParam);

                        cmd.ExecuteNonQuery();
                    }

                    using (OleDbCommand idCmd = new OleDbCommand("SELECT @@IDENTITY", con))
                    {
                        object idObj = idCmd.ExecuteScalar();
                        if (idObj != null && idObj != DBNull.Value)
                            newEventId = Convert.ToInt32(idObj);
                    }
                }
                catch (System.Data.OleDb.OleDbException ex)
                {
                    if (ex.Message.Contains("unknown field name"))
                    {
                        string sql = "INSERT INTO SharedCalendarEvents (CalendarId, Title, Notes, Category, CreatedBy, CreatedDate) VALUES (?, ?, ?, ?, ?, ?)";
                        using (OleDbCommand cmd = new OleDbCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("?", calendarId);
                            cmd.Parameters.AddWithValue("?", cleanTitle);
                            cmd.Parameters.AddWithValue("?", cleanNotes);
                            cmd.Parameters.AddWithValue("?", cleanCategory);
                            cmd.Parameters.AddWithValue("?", createdBy);
                            cmd.Parameters.AddWithValue("?", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                        using (OleDbCommand idCmd = new OleDbCommand("SELECT @@IDENTITY", con))
                        {
                            object idObj = idCmd.ExecuteScalar();
                            if (idObj != null && idObj != DBNull.Value)
                                newEventId = Convert.ToInt32(idObj);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                if (newEventId > 0)
                {
                    ApplyEventExtras(con, newEventId, reminderMinutes, assigneeUserId, status);
                    NotifyAssignee(calendarId, newEventId, cleanTitle, assigneeUserId, createdBy);
                }

                return newEventId;
            }
        }
        catch
        {
            throw;
        }
    }

    // עדכון אירוע בטבלה משותפת
    public void UpdateSharedCalendarEvent(int eventId, string title, DateTime date, string time, string notes, string category, int reminderMinutes = 60, int? assigneeUserId = null, string status = "Pending")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }

            string cleanTitle = Utilities.FixEncoding((title ?? "").Trim());
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = Utilities.FixEncoding((time ?? "").Trim());
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = Utilities.FixEncoding((notes ?? "").Trim());
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = Utilities.FixEncoding((category ?? "אחר").Trim());
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

            if (string.IsNullOrWhiteSpace(status)) status = "Pending";

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string[] dateColumnNames = { "EventDate", "Date", "eventDate", "date" };
                string[] timeColumnNames = { "EventTime", "Time", "eventTime", "time" };

                string dateColumnName = "EventDate";
                string timeColumnName = "EventTime";

                foreach (string colName in dateColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        dateColumnName = colName;
                        break;
                    }
                }

                foreach (string colName in timeColumnNames)
                {
                    if (ColumnExists(con, "SharedCalendarEvents", colName))
                    {
                        timeColumnName = colName;
                        break;
                    }
                }

                try
                {
                    string sql = "UPDATE SharedCalendarEvents SET Title = ?, [" + dateColumnName + "] = ?, [" + timeColumnName + "] = ?, Notes = ?, Category = ? WHERE Id = ?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
                        titleParam.Value = cleanTitle;
                        cmd.Parameters.Add(titleParam);

                        OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                        dateParam.Value = date;
                        cmd.Parameters.Add(dateParam);

                        OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                        timeParam.Value = cleanTime;
                        cmd.Parameters.Add(timeParam);

                        OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                        notesParam.Value = cleanNotes;
                        cmd.Parameters.Add(notesParam);

                        OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                        categoryParam.Value = cleanCategory;
                        cmd.Parameters.Add(categoryParam);

                        OleDbParameter eventIdParam = new OleDbParameter("?", OleDbType.Integer);
                        eventIdParam.Value = eventId;
                        cmd.Parameters.Add(eventIdParam);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (System.Data.OleDb.OleDbException ex)
                {
                    if (ex.Message.Contains("unknown field name"))
                    {
                        string sql = "UPDATE SharedCalendarEvents SET Title = ?, Notes = ?, Category = ? WHERE Id = ?";
                        using (OleDbCommand cmd = new OleDbCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("?", cleanTitle);
                            cmd.Parameters.AddWithValue("?", cleanNotes);
                            cmd.Parameters.AddWithValue("?", cleanCategory);
                            cmd.Parameters.AddWithValue("?", eventId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                ApplyEventExtras(con, eventId, reminderMinutes, assigneeUserId, status);

                int calendarId = 0;
                using (OleDbCommand c = new OleDbCommand("SELECT CalendarId FROM SharedCalendarEvents WHERE Id=?", con))
                {
                    c.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                    object o = c.ExecuteScalar();
                    if (o != null && o != DBNull.Value) calendarId = Convert.ToInt32(o);
                }
                NotifyAssignee(calendarId, eventId, cleanTitle, assigneeUserId, 0);
            }
        }
        catch
        {
            throw;
        }
    }

    private void ApplyEventExtras(OleDbConnection con, int eventId, int reminderMinutes, int? assigneeUserId, string status)
    {
        try
        {
            // Access YESNO: use parameter, not False literal
            string sql = "UPDATE SharedCalendarEvents SET ReminderMinutes=?, ReminderSent=?, Status=?, AssigneeUserId=? WHERE Id=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = reminderMinutes });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Boolean) { Value = false });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = status ?? "Pending" });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer)
                {
                    Value = assigneeUserId.HasValue && assigneeUserId.Value > 0
                        ? (object)assigneeUserId.Value
                        : DBNull.Value
                });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                int n = cmd.ExecuteNonQuery();
                LoggingService.Log("SharedCalendarService",
                    string.Format("ApplyEventExtras eventId={0} rows={1} rem={2} assignee={3} status={4}",
                        eventId, n, reminderMinutes,
                        assigneeUserId.HasValue ? assigneeUserId.Value.ToString() : "null",
                        status));
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "ApplyEventExtras failed", ex);
            // Fallback column-by-column
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "UPDATE SharedCalendarEvents SET ReminderMinutes=? WHERE Id=?", con))
                {
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = reminderMinutes });
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "UPDATE SharedCalendarEvents SET Status=? WHERE Id=?", con))
                {
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = status ?? "Pending" });
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "UPDATE SharedCalendarEvents SET AssigneeUserId=? WHERE Id=?", con))
                {
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer)
                    {
                        Value = assigneeUserId.HasValue && assigneeUserId.Value > 0
                            ? (object)assigneeUserId.Value
                            : DBNull.Value
                    });
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }

    private void NotifyAssignee(int calendarId, int eventId, string title, int? assigneeUserId, int createdBy)
    {
        if (!assigneeUserId.HasValue || assigneeUserId.Value <= 0) return;
        if (assigneeUserId.Value == createdBy) return;
        try
        {
            string link = "sharedCalendarDetails.aspx?id=" + calendarId;
            NotificationService.Create(assigneeUserId.Value, "הוקצתה לך משימה",
                "הוקצה לך האירוע: " + (title ?? ""), link, eventId);
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "NotifyAssignee failed", ex);
        }
    }

    // רשימת חברי טבלה משותפת (לבחירת אחראי + צבע + הרשאה)
    public DataTable GetCalendarMembers(int calendarId)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("UserId", typeof(int));
        dt.Columns.Add("UserName", typeof(string));
        dt.Columns.Add("MemberColor", typeof(string));
        dt.Columns.Add("Permission", typeof(string));
        dt.Columns.Add("Role", typeof(string));

        string conStr = Connect.GetConnectionString();
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            EnsureMemberColors(calendarId);

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                string userCol = "username";
                if (ColumnExists(con, "Users", "UserName")) userCol = "UserName";
                else if (ColumnExists(con, "Users", "userName")) userCol = "userName";

                string idCol = ColumnExists(con, "Users", "Id") ? "Id" : "id";
                bool hasColor = ColumnExists(con, "SharedCalendarMembers", "MemberColor");
                bool hasPerm = ColumnExists(con, "SharedCalendarMembers", "Permission");
                bool hasRole = ColumnExists(con, "SharedCalendarMembers", "Role");

                string colorSelect = hasColor ? ", M.MemberColor" : ", '' AS MemberColor";
                string permSelect = hasPerm ? ", M.Permission" : ", 'Read' AS Permission";
                string roleSelect = hasRole ? ", M.Role" : ", '' AS Role";

                string sql = string.Format(@"
                    SELECT U.[{0}] AS UserId, U.[{1}] AS UserName{2}{3}{4}
                    FROM SharedCalendarMembers M
                    INNER JOIN Users U ON CInt(M.UserId)=CInt(U.[{0}])
                    WHERE M.CalendarId=?
                    ORDER BY U.[{1}]", idCol, userCol, colorSelect, permSelect, roleSelect);

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                        adp.Fill(dt);
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (row["UserName"] != DBNull.Value)
                        row["UserName"] = Utilities.FixEncoding(Convert.ToString(row["UserName"]));
                    string color = Convert.ToString(row["MemberColor"]);
                    if (string.IsNullOrWhiteSpace(color))
                        row["MemberColor"] = GetDefaultColorForUser(Convert.ToInt32(row["UserId"]));
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "GetCalendarMembers failed", ex);
        }
        return dt;
    }

    // מחיקת אירוע מטבלה משותפת
    public void DeleteSharedCalendarEvent(int eventId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                try
                {
                    using (OleDbCommand delVals = new OleDbCommand(
                        "DELETE FROM SharedCalendarEventFieldValues WHERE EventId=?", con))
                    {
                        delVals.Parameters.AddWithValue("?", eventId);
                        delVals.ExecuteNonQuery();
                    }
                }
                catch { }

                string sql = "DELETE FROM SharedCalendarEvents WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", eventId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch
        {
            throw;
        }
    }


    // בדיקה אם המשתמש הוא יוצר/מנהל הטבלה
    public bool IsCalendarAdmin(int calendarId, int userId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "SELECT COUNT(*) FROM SharedCalendars WHERE Id = ? AND CInt(CreatedBy) = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);

                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    // בדיקה אם המשתמש חבר בטבלה
    public bool IsCalendarMember(int calendarId, int userId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "SELECT COUNT(*) FROM SharedCalendarMembers WHERE CalendarId = ? AND CInt(UserId) = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);

                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    // שליפת פרטי טבלה משותפת לפי מזהה
    public DataRow GetSharedCalendar(int calendarId)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string userNameCol = "username";
                if (!ColumnExists(con, "Users", "username"))
                {
                    if (ColumnExists(con, "Users", "UserName"))
                        userNameCol = "UserName";
                    else if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                }

                string sql = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U." + userNameCol + @" AS CreatorName,
    SC.CreatedDate
FROM SharedCalendars SC
LEFT JOIN Users U ON CInt(SC.CreatedBy) = CInt(U.id)
WHERE SC.Id = ?
ORDER BY SC.Id";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                    calendarIdParam.Value = calendarId;
                    cmd.Parameters.Add(calendarIdParam);

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    if (dt.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value && row["CalendarName"] != null)
                        row["CalendarName"] = Utilities.FixEncoding(row["CalendarName"].ToString());
                    if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value && row["Description"] != null)
                        row["Description"] = Utilities.FixEncoding(row["Description"].ToString());
                    if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value && row["CreatorName"] != null)
                        row["CreatorName"] = Utilities.FixEncoding(row["CreatorName"].ToString());
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                LoggingService.Log("SharedCalendarService",
                    "{\"location\":\"SharedCalendarService.GetSharedCalendar:ERROR\",\"message\":\"Error getting shared calendar\",\"data\":{\"calendarId\":\"" + calendarId + "\",\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

        if (dt.Rows.Count > 0)
            return dt.Rows[0];

        return null;
    }

    // בקשת גישה לטבלה משותפת
    public bool RequestAccess(int calendarId, int userId, string message = "")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // Check if JoinRequests table exists
                bool tableExists = TableExists(con, "JoinRequests");
                if (!tableExists)
                {
                    // Try to create the table
                    try
                    {
                        string createSql = @"
CREATE TABLE JoinRequests (
    Id AUTOINCREMENT PRIMARY KEY,
    CalendarId INTEGER,
    UserId INTEGER,
    Status TEXT(50),
    RequestDate DATETIME,
    Message MEMO
)";
                        using (OleDbCommand createCmd = new OleDbCommand(createSql, con))
                        {
                            createCmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception createEx)
                    {
                        try
                        {
                            LoggingService.Log("SharedCalendarService",
                                "{\"location\":\"SharedCalendarService.RequestAccess:CREATE_TABLE_ERROR\",\"message\":\"Failed to create JoinRequests table\",\"data\":{\"error\":\"" + createEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                        }
                        catch { }
                        return false;
                    }
                }

                // Verify calendar exists
                string checkCalendarSql = "SELECT COUNT(*) FROM SharedCalendars WHERE Id = ?";
                using (OleDbCommand checkCmd = new OleDbCommand(checkCalendarSql, con))
                {
                    OleDbParameter checkParam = new OleDbParameter("?", OleDbType.Integer);
                    checkParam.Value = calendarId;
                    checkCmd.Parameters.Add(checkParam);
                    object calendarExists = checkCmd.ExecuteScalar();
                    if (calendarExists == null || Convert.ToInt32(calendarExists) == 0)
                    {
                        return false;
                    }
                }

                // Allow multiple requests - just insert a new one
                // The system will show only the latest request status
                string sql = "INSERT INTO JoinRequests (CalendarId, UserId, Status, RequestDate, Message) VALUES (?, ?, ?, ?, ?)";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    OleDbParameter calendarIdParam = new OleDbParameter("?", OleDbType.Integer);
                    calendarIdParam.Value = calendarId;
                    cmd.Parameters.Add(calendarIdParam);

                    OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                    userIdParam.Value = userId;
                    cmd.Parameters.Add(userIdParam);

                    OleDbParameter statusParam = new OleDbParameter("?", OleDbType.WChar);
                    statusParam.Value = "Pending";
                    cmd.Parameters.Add(statusParam);

                    OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                    dateParam.Value = DateTime.Now;
                    cmd.Parameters.Add(dateParam);

                    OleDbParameter messageParam = new OleDbParameter("?", OleDbType.WChar);
                    messageParam.Value = (message ?? "").Trim();
                    cmd.Parameters.Add(messageParam);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        catch
        {
            return false;
        }
    }

    // בדיקה אם טבלה קיימת במסד
    private bool TableExists(OleDbConnection conn, string tableName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM [" + tableName + "]", conn))
            {
                cmd.ExecuteScalar();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    // שליפת נתונים: GetAccessRequests
    public DataTable GetAccessRequests(int calendarId, int? approverUserId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string userNameCol = "UserName";
                if (!ColumnExists(con, "Users", "UserName"))
                {
                    if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                    else if (ColumnExists(con, "Users", "username"))
                        userNameCol = "username";
                }

                string emailCol = "Email";
                if (!ColumnExists(con, "Users", "Email"))
                {
                    if (ColumnExists(con, "Users", "email"))
                        emailCol = "email";
                }

                string sql = @"
SELECT 
    JR.Id AS RequestId,
    JR.CalendarId,
    JR.UserId,
    JR.Status,
    JR.RequestDate,
    JR.Message,
    U." + userNameCol + @" AS RequesterName,
    U." + emailCol + @" AS RequesterEmail,
    SC.Name AS CalendarName,
    SC.CreatedBy
FROM JoinRequests JR
INNER JOIN Users U ON CInt(JR.UserId) = CInt(U.Id)
INNER JOIN SharedCalendars SC ON JR.CalendarId = SC.Id
WHERE JR.CalendarId = ? AND JR.Status = 'Pending'";

                if (approverUserId.HasValue)
                {
                    sql += " AND (SC.CreatedBy = ? OR ? IN (SELECT Id FROM Users WHERE Role = 'owner'))";
                }

                sql += " ORDER BY JR.RequestDate DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    if (approverUserId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("?", approverUserId.Value);
                        cmd.Parameters.AddWithValue("?", approverUserId.Value);
                    }

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                LoggingService.Log("SharedCalendarService",
                    "{\"location\":\"SharedCalendarService.GetAccessRequests:ERROR\",\"message\":\"Error getting access requests\",\"data\":{\"calendarId\":\"" + calendarId + "\",\"approverUserId\":\"" + (approverUserId != null ? approverUserId.ToString() : "null") + "\",\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

        return dt;
    }

    // פונקציה: ApproveRequest
    public bool ApproveRequest(int requestId, int approverUserId, string permission = "Read")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string getRequestSql = @"
SELECT JR.CalendarId, JR.UserId, SC.CreatedBy
FROM JoinRequests JR
INNER JOIN SharedCalendars SC ON JR.CalendarId = SC.Id
WHERE JR.Id = ?";

                int calendarId = 0;
                int userId = 0;
                int createdBy = 0;

                using (OleDbCommand getCmd = new OleDbCommand(getRequestSql, con))
                {
                    getCmd.Parameters.AddWithValue("?", requestId);
                    using (OleDbDataReader dr = getCmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            calendarId = Convert.ToInt32(dr["CalendarId"]);
                            userId = Convert.ToInt32(dr["UserId"]);
                            createdBy = Convert.ToInt32(dr["CreatedBy"]);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                bool canApprove = (createdBy == approverUserId) || IsOwner(approverUserId);
                if (!canApprove)
                {
                    return false;
                }

                // Validate permission value
                if (permission != "Read" && permission != "Write" && permission != "ReadWrite")
                {
                    permission = "Read";
                }

                string updateSql = "UPDATE JoinRequests SET Status = 'Approved' WHERE Id = ?";
                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, con))
                {
                    updateCmd.Parameters.AddWithValue("?", requestId);
                    updateCmd.ExecuteNonQuery();
                }

                string checkMemberSql = "SELECT COUNT(*) FROM SharedCalendarMembers WHERE CalendarId = ? AND CInt(UserId) = ?";
                using (OleDbCommand checkCmd = new OleDbCommand(checkMemberSql, con))
                {
                    checkCmd.Parameters.AddWithValue("?", calendarId);
                    checkCmd.Parameters.AddWithValue("?", userId);
                    object result = checkCmd.ExecuteScalar();
                    if (result == null || Convert.ToInt32(result) == 0)
                    {
                        // Check if Permission column exists
                        bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");

                        if (hasPermissionColumn)
                        {
                            string insertMemberSql = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, Permission, JoinedDate) VALUES (?, ?, ?, ?, ?)";
                            using (OleDbCommand insertCmd = new OleDbCommand(insertMemberSql, con))
                            {
                                insertCmd.Parameters.AddWithValue("?", calendarId);
                                insertCmd.Parameters.AddWithValue("?", userId);
                                insertCmd.Parameters.AddWithValue("?", "Member");
                                insertCmd.Parameters.AddWithValue("?", permission);
                                insertCmd.Parameters.AddWithValue("?", DateTime.Now);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Fallback to old schema without Permission column
                            string insertMemberSql = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, JoinedDate) VALUES (?, ?, ?, ?)";
                            using (OleDbCommand insertCmd = new OleDbCommand(insertMemberSql, con))
                            {
                                insertCmd.Parameters.AddWithValue("?", calendarId);
                                insertCmd.Parameters.AddWithValue("?", userId);
                                insertCmd.Parameters.AddWithValue("?", "Member");
                                insertCmd.Parameters.AddWithValue("?", DateTime.Now);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // Update existing member's permission if column exists
                        bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");
                        if (hasPermissionColumn)
                        {
                            string updatePermissionSql = "UPDATE SharedCalendarMembers SET Permission = ? WHERE CalendarId = ? AND CInt(UserId) = ?";
                            using (OleDbCommand updatePermissionCmd = new OleDbCommand(updatePermissionSql, con))
                            {
                                updatePermissionCmd.Parameters.AddWithValue("?", permission);
                                updatePermissionCmd.Parameters.AddWithValue("?", calendarId);
                                updatePermissionCmd.Parameters.AddWithValue("?", userId);
                                updatePermissionCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    // בדיקה אם עמודה קיימת בטבלה
    private bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return false;

            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper() };
            if (columnName.Length > 0)
            {
                variations = new string[] { columnName, columnName.ToLower(), columnName.ToUpper(),
                                           char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            }

            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        return true;
                    }
                }
                catch (System.Data.OleDb.OleDbException)
                {
                    continue;
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

    // פונקציה: RejectRequest
    public bool RejectRequest(int requestId, int approverUserId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string getRequestSql = @"
SELECT SC.CreatedBy
FROM JoinRequests JR
INNER JOIN SharedCalendars SC ON JR.CalendarId = SC.Id
WHERE JR.Id = ?";

                int createdBy = 0;

                using (OleDbCommand getCmd = new OleDbCommand(getRequestSql, con))
                {
                    getCmd.Parameters.AddWithValue("?", requestId);
                    using (OleDbDataReader dr = getCmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            createdBy = Convert.ToInt32(dr["CreatedBy"]);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                bool canReject = (createdBy == approverUserId) || IsOwner(approverUserId);
                if (!canReject)
                {
                    return false;
                }

                string updateSql = "UPDATE JoinRequests SET Status = 'Rejected' WHERE Id = ?";
                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, con))
                {
                    updateCmd.Parameters.AddWithValue("?", requestId);
                    updateCmd.ExecuteNonQuery();
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    // בדיקה אם המשתמש המחובר הוא בעלים (owner)
    private bool IsOwner(int userId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                // Check both Role and role columns for compatibility
                string sql = "SELECT COUNT(*) FROM Users WHERE Id = ? AND (Role = 'owner' OR Role = 'Owner' OR role = 'owner' OR role = 'Owner')";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", userId);
                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    // החזרת סטטוס בקשת הצטרפות
    public string GetRequestStatus(int calendarId, int userId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                string sql = "SELECT TOP 1 Status FROM JoinRequests WHERE CalendarId = ? AND CInt(UserId) = ? ORDER BY RequestDate DESC";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return result.ToString();
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    // החזרת רמת הרשאה של חבר בטבלה
    private string GetMemberPermission(OleDbConnection con, int calendarId, int userId)
    {
        try
        {
            bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");
            if (hasPermissionColumn)
            {
                string sql = "SELECT TOP 1 Permission FROM SharedCalendarMembers WHERE CalendarId = ? AND CInt(UserId) = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                }
            }
        }
        catch
        {
        }
        return "Read";
    }

    // Gets a user's permission level (Read or ReadWrite) for a shared calendar
    public string GetUserPermission(int calendarId, int userId)
    {
        if (IsOwner(userId))
            return "ReadWrite";

        string conStr = Connect.GetConnectionString();
        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                DataRow calendar = GetSharedCalendar(calendarId);
                if (calendar != null)
                {
                    int createdBy = Convert.ToInt32(calendar["CreatedBy"]);
                    if (createdBy == userId)
                    {
                        return "ReadWrite";
                    }
                }
                return GetMemberPermission(con, calendarId, userId);
            }
        }
        catch
        {
            return "Read";
        }
    }

    // Checks if a user has permission to edit events in a shared calendar (ReadWrite permission or admin/owner)
    public bool CanUserEdit(int calendarId, int userId)
    {
        string permission = GetUserPermission(calendarId, userId);
        return permission == "ReadWrite" || IsCalendarAdmin(calendarId, userId) || IsOwner(userId);
    }

    // Gets all pending join requests for calendars where the user is the creator/admin
    public DataTable GetPendingRequestsForUser(int userId)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string userNameCol = "username";
                if (!ColumnExists(con, "Users", "username"))
                {
                    if (ColumnExists(con, "Users", "UserName"))
                        userNameCol = "UserName";
                    else if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                }

                string sql = @"
SELECT 
    JR.Id AS RequestId,
    JR.CalendarId,
    JR.UserId,
    JR.Status,
    JR.RequestDate,
    JR.Message,
    SC.Name AS CalendarName
FROM JoinRequests JR
INNER JOIN SharedCalendars SC ON JR.CalendarId = SC.Id
WHERE JR.UserId = ? AND JR.Status = 'pending'
ORDER BY JR.RequestDate DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", userId);
                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (row.Table.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value && row["CalendarName"] != null)
                        row["CalendarName"] = Utilities.FixEncoding(row["CalendarName"].ToString());
                }
            }
        }
        catch
        {
        }

        return dt;
    }

    // פלטת צבעים קבועה — צבע שונה לכל משתמש לפי מזהה
    private static readonly string[] MemberColorPalette = new string[]
    {
        "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", "#9B59B6",
        "#1ABC9C", "#E67E22", "#34495E", "#16A085", "#C0392B",
        "#2980B9", "#27AE60", "#D35400", "#8E44AD", "#2C3E50"
    };

    public static string GetDefaultColorForUser(int userId)
    {
        int idx = Math.Abs(userId) % MemberColorPalette.Length;
        return MemberColorPalette[idx];
    }

    public string GetMemberColor(int calendarId, int userId)
    {
        if (userId <= 0) return MemberColorPalette[0];
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!ColumnExists(con, "SharedCalendarMembers", "MemberColor"))
                    return GetDefaultColorForUser(userId);

                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT MemberColor FROM SharedCalendarMembers WHERE CalendarId=? AND CInt(UserId)=?", con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    cmd.Parameters.AddWithValue("?", userId);
                    object result = cmd.ExecuteScalar();
                    string color = result != null && result != DBNull.Value ? Convert.ToString(result).Trim() : "";
                    if (!string.IsNullOrEmpty(color)) return color;
                }
            }
        }
        catch { }
        return GetDefaultColorForUser(userId);
    }

    public void EnsureMemberColors(int calendarId)
    {
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!ColumnExists(con, "SharedCalendarMembers", "MemberColor")) return;

                DataTable members = new DataTable();
                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT Id, UserId, MemberColor FROM SharedCalendarMembers WHERE CalendarId=?", con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                        adp.Fill(members);
                }

                foreach (DataRow row in members.Rows)
                {
                    string existing = row["MemberColor"] == DBNull.Value ? "" : Convert.ToString(row["MemberColor"]).Trim();
                    if (!string.IsNullOrEmpty(existing)) continue;
                    int uid = Convert.ToInt32(row["UserId"]);
                    string color = GetDefaultColorForUser(uid);
                    using (OleDbCommand upd = new OleDbCommand(
                        "UPDATE SharedCalendarMembers SET MemberColor=? WHERE Id=?", con))
                    {
                        upd.Parameters.AddWithValue("?", color);
                        upd.Parameters.AddWithValue("?", Convert.ToInt32(row["Id"]));
                        upd.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "EnsureMemberColors failed", ex);
        }
    }

    public void SetMemberColor(int calendarId, int userId, string color)
    {
        if (string.IsNullOrWhiteSpace(color)) color = GetDefaultColorForUser(userId);
        color = color.Trim();
        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!ColumnExists(con, "SharedCalendarMembers", "MemberColor")) return;
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendarMembers SET MemberColor=? WHERE CalendarId=? AND CInt(UserId)=?", con))
            {
                cmd.Parameters.AddWithValue("?", color);
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.Parameters.AddWithValue("?", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void UpdateMemberPermission(int calendarId, int userId, string permission)
    {
        if (permission != "ReadWrite") permission = "Read";
        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!ColumnExists(con, "SharedCalendarMembers", "Permission")) return;
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendarMembers SET Permission=? WHERE CalendarId=? AND CInt(UserId)=?", con))
            {
                cmd.Parameters.AddWithValue("?", permission);
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.Parameters.AddWithValue("?", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void RemoveMember(int calendarId, int userId)
    {
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            using (OleDbCommand cmd = new OleDbCommand(
                "DELETE FROM SharedCalendarMembers WHERE CalendarId=? AND CInt(UserId)=?", con))
            {
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.Parameters.AddWithValue("?", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static readonly string DefaultVisibleFields = "Title,EventDate,EventTime,Category,Notes,Assignee,Status,Creator,Reminder";
    public static readonly string DefaultRequiredFields = "Title,EventDate";
    public static readonly string[] AlwaysRequiredBuiltinFields = new[] { "Title", "EventDate" };

    private static HashSet<string> ParseFieldCsv(string csv, string fallbackCsv)
    {
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string source = string.IsNullOrWhiteSpace(csv) ? fallbackCsv : csv;
        foreach (string part in source.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string key = part.Trim();
            if (!string.IsNullOrEmpty(key)) set.Add(key);
        }
        if (set.Count == 0 && !string.IsNullOrWhiteSpace(fallbackCsv))
        {
            foreach (string part in fallbackCsv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string key = part.Trim();
                if (!string.IsNullOrEmpty(key)) set.Add(key);
            }
        }
        return set;
    }

    private static string NormalizeFieldCsv(string fieldsCsv, string fallbackCsv, IEnumerable<string> alwaysInclude)
    {
        HashSet<string> set = ParseFieldCsv(fieldsCsv, fallbackCsv);
        if (alwaysInclude != null)
        {
            foreach (string key in alwaysInclude)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    set.Add(key.Trim());
            }
        }
        return string.Join(",", new List<string>(set).ToArray());
    }

    public string GetVisibleFields(int calendarId)
    {
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!ColumnExists(con, "SharedCalendars", "VisibleFields"))
                    return DefaultVisibleFields;
                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT VisibleFields FROM SharedCalendars WHERE Id=?", con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    object result = cmd.ExecuteScalar();
                    string fields = result != null && result != DBNull.Value ? Convert.ToString(result).Trim() : "";
                    return string.IsNullOrEmpty(fields) ? DefaultVisibleFields : fields;
                }
            }
        }
        catch
        {
            return DefaultVisibleFields;
        }
    }

    public HashSet<string> GetVisibleFieldSet(int calendarId)
    {
        HashSet<string> set = ParseFieldCsv(GetVisibleFields(calendarId), DefaultVisibleFields);
        foreach (string key in AlwaysRequiredBuiltinFields)
            set.Add(key);
        return set;
    }

    public void SetVisibleFields(int calendarId, string fieldsCsv)
    {
        fieldsCsv = NormalizeFieldCsv(fieldsCsv, DefaultVisibleFields, AlwaysRequiredBuiltinFields);
        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!ColumnExists(con, "SharedCalendars", "VisibleFields")) return;
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendars SET VisibleFields=? WHERE Id=?", con))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = fieldsCsv });
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public string GetRequiredFields(int calendarId)
    {
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!ColumnExists(con, "SharedCalendars", "RequiredFields"))
                    return DefaultRequiredFields;
                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT RequiredFields FROM SharedCalendars WHERE Id=?", con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    object result = cmd.ExecuteScalar();
                    string fields = result != null && result != DBNull.Value ? Convert.ToString(result).Trim() : "";
                    return string.IsNullOrEmpty(fields) ? DefaultRequiredFields : fields;
                }
            }
        }
        catch
        {
            return DefaultRequiredFields;
        }
    }

    public HashSet<string> GetRequiredFieldSet(int calendarId)
    {
        HashSet<string> set = ParseFieldCsv(GetRequiredFields(calendarId), DefaultRequiredFields);
        foreach (string key in AlwaysRequiredBuiltinFields)
            set.Add(key);
        // Creator is system-filled — never treat as user-required on the form.
        set.Remove("Creator");
        return set;
    }

    public void SetRequiredFields(int calendarId, string fieldsCsv)
    {
        fieldsCsv = NormalizeFieldCsv(fieldsCsv, DefaultRequiredFields, AlwaysRequiredBuiltinFields);
        HashSet<string> set = ParseFieldCsv(fieldsCsv, DefaultRequiredFields);
        set.Remove("Creator");
        fieldsCsv = string.Join(",", new List<string>(set).ToArray());
        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!ColumnExists(con, "SharedCalendars", "RequiredFields")) return;
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendars SET RequiredFields=? WHERE Id=?", con))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = fieldsCsv });
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void SetBuiltinFieldSettings(int calendarId, string visibleCsv, string requiredCsv)
    {
        HashSet<string> visible = ParseFieldCsv(visibleCsv, DefaultVisibleFields);
        HashSet<string> required = ParseFieldCsv(requiredCsv, DefaultRequiredFields);
        foreach (string key in AlwaysRequiredBuiltinFields)
        {
            visible.Add(key);
            required.Add(key);
        }
        required.Remove("Creator");
        // Required fields must also be visible.
        foreach (string key in new List<string>(required))
            visible.Add(key);

        SetVisibleFields(calendarId, string.Join(",", new List<string>(visible).ToArray()));
        SetRequiredFields(calendarId, string.Join(",", new List<string>(required).ToArray()));
    }

    public void UpdateCalendarInfo(int calendarId, string name, string description)
    {
        string cleanName = Utilities.FixEncoding((name ?? "").Trim());
        string cleanDesc = Utilities.FixEncoding((description ?? "").Trim());
        if (string.IsNullOrWhiteSpace(cleanName))
            throw new ArgumentException("שם הטבלה חובה");

        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            string nameCol = ColumnExists(con, "SharedCalendars", "Name") ? "Name"
                : (ColumnExists(con, "SharedCalendars", "CalendarName") ? "CalendarName" : "Name");
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendars SET [" + nameCol + "]=?, Description=? WHERE Id=?", con))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = cleanName });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = cleanDesc });
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // בונה HTML של לוח זמנים לשליחה במייל
    public string BuildScheduleEmailHtml(int calendarId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        DataRow calendar = GetSharedCalendar(calendarId);
        string calendarName = "טבלה משותפת";
        if (calendar != null)
        {
            if (calendar.Table.Columns.Contains("CalendarName") && calendar["CalendarName"] != DBNull.Value)
                calendarName = Utilities.FixEncoding(Convert.ToString(calendar["CalendarName"]));
            else if (calendar.Table.Columns.Contains("Name") && calendar["Name"] != DBNull.Value)
                calendarName = Utilities.FixEncoding(Convert.ToString(calendar["Name"]));
        }

        DataTable events = GetSharedCalendarEvents(calendarId);
        DataTable members = GetCalendarMembers(calendarId);
        Dictionary<string, string> memberNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow m in members.Rows)
            memberNames[Convert.ToString(m["UserId"])] = Convert.ToString(m["UserName"]);

        StringBuilder sb = new StringBuilder();
        sb.Append("<div style='font-family:Arial,Helvetica,sans-serif;direction:rtl;text-align:right;'>");
        sb.AppendFormat("<h2 style='margin:0 0 8px;'>{0}</h2>", System.Web.HttpUtility.HtmlEncode(calendarName));
        sb.AppendFormat("<p style='color:#555;margin:0 0 16px;'>לו\"ז שנשלח מ-OptiSched · {0}</p>",
            DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

        if (fromDate.HasValue || toDate.HasValue)
        {
            sb.Append("<p style='margin:0 0 12px;'>טווח: ");
            if (fromDate.HasValue) sb.Append(fromDate.Value.ToString("dd/MM/yyyy"));
            sb.Append(" – ");
            if (toDate.HasValue) sb.Append(toDate.Value.ToString("dd/MM/yyyy"));
            else sb.Append("הלאה");
            sb.Append("</p>");
        }

        sb.Append("<table cellpadding='8' cellspacing='0' border='1' style='border-collapse:collapse;width:100%;font-size:14px;'>");
        sb.Append("<thead><tr style='background:#f0f0f0;'>");
        sb.Append("<th>תאריך</th><th>שעה</th><th>כותרת</th><th>קטגוריה</th><th>אחראי</th><th>סטטוס</th><th>נוצר ע\"י</th><th>הערות</th>");
        sb.Append("</tr></thead><tbody>");

        int count = 0;
        List<DataRow> rows = new List<DataRow>();
        foreach (DataRow row in events.Rows) rows.Add(row);
        rows.Sort((a, b) =>
        {
            DateTime da = a["EventDate"] != DBNull.Value ? Convert.ToDateTime(a["EventDate"]) : DateTime.MaxValue;
            DateTime db = b["EventDate"] != DBNull.Value ? Convert.ToDateTime(b["EventDate"]) : DateTime.MaxValue;
            return da.CompareTo(db);
        });

        foreach (DataRow row in rows)
        {
            if (row["EventDate"] == DBNull.Value) continue;
            DateTime ed = Convert.ToDateTime(row["EventDate"]).Date;
            if (fromDate.HasValue && ed < fromDate.Value.Date) continue;
            if (toDate.HasValue && ed > toDate.Value.Date) continue;

            string title = row.Table.Columns.Contains("Title") ? Utilities.FixEncoding(Convert.ToString(row["Title"])) : "";
            string time = row.Table.Columns.Contains("EventTime") ? Utilities.FixEncoding(Convert.ToString(row["EventTime"])) : "";
            string category = row.Table.Columns.Contains("Category") ? Utilities.FixEncoding(Convert.ToString(row["Category"])) : "";
            string notes = row.Table.Columns.Contains("Notes") ? Utilities.FixEncoding(Convert.ToString(row["Notes"])) : "";
            string createdBy = row.Table.Columns.Contains("CreatedByName") ? Utilities.FixEncoding(Convert.ToString(row["CreatedByName"])) : "";
            string assignee = "";
            if (row.Table.Columns.Contains("AssigneeUserId") && row["AssigneeUserId"] != DBNull.Value)
            {
                string aid = Convert.ToString(row["AssigneeUserId"]);
                if (!memberNames.TryGetValue(aid, out assignee)) assignee = aid;
            }
            string status = "";
            if (row.Table.Columns.Contains("Status") && row["Status"] != DBNull.Value)
            {
                string st = Convert.ToString(row["Status"]);
                if (st == "Done") status = "בוצע";
                else if (st == "InProgress") status = "בתהליך";
                else if (st == "Pending") status = "ממתין";
                else status = st;
            }

            int createdById = 0;
            if (row.Table.Columns.Contains("CreatedBy") && row["CreatedBy"] != DBNull.Value)
                int.TryParse(Convert.ToString(row["CreatedBy"]), out createdById);
            string color = GetMemberColor(calendarId, createdById);

            sb.AppendFormat("<tr style='border-right:4px solid {0};'>", color);
            sb.AppendFormat("<td>{0}</td>", ed.ToString("dd/MM/yyyy"));
            sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(time));
            sb.AppendFormat("<td><strong>{0}</strong></td>", System.Web.HttpUtility.HtmlEncode(title));
            sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(category));
            sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(assignee));
            sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(status));
            sb.AppendFormat("<td><span style='color:{0};font-weight:bold;'>{1}</span></td>", color, System.Web.HttpUtility.HtmlEncode(createdBy));
            sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(notes));
            sb.Append("</tr>");
            count++;
        }

        sb.Append("</tbody></table>");
        if (count == 0)
            sb.Append("<p>אין אירועים בטווח שנבחר.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    // ===== Custom fields (text / list / button) =====

    public static string NormalizeCustomFieldType(string fieldType)
    {
        string t = (fieldType ?? "").Trim().ToLowerInvariant();
        if (t == "list" || t == "dropdown" || t == "select") return "list";
        if (t == "button" || t == "link") return "button";
        return "text";
    }

    public static string[] ParseCustomFieldOptions(string options)
    {
        if (string.IsNullOrWhiteSpace(options)) return new string[0];
        string normalized = options.Replace("\r\n", "\n").Replace('\r', '\n');
        List<string> parts = new List<string>();
        foreach (string line in normalized.Split('\n'))
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            if (trimmedLine.IndexOf(',') >= 0 && trimmedLine.IndexOf('\n') < 0)
            {
                foreach (string piece in trimmedLine.Split(','))
                {
                    string p = Utilities.FixEncoding(piece.Trim());
                    if (!string.IsNullOrEmpty(p)) parts.Add(p);
                }
            }
            else
            {
                parts.Add(Utilities.FixEncoding(trimmedLine));
            }
        }
        return parts.ToArray();
    }

    private static bool ReadYesNo(object value)
    {
        if (value == null || value == DBNull.Value) return false;
        if (value is bool) return (bool)value;
        string s = Convert.ToString(value).Trim();
        if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)) return true;
        if (s == "1" || s == "-1") return true;
        bool b;
        if (bool.TryParse(s, out b)) return b;
        int n;
        if (int.TryParse(s, out n)) return n != 0;
        return false;
    }

    public DataTable GetCustomFields(int calendarId, bool activeOnly = true)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("CalendarId", typeof(int));
        dt.Columns.Add("FieldKey", typeof(string));
        dt.Columns.Add("FieldLabel", typeof(string));
        dt.Columns.Add("FieldType", typeof(string));
        dt.Columns.Add("FieldOptions", typeof(string));
        dt.Columns.Add("SortOrder", typeof(int));
        dt.Columns.Add("IsRequired", typeof(bool));
        dt.Columns.Add("IsActive", typeof(bool));
        dt.Columns.Add("TypeLabel", typeof(string));

        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!TableExists(con, "SharedCalendarCustomFields"))
                    return dt;

                string sql = "SELECT Id, CalendarId, FieldKey, FieldLabel, FieldType, FieldOptions, SortOrder, IsRequired, IsActive FROM SharedCalendarCustomFields WHERE CalendarId=? ORDER BY SortOrder, Id";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                    {
                        DataTable raw = new DataTable();
                        adp.Fill(raw);
                        foreach (DataRow row in raw.Rows)
                        {
                            bool isActive = !row.Table.Columns.Contains("IsActive") || ReadYesNo(row["IsActive"]);
                            if (activeOnly && !isActive) continue;

                            DataRow nr = dt.NewRow();
                            nr["Id"] = Convert.ToInt32(row["Id"]);
                            nr["CalendarId"] = Convert.ToInt32(row["CalendarId"]);
                            nr["FieldKey"] = Convert.ToString(row["FieldKey"] ?? "");
                            nr["FieldLabel"] = Utilities.FixEncoding(Convert.ToString(row["FieldLabel"] ?? ""));
                            string ftype = NormalizeCustomFieldType(Convert.ToString(row["FieldType"]));
                            nr["FieldType"] = ftype;
                            nr["FieldOptions"] = Utilities.FixEncoding(Convert.ToString(row["FieldOptions"] ?? ""));
                            nr["SortOrder"] = row["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(row["SortOrder"]);
                            nr["IsRequired"] = ReadYesNo(row.Table.Columns.Contains("IsRequired") ? row["IsRequired"] : false);
                            nr["IsActive"] = isActive;
                            if (ftype == "list")
                                nr["TypeLabel"] = "\u05E8\u05E9\u05D9\u05DE\u05D4";
                            else if (ftype == "button")
                                nr["TypeLabel"] = "\u05DB\u05E4\u05EA\u05D5\u05E8";
                            else
                                nr["TypeLabel"] = "\u05D8\u05E7\u05E1\u05D8";
                            dt.Rows.Add(nr);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "GetCustomFields failed", ex);
        }
        return dt;
    }

    public int AddCustomField(int calendarId, string label, string fieldType, string options, bool isRequired)
    {
        string cleanLabel = Utilities.FixEncoding((label ?? "").Trim());
        if (string.IsNullOrWhiteSpace(cleanLabel))
            throw new ArgumentException("\u05E9\u05DD \u05D4\u05E9\u05D3\u05D4 \u05D7\u05D5\u05D1\u05D4");

        string type = NormalizeCustomFieldType(fieldType);
        string cleanOptions = Utilities.FixEncoding((options ?? "").Trim());
        if (type == "list" && string.IsNullOrWhiteSpace(cleanOptions))
            throw new ArgumentException("\u05E8\u05E9\u05D9\u05DE\u05D4 \u05D3\u05D5\u05E8\u05E9\u05EA \u05D0\u05E4\u05E9\u05E8\u05D5\u05D9\u05D5\u05EA");
        if (type == "button" && string.IsNullOrWhiteSpace(cleanOptions))
            throw new ArgumentException("\u05DB\u05E4\u05EA\u05D5\u05E8 \u05D3\u05D5\u05E8\u05E9 \u05DB\u05EA\u05D5\u05D1\u05EA URL");
        if (type == "text")
            cleanOptions = "";

        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!TableExists(con, "SharedCalendarCustomFields"))
                throw new InvalidOperationException("Custom fields table missing");

            int nextOrder = 1;
            using (OleDbCommand maxCmd = new OleDbCommand(
                "SELECT MAX(SortOrder) FROM SharedCalendarCustomFields WHERE CalendarId=?", con))
            {
                maxCmd.Parameters.AddWithValue("?", calendarId);
                object maxObj = maxCmd.ExecuteScalar();
                if (maxObj != null && maxObj != DBNull.Value)
                    nextOrder = Convert.ToInt32(maxObj) + 1;
            }

            int nextKeyNum = 1;
            using (OleDbCommand keyCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM SharedCalendarCustomFields WHERE CalendarId=?", con))
            {
                keyCmd.Parameters.AddWithValue("?", calendarId);
                object cnt = keyCmd.ExecuteScalar();
                if (cnt != null && cnt != DBNull.Value)
                    nextKeyNum = Convert.ToInt32(cnt) + 1;
            }
            string fieldKey = "custom_" + nextKeyNum + "_" + DateTime.UtcNow.Ticks.ToString().Substring(10);

            using (OleDbCommand cmd = new OleDbCommand(@"
                INSERT INTO SharedCalendarCustomFields
                (CalendarId, FieldKey, FieldLabel, FieldType, FieldOptions, SortOrder, IsRequired, IsActive)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)", con))
            {
                cmd.Parameters.AddWithValue("?", calendarId);
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = fieldKey });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = cleanLabel });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = type });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = cleanOptions });
                cmd.Parameters.AddWithValue("?", nextOrder);
                cmd.Parameters.AddWithValue("?", isRequired);
                cmd.Parameters.AddWithValue("?", true);
                cmd.ExecuteNonQuery();
            }

            using (OleDbCommand idCmd = new OleDbCommand("SELECT @@IDENTITY", con))
            {
                object idObj = idCmd.ExecuteScalar();
                return idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
            }
        }
    }

    public void UpdateCustomField(int fieldId, string label, string fieldType, string options, bool isRequired)
    {
        string cleanLabel = Utilities.FixEncoding((label ?? "").Trim());
        if (string.IsNullOrWhiteSpace(cleanLabel))
            throw new ArgumentException("\u05E9\u05DD \u05D4\u05E9\u05D3\u05D4 \u05D7\u05D5\u05D1\u05D4");

        string type = NormalizeCustomFieldType(fieldType);
        string cleanOptions = Utilities.FixEncoding((options ?? "").Trim());
        if (type == "list" && string.IsNullOrWhiteSpace(cleanOptions))
            throw new ArgumentException("\u05E8\u05E9\u05D9\u05DE\u05D4 \u05D3\u05D5\u05E8\u05E9\u05EA \u05D0\u05E4\u05E9\u05E8\u05D5\u05D9\u05D5\u05EA");
        if (type == "button" && string.IsNullOrWhiteSpace(cleanOptions))
            throw new ArgumentException("\u05DB\u05E4\u05EA\u05D5\u05E8 \u05D3\u05D5\u05E8\u05E9 \u05DB\u05EA\u05D5\u05D1\u05EA URL");
        if (type == "text")
            cleanOptions = "";

        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            using (OleDbCommand cmd = new OleDbCommand(@"
                UPDATE SharedCalendarCustomFields
                SET FieldLabel=?, FieldType=?, FieldOptions=?, IsRequired=?
                WHERE Id=?", con))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = cleanLabel });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = type });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = cleanOptions });
                cmd.Parameters.AddWithValue("?", isRequired);
                cmd.Parameters.AddWithValue("?", fieldId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void DeleteCustomField(int fieldId)
    {
        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            string fieldKey = null;
            using (OleDbCommand getCmd = new OleDbCommand(
                "SELECT FieldKey FROM SharedCalendarCustomFields WHERE Id=?", con))
            {
                getCmd.Parameters.AddWithValue("?", fieldId);
                object keyObj = getCmd.ExecuteScalar();
                if (keyObj != null && keyObj != DBNull.Value)
                    fieldKey = Convert.ToString(keyObj);
            }

            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE SharedCalendarCustomFields SET IsActive=? WHERE Id=?", con))
            {
                cmd.Parameters.AddWithValue("?", false);
                cmd.Parameters.AddWithValue("?", fieldId);
                cmd.ExecuteNonQuery();
            }

            if (!string.IsNullOrEmpty(fieldKey) && TableExists(con, "SharedCalendarEventFieldValues"))
            {
                try
                {
                    using (OleDbCommand del = new OleDbCommand(
                        "DELETE FROM SharedCalendarEventFieldValues WHERE FieldKey=?", con))
                    {
                        del.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = fieldKey });
                        del.ExecuteNonQuery();
                    }
                }
                catch { }
            }
        }
    }

    public Dictionary<string, string> GetEventFieldValues(int eventId)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (eventId <= 0) return map;
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!TableExists(con, "SharedCalendarEventFieldValues"))
                    return map;
                using (OleDbCommand cmd = new OleDbCommand(
                    "SELECT FieldKey, FieldValue FROM SharedCalendarEventFieldValues WHERE EventId=?", con))
                {
                    cmd.Parameters.AddWithValue("?", eventId);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string key = Convert.ToString(reader["FieldKey"] ?? "");
                            string val = Utilities.FixEncoding(Convert.ToString(reader["FieldValue"] ?? ""));
                            if (!string.IsNullOrEmpty(key))
                                map[key] = val;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "GetEventFieldValues failed", ex);
        }
        return map;
    }

    public Dictionary<int, Dictionary<string, string>> GetEventFieldValuesForCalendar(int calendarId)
    {
        Dictionary<int, Dictionary<string, string>> result =
            new Dictionary<int, Dictionary<string, string>>();
        try
        {
            try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();
                if (!TableExists(con, "SharedCalendarEventFieldValues") || !TableExists(con, "SharedCalendarEvents"))
                    return result;

                string sql = @"
                    SELECT V.EventId, V.FieldKey, V.FieldValue
                    FROM SharedCalendarEventFieldValues V
                    INNER JOIN SharedCalendarEvents E ON V.EventId = E.Id
                    WHERE E.CalendarId=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int eid = Convert.ToInt32(reader["EventId"]);
                            string key = Convert.ToString(reader["FieldKey"] ?? "");
                            string val = Utilities.FixEncoding(Convert.ToString(reader["FieldValue"] ?? ""));
                            if (string.IsNullOrEmpty(key)) continue;
                            Dictionary<string, string> map;
                            if (!result.TryGetValue(eid, out map))
                            {
                                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                result[eid] = map;
                            }
                            map[key] = val;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SharedCalendarService", "GetEventFieldValuesForCalendar failed", ex);
        }
        return result;
    }

    public void SetEventFieldValues(int eventId, Dictionary<string, string> values)
    {
        if (eventId <= 0) return;
        if (values == null) values = new Dictionary<string, string>();

        try { DatabaseMigration.EnsureCollaborationFeatures(); } catch { }
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            if (!TableExists(con, "SharedCalendarEventFieldValues"))
                return;

            using (OleDbCommand del = new OleDbCommand(
                "DELETE FROM SharedCalendarEventFieldValues WHERE EventId=?", con))
            {
                del.Parameters.AddWithValue("?", eventId);
                del.ExecuteNonQuery();
            }

            foreach (KeyValuePair<string, string> kv in values)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                string val = Utilities.FixEncoding(kv.Value ?? "");
                using (OleDbCommand ins = new OleDbCommand(
                    "INSERT INTO SharedCalendarEventFieldValues (EventId, FieldKey, FieldValue) VALUES (?, ?, ?)", con))
                {
                    ins.Parameters.AddWithValue("?", eventId);
                    ins.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = kv.Key.Trim() });
                    ins.Parameters.Add(new OleDbParameter("?", OleDbType.LongVarWChar) { Value = val });
                    ins.ExecuteNonQuery();
                }
            }
        }
    }
}
