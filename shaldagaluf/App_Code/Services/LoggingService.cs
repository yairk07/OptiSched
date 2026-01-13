using System;
using System.IO;
using System.Web;

public static class LoggingService
{
    // Gets the log file path for today's log file, creating directory if needed
    private static string GetLogFilePath()
    {
        // Use HttpContext if available, otherwise fall back to AppDomain base directory
        string logDir = HttpContext.Current != null
            ? HttpContext.Current.Server.MapPath("~/App_Data/Logs")
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");

        // Create log directory if it doesn't exist
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        // Generate filename with current date
        string fileName = "optischedule_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        return Path.Combine(logDir, fileName);
    }

    // Writes a log entry to file and debug output with optional exception details
    public static void Log(string category, string message, Exception ex = null)
    {
        try
        {
            // Format log entry with timestamp and category
            string logEntry = string.Format(
                "[{0}] [{1}] {2}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                category,
                message
            );

            // Append exception details if provided
            if (ex != null)
            {
                logEntry += Environment.NewLine + "Exception: " + ex.GetType().Name;
                logEntry += Environment.NewLine + "Message: " + ex.Message;
                logEntry += Environment.NewLine + "Stack Trace: " + ex.StackTrace;
                if (ex.InnerException != null)
                {
                    logEntry += Environment.NewLine + "Inner Exception: " + ex.InnerException.Message;
                }
            }

            logEntry += Environment.NewLine + new string('-', 80) + Environment.NewLine;

            // Append to log file and write to debug output
            string logFile = GetLogFilePath();
            File.AppendAllText(logFile, logEntry, System.Text.Encoding.UTF8);

            System.Diagnostics.Debug.WriteLine(logEntry);
        }
        catch
        {
            // Silently fail if logging itself fails (prevents infinite loops)
        }
    }

    // Logs login code generation events with detailed information
    public static void LogCodeGeneration(string email, string code, bool success, string error = null)
    {
        string message = string.Format(
            "Code Generation - Email: {0}, Code: {1}, Success: {2}",
            email ?? "NULL",
            code ?? "NULL",
            success
        );

        if (!string.IsNullOrEmpty(error))
        {
            message += ", Error: " + error;
        }

        if (code != null)
        {
            message += string.Format(", Code Length: {0}, Code Type: {1}", code.Length, code.GetType().Name);
            message += string.Format(", Code Characters: [{0}]", string.Join(", ", code.ToCharArray()));
        }

        Log("CODE_GENERATION", message);
    }

    // Logs email sending events with preview and status information
    public static void LogEmailSending(string to, string subject, string bodyPreview, bool success, string error = null)
    {
        string message = string.Format(
            "Email Sending - To: {0}, Subject: {1}, Success: {2}",
            to ?? "NULL",
            subject ?? "NULL",
            success
        );

        if (!string.IsNullOrEmpty(error))
        {
            message += ", Error: " + error;
        }

        if (bodyPreview != null)
        {
            string preview = bodyPreview.Length > 200 ? bodyPreview.Substring(0, 200) + "..." : bodyPreview;
            message += string.Format(", Body Preview: {0}", preview.Replace("\r", "").Replace("\n", " "));
            message += string.Format(", Body Length: {0}, Body Encoding: UTF-8", bodyPreview.Length);
        }

        Log("EMAIL_SENDING", message);
    }

    // Logs login code validation attempts with result and reason
    public static void LogCodeValidation(string email, string code, bool isValid, string reason = null)
    {
        string message = string.Format(
            "Code Validation - Email: {0}, Code: {1}, IsValid: {2}",
            email ?? "NULL",
            code ?? "NULL",
            isValid
        );

        if (!string.IsNullOrEmpty(reason))
        {
            message += ", Reason: " + reason;
        }

        if (code != null)
        {
            message += string.Format(", Code Length: {0}", code.Length);
        }

        Log("CODE_VALIDATION", message);
    }
}

