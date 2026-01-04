using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

/// <summary>
/// SharedCalendarService - DSD Schema: Static tables, INTEGER types, EventDate/EventTime columns
/// No dynamic table creation - tables must exist in database
/// </summary>
public class SharedCalendarService
{
    // DSD Schema: No dynamic table creation - tables are predefined
    // Constructor no longer creates tables
    public SharedCalendarService()
    {
        // Tables must exist in database - no dynamic creation
    }
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
                    // Check if Permission column exists once at the beginning
                    bool hasPermissionColumn = ColumnExists(con, "SharedCalendarMembers", "Permission");
                    
                    string userNameCol = "username";
                    if (!ColumnExists(con, "Users", "username"))
                    {
                        if (ColumnExists(con, "Users", "UserName"))
                            userNameCol = "UserName";
                        else if (ColumnExists(con, "Users", "userName"))
                            userNameCol = "userName";
                    }
                    
                    // Get ALL calendars first
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
                    if (hasPermissionColumn && !dt.Columns.Contains("Permission"))
                    {
                        dt.Columns.Add("Permission", typeof(string));
                    }

                    // Check membership and admin status for each calendar
                    foreach (DataRow row in dt.Rows)
                    {
                        int calendarId = Convert.ToInt32(row["CalendarId"]);
                        int createdBy = Convert.ToInt32(row["CreatedBy"]);
                        
                        // Check if user is admin (creator)
                        if (createdBy == userId.Value)
                        {
                            row["IsAdmin"] = 1;
                            row["IsMember"] = 0;
                            if (hasPermissionColumn)
                            {
                                row["Permission"] = "ReadWrite";
                            }
                        }
                        else
                        {
                            // Check if user is member
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
                                    if (hasPermissionColumn)
                                    {
                                        string permission = GetMemberPermission(con, calendarId, userId.Value);
                                        row["Permission"] = permission ?? "Read";
                                    }
                                }
                                else
                                {
                                    row["IsAdmin"] = 0;
                                    row["IsMember"] = 0;
                                    if (hasPermissionColumn)
                                    {
                                        row["Permission"] = "";
                                    }
                                }
                            }
                        }
                        
                        // Get request status
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
                        row["CalendarName"] = Connect.FixEncoding(row["CalendarName"].ToString());
                    if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value && row["Description"] != null)
                        row["Description"] = Connect.FixEncoding(row["Description"].ToString());
                    if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value && row["CreatorName"] != null)
                        row["CreatorName"] = Connect.FixEncoding(row["CreatorName"].ToString());
                }
            }
        }
        catch
        {
        }

        return dt;
    }

    public int CreateSharedCalendar(string name, string description, int createdBy)
    {
        string conStr = Connect.GetConnectionString();
        int calendarId = 0;

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // DSD Schema: INTEGER types, creator must be inserted as admin in SharedCalendarMembers
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

                    cmd.CommandText = "SELECT @@IDENTITY";
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        calendarId = Convert.ToInt32(result);

                        // DSD Schema: Creator MUST be inserted as admin in SharedCalendarMembers
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

    public void AddMemberToCalendar(int calendarId, int userId, string role = "member")
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // DSD Schema: INTEGER types
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
                    roleParam.Value = role?.Trim() ?? "member";
                    cmd.Parameters.Add(roleParam);
                    
                    cmd.Parameters.AddWithValue("?", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch
        {
            throw;
        }
    }

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
                        row["UserName"] = Connect.FixEncoding(row["UserName"].ToString());
                    if (dt.Columns.Contains("firstName") && row["firstName"] != DBNull.Value && row["firstName"] != null)
                        row["firstName"] = Connect.FixEncoding(row["firstName"].ToString());
                    if (dt.Columns.Contains("FirstName") && row["FirstName"] != DBNull.Value && row["FirstName"] != null)
                        row["FirstName"] = Connect.FixEncoding(row["FirstName"].ToString());
                    if (dt.Columns.Contains("lastName") && row["lastName"] != DBNull.Value && row["lastName"] != null)
                        row["lastName"] = Connect.FixEncoding(row["lastName"].ToString());
                    if (dt.Columns.Contains("LastName") && row["LastName"] != DBNull.Value && row["LastName"] != null)
                        row["LastName"] = Connect.FixEncoding(row["LastName"].ToString());
                    if (dt.Columns.Contains("Message") && row["Message"] != DBNull.Value && row["Message"] != null)
                        row["Message"] = Connect.FixEncoding(row["Message"].ToString());
                }
            }
        }
        catch
        {
        }

        return dt;
    }

    public void ApproveJoinRequest(int requestId, int calendarId, int userId)
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

                AddMemberToCalendar(calendarId, userId, "member");
            }
        }
        catch
        {
            throw;
        }
    }

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
                            row["Title"] = Connect.FixEncoding(row["Title"].ToString());
                        if (dt.Columns.Contains("EventTime") && row["EventTime"] != DBNull.Value && row["EventTime"] != null)
                            row["EventTime"] = Connect.FixEncoding(row["EventTime"].ToString());
                        if (dt.Columns.Contains("Notes") && row["Notes"] != DBNull.Value && row["Notes"] != null)
                            row["Notes"] = Connect.FixEncoding(row["Notes"].ToString());
                        if (dt.Columns.Contains("Category") && row["Category"] != DBNull.Value && row["Category"] != null)
                            row["Category"] = Connect.FixEncoding(row["Category"].ToString());
                        if (dt.Columns.Contains("CreatedByName") && row["CreatedByName"] != DBNull.Value && row["CreatedByName"] != null)
                            row["CreatedByName"] = Connect.FixEncoding(row["CreatedByName"].ToString());
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

    public void AddSharedCalendarEvent(int calendarId, string title, DateTime date, string time, string notes, string category, int createdBy)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            string cleanTitle = Connect.FixEncoding((title ?? "").Trim());
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = Connect.FixEncoding((time ?? "").Trim());
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = Connect.FixEncoding((notes ?? "").Trim());
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = Connect.FixEncoding((category ?? "אחר").Trim());
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

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
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        catch
        {
            throw;
        }
    }

    public void UpdateSharedCalendarEvent(int eventId, string title, DateTime date, string time, string notes, string category)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            string cleanTitle = Connect.FixEncoding((title ?? "").Trim());
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = Connect.FixEncoding((time ?? "").Trim());
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = Connect.FixEncoding((notes ?? "").Trim());
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = Connect.FixEncoding((category ?? "אחר").Trim());
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

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
            }
        }
        catch
        {
            throw;
        }
    }

    public void DeleteSharedCalendarEvent(int eventId)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

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
                        row["CalendarName"] = Connect.FixEncoding(row["CalendarName"].ToString());
                    if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value && row["Description"] != null)
                        row["Description"] = Connect.FixEncoding(row["Description"].ToString());
                    if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value && row["CreatorName"] != null)
                        row["CreatorName"] = Connect.FixEncoding(row["CreatorName"].ToString());
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"SharedCalendarService.GetSharedCalendar:ERROR\",\"message\":\"Error getting shared calendar\",\"data\":{\"calendarId\":\"" + calendarId + "\",\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

        if (dt.Rows.Count > 0)
            return dt.Rows[0];

        return null;
    }

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
                            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
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
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"SharedCalendarService.GetAccessRequests:ERROR\",\"message\":\"Error getting access requests\",\"data\":{\"calendarId\":\"" + calendarId + "\",\"approverUserId\":\"" + (approverUserId?.ToString() ?? "null") + "\",\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

        return dt;
    }

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
        return "Read"; // Default permission
    }
}