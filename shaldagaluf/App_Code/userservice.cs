using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;

public class UsersService
{

    // ------------------------------
    // INSERT USER
    // DSD Schema: Uses PasswordHash, UserIdNumber, PhoneNumber, CityId, CreatedDate
    // ------------------------------
    public void insertIntoDB(string userName, string firstName, string lastName, string email, string password,
                int gender, int yearOfBirth, string userId, string phonenum, int city)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            // DSD Schema: PasswordHash, UserIdNumber, PhoneNumber, CityId, CreatedDate
            string sSql =
                "INSERT INTO Users (UserName, FirstName, LastName, Email, PasswordHash, Gender, YearOfBirth, UserIdNumber, PhoneNumber, CityId, Role, CreatedDate) " +
                "VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"UsersService.insertIntoDB:24\",\"message\":\"INSERT SQL\",\"data\":{\"sql\":\"" + sSql.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"placeholderCount\":12,\"userName\":\"" + (userName ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
            // #endregion
            using (OleDbCommand cmd = new OleDbCommand(sSql, myConnection))
            {
                string hashedPassword = PasswordHelper.HashPassword(password);

                OleDbParameter userNameParam = new OleDbParameter("?", OleDbType.WChar);
                userNameParam.Value = userName?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam);
                
                OleDbParameter firstNameParam = new OleDbParameter("?", OleDbType.WChar);
                firstNameParam.Value = firstName?.Trim() ?? "";
                cmd.Parameters.Add(firstNameParam);
                
                OleDbParameter lastNameParam = new OleDbParameter("?", OleDbType.WChar);
                lastNameParam.Value = lastName?.Trim() ?? "";
                cmd.Parameters.Add(lastNameParam);
                
                OleDbParameter emailParam = new OleDbParameter("?", OleDbType.WChar);
                emailParam.Value = email?.Trim() ?? "";
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
                userIdParam.Value = userId?.Trim() ?? "";
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter phonenumParam = new OleDbParameter("?", OleDbType.WChar);
                phonenumParam.Value = phonenum?.Trim() ?? "";
                cmd.Parameters.Add(phonenumParam);
                
                OleDbParameter cityParam = new OleDbParameter("?", OleDbType.Integer);
                cityParam.Value = city;
                cmd.Parameters.Add(cityParam);
                
                OleDbParameter roleParam = new OleDbParameter("?", OleDbType.WChar);
                roleParam.Value = "user";
                cmd.Parameters.Add(roleParam);
                
                OleDbParameter createdDateParam = new OleDbParameter("?", OleDbType.Date);
                createdDateParam.Value = DateTime.Now;
                cmd.Parameters.Add(createdDateParam);

                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"UsersService.insertIntoDB:78\",\"message\":\"Before ExecuteNonQuery\",\"data\":{\"paramCount\":12},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                // #endregion
                try
                {
                    cmd.ExecuteNonQuery();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"UsersService.insertIntoDB:81\",\"message\":\"ExecuteNonQuery success\",\"data\":{},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                }
                catch (Exception ex)
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", "{\"location\":\"UsersService.insertIntoDB:85\",\"message\":\"ExecuteNonQuery error\",\"data\":{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"type\":\"" + ex.GetType().Name + "\"},\"timestamp\":" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + "}\n"); } catch { }
                    // #endregion
                    throw;
                }
            }
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
                            if (int.TryParse(Convert.ToString(r[idCol]).Trim(), out int id))
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
                            if (int.TryParse(raw, out int code) && dict.ContainsKey(code))
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
                userNameParam1.Value = userName?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam1);
                
                OleDbParameter userNameParam2 = new OleDbParameter("?", OleDbType.WChar);
                userNameParam2.Value = userName?.Trim() ?? "";
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
                    emailParam.Value = email?.Trim() ?? "";
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
                    emailParam.Value = email?.Trim() ?? "";
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
            // DSD Schema: Use PasswordHash column (update both during migration)
            string sql = "UPDATE Users SET PasswordHash=?, [password]=? WHERE Id=?";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter passwordHashParam = new OleDbParameter("?", OleDbType.WChar);
                passwordHashParam.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordHashParam);
                
                OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                passwordParam.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam);
                
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
                googleIdParam.Value = googleId?.Trim() ?? "";
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
