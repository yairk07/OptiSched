using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;

public class UsersService
{

    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        cmd.ExecuteScalar();
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // ------------------------------
    // INSERT USER
    // DSD Schema: Uses PasswordHash, UserIdNumber, PhoneNumber, CityId, CreatedDate
    // Supports both old and new column names for migration
    // ------------------------------
    public int insertIntoDB(string userName, string firstName, string lastName, string email, string password,
                int gender, int yearOfBirth, string userId, string phonenum, int city)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            bool hasPasswordHash = ColumnExists(myConnection, "Users", "PasswordHash");
            bool hasPassword = ColumnExists(myConnection, "Users", "password");
            bool hasUserIdNumber = ColumnExists(myConnection, "Users", "UserIdNumber");
            bool hasUserId = ColumnExists(myConnection, "Users", "userId");
            bool hasPhoneNumber = ColumnExists(myConnection, "Users", "PhoneNumber");
            bool hasPhonenum = ColumnExists(myConnection, "Users", "phonenum");
            bool hasCityId = ColumnExists(myConnection, "Users", "CityId");
            bool hasCity = ColumnExists(myConnection, "Users", "city");
            bool hasRole = ColumnExists(myConnection, "Users", "Role") || ColumnExists(myConnection, "Users", "role");
            bool hasCreatedDate = ColumnExists(myConnection, "Users", "CreatedDate");

            string passwordCol = hasPasswordHash ? "PasswordHash" : (hasPassword ? "[password]" : "PasswordHash");
            string userIdCol = hasUserIdNumber ? "UserIdNumber" : (hasUserId ? "userId" : "UserIdNumber");
            string phoneCol = hasPhoneNumber ? "PhoneNumber" : (hasPhonenum ? "phonenum" : "PhoneNumber");
            string cityCol = hasCityId ? "CityId" : (hasCity ? "city" : "CityId");
            string roleCol = ColumnExists(myConnection, "Users", "Role") ? "Role" : (ColumnExists(myConnection, "Users", "role") ? "role" : "Role");

            List<string> columns = new List<string> { "UserName", "FirstName", "LastName", "Email", passwordCol, "Gender", "YearOfBirth", userIdCol, phoneCol, cityCol };
            List<string> values = new List<string> { "?", "?", "?", "?", "?", "?", "?", "?", "?", "?" };
            
            if (hasRole)
            {
                columns.Add(roleCol);
                values.Add("?");
            }
            
            if (hasCreatedDate)
            {
                columns.Add("CreatedDate");
                values.Add("?");
            }

            string sSql = "INSERT INTO Users (" + string.Join(", ", columns) + ") VALUES(" + string.Join(", ", values) + ")";
            
            // #region agent log
            try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:SCHEMA_CHECK\",\"message\":\"Checking column schema\",\"data\":{\"hasPasswordHash\":\"" + hasPasswordHash + "\",\"hasPassword\":\"" + hasPassword + "\",\"hasUserIdNumber\":\"" + hasUserIdNumber + "\",\"hasUserId\":\"" + hasUserId + "\",\"hasPhoneNumber\":\"" + hasPhoneNumber + "\",\"hasPhonenum\":\"" + hasPhonenum + "\",\"hasCityId\":\"" + hasCityId + "\",\"hasCity\":\"" + hasCity + "\",\"hasRole\":\"" + hasRole + "\",\"hasCreatedDate\":\"" + hasCreatedDate + "\",\"passwordCol\":\"" + passwordCol + "\",\"userIdCol\":\"" + userIdCol + "\",\"phoneCol\":\"" + phoneCol + "\",\"cityCol\":\"" + cityCol + "\",\"roleCol\":\"" + roleCol + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:INSERT_SQL\",\"message\":\"INSERT SQL with schema detection\",\"data\":{\"sql\":\"" + sSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"placeholderCount\":" + values.Count + ",\"userName\":\"" + (userName ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            
            using (OleDbCommand cmd = new OleDbCommand(sSql, myConnection))
            {
                string hashedPassword = PasswordHelper.HashPassword(password);

                OleDbParameter userNameParam = new OleDbParameter("?", OleDbType.WChar);
                userNameParam.Value = userName != null ? userName.Trim() : "";
                cmd.Parameters.Add(userNameParam);
                
                OleDbParameter firstNameParam = new OleDbParameter("?", OleDbType.WChar);
                firstNameParam.Value = firstName != null ? firstName.Trim() : "";
                cmd.Parameters.Add(firstNameParam);
                
                OleDbParameter lastNameParam = new OleDbParameter("?", OleDbType.WChar);
                lastNameParam.Value = lastName != null ? lastName.Trim() : "";
                cmd.Parameters.Add(lastNameParam);
                
                OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                emailParam.Value = email != null ? email.Trim() : "";
                cmd.Parameters.Add(emailParam);
                
                OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                passwordParam.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam);
                
                OleDbParameter genderParam = new OleDbParameter("?", OleDbType.Integer);
                genderParam.Value = gender;
                cmd.Parameters.Add(genderParam);
                
                OleDbParameter yearOfBirthParam = new OleDbParameter("?", OleDbType.Integer);
                yearOfBirthParam.Value = yearOfBirth;
                cmd.Parameters.Add(yearOfBirthParam);
                
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.WChar);
                userIdParam.Value = userId != null ? userId.Trim() : "";
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter phonenumParam = new OleDbParameter("?", OleDbType.WChar);
                phonenumParam.Value = phonenum != null ? phonenum.Trim() : "";
                cmd.Parameters.Add(phonenumParam);
                
                OleDbParameter cityParam = new OleDbParameter("?", OleDbType.Integer);
                cityParam.Value = city;
                cmd.Parameters.Add(cityParam);
                
                if (hasRole)
                {
                    OleDbParameter roleParam = new OleDbParameter("?", OleDbType.WChar);
                    roleParam.Value = "user";
                    cmd.Parameters.Add(roleParam);
                }
                
                if (hasCreatedDate)
                {
                    OleDbParameter createdDateParam = new OleDbParameter("?", OleDbType.Date);
                    createdDateParam.Value = DateTime.Now;
                    cmd.Parameters.Add(createdDateParam);
                }

                // #region agent log
                try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:BEFORE_EXECUTE\",\"message\":\"Before ExecuteNonQuery\",\"data\":{\"paramCount\":\"" + cmd.Parameters.Count + "\",\"expectedCount\":\"" + values.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                // #endregion
                try
                {
                    cmd.ExecuteNonQuery();
                    // #region agent log
                    try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:EXECUTE_SUCCESS\",\"message\":\"ExecuteNonQuery success\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                    
                    cmd.CommandText = "SELECT @@IDENTITY";
                    object newId = cmd.ExecuteScalar();
                    if (newId != null && newId != DBNull.Value)
                    {
                        return Convert.ToInt32(newId);
                    }
                    return -1;
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:EXECUTE_ERROR\",\"message\":\"ExecuteNonQuery error\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\",\"sql\":\"" + sSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"paramCount\":\"" + cmd.Parameters.Count + "\",\"expectedCount\":\"" + values.Count + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                    
                    if (ex.Message.Contains("You must enter a value in the 'Users.id' field") || ex.Message.Contains("Users.id") || ex.Message.Contains("Id field"))
                    {
                        try
                        {
                            string getIdentitySql = "SELECT @@IDENTITY";
                            using (OleDbCommand identityCmd = new OleDbCommand(getIdentitySql, myConnection))
                            {
                                object identityResult = identityCmd.ExecuteScalar();
                                if (identityResult != null && identityResult != DBNull.Value)
                                {
                                    // #region agent log
                                    try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:IDENTITY_CHECK\",\"message\":\"Checking @@IDENTITY after failed insert\",\"data\":{\"identity\":\"" + identityResult.ToString() + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                                    // #endregion
                                }
                            }
                            
                            string idCol = ColumnExists(myConnection, "Users", "Id") ? "Id" : (ColumnExists(myConnection, "Users", "id") ? "id" : "Id");
                            string sSqlWithId = "INSERT INTO Users (" + idCol + ", " + string.Join(", ", columns) + ") VALUES(0, " + string.Join(", ", values) + ")";
                            
                            // #region agent log
                            try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:RETRY_WITH_ID\",\"message\":\"Retrying INSERT with Id=0\",\"data\":{\"sql\":\"" + sSqlWithId.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                            // #endregion
                            
                            using (OleDbCommand cmd2 = new OleDbCommand(sSqlWithId, myConnection))
                            {
                                foreach (OleDbParameter param in cmd.Parameters)
                                {
                                    OleDbParameter newParam = new OleDbParameter("?", param.OleDbType);
                                    newParam.Value = param.Value;
                                    cmd2.Parameters.Add(newParam);
                                }
                                
                                cmd2.ExecuteNonQuery();
                                // #region agent log
                                try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:RETRY_SUCCESS\",\"message\":\"Retry with Id=0 success\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                                // #endregion
                                
                                cmd2.CommandText = "SELECT @@IDENTITY";
                                object newId = cmd2.ExecuteScalar();
                                if (newId != null && newId != DBNull.Value)
                                {
                                    return Convert.ToInt32(newId);
                                }
                                return -1;
                            }
                        }
                        catch (Exception retryEx)
                        {
                            // #region agent log
                            try { LoggingService.Log("UsersService", "{\"location\":\"UsersService.insertIntoDB:RETRY_FAILED\",\"message\":\"Retry with Id=0 also failed\",\"data\":{\"error\":\"" + retryEx.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                            // #endregion
                            throw new Exception("Failed to insert user: " + ex.Message + " (Retry also failed: " + retryEx.Message + ")", ex);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            return -1;
        }
    }

    // ------------------------------
    // GET ALL USERS
    // ------------------------------
    public DataSet getallusers()
    {
        var data = new DataSet();
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            // DSD Schema: Use new column names (UserName, FirstName, LastName, Email, PhoneNumber, UserIdNumber, CityId, Role)
            using (var usersCmd = new OleDbCommand("SELECT * FROM [Users]", myConnection))
            {
                var usersAdp = new OleDbDataAdapter(usersCmd);
                var usersTable = new DataTable("Users");
                usersAdp.Fill(usersTable);

                foreach (DataRow row in usersTable.Rows)
                {
                    // Handle both old and new column names for backward compatibility during migration
                    if (row.Table.Columns.Contains("UserName") || row.Table.Columns.Contains("userName"))
                    {
                        string colName = row.Table.Columns.Contains("UserName") ? "UserName" : "userName";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                    if (row.Table.Columns.Contains("FirstName") || row.Table.Columns.Contains("firstName"))
                    {
                        string colName = row.Table.Columns.Contains("FirstName") ? "FirstName" : "firstName";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                    if (row.Table.Columns.Contains("LastName") || row.Table.Columns.Contains("lastName"))
                    {
                        string colName = row.Table.Columns.Contains("LastName") ? "LastName" : "lastName";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                    if (row.Table.Columns.Contains("Email") || row.Table.Columns.Contains("email"))
                    {
                        string colName = row.Table.Columns.Contains("Email") ? "Email" : "email";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                    if (row.Table.Columns.Contains("PhoneNumber") || row.Table.Columns.Contains("phonenum"))
                    {
                        string colName = row.Table.Columns.Contains("PhoneNumber") ? "PhoneNumber" : "phonenum";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                    if (row.Table.Columns.Contains("UserIdNumber") || row.Table.Columns.Contains("userId"))
                    {
                        string colName = row.Table.Columns.Contains("UserIdNumber") ? "UserIdNumber" : "userId";
                        row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
                    }
                }

                // Ensure Role column exists (DSD schema requires it)
                if (!usersTable.Columns.Contains("Role"))
                {
                    usersTable.Columns.Add("Role", typeof(string));
                    foreach (DataRow row in usersTable.Rows)
                    {
                        row["Role"] = "user";
                    }
                }

                using (var citiesCmd = new OleDbCommand("SELECT Id, CityName FROM [Citys]", myConnection))
                {
                    var citiesAdp = new OleDbDataAdapter(citiesCmd);
                    var citiesTable = new DataTable("Citys");
                    citiesAdp.Fill(citiesTable);

                    var dict = new Dictionary<int, string>();
                    foreach (DataRow r in citiesTable.Rows)
                    {
                        string idCol = r.Table.Columns.Contains("Id") ? "Id" : "id";
                        string nameCol = r.Table.Columns.Contains("CityName") ? "CityName" : "cityname";
                        int id;
                        if (int.TryParse(Convert.ToString(r[idCol]).Trim(), out id))
                            dict[id] = Connect.FixEncoding(Convert.ToString(r[nameCol]));
                    }

                    if (!usersTable.Columns.Contains("CityName"))
                        usersTable.Columns.Add("CityName", typeof(string));

                    foreach (DataRow u in usersTable.Rows)
                    {
                        // Handle both CityId (DSD) and city (old) column names
                        string cityCol = u.Table.Columns.Contains("CityId") ? "CityId" : "city";
                        if (u.Table.Columns.Contains(cityCol))
                        {
                            string raw = Convert.ToString(u[cityCol]).Trim();
                            int code;
                            if (int.TryParse(raw, out code) && dict.ContainsKey(code))
                                u["CityName"] = dict[code];
                            else
                                u["CityName"] = "";
                        }
                        else
                        {
                            u["CityName"] = "";
                        }
                    }
                }

                data.Tables.Add(usersTable);
            }
        }

        return data;
    }

    // ------------------------------
    // LOGIN CHECK
    // ------------------------------
    public bool IsExist(string userName, string password)
    {
        DataSet ds = new DataSet();
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            string hashedPassword = PasswordHelper.HashPassword(password);
            // DSD Schema: Use UserName and PasswordHash columns
            // Support both old and new column names during migration
            string sql = "SELECT * FROM Users WHERE (UserName=? OR userName=?) AND (PasswordHash=? OR [password]=?)";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter userNameParam1 = new OleDbParameter("?", OleDbType.WChar);
                userNameParam1.Value = userName != null ? userName.Trim() : "";
                cmd.Parameters.Add(userNameParam1);
                
                OleDbParameter userNameParam2 = new OleDbParameter("?", OleDbType.WChar);
                userNameParam2.Value = userName != null ? userName.Trim() : "";
                cmd.Parameters.Add(userNameParam2);
                
                OleDbParameter passwordParam1 = new OleDbParameter("?", OleDbType.WChar);
                passwordParam1.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam1);
                
                OleDbParameter passwordParam2 = new OleDbParameter("?", OleDbType.WChar);
                passwordParam2.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam2);

                var adp = new OleDbDataAdapter(cmd);
                adp.Fill(ds, "Users");
            }
        }

        return ds.Tables[0].Rows.Count > 0;
    }

    public DataRow GetUserByEmail(string email)
    {
        DataSet ds = new DataSet();
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            // DSD Schema: Use Email column (try new first, fallback to old during migration)
            string sql = "SELECT * FROM Users WHERE CStr(Email)=?";
            bool useOldColumn = false;
            
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
                {
                    OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                    emailParam.Value = email != null ? email.Trim() : "";
                    cmd.Parameters.Add(emailParam);

                    var adp = new OleDbDataAdapter(cmd);
                    adp.Fill(ds, "Users");
                }
            }
            catch
            {
                // Fallback to old column name during migration
                useOldColumn = true;
                sql = "SELECT * FROM Users WHERE CStr(email)=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
                {
                    OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                    emailParam.Value = email != null ? email.Trim() : "";
                    cmd.Parameters.Add(emailParam);

                    var adp = new OleDbDataAdapter(cmd);
                    adp.Fill(ds, "Users");
                }
            }
        }

        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            return ds.Tables[0].Rows[0];
        return null;
    }

    public void UpdatePassword(int userId, string newPassword)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            string hashedPassword = PasswordHelper.HashPassword(newPassword);
            
            bool hasPasswordHash = ColumnExists(myConnection, "Users", "PasswordHash");
            bool hasPassword = ColumnExists(myConnection, "Users", "password");
            
            List<string> updates = new List<string>();
            if (hasPasswordHash)
            {
                updates.Add("PasswordHash=?");
            }
            if (hasPassword)
            {
                updates.Add("[password]=?");
            }
            
            if (updates.Count == 0)
            {
                throw new InvalidOperationException("No password column found in Users table");
            }
            
            string sql = "UPDATE Users SET " + string.Join(", ", updates) + " WHERE Id=?";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                if (hasPasswordHash)
                {
                    OleDbParameter passwordHashParam = new OleDbParameter("?", OleDbType.WChar);
                    passwordHashParam.Value = hashedPassword ?? "";
                    cmd.Parameters.Add(passwordHashParam);
                }
                
                if (hasPassword)
                {
                    OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                    passwordParam.Value = hashedPassword ?? "";
                    cmd.Parameters.Add(passwordParam);
                }
                
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = userId;
                cmd.Parameters.Add(idParam);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public DataRow GetUserByGoogleId(string googleId)
    {
        DataSet ds = new DataSet();
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            // DSD Schema: GoogleId is a standard column, no dynamic creation needed
            string sql = "SELECT * FROM Users WHERE GoogleId=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter googleIdParam = new OleDbParameter("?", OleDbType.WChar);
                googleIdParam.Value = googleId != null ? googleId.Trim() : "";
                cmd.Parameters.Add(googleIdParam);
                var adp = new OleDbDataAdapter(cmd);
                adp.Fill(ds, "Users");
            }
        }

        if (ds.Tables[0].Rows.Count > 0)
        {
            DataRow row = ds.Tables[0].Rows[0];
            // DSD Schema: Handle both old and new column names
            if (row.Table.Columns.Contains("UserName") || row.Table.Columns.Contains("userName"))
            {
                string colName = row.Table.Columns.Contains("UserName") ? "UserName" : "userName";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("FirstName") || row.Table.Columns.Contains("firstName"))
            {
                string colName = row.Table.Columns.Contains("FirstName") ? "FirstName" : "firstName";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("LastName") || row.Table.Columns.Contains("lastName"))
            {
                string colName = row.Table.Columns.Contains("LastName") ? "LastName" : "lastName";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("Email") || row.Table.Columns.Contains("email"))
            {
                string colName = row.Table.Columns.Contains("Email") ? "Email" : "email";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("PhoneNumber") || row.Table.Columns.Contains("phonenum"))
            {
                string colName = row.Table.Columns.Contains("PhoneNumber") ? "PhoneNumber" : "phonenum";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("UserIdNumber") || row.Table.Columns.Contains("userId"))
            {
                string colName = row.Table.Columns.Contains("UserIdNumber") ? "UserIdNumber" : "userId";
                row[colName] = Connect.FixEncoding(Convert.ToString(row[colName]));
            }
            if (row.Table.Columns.Contains("GoogleId"))
                row["GoogleId"] = Connect.FixEncoding(Convert.ToString(row["GoogleId"]));
            return row;
        }
        return null;
    }

    public bool DeleteUser(int userId)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            // DSD Schema: Use Id column (standard)
            string sql = "DELETE FROM Users WHERE Id=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = userId;
                cmd.Parameters.Add(idParam);
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
    }
}
