using System;
using System.Data;
using System.Data.OleDb;

public class calnderservice
{
    public void InsertEvent(string title, DateTime date, string time, string notes, string category, int? userId = null)
    {
        string sql = "INSERT INTO calnder ([title], [date], [time], [notes], [category], [Userid]) VALUES (?, ?, ?, ?, ?, ?)";

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
        {
            OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
            titleParam.Value = title?.Trim() ?? "";
            cmd.Parameters.Add(titleParam);
            
            cmd.Parameters.AddWithValue("?", date);
            
            OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
            timeParam.Value = time?.Trim() ?? "";
            cmd.Parameters.Add(timeParam);
            
            OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
            notesParam.Value = notes?.Trim() ?? "";
            cmd.Parameters.Add(notesParam);
            
            OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
            categoryParam.Value = (category ?? "אחר").Trim();
            cmd.Parameters.Add(categoryParam);
            
            cmd.Parameters.AddWithValue("?", userId.HasValue ? (object)userId.Value : DBNull.Value);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }

    public DataSet GetAllEvents(int? userId = null)
    {
        DataSet data = new DataSet();
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();

            string sql = "SELECT * FROM calnder";
            if (userId.HasValue)
            {
                sql += " WHERE Userid = ?";
            }

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (userId.HasValue)
                {
                    cmd.Parameters.AddWithValue("?", userId.Value);
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
                    if (row.Table.Columns.Contains("title"))
                        row["title"] = Connect.FixEncoding(Convert.ToString(row["title"]));
                    if (row.Table.Columns.Contains("time"))
                        row["time"] = Connect.FixEncoding(Convert.ToString(row["time"]));
                    if (row.Table.Columns.Contains("notes"))
                        row["notes"] = Connect.FixEncoding(Convert.ToString(row["notes"]));
                    if (row.Table.Columns.Contains("category"))
                        row["category"] = Connect.FixEncoding(Convert.ToString(row["category"]));
                }
            }

            if (userId.HasValue)
            {
                try
                {
                    using (OleDbCommand testCmd = new OleDbCommand("SELECT TOP 1 * FROM SharedCalendarEvents", conn))
                    {
                        testCmd.ExecuteScalar();
                    }

                    string sharedSql = @"
SELECT 
    SCE.Id,
    SCE.CalendarId AS Userid,
    SCE.Title AS title,
    SCE.[Date] AS [date],
    SCE.[Time] AS [time],
    SCE.Notes AS notes,
    SCE.Category AS category
FROM SharedCalendarEvents SCE
INNER JOIN SharedCalendarMembers SCM ON SCE.CalendarId = SCM.CalendarId
WHERE SCM.UserId = ?";

                    using (OleDbCommand sharedCmd = new OleDbCommand(sharedSql, conn))
                    {
                        sharedCmd.Parameters.AddWithValue("?", userId.Value);
                        using (OleDbDataAdapter sharedAdapter = new OleDbDataAdapter(sharedCmd))
                        {
                            sharedAdapter.Fill(data, "SharedEvents");
                        }
                    }

                    if (data.Tables.Contains("SharedEvents"))
                    {
                        foreach (DataRow row in data.Tables["SharedEvents"].Rows)
                        {
                            if (row.Table.Columns.Contains("title"))
                                row["title"] = Connect.FixEncoding(Convert.ToString(row["title"]));
                            if (row.Table.Columns.Contains("time"))
                                row["time"] = Connect.FixEncoding(Convert.ToString(row["time"]));
                            if (row.Table.Columns.Contains("notes"))
                                row["notes"] = Connect.FixEncoding(Convert.ToString(row["notes"]));
                            if (row.Table.Columns.Contains("category"))
                                row["category"] = Connect.FixEncoding(Convert.ToString(row["category"]));
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

    public void DeleteEvent(int eventId, int? userId = null)
    {
        string sql = "DELETE FROM calnder WHERE Id = ?";
        if (userId.HasValue)
        {
            sql += " AND Userid = ?";
        }

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("?", eventId);
            if (userId.HasValue)
            {
                cmd.Parameters.AddWithValue("?", userId.Value);
            }

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}

