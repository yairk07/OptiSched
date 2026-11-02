using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

public static class ReminderService
{
    public static void ProcessDueReminders(int userId)
    {
        if (userId <= 0) return;

        try
        {
            DatabaseMigration.EnsureCollaborationFeatures();
        }
        catch (Exception ex)
        {
            LoggingService.Log("ReminderService", "Ensure schema failed", ex);
            return;
        }

        DateTime now = DateTime.Now;

        try
        {
            ProcessPersonal(userId, now);
        }
        catch (Exception ex)
        {
            LoggingService.Log("ReminderService", "Personal reminders failed", ex);
        }

        try
        {
            ProcessShared(userId, now);
        }
        catch (Exception ex)
        {
            LoggingService.Log("ReminderService", "Shared reminders failed", ex);
        }
    }

    private static void ProcessPersonal(int userId, DateTime now)
    {
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            if (!ColumnExists(conn, "CalendarEvents", "ReminderMinutes"))
                return;

            string sql = "SELECT Id, Title, EventDate, EventTime, ReminderMinutes FROM CalendarEvents WHERE UserId=? AND ReminderMinutes>0 AND (ReminderSent=0 OR ReminderSent=False OR ReminderSent IS NULL)";
            DataTable dt = new DataTable();
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                    adp.Fill(dt);
            }

            foreach (DataRow row in dt.Rows)
            {
                if (!IsDue(row, now)) continue;
                int eventId = Convert.ToInt32(row["Id"]);
                string title = Connect.FixEncoding(Convert.ToString(row["Title"]));
                string dateText = row["EventDate"] != DBNull.Value
                    ? Convert.ToDateTime(row["EventDate"]).ToString("dd/MM/yyyy")
                    : "";
                string timeText = row.Table.Columns.Contains("EventTime")
                    ? Connect.FixEncoding(Convert.ToString(row["EventTime"]))
                    : "";
                FireReminder(conn, userId, eventId, title, dateText, timeText, "tasks.aspx", "CalendarEvents");
            }
        }
    }

    private static void ProcessShared(int userId, DateTime now)
    {
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            if (!ColumnExists(conn, "SharedCalendarEvents", "ReminderMinutes"))
                return;

            string sql = @"
                SELECT E.Id, E.Title, E.EventDate, E.EventTime, E.ReminderMinutes, E.CalendarId, E.AssigneeUserId
                FROM SharedCalendarEvents E
                INNER JOIN SharedCalendarMembers M ON E.CalendarId = M.CalendarId
                WHERE CInt(M.UserId)=? AND E.ReminderMinutes>0 AND (E.ReminderSent=0 OR E.ReminderSent=False OR E.ReminderSent IS NULL)";

            DataTable dt = new DataTable();
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                    adp.Fill(dt);
            }

            foreach (DataRow row in dt.Rows)
            {
                if (!IsDue(row, now)) continue;
                int eventId = Convert.ToInt32(row["Id"]);
                string title = Connect.FixEncoding(Convert.ToString(row["Title"]));
                int calendarId = Convert.ToInt32(row["CalendarId"]);
                string link = "sharedCalendarDetails.aspx?id=" + calendarId;
                string dateText = row["EventDate"] != DBNull.Value
                    ? Convert.ToDateTime(row["EventDate"]).ToString("dd/MM/yyyy")
                    : "";
                string timeText = row.Table.Columns.Contains("EventTime")
                    ? Connect.FixEncoding(Convert.ToString(row["EventTime"]))
                    : "";

                // Mark once, notify all members (+ email best-effort)
                using (OleDbCommand mark = new OleDbCommand(
                    "UPDATE SharedCalendarEvents SET ReminderSent=True WHERE Id=?", conn))
                {
                    mark.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
                    mark.ExecuteNonQuery();
                }

                DataTable members = new SharedCalendarService().GetCalendarMembers(calendarId);
                foreach (DataRow m in members.Rows)
                {
                    int mid = Convert.ToInt32(m["UserId"]);
                    try
                    {
                        string msg = title;
                        if (!string.IsNullOrWhiteSpace(dateText) || !string.IsNullOrWhiteSpace(timeText))
                            msg = title + " - " + dateText + (string.IsNullOrWhiteSpace(timeText) ? "" : " " + timeText);
                        NotificationService.Create(mid, "תזכורת לאירוע", msg, link, eventId);
                        TryEmail(mid, title, link, dateText, timeText);
                    }
                    catch { }
                }
            }
        }
    }

    private static bool IsDue(DataRow row, DateTime now)
    {
        if (row["EventDate"] == null || row["EventDate"] == DBNull.Value)
            return false;

        DateTime eventDate = Convert.ToDateTime(row["EventDate"]).Date;
        string timeStr = row.Table.Columns.Contains("EventTime") ? Convert.ToString(row["EventTime"]) : "";
        DateTime when = CombineDateTime(eventDate, timeStr);
        int minutes = Convert.ToInt32(row["ReminderMinutes"]);
        if (minutes <= 0) return false;

        DateTime windowStart = when.AddMinutes(-minutes);
        // Due if now is after window start and before/at event (or slightly after for late page loads)
        return now >= windowStart && now <= when.AddMinutes(30);
    }

    public static DateTime CombineDateTime(DateTime date, string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
            return date.Date.AddHours(9); // default morning if no time

        timeStr = timeStr.Trim();
        // Support HH:mm or HH:mm-HH:mm (take start)
        if (timeStr.Contains("-"))
            timeStr = timeStr.Split('-')[0].Trim();

        DateTime parsed;
        if (DateTime.TryParseExact(timeStr, new[] { "HH:mm", "H:mm", "HH:mm:ss" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return date.Date.Add(parsed.TimeOfDay);
        }

        if (DateTime.TryParse(timeStr, out parsed))
            return date.Date.Add(parsed.TimeOfDay);

        return date.Date.AddHours(9);
    }

    private static void FireReminder(OleDbConnection conn, int userId, int eventId, string title, string dateText, string timeText, string link, string tableName)
    {
        string msg = title ?? "אירוע";
        if (!string.IsNullOrWhiteSpace(dateText) || !string.IsNullOrWhiteSpace(timeText))
            msg = (title ?? "אירוע") + " - " + dateText + (string.IsNullOrWhiteSpace(timeText) ? "" : " " + timeText);

        NotificationService.Create(userId, "תזכורת לאירוע", msg, link, eventId);
        TryEmail(userId, title, link, dateText, timeText);

        using (OleDbCommand cmd = new OleDbCommand(
            "UPDATE [" + tableName + "] SET ReminderSent=True WHERE Id=?", conn))
        {
            cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = eventId });
            cmd.ExecuteNonQuery();
        }
    }

    private static void TryEmail(int userId, string title, string link, string dateText = null, string timeText = null)
    {
        try
        {
            UsersService us = new UsersService();
            // Prefer username lookup then email column from row
            DataRow user = us.GetUserByUsername(FindUsername(userId));
            if (user == null) return;

            string email = null;
            foreach (DataColumn col in user.Table.Columns)
            {
                if (string.Equals(col.ColumnName, "email", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(col.ColumnName, "Email", StringComparison.OrdinalIgnoreCase))
                {
                    email = Convert.ToString(user[col]);
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(email)) return;

            string absLink = link ?? "home.aspx";
            if (!absLink.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && System.Web.HttpContext.Current != null
                && System.Web.HttpContext.Current.Request != null)
            {
                Uri req = System.Web.HttpContext.Current.Request.Url;
                absLink = req.GetLeftPart(UriPartial.Authority) + "/" + absLink.TrimStart('/');
            }

            EmailService.SendEventReminder(email, title ?? "אירוע", absLink, dateText, timeText);
        }
        catch (Exception ex)
        {
            LoggingService.Log("ReminderService", "Email reminder skipped/failed", ex);
        }
    }

    private static string FindUsername(int userId)
    {
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            string[] sqls =
            {
                "SELECT TOP 1 username FROM Users WHERE id=?",
                "SELECT TOP 1 userName FROM Users WHERE Id=?",
                "SELECT TOP 1 UserName FROM Users WHERE Id=?"
            };
            foreach (string sql in sqls)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value)
                            return Convert.ToString(r);
                    }
                }
                catch { }
            }
        }
        return null;
    }

    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand(
                string.Format("SELECT TOP 1 [{0}] FROM [{1}]", columnName, tableName), conn))
            {
                cmd.ExecuteScalar();
                return true;
            }
        }
        catch { return false; }
    }
}
