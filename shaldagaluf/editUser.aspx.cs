using System;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;

public partial class editUser : System.Web.UI.Page
{
    private static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9_\.\-]{3,30}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new Regex(@"^0(5\d|[2-489]|7[2-9])-?\d{7}$", RegexOptions.Compiled);

    private int userId;

    // טעינת הדף והפעלת לוגיקה ראשונית
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;

        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        string role = Session["Role"] != null ? Session["Role"].ToString() : "user";
        if (!string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
        {
            Response.Redirect("exusers.aspx");
            return;
        }

        if (!int.TryParse(Request.QueryString["id"], out userId))
        {
            ShowNotFound();
            return;
        }

        if (!IsPostBack)
        {
            LoadUser();
        }
    }

    // הצגת הודעת שגיאה למשתמש
    private void ShowError(string message)
    {
        lblMessage.Text = message;
        lblMessage.CssClass = "form-message error";
        lblMessage.Visible = true;
    }

    // טעינה וקישור נתונים: LoadUser
    private void LoadUser()
    {
        try
        {
            UsersService us = new UsersService();
            DataSet ds = us.getallusers();

            if (ds == null || ds.Tables.Count == 0)
            {
                ShowNotFound();
                return;
            }

            DataTable t = ds.Tables[0];

            DataRow row = t.AsEnumerable()
                .FirstOrDefault(r =>
                {
                    int id;
                    return int.TryParse(Convert.ToString(r["id"]), out id) && id == userId;
                });

            if (row == null)
            {
                ShowNotFound();
                return;
            }

            pnlForm.Visible = true;
            pnlNotFound.Visible = false;

            txtUserName.Text = GetFieldValue(row, "userName", "UserName", "username");
            txtFirstName.Text = GetFieldValue(row, "firstName", "FirstName", "firstname");
            txtLastName.Text = GetFieldValue(row, "lastName", "LastName", "lastname");
            txtEmail.Text = GetFieldValue(row, "email", "Email");
            txtPhone.Text = GetFieldValue(row, "phonenum", "PhoneNumber", "phone");
            txtCity.Text = GetFieldValue(row, "CityName", "cityname", "city");

            string userRole = GetFieldValue(row, "Role", "role");
            if (!string.IsNullOrEmpty(userRole) && ddlRole.Items.FindByValue(userRole) != null)
            {
                ddlRole.SelectedValue = userRole;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("EDITUSER", "Error loading user", ex);
            ShowNotFound();
        }
    }

    // שמירת הנתונים מהטופס (עם בדיקות תקינות)
    protected void btnSave_Click(object sender, EventArgs e)
    {
        Page.Validate("EditUser");
        if (!Page.IsValid)
        {
            ShowError("יש לתקן את השגיאות בטופס לפני השמירה.");
            return;
        }

        string userName = txtUserName.Text != null ? txtUserName.Text.Trim() : "";
        string firstName = txtFirstName.Text != null ? txtFirstName.Text.Trim() : "";
        string lastName = txtLastName.Text != null ? txtLastName.Text.Trim() : "";
        string email = txtEmail.Text != null ? txtEmail.Text.Trim() : "";
        string phone = txtPhone.Text != null ? txtPhone.Text.Trim().Replace(" ", "").Replace("-", "") : "";
        string roleValue = ddlRole.SelectedValue != null ? ddlRole.SelectedValue.Trim() : "user";

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
        {
            ShowError("אנא מלא את כל השדות החובה.");
            return;
        }

        if (!UsernameRegex.IsMatch(userName))
        {
            ShowError("שם משתמש חייב להכיל 3–30 תווים באנגלית, ספרות, נקודה, קו תחתון או מקף.");
            return;
        }

        if (firstName.Length < 2 || lastName.Length < 2)
        {
            ShowError("שם פרטי ושם משפחה חייבים להכיל לפחות 2 תווים.");
            return;
        }

        if (!EmailRegex.IsMatch(email))
        {
            ShowError("אנא הכנס כתובת אימייל תקינה.");
            return;
        }

        if (!PhoneRegex.IsMatch(phone))
        {
            ShowError("מספר טלפון לא תקין. לדוגמה: 0501234567 או 03-1234567.");
            return;
        }

        if (roleValue != "user" && roleValue != "owner")
        {
            ShowError("רמת גישה לא תקינה.");
            return;
        }

        UsersService us = new UsersService();
        if (us.UsernameExistsForOtherUser(userName, userId))
        {
            ShowError("שם המשתמש כבר קיים אצל משתמש אחר.");
            return;
        }

        DataRow existingEmail = us.GetUserByEmail(email);
        if (existingEmail != null)
        {
            int existingId;
            string idStr = GetFieldValue(existingEmail, "id", "Id");
            if (int.TryParse(idStr, out existingId) && existingId != userId)
            {
                ShowError("כתובת האימייל כבר רשומה למשתמש אחר.");
                return;
            }
        }

        try
        {
            us.UpdateUserProfile(userId, userName, firstName, lastName, email, phone);
            UpdateUserRoleAndCity(userId, roleValue, txtCity.Text != null ? txtCity.Text.Trim() : "");

            lblMessage.Text = "המשתמש עודכן בהצלחה!";
            lblMessage.CssClass = "form-message success";
            lblMessage.Visible = true;

            Response.AddHeader("REFRESH", "2;URL=exusers.aspx");
        }
        catch (Exception ex)
        {
            LoggingService.Log("EDITUSER", "Error saving user", ex);
            ShowError("שגיאה בעדכון המשתמש: " + ex.Message);
        }
    }

    // עדכון תפקיד ועיר אחרי עדכון הפרופיל
    private void UpdateUserRoleAndCity(int id, string role, string city)
    {
        string conStr = Connect.GetConnectionString();
        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();

            if (ColumnExists(con, "Users", "Role") || ColumnExists(con, "Users", "role"))
            {
                string roleCol = ColumnExists(con, "Users", "Role") ? "[Role]" : "[role]";
                string idCol = ColumnExists(con, "Users", "Id") ? "[Id]" : "[id]";
                using (OleDbCommand cmd = new OleDbCommand("UPDATE [Users] SET " + roleCol + "=? WHERE " + idCol + "=?", con))
                {
                    cmd.Parameters.AddWithValue("?", role);
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.ExecuteNonQuery();
                }
            }

            // City is displayed as name; store only if a free-text city column exists
            if (ColumnExists(con, "Users", "city") && !string.IsNullOrWhiteSpace(city))
            {
                string idCol = ColumnExists(con, "Users", "Id") ? "[Id]" : "[id]";
                using (OleDbCommand cityCmd = new OleDbCommand("UPDATE [Users] SET [city]=? WHERE " + idCol + "=?", con))
                {
                    cityCmd.Parameters.AddWithValue("?", city);
                    cityCmd.Parameters.AddWithValue("?", id);
                    cityCmd.ExecuteNonQuery();
                }
            }
        }
    }

    // הצגת הודעה כשהפריט לא נמצא
    private void ShowNotFound()
    {
        pnlForm.Visible = false;
        pnlNotFound.Visible = true;
    }

    // שליפת נתונים: GetFieldValue
    private string GetFieldValue(DataRow row, params string[] columnNames)
    {
        if (row == null || row.Table == null) return string.Empty;

        foreach (string columnName in columnNames)
        {
            DataColumn col = row.Table.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.Trim().ToLower() == columnName.ToLower());

            if (col != null)
            {
                object val = row[col.ColumnName];
                if (val != null && val != DBNull.Value)
                {
                    return Convert.ToString(val);
                }
            }
        }

        return string.Empty;
    }

    // בדיקה אם עמודה קיימת בטבלה
    private bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
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
}
