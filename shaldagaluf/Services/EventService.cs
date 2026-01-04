using System;
using System.Data;
using System.Data.OleDb;
using System.Linq;

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

    public DataTable GetAllEvents(int? userId = null)
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        try
        {
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                try
                {
                    con.Open();
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:CONNECTION_OPENED\",\"message\":\"Connection opened successfully\",\"data\":{\"connectionString\":\"" + conStr.Replace("\\", "\\\\").Replace("\"", "\\\"").Substring(0, Math.Min(100, conStr.Length)) + "...\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                }
                catch (Exception connEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:CONNECTION_ERROR\",\"message\":\"Connection failed\",\"data\":{\"error\":\"" + connEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + connEx.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    throw;
                }

                bool tableExists = TableExists("CalendarEvents", con);
                if (!tableExists)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:TABLE_NOT_FOUND\",\"message\":\"CalendarEvents table does not exist\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                    return dt;
                }

                try
                {
                    using (OleDbCommand countCmd = new OleDbCommand("SELECT COUNT(*) FROM CalendarEvents", con))
                    {
                        object countResult = countCmd.ExecuteScalar();
                        int totalCount = countResult != null && countResult != DBNull.Value ? Convert.ToInt32(countResult) : 0;
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:COUNT_CALENDAR_EVENTS\",\"message\":\"Total events in CalendarEvents table\",\"data\":{\"count\":\"" + totalCount + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                }
                catch (Exception countEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:COUNT_ERROR\",\"message\":\"Error counting CalendarEvents\",\"data\":{\"error\":\"" + countEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                }

                string sql = @"
SELECT *
FROM CalendarEvents
ORDER BY Id";

                OleDbCommand cmd = new OleDbCommand(sql, con);

                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"EventService.GetAllEvents:BEFORE_QUERY\",\"message\":\"Before executing query\",\"data\":{\"sql\":\"" + sql.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                da.Fill(dt);
                
                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"EventService.GetAllEvents:AFTER_FILL_CALENDAR\",\"message\":\"After Fill CalendarEvents\",\"data\":{\"rowCount\":\"" + dt.Rows.Count + "\",\"columnCount\":\"" + dt.Columns.Count + "\",\"columns\":\"" + string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }

                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"EventService.GetAllEvents:QUERY_SUCCESS_CALENDAR\",\"message\":\"CalendarEvents query executed successfully\",\"data\":{\"rowCount\":\"" + dt.Rows.Count + "\",\"columnCount\":\"" + dt.Columns.Count + "\",\"columns\":\"" + string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }

            if (!dt.Columns.Contains("UserName"))
                dt.Columns.Add("UserName", typeof(string));
            if (!dt.Columns.Contains("EventType"))
                dt.Columns.Add("EventType", typeof(string));

            string userNameCol = "UserName";
            if (!ColumnExists(con, "Users", "UserName"))
            {
                if (ColumnExists(con, "Users", "userName"))
                    userNameCol = "userName";
                else if (ColumnExists(con, "Users", "username"))
                    userNameCol = "username";
            }

            bool usersTableExists = TableExists("Users", con);
            System.Collections.Generic.Dictionary<int, string> usersDict = new System.Collections.Generic.Dictionary<int, string>();
            
            if (usersTableExists)
            {
                try
                {
                    DataTable usersTable = new DataTable();
                    OleDbCommand usersCmd = new OleDbCommand("SELECT Id, " + userNameCol + " AS UserName FROM Users", con);
                    OleDbDataAdapter usersDa = new OleDbDataAdapter(usersCmd);
                    usersDa.Fill(usersTable);

                    foreach (DataRow userRow in usersTable.Rows)
                    {
                        string idCol = userRow.Table.Columns.Contains("Id") ? "Id" : "id";
                        string nameCol = userRow.Table.Columns.Contains("UserName") ? "UserName" : "username";
                        int uid = Convert.ToInt32(userRow[idCol]);
                        string uname = Connect.FixEncoding(userRow[nameCol]?.ToString() ?? "");
                        usersDict[uid] = uname;
                    }
                }
                catch (Exception usersEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:USERS_QUERY_ERROR\",\"message\":\"Error loading users\",\"data\":{\"error\":\"" + usersEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                }
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
                if (row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value && row["UserId"] != null)
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
                
                if (row.Table.Columns.Contains("Category"))
                {
                    if (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString()))
                    {
                        row["Category"] = "אחר";
                    }
                }
                else
                {
                    if (!dt.Columns.Contains("Category"))
                        dt.Columns.Add("Category", typeof(string));
                    row["Category"] = "אחר";
                }
                
                if (row.Table.Columns.Contains("EventType"))
                    row["EventType"] = "personal";
                else
                {
                    if (!dt.Columns.Contains("EventType"))
                        dt.Columns.Add("EventType", typeof(string));
                    row["EventType"] = "personal";
                }
            }

            bool hasSharedTables = TableExists("SharedCalendarEvents", con) && 
                                   TableExists("SharedCalendarMembers", con);

            if (hasSharedTables)
            {
                try
                {
                    using (OleDbCommand countCmd = new OleDbCommand("SELECT COUNT(*) FROM SharedCalendarEvents", con))
                    {
                        object countResult = countCmd.ExecuteScalar();
                        int totalCount = countResult != null && countResult != DBNull.Value ? Convert.ToInt32(countResult) : 0;
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:COUNT_SHARED_EVENTS\",\"message\":\"Total events in SharedCalendarEvents table\",\"data\":{\"count\":\"" + totalCount + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                }
                catch (Exception countEx)
                {
                    try
                    {
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            "{\"location\":\"EventService.GetAllEvents:COUNT_SHARED_ERROR\",\"message\":\"Error counting SharedCalendarEvents\",\"data\":{\"error\":\"" + countEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                    }
                    catch { }
                }
                
                string sharedSql = @"
SELECT *
FROM SharedCalendarEvents
ORDER BY Id";

                DataTable sharedDt = new DataTable();
                using (OleDbCommand sharedCmd = new OleDbCommand(sharedSql, con))
                {
                    using (OleDbDataAdapter sharedDa = new OleDbDataAdapter(sharedCmd))
                    {
                        sharedDa.Fill(sharedDt);
                        try
                        {
                            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                                "{\"location\":\"EventService.GetAllEvents:AFTER_FILL_SHARED\",\"message\":\"After Fill SharedCalendarEvents\",\"data\":{\"rowCount\":\"" + sharedDt.Rows.Count + "\",\"columnCount\":\"" + sharedDt.Columns.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                        }
                        catch { }
                    }
                }

                bool hasEventDate = sharedDt.Columns.Contains("EventDate");
                bool hasDate = sharedDt.Columns.Contains("Date");
                bool hasEventTime = sharedDt.Columns.Contains("EventTime");
                bool hasTime = sharedDt.Columns.Contains("Time");
                bool hasCategory = sharedDt.Columns.Contains("Category");
                bool hasCategoryLower = sharedDt.Columns.Contains("category");
                bool hasUserId = sharedDt.Columns.Contains("UserId");
                bool hasCreatedBy = sharedDt.Columns.Contains("CreatedBy");

                if (!hasEventDate)
                {
                    sharedDt.Columns.Add("EventDate", typeof(DateTime));
                    if (hasDate)
                    {
                        foreach (DataRow row in sharedDt.Rows)
                        {
                            if (row.Table.Columns.Contains("Date") && row["Date"] != DBNull.Value && row["Date"] != null)
                            {
                                row["EventDate"] = row["Date"];
                            }
                        }
                    }
                }
                if (!hasEventTime)
                {
                    sharedDt.Columns.Add("EventTime", typeof(string));
                    if (hasTime)
                    {
                        foreach (DataRow row in sharedDt.Rows)
                        {
                            if (row.Table.Columns.Contains("Time") && row["Time"] != DBNull.Value && row["Time"] != null)
                            {
                                row["EventTime"] = row["Time"];
                            }
                        }
                    }
                }
                if (!hasCategory)
                {
                    sharedDt.Columns.Add("Category", typeof(string));
                    if (hasCategoryLower)
                    {
                        foreach (DataRow row in sharedDt.Rows)
                        {
                            if (row.Table.Columns.Contains("category") && row["category"] != DBNull.Value && row["category"] != null)
                            {
                                row["Category"] = row["category"];
                            }
                        }
                    }
                }
                if (!hasUserId)
                {
                    sharedDt.Columns.Add("UserId", typeof(int));
                    if (hasCreatedBy)
                    {
                        foreach (DataRow row in sharedDt.Rows)
                        {
                            if (row["CreatedBy"] != DBNull.Value && row["CreatedBy"] != null)
                            {
                                string createdByStr = row["CreatedBy"].ToString();
                                if (int.TryParse(createdByStr, out int parsedUid))
                                {
                                    row["UserId"] = parsedUid;
                                }
                            }
                        }
                    }
                }

                if (!sharedDt.Columns.Contains("UserName"))
                    sharedDt.Columns.Add("UserName", typeof(string));
                if (!sharedDt.Columns.Contains("EventType"))
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

                    int uid = -1;
                    if (row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value && row["UserId"] != null)
                    {
                        try
                        {
                            uid = Convert.ToInt32(row["UserId"]);
                        }
                        catch
                        {
                            uid = -1;
                        }
                    }
                    else if (row.Table.Columns.Contains("CreatedBy") && row["CreatedBy"] != DBNull.Value && row["CreatedBy"] != null)
                    {
                        try
                        {
                            string createdByStr = row["CreatedBy"].ToString();
                            if (int.TryParse(createdByStr, out int parsedUid))
                            {
                                uid = parsedUid;
                            }
                        }
                        catch
                        {
                            uid = -1;
                        }
                    }
                    
                    if (uid > 0 && usersDict.ContainsKey(uid))
                    {
                        row["UserName"] = usersDict[uid];
                    }
                    else
                    {
                        row["UserName"] = "";
                    }
                    
                    if (row.Table.Columns.Contains("Category") && (row["Category"] == DBNull.Value || row["Category"] == null || string.IsNullOrWhiteSpace(row["Category"].ToString())))
                    {
                        row["Category"] = "אחר";
                    }
                    else if (!row.Table.Columns.Contains("Category"))
                    {
                        row["Category"] = "אחר";
                    }
                    
                    row["EventType"] = "shared";
                }

                foreach (DataRow sharedRow in sharedDt.Rows)
                {
                    DataRow newRow = dt.NewRow();
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (sharedRow.Table.Columns.Contains(col.ColumnName))
                        {
                            if (sharedRow[col.ColumnName] != DBNull.Value && sharedRow[col.ColumnName] != null)
                            {
                                newRow[col.ColumnName] = sharedRow[col.ColumnName];
                            }
                            else
                            {
                                if (col.DataType == typeof(int))
                                    newRow[col.ColumnName] = 0;
                                else if (col.DataType == typeof(DateTime))
                                    newRow[col.ColumnName] = DateTime.MinValue;
                                else
                                    newRow[col.ColumnName] = "";
                            }
                        }
                        else
                        {
                            if (col.DataType == typeof(int))
                                newRow[col.ColumnName] = 0;
                            else if (col.DataType == typeof(DateTime))
                                newRow[col.ColumnName] = DateTime.MinValue;
                            else if (col.ColumnName == "Category")
                                newRow[col.ColumnName] = "אחר";
                            else
                                newRow[col.ColumnName] = "";
                        }
                    }
                    dt.Rows.Add(newRow);
                }
                
                try
                {
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        "{\"location\":\"EventService.GetAllEvents:AFTER_MERGE\",\"message\":\"After merging shared events\",\"data\":{\"totalRowCount\":\"" + dt.Rows.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
                }
                catch { }
            }

            EnsureRequiredColumnsInTable(dt);

            if (dt.Columns.Contains("EventDate") && dt.Columns.Contains("EventTime"))
            {
                DataView dv = dt.DefaultView;
                dv.Sort = "EventDate DESC, EventTime DESC";
                DataTable sortedDt = new DataTable();
                foreach (DataColumn col in dt.Columns)
                {
                    sortedDt.Columns.Add(col.ColumnName, col.DataType);
                }
                foreach (DataRowView rowView in dv)
                {
                    DataRow newRow = sortedDt.NewRow();
                    DataRow sourceRow = rowView.Row;
                    foreach (DataColumn col in sortedDt.Columns)
                    {
                        if (sourceRow.Table.Columns.Contains(col.ColumnName))
                        {
                            if (sourceRow[col.ColumnName] != DBNull.Value && sourceRow[col.ColumnName] != null)
                            {
                                newRow[col.ColumnName] = sourceRow[col.ColumnName];
                            }
                            else
                            {
                                if (col.DataType == typeof(int))
                                    newRow[col.ColumnName] = 0;
                                else if (col.DataType == typeof(DateTime))
                                    newRow[col.ColumnName] = DateTime.MinValue;
                                else if (col.ColumnName == "Category")
                                    newRow[col.ColumnName] = "אחר";
                                else
                                    newRow[col.ColumnName] = "";
                            }
                        }
                        else
                        {
                            if (col.DataType == typeof(int))
                                newRow[col.ColumnName] = 0;
                            else if (col.DataType == typeof(DateTime))
                                newRow[col.ColumnName] = DateTime.MinValue;
                            else if (col.ColumnName == "Category")
                                newRow[col.ColumnName] = "אחר";
                            else
                                newRow[col.ColumnName] = "";
                        }
                    }
                    sortedDt.Rows.Add(newRow);
                }
                EnsureRequiredColumnsInTable(sortedDt);
                dt = sortedDt;
            }
            else if (dt.Columns.Contains("Date") && dt.Columns.Contains("Time"))
            {
                DataView dv = dt.DefaultView;
                dv.Sort = "Date DESC, Time DESC";
                DataTable sortedDt = new DataTable();
                foreach (DataColumn col in dt.Columns)
                {
                    sortedDt.Columns.Add(col.ColumnName, col.DataType);
                }
                foreach (DataRowView rowView in dv)
                {
                    DataRow newRow = sortedDt.NewRow();
                    DataRow sourceRow = rowView.Row;
                    foreach (DataColumn col in sortedDt.Columns)
                    {
                        if (sourceRow.Table.Columns.Contains(col.ColumnName))
                        {
                            if (sourceRow[col.ColumnName] != DBNull.Value && sourceRow[col.ColumnName] != null)
                            {
                                newRow[col.ColumnName] = sourceRow[col.ColumnName];
                            }
                            else
                            {
                                if (col.DataType == typeof(int))
                                    newRow[col.ColumnName] = 0;
                                else if (col.DataType == typeof(DateTime))
                                    newRow[col.ColumnName] = DateTime.MinValue;
                                else if (col.ColumnName == "Category")
                                    newRow[col.ColumnName] = "אחר";
                                else
                                    newRow[col.ColumnName] = "";
                            }
                        }
                        else
                        {
                            if (col.DataType == typeof(int))
                                newRow[col.ColumnName] = 0;
                            else if (col.DataType == typeof(DateTime))
                                newRow[col.ColumnName] = DateTime.MinValue;
                            else if (col.ColumnName == "Category")
                                newRow[col.ColumnName] = "אחר";
                            else
                                newRow[col.ColumnName] = "";
                        }
                    }
                    sortedDt.Rows.Add(newRow);
                }
                EnsureRequiredColumnsInTable(sortedDt);
                dt = sortedDt;
            }
            }
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    "{\"location\":\"EventService.GetAllEvents:ERROR\",\"message\":\"Error getting events\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\",\"stackTrace\":\"" + ex.StackTrace?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
            }
            catch { }
        }

        EnsureRequiredColumnsInTable(dt);
        
        try
        {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"location\":\"EventService.GetAllEvents:FINAL_RESULT\",\"message\":\"Final result before return\",\"data\":{\"rowCount\":\"" + dt.Rows.Count + "\",\"columnCount\":\"" + dt.Columns.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n");
        }
        catch { }
        
        return dt;
    }

    private void EnsureRequiredColumnsInTable(DataTable dt)
    {
        if (dt == null) return;

        string[] requiredColumns = { "Id", "Title", "UserName", "UserId", "EventDate", "EventTime", "Category", "Notes", "EventType" };
        
        foreach (string colName in requiredColumns)
        {
            if (!dt.Columns.Contains(colName))
            {
                Type colType = typeof(string);
                if (colName == "Id" || colName == "UserId")
                    colType = typeof(int);
                else if (colName == "EventDate")
                    colType = typeof(DateTime);
                
                dt.Columns.Add(colName, colType);
                
                foreach (DataRow row in dt.Rows)
                {
                    if (colName == "Id" || colName == "UserId")
                        row[colName] = 0;
                    else if (colName == "EventDate")
                        row[colName] = DateTime.MinValue;
                    else if (colName == "Category")
                        row[colName] = "אחר";
                    else
                        row[colName] = "";
                }
            }
        }
    }
}
