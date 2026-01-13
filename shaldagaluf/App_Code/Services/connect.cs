using System;
using System.Text;
using System.Web;

public class Connect
{
    // Gets the database connection string for Microsoft Access database
    public static string GetConnectionString()
    {
        try
        {
            LoggingService.Log("CONNECT", "GetConnectionString called");

            // Verify HttpContext is available (required for Server.MapPath)
            if (HttpContext.Current == null)
            {
                LoggingService.Log("CONNECT", "HttpContext is null", new InvalidOperationException("HttpContext is not available"));
                throw new InvalidOperationException("HttpContext is not available");
            }

            // Resolve database file path
            string path = HttpContext.Current.Server.MapPath("~/App_Data/calnder.db1.accdb.mdb");
            LoggingService.Log("CONNECT", string.Format("Database path resolved: {0}", path ?? "null"));

            if (string.IsNullOrEmpty(path))
            {
                LoggingService.Log("CONNECT", "Database path is empty", new InvalidOperationException("Database path is empty"));
                throw new InvalidOperationException("Database path is empty");
            }

            // Warn if database file doesn't exist (but don't fail)
            if (!System.IO.File.Exists(path))
            {
                LoggingService.Log("CONNECT", string.Format("Database file does not exist: {0}", path));
            }

            // Build Access connection string with ACE provider
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Persist Security Info=False;";
            LoggingService.Log("CONNECT", "GetConnectionString completed successfully");

            return connectionString;
        }
        catch (Exception ex)
        {
            LoggingService.Log("CONNECT", "Exception in GetConnectionString", ex);
            throw;
        }
    }

    // Fixes Hebrew text encoding by trying multiple encoding conversions until valid Hebrew is found
    public static string FixEncoding(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";

        string text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return "";

        // Check if text already contains valid Hebrew - if yes, return as-is
        if (IsValidHebrewText(text))
        {
            return text;
        }

        try
        {
            Encoding hebrew1255 = Encoding.GetEncoding("Windows-1255");
            Encoding utf8 = Encoding.UTF8;

            // Strategy 1: Text might be UTF-8 bytes interpreted as Windows-1255
            // Convert: read as Windows-1255 bytes, interpret as UTF-8
            try
            {
                byte[] as1255Bytes = hebrew1255.GetBytes(text);
                string asUtf8 = utf8.GetString(as1255Bytes);
                if (IsValidHebrewText(asUtf8))
                    return asUtf8;
            }
            catch { }

            // Strategy 2: Text might be Windows-1255 bytes interpreted as UTF-8
            // Convert: read as UTF-8 bytes, interpret as Windows-1255
            try
            {
                byte[] asUtf8Bytes = utf8.GetBytes(text);
                string as1255 = hebrew1255.GetString(asUtf8Bytes);
                if (IsValidHebrewText(as1255))
                    return as1255;
            }
            catch { }

            // Strategy 3: Text might be in default encoding, convert to UTF-8 via Windows-1255
            try
            {
                byte[] asDefaultBytes = Encoding.Default.GetBytes(text);
                string as1255FromDefault = hebrew1255.GetString(asDefaultBytes);
                if (IsValidHebrewText(as1255FromDefault))
                    return as1255FromDefault;
            }
            catch { }

            // Strategy 4: Try reading the original string as if it's already UTF-8
            try
            {
                byte[] originalBytes = Encoding.Default.GetBytes(text);
                string asUtf8FromDefault = utf8.GetString(originalBytes);
                if (IsValidHebrewText(asUtf8FromDefault))
                    return asUtf8FromDefault;
            }
            catch { }

            // Return original if no valid conversion found
            return text;
        }
        catch (Exception ex)
        {
            LoggingService.Log("CONNECT", "Error in FixEncoding", ex);
            return text;
        }
    }

    // Checks if text contains valid Hebrew characters and no invalid control characters
    private static bool IsValidHebrewText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        bool hasHebrew = false;
        bool hasInvalidChars = false;

        foreach (char c in text)
        {
            // Check if character is in Hebrew Unicode range (0x0590-0x05FF)
            if (c >= 0x0590 && c <= 0x05FF)
            {
                hasHebrew = true;
            }
            // Check for invalid control characters (excluding non-breaking space 0x00A0)
            else if (c >= 0x0080 && c <= 0x009F && c != 0x00A0)
            {
                hasInvalidChars = true;
            }
        }

        // Text is valid if it contains Hebrew characters and no invalid control chars
        return hasHebrew && !hasInvalidChars;
    }
}

