using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

/// <summary>
/// Exports OptiSched events into the signed-in user's Google Calendar via Calendar API v3.
/// </summary>
public class GoogleCalendarService
{
    private const string CalendarEventsScope = "https://www.googleapis.com/auth/calendar.events";
    private const string CalendarApiUrl = "https://www.googleapis.com/calendar/v3/calendars/primary/events";

    public static void EnsureTokenColumns()
    {
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            EnsureColumn(conn, "GoogleAccessToken", "MEMO");
            EnsureColumn(conn, "GoogleRefreshToken", "MEMO");
            EnsureColumn(conn, "GoogleTokenExpiry", "DATETIME");
        }
    }

    private static void EnsureColumn(OleDbConnection conn, string columnName, string type)
    {
        if (ColumnExists(conn, "Users", columnName))
            return;

        using (OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Users] ADD COLUMN [" + columnName + "] " + type, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }

    private static bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + columnName + "] FROM [" + tableName + "]", conn))
            {
                cmd.ExecuteScalar();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool HasCalendarAccess(int userId)
    {
        EnsureTokenColumns();
        string refresh;
        string access;
        DateTime? expiry;
        LoadTokens(userId, out access, out refresh, out expiry);
        return !string.IsNullOrEmpty(refresh) || !string.IsNullOrEmpty(access);
    }

    public static string GetCalendarAuthorizationUrl()
    {
        if (!GoogleOAuthService.IsConfigured())
            throw new Exception("Google OAuth לא מוגדר ב-Web.config");

        if (HttpContext.Current == null || HttpContext.Current.Session == null)
            throw new Exception("Session לא זמין");

        string state = Guid.NewGuid().ToString("N");
        HttpContext.Current.Session["OAuthState"] = state;
        HttpContext.Current.Session["OAuthStartedUtc"] = DateTime.UtcNow;
        HttpContext.Current.Session["OAuthPurpose"] = "calendar_export";

        string clientId = System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "";
        string redirectUri = GoogleOAuthService.GetConfiguredRedirectUri();
        string scope = "openid email profile " + CalendarEventsScope;

        return "https://accounts.google.com/o/oauth2/v2/auth?" +
            "client_id=" + Uri.EscapeDataString(clientId.Trim()) + "&" +
            "redirect_uri=" + Uri.EscapeDataString(redirectUri) + "&" +
            "response_type=code&" +
            "scope=" + Uri.EscapeDataString(scope) + "&" +
            "state=" + Uri.EscapeDataString(state) + "&" +
            "access_type=offline&" +
            "include_granted_scopes=true&" +
            "prompt=consent";
    }

    public static GoogleTokenSet ExchangeCodeForTokens(string code)
    {
        GoogleOAuthService.EnsureTls12Public();

        string clientId = (System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "").Trim();
        string clientSecret = (System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientSecret"] ?? "").Trim();
        string redirectUri = GoogleOAuthService.GetConfiguredRedirectUri();

        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

            NameValueCollection postData = new NameValueCollection();
            postData["code"] = code;
            postData["client_id"] = clientId;
            postData["client_secret"] = clientSecret;
            postData["redirect_uri"] = redirectUri;
            postData["grant_type"] = "authorization_code";

            byte[] responseBytes = client.UploadValues("https://oauth2.googleapis.com/token", "POST", postData);
            string response = Encoding.UTF8.GetString(responseBytes);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var tokenResponse = serializer.Deserialize<Dictionary<string, object>>(response);

            string accessToken = tokenResponse.ContainsKey("access_token") ? Convert.ToString(tokenResponse["access_token"]) : "";
            string refreshToken = tokenResponse.ContainsKey("refresh_token") ? Convert.ToString(tokenResponse["refresh_token"]) : "";
            int expiresIn = 3600;
            if (tokenResponse.ContainsKey("expires_in"))
                int.TryParse(Convert.ToString(tokenResponse["expires_in"]), out expiresIn);

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("לא התקבל access token מ-Google");

            return new GoogleTokenSet
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiryUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60))
            };
        }
    }

    public static void SaveTokens(int userId, GoogleTokenSet tokens)
    {
        if (tokens == null || userId < 0)
            return;

        EnsureTokenColumns();
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string existingRefresh;
            string existingAccess;
            DateTime? existingExpiry;
            LoadTokens(userId, out existingAccess, out existingRefresh, out existingExpiry);

            string refreshToSave = !string.IsNullOrEmpty(tokens.RefreshToken) ? tokens.RefreshToken : existingRefresh;

            string sql = "UPDATE [Users] SET [GoogleAccessToken]=?, [GoogleRefreshToken]=?, [GoogleTokenExpiry]=? WHERE [Id]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", tokens.AccessToken ?? "");
                cmd.Parameters.AddWithValue("?", refreshToSave ?? "");
                cmd.Parameters.AddWithValue("?", tokens.ExpiryUtc);
                cmd.Parameters.AddWithValue("?", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static string GetValidAccessToken(int userId)
    {
        EnsureTokenColumns();
        string access;
        string refresh;
        DateTime? expiry;
        LoadTokens(userId, out access, out refresh, out expiry);

        if (!string.IsNullOrEmpty(access) && expiry.HasValue && expiry.Value > DateTime.UtcNow.AddMinutes(1))
            return access;

        if (string.IsNullOrEmpty(refresh))
            return null;

        GoogleTokenSet refreshed = RefreshAccessToken(refresh);
        if (string.IsNullOrEmpty(refreshed.RefreshToken))
            refreshed.RefreshToken = refresh;
        SaveTokens(userId, refreshed);
        return refreshed.AccessToken;
    }

    private static GoogleTokenSet RefreshAccessToken(string refreshToken)
    {
        GoogleOAuthService.EnsureTls12Public();

        string clientId = (System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientId"] ?? "").Trim();
        string clientSecret = (System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientSecret"] ?? "").Trim();

        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

            NameValueCollection postData = new NameValueCollection();
            postData["client_id"] = clientId;
            postData["client_secret"] = clientSecret;
            postData["refresh_token"] = refreshToken;
            postData["grant_type"] = "refresh_token";

            byte[] responseBytes = client.UploadValues("https://oauth2.googleapis.com/token", "POST", postData);
            string response = Encoding.UTF8.GetString(responseBytes);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var tokenResponse = serializer.Deserialize<Dictionary<string, object>>(response);

            string accessToken = tokenResponse.ContainsKey("access_token") ? Convert.ToString(tokenResponse["access_token"]) : "";
            int expiresIn = 3600;
            if (tokenResponse.ContainsKey("expires_in"))
                int.TryParse(Convert.ToString(tokenResponse["expires_in"]), out expiresIn);

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("רענון access token מ-Google נכשל");

            return new GoogleTokenSet
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiryUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60))
            };
        }
    }

    private static void LoadTokens(int userId, out string accessToken, out string refreshToken, out DateTime? expiryUtc)
    {
        accessToken = null;
        refreshToken = null;
        expiryUtc = null;

        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            if (!ColumnExists(conn, "Users", "GoogleAccessToken"))
                return;

            string sql = "SELECT [GoogleAccessToken], [GoogleRefreshToken], [GoogleTokenExpiry] FROM [Users] WHERE [Id]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", userId);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        return;

                    if (dr["GoogleAccessToken"] != DBNull.Value)
                        accessToken = Convert.ToString(dr["GoogleAccessToken"]);
                    if (dr["GoogleRefreshToken"] != DBNull.Value)
                        refreshToken = Convert.ToString(dr["GoogleRefreshToken"]);
                    if (dr["GoogleTokenExpiry"] != DBNull.Value)
                        expiryUtc = Convert.ToDateTime(dr["GoogleTokenExpiry"]).ToUniversalTime();
                }
            }
        }
    }

    public static int ExportUserEventsToGoogle(int userId)
    {
        string accessToken = GetValidAccessToken(userId);
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("NO_CALENDAR_TOKEN");

        EventService es = new EventService();
        DataTable all = es.GetAllEvents(userId);
        if (all == null || all.Rows.Count == 0)
            return 0;

        int exported = 0;
        foreach (DataRow row in all.Rows)
        {
            if (!IsOwnedByUser(row, userId))
                continue;

            try
            {
                if (CreateGoogleEvent(accessToken, row))
                    exported++;
            }
            catch (Exception ex)
            {
                LoggingService.Log("GOOGLE_CALENDAR_EXPORT_ITEM_ERROR", "Failed to export one event: " + ex.Message, ex);
            }
        }

        return exported;
    }

    public static bool ExportSingleEventToGoogle(int userId, int eventId)
    {
        string accessToken = GetValidAccessToken(userId);
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("NO_CALENDAR_TOKEN");

        EventService es = new EventService();
        DataTable all = es.GetAllEvents(userId);
        if (all == null)
            return false;

        foreach (DataRow row in all.Rows)
        {
            if (!row.Table.Columns.Contains("Id") || row["Id"] == DBNull.Value)
                continue;

            int id;
            if (!int.TryParse(Convert.ToString(row["Id"]), out id) || id != eventId)
                continue;

            if (!IsOwnedByUser(row, userId))
                continue;

            return CreateGoogleEvent(accessToken, row);
        }

        return false;
    }

    private static bool IsOwnedByUser(DataRow row, int userId)
    {
        if (row.Table.Columns.Contains("UserId") && row["UserId"] != null && row["UserId"] != DBNull.Value)
        {
            int rowUserId;
            if (int.TryParse(Convert.ToString(row["UserId"]), out rowUserId))
                return rowUserId == userId;
        }
        return true;
    }

    private static bool CreateGoogleEvent(string accessToken, DataRow row)
    {
        string title = GetString(row, "Title", "title");
        if (string.IsNullOrWhiteSpace(title))
            title = "אירוע OptiSched";

        string notes = GetString(row, "Notes", "notes");
        string category = GetString(row, "Category", "category");
        string timeText = GetString(row, "EventTime", "time");

        DateTime eventDate = DateTime.Today;
        object dateObj = null;
        if (row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value)
            dateObj = row["EventDate"];
        else if (row.Table.Columns.Contains("date") && row["date"] != DBNull.Value)
            dateObj = row["date"];

        if (dateObj != null)
            eventDate = Convert.ToDateTime(dateObj).Date;

        DateTime startLocal;
        DateTime endLocal;
        ParseEventTimes(eventDate, timeText, out startLocal, out endLocal);

        string description = notes ?? "";
        if (!string.IsNullOrWhiteSpace(category))
            description = "קטגוריה: " + category + (string.IsNullOrWhiteSpace(description) ? "" : "\n" + description);

        description += "\n\nיוצא מ-OptiSched";

        var payload = new Dictionary<string, object>();
        payload["summary"] = title;
        payload["description"] = description;
        payload["start"] = new Dictionary<string, object>
        {
            { "dateTime", startLocal.ToString("yyyy-MM-dd'T'HH:mm:ss") },
            { "timeZone", "Asia/Jerusalem" }
        };
        payload["end"] = new Dictionary<string, object>
        {
            { "dateTime", endLocal.ToString("yyyy-MM-dd'T'HH:mm:ss") },
            { "timeZone", "Asia/Jerusalem" }
        };

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        string json = serializer.Serialize(payload);

        GoogleOAuthService.EnsureTls12Public();
        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;
            client.Headers[HttpRequestHeader.ContentType] = "application/json";

            try
            {
                client.UploadString(CalendarApiUrl, "POST", json);
                return true;
            }
            catch (WebException webEx)
            {
                string body = ReadWebExceptionBody(webEx);
                LoggingService.Log("GOOGLE_CALENDAR_API_ERROR", body, webEx);
                if (body != null && (body.Contains("accessNotConfigured") || body.Contains("Calendar API")))
                    throw new Exception("יש להפעיל Google Calendar API בפרויקט ב-Google Cloud Console.");
                throw;
            }
        }
    }

    private static void ParseEventTimes(DateTime date, string timeText, out DateTime startLocal, out DateTime endLocal)
    {
        startLocal = date.AddHours(9);
        endLocal = startLocal.AddHours(1);

        if (string.IsNullOrWhiteSpace(timeText))
            return;

        MatchCollection matches = Regex.Matches(timeText, @"(\d{1,2}):(\d{2})");
        if (matches.Count >= 1)
        {
            int h = int.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture);
            int m = int.Parse(matches[0].Groups[2].Value, CultureInfo.InvariantCulture);
            if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
            {
                startLocal = date.AddHours(h).AddMinutes(m);
                endLocal = startLocal.AddHours(1);
            }
        }
        if (matches.Count >= 2)
        {
            int h = int.Parse(matches[1].Groups[1].Value, CultureInfo.InvariantCulture);
            int m = int.Parse(matches[1].Groups[2].Value, CultureInfo.InvariantCulture);
            if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
            {
                DateTime parsedEnd = date.AddHours(h).AddMinutes(m);
                if (parsedEnd > startLocal)
                    endLocal = parsedEnd;
            }
        }
    }

    private static string GetString(DataRow row, params string[] names)
    {
        foreach (string name in names)
        {
            if (row.Table.Columns.Contains(name) && row[name] != null && row[name] != DBNull.Value)
                return Convert.ToString(row[name]);
        }
        return "";
    }

    private static string ReadWebExceptionBody(WebException webEx)
    {
        try
        {
            if (webEx.Response == null)
                return webEx.Message;
            using (var stream = webEx.Response.GetResponseStream())
            {
                if (stream == null)
                    return webEx.Message;
                using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
        catch
        {
            return webEx.Message;
        }
    }
}

public class GoogleTokenSet
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiryUtc { get; set; }
}
