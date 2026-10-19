using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

public class UsersService
{

    // בדיקה אם עמודה קיימת בטבלה
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
    private static int GetNextUserId(OleDbConnection conn)
    {
        string[] maxQueries =
        {
            "SELECT MAX(Val([id])) FROM [Users]",
            "SELECT MAX(Val([Id])) FROM [Users]",
            "SELECT MAX([id]) FROM [Users]",
            "SELECT MAX([Id]) FROM [Users]"
        };

        foreach (string sql in maxQueries)
        {
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        int maxId;
                        if (int.TryParse(Convert.ToString(result).Trim(), out maxId))
                            return maxId + 1;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return 1;
    }

    // הוספת משתמש חדש למסד הנתונים
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
            // Access schema: [id] is required text (not AutoNumber) — must supply next unique value
            bool hasId = ColumnExists(myConnection, "Users", "id") || ColumnExists(myConnection, "Users", "Id");
            string idCol = ColumnExists(myConnection, "Users", "id") ? "id" : "Id";
            int nextId = hasId ? GetNextUserId(myConnection) : -1;

            string passwordCol = hasPasswordHash ? "PasswordHash" : (hasPassword ? "[password]" : "PasswordHash");
            string userIdCol = hasUserIdNumber ? "UserIdNumber" : (hasUserId ? "userId" : "UserIdNumber");
            string phoneCol = hasPhoneNumber ? "PhoneNumber" : (hasPhonenum ? "phonenum" : "PhoneNumber");
            string cityCol = hasCityId ? "CityId" : (hasCity ? "city" : "CityId");
            string roleCol = ColumnExists(myConnection, "Users", "Role") ? "Role" : (ColumnExists(myConnection, "Users", "role") ? "role" : "Role");

            List<string> columns = new List<string>();
            List<string> values = new List<string>();

            if (hasId)
            {
                columns.Add("[" + idCol + "]");
                values.Add("?");
            }

            columns.AddRange(new string[] { "UserName", "FirstName", "LastName", "Email", passwordCol, "Gender", "YearOfBirth", userIdCol, phoneCol, cityCol });
            values.AddRange(new string[] { "?", "?", "?", "?", "?", "?", "?", "?", "?", "?" });

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

            using (OleDbCommand cmd = new OleDbCommand(sSql, myConnection))
            {
                string hashedPassword = PasswordHelper.HashPassword(password);

                if (hasId)
                {
                    OleDbParameter idParam = new OleDbParameter("?", OleDbType.WChar);
                    idParam.Value = nextId.ToString();
                    cmd.Parameters.Add(idParam);
                }

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

                try
                {
                    cmd.ExecuteNonQuery();
                    return hasId ? nextId : -1;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to insert user: " + ex.Message, ex);
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
    // USERNAME UNIQUENESS
    // ------------------------------
    public bool UsernameExists(string userName)
    {
        return UsernameExistsForOtherUser(userName, -1);
    }

    // בדיקה אם שם משתמש קיים אצל משתמש אחר
    public bool UsernameExistsForOtherUser(string userName, int excludeUserId)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        string connectionString = Connect.GetConnectionString();
        string lower = userName.Trim().ToLower();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string[] queries =
            {
                "SELECT COUNT(*) FROM [Users] WHERE LCase([UserName])=? AND [Id]<>?",
                "SELECT COUNT(*) FROM [Users] WHERE LCase([userName])=? AND [Id]<>?",
                "SELECT COUNT(*) FROM [Users] WHERE LCase([username])=? AND [id]<>?"
            };

            foreach (string sql in queries)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", lower);
                        cmd.Parameters.AddWithValue("?", excludeUserId);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
                            return true;
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

    // עדכון פרטי פרופיל משתמש במסד
    public void UpdateUserProfile(int userId, string userName, string firstName, string lastName, string email, string phone)
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string userNameCol = ColumnExists(conn, "Users", "UserName") ? "[UserName]"
                : (ColumnExists(conn, "Users", "userName") ? "[userName]" : "[username]");
            string firstNameCol = ColumnExists(conn, "Users", "FirstName") ? "[FirstName]"
                : (ColumnExists(conn, "Users", "firstName") ? "[firstName]" : "[firstname]");
            string lastNameCol = ColumnExists(conn, "Users", "LastName") ? "[LastName]"
                : (ColumnExists(conn, "Users", "lastName") ? "[lastName]" : "[lastname]");
            string emailCol = ColumnExists(conn, "Users", "Email") ? "[Email]"
                : (ColumnExists(conn, "Users", "email") ? "[email]" : "[email]");
            string idCol = ColumnExists(conn, "Users", "Id") ? "[Id]"
                : (ColumnExists(conn, "Users", "id") ? "[id]" : "[id]");

            bool hasPhoneNumber = ColumnExists(conn, "Users", "PhoneNumber");
            bool hasPhonenum = ColumnExists(conn, "Users", "phonenum");

            // Keep both phone columns in sync when the schema has both.
            System.Text.StringBuilder set = new System.Text.StringBuilder();
            set.Append(userNameCol).Append("=?, ")
               .Append(firstNameCol).Append("=?, ")
               .Append(lastNameCol).Append("=?, ")
               .Append(emailCol).Append("=?");
            if (hasPhonenum)
                set.Append(", [phonenum]=?");
            if (hasPhoneNumber)
                set.Append(", [PhoneNumber]=?");

            string sql = "UPDATE [Users] SET " + set + " WHERE " + idCol + "=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                string phoneVal = phone != null ? phone.Trim() : "";
                cmd.Parameters.AddWithValue("?", userName != null ? userName.Trim() : "");
                cmd.Parameters.AddWithValue("?", firstName != null ? firstName.Trim() : "");
                cmd.Parameters.AddWithValue("?", lastName != null ? lastName.Trim() : "");
                cmd.Parameters.AddWithValue("?", email != null ? email.Trim() : "");
                if (hasPhonenum)
                    cmd.Parameters.AddWithValue("?", phoneVal);
                if (hasPhoneNumber)
                    cmd.Parameters.AddWithValue("?", phoneVal);
                cmd.Parameters.AddWithValue("?", userId);
                int affected = cmd.ExecuteNonQuery();
                if (affected <= 0)
                    throw new InvalidOperationException("לא נמצא משתמש לעדכון (Id=" + userId + ").");
            }
        }
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

    // שליפת משתמש לפי אימייל
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

    // שמירה/עדכון במסד: UpdatePassword
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

    // שליפת משתמש לפי GoogleId
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

    // מחיקה: DeleteUser
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

