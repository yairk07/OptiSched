using System;
using System.Data;
using System.Data.OleDb;

public static class LoginCodeService
{
    private static void EnsureLoginCodesTable(OleDbConnection conn)
    {
        LoggingService.Log("ENSURE_TABLE_START", "Checking if LoginCodes table exists");
        
        bool tableExists = TableExists(conn, "LoginCodes");
        LoggingService.Log("ENSURE_TABLE_CHECK", string.Format("Table exists check result: {0}", tableExists));
        
        if (!tableExists)
        {
            LoggingService.Log("ENSURE_TABLE_CREATE", "Creating LoginCodes table");
            
            string createSql = @"
                CREATE TABLE LoginCodes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Email TEXT,
                    Code TEXT,
                    ExpiryDate DATETIME,
                    Used BIT DEFAULT 0,
                    CreatedDate DATETIME
                )";
            
            LoggingService.Log("ENSURE_TABLE_SQL", string.Format("CREATE TABLE SQL: {0}", createSql));
            
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(createSql, conn))
                {
                    cmd.ExecuteNonQuery();
                    LoggingService.Log("ENSURE_TABLE_SUCCESS", "LoginCodes table created successfully");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("ENSURE_TABLE_ERROR", string.Format("Failed to create LoginCodes table - Error: {0}", ex.Message), ex);
                throw;
            }
        }
        else
        {
            LoggingService.Log("ENSURE_TABLE_EXISTS", "LoginCodes table already exists");
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

        LoggingService.Log("GENERATE_CODE_START", string.Format("Starting GenerateCode - Email: {0}", email));

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            try
            {
                conn.Open();
                LoggingService.Log("GENERATE_CODE_CONNECTION", "Connection opened successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Log("GENERATE_CODE_CONNECTION_ERROR", string.Format("Failed to open connection - Error: {0}", ex.Message), ex);
                throw;
            }

            EnsureLoginCodesTable(conn);
            LoggingService.Log("GENERATE_CODE_TABLE_CHECK", "LoginCodes table ensured");

            DateTime now = DateTime.Now;
            
            try
            {
                string deleteOldSql = "DELETE FROM [LoginCodes] WHERE [Email]=? AND ([ExpiryDate] < ? OR [Used]=1)";
                LoggingService.Log("GENERATE_CODE_DELETE", string.Format("Executing DELETE - SQL: {0}, Email: {1}, Now: {2}", deleteOldSql, email.Trim().ToLower(), now));
                
                using (OleDbCommand deleteCmd = new OleDbCommand(deleteOldSql, conn))
                {
                    deleteCmd.Parameters.AddWithValue("?", email.Trim().ToLower());
                    deleteCmd.Parameters.AddWithValue("?", now);
                    LoggingService.Log("GENERATE_CODE_DELETE_PARAMS", string.Format("DELETE params - Param1: {0}, Param2: {1}, ParamCount: {2}", email.Trim().ToLower(), now, deleteCmd.Parameters.Count));
                    
                    int deleted = deleteCmd.ExecuteNonQuery();
                    LoggingService.Log("GENERATE_CODE_DELETE_SUCCESS", string.Format("DELETE successful - Rows deleted: {0}", deleted));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("GENERATE_CODE_DELETE_ERROR", string.Format("DELETE failed - Error: {0}", ex.Message), ex);
                throw;
            }

            int codeCount = 0;
            DateTime oneHourAgo = now.AddHours(-1);
            
            try
            {
                string countSql = "SELECT COUNT(*) FROM [LoginCodes] WHERE [Email]=? AND [CreatedDate] > ? AND [Used]=0";
                LoggingService.Log("GENERATE_CODE_COUNT", string.Format("Executing COUNT - SQL: {0}, Email: {1}, OneHourAgo: {2}", countSql, email.Trim().ToLower(), oneHourAgo));
                
                using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
                {
                    countCmd.Parameters.AddWithValue("?", email.Trim().ToLower());
                    countCmd.Parameters.AddWithValue("?", oneHourAgo);
                    LoggingService.Log("GENERATE_CODE_COUNT_PARAMS", string.Format("COUNT params - Param1: {0}, Param2: {1}, ParamCount: {2}", email.Trim().ToLower(), oneHourAgo, countCmd.Parameters.Count));
                    
                    object result = countCmd.ExecuteScalar();
                    LoggingService.Log("GENERATE_CODE_COUNT_RESULT", string.Format("COUNT result - Raw: {0}, Type: {1}", result, result != null ? result.GetType().Name : "NULL"));
                    
                    if (result != null && result != DBNull.Value)
                    {
                        codeCount = Convert.ToInt32(result);
                        LoggingService.Log("GENERATE_CODE_COUNT_SUCCESS", string.Format("COUNT successful - CodeCount: {0}", codeCount));
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("GENERATE_CODE_COUNT_ERROR", string.Format("COUNT failed - Error: {0}", ex.Message), ex);
                throw;
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
            
            try
            {
                string insertSql = "INSERT INTO [LoginCodes] ([Email], [Code], [ExpiryDate], [Used], [CreatedDate]) VALUES (?, ?, ?, ?, ?)";
                LoggingService.Log("GENERATE_CODE_INSERT", string.Format("Executing INSERT - SQL: {0}, Email: {1}, Code: {2}, ExpiryDate: {3}, Used: {4}, CreatedDate: {5}", 
                    insertSql, email.Trim().ToLower(), code, expiryDate, false, createdDate));
                
                using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("?", email.Trim().ToLower());
                    insertCmd.Parameters.AddWithValue("?", code);
                    insertCmd.Parameters.AddWithValue("?", expiryDate);
                    insertCmd.Parameters.AddWithValue("?", false);
                    insertCmd.Parameters.AddWithValue("?", createdDate);
                    
                    LoggingService.Log("GENERATE_CODE_INSERT_PARAMS", string.Format("INSERT params - ParamCount: {0}, Param1: {1}, Param2: {2}, Param3: {3}, Param4: {4}, Param5: {5}", 
                        insertCmd.Parameters.Count, email.Trim().ToLower(), code, expiryDate, false, createdDate));
                    
                    insertCmd.ExecuteNonQuery();
                    LoggingService.Log("GENERATE_CODE_INSERT_SUCCESS", "INSERT successful");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("GENERATE_CODE_INSERT_ERROR", string.Format("INSERT failed - Error: {0}, StackTrace: {1}", ex.Message, ex.StackTrace), ex);
                throw;
            }

            LoggingService.Log("CODE_STORED", string.Format("Code stored in DB - Email: {0}, Code: {1}, Expiry: {2}", email, code, expiryDate));
            
            return code;
        }
    }

    public static bool ValidateCode(string email, string code)
    {
        LoggingService.Log("VALIDATE_CODE_START", string.Format("Starting ValidateCode - Email: {0}, Code: {1}", email, code));
        
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            LoggingService.LogCodeValidation(email, code, false, "Email or code is null/empty");
            return false;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            try
            {
                conn.Open();
                LoggingService.Log("VALIDATE_CODE_CONNECTION", "Connection opened successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Log("VALIDATE_CODE_CONNECTION_ERROR", string.Format("Failed to open connection - Error: {0}", ex.Message), ex);
                return false;
            }
            
            EnsureLoginCodesTable(conn);

            try
            {
                string sql = "SELECT [Id], [ExpiryDate], [Used] FROM [LoginCodes] WHERE [Email]=? AND [Code]=? AND [Used]=0";
                LoggingService.Log("VALIDATE_CODE_QUERY", string.Format("Executing SELECT - SQL: {0}, Email: {1}, Code: {2}", sql, email.Trim().ToLower(), code.Trim()));
                
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", email.Trim().ToLower());
                    cmd.Parameters.AddWithValue("?", code.Trim());
                    LoggingService.Log("VALIDATE_CODE_PARAMS", string.Format("SELECT params - ParamCount: {0}, Param1: {1}, Param2: {2}", cmd.Parameters.Count, email.Trim().ToLower(), code.Trim()));

                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        LoggingService.Log("VALIDATE_CODE_READER", "DataReader created, reading data");
                        
                        if (dr.Read())
                        {
                            LoggingService.Log("VALIDATE_CODE_READ", "DataReader read successful");
                            
                            object usedObj = dr["Used"];
                            object expiryDateObj = dr["ExpiryDate"];
                            object idObj = dr["Id"];
                            
                            LoggingService.Log("VALIDATE_CODE_VALUES", string.Format("Read values - Used: {0}, ExpiryDate: {1}, Id: {2}", usedObj, expiryDateObj, idObj));
                            
                            if (usedObj == null || usedObj == DBNull.Value || idObj == null || idObj == DBNull.Value)
                            {
                                LoggingService.Log("VALIDATE_CODE_NULL", "One or more values are null");
                                return false;
                            }
                            
                            bool used = Convert.ToBoolean(usedObj);
                            if (used)
                            {
                                LoggingService.Log("VALIDATE_CODE_USED", "Code already used");
                                return false;
                            }

                            if (expiryDateObj == null || expiryDateObj == DBNull.Value)
                            {
                                LoggingService.Log("VALIDATE_CODE_EXPIRY_NULL", "ExpiryDate is null");
                                return false;
                            }
                            DateTime expiryDate = Convert.ToDateTime(expiryDateObj);
                            if (expiryDate < DateTime.Now)
                            {
                                LoggingService.Log("VALIDATE_CODE_EXPIRED", string.Format("Code expired - ExpiryDate: {0}, Now: {1}", expiryDate, DateTime.Now));
                                return false;
                            }

                            int codeId = Convert.ToInt32(idObj);
                            LoggingService.Log("VALIDATE_CODE_UPDATE", string.Format("Updating code to used - CodeId: {0}", codeId));
                            
                            string updateSql = "UPDATE [LoginCodes] SET [Used]=1 WHERE [Id]=?";
                            using (OleDbCommand updateCmd = new OleDbCommand(updateSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("?", codeId);
                                LoggingService.Log("VALIDATE_CODE_UPDATE_PARAMS", string.Format("UPDATE params - ParamCount: {0}, Param1: {1}", updateCmd.Parameters.Count, codeId));
                                
                                updateCmd.ExecuteNonQuery();
                                LoggingService.Log("VALIDATE_CODE_UPDATE_SUCCESS", "UPDATE successful");
                            }

                            LoggingService.LogCodeValidation(email, code, true, "Code validated successfully");
                            return true;
                        }
                        else
                        {
                            LoggingService.Log("VALIDATE_CODE_NOT_FOUND", "Code not found in database");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("VALIDATE_CODE_ERROR", string.Format("ValidateCode failed - Error: {0}, StackTrace: {1}", ex.Message, ex.StackTrace), ex);
                return false;
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
                cmd.Parameters.AddWithValue("?", now);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

