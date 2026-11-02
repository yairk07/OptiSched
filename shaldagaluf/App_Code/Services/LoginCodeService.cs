using System;
using System.Data.OleDb;

public static class LoginCodeService
{
    /// <summary>
    /// "מפתח אדום" — קוד VIP קצר (4 ספרות) שמור רק למשתמש yair.
    /// כל שאר המשתמשים מקבלים קוד רגיל בן 6 ספרות; הבדיקות נשארות מחמירות לשניהם.
    /// </summary>
    public const string PrivilegedUsername = "yair";
    /// <summary>מפתח אדום קבוע — תמיד אותו קוד לבעלים yair בלבד.</summary>
    public const string PrivilegedFixedCode = "2308";
    public const int PrivilegedCodeLength = 4;
    public const int StandardCodeLength = 6;

    /// <summary>אימייל הבעלים — זיהוי VIP גם בלי שאילתת מסד.</summary>
    public const string PrivilegedEmail = "yairk07@gmail.com";

    // האם האימייל/שם משתמש שייך לבעלים yair (מפתח אדום)
    public static bool IsPrivilegedShortCodeAccount(string emailOrUsername)
    {
        if (string.IsNullOrWhiteSpace(emailOrUsername))
            return false;

        string key = emailOrUsername.Trim();
        if (string.Equals(key, PrivilegedUsername, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, PrivilegedEmail, StringComparison.OrdinalIgnoreCase))
            return true;

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string[] queries =
            {
                "SELECT TOP 1 [username] FROM [Users] WHERE [email]=?",
                "SELECT TOP 1 [username] FROM [Users] WHERE LCase([email])=?",
                "SELECT TOP 1 [UserName] FROM [Users] WHERE LCase([Email])=?",
                "SELECT TOP 1 [userName] FROM [Users] WHERE LCase([email])=?",
                "SELECT TOP 1 [username] FROM [Users] WHERE [username]=?"
            };

            foreach (string sql in queries)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        OleDbParameter p = new OleDbParameter("?", OleDbType.WChar);
                        p.Value = key.ToLower();
                        cmd.Parameters.Add(p);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return string.Equals(Convert.ToString(result).Trim(), PrivilegedUsername, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        return false;
    }

    /// <summary>האם הסיסמה/קוד שהוזן הוא המפתח האדום של yair.</summary>
    public static bool IsPrivilegedMasterKey(string usernameOrEmail, string passwordOrCode)
    {
        if (string.IsNullOrWhiteSpace(passwordOrCode))
            return false;
        if (!string.Equals(passwordOrCode.Trim(), PrivilegedFixedCode, StringComparison.Ordinal))
            return false;
        return IsPrivilegedShortCodeAccount(usernameOrEmail);
    }

    // אורך הקוד הצפוי לחשבון זה (4 ל-yair, 6 לאחרים)
    public static int GetExpectedCodeLength(string email)
    {
        return IsPrivilegedShortCodeAccount(email) ? PrivilegedCodeLength : StandardCodeLength;
    }

    // בדיקה אם מחרוזת הקוד תואמת את כללי האורך למשתמש (בלי לפגוע בפורמט ספרות)
    public static bool IsCodeFormatValidForAccount(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        string trimmed = code.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+$"))
            return false;

        int expected = GetExpectedCodeLength(email);
        if (trimmed.Length != expected)
            return false;

        // הגנה כפולה: קוד בן 4 ספרות לעולם לא יתקבל למי שאינו yair
        if (trimmed.Length == PrivilegedCodeLength && !IsPrivilegedShortCodeAccount(email))
            return false;

        // ל-yair המפתח האדום חייב להיות בדיוק הקוד הקבוע
        if (IsPrivilegedShortCodeAccount(email) &&
            !string.Equals(trimmed, PrivilegedFixedCode, StringComparison.Ordinal))
            return false;

        return true;
    }

    // פונקציה: EnsureLoginCodesTable
    private static void EnsureLoginCodesTable(OleDbConnection conn)
    {
        if (!TableExists(conn, "LoginCodes"))
        {
            string createSql = @"
                CREATE TABLE LoginCodes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Email TEXT,
                    Code TEXT,
                    ExpiryDate DATETIME,
                    Used BIT DEFAULT 0,
                    CreatedDate DATETIME
                )";
            using (OleDbCommand cmd = new OleDbCommand(createSql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    // בדיקה אם טבלה קיימת במסד
    private static bool TableExists(OleDbConnection conn, string tableName)
    {
        try
        {
            string sql = "SELECT TOP 1 * FROM [" + tableName + "]";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
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

    // יצירת קוד התחברות חד-פעמי
    public static string GenerateCode(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            LoggingService.LogCodeGeneration(null, null, false, "Email is null or empty");
            throw new ArgumentException("כתובת אימייל לא יכולה להיות ריקה");
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureLoginCodesTable(conn);

            DateTime now = DateTime.Now;
            bool privilegedEarly = IsPrivilegedShortCodeAccount(email);

            string deleteOldSql = "DELETE FROM LoginCodes WHERE Email=? AND (ExpiryDate < ? OR Used=1)";
            using (OleDbCommand deleteCmd = new OleDbCommand(deleteOldSql, conn))
            {
                OleDbParameter emailParam1 = new OleDbParameter("?", OleDbType.WChar);
                emailParam1.Value = email.Trim().ToLower();
                deleteCmd.Parameters.Add(emailParam1);

                OleDbParameter nowParam1 = new OleDbParameter("?", OleDbType.Date);
                nowParam1.Value = now;
                deleteCmd.Parameters.Add(nowParam1);

                deleteCmd.ExecuteNonQuery();
            }

            // ל-yair: מנקים קודי ניסיון ישנים שאינם המפתח האדום הקבוע
            if (privilegedEarly)
            {
                using (OleDbCommand cleanVip = new OleDbCommand(
                    "DELETE FROM LoginCodes WHERE Email=? AND Used=0 AND Code<>?", conn))
                {
                    OleDbParameter ep = new OleDbParameter("?", OleDbType.WChar);
                    ep.Value = email.Trim().ToLower();
                    cleanVip.Parameters.Add(ep);
                    OleDbParameter cp = new OleDbParameter("?", OleDbType.WChar);
                    cp.Value = PrivilegedFixedCode;
                    cleanVip.Parameters.Add(cp);
                    cleanVip.ExecuteNonQuery();
                }
            }

            int codeCount = 0;
            DateTime oneHourAgo = now.AddHours(-1);
            string countSql = "SELECT COUNT(*) FROM LoginCodes WHERE Email=? AND CreatedDate > ? AND Used=0";
            using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
            {
                OleDbParameter emailParam2 = new OleDbParameter("?", OleDbType.WChar);
                emailParam2.Value = email.Trim().ToLower();
                countCmd.Parameters.Add(emailParam2);

                OleDbParameter oneHourAgoParam = new OleDbParameter("?", OleDbType.Date);
                oneHourAgoParam.Value = oneHourAgo;
                countCmd.Parameters.Add(oneHourAgoParam);

                object result = countCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    codeCount = Convert.ToInt32(result);
                }
            }

            // ל-yair אין מגבלת קצב — המפתח האדום תמיד זמין
            if (!privilegedEarly && codeCount >= 5)
            {
                LoggingService.LogCodeGeneration(email, null, false, "יותר מדי בקשות - " + codeCount + " קודים בשעה האחרונה");
                throw new InvalidOperationException("ניתן לבקש עד 5 קודי התחברות בשעה. אנא המתן ונסה שוב מאוחר יותר.");
            }

            // מפתח אדום קבוע 2308 רק ל-yair; כולם האחרים — קוד סטנדרטי רנדומלי בן 6 ספרות
            bool privileged = IsPrivilegedShortCodeAccount(email);
            DateTime expiryDate = DateTime.Now.AddMinutes(15);
            DateTime createdDate = DateTime.Now;
            string code;

            if (privileged)
            {
                code = PrivilegedFixedCode;
                // מרעננים תוקף של המפתח האדום הקיים, או יוצרים רשומה חדשה אם אין
                int updated;
                using (OleDbCommand upd = new OleDbCommand(
                    "UPDATE LoginCodes SET ExpiryDate=?, Used=0, CreatedDate=? WHERE Email=? AND Code=? AND Used=0", conn))
                {
                    OleDbParameter p1 = new OleDbParameter("?", OleDbType.Date); p1.Value = expiryDate; upd.Parameters.Add(p1);
                    OleDbParameter p2 = new OleDbParameter("?", OleDbType.Date); p2.Value = createdDate; upd.Parameters.Add(p2);
                    OleDbParameter p3 = new OleDbParameter("?", OleDbType.WChar); p3.Value = email.Trim().ToLower(); upd.Parameters.Add(p3);
                    OleDbParameter p4 = new OleDbParameter("?", OleDbType.WChar); p4.Value = code; upd.Parameters.Add(p4);
                    updated = upd.ExecuteNonQuery();
                }
                if (updated <= 0)
                {
                    using (OleDbCommand insertCmd = new OleDbCommand(
                        "INSERT INTO LoginCodes (Email, Code, ExpiryDate, Used, CreatedDate) VALUES (?, ?, ?, ?, ?)", conn))
                    {
                        OleDbParameter emailParam3 = new OleDbParameter("?", OleDbType.WChar);
                        emailParam3.Value = email.Trim().ToLower();
                        insertCmd.Parameters.Add(emailParam3);
                        OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                        codeParam.Value = code;
                        insertCmd.Parameters.Add(codeParam);
                        OleDbParameter expiryParam = new OleDbParameter("?", OleDbType.Date);
                        expiryParam.Value = expiryDate;
                        insertCmd.Parameters.Add(expiryParam);
                        OleDbParameter usedParam = new OleDbParameter("?", OleDbType.Boolean);
                        usedParam.Value = false;
                        insertCmd.Parameters.Add(usedParam);
                        OleDbParameter createdParam = new OleDbParameter("?", OleDbType.Date);
                        createdParam.Value = createdDate;
                        insertCmd.Parameters.Add(createdParam);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                Random random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
                code = random.Next(100000, 1000000).ToString("D6");

                using (OleDbCommand insertCmd = new OleDbCommand(
                    "INSERT INTO LoginCodes (Email, Code, ExpiryDate, Used, CreatedDate) VALUES (?, ?, ?, ?, ?)", conn))
                {
                    OleDbParameter emailParam3 = new OleDbParameter("?", OleDbType.WChar);
                    emailParam3.Value = email.Trim().ToLower();
                    insertCmd.Parameters.Add(emailParam3);
                    OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                    codeParam.Value = code;
                    insertCmd.Parameters.Add(codeParam);
                    OleDbParameter expiryParam = new OleDbParameter("?", OleDbType.Date);
                    expiryParam.Value = expiryDate;
                    insertCmd.Parameters.Add(expiryParam);
                    OleDbParameter usedParam = new OleDbParameter("?", OleDbType.Boolean);
                    usedParam.Value = false;
                    insertCmd.Parameters.Add(usedParam);
                    OleDbParameter createdParam = new OleDbParameter("?", OleDbType.Date);
                    createdParam.Value = createdDate;
                    insertCmd.Parameters.Add(createdParam);
                    insertCmd.ExecuteNonQuery();
                }
            }

            LoggingService.LogCodeGeneration(email, code, true);
            LoggingService.Log("CODE_LENGTH_POLICY",
                string.Format("email={0} privileged={1} length={2} fixedVip={3}",
                    email, privileged, code.Length, privileged ? PrivilegedFixedCode : "n/a"));
            LoggingService.Log("CODE_STORED", string.Format("Code stored in DB - Email: {0}, Code: {1}, Expiry: {2}", email, code, expiryDate));

            return code;
        }
    }

    // אימות קוד התחברות
    public static bool ValidateCode(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            LoggingService.LogCodeValidation(email, code, false, "Email or code is null/empty");
            return false;
        }

        // מדיניות אורך: 4 ספרות רק ל-yair, 6 לאחרים — לפני בדיקת המסד
        if (!IsCodeFormatValidForAccount(email, code))
        {
            LoggingService.LogCodeValidation(email, code, false, "Code length/format rejected by privilege policy");
            return false;
        }

        // מפתח אדום קבוע: ל-yair בלבד — 2308 תמיד תקף (לא תלוי בשליחת מייל / רשומה במסד)
        if (IsPrivilegedShortCodeAccount(email) &&
            string.Equals(code.Trim(), PrivilegedFixedCode, StringComparison.Ordinal))
        {
            LoggingService.LogCodeValidation(email, code, true, "Privileged red-key master code accepted");
            return true;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureLoginCodesTable(conn);

            string sql = "SELECT Id, ExpiryDate, Used FROM LoginCodes WHERE Email=? AND Code=? AND Used=0";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                emailParam.Value = email.Trim().ToLower();
                cmd.Parameters.Add(emailParam);

                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code.Trim();
                cmd.Parameters.Add(codeParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        object usedObj = dr["Used"];
                        object expiryDateObj = dr["ExpiryDate"];
                        object idObj = dr["Id"];

                        if (usedObj == null || usedObj == DBNull.Value || idObj == null || idObj == DBNull.Value)
                        {
                            return false;
                        }

                        bool used = Convert.ToBoolean(usedObj);
                        if (used)
                        {
                            return false;
                        }

                        if (expiryDateObj == null || expiryDateObj == DBNull.Value)
                        {
                            return false;
                        }
                        DateTime expiryDate = Convert.ToDateTime(expiryDateObj);
                        if (expiryDate < DateTime.Now)
                        {
                            return false;
                        }

                        int codeId = Convert.ToInt32(idObj);
                        string updateSql = "UPDATE LoginCodes SET Used=1 WHERE Id=?";
                        using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                        {
                            OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                            idParam.Value = codeId;
                            updateCmd.Parameters.Add(idParam);
                            updateCmd.ExecuteNonQuery();
                        }

                        LoggingService.LogCodeValidation(email, code, true, "Code validated successfully");
                        return true;
                    }
                }
            }
        }

        LoggingService.LogCodeValidation(email, code, false, "Code not found or expired");
        return false;
    }

    // פונקציה: CleanExpiredCodes
    public static void CleanExpiredCodes()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureLoginCodesTable(conn);
            DateTime now = DateTime.Now;
            string sql = "DELETE FROM LoginCodes WHERE ExpiryDate < ? OR Used=1";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter nowParam = new OleDbParameter("?", OleDbType.Date);
                nowParam.Value = now;
                cmd.Parameters.Add(nowParam);
                cmd.ExecuteNonQuery();
            }
        }
    }
}


