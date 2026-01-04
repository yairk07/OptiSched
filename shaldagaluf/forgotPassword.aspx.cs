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
        // Set UTF-8 encoding - MUST be first, before any output
        Response.Clear();
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        Response.AppendHeader("Content-Type", "text/html; charset=utf-8");
        
        if (Session["username"] != null)
        {
            Response.Redirect("home.aspx");
            return;
        }

        if (!IsPostBack)
        {
            pnlRequest.Visible = true;
            pnlReset.Visible = false;
        }
        else
        {
            // If we have email in session, show reset panel
            if (Session["ResetPasswordEmail"] != null)
            {
                pnlRequest.Visible = false;
                pnlReset.Visible = true;
            }
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

        try
        {
            // Generate login code using LoginCodeService
            string code = LoginCodeService.GenerateCode(email);
            LoggingService.Log("FORGOT_PASSWORD_CODE_GENERATED", string.Format("Code generated for password reset - Email: {0}, Code: {1}", email, code));
            
            // Send code via email
            EmailService.SendLoginCodeEmail(email, code);
            
            // Store email in session for verification
            Session["ResetPasswordEmail"] = email;
            
            // Show reset panel
            pnlRequest.Visible = false;
            pnlReset.Visible = true;
            
            lblResetMessage.Text = "קוד נשלח בהצלחה לכתובת האימייל שלך. אנא הזן את הקוד בן 6 הספרות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Green;
            lblResetMessage.Visible = true;
        }
        catch (InvalidOperationException ex)
        {
            lblMessage.Text = ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
            LoggingService.Log("FORGOT_PASSWORD_CODE_ERROR", string.Format("Failed to generate code - Error: {0}", ex.Message), ex);
        }
        catch (Exception ex)
        {
            lblMessage.Text = "שגיאה בשליחת האימייל. אנא נסה שוב מאוחר יותר.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            LoggingService.Log("FORGOT_PASSWORD_EMAIL_ERROR", string.Format("Failed to send reset email - Error: {0}", ex.Message), ex);
        }
    }

    protected void btnResetPassword_Click(object sender, EventArgs e)
    {
        string email = Session["ResetPasswordEmail"]?.ToString() ?? "";
        string code = txtResetToken.Text?.Trim();
        
        if (string.IsNullOrEmpty(email))
        {
            lblResetMessage.Text = "שגיאה: לא נמצא אימייל. אנא התחל מחדש.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }
        
        if (string.IsNullOrEmpty(code))
        {
            lblResetMessage.Text = "אנא הזן את קוד ההתחברות שקיבלת באימייל.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        if (code.Length != 6)
        {
            lblResetMessage.Text = "קוד ההתחברות חייב להיות בן 6 ספרות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            lblResetMessage.Text = "אנא מלא את כל השדות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        if (newPassword != confirmPassword)
        {
            lblResetMessage.Text = "הסיסמאות אינן תואמות.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        // Validate the code
        bool isValid = LoginCodeService.ValidateCode(email, code);
        if (!isValid)
        {
            lblResetMessage.Text = "קוד שגוי או שפג תוקפו. אנא נסה שוב או בקש קוד חדש.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        // Get user by email
        UsersService us = new UsersService();
        DataRow user = us.GetUserByEmail(email);
        
        if (user == null)
        {
            lblResetMessage.Text = "שגיאה: משתמש לא נמצא במערכת.";
            lblResetMessage.ForeColor = System.Drawing.Color.Red;
            lblResetMessage.Visible = true;
            return;
        }

        int userId = Convert.ToInt32(user["Id"] ?? user["id"]);
        
        // Update password
        us.UpdatePassword(userId, newPassword);

        // Clear session
        Session.Remove("ResetPasswordEmail");

        lblResetMessage.Text = "הסיסמה עודכנה בהצלחה! אתה יכול להתחבר עכשיו.";
        lblResetMessage.ForeColor = System.Drawing.Color.Green;
        lblResetMessage.Visible = true;

        // Redirect to login after 3 seconds
        Response.AddHeader("REFRESH", "3;URL=login.aspx");
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
            
            // Check if notes column exists, if not create it
            bool notesExists = ColumnExists(con, "Users", "notes");
            if (!notesExists)
            {
                try
                {
                    using (System.Data.OleDb.OleDbCommand alterCmd = new System.Data.OleDb.OleDbCommand("ALTER TABLE [Users] ADD COLUMN [notes] TEXT", con))
                    {
                        alterCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Log("FORGOT_PASSWORD_CREATE_NOTES_COLUMN_ERROR", string.Format("Failed to create notes column - Error: {0}", ex.Message), ex);
                }
            }
            
            string expiry = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
            string tokenData = string.Format("{0}|{1}", token, expiry);
            
            // Try different column name variations
            string[] notesVariations = { "notes", "Notes", "note", "Note" };
            bool updated = false;
            
            foreach (string colName in notesVariations)
            {
                if (ColumnExists(con, "Users", colName))
                {
                    try
                    {
                        // Try different Id column variations
                        string[] idVariations = { "Id", "id", "ID" };
                        foreach (string idCol in idVariations)
                        {
                            if (ColumnExists(con, "Users", idCol))
                            {
                                string sql = string.Format("UPDATE [Users] SET [{0}] = ? WHERE [{1}] = ?", colName, idCol);
                                LoggingService.Log("FORGOT_PASSWORD_UPDATE_TOKEN", string.Format("Updating token - SQL: {0}, UserId: {1}, TokenData: {2}", sql, userId, tokenData));
                                try
                                {
                                    using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con))
                                    {
                                        cmd.Parameters.AddWithValue("?", tokenData);
                                        cmd.Parameters.AddWithValue("?", userId);
                                        cmd.ExecuteNonQuery();
                                        updated = true;
                                        LoggingService.Log("FORGOT_PASSWORD_UPDATE_SUCCESS", "Token updated successfully");
                                        break;
                                    }
                                }
                                catch (Exception idEx)
                                {
                                    LoggingService.Log("FORGOT_PASSWORD_UPDATE_ID_ERROR", string.Format("Failed with Id column {0} - Error: {1}", idCol, idEx.Message), idEx);
                                    continue;
                                }
                            }
                        }
                        if (updated) break;
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("FORGOT_PASSWORD_UPDATE_ERROR", string.Format("Failed to update with column {0} - Error: {1}", colName, ex.Message), ex);
                        continue;
                    }
                }
            }
            
            if (!updated)
            {
                LoggingService.Log("FORGOT_PASSWORD_UPDATE_FAILED", "Failed to update token - no suitable column found");
                throw new Exception("Failed to save reset token - no suitable column found in Users table");
            }
        }
    }
    
    private bool ColumnExists(System.Data.OleDb.OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
            foreach (string variant in variations)
            {
                try
                {
                    using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(string.Format("SELECT TOP 1 [{0}] FROM [{1}]", variant, tableName), conn))
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

    private int? GetUserIdByToken(string token)
    {
        string conStr = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
        {
            con.Open();
            
            // Try different column name variations
            string[] notesVariations = { "notes", "Notes", "note", "Note" };
            string[] idVariations = { "Id", "id", "ID" };
            
            foreach (string notesCol in notesVariations)
            {
                if (ColumnExists(con, "Users", notesCol))
                {
                    foreach (string idCol in idVariations)
                    {
                        if (ColumnExists(con, "Users", idCol))
                        {
                            try
                            {
                                string sql = string.Format("SELECT [{0}], [{1}] FROM [Users] WHERE [{1}] LIKE ?", idCol, notesCol);
                                LoggingService.Log("FORGOT_PASSWORD_GET_TOKEN", string.Format("Getting user by token - SQL: {0}, Token: {1}", sql, token));
                                using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con))
                                {
                                    cmd.Parameters.AddWithValue("?", token + "%");
                                    using (System.Data.OleDb.OleDbDataReader dr = cmd.ExecuteReader())
                                    {
                                        while (dr.Read())
                                        {
                                            string notes = dr[notesCol]?.ToString() ?? "";
                                            if (notes.StartsWith(token + "|"))
                                            {
                                                string[] parts = notes.Split('|');
                                                if (parts.Length >= 2)
                                                {
                                                    if (DateTime.TryParse(parts[1], out DateTime expiry) && expiry > DateTime.Now)
                                                    {
                                                        int userId = Convert.ToInt32(dr[idCol]);
                                                        LoggingService.Log("FORGOT_PASSWORD_TOKEN_FOUND", string.Format("Token found - UserId: {0}", userId));
                                                        return userId;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Log("FORGOT_PASSWORD_GET_TOKEN_ERROR", string.Format("Error getting token - Error: {0}", ex.Message), ex);
                                continue;
                            }
                        }
                    }
                }
            }
        }
        LoggingService.Log("FORGOT_PASSWORD_TOKEN_NOT_FOUND", string.Format("Token not found - Token: {0}", token));
        return null;
    }

    private void DeleteResetToken(string token)
    {
        string conStr = Connect.GetConnectionString();
        using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
        {
            con.Open();
            
            // Try different column name variations
            string[] notesVariations = { "notes", "Notes", "note", "Note" };
            
            foreach (string colName in notesVariations)
            {
                if (ColumnExists(con, "Users", colName))
                {
                    try
                    {
                        string sql = string.Format("UPDATE [Users] SET [{0}] = NULL WHERE [{0}] LIKE ?", colName);
                        LoggingService.Log("FORGOT_PASSWORD_DELETE_TOKEN", string.Format("Deleting token - SQL: {0}, Token: {1}", sql, token));
                        using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("?", token + "%");
                            cmd.ExecuteNonQuery();
                            LoggingService.Log("FORGOT_PASSWORD_DELETE_SUCCESS", "Token deleted successfully");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("FORGOT_PASSWORD_DELETE_ERROR", string.Format("Failed to delete token with column {0} - Error: {1}", colName, ex.Message), ex);
                        continue;
                    }
                }
            }
        }
    }

    private void SendResetEmail(string email, string resetLink, string token)
    {
        string smtpServer = "smtp.gmail.com";
        int smtpPort = 587;
        string smtpUsername = "yairk07@gmail.com";
        string smtpPassword = "wdbf swcf qexu qugl";

        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(smtpUsername, "OptiSched", System.Text.Encoding.UTF8);
        mail.To.Add(email);
        mail.SubjectEncoding = System.Text.Encoding.UTF8;
        mail.Subject = "איפוס סיסמה - OptiSched";
        mail.BodyEncoding = System.Text.Encoding.UTF8;
        
        string body = string.Format(@"
<html dir='rtl'>
<head>
    <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
</head>
<body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #e50914;'>איפוס סיסמה</h2>
        <p>שלום,</p>
        <p>קיבלנו בקשה לאיפוס הסיסמה שלך.</p>
        <p><strong>קוד איפוס הסיסמה שלך:</strong></p>
        <p style='text-align: center; margin: 20px 0;'>
            <span style='background: #f5f5f5; padding: 15px 25px; border-radius: 5px; font-size: 18px; font-weight: bold; letter-spacing: 2px; display: inline-block; font-family: monospace;'>{1}</span>
        </p>
        <p>לחץ על הקישור הבא כדי לאפס את הסיסמה:</p>
        <p style='text-align: center; margin: 30px 0;'>
            <a href='{0}' style='background: #e50914; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>איפוס סיסמה</a>
        </p>
        <p>או העתק את הקישור הבא לדפדפן:</p>
        <p style='background: #f5f5f5; padding: 10px; border-radius: 5px; word-break: break-all; font-size: 12px;'>{0}</p>
        <p><strong>קישור זה תקף למשך שעה אחת בלבד.</strong></p>
        <p>אם הקישור לא עובד, תוכל להעתיק את הקוד למעלה ולהכניס אותו בדף איפוס הסיסמה.</p>
        <p>אם לא ביקשת איפוס סיסמה, אנא התעלם מהאימייל הזה.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px; text-align: center;'>OptiSched - Smart Scheduling for Maximum Efficiency</p>
    </div>
</body>
</html>", resetLink, token);
        
        mail.Body = body;
        mail.IsBodyHtml = true;

        SmtpClient smtp = new SmtpClient(smtpServer, smtpPort);
        smtp.EnableSsl = true;
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtp.UseDefaultCredentials = false;
        smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

        smtp.Send(mail);
    }
}

