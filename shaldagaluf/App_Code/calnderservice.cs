using System;
using System.Data;
using System.Data.OleDb;

/// <summary>
/// Calendar Service - DSD Schema: Uses CalendarEvents table
/// Table: CalendarEvents (replaces old "calnder" table)
/// Columns: Id, UserId, Title, EventDate, EventTime, Notes, Category, CreatedDate
/// </summary>
public class calnderservice
{
    /// <summary>
    /// Ensure CalendarEvents table exists - creates it if missing
    /// </summary>
    private static void EnsureCalendarEventsTable(OleDbConnection conn)
    {
        if (!TableExists(conn, "CalendarEvents"))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.EnsureCalendarEventsTable\",\"message\":\"Creating CalendarEvents table\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            string createSql = @"
                CREATE TABLE CalendarEvents (
                    Id AUTOINCREMENT PRIMARY KEY,
                    UserId INTEGER,
                    Title TEXT,
                    EventDate DATETIME,
                    EventTime TEXT,
                    Notes MEMO,
                    Category TEXT,
                    CreatedDate DATETIME
                )";
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(createSql, conn))
                {
                    cmd.ExecuteNonQuery();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.EnsureCalendarEventsTable\",\"message\":\"CalendarEvents table created successfully\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.EnsureCalendarEventsTable\",\"message\":\"Error creating CalendarEvents table\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                // #endregion
                throw;
            }
        }
    }
    
    private static bool TableExists(OleDbConnection conn, string tableName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + tableName + "]", conn))
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
    
    /// <summary>
    /// Insert personal calendar event - DSD Schema: CalendarEvents table
    /// </summary>
    public void InsertEvent(string title, DateTime date, string time, string notes, string category, int? userId = null)
    {
        // DSD Schema: CalendarEvents table with UserId (required, not nullable)
        if (!userId.HasValue)
        {
            throw new ArgumentException("UserId is required for calendar events");
        }
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            EnsureCalendarEventsTable(conn);
        
            string sql = "INSERT INTO CalendarEvents (UserId, Title, EventDate, EventTime, Notes, Category, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?)";
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.InsertEvent:85\",\"message\":\"INSERT SQL\",\"data\":{\"sql\":\"" + sql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"placeholderCount\":7,\"userId\":" + (userId.HasValue ? userId.Value.ToString() : "null") + "},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                userIdParam.Value = userId.Value;
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
                titleParam.Value = title?.Trim() ?? "";
                cmd.Parameters.Add(titleParam);
                
                OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                dateParam.Value = date;
                cmd.Parameters.Add(dateParam);
                
                OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                timeParam.Value = time?.Trim() ?? "";
                cmd.Parameters.Add(timeParam);
                
                OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                notesParam.Value = notes?.Trim() ?? "";
                cmd.Parameters.Add(notesParam);
                
                OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                categoryParam.Value = (category ?? "אחר").Trim();
                cmd.Parameters.Add(categoryParam);
                
                OleDbParameter createdDateParam = new OleDbParameter("?", OleDbType.Date);
                createdDateParam.Value = DateTime.Now;
                cmd.Parameters.Add(createdDateParam);

                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.InsertEvent:BeforeExecute\",\"message\":\"Before ExecuteNonQuery\",\"data\":{\"paramCount\":" + cmd.Parameters.Count + "},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                // #endregion
                try
                {
                    cmd.ExecuteNonQuery();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.InsertEvent:Success\",\"message\":\"ExecuteNonQuery success\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"calnderservice.InsertEvent:Error\",\"message\":\"ExecuteNonQuery error\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Get all events for a user - DSD Schema: CalendarEvents table
    /// Also includes shared calendar events the user is a member of
    /// </summary>
    public DataSet GetAllEvents(int? userId = null)
    {
        DataSet data = new DataSet();
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            EnsureCalendarEventsTable(conn);

            // DSD Schema: CalendarEvents table with UserId, EventDate, EventTime columns
            string sql = "SELECT * FROM CalendarEvents";
            if (userId.HasValue)
            {
                sql += " WHERE UserId = ?";
            }

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (userId.HasValue)
                {
                    OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                    userIdParam.Value = userId.Value;
                    cmd.Parameters.Add(userIdParam);
                }

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(data, "PersonalEvents");
                }
            }

            if (data.Tables.Contains("PersonalEvents"))
            {
                foreach (DataRow row in data.Tables["PersonalEvents"].Rows)
                {
                    // DSD Schema: Use Title, EventTime, Notes, Category
                    if (row.Table.Columns.Contains("Title"))
                        row["Title"] = Connect.FixEncoding(Convert.ToString(row["Title"]));
                    if (row.Table.Columns.Contains("EventTime"))
                        row["EventTime"] = Connect.FixEncoding(Convert.ToString(row["EventTime"]));
                    if (row.Table.Columns.Contains("Notes"))
                        row["Notes"] = Connect.FixEncoding(Convert.ToString(row["Notes"]));
                    if (row.Table.Columns.Contains("Category"))
                        row["Category"] = Connect.FixEncoding(Convert.ToString(row["Category"]));
                }
            }

            if (userId.HasValue)
            {
                try
                {
                    // DSD Schema: SharedCalendarEvents with EventDate, EventTime columns
                    string sharedSql = @"
SELECT 
    SCE.Id,
    SCE.CalendarId,
    SCE.Title,
    SCE.EventDate,
    SCE.EventTime,
    SCE.Notes,
    SCE.Category,
    SCE.CreatedBy AS UserId
FROM SharedCalendarEvents SCE
INNER JOIN SharedCalendarMembers SCM ON SCE.CalendarId = SCM.CalendarId
WHERE SCM.UserId = ?";

                    using (OleDbCommand sharedCmd = new OleDbCommand(sharedSql, conn))
                    {
                        OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                        userIdParam.Value = userId.Value;
                        sharedCmd.Parameters.Add(userIdParam);
                        using (OleDbDataAdapter sharedAdapter = new OleDbDataAdapter(sharedCmd))
                        {
                            sharedAdapter.Fill(data, "SharedEvents");
                        }
                    }

                    if (data.Tables.Contains("SharedEvents"))
                    {
                        foreach (DataRow row in data.Tables["SharedEvents"].Rows)
                        {
                            if (row.Table.Columns.Contains("Title"))
                                row["Title"] = Connect.FixEncoding(Convert.ToString(row["Title"]));
                            if (row.Table.Columns.Contains("EventTime"))
                                row["EventTime"] = Connect.FixEncoding(Convert.ToString(row["EventTime"]));
                            if (row.Table.Columns.Contains("Notes"))
                                row["Notes"] = Connect.FixEncoding(Convert.ToString(row["Notes"]));
                            if (row.Table.Columns.Contains("Category"))
                                row["Category"] = Connect.FixEncoding(Convert.ToString(row["Category"]));
                        }
                    }
                }
                catch
                {
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Delete calendar event - DSD Schema: CalendarEvents table
    /// </summary>
    public void DeleteEvent(int eventId, int? userId = null)
    {
        // DSD Schema: CalendarEvents table with UserId column
        string sql = "DELETE FROM CalendarEvents WHERE Id = ?";
        if (userId.HasValue)
        {
            sql += " AND UserId = ?";
        }

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            EnsureCalendarEventsTable(conn);
            
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = eventId;
                cmd.Parameters.Add(idParam);
                
                if (userId.HasValue)
                {
                    OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                    userIdParam.Value = userId.Value;
                    cmd.Parameters.Add(userIdParam);
                }

                cmd.ExecuteNonQuery();
            }
        }
    }
}

