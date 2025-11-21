using System;
using System.Data.OleDb;
using System.Web.UI;

public partial class login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text.Trim();

        string connStr = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connStr))
        {
            conn.Open();

            string sql = "SELECT userName, role FROM Users WHERE userName=@u AND [password]=@p";

            OleDbCommand cmd = new OleDbCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password);

            OleDbDataReader dr = cmd.ExecuteReader();

            if (dr.Read()) // ← זה נמצא עכשיו בתוך הפונקציה, במקום הנכון!
            {
                Session["username"] = dr["userName"].ToString();
                Session["Role"] = dr["role"].ToString();
                Session["loggedIn"] = true;

                Response.Redirect("home.aspx");
            }
            else
            {
                lblError.Text = "שם משתמש או סיסמה שגויים.";
            }
        }
    }
}
