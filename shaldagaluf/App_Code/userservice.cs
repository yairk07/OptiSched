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
    // ------------------------------
    public void insertIntoDB(string userName, string firstName, string lastName, string email, string password,
                int gender, int yearOfBirth, string userId, string phonenum, int city)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            string sSql =
                "INSERT INTO Users (userName, firstName, lastName, email, [password], gender, yearOfBirth, userId, phonenum, city) " +
                "VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
                
                cmd.Parameters.AddWithValue("?", gender);
                cmd.Parameters.AddWithValue("?", yearOfBirth);
                
                OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.WChar);
                userIdParam.Value = userId?.Trim() ?? "";
                cmd.Parameters.Add(userIdParam);
                
                OleDbParameter phonenumParam = new OleDbParameter("?", OleDbType.WChar);
                phonenumParam.Value = phonenum?.Trim() ?? "";
                cmd.Parameters.Add(phonenumParam);
                
                cmd.Parameters.AddWithValue("?", city);

                cmd.ExecuteNonQuery();
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

                using (var usersCmd = new OleDbCommand("SELECT * FROM [Users]", myConnection))
                {
                    var usersAdp = new OleDbDataAdapter(usersCmd);
                    var usersTable = new DataTable("Users");
                    usersAdp.Fill(usersTable);

                    foreach (DataRow row in usersTable.Rows)
                    {
                        if (row.Table.Columns.Contains("userName"))
                            row["userName"] = Connect.FixEncoding(Convert.ToString(row["userName"]));
                        if (row.Table.Columns.Contains("firstName"))
                            row["firstName"] = Connect.FixEncoding(Convert.ToString(row["firstName"]));
                        if (row.Table.Columns.Contains("lastName"))
                            row["lastName"] = Connect.FixEncoding(Convert.ToString(row["lastName"]));
                        if (row.Table.Columns.Contains("email"))
                            row["email"] = Connect.FixEncoding(Convert.ToString(row["email"]));
                        if (row.Table.Columns.Contains("phonenum"))
                            row["phonenum"] = Connect.FixEncoding(Convert.ToString(row["phonenum"]));
                        if (row.Table.Columns.Contains("userId"))
                            row["userId"] = Connect.FixEncoding(Convert.ToString(row["userId"]));
                    }

                    string roleCol = usersTable.Columns
                        .Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .FirstOrDefault(name => name.Trim().ToLower() == "role");

                    if (roleCol == null)
                        usersTable.Columns.Add("Role", typeof(string));
                    else if (roleCol != "Role")
                        usersTable.Columns[roleCol].ColumnName = "Role";


                    using (var citiesCmd = new OleDbCommand("SELECT id, cityname FROM [Citys]", myConnection))
                    {
                        var citiesAdp = new OleDbDataAdapter(citiesCmd);
                        var citiesTable = new DataTable("Citys");
                        citiesAdp.Fill(citiesTable);

                        var dict = new Dictionary<int, string>();
                        foreach (DataRow r in citiesTable.Rows)
                        {
                            if (int.TryParse(Convert.ToString(r["id"]).Trim(), out int id))
                                dict[id] = Connect.FixEncoding(Convert.ToString(r["cityname"]));
                        }

                    if (!usersTable.Columns.Contains("CityName"))
                        usersTable.Columns.Add("CityName", typeof(string));

                    foreach (DataRow u in usersTable.Rows)
                    {
                        string raw = Convert.ToString(u["city"]).Trim();
                        if (int.TryParse(raw, out int code) && dict.ContainsKey(code))
                            u["CityName"] = dict[code];
                        else
                            u["CityName"] = "";
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
            string sql = "SELECT * FROM Users WHERE userName=? AND [password]=?";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter userNameParam = new OleDbParameter("?", OleDbType.WChar);
                userNameParam.Value = userName?.Trim() ?? "";
                cmd.Parameters.Add(userNameParam);
                
                OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                passwordParam.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam);

                var adp = new OleDbDataAdapter(cmd);
                adp.Fill(ds, "Users");
            }
        }

        return ds.Tables[0].Rows.Count > 0;
    }

    public DataRow GetUserByEmail(string email)
    {
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run3",
                hypothesisId = "G",
                location = "userservice.cs:GetUserByEmail:entry",
                message = "GetUserByEmail entry",
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

        DataSet ds = new DataSet();
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection myConnection = new OleDbConnection(connectionString))
        {
            myConnection.Open();

            string sql = "SELECT * FROM Users WHERE CStr(email)=?";
            
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
            catch (Exception ex)
            {
                // #region agent log
                try {
                    var logData = new {
                        sessionId = "debug-session",
                        runId = "run3",
                        hypothesisId = "G",
                        location = "userservice.cs:GetUserByEmail:exception",
                        message = "Exception in GetUserByEmail query",
                        data = new {
                            error = ex.Message,
                            sql = sql,
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
        }

        if (ds.Tables[0].Rows.Count > 0)
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
            string sql = "UPDATE Users SET [password]=? WHERE id=?";
            
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                OleDbParameter passwordParam = new OleDbParameter("?", OleDbType.WChar);
                passwordParam.Value = hashedPassword ?? "";
                cmd.Parameters.Add(passwordParam);
                
                cmd.Parameters.AddWithValue("?", userId);

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

            try
            {
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
            catch
            {
                GoogleOAuthService.EnsureGoogleIdColumn();
                string sql = "SELECT * FROM Users WHERE GoogleId=?";
                using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
                {
                    OleDbParameter googleIdParam2 = new OleDbParameter("?", OleDbType.WChar);
                    googleIdParam2.Value = googleId?.Trim() ?? "";
                    cmd.Parameters.Add(googleIdParam2);
                    var adp = new OleDbDataAdapter(cmd);
                    adp.Fill(ds, "Users");
                }
            }
        }

        if (ds.Tables[0].Rows.Count > 0)
        {
            DataRow row = ds.Tables[0].Rows[0];
            if (row.Table.Columns.Contains("userName"))
                row["userName"] = Connect.FixEncoding(Convert.ToString(row["userName"]));
            if (row.Table.Columns.Contains("firstName"))
                row["firstName"] = Connect.FixEncoding(Convert.ToString(row["firstName"]));
            if (row.Table.Columns.Contains("lastName"))
                row["lastName"] = Connect.FixEncoding(Convert.ToString(row["lastName"]));
            if (row.Table.Columns.Contains("email"))
                row["email"] = Connect.FixEncoding(Convert.ToString(row["email"]));
            if (row.Table.Columns.Contains("phonenum"))
                row["phonenum"] = Connect.FixEncoding(Convert.ToString(row["phonenum"]));
            if (row.Table.Columns.Contains("userId"))
                row["userId"] = Connect.FixEncoding(Convert.ToString(row["userId"]));
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

            string sql = "DELETE FROM Users WHERE id=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, myConnection))
            {
                cmd.Parameters.AddWithValue("?", userId);
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
    }
}
