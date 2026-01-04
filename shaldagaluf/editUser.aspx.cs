using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI.WebControls;
using System.Data.OleDb;

public partial class editUser : System.Web.UI.Page
{
    private int userId = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;

        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!IsOwner())
        {
            Response.Redirect("exusers.aspx");
            return;
        }

        if (!TryGetUserId(out userId))
        {
            pnlNotFound.Visible = true;
            pnlContent.Visible = false;
            return;
        }

        if (!IsPostBack)
        {
            LoadCities();        // ⚠️ קודם ערים
            LoadUserData(userId);
        }
        
        // Show role field only for owners
        if (pnlRole != null)
        {
            pnlRole.Visible = IsOwner();
        }
    }

    private bool TryGetUserId(out int id)
    {
        return int.TryParse(Request.QueryString["id"], out id);
    }

    private void LoadUserData(int userId)
    {
        try
        {
            UsersService us = new UsersService();
            var ds = us.getallusers();

            if (ds == null || ds.Tables.Count == 0)
                throw new Exception("לא נמצאו נתונים");

            DataRow row = null;
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                try
                {
                    string idCol = "id";
                    if (!r.Table.Columns.Contains("id"))
                    {
                        if (r.Table.Columns.Contains("Id"))
                            idCol = "Id";
                        else
                            continue;
                    }
                    
                    if (int.TryParse(Convert.ToString(r[idCol]), out int id) && id == userId)
                    {
                        row = r;
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (row == null)
                throw new Exception("משתמש לא נמצא");

            pnlContent.Visible = true;
            pnlNotFound.Visible = false;

            txtUserName.Text = SafeGet(row, "UserName");
            txtFirstName.Text = SafeGet(row, "FirstName");
            txtLastName.Text = SafeGet(row, "LastName");
            txtEmail.Text = SafeGet(row, "Email");
            txtPhone.Text = SafeGet(row, "PhoneNumber");

            string role = SafeGet(row, "Role");
            if (!string.IsNullOrEmpty(role))
            {
                role = role.ToLower();
                if (ddlRole.Items.FindByValue(role) != null)
                    ddlRole.SelectedValue = role;
            }

            string cityValue = SafeGet(row, "CityId");
            if (string.IsNullOrEmpty(cityValue))
                cityValue = SafeGet(row, "city");

            if (int.TryParse(cityValue, out int cityId) &&
                ddlCity.Items.FindByValue(cityId.ToString()) != null)
            {
                ddlCity.SelectedValue = cityId.ToString();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("שגיאה בטעינת נתוני המשתמש: " + ex.Message, true);
        }
    }

    private void LoadCities()
    {
        try
        {
            using (OleDbConnection con = new OleDbConnection(Connect.GetConnectionString()))
            {
                con.Open();
                string sql = "SELECT Id, CityName FROM Citys ORDER BY CityName";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    ddlCity.Items.Clear();
                    ddlCity.Items.Add(new ListItem("בחר עיר", ""));

                    while (dr.Read())
                    {
                        ddlCity.Items.Add(
                            new ListItem(
                                Connect.FixEncoding(dr["CityName"].ToString()),
                                dr["Id"].ToString()
                            ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("שגיאה בטעינת ערים: " + ex.Message, true);
        }
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        try
        {
            using (OleDbConnection con = new OleDbConnection(Connect.GetConnectionString()))
            {
                con.Open();

                bool hasRole = ColumnExists(con, "Users", "Role");
                bool hasCityId = ColumnExists(con, "Users", "CityId");
                bool hasCity = ColumnExists(con, "Users", "city");

                string sql = "UPDATE [Users] SET [UserName]=?, [FirstName]=?, [LastName]=?, [Email]=?, [phonenum]=?";

                if (hasCityId) sql += ", [CityId]=?";
                else if (hasCity) sql += ", [city]=?";

                if (hasRole) sql += ", [Role]=?";
                sql += " WHERE [Id]=?";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    string userName = Connect.FixEncoding(txtUserName.Text?.Trim() ?? "");
                    string firstName = Connect.FixEncoding(txtFirstName.Text?.Trim() ?? "");
                    string lastName = Connect.FixEncoding(txtLastName.Text?.Trim() ?? "");
                    string email = Connect.FixEncoding(txtEmail.Text?.Trim() ?? "");
                    string phone = Connect.FixEncoding(txtPhone.Text?.Trim() ?? "");
                    
                    cmd.Parameters.AddWithValue("?", userName);
                    cmd.Parameters.AddWithValue("?", firstName);
                    cmd.Parameters.AddWithValue("?", lastName);
                    cmd.Parameters.AddWithValue("?", email);
                    cmd.Parameters.AddWithValue("?", phone);

                    if (hasCityId || hasCity)
                    {
                        if (int.TryParse(ddlCity.SelectedValue, out int cityId))
                            cmd.Parameters.AddWithValue("?", cityId);
                        else
                            cmd.Parameters.AddWithValue("?", DBNull.Value);
                    }

                    if (hasRole && pnlRole != null && pnlRole.Visible)
                    {
                        string roleValue = ddlRole.SelectedValue ?? "user";
                        cmd.Parameters.AddWithValue("?", roleValue);
                    }

                    cmd.Parameters.AddWithValue("?", userId);

                    cmd.ExecuteNonQuery();
                }
            }

            ShowMessage("השינויים נשמרו בהצלחה!", false);
        }
        catch (Exception ex)
        {
            ShowMessage("שגיאה בשמירת הנתונים: " + ex.Message, true);
        }
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        Response.Redirect("exusers.aspx");
    }


    private bool IsOwner()
    {
        return (Session["Role"]?.ToString().ToLower() == "owner");
    }

    private string SafeGet(DataRow row, string column)
    {
        if (row == null || row.Table == null || string.IsNullOrEmpty(column))
            return "";
        
        try
        {
            Dictionary<string, string[]> columnMappings = new Dictionary<string, string[]>
            {
                { "username", new[] { "UserName", "userName", "username", "USERNAME" } },
                { "firstname", new[] { "FirstName", "firstName", "firstname", "FIRSTNAME" } },
                { "lastname", new[] { "LastName", "lastName", "lastname", "LASTNAME" } },
                { "email", new[] { "Email", "email", "EMAIL" } },
                { "phonenumber", new[] { "PhoneNumber", "phonenum", "PhoneNum", "phoneNumber", "PHONENUM" } },
                { "phonenum", new[] { "PhoneNumber", "phonenum", "PhoneNum", "phoneNumber", "PHONENUM" } },
                { "role", new[] { "Role", "role", "ROLE" } },
                { "cityid", new[] { "CityId", "city", "City", "CITYID" } },
                { "city", new[] { "CityId", "city", "City", "CITYID" } }
            };
            
            string columnLower = column.ToLower();
            
            if (columnMappings.ContainsKey(columnLower))
            {
                foreach (string variant in columnMappings[columnLower])
                {
                    try
                    {
                        if (row.Table.Columns.Contains(variant) && row.Table.Columns[variant] != null)
                        {
                            object value = row[variant];
                            if (value != null && value != DBNull.Value)
                            {
                                string strValue = value.ToString();
                                if (!string.IsNullOrWhiteSpace(strValue))
                                    return Connect.FixEncoding(strValue);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            else
            {
                List<string> variations = new List<string> { column, column.ToLower(), column.ToUpper() };
                if (column.Length > 0)
                {
                    variations.Add(char.ToUpper(column[0]) + column.Substring(1).ToLower());
                }
                
                foreach (string variant in variations)
                {
                    try
                    {
                        if (row.Table.Columns.Contains(variant) && row.Table.Columns[variant] != null)
                        {
                            object value = row[variant];
                            if (value != null && value != DBNull.Value)
                            {
                                string strValue = value.ToString();
                                if (!string.IsNullOrWhiteSpace(strValue))
                                    return Connect.FixEncoding(strValue);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }
        catch
        {
        }
        
        return "";
    }

    private bool ColumnExists(OleDbConnection conn, string table, string column)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand(
                $"SELECT TOP 1 [{column}] FROM [{table}]", conn))
            {
                cmd.ExecuteScalar();
                return true;
            }
        }
        catch { return false; }
    }

    private void ShowMessage(string msg, bool isError)
    {
        lblMessage.Text = msg;
        lblMessage.Visible = true;
        lblMessage.ForeColor = isError ? System.Drawing.Color.Red : System.Drawing.Color.Green;
    }
}
