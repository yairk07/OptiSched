using System;
using System.Data;
using System.Data.OleDb;

/// <summary>
/// Database Migration Utility - Migrates existing database to DSD-compliant schema
/// This class handles one-time migration of tables and columns to match the DSD structure.
/// Run this once to migrate existing data to the new schema.
/// </summary>
public static class DatabaseMigration
{
    /// <summary>
    /// Performs complete database migration to DSD-compliant schema
    /// </summary>
    public static void MigrateToDSD()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            try
            {
                // 1. Migrate Users table
                MigrateUsersTable(conn);

                // 2. Migrate calnder to CalendarEvents
                MigrateCalendarEventsTable(conn);

                // 3. Ensure SharedCalendars tables are correct
                EnsureSharedCalendarTables(conn);

                // 4. Migrate OTP and Verification codes to AuthCodes
                MigrateAuthCodesTable(conn);

                // 5. Reminders, notifications, invites, assignee/status
                EnsureCollaborationFeatures(conn);
            }
            catch (Exception ex)
            {
                throw new Exception("Database migration failed: " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// Public entry: ensure reminder/invite/notification schema (safe to call on every request).
    /// </summary>
    public static void EnsureCollaborationFeatures()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureCollaborationFeatures(conn);
        }
    }

    private static void EnsureCollaborationFeatures(OleDbConnection conn)
    {
        EnsureSharedCalendarTables(conn);

        AddColumnIfMissing(conn, "CalendarEvents", "ReminderMinutes", "INTEGER");
        AddColumnIfMissing(conn, "CalendarEvents", "ReminderSent", "YESNO");
        AddColumnIfMissing(conn, "SharedCalendarEvents", "ReminderMinutes", "INTEGER");
        AddColumnIfMissing(conn, "SharedCalendarEvents", "ReminderSent", "YESNO");
        AddColumnIfMissing(conn, "SharedCalendarEvents", "AssigneeUserId", "INTEGER");
        AddColumnIfMissing(conn, "SharedCalendarEvents", "Status", "TEXT(50)");

        if (!TableExists(conn, "Notifications"))
        {
            ExecuteNonQuery(conn, @"
                CREATE TABLE Notifications (
                    Id AUTOINCREMENT PRIMARY KEY,
                    UserId INTEGER,
                    Title MEMO,
                    Message MEMO,
                    LinkUrl MEMO,
                    CreatedDate DATETIME,
                    IsRead YESNO,
                    RelatedEventId INTEGER
                )");
        }

        if (!TableExists(conn, "CalendarInvites"))
        {
            ExecuteNonQuery(conn, @"
                CREATE TABLE CalendarInvites (
                    Id AUTOINCREMENT PRIMARY KEY,
                    CalendarId INTEGER,
                    Token TEXT(64),
                    Permission TEXT(50),
                    CreatedBy INTEGER,
                    CreatedDate DATETIME,
                    ExpiresDate DATETIME,
                    MaxUses INTEGER,
                    UseCount INTEGER,
                    IsActive YESNO
                )");
        }

        // One-time: repair mojibake already stored in Hebrew text columns
        RepairHebrewTextData(conn);
    }

    private static bool _hebrewRepairDone;

    /// <summary>
    /// Re-writes Hebrew fields through Connect.FixEncoding so garbled UTF-8/1255 text is cleaned in-place.
    /// </summary>
    private static void RepairHebrewTextData(OleDbConnection conn)
    {
        if (_hebrewRepairDone) return;
        _hebrewRepairDone = true;

        try
        {
            RepairTableColumns(conn, "Notifications", new[] { "Title", "Message" });
            RepairTableColumns(conn, "CalendarEvents", new[] { "Title", "EventTime", "Notes", "Category" });
            RepairTableColumns(conn, "SharedCalendarEvents", new[] { "Title", "EventTime", "Notes", "Category" });
            RepairTableColumns(conn, "SharedCalendars", new[] { "CalendarName", "Description" });
        }
        catch (Exception ex)
        {
            LoggingService.Log("DatabaseMigration", "RepairHebrewTextData failed", ex);
            _hebrewRepairDone = false;
        }
    }

    private static void RepairTableColumns(OleDbConnection conn, string table, string[] columns)
    {
        if (!TableExists(conn, table)) return;

        System.Collections.Generic.List<string> existing = new System.Collections.Generic.List<string>();
        foreach (string col in columns)
        {
            if (ColumnExists(conn, table, col))
                existing.Add(col);
        }
        if (existing.Count == 0) return;

        string idCol = ColumnExists(conn, table, "Id") ? "Id" : null;
        if (idCol == null) return;

        string selectList = idCol + ", " + string.Join(", ", existing.ConvertAll(c => "[" + c + "]").ToArray());
        string sql = "SELECT " + selectList + " FROM [" + table + "]";

        System.Data.DataTable dt = new System.Data.DataTable();
        using (OleDbDataAdapter adp = new OleDbDataAdapter(sql, conn))
            adp.Fill(dt);

        foreach (System.Data.DataRow row in dt.Rows)
        {
            bool changed = false;
            System.Collections.Generic.List<string> sets = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<object> vals = new System.Collections.Generic.List<object>();

            foreach (string col in existing)
            {
                string original = row[col] == DBNull.Value ? "" : Convert.ToString(row[col]);
                if (string.IsNullOrEmpty(original)) continue;
                string fixedText = Connect.FixEncoding(original);
                if (!string.Equals(original, fixedText, StringComparison.Ordinal))
                {
                    sets.Add("[" + col + "]=?");
                    vals.Add(fixedText);
                    changed = true;
                }
            }

            if (!changed) continue;

            string updateSql = "UPDATE [" + table + "] SET " + string.Join(", ", sets.ToArray()) + " WHERE [" + idCol + "]=?";
            using (OleDbCommand cmd = new OleDbCommand(updateSql, conn))
            {
                foreach (object v in vals)
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.WChar) { Value = v ?? "" });
                cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = Convert.ToInt32(row[idCol]) });
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void AddColumnIfMissing(OleDbConnection conn, string table, string column, string typeSql)
    {
        if (!TableExists(conn, table))
            return;
        if (ColumnExists(conn, table, column))
            return;
        try
        {
            ExecuteNonQuery(conn, string.Format("ALTER TABLE [{0}] ADD COLUMN [{1}] {2}", table, column, typeSql));
        }
        catch { }
    }

    /// <summary>
    /// Migrates Users table to DSD schema:
    /// - Rename password -> PasswordHash
    /// - Rename userId -> UserIdNumber
    /// - Rename phonenum -> PhoneNumber
    /// - Rename city -> CityId
    /// - Add CreatedDate if missing
    /// - Ensure GoogleId and Role exist
    /// </summary>
    private static void MigrateUsersTable(OleDbConnection conn)
    {
        // Check if Users table exists
        if (!TableExists(conn, "Users"))
        {
            // Create new Users table with DSD schema
            string createSql = @"
                CREATE TABLE Users (
                    Id AUTOINCREMENT PRIMARY KEY,
                    UserName TEXT,
                    FirstName TEXT,
                    LastName TEXT,
                    Email TEXT,
                    PasswordHash TEXT,
                    Gender INTEGER,
                    YearOfBirth INTEGER,
                    UserIdNumber TEXT,
                    PhoneNumber TEXT,
                    CityId INTEGER,
                    GoogleId TEXT,
                    Role TEXT,
                    CreatedDate DATETIME
                )";
            ExecuteNonQuery(conn, createSql);
            return;
        }

        // Rename columns if they exist with old names
        try
        {
            if (ColumnExists(conn, "Users", "password"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN PasswordHash TEXT");
                ExecuteNonQuery(conn, "UPDATE Users SET PasswordHash = [password] WHERE PasswordHash IS NULL");
                // Note: Access doesn't support DROP COLUMN easily, so we keep both for now
                // Old password column will be ignored in code
            }
        }
        catch { }

        try
        {
            if (ColumnExists(conn, "Users", "userId"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN UserIdNumber TEXT");
                ExecuteNonQuery(conn, "UPDATE Users SET UserIdNumber = [userId] WHERE UserIdNumber IS NULL");
            }
        }
        catch { }

        try
        {
            if (ColumnExists(conn, "Users", "phonenum"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN PhoneNumber TEXT");
                ExecuteNonQuery(conn, "UPDATE Users SET PhoneNumber = [phonenum] WHERE PhoneNumber IS NULL");
            }
        }
        catch { }

        try
        {
            if (ColumnExists(conn, "Users", "city"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN CityId INTEGER");
                ExecuteNonQuery(conn, "UPDATE Users SET CityId = [city] WHERE CityId IS NULL");
            }
        }
        catch { }

        // Ensure GoogleId exists
        try
        {
            if (!ColumnExists(conn, "Users", "GoogleId"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN GoogleId TEXT");
            }
        }
        catch { }

        // Ensure Role exists
        try
        {
            if (!ColumnExists(conn, "Users", "Role"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN Role TEXT");
                ExecuteNonQuery(conn, "UPDATE Users SET Role = 'user' WHERE Role IS NULL");
            }
        }
        catch { }

        // Ensure CreatedDate exists
        try
        {
            if (!ColumnExists(conn, "Users", "CreatedDate"))
            {
                ExecuteNonQuery(conn, "ALTER TABLE Users ADD COLUMN CreatedDate DATETIME");
            }
        }
        catch { }
    }

    /// <summary>
    /// Migrates calnder table to CalendarEvents with DSD schema
    /// </summary>
    private static void MigrateCalendarEventsTable(OleDbConnection conn)
    {
        // Check if old calnder table exists
        if (TableExists(conn, "calnder"))
        {
            // Create new CalendarEvents table if it doesn't exist
            if (!TableExists(conn, "CalendarEvents"))
            {
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
                ExecuteNonQuery(conn, createSql);

                // Migrate data from calnder to CalendarEvents
                string migrateSql = @"
                    INSERT INTO CalendarEvents (Id, UserId, Title, EventDate, EventTime, Notes, Category, CreatedDate)
                    SELECT Id, Userid, title, [date], [time], notes, category, Now() AS CreatedDate
                    FROM calnder
                    WHERE Userid IS NOT NULL";
                ExecuteNonQuery(conn, migrateSql);
            }
        }
        else if (!TableExists(conn, "CalendarEvents"))
        {
            // Create new CalendarEvents table if neither exists
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
            ExecuteNonQuery(conn, createSql);
        }
    }

    /// <summary>
    /// Ensures SharedCalendar tables match DSD schema (INTEGER types, correct column names)
    /// </summary>
    private static void EnsureSharedCalendarTables(OleDbConnection conn)
    {
        // SharedCalendars table
        if (!TableExists(conn, "SharedCalendars"))
        {
            string createSql = @"
                CREATE TABLE SharedCalendars (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Name TEXT(255),
                    Description MEMO,
                    CreatedBy INTEGER,
                    CreatedDate DATETIME
                )";
            ExecuteNonQuery(conn, createSql);
        }

        // SharedCalendarMembers table
        if (!TableExists(conn, "SharedCalendarMembers"))
        {
            string createSql = @"
                CREATE TABLE SharedCalendarMembers (
                    Id AUTOINCREMENT PRIMARY KEY,
                    CalendarId INTEGER,
                    UserId INTEGER,
                    Role TEXT(50),
                    JoinedDate DATETIME
                )";
            ExecuteNonQuery(conn, createSql);
        }

        // JoinRequests table
        if (!TableExists(conn, "JoinRequests"))
        {
            string createSql = @"
                CREATE TABLE JoinRequests (
                    Id AUTOINCREMENT PRIMARY KEY,
                    CalendarId INTEGER,
                    UserId INTEGER,
                    Status TEXT(50),
                    RequestDate DATETIME,
                    Message MEMO
                )";
            ExecuteNonQuery(conn, createSql);
        }

        // SharedCalendarEvents table - update column names if needed
        if (!TableExists(conn, "SharedCalendarEvents"))
        {
            string createSql = @"
                CREATE TABLE SharedCalendarEvents (
                    Id AUTOINCREMENT PRIMARY KEY,
                    CalendarId INTEGER,
                    Title TEXT(255),
                    EventDate DATETIME,
                    EventTime TEXT(50),
                    Notes MEMO,
                    Category TEXT(50),
                    CreatedBy INTEGER,
                    CreatedDate DATETIME
                )";
            ExecuteNonQuery(conn, createSql);
        }
        else
        {
            // Migrate Date -> EventDate, Time -> EventTime if needed
            try
            {
                if (ColumnExists(conn, "SharedCalendarEvents", "Date") && !ColumnExists(conn, "SharedCalendarEvents", "EventDate"))
                {
                    ExecuteNonQuery(conn, "ALTER TABLE SharedCalendarEvents ADD COLUMN EventDate DATETIME");
                    ExecuteNonQuery(conn, "UPDATE SharedCalendarEvents SET EventDate = [Date] WHERE EventDate IS NULL");
                }
            }
            catch { }

            try
            {
                if (ColumnExists(conn, "SharedCalendarEvents", "Time") && !ColumnExists(conn, "SharedCalendarEvents", "EventTime"))
                {
                    ExecuteNonQuery(conn, "ALTER TABLE SharedCalendarEvents ADD COLUMN EventTime TEXT(50)");
                    ExecuteNonQuery(conn, "UPDATE SharedCalendarEvents SET EventTime = [Time] WHERE EventTime IS NULL");
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Migrates OTPLoginCodes and VerificationCodes to unified AuthCodes table
    /// </summary>
    private static void MigrateAuthCodesTable(OleDbConnection conn)
    {
        // Create AuthCodes table
        if (!TableExists(conn, "AuthCodes"))
        {
            string createSql = @"
                CREATE TABLE AuthCodes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    UserId INTEGER,
                    Code TEXT,
                    CodeType TEXT,
                    ExpiryDate DATETIME,
                    Used YESNO,
                    AttemptCount INTEGER,
                    CreatedDate DATETIME
                )";
            ExecuteNonQuery(conn, createSql);
        }

        // Migrate OTPLoginCodes data (if table exists)
        if (TableExists(conn, "OTPLoginCodes"))
        {
            try
            {
                // Get users by email and insert into AuthCodes
                string migrateOTPSql = @"
                    INSERT INTO AuthCodes (UserId, Code, CodeType, ExpiryDate, Used, AttemptCount, CreatedDate)
                    SELECT 
                        U.Id AS UserId,
                        OLC.Code,
                        'OTP' AS CodeType,
                        OLC.ExpiryDate,
                        OLC.Used,
                        OLC.AttemptCount,
                        OLC.CreatedDate
                    FROM OTPLoginCodes OLC
                    INNER JOIN Users U ON CStr(OLC.Email) = CStr(U.Email)
                    WHERE OLC.ExpiryDate > Now() AND OLC.Used = 0";
                ExecuteNonQuery(conn, migrateOTPSql);
            }
            catch { }
        }

        // Migrate VerificationCodes data (if table exists)
        if (TableExists(conn, "VerificationCodes"))
        {
            try
            {
                string migrateVerificationSql = @"
                    INSERT INTO AuthCodes (UserId, Code, CodeType, ExpiryDate, Used, AttemptCount, CreatedDate)
                    SELECT 
                        U.Id AS UserId,
                        VC.Code,
                        'RESET' AS CodeType,
                        VC.ExpiryDate,
                        VC.Used,
                        0 AS AttemptCount,
                        VC.CreatedDate
                    FROM VerificationCodes VC
                    INNER JOIN Users U ON CStr(VC.Email) = CStr(U.Email)
                    WHERE VC.ExpiryDate > Now() AND VC.Used = 0";
                ExecuteNonQuery(conn, migrateVerificationSql);
            }
            catch { }
        }
    }

    // Helper methods
    private static bool TableExists(OleDbConnection conn, string tableName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand(string.Format("SELECT TOP 1 * FROM [{0}]", tableName), conn))
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

    // בדיקה אם עמודה קיימת בטבלה
    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand(string.Format("SELECT TOP 1 [{0}] FROM [{1}]", columnName, tableName), conn))
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

    // פונקציה: ExecuteNonQuery
    private static void ExecuteNonQuery(OleDbConnection conn, string sql)
    {
        using (OleDbCommand cmd = new OleDbCommand(sql, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }
}





