using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Web;

/// <summary>
/// Service to handle database operations for CalendarPermissions and CalendarJoinRequests
/// </summary>
public class PermissionService
{
    public PermissionService()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    /// <summary>
    /// Adds a permission record for a user on a calendar
    /// </summary>
    public static void AddPermission(int calendarId, int userId, int permissionTypeId)
    {
        string sql = "INSERT INTO CalendarPermissions (calendar_id, user_id, permission_type_id) VALUES (?, ?, ?)";
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection conn = new OleDbConnection(conStr))
            {
                conn.Open();

                // Auto-healing: Check if table exists
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, "CalendarPermissions", "TABLE" });
                if (schema.Rows.Count == 0)
                {
                    LoggingService.Log("PermissionService", "Table 'CalendarPermissions' missing in AddPermission. Creating tables...");
                    try
                    {
                        DatabaseSchemaUpdater.CreateMissingTables();
                    }
                    catch (Exception ex)
                    {
                         LoggingService.Log("PermissionService", "Schema update failed", ex);
                         // Swallow and try anyway, or return?
                         // If table is missing, INSERT will fail.
                    }
                }

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", calendarId);
                    cmd.Parameters.AddWithValue("@p2", userId);
                    cmd.Parameters.AddWithValue("@p3", permissionTypeId);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
             LoggingService.Log("PermissionService", "Error adding permission", ex);
             if (!ex.Message.Contains("CalendarPermissions"))
                throw;
        }
    }

    // ... (Other methods remain unchanged) ...



    /// <summary>
    /// requests to join a calendar
    /// </summary>
    public static void RequestJoin(int calendarId, int userId)
    {
        string conStr = Connect.GetConnectionString();
        try 
        {
            using (OleDbConnection conn = new OleDbConnection(conStr))
            {
                conn.Open();
                
                // Check if table exists
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, "CalendarJoinRequests", "TABLE" });
                if (schema.Rows.Count == 0)
                {
                    LoggingService.Log("PermissionService", "Table 'CalendarJoinRequests' does not exist. Attempting to create missing tables...");
                    try 
                    {
                        DatabaseSchemaUpdater.CreateMissingTables();
                        // Re-check schema after update attempt
                        schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, "CalendarJoinRequests", "TABLE" });
                        if (schema.Rows.Count == 0)
                        {
                            LoggingService.Log("PermissionService", "Failed to create table 'CalendarJoinRequests'. Skipping join request.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("PermissionService", "Error running schema updater", ex);
                        return;
                    }
                }

                // Check if request already exists
                string sqlCheck = "SELECT COUNT(*) FROM CalendarJoinRequests WHERE calendar_id = ? AND user_id = ? AND status = 'Pending'";
                using (OleDbCommand cmdCheck = new OleDbCommand(sqlCheck, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@p1", calendarId);
                    cmdCheck.Parameters.AddWithValue("@p2", userId);
                    int count = (int)cmdCheck.ExecuteScalar();
                    if (count > 0) return;
                }

                string sql = "INSERT INTO CalendarJoinRequests (calendar_id, user_id, status, requested_at) VALUES (?, ?, 'Pending', ?)";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", calendarId);
                    cmd.Parameters.AddWithValue("@p2", userId);
                    cmd.Parameters.AddWithValue("@p3", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("PermissionService", "Error in RequestJoin", ex);
            // Throwing is risky if table is missing, but maybe better to swallow for now?
            // Re-throw if it's not a missing table issue
             if (!ex.Message.Contains("CalendarJoinRequests"))
                throw;
        }
    }

    // בדיקת תנאי: HasPendingRequest
    public static bool HasPendingRequest(int calendarId, int userId)
    {
        string sql = "SELECT COUNT(*) FROM CalendarJoinRequests WHERE calendar_id = ? AND user_id = ? AND status = 'Pending'";
        
        using (OleDbConnection conn = new OleDbConnection(Connect.GetConnectionString()))
        {
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@p1", calendarId);
                cmd.Parameters.AddWithValue("@p2", userId);
                
                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }
    }

    // שליפת בקשות ממתינות לטבלה
    public static DataTable GetPendingRequestsForCalendar(int calendarId)
    {
        DataTable dt = new DataTable();
        string conStr = Connect.GetConnectionString();

        try
        {
            using (OleDbConnection conn = new OleDbConnection(conStr))
            {
                conn.Open();

                // Auto-healing: Check if table exists
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, "CalendarJoinRequests", "TABLE" });
                if (schema.Rows.Count == 0)
                {
                    LoggingService.Log("PermissionService", "Table 'CalendarJoinRequests' missing in GetPendingRequests. Creating tables...");
                    try
                    {
                        DatabaseSchemaUpdater.CreateMissingTables();
                    }
                    catch (Exception ex)
                    {
                         LoggingService.Log("PermissionService", "Schema update failed", ex);
                         return dt; // Return empty table on failure
                    }
                }

                // Matches the column names expected by sharedCalendarDetails.aspx
                string sql = @"
                    SELECT 
                        r.Id AS RequestId, 
                        r.user_id AS UserId, 
                        u.username AS UserName,
                        u.First_Name AS firstName, 
                        u.Last_Name AS lastName, 
                        r.requested_at AS RequestDate,
                        'Pending' as Status,
                        '' as Message 
                    FROM CalendarJoinRequests r
                    INNER JOIN Users u ON r.user_id = u.id
                    WHERE r.calendar_id = ? AND r.status = 'Pending'
                    ORDER BY r.requested_at DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", calendarId);
                    
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
        }
        catch (Exception ex)
        {
             LoggingService.Log("PermissionService", "Error getting requests", ex);
             // If table is still missing for some reason, return empty dt
        }
        return dt;
    }

    // אישור בקשת הצטרפות והענקת הרשאה
    public static void ApproveJoinRequest(int requestId, int permissionTypeId)
    {
        // 1. Get request details
        int calendarId = 0;
        int userId = 0;

        string conStr = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(conStr))
        {
            conn.Open();
            // No need to check table here if we assume it was checked during "LoadRequests" which usually happens first, 
            // but for safety we could. For now, let's rely on the read having run or just standard error handling.
            
            string sqlGet = "SELECT calendar_id, user_id FROM CalendarJoinRequests WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sqlGet, conn))
            {
                cmd.Parameters.AddWithValue("@p1", requestId);
                try 
                {
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            calendarId = Convert.ToInt32(reader["calendar_id"]);
                            userId = Convert.ToInt32(reader["user_id"]);
                        }
                        else
                        {
                            return; // Request not found
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    if (ex.Message.Contains("CalendarJoinRequests"))
                    {
                         // Try to heal and retry once
                         DatabaseSchemaUpdater.CreateMissingTables();
                         // Retry logic would be complex here, so we might just fail this time but next time it works.
                         // Or we can just let the user re-click.
                         return;
                    }
                    throw;
                }
            }

            // 2. Add Permission (New System)
            AddPermission(calendarId, userId, permissionTypeId);

            // 3. Add to SharedCalendarMembers (Legacy System - for backward compatibility)
            try 
            {
                string role = "member";
                string permStr = (permissionTypeId == 2) ? "ReadWrite" : "Read"; // Assuming 2 is ReadWrite
                
                string sqlLegacy = "INSERT INTO SharedCalendarMembers (CalendarId, UserId, Role, Permission, JoinedDate) VALUES (?, ?, ?, ?, ?)";
                using (OleDbCommand cmd = new OleDbCommand(sqlLegacy, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", calendarId);
                    cmd.Parameters.AddWithValue("@p2", userId);
                    cmd.Parameters.AddWithValue("@p3", role);
                    cmd.Parameters.AddWithValue("@p4", permStr);
                    cmd.Parameters.AddWithValue("@p5", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Ignore if already exists or table issue, main goal is new system
                LoggingService.Log("PermissionService", "Legacy sync failed", ex);
            }

            // 4. Update Request Status
            string sqlUpdate = "UPDATE CalendarJoinRequests SET status = 'Approved' WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sqlUpdate, conn))
            {
                cmd.Parameters.AddWithValue("@p1", requestId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // דחיית בקשת הצטרפות
    public static void RejectJoinRequest(int requestId)
    {
        string sql = "UPDATE CalendarJoinRequests SET status = 'Rejected' WHERE Id = ?";
        string conStr = Connect.GetConnectionString();

        try 
        {
            using (OleDbConnection conn = new OleDbConnection(conStr))
            {
                conn.Open();

                // Auto-healing: Check if table exists
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, "CalendarJoinRequests", "TABLE" });
                if (schema.Rows.Count == 0)
                {
                    // If table missing, update is moot, but let's try to create it anyway so future calls work
                    try { DatabaseSchemaUpdater.CreateMissingTables(); } catch { }
                    return; // No record to update if table didn't exist
                }

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", requestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("PermissionService", "Error rejecting request", ex);
        }
    }
}

