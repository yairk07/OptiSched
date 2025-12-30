using System;
using System.Data;
using System.Data.OleDb;
using System.Text;

/// <summary>
/// AuthCodeService - DSD Schema: Unified authentication codes service
/// Replaces OTPLoginService and VerificationCodeService
/// Table: AuthCodes
/// Columns: Id, UserId (FK -> Users.Id), Code, CodeType (OTP/RESET), ExpiryDate, Used, AttemptCount, CreatedDate
/// </summary>
public static class AuthCodeService
{
    /// <summary>
    /// Ensure AuthCodes table exists - creates it if missing
    /// </summary>
    private static void EnsureAuthCodesTable(OleDbConnection conn)
    {
        if (!TableExists(conn, "AuthCodes"))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.EnsureAuthCodesTable\",\"message\":\"Creating AuthCodes table\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            string createSql = @"
                CREATE TABLE AuthCodes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    UserId INTEGER,
                    Code TEXT,
                    CodeType TEXT,
                    ExpiryDate DATETIME,
                    Used BIT DEFAULT 0,
                    AttemptCount INTEGER,
                    CreatedDate DATETIME
                )";
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(createSql, conn))
                {
                    cmd.ExecuteNonQuery();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.EnsureAuthCodesTable\",\"message\":\"AuthCodes table created successfully\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.EnsureAuthCodesTable\",\"message\":\"Error creating AuthCodes table\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                // #endregion
                throw;
            }
        }
        else
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.EnsureAuthCodesTable\",\"message\":\"AuthCodes table already exists\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
        }
    }
    
    private static bool TableExists(OleDbConnection conn, string tableName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand($"SELECT TOP 1 * FROM [{tableName}]", conn))
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
    
    /// <summary>
    /// Generate OTP login code - DSD Schema: Uses AuthCodes table with UserId FK
    /// </summary>
    public static string GenerateLoginCode(int userId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureAuthCodesTable(conn);

            DateTime now = DateTime.Now;
            
            // Delete old codes for this user
            string deleteOldSql = "DELETE FROM AuthCodes WHERE UserId=? AND (ExpiryDate < ? OR Used=1) AND CodeType='OTP'";
            using (OleDbCommand deleteCmd = new OleDbCommand(deleteOldSql, conn))
            {
                OleDbParameter userIdParam1 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam1.Value = userId;
                deleteCmd.Parameters.Add(userIdParam1);
                
                OleDbParameter nowParam1 = new OleDbParameter("?", OleDbType.Date);
                nowParam1.Value = now;
                deleteCmd.Parameters.Add(nowParam1);
                
                deleteCmd.ExecuteNonQuery();
            }

            // Check code count (max 5 per hour)
            int codeCount = 0;
            DateTime oneHourAgo = now.AddHours(-1);
            string countSql = "SELECT COUNT(*) FROM AuthCodes WHERE UserId=? AND CreatedDate > ? AND Used=0 AND CodeType='OTP'";
            using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
            {
                OleDbParameter userIdParam2 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam2.Value = userId;
                countCmd.Parameters.Add(userIdParam2);
                
                OleDbParameter oneHourAgoParam = new OleDbParameter("?", OleDbType.Date);
                oneHourAgoParam.Value = oneHourAgo;
                countCmd.Parameters.Add(oneHourAgoParam);
                
                object result = countCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    codeCount = Convert.ToInt32(result);
                }
            }

            if (codeCount >= 5)
            {
                throw new Exception("ניתן לבקש עד 5 קודי התחברות בשעה. אנא המתן ונסה שוב מאוחר יותר.");
            }

            Random random = new Random();
            string code = random.Next(100000, 999999).ToString();
            DateTime expiryDate = DateTime.Now.AddMinutes(15);
            DateTime createdDate = DateTime.Now;
            
            // DSD Schema: Insert into AuthCodes with UserId FK
            string insertSql = "INSERT INTO AuthCodes (UserId, Code, CodeType, ExpiryDate, Used, AttemptCount, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?)";
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.GenerateLoginCode:72\",\"message\":\"INSERT SQL\",\"data\":{\"sql\":\"" + insertSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"placeholderCount\":7,\"userId\":" + userId + "},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
            {
                OleDbParameter userIdParam3 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam3.Value = userId;
                insertCmd.Parameters.Add(userIdParam3);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                insertCmd.Parameters.Add(codeParam);
                
                OleDbParameter codeTypeParam = new OleDbParameter("?", OleDbType.WChar);
                codeTypeParam.Value = "OTP";
                insertCmd.Parameters.Add(codeTypeParam);
                
                OleDbParameter expiryParam = new OleDbParameter("?", OleDbType.Date);
                expiryParam.Value = expiryDate;
                insertCmd.Parameters.Add(expiryParam);
                
                OleDbParameter usedParam = new OleDbParameter("?", OleDbType.Boolean);
                usedParam.Value = false;
                insertCmd.Parameters.Add(usedParam);
                
                OleDbParameter attemptParam = new OleDbParameter("?", OleDbType.Integer);
                attemptParam.Value = 0;
                insertCmd.Parameters.Add(attemptParam);
                
                OleDbParameter createdParam = new OleDbParameter("?", OleDbType.Date);
                createdParam.Value = createdDate;
                insertCmd.Parameters.Add(createdParam);
                
                // #region agent log
                try 
                { 
                    string paramDetails = "";
                    for (int i = 0; i < insertCmd.Parameters.Count; i++)
                    {
                        var p = insertCmd.Parameters[i];
                        paramDetails += $"param{i}:type={p.OleDbType},value={(p.Value?.ToString() ?? "null").Substring(0, Math.Min(50, p.Value?.ToString()?.Length ?? 0))};";
                    }
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.GenerateLoginCode:BeforeExecute\",\"message\":\"Before ExecuteNonQuery\",\"data\":{\"paramCount\":" + insertCmd.Parameters.Count + ",\"expectedCount\":7,\"sql\":\"" + insertSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"paramDetails\":\"" + paramDetails.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"code\":\"" + (code ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); 
                } 
                catch { }
                // #endregion
                try
                {
                    insertCmd.ExecuteNonQuery();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.GenerateLoginCode:106\",\"message\":\"ExecuteNonQuery success\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try 
                    { 
                        string paramDetails = "";
                        for (int i = 0; i < insertCmd.Parameters.Count; i++)
                        {
                            var p = insertCmd.Parameters[i];
                            paramDetails += $"param{i}:type={p.OleDbType},value={(p.Value?.ToString() ?? "null").Substring(0, Math.Min(50, p.Value?.ToString()?.Length ?? 0))};";
                        }
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"AuthCodeService.GenerateLoginCode:ExecuteError\",\"message\":\"ExecuteNonQuery error\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\",\"sql\":\"" + insertSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"paramCount\":" + insertCmd.Parameters.Count + ",\"paramDetails\":\"" + paramDetails.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); 
                    } 
                    catch { }
                    // #endregion
                    throw;
                }
            }

            return code;
        }
    }

    /// <summary>
    /// Validate OTP login code - DSD Schema: Uses AuthCodes table with UserId
    /// </summary>
    public static bool ValidateLoginCode(int userId, string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureAuthCodesTable(conn);

            // DSD Schema: Query AuthCodes by UserId and Code
            string sql = "SELECT Id, ExpiryDate, Used, AttemptCount FROM AuthCodes WHERE UserId=? AND Code=? AND CodeType='OTP'";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                userIdParam.Value = userId;
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                cmd.Parameters.Add(codeParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        bool used = Convert.ToBoolean(dr["Used"]);
                        DateTime expiryDate = Convert.ToDateTime(dr["ExpiryDate"]);
                        int attemptCount = Convert.ToInt32(dr["AttemptCount"]);

                        if (used)
                        {
                            return false;
                        }

                        if (expiryDate < DateTime.Now)
                        {
                            return false;
                        }

                        if (attemptCount >= 5)
                        {
                            int id = Convert.ToInt32(dr["Id"]);
                            string invalidateSql = "UPDATE AuthCodes SET Used=1 WHERE Id=?";
                            using (OleDbCommand invalidateCmd = new OleDbCommand(invalidateSql, conn))
                            {
                                OleDbParameter idParam1 = new OleDbParameter("?", OleDbType.Integer);
                                idParam1.Value = id;
                                invalidateCmd.Parameters.Add(idParam1);
                                invalidateCmd.ExecuteNonQuery();
                            }
                            return false;
                        }

                        int codeId = Convert.ToInt32(dr["Id"]);
                        string updateSql = "UPDATE AuthCodes SET Used=1 WHERE Id=?";
                        using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                        {
                            OleDbParameter idParam2 = new OleDbParameter("?", OleDbType.Integer);
                            idParam2.Value = codeId;
                            updateCmd.Parameters.Add(idParam2);
                            updateCmd.ExecuteNonQuery();
                        }

                        return true;
                    }
                    else
                    {
                        IncrementFailedAttempts(conn, userId);
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Generate password reset code - DSD Schema: Uses AuthCodes table with UserId FK
    /// </summary>
    public static string GenerateResetCode(int userId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureAuthCodesTable(conn);

            DateTime now = DateTime.Now;
            
            // Delete old codes for this user
            string deleteOldSql = "DELETE FROM AuthCodes WHERE UserId=? AND (ExpiryDate < ? OR Used=1) AND CodeType='RESET'";
            using (OleDbCommand deleteCmd = new OleDbCommand(deleteOldSql, conn))
            {
                OleDbParameter userIdParam1 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam1.Value = userId;
                deleteCmd.Parameters.Add(userIdParam1);
                
                OleDbParameter nowParam1 = new OleDbParameter("?", OleDbType.Date);
                nowParam1.Value = now;
                deleteCmd.Parameters.Add(nowParam1);
                
                deleteCmd.ExecuteNonQuery();
            }

            // Check code count (max 3 per hour)
            int codeCount = 0;
            string countSql = "SELECT COUNT(*) FROM AuthCodes WHERE UserId=? AND ExpiryDate > ? AND Used=0 AND CodeType='RESET'";
            using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
            {
                OleDbParameter userIdParam2 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam2.Value = userId;
                countCmd.Parameters.Add(userIdParam2);
                
                OleDbParameter nowParam2 = new OleDbParameter("?", OleDbType.Date);
                nowParam2.Value = now;
                countCmd.Parameters.Add(nowParam2);
                
                object result = countCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    codeCount = Convert.ToInt32(result);
                }
            }

            if (codeCount >= 3)
            {
                throw new Exception("ניתן ליצור מקסימום 3 קודי אימות לשעה. אנא נסה שוב מאוחר יותר.");
            }

            Random random = new Random();
            string code = random.Next(100000, 999999).ToString();
            DateTime expiryDate = DateTime.Now.AddMinutes(15);
            DateTime createdDate = DateTime.Now;
            
            // DSD Schema: Insert into AuthCodes with CodeType='RESET'
            string insertSql = "INSERT INTO AuthCodes (UserId, Code, CodeType, ExpiryDate, Used, AttemptCount, CreatedDate) VALUES (?, ?, ?, ?, ?, ?, ?)";
            using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
            {
                OleDbParameter userIdParam3 = new OleDbParameter("?", OleDbType.Integer);
                userIdParam3.Value = userId;
                insertCmd.Parameters.Add(userIdParam3);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                insertCmd.Parameters.Add(codeParam);
                
                OleDbParameter codeTypeParam = new OleDbParameter("?", OleDbType.WChar);
                codeTypeParam.Value = "RESET";
                insertCmd.Parameters.Add(codeTypeParam);
                
                OleDbParameter expiryParam = new OleDbParameter("?", OleDbType.Date);
                expiryParam.Value = expiryDate;
                insertCmd.Parameters.Add(expiryParam);
                
                OleDbParameter usedParam = new OleDbParameter("?", OleDbType.Boolean);
                usedParam.Value = false;
                insertCmd.Parameters.Add(usedParam);
                
                OleDbParameter attemptParam = new OleDbParameter("?", OleDbType.Integer);
                attemptParam.Value = 0;
                insertCmd.Parameters.Add(attemptParam);
                
                OleDbParameter createdParam = new OleDbParameter("?", OleDbType.Date);
                createdParam.Value = createdDate;
                insertCmd.Parameters.Add(createdParam);
                
                insertCmd.ExecuteNonQuery();
            }
            
            return code;
        }
    }

    /// <summary>
    /// Validate password reset code - DSD Schema: Uses AuthCodes table with UserId
    /// </summary>
    public static bool ValidateResetCode(int userId, string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureAuthCodesTable(conn);

            DateTime now = DateTime.Now;
            // DSD Schema: Query AuthCodes by UserId, Code, and CodeType='RESET'
            string sql = "SELECT Id, ExpiryDate, Used FROM AuthCodes WHERE UserId=? AND Code=? AND Used=0 AND ExpiryDate > ? AND CodeType='RESET'";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                userIdParam.Value = userId;
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                cmd.Parameters.Add(codeParam);
                
                OleDbParameter nowParam = new OleDbParameter("?", OleDbType.Date);
                nowParam.Value = now;
                cmd.Parameters.Add(nowParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int id = Convert.ToInt32(dr["Id"]);
                        string updateSql = "UPDATE AuthCodes SET Used=1 WHERE Id=?";
                        using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                        {
                            OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                            idParam.Value = id;
                            updateCmd.Parameters.Add(idParam);
                            updateCmd.ExecuteNonQuery();
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Increment failed attempt count for OTP codes
    /// </summary>
    private static void IncrementFailedAttempts(OleDbConnection conn, int userId)
    {
        DateTime now = DateTime.Now;
        string updateSql = "UPDATE AuthCodes SET AttemptCount=AttemptCount+1 WHERE UserId=? AND Used=0 AND ExpiryDate > ? AND CodeType='OTP'";
        using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
        {
            OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
            userIdParam.Value = userId;
            updateCmd.Parameters.Add(userIdParam);
            
            OleDbParameter nowParam = new OleDbParameter("?", OleDbType.Date);
            nowParam.Value = now;
            updateCmd.Parameters.Add(nowParam);
            
            updateCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Clean expired codes - DSD Schema: Uses AuthCodes table
    /// </summary>
    public static void CleanExpiredCodes()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureAuthCodesTable(conn);
            DateTime now = DateTime.Now;
            string sql = "DELETE FROM AuthCodes WHERE ExpiryDate < ? OR Used=1";
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

