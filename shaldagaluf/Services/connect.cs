using System;
using System.Text;
using System.Web;

public class Connect
{
    public static string GetConnectionString()
    {
        try
        {
            if (HttpContext.Current == null)
            {
                throw new InvalidOperationException("HttpContext is not available");
            }
            
            string path = HttpContext.Current.Server.MapPath("~/App_Data/calnder.db1.accdb.mdb");
            
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Database path is empty");
            }
            
            if (!System.IO.File.Exists(path))
            {
            }
            
            return @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Persist Security Info=False;";
        }
        catch
        {
            throw;
        }
    }

    public static string FixEncoding(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";

        string text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return "";

        try
        {
            Encoding hebrew1255 = Encoding.GetEncoding("Windows-1255");
            Encoding utf8 = Encoding.UTF8;
            
            byte[] textAsUtf8 = utf8.GetBytes(text);
            string from1255 = hebrew1255.GetString(textAsUtf8);
            
            if (IsValidHebrewText(from1255))
                return from1255;
            
            byte[] textAs1255 = hebrew1255.GetBytes(text);
            string fromUtf8 = utf8.GetString(textAs1255);
            
            if (IsValidHebrewText(fromUtf8))
                return fromUtf8;
            
            byte[] textAsDefault = Encoding.Default.GetBytes(text);
            string from1255Default = hebrew1255.GetString(textAsDefault);
            
            if (IsValidHebrewText(from1255Default))
                return from1255Default;
            
            return text;
        }
        catch
        {
            return text;
        }
    }

    private static bool IsValidHebrewText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        
        bool hasHebrew = false;
        bool hasInvalidChars = false;
        
        foreach (char c in text)
        {
            if (c >= 0x0590 && c <= 0x05FF)
            {
                hasHebrew = true;
            }
            else if (c >= 0x0080 && c <= 0x009F && c != 0x00A0)
            {
                hasInvalidChars = true;
            }
        }
        
        return hasHebrew && !hasInvalidChars;
    }
}

