using System;
using System.Web;

/// <summary>
/// חיבור למסד הנתונים Access בלבד.
/// עזרים כלליים (כגון FixEncoding) נמצאים ב-Utilities.
/// </summary>
public class Connect
{
    private static string _cachedConnectionString;

    // בונה את מחרוזת החיבור לקובץ Access שבתיקיית App_Data.
    // מנסה קודם את הספק החדש 16.0, ואם הוא לא מותקן עוברת ל-12.0.
    // אחרי הצלחה ראשונה שומרת את המחרוזת בזיכרון כדי לא לבדוק שוב בכל בקשה.
    public static string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(_cachedConnectionString))
            return _cachedConnectionString;

        try
        {
            LoggingService.Log("CONNECT", "GetConnectionString called - detecting provider");

            if (HttpContext.Current == null)
            {
                LoggingService.Log("CONNECT", "HttpContext is null", new InvalidOperationException("HttpContext is not available"));
                throw new InvalidOperationException("HttpContext is not available");
            }

            string path = HttpContext.Current.Server.MapPath("~/App_Data/calnder.db1.accdb.mdb");
            LoggingService.Log("CONNECT", string.Format("Database path resolved: {0}", path ?? "null"));

            if (string.IsNullOrEmpty(path))
            {
                LoggingService.Log("CONNECT", "Database path is empty", new InvalidOperationException("Database path is empty"));
                throw new InvalidOperationException("Database path is empty");
            }

            if (!System.IO.File.Exists(path))
            {
                LoggingService.Log("CONNECT", string.Format("Database file does not exist: {0}", path));
            }

            string[] providers = new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" };

            foreach (var provider in providers)
            {
                string connectionString = @"Provider=" + provider + ";Data Source=" + path + ";Persist Security Info=False;";
                try
                {
                    using (var conn = new System.Data.OleDb.OleDbConnection(connectionString))
                    {
                        conn.Open();
                        LoggingService.Log("CONNECT", "Successfully connected using provider: " + provider);
                    }

                    _cachedConnectionString = connectionString;
                    return _cachedConnectionString;
                }
                catch (Exception ex)
                {
                    LoggingService.Log("CONNECT", "Failed to connect with provider: " + provider + " - " + ex.Message);
                }
            }

            LoggingService.Log("CONNECT", "All providers failed. Defaulting to 12.0");
            _cachedConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Persist Security Info=False;";
            return _cachedConnectionString;
        }
        catch (Exception ex)
        {
            LoggingService.Log("CONNECT", "Exception in GetConnectionString", ex);
            throw;
        }
    }
}
