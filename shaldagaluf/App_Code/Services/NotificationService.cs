using System;
using System.Data;
using System.Data.OleDb;

public static class NotificationService
{
    private static void Ensure()
    {
        try { DatabaseMigration.EnsureCollaborationFeatures(); }
        catch (Exception ex) { LoggingService.Log("NotificationService", "Ensure schema failed", ex); }
    }

    public static void Create(int userId, string title, string message, string linkUrl, int? relatedEventId = null)
    {
        if (userId <= 0) return;
        Ensure();

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            string sql = "INSERT INTO Notifications (UserId, Title, Message, LinkUrl, CreatedDate, IsRead, RelatedEventId) VALUES (?, ?, ?, ?, ?, ?, ?)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = Connect.FixEncoding(title ?? "") });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = Connect.FixEncoding(message ?? "") });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = linkUrl ?? "" });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Date) { Value = DateTime.Now });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Boolean) { Value = false });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = relatedEventId.HasValue ? (object)relatedEventId.Value : DBNull.Value });
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static DataTable GetRecent(int userId, int take = 15)
    {
        Ensure();
        DataTable dt = new DataTable();
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            string sql = "SELECT TOP " + take + " Id, UserId, Title, Message, LinkUrl, CreatedDate, IsRead, RelatedEventId FROM Notifications WHERE UserId=? ORDER BY CreatedDate DESC";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                    adp.Fill(dt);
            }
        }
        foreach (DataRow row in dt.Rows)
        {
            if (dt.Columns.Contains("Title"))
                row["Title"] = Connect.FixEncoding(Convert.ToString(row["Title"]));
            if (dt.Columns.Contains("Message"))
                row["Message"] = Connect.FixEncoding(Convert.ToString(row["Message"]));
        }
        return dt;
    }

    public static int CountUnread(int userId)
    {
        Ensure();
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            string sql = "SELECT COUNT(*) FROM Notifications WHERE UserId=? AND (IsRead=0 OR IsRead=False)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
    }

    public static void MarkRead(int notificationId, int userId)
    {
        Ensure();
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            using (OleDbCommand cmd = new OleDbCommand("UPDATE Notifications SET IsRead=True WHERE Id=? AND UserId=?", conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = notificationId });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void MarkAllRead(int userId)
    {
        Ensure();
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            using (OleDbCommand cmd = new OleDbCommand("UPDATE Notifications SET IsRead=True WHERE UserId=?", conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                cmd.ExecuteNonQuery();
            }
        }
    }
}
