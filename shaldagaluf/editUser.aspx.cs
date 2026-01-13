using System;
using System.Data;
using System.Data.OleDb;
using System.Linq;

public partial class editUser : System.Web.UI.Page
{
    private int userId;

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
        if (role != "owner")
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

            txtUserName.Text = GetFieldValue(row, "userName");
            txtFirstName.Text = GetFieldValue(row, "firstName");
            txtLastName.Text = GetFieldValue(row, "lastName");
            txtEmail.Text = GetFieldValue(row, "email");
            txtPhone.Text = GetFieldValue(row, "phonenum");
            txtCity.Text = GetFieldValue(row, "CityName", "cityname", "city");

            string userRole = GetFieldValue(row, "Role");
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

    protected void btnSave_Click(object sender, EventArgs e)
    {
        try
        {
            string conStr = Connect.GetConnectionString();

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = @"
                    UPDATE Users
                    SET UserName = ?,
                        FirstName = ?,
                        LastName = ?,
                        Email = ?,
                        phonenum = ?,
                        Role = ?
                    WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", txtUserName.Text != null ? txtUserName.Text.Trim() : "");
                    cmd.Parameters.AddWithValue("?", txtFirstName.Text != null ? txtFirstName.Text.Trim() : "");
                    cmd.Parameters.AddWithValue("?", txtLastName.Text != null ? txtLastName.Text.Trim() : "");
                    cmd.Parameters.AddWithValue("?", txtEmail.Text != null ? txtEmail.Text.Trim() : "");
                    cmd.Parameters.AddWithValue("?", txtPhone.Text != null ? txtPhone.Text.Trim() : "");
                    cmd.Parameters.AddWithValue("?", ddlRole.SelectedValue != null ? ddlRole.SelectedValue.Trim() : "user");
                    cmd.Parameters.AddWithValue("?", userId);

                    cmd.ExecuteNonQuery();
                }

                if (ColumnExists(con, "Users", "city") || ColumnExists(con, "Users", "CityName"))
                {
                    string cityCol = ColumnExists(con, "Users", "city") ? "city" : "CityName";
                    string citySql = "UPDATE Users SET [" + cityCol + "] = ? WHERE Id = ?";
                    using (OleDbCommand cityCmd = new OleDbCommand(citySql, con))
                    {
                        cityCmd.Parameters.AddWithValue("?", txtCity.Text != null ? txtCity.Text.Trim() : "");
                        cityCmd.Parameters.AddWithValue("?", userId);
                        cityCmd.ExecuteNonQuery();
                    }
                }
            }

            lblMessage.Text = "המשתמש עודכן בהצלחה!";
            lblMessage.CssClass = "form-message success";
            lblMessage.Visible = true;

            Response.AddHeader("REFRESH", "2;URL=exusers.aspx");
        }
        catch (Exception ex)
        {
            LoggingService.Log("EDITUSER", "Error saving user", ex);
            lblMessage.Text = "שגיאה בעדכון המשתמש: " + ex.Message;
            lblMessage.CssClass = "form-message error";
            lblMessage.Visible = true;
        }
    }

    private void ShowNotFound()
    {
        pnlForm.Visible = false;
        pnlNotFound.Visible = true;
    }

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


