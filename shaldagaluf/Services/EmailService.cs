using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

public class EmailService
{
    private static string GetSmtpServer()
    {
        return ConfigurationManager.AppSettings["Smtp:Server"] ?? "smtp.gmail.com";
    }

    private static int GetSmtpPort()
    {
        string portStr = ConfigurationManager.AppSettings["Smtp:Port"];
        if (int.TryParse(portStr, out int port))
        {
            return port;
        }
        return 587;
    }

    private static string GetSmtpUsername()
    {
        return ConfigurationManager.AppSettings["Smtp:Username"] ?? "";
    }

    private static string GetSmtpPassword()
    {
        return ConfigurationManager.AppSettings["Smtp:Password"] ?? "";
    }

    public static void SendEmail(string to, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("כתובת אימייל לא יכולה להיות ריקה");
        }
        
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("נושא האימייל לא יכול להיות ריק");
        }
        
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("תוכן האימייל לא יכול להיות ריק");
        }

        string smtpServer = GetSmtpServer();
        int smtpPort = GetSmtpPort();
        string smtpUsername = GetSmtpUsername();
        string smtpPassword = GetSmtpPassword();
        
        LoggingService.Log("EMAIL_START", string.Format("Starting email send - To: {0}, Subject: {1}, Body Length: {2}, IsHtml: {3}", to, subject, body?.Length ?? 0, isHtml));
        LoggingService.Log("EMAIL_CONFIG", string.Format("SMTP Config - Server: {0}, Port: {1}, Username: {2}, Password Length: {3}", smtpServer, smtpPort, smtpUsername, smtpPassword?.Length ?? 0));

        if (string.IsNullOrWhiteSpace(smtpServer))
        {
            throw new InvalidOperationException("שרת SMTP לא מוגדר ב-Web.config");
        }

        if (string.IsNullOrWhiteSpace(smtpUsername))
        {
            throw new InvalidOperationException("שם משתמש SMTP לא מוגדר ב-Web.config");
        }

        if (string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new InvalidOperationException("סיסמת SMTP לא מוגדרת ב-Web.config");
        }

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
        System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

        try
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(smtpUsername, "OptiSched", System.Text.Encoding.UTF8);
                mail.To.Add(to);
                mail.SubjectEncoding = System.Text.Encoding.UTF8;
                mail.Subject = subject;
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.Body = body;
                mail.IsBodyHtml = isHtml;
                
                LoggingService.Log("EMAIL_BODY", string.Format("Email body prepared - Encoding: UTF-8, Length: {0}, IsHtml: {1}", body?.Length ?? 0, isHtml));
                if (body != null && body.Length < 500)
                {
                    LoggingService.Log("EMAIL_BODY_CONTENT", "Body content: " + body.Replace("\r", "").Replace("\n", " "));
                }

                string cleanPassword = smtpPassword?.Replace(" ", "") ?? "";
                
                using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Timeout = 120000;
                    
                    NetworkCredential credentials = new NetworkCredential(smtpUsername, cleanPassword);
                    smtp.Credentials = credentials;
                    
                    LoggingService.Log("EMAIL_SEND_ATTEMPT", string.Format("Attempting to send email via SMTP - Server: {0}, Port: {1}, SSL: True", smtpServer, smtpPort));
                    smtp.Send(mail);
                    LoggingService.LogEmailSending(to, subject, body, true);
                    LoggingService.Log("EMAIL_SEND_SUCCESS", string.Format("Email sent successfully - To: {0}, Subject: {1}", to, subject));
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogEmailSending(to, subject, body, false, ex.Message);
            LoggingService.Log("EMAIL_SEND_ERROR", string.Format("Email send failed - To: {0}, Subject: {1}", to, subject), ex);
            throw;
        }
    }

    public static void SendRegistrationEmail(string email, string firstName = "")
    {
        string greeting = string.IsNullOrEmpty(firstName) ? "שלום" : "שלום " + firstName;
        
        string subject = "נרשמת בהצלחה - OptiSched";
        string body = string.Format(@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""he"">
<head>
    <meta charset=""utf-8"">
</head>
<body style=""font-family: Arial, sans-serif; direction: rtl; text-align: right;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #e50914;"">ברוך הבא ל-OptiSched!</h2>
        <p>{0},</p>
        <p>ההרשמה שלך ל-OptiSched הושלמה בהצלחה!</p>
        <p>כעת תוכל להתחבר למערכת ולהתחיל להשתמש בכל התכונות:</p>
        <ul style=""line-height: 1.8;"">
            <li>ניהול לוח שנה אישי</li>
            <li>יצירת אירועים ומשימות</li>
            <li>שיתוף לוחות שנה עם צוותים</li>
            <li>תכנון זמן חכם</li>
        </ul>
        <p>אם יש לך שאלות או שאתה צריך עזרה, אנא צור איתנו קשר.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">OptiSched - תכנון חכם למקסימום יעילות</p>
    </div>
</body>
</html>", greeting);

        SendEmail(email, subject, body, true);
    }

    public static void SendLoginCodeEmail(string email, string code)
    {
        LoggingService.Log("EMAIL_LOGIN_CODE_START", string.Format("Preparing login code email - Email: {0}, Code: {1}, Code Length: {2}", email, code, code?.Length ?? 0));
        
        if (string.IsNullOrWhiteSpace(code))
        {
            LoggingService.Log("EMAIL_LOGIN_CODE_ERROR", "Code is null or empty", new ArgumentException("קוד לא יכול להיות ריק"));
            throw new ArgumentException("קוד לא יכול להיות ריק");
        }
        
        string subject = "קוד התחברות - OptiSched";
        string body = string.Format(@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""he"">
<head>
    <meta charset=""utf-8"">
</head>
<body style=""font-family: Arial, sans-serif; direction: rtl; text-align: right;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #e50914;"">קוד התחברות</h2>
        <p>קיבלנו בקשת התחברות לחשבון OptiSched שלך.</p>
        <p>קוד ההתחברות שלך הוא:</p>
        <div style=""background: #f5f5f5; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;"">
            <span style=""font-size: 32px; font-weight: bold; color: #e50914; letter-spacing: 8px; font-family: monospace;"">{0}</span>
        </div>
        <p>הזן את הקוד הזה בדף ההתחברות כדי לגשת לחשבון שלך.</p>
        <p style=""color: #666; font-size: 14px;""><strong>קוד זה תקף ל-15 דקות בלבד.</strong></p>
        <p>אם לא ביקשת קוד התחברות, אנא התעלם מהאימייל הזה.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">OptiSched - תכנון חכם למקסימום יעילות</p>
    </div>
</body>
        </html>", code);

        LoggingService.Log("EMAIL_LOGIN_CODE_BODY", string.Format("Login code email body created - Code in body: {0}, Body contains code: {1}, Body length: {2}", code, body.Contains(code), body?.Length ?? 0));
        LoggingService.Log("EMAIL_LOGIN_CODE_BODY_DETAIL", string.Format("Code details - Code: {0}, Code Length: {1}, Code Type: {2}, Code Characters: [{3}]", code, code?.Length ?? 0, code?.GetType().Name ?? "NULL", code != null ? string.Join(", ", code.ToCharArray()) : "NULL"));
        
        SendEmail(email, subject, body, true);
        LoggingService.Log("EMAIL_LOGIN_CODE_SENT", string.Format("Login code email sent successfully - Email: {0}, Code: {1}", email, code));
    }
}

