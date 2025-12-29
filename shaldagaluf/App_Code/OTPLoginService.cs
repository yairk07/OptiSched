using System;
using System.Data;
using System.Data.OleDb;

public class OTPLoginService
{
    private static void EnsureOTPLoginCodesTable()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            try
            {
                string checkSql = "SELECT COUNT(*) FROM OTPLoginCodes WHERE 1=0";
                using (OleDbCommand checkCmd = new OleDbCommand(checkSql, conn))
                {
                    checkCmd.ExecuteScalar();
                }
            }
            catch
            {
                string createSql = @"
                    CREATE TABLE OTPLoginCodes (
                        Id AUTOINCREMENT PRIMARY KEY,
                        Email TEXT NOT NULL,
                        Code TEXT NOT NULL,
                        ExpiryDate DATETIME NOT NULL,
                        Used BIT DEFAULT 0,
                        CreatedDate DATETIME,
                        AttemptCount INT DEFAULT 0
                    )";
                using (OleDbCommand createCmd = new OleDbCommand(createSql, conn))
                {
                    createCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public static string GenerateLoginCode(string email)
    {
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run3",
                hypothesisId = "G",
                location = "OTPLoginService.cs:GenerateLoginCode:entry",
                message = "GenerateLoginCode entry",
                data = new {
                    email = email,
                    emailLength = email?.Length ?? 0
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch {}
        // #endregion agent log

        EnsureOTPLoginCodesTable();

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            DateTime now = DateTime.Now;
            string deleteOldSql = "DELETE FROM OTPLoginCodes WHERE CStr(Email)=? AND (ExpiryDate < ? OR Used=1)";
            try
            {
                using (OleDbCommand deleteCmd = new OleDbCommand(deleteOldSql, conn))
                {
                    OleDbParameter emailParam1 = new OleDbParameter("?", OleDbType.WChar);
                    emailParam1.Value = email?.Trim() ?? "";
                    deleteCmd.Parameters.Add(emailParam1);
                    
                    OleDbParameter nowParam1 = new OleDbParameter("?", OleDbType.Date);
                    nowParam1.Value = now;
                    deleteCmd.Parameters.Add(nowParam1);
                    
                    deleteCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "OTPLoginService.cs:GenerateLoginCode:delete_exception",
                        message = "Exception in DELETE query",
                        data = new {
                            error = ex.Message,
                            sql = deleteOldSql,
                            email = email
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer.Serialize(logData) + "\n");
                } catch {}
                // #endregion agent log
                throw;
            }

            int codeCount = 0;
            DateTime oneHourAgo = now.AddHours(-1);
            string countSql = "SELECT COUNT(*) FROM OTPLoginCodes WHERE CStr(Email)=? AND CreatedDate > ? AND Used=0";
            try
            {
                using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
                {
                    OleDbParameter emailParam2 = new OleDbParameter("?", OleDbType.WChar);
                    emailParam2.Value = email?.Trim() ?? "";
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
            }
            catch (Exception ex)
            {
                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "OTPLoginService.cs:GenerateLoginCode:count_exception",
                        message = "Exception in COUNT query",
                        data = new {
                            error = ex.Message,
                            sql = countSql,
                            email = email
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer.Serialize(logData) + "\n");
                } catch {}
                // #endregion agent log
                throw;
            }

            if (codeCount >= 5)
            {
                throw new Exception("ניתן לבקש עד 5 קודי התחברות בשעה. אנא המתן ונסה שוב מאוחר יותר.");
            }

            Random random = new Random();
            string code = random.Next(100000, 999999).ToString();
            DateTime expiryDate = DateTime.Now.AddMinutes(15);

            DateTime createdDate = DateTime.Now;
            string insertSql = "INSERT INTO OTPLoginCodes (Email, Code, ExpiryDate, Used, AttemptCount, CreatedDate) VALUES (?, ?, ?, ?, ?, ?)";
            try
            {
                using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
                {
                    OleDbParameter emailParam3 = new OleDbParameter("?", OleDbType.WChar);
                    emailParam3.Value = email?.Trim() ?? "";
                    insertCmd.Parameters.Add(emailParam3);
                    
                    OleDbParameter codeParam2 = new OleDbParameter("?", OleDbType.WChar);
                    codeParam2.Value = code?.Trim() ?? "";
                    insertCmd.Parameters.Add(codeParam2);
                    
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
            }
            catch (Exception ex)
            {
                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "OTPLoginService.cs:GenerateLoginCode:insert_exception",
                        message = "Exception in INSERT query",
                        data = new {
                            error = ex.Message,
                            sql = insertSql,
                            email = email
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer.Serialize(logData) + "\n");
                } catch {}
                // #endregion agent log
                throw;
            }

            return code;
        }
    }

    public static bool ValidateLoginCode(string email, string code)
    {
        EnsureOTPLoginCodesTable();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return false;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            // #region agent log
            try {
                var logData = new {
                    sessionId = "debug-session",
                    runId = "run3",
                    hypothesisId = "G",
                    location = "OTPLoginService.cs:ValidateLoginCode:before_query",
                    message = "Before executing ValidateLoginCode query",
                    data = new {
                        email = email,
                        code = code,
                        emailLength = email?.Length ?? 0,
                        codeLength = code?.Length ?? 0
                    },
                    timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                };
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                    serializer.Serialize(logData) + "\n");
            } catch {}
            // #endregion agent log

            string sql = "SELECT Id, ExpiryDate, Used, AttemptCount FROM OTPLoginCodes WHERE CStr(Email)=? AND CStr(Code)=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                emailParam.Value = email?.Trim() ?? "";
                cmd.Parameters.Add(emailParam);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                cmd.Parameters.Add(codeParam);

                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "OTPLoginService.cs:ValidateLoginCode:before_execute",
                        message = "Before ExecuteReader",
                        data = new {
                            sql = sql,
                            emailParamType = emailParam.OleDbType.ToString(),
                            emailParamValue = emailParam.Value?.ToString() ?? "null",
                            codeParamType = codeParam.OleDbType.ToString(),
                            codeParamValue = codeParam.Value?.ToString() ?? "null"
                        },
                        timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                    };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                        serializer.Serialize(logData) + "\n");
                } catch {}
                // #endregion agent log

                try
                {
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
                                string invalidateSql = "UPDATE OTPLoginCodes SET Used=1 WHERE Id=?";
                                using (OleDbCommand invalidateCmd = new OleDbCommand(invalidateSql, conn))
                                {
                                    OleDbParameter idParam1 = new OleDbParameter("?", OleDbType.Integer);
                                    idParam1.Value = Convert.ToInt32(dr["Id"]);
                                    invalidateCmd.Parameters.Add(idParam1);
                                    invalidateCmd.ExecuteNonQuery();
                                }
                                return false;
                            }

                            int id = Convert.ToInt32(dr["Id"]);
                            string updateSql = "UPDATE OTPLoginCodes SET Used=1 WHERE Id=?";
                            using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                            {
                                OleDbParameter idParam2 = new OleDbParameter("?", OleDbType.Integer);
                                idParam2.Value = id;
                                updateCmd.Parameters.Add(idParam2);
                                updateCmd.ExecuteNonQuery();
                            }

                            return true;
                        }
                        else
                        {
                            IncrementFailedAttempts(conn, email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try {
                        var logData = new {
                            sessionId = "debug-session",
                            runId = "run3",
                            hypothesisId = "G",
                            location = "OTPLoginService.cs:ValidateLoginCode:exception",
                            message = "Exception during ExecuteReader",
                            data = new {
                                error = ex.Message,
                                stackTrace = ex.StackTrace,
                                sql = sql
                            },
                            timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
                        };
                        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                            serializer.Serialize(logData) + "\n");
                    } catch {}
                    // #endregion agent log
                    throw;
                }
            }
        }

        return false;
    }

    private static void IncrementFailedAttempts(OleDbConnection conn, string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return;
        }
        
        DateTime now = DateTime.Now;
        string updateSql = "UPDATE OTPLoginCodes SET AttemptCount=AttemptCount+1 WHERE CStr(Email)=? AND Used=0 AND ExpiryDate > ?";
        using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
        {
            OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
            emailParam.Value = email?.Trim() ?? "";
            updateCmd.Parameters.Add(emailParam);
            
            OleDbParameter nowParam = new OleDbParameter("?", OleDbType.Date);
            nowParam.Value = now;
            updateCmd.Parameters.Add(nowParam);
            
            updateCmd.ExecuteNonQuery();
        }
    }

    public static void CleanExpiredCodes()
    {
        EnsureOTPLoginCodesTable();

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            DateTime now = DateTime.Now;
            string sql = "DELETE FROM OTPLoginCodes WHERE ExpiryDate < ? OR Used=1";
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

