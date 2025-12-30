using System;
using System.Data;
using System.Data.OleDb;

public class EventService
{
    private bool TableExists(string tableName, OleDbConnection con)
    {
        try
        {
            OleDbCommand cmd = new OleDbCommand($"SELECT TOP 1 * FROM [{tableName}]", con);
            cmd.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DataTable GetAllEvents(int? userId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();

            // DSD Schema: CalendarEvents table with UserId, EventDate, EventTime columns
            string sql = @"
SELECT 
    C.Id AS Id,
    C.UserId AS UserId,
    C.Title AS Title,
    C.EventDate AS EventDate,
    C.EventTime AS EventTime,
    C.Notes AS Notes,
    C.Category AS Category
FROM CalendarEvents AS C";
            
            if (userId.HasValue)
            {
                sql += " WHERE C.UserId = ?";
            }

            OleDbCommand cmd = new OleDbCommand(sql, con);
            if (userId.HasValue)
            {
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                userIdParam.Value = userId.Value;
                cmd.Parameters.Add(userIdParam);
            }

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            da.Fill(dt);

            dt.Columns.Add("UserName", typeof(string));
            dt.Columns.Add("EventType", typeof(string));

            // DSD Schema: Use Id and UserName columns
            DataTable usersTable = new DataTable();
            OleDbCommand usersCmd = new OleDbCommand("SELECT Id, UserName FROM Users", con);
            OleDbDataAdapter usersDa = new OleDbDataAdapter(usersCmd);
            usersDa.Fill(usersTable);

            System.Collections.Generic.Dictionary<int, string> usersDict = new System.Collections.Generic.Dictionary<int, string>();
            foreach (DataRow userRow in usersTable.Rows)
            {
                string idCol = userRow.Table.Columns.Contains("Id") ? "Id" : "id";
                string nameCol = userRow.Table.Columns.Contains("UserName") ? "UserName" : "username";
                int uid = Convert.ToInt32(userRow[idCol]);
                string uname = Connect.FixEncoding(userRow[nameCol]?.ToString() ?? "");
                usersDict[uid] = uname;
            }

            foreach (DataRow row in dt.Rows)
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

            foreach (DataRow row in dt.Rows)
            {
                if (row["UserId"] != DBNull.Value && row["UserId"] != null)
                {
                    try
                    {
                        int uid = Convert.ToInt32(row["UserId"]);
                        if (usersDict.ContainsKey(uid))
                        {
                            row["UserName"] = usersDict[uid];
                        }
                        else
                        {
                            row["UserName"] = "";
                        }
                    }
                    catch
                    {
                        row["UserName"] = "";
                    }
                }
                else
                {
                    row["UserName"] = "";
                }
                
                if (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString()))
                {
                    row["Category"] = "אחר";
                }
                
                row["EventType"] = "personal";
            }

            bool hasSharedTables = TableExists("SharedCalendarEvents", con) && 
                                   TableExists("SharedCalendarMembers", con);

            if (hasSharedTables && userId.HasValue)
            {
                // DSD Schema: SharedCalendarEvents with EventDate, EventTime columns
                string sharedSql = @"
SELECT
    SCE.Id          AS Id,
    SCE.CreatedBy   AS UserId,
    SCE.Title       AS Title,
    SCE.EventDate   AS EventDate,
    SCE.EventTime   AS EventTime,
    SCE.Notes       AS Notes,
    SCE.Category    AS Category
FROM SharedCalendarEvents SCE
INNER JOIN SharedCalendarMembers SCM ON SCE.CalendarId = SCM.CalendarId
WHERE SCM.UserId = ?";

                DataTable sharedDt = new DataTable();
                using (OleDbCommand sharedCmd = new OleDbCommand(sharedSql, con))
                {
                    OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                    userIdParam.Value = userId.Value;
                    sharedCmd.Parameters.Add(userIdParam);
                    using (OleDbDataAdapter sharedDa = new OleDbDataAdapter(sharedCmd))
                    {
                        sharedDa.Fill(sharedDt);
                    }
                }

                sharedDt.Columns.Add("UserName", typeof(string));
                sharedDt.Columns.Add("EventType", typeof(string));

                foreach (DataRow row in sharedDt.Rows)
                {
                    if (row.Table.Columns.Contains("Title"))
                        row["Title"] = Connect.FixEncoding(Convert.ToString(row["Title"]));
                    if (row.Table.Columns.Contains("EventTime"))
                        row["EventTime"] = Connect.FixEncoding(Convert.ToString(row["EventTime"]));
                    if (row.Table.Columns.Contains("Notes"))
                        row["Notes"] = Connect.FixEncoding(Convert.ToString(row["Notes"]));
                    if (row.Table.Columns.Contains("Category"))
                        row["Category"] = Connect.FixEncoding(Convert.ToString(row["Category"]));

                    if (row["UserId"] != DBNull.Value)
                    {
                        int uid = Convert.ToInt32(row["UserId"]);
                        if (usersDict.ContainsKey(uid))
                        {
                            row["UserName"] = usersDict[uid];
                        }
                        else
                        {
                            row["UserName"] = "";
                        }
                    }
                    else
                    {
                        row["UserName"] = "";
                    }
                    
                    if (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString()))
                    {
                        row["Category"] = "אחר";
                    }
                    
                    row["EventType"] = "shared";
                }

                foreach (DataRow row in sharedDt.Rows)
                {
                    dt.ImportRow(row);
                }
            }

            DataView dv = dt.DefaultView;
            dv.Sort = "EventDate DESC, EventTime DESC";
            dt = dv.ToTable();
        }

        return dt;
    }
}
