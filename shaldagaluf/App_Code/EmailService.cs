using System;
using System.Net;
using System.Net.Mail;

public class EmailService
{
    private static string GetSmtpServer()
    {
        return "smtp.gmail.com";
    }

    private static int GetSmtpPort()
    {
        return 587;
    }

    private static string GetSmtpUsername()
    {
        return "optischedual@gmail.com";
    }

    private static string GetSmtpPassword()
    {
        return "wdbf swcf qexu qugl";
    }

    public static void SendEmail(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            string smtpServer = GetSmtpServer();
            int smtpPort = GetSmtpPort();
            string smtpUsername = GetSmtpUsername();
            string smtpPassword = GetSmtpPassword();

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(smtpUsername, "OptiSched", System.Text.Encoding.UTF8);
            mail.To.Add(to);
            mail.SubjectEncoding = System.Text.Encoding.UTF8;
            mail.Subject = subject;
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.Body = body;
            mail.IsBodyHtml = isHtml;

            SmtpClient smtp = new SmtpClient(smtpServer, smtpPort);
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            smtp.Send(mail);
        }
        catch (Exception ex)
        {
            throw new Exception("שגיאה בשליחת אימייל: " + ex.Message);
        }
    }

    public static void SendRegistrationEmail(string email, string firstName = "")
    {
        string greeting = string.IsNullOrEmpty(firstName) ? "Hello" : $"Hello {firstName}";
        
        string subject = "Registration Successful - OptiSched";
        string body = $@"
<html dir='ltr'>
<body style='font-family: Arial, sans-serif; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #e50914;'>Welcome to OptiSched!</h2>
        <p>{greeting},</p>
        <p>Your registration to OptiSched has been completed successfully!</p>
        <p>You can now log in to the system and start using all the features:</p>
        <ul style='line-height: 1.8;'>
            <li>Personal calendar management</li>
            <li>Create events and tasks</li>
            <li>Share calendars with teams</li>
            <li>Smart time planning</li>
        </ul>
        <p>If you have any questions or need help, please contact us.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px;'>OptiSched - Smart Scheduling for Maximum Efficiency</p>
    </div>
</body>
</html>";

        SendEmail(email, subject, body, true);
    }

    public static void SendLoginCodeEmail(string email, string code)
    {
        string subject = "Login Code - OptiSched";
        string body = $@"
<html dir='ltr'>
<body style='font-family: Arial, sans-serif; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #e50914;'>Login Code</h2>
        <p>We received a login request for your OptiSched account.</p>
        <p>Your login code is:</p>
        <div style='background: #f5f5f5; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: bold; color: #e50914; letter-spacing: 8px; font-family: monospace;'>{code}</span>
        </div>
        <p>Enter this code on the login page to access your account.</p>
        <p style='color: #666; font-size: 14px;'><strong>This code is valid for 15 minutes only.</strong></p>
        <p>If you did not request a login code, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px;'>OptiSched - Smart Scheduling for Maximum Efficiency</p>
    </div>
</body>
</html>";

        SendEmail(email, subject, body, true);
    }
}



