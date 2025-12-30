using System;
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

                if (userId.HasValue)
                {
                    string sql1 = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U.username AS CreatorName,
    SC.CreatedDate
FROM SharedCalendars SC
INNER JOIN Users U ON CLng(SC.CreatedBy) = CLng(U.id)
WHERE SC.CreatedBy = ?";

                    using (OleDbCommand cmd1 = new OleDbCommand(sql1, con))
                    {
                        cmd1.Parameters.AddWithValue("?", userId.Value);
                        using (OleDbDataAdapter da1 = new OleDbDataAdapter(cmd1))
                        {
                            da1.Fill(dt);
                        }
                    }

                    dt.Columns.Add("IsMember", typeof(int));
                    dt.Columns.Add("IsAdmin", typeof(int));

                    foreach (DataRow row in dt.Rows)
                    {
                        row["IsAdmin"] = 1;
                        row["IsMember"] = 0;
                    }

                    string sql2 = @"
SELECT DISTINCT
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U.username AS CreatorName,
    SC.CreatedDate
FROM (SharedCalendars SC
INNER JOIN SharedCalendarMembers SCM ON SC.Id = SCM.CalendarId)
INNER JOIN Users U ON CLng(SC.CreatedBy) = CLng(U.id)
WHERE CLng(SCM.UserId) = ? AND CLng(SC.CreatedBy) <> ?";

                    using (OleDbCommand cmd2 = new OleDbCommand(sql2, con))
                    {
                        cmd2.Parameters.AddWithValue("?", userId.Value);
                        cmd2.Parameters.AddWithValue("?", userId.Value);
                        using (OleDbDataAdapter da2 = new OleDbDataAdapter(cmd2))
                        {
                            DataTable dt2 = new DataTable();
                            da2.Fill(dt2);

                            dt2.Columns.Add("IsMember", typeof(int));
                            dt2.Columns.Add("IsAdmin", typeof(int));

                            foreach (DataRow row in dt2.Rows)
                            {
                                row["IsAdmin"] = 0;
                                row["IsMember"] = 1;
                            }

                            foreach (DataRow row in dt2.Rows)
                            {
                                dt.ImportRow(row);
                            }
                        }
                    }

                    DataView dv = dt.DefaultView;
                    dv.Sort = "CreatedDate DESC";
                    dt = dv.ToTable();
                }
                else
                {
                    string sql = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U.username AS CreatorName,
    SC.CreatedDate,
    0 AS IsMember,
    0 AS IsAdmin
FROM SharedCalendars SC
LEFT JOIN Users U ON CLng(SC.CreatedBy) = CLng(U.id)
ORDER BY SC.CreatedDate DESC";

                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
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

                string sql = @"
SELECT 
    JR.Id AS RequestId,
    JR.CalendarId,
    JR.UserId,
    U.username AS UserName,
    U.firstName,
    U.lastName,
    JR.Status,
    JR.RequestDate,
    JR.Message
FROM JoinRequests JR
LEFT JOIN Users U ON CLng(JR.UserId) = CLng(U.id)
WHERE JR.CalendarId = ? AND JR.Status = 'pending'";

                if (adminUserId.HasValue)
                {
                    sql += " AND EXISTS (SELECT 1 FROM SharedCalendars SC WHERE SC.Id = ? AND CLng(SC.CreatedBy) = ?)";
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

                string sql = @"
SELECT 
    SCE.Id AS Id,
    SCE.CalendarId,
    SCE.Title,
    SCE.EventDate AS EventDate,
    SCE.EventTime AS EventTime,
    SCE.Notes,
    SCE.Category,
    SCE.CreatedBy,
    U.username AS CreatedByName,
    SCE.CreatedDate
FROM SharedCalendarEvents SCE
LEFT JOIN Users U ON CLng(SCE.CreatedBy) = CLng(U.id)
WHERE SCE.CalendarId = ?";

                if (userId.HasValue)
                {
                    sql += " AND EXISTS (SELECT 1 FROM SharedCalendarMembers SCM WHERE SCM.CalendarId = ? AND CLng(SCM.UserId) = ?)";
                }

                sql += " ORDER BY SCE.EventDate DESC, SCE.EventTime DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);
                    if (userId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("?", calendarId);
                        cmd.Parameters.AddWithValue("?", userId.Value);
                    }

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
        }
        catch
        {
        }

        foreach (DataRow row in dt.Rows)
        {
            if (dt.Columns.Contains("Title"))
            {
                object titleObj = row["Title"];
                if (titleObj != null && titleObj != DBNull.Value)
                {
                    string title = Connect.FixEncoding(titleObj.ToString());
                    string trimmed = title.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." ||
                        trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟")
                        row["Title"] = DBNull.Value;
                    else
                        row["Title"] = title;
                }
            }
            
            if (dt.Columns.Contains("Notes"))
            {
                object notesObj = row["Notes"];
                if (notesObj != null && notesObj != DBNull.Value)
                {
                    string notes = Connect.FixEncoding(notesObj.ToString());
                    string trimmed = notes.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." ||
                        trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟")
                        row["Notes"] = DBNull.Value;
                    else
                        row["Notes"] = notes;
                }
            }
            
            if (dt.Columns.Contains("EventTime"))
            {
                object timeObj = row["EventTime"];
                if (timeObj != null && timeObj != DBNull.Value)
                {
                    string time = timeObj.ToString();
                    string trimmed = time.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." ||
                        trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟")
                        row["EventTime"] = DBNull.Value;
                }
            }
            
            if (dt.Columns.Contains("Category"))
            {
                object categoryObj = row["Category"];
                if (categoryObj != null && categoryObj != DBNull.Value)
                {
                    string category = Connect.FixEncoding(categoryObj.ToString());
                    string trimmed = category.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." ||
                        trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟")
                        row["Category"] = "אחר";
                    else
                        row["Category"] = category;
                }
                else
                {
                    row["Category"] = "אחר";
                }
            }
            
            if (dt.Columns.Contains("CreatedByName"))
            {
                object createdByObj = row["CreatedByName"];
                if (createdByObj != null && createdByObj != DBNull.Value)
                {
                    string createdBy = Connect.FixEncoding(createdByObj.ToString());
                    string trimmed = createdBy.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "...." || trimmed == "..." || trimmed == ".." || trimmed == "." ||
                        trimmed == "؟؟؟؟" || trimmed == "؟؟؟" || trimmed == "؟؟" || trimmed == "؟")
                        row["CreatedByName"] = DBNull.Value;
                    else
                        row["CreatedByName"] = createdBy;
                }
            }
        }

        return dt;
    }

    public void AddSharedCalendarEvent(int calendarId, string title, DateTime date, string time, string notes, string category, int createdBy)
    {
        string conStr = Connect.GetConnectionString();

        try
        {
            string cleanTitle = (title ?? "").Trim();
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = (time ?? "").Trim();
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = (notes ?? "").Trim();
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = (category ?? "אחר").Trim();
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // DSD Schema: EventDate, EventTime columns, INTEGER types
                string sql = "INSERT INTO SharedCalendarEvents (CalendarId, Title, EventDate, EventTime, Notes, Category, CreatedBy, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
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
            string cleanTitle = (title ?? "").Trim();
            if (cleanTitle == "...." || cleanTitle == "...")
                cleanTitle = "";

            string cleanTime = (time ?? "").Trim();
            if (cleanTime == "...." || cleanTime == "...")
                cleanTime = "";

            string cleanNotes = (notes ?? "").Trim();
            if (cleanNotes == "...." || cleanNotes == "...")
                cleanNotes = "";

            string cleanCategory = (category ?? "אחר").Trim();
            if (cleanCategory == "...." || cleanCategory == "...")
                cleanCategory = "אחר";

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                // DSD Schema: EventDate, EventTime columns, INTEGER types
                string sql = "UPDATE SharedCalendarEvents SET Title = ?, EventDate = ?, EventTime = ?, Notes = ?, Category = ? WHERE Id = ?";
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

                string sql = "SELECT COUNT(*) FROM SharedCalendars WHERE Id = ? AND CLng(CreatedBy) = ?";
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

                string sql = "SELECT COUNT(*) FROM SharedCalendarMembers WHERE CalendarId = ? AND CLng(UserId) = ?";
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

                string sql = @"
SELECT 
    SC.Id AS CalendarId,
    SC.Name AS CalendarName,
    SC.Description,
    SC.CreatedBy,
    U.username AS CreatorName,
    SC.CreatedDate
FROM SharedCalendars SC
LEFT JOIN Users U ON CLng(SC.CreatedBy) = CLng(U.id)
WHERE SC.Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", calendarId);

                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
        }
        catch
        {
        }

        if (dt.Rows.Count > 0)
            return dt.Rows[0];

        return null;
    }
}