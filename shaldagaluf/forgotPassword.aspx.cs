using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.UI;

public partial class forgotPassword : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        string token = Request.QueryString["token"];
        if (!string.IsNullOrEmpty(token))
        {
            pnlRequest.Visible = false;
            pnlReset.Visible = true;
            ViewState["ResetToken"] = token;
        }
    }

    protected void btnSendReset_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "אנא הזן כתובת אימייל.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        UsersService us = new UsersService();
        DataRow user = us.GetUserByEmail(email);

        if (user == null)
        {
            lblMessage.Text = "כתובת האימייל לא נמצאה במערכת.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        string token = GenerateResetToken();
        int userId = Convert.ToInt32(user["id"]);

        SaveResetToken(userId, token);

        string resetLink = string.Format("{0}://{1}{2}forgotPassword.aspx?token={3}", Request.Url.Scheme, Request.Url.Authority, Request.ApplicationPath, token);

        try
        {
            SendResetEmail(email, resetLink);
            lblMessage.Text = "נשלח לך אימייל עם קישור לאיפוס הסיסמה. אנא בדוק את תיבת הדואר הנכנס.";
            lblMessage.ForeColor = System.Drawing.Color.Green;
        }
        catch (Exception ex)
        {
            lblMessage.Text = "שגיאה בשליחת האימייל. אנא נסה שוב מאוחר יותר.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnResetPassword_Click(object sender, EventArgs e)
    {
        string token = ViewState["ResetToken"] != null ? ViewState["ResetToken"].ToString() : null;
        if (string.IsNullOrEmpty(token))
        {
            lblResetMessage.Text = "קישור לא תקין.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            lblResetMessage.Text = "אנא מלא את כל השדות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (newPassword != confirmPassword)
        {
            lblResetMessage.Text = "הסיסמאות אינן תואמות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        int? userId = GetUserIdByToken(token);
        if (!userId.HasValue)
        {
            lblResetMessage.Text = "קישור לא תקין או שפג תוקפו.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        UsersService us = new UsersService();
        us.UpdatePassword(userId.Value, newPassword);

        DeleteResetToken(token);

        lblResetMessage.Text = "הסיסמה עודכנה בהצלחה! אתה יכול להתחבר עכשיו.";
        lblResetMessage.ForeColor = System.Drawing.Color.Green;

        pnlReset.Visible = false;
    }

    private string GenerateResetToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private void SaveResetToken(int userId, string token)
    {
        string conStr = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
        {
            con.Open();
            string expiry = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
            string tokenData = string.Format("{0}|{1}", token, expiry);
            
            string sql = "UPDATE Users SET notes = ? WHERE id = ?";
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con);
            cmd.Parameters.AddWithValue("?", tokenData);
            cmd.Parameters.AddWithValue("?", userId);
            cmd.ExecuteNonQuery();
        }
    }

    private int? GetUserIdByToken(string token)
    {
        string conStr = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
        {
            con.Open();
            string sql = "SELECT id, notes FROM Users WHERE notes LIKE ?";
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con);
            cmd.Parameters.AddWithValue("?", token + "%");

            System.Data.OleDb.OleDbDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                string notes = dr["notes"] != null && dr["notes"] != DBNull.Value ? dr["notes"].ToString() : "";
                if (notes.StartsWith(token + "|"))
                {
                    string[] parts = notes.Split('|');
                    if (parts.Length >= 2)
                    {
                        DateTime expiry;
                        if (DateTime.TryParse(parts[1], out expiry) && expiry > DateTime.Now)
                        {
                            return Convert.ToInt32(dr["id"]);
                        }
                    }
                }
            }
        }
        return null;
    }

    private void DeleteResetToken(string token)
    {
        string conStr = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
        {
            con.Open();
            string sql = "UPDATE Users SET notes = NULL WHERE notes LIKE ?";
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con);
            cmd.Parameters.AddWithValue("?", token + "%");
            cmd.ExecuteNonQuery();
        }
    }

    private void SendResetEmail(string email, string resetLink)
    {
        string smtpServer = "smtp.gmail.com";
        int smtpPort = 587;
        string smtpUsername = "yairk07@gmail.com";
        string smtpPassword = "wdbf swcf qexu qugl";

        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(smtpUsername, "OptiSched");
        mail.To.Add(email);
        mail.Subject = "איפוס סיסמה - OptiSched";
        mail.Body = string.Format(@"
<html dir='rtl'>
<body style='font-family: Arial, sans-serif; direction: rtl;'>
    <h2>איפוס סיסמה</h2>
    <p>קיבלנו בקשה לאיפוס הסיסמה שלך.</p>
    <p>לחץ על הקישור הבא כדי לאפס את הסיסמה:</p>
    <p><a href='{0}' style='background: #e50914; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>איפוס סיסמה</a></p>
    <p>או העתק את הקישור הבא לדפדפן:</p>
    <p>{0}</p>
    <p>קישור זה תקף למשך שעה אחת בלבד.</p>
    <p>אם לא ביקשת איפוס סיסמה, אנא התעלם מהאימייל הזה.</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>OptiSched - Smart Scheduling for Maximum Efficiency</p>
</body>
</html>", resetLink);
        mail.IsBodyHtml = true;

        SmtpClient smtp = new SmtpClient(smtpServer, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

        smtp.Send(mail);
    }
}

