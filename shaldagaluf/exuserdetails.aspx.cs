using System;
using System.Data;
using System.Linq;

public partial class exuserdetails : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        // אם אין סשן — שולח לכניסה
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!IsPostBack)
        {
            // קורא את ה־ID מה־URL
            string idStr = Request.QueryString["id"];

            if (!int.TryParse(idStr, out int userId))
            {
                ShowNotFound();
                return;
            }

            LoadUserById(userId);
        }
    }

    private void LoadUserById(int userId)
    {
        var us = new UsersService();
        var ds = us.getallusers();

        if (ds == null || ds.Tables.Count == 0)
        {
            ShowNotFound();
            return;
        }

        DataTable t = ds.Tables[0];

        // 🎯 קריאה לפי ID בלבד
        DataRow row = t.AsEnumerable()
            .FirstOrDefault(r =>
                int.TryParse(Convert.ToString(r["id"]), out int id) &&
                id == userId
            );

        if (row == null)
        {
            ShowNotFound();
            return;
        }

        pnlContent.Visible = true;
        pnlNotFound.Visible = false;

        string userName = SafeGet(row, "userName");
        lblUserName.Text = userName;
        lblFirstName.Text = SafeGet(row, "firstName");
        lblLastName.Text = SafeGet(row, "lastName");
        lblEmail.Text = SafeGet(row, "email");
        lblPhone.Text = SafeGet(row, "phonenum");
        lblCity.Text = GetCity(row);

        lblAccessLevel.Text = SafeGet(row, "Role");

        if (!string.IsNullOrEmpty(userName) && avatarLetter != null)
        {
            avatarLetter.InnerText = userName.Substring(0, 1).ToUpper();
        }
    }

    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }

    private string SafeGet(DataRow row, string columnName)
    {
        if (row == null || row.Table == null) return string.Empty;

        var col = row.Table.Columns
            .Cast<DataColumn>()
            .FirstOrDefault(c => c.ColumnName.Trim().ToLower() == columnName.ToLower());

        if (col == null) return string.Empty;

        object val = row[col.ColumnName];
        if (val == null || val == DBNull.Value) return string.Empty;
        
        string result = Convert.ToString(val);
        return Connect.FixEncoding(result);
    }

    private string GetCity(DataRow row)
    {
        if (row == null || row.Table == null) return string.Empty;

        string[] names = { "CityName", "cityname", "city" };

        foreach (var n in names)
        {
            if (row.Table.Columns.Contains(n) && row[n] != DBNull.Value && row[n] != null)
            {
                return Connect.FixEncoding(Convert.ToString(row[n]));
            }
        }

        return string.Empty;
    }
}
