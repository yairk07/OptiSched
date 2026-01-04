using System;
using System.Data.OleDb;
using System.Web.UI;
using System.Security.Cryptography;
using System.Text;

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

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text.Trim();

        string hashedPassword = HashPassword(password);

        string connStr = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connStr))
        {
            conn.Open();

            string sql = "SELECT id, userName, role, [password] FROM Users WHERE userName=?";
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            cmd.Parameters.AddWithValue("?", username);

            OleDbDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                string dbPassword = dr["password"]?.ToString() ?? "";
                
                bool passwordMatch = false;
                
                if (dbPassword.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(dbPassword, @"^[a-f0-9]{64}$"))
                {
                    passwordMatch = (dbPassword == hashedPassword);
                }
                else
                {
                    passwordMatch = (dbPassword == password);
                    
                    if (passwordMatch)
                    {
                        string updateSql = "UPDATE Users SET [password]=? WHERE id=?";
                        OleDbCommand updateCmd = new OleDbCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("?", hashedPassword);
                        updateCmd.Parameters.AddWithValue("?", dr["id"]);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                if (passwordMatch)
                {
                    Session["username"] = dr["userName"].ToString();
                    Session["Role"] = dr["role"]?.ToString() ?? "user";
                    Session["userId"] = dr["id"].ToString();
                    Session["loggedIn"] = true;

                    Response.Redirect("home.aspx");
                    return;
                }
            }
            
            lblError.Text = "שם משתמש או סיסמה שגויים.";
        }
    }
}
