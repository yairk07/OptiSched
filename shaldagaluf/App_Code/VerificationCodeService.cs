using System;
using System.Data;
using System.Data.OleDb;
using System.Linq;

public class VerificationCodeService
{
    private static void EnsureVerificationCodesTable()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            try
            {
                string checkSql = "SELECT COUNT(*) FROM VerificationCodes WHERE 1=0";
                using (OleDbCommand checkCmd = new OleDbCommand(checkSql, conn))
                {
                    checkCmd.ExecuteScalar();
                }
            }
            catch
            {
                string createSql = @"
                    CREATE TABLE VerificationCodes (
                        Id AUTOINCREMENT PRIMARY KEY,
                        Email TEXT NOT NULL,
                        Code TEXT NOT NULL,
                        ExpiryDate DATETIME NOT NULL,
                        Used BIT DEFAULT 0,
                        CreatedDate DATETIME
                    )";
                using (OleDbCommand createCmd = new OleDbCommand(createSql, conn))
                {
                    createCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public static string GenerateCode(string email)
    {
        EnsureVerificationCodesTable();

        Random random = new Random();
        string code = random.Next(100000, 999999).ToString();

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            DateTime now = DateTime.Now;
            string deleteOldSql = "DELETE FROM VerificationCodes WHERE CStr(Email)=? AND (ExpiryDate < ? OR Used=1)";
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

            int codeCount = 0;
            string countSql = "SELECT COUNT(*) FROM VerificationCodes WHERE CStr(Email)=? AND ExpiryDate > ? AND Used=0";
            using (OleDbCommand countCmd = new OleDbCommand(countSql, conn))
            {
                OleDbParameter emailParam2 = new OleDbParameter("?", OleDbType.WChar);
                emailParam2.Value = email?.Trim() ?? "";
                countCmd.Parameters.Add(emailParam2);
                
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

            DateTime expiryDate = DateTime.Now.AddMinutes(15);
            DateTime createdDate = DateTime.Now;
            string insertSql = "INSERT INTO VerificationCodes (Email, Code, ExpiryDate, Used, CreatedDate) VALUES (?, ?, ?, ?, ?)";
            using (OleDbCommand insertCmd = new OleDbCommand(insertSql, conn))
            {
                OleDbParameter emailParam3 = new OleDbParameter("?", OleDbType.WChar);
                emailParam3.Value = email?.Trim() ?? "";
                insertCmd.Parameters.Add(emailParam3);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
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

        return code;
    }

    public static bool ValidateCode(string email, string code)
    {
        EnsureVerificationCodesTable();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return false;
        }

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            

            DateTime now = DateTime.Now;
            string sql = "SELECT Id, ExpiryDate, Used FROM VerificationCodes WHERE CStr(Email)=? AND CStr(Code)=? AND Used=0 AND ExpiryDate > ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                emailParam.Value = email?.Trim() ?? "";
                cmd.Parameters.Add(emailParam);
                
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                cmd.Parameters.Add(codeParam);
                
                OleDbParameter nowParam = new OleDbParameter("?", OleDbType.Date);
                nowParam.Value = now;
                cmd.Parameters.Add(nowParam);

                

                try
                {
                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int id = Convert.ToInt32(dr["Id"]);
                            string updateSql = "UPDATE VerificationCodes SET Used=1 WHERE Id=?";
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
                catch (Exception ex)
                {
                    
                    throw;
                }
            }
        }

        return false;
    }

    public static void DeleteCode(string code)
    {
        EnsureVerificationCodesTable();

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "DELETE FROM VerificationCodes WHERE CStr(Code)=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter codeParam = new OleDbParameter("?", OleDbType.WChar);
                codeParam.Value = code?.Trim() ?? "";
                cmd.Parameters.Add(codeParam);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void CleanExpiredCodes()
    {
        EnsureVerificationCodesTable();

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            DateTime now = DateTime.Now;
            string sql = "DELETE FROM VerificationCodes WHERE ExpiryDate < ? OR Used=1";
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

