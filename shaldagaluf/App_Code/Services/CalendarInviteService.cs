using System;
using System.Data;
using System.Data.OleDb;

public static class CalendarInviteService
{
    private static void Ensure()
    {
        DatabaseMigration.EnsureCollaborationFeatures();
    }

    public static string CreateInvite(int calendarId, int createdBy, string permission, int maxUses, int validDays)
    {
        Ensure();
        if (string.IsNullOrWhiteSpace(permission))
            permission = "Read";
        if (permission != "ReadWrite" && permission != "Read")
            permission = "Read";
        if (maxUses <= 0) maxUses = 25;
        if (validDays <= 0) validDays = 14;

        string token = Guid.NewGuid().ToString("N");

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            string sql = @"INSERT INTO CalendarInvites
                (CalendarId, Token, Permission, CreatedBy, CreatedDate, ExpiresDate, MaxUses, UseCount, IsActive)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = calendarId });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = token });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = permission });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = createdBy });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Date) { Value = DateTime.Now });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Date) { Value = DateTime.Now.AddDays(validDays) });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = maxUses });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = 0 });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Boolean) { Value = true });
                cmd.ExecuteNonQuery();
            }
        }

        return token;
    }

    public static DataRow GetByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        Ensure();

        DataTable dt = new DataTable();
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            using (OleDbCommand cmd = new OleDbCommand(
                "SELECT * FROM CalendarInvites WHERE Token=?", conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = token.Trim() });
                using (OleDbDataAdapter adp = new OleDbDataAdapter(cmd))
                    adp.Fill(dt);
            }
        }
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    /// <summary>
    /// Redeems invite for user. Returns calendarId on success; throws on failure.
    /// </summary>
    public static int RedeemInvite(string token, int userId)
    {
        if (userId <= 0) throw new InvalidOperationException("יש להתחבר כדי להצטרף.");
        DataRow invite = GetByToken(token);
        if (invite == null)
            throw new InvalidOperationException("קישור ההזמנה לא תקף.");

        bool active = true;
        if (invite.Table.Columns.Contains("IsActive") && invite["IsActive"] != DBNull.Value)
            active = Convert.ToBoolean(invite["IsActive"]);
        if (!active)
            throw new InvalidOperationException("קישור ההזמנה אינו פעיל.");

        DateTime expires = Convert.ToDateTime(invite["ExpiresDate"]);
        if (expires < DateTime.Now)
            throw new InvalidOperationException("פג תוקף קישור ההזמנה.");

        int useCount = invite["UseCount"] != DBNull.Value ? Convert.ToInt32(invite["UseCount"]) : 0;
        int maxUses = invite["MaxUses"] != DBNull.Value ? Convert.ToInt32(invite["MaxUses"]) : 1;
        if (useCount >= maxUses)
            throw new InvalidOperationException("ההזמנה הגיעה למספר השימושים המרבי.");

        int calendarId = Convert.ToInt32(invite["CalendarId"]);
        string permission = Convert.ToString(invite["Permission"]);
        if (permission != "ReadWrite") permission = "Read";

        SharedCalendarService shared = new SharedCalendarService();
        if (shared.IsCalendarMember(calendarId, userId) || shared.IsCalendarAdmin(calendarId, userId))
        {
            // Already a member — still OK, bump use optional; just return
            return calendarId;
        }

        int permissionTypeId = permission == "ReadWrite" ? 2 : 1;
        try
        {
            PermissionService.AddPermission(calendarId, userId, permissionTypeId);
        }
        catch (Exception ex)
        {
            LoggingService.Log("CalendarInviteService", "AddPermission failed", ex);
        }

        try
        {
            shared.AddMemberToCalendar(calendarId, userId, "member", permission);
        }
        catch (Exception ex)
        {
            LoggingService.Log("CalendarInviteService", "AddMemberToCalendar failed", ex);
        }

        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            conn.Open();
            int inviteId = Convert.ToInt32(invite["Id"]);
            using (OleDbCommand cmd = new OleDbCommand(
                "UPDATE CalendarInvites SET UseCount=? WHERE Id=?", conn))
            {
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = useCount + 1 });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = inviteId });
                cmd.ExecuteNonQuery();
            }

            if (useCount + 1 >= maxUses)
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "UPDATE CalendarInvites SET IsActive=False WHERE Id=?", conn))
                {
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = inviteId });
                    cmd.ExecuteNonQuery();
                }
            }
        }

        return calendarId;
    }
}
