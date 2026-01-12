using System;
using System.Data;
using System.Data.OleDb;

public class CalendarService
{
    private static void EnsureCalendarEventsTable(OleDbConnection conn)
    {
        if (!TableExists(conn, "CalendarEvents"))
        {
            LoggingService.Log("CalendarService", "Creating CalendarEvents table");
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
                    LoggingService.Log("CalendarService", "CalendarEvents table created successfully");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("CalendarService", "Error creating CalendarEvents table", ex);
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
        catch (Exception ex)
        {
            LoggingService.Log("CalendarService", string.Format("Table {0} does not exist", tableName), ex);
            return false;
        }
    }
    
    public int InsertEvent(string title, DateTime date, string time, string notes, string category, int? userId = null)
    {
        if (!userId.HasValue)
        {
            throw new ArgumentException("UserId is required for calendar events");
        }
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            EnsureCalendarEventsTable(conn);
        
            string sql = "INSERT INTO CalendarEvents (UserId, Title, EventDate, EventTime, Notes, Category, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?)";
            LoggingService.Log("CalendarService", string.Format("Inserting event: Title={0}, UserId={1}", title, userId.Value));
            
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                userIdParam.Value = userId.Value;
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
                titleParam.Value = title != null ? title.Trim() : "";
                cmd.Parameters.Add(titleParam);
                
                OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                dateParam.Value = date;
                cmd.Parameters.Add(dateParam);
                
                OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                timeParam.Value = time != null ? time.Trim() : "";
                cmd.Parameters.Add(timeParam);
                
                OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                notesParam.Value = notes != null ? notes.Trim() : "";
                cmd.Parameters.Add(notesParam);
                
                OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                categoryParam.Value = (category ?? "אחר").Trim();
                cmd.Parameters.Add(categoryParam);
                
                OleDbParameter createdDateParam = new OleDbParameter("?", OleDbType.Date);
                createdDateParam.Value = DateTime.Now;
                cmd.Parameters.Add(createdDateParam);

                try
                {
                    cmd.ExecuteNonQuery();
                    
                    // Get the inserted event ID
                    sql = "SELECT @@IDENTITY";
                    using (OleDbCommand getIdCmd = new OleDbCommand(sql, conn))
                    {
                        object result = getIdCmd.ExecuteScalar();
                        int eventId = Convert.ToInt32(result);
                        LoggingService.Log("CalendarService", string.Format("Event inserted successfully - EventId: {0}", eventId));
                        return eventId;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Log("CalendarService", "Error inserting event", ex);
                    throw;
                }
            }
        }
    }

    public DataSet GetAllEvents(int? userId = null)
    {
        DataSet data = new DataSet();
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            EnsureCalendarEventsTable(conn);

            string sql = "SELECT * FROM CalendarEvents ORDER BY Id";

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(data, "PersonalEvents");
                }
            }

            if (data.Tables.Contains("PersonalEvents"))
            {
                foreach (DataRow row in data.Tables["PersonalEvents"].Rows)
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

            if (userId.HasValue)
            {
                try
                {
                    string sharedSql = @"
SELECT *
FROM SharedCalendarEvents
ORDER BY Id";

                    using (OleDbCommand sharedCmd = new OleDbCommand(sharedSql, conn))
                    {
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
                catch (Exception ex)
                {
                    LoggingService.Log("CalendarService", "Error loading shared events", ex);
                }
            }
        }

        return data;
    }

    public void DeleteEvent(int eventId, int? userId = null)
    {
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

