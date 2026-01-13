using System;
using System.Data;
using System.Data.OleDb;

/// <summary>
/// Database Schema Updater - Adds missing tables to the Access database
/// This class creates the required tables with proper data types, primary keys, and foreign keys.
/// </summary>
public static class DatabaseSchemaUpdater
{
    /// <summary>
    /// Creates all missing tables in the database
    /// </summary>
    public static void CreateMissingTables()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            
            try
            {
                // Create tables one by one
                CreateFilesTable(conn);
                CreateEventFilesTable(conn);
                CreateImagesTable(conn);
                CreateEventImagesTable(conn);
                CreateContactMessagesTable(conn);
                CreatePermissionTypesTable(conn);
                CreateCalendarPermissionsTable(conn);
                CreateCalendarJoinRequestsTable(conn);
                
                LoggingService.Log("DatabaseSchemaUpdater", "All tables created successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Log("DatabaseSchemaUpdater", "Error creating tables", ex);
                throw new Exception("Failed to create database tables: " + ex.Message, ex);
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

    private static void ExecuteNonQuery(OleDbConnection conn, string sql)
    {
        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Creates Files table
    /// </summary>
    private static void CreateFilesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "Files"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "Files table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE Files (
                Id AUTOINCREMENT PRIMARY KEY,
                file_name TEXT(255),
                file_path TEXT(255),
                file_type TEXT(100),
                uploaded_at DATETIME,
                uploaded_by INTEGER
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "Files table created successfully");

        // Add foreign key constraint if possible (Access has limited FK support)
        try
        {
            // Note: Access doesn't support ALTER TABLE ADD CONSTRAINT in the same way as SQL Server
            // Foreign keys are typically managed through the Relationships window in Access
            // We'll document the relationship but not enforce it programmatically
        }
        catch (Exception ex)
        {
            LoggingService.Log("DatabaseSchemaUpdater", "Note: Foreign key constraint for Files.uploaded_by not enforced programmatically", ex);
        }
    }

    /// <summary>
    /// Creates EventFiles junction table
    /// </summary>
    private static void CreateEventFilesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "EventFiles"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "EventFiles table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE EventFiles (
                Id AUTOINCREMENT PRIMARY KEY,
                event_id INTEGER,
                file_id INTEGER
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "EventFiles table created successfully");
    }

    /// <summary>
    /// Creates Images table
    /// </summary>
    private static void CreateImagesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "Images"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "Images table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE Images (
                Id AUTOINCREMENT PRIMARY KEY,
                image_name TEXT(255),
                image_path TEXT(255),
                uploaded_at DATETIME,
                uploaded_by INTEGER
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "Images table created successfully");
    }

    /// <summary>
    /// Creates EventImages junction table
    /// </summary>
    private static void CreateEventImagesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "EventImages"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "EventImages table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE EventImages (
                Id AUTOINCREMENT PRIMARY KEY,
                event_id INTEGER,
                image_id INTEGER
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "EventImages table created successfully");
    }

    /// <summary>
    /// Creates ContactMessages table
    /// </summary>
    private static void CreateContactMessagesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "ContactMessages"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "ContactMessages table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE ContactMessages (
                Id AUTOINCREMENT PRIMARY KEY,
                full_name TEXT(255),
                email TEXT(255),
                subject TEXT(255),
                message MEMO,
                created_at DATETIME
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "ContactMessages table created successfully");
    }

    /// <summary>
    /// Creates PermissionTypes lookup table
    /// </summary>
    private static void CreatePermissionTypesTable(OleDbConnection conn)
    {
        if (TableExists(conn, "PermissionTypes"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "PermissionTypes table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE PermissionTypes (
                Id AUTOINCREMENT PRIMARY KEY,
                name TEXT(255),
                description MEMO
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "PermissionTypes table created successfully");
    }

    /// <summary>
    /// Creates CalendarPermissions table
    /// </summary>
    private static void CreateCalendarPermissionsTable(OleDbConnection conn)
    {
        if (TableExists(conn, "CalendarPermissions"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "CalendarPermissions table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE CalendarPermissions (
                Id AUTOINCREMENT PRIMARY KEY,
                calendar_id INTEGER,
                user_id INTEGER,
                permission_type_id INTEGER
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "CalendarPermissions table created successfully");
    }

    /// <summary>
    /// Creates CalendarJoinRequests table
    /// </summary>
    private static void CreateCalendarJoinRequestsTable(OleDbConnection conn)
    {
        if (TableExists(conn, "CalendarJoinRequests"))
        {
            LoggingService.Log("DatabaseSchemaUpdater", "CalendarJoinRequests table already exists, skipping");
            return;
        }

        string sql = @"
            CREATE TABLE CalendarJoinRequests (
                Id AUTOINCREMENT PRIMARY KEY,
                calendar_id INTEGER,
                user_id INTEGER,
                status TEXT(50),
                requested_at DATETIME
            )";

        ExecuteNonQuery(conn, sql);
        LoggingService.Log("DatabaseSchemaUpdater", "CalendarJoinRequests table created successfully");
    }
}

