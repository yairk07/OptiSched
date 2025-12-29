using System;
using System.Text;
using System.Web;
  
public class Connect
{
    private const string calnder = "calnder.db1.accdb.mdb";

    public static string GetConnectionString()
    {
        string location = HttpContext.Current.Server.MapPath("~/App_Data/" + calnder);
        string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + location;
        return connectionString;
    }

    public static string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        try
        {
            if (IsValidUtf8(text))
                return text;
            
            byte[] bytes = Encoding.GetEncoding("Windows-1255").GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return text;
        }
    }
    
    private static bool IsValidUtf8(string text)
    {
        try
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
            string decoded = Encoding.UTF8.GetString(utf8Bytes);
            return decoded == text;
        }
        catch
        {
            return false;
        }
    }
}
