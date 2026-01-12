using System;
using System.Data;
using System.Data.OleDb;

public static class LoginCodeService
{
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

            if (codeCount >= 5)
            {
                LoggingService.LogCodeGeneration(email, null, false, "יותר מדי בקשות - " + codeCount + " קודים בשעה האחרונה");
                throw new InvalidOperationException("ניתן לבקש עד 5 קודי התחברות בשעה. אנא המתן ונסה שוב מאוחר יותר.");
            }

            Random random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            string code = random.Next(100000, 999999).ToString();
            DateTime expiryDate = DateTime.Now.AddMinutes(15);
            DateTime createdDate = DateTime.Now;
            
            LoggingService.LogCodeGeneration(email, code, true);
            
            string insertSql = "INSERT INTO LoginCodes (Email, Code, ExpiryDate, Used, CreatedDate) VALUES (?, ?, ?, ?, ?)";
            using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
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

            LoggingService.Log("CODE_STORED", string.Format("Code stored in DB - Email: {0}, Code: {1}, Expiry: {2}", email, code, expiryDate));
            
            return code;
        }
    }

    public static bool ValidateCode(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            LoggingService.LogCodeValidation(email, code, false, "Email or code is null/empty");
            return false;
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

