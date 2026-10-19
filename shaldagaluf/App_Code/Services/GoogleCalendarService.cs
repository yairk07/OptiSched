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

    // פונקציה: EnsureTokenColumns
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

    // פונקציה: EnsureColumn
    private static void EnsureColumn(OleDbConnection conn, string columnName, string type)
    {
        if (ColumnExists(conn, "Users", columnName))
            return;

        using (OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Users] ADD COLUMN [" + columnName + "] " + type, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }

    // בדיקה אם עמודה קיימת בטבלה
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

    // בדיקת תנאי: HasCalendarAccess
    public static bool HasCalendarAccess(int userId)
    {
        EnsureTokenColumns();
        string refresh;
        string access;
        DateTime? expiry;
        LoadTokens(userId, out access, out refresh, out expiry);

        if (HttpContext.Current != null && HttpContext.Current.Session != null)
        {
            if (string.IsNullOrEmpty(access))
                access = HttpContext.Current.Session["GoogleAccessToken"] as string;
            if (string.IsNullOrEmpty(refresh))
                refresh = HttpContext.Current.Session["GoogleRefreshToken"] as string;
        }

        // Ignore short junk values left from tests / truncated saves.
        bool hasAccess = !string.IsNullOrEmpty(access) && access.Length > 40;
        bool hasRefresh = !string.IsNullOrEmpty(refresh) && refresh.Length > 20;
        return hasAccess || hasRefresh;
    }

    // פונקציה: ClearTokens
    public static void ClearTokens(int userId)
    {
        EnsureTokenColumns();
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "UPDATE [Users] SET [GoogleAccessToken]=Null, [GoogleRefreshToken]=Null, [GoogleTokenExpiry]=Null WHERE [Id]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter idParam = cmd.Parameters.Add("?", OleDbType.VarWChar);
                idParam.Value = userId.ToString();
                if (cmd.ExecuteNonQuery() == 0)
                {
                    cmd.Parameters.Clear();
                    idParam = cmd.Parameters.Add("?", OleDbType.Integer);
                    idParam.Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        if (HttpContext.Current != null && HttpContext.Current.Session != null)
        {
            HttpContext.Current.Session.Remove("GoogleAccessToken");
            HttpContext.Current.Session.Remove("GoogleRefreshToken");
            HttpContext.Current.Session.Remove("GoogleTokenExpiryUtc");
        }
    }

    // שליפת נתונים: GetUserPersonalEvents
    public static DataTable GetUserPersonalEvents(int userId)
    {
        DataTable dt = new DataTable();
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT * FROM [CalendarEvents] WHERE [UserId]=? ORDER BY [EventDate], [Id]";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter p = cmd.Parameters.Add("?", OleDbType.Integer);
                p.Value = userId;
                using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                // Some DBs store UserId as text.
                using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM [CalendarEvents] WHERE [UserId]=? ORDER BY [Id]", conn))
                {
                    OleDbParameter p = cmd.Parameters.Add("?", OleDbType.VarWChar);
                    p.Value = userId.ToString();
                    using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                    {
                        dt.Clear();
                        da.Fill(dt);
                    }
                }
            }
        }

        foreach (string col in new[] { "Title", "Notes", "Category", "EventTime" })
        {
            if (!dt.Columns.Contains(col))
                continue;
            foreach (DataRow row in dt.Rows)
            {
                if (row[col] != null && row[col] != DBNull.Value)
                    row[col] = Connect.FixEncoding(Convert.ToString(row[col]));
            }
        }

        return dt;
    }

    // פונקציה: BuildIcsForUser
    public static string BuildIcsForUser(int userId)
    {
        DataTable events = GetUserPersonalEvents(userId);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//OptiSched//Google Export//HE");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:OptiSched");

        int index = 0;
        foreach (DataRow row in events.Rows)
        {
            index++;
            string title = EscapeIcsText(GetString(row, "Title", "title"));
            if (string.IsNullOrWhiteSpace(title))
                title = "OptiSched Event";

            string notes = GetString(row, "Notes", "notes");
            string category = GetString(row, "Category", "category");
            string timeText = GetString(row, "EventTime", "time");
            string description = EscapeIcsText(
                (string.IsNullOrWhiteSpace(category) ? "" : ("\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4: " + category + "\\n")) +
                (notes ?? "") +
                "\\n\u05D9\u05D5\u05E6\u05D0 \u05DE-OptiSched");

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

            string uid = "optischedule-" + userId + "-" + GetString(row, "Id") + "-" + index + "@optischedule.local";
            string dtStamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            string dtStart = startLocal.ToString("yyyyMMdd'T'HHmmss");
            string dtEnd = endLocal.ToString("yyyyMMdd'T'HHmmss");

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine("UID:" + uid);
            sb.AppendLine("DTSTAMP:" + dtStamp);
            sb.AppendLine("DTSTART;TZID=Asia/Jerusalem:" + dtStart);
            sb.AppendLine("DTEND;TZID=Asia/Jerusalem:" + dtEnd);
            sb.AppendLine("SUMMARY:" + title);
            if (!string.IsNullOrWhiteSpace(description))
                sb.AppendLine("DESCRIPTION:" + description);
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    // פונקציה: EscapeIcsText
    private static string EscapeIcsText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }

    // ביצוע פעולה: ExportUserEventsToGoogle
    public static ExportResult ExportUserEventsToGoogle(int userId)
    {
        string accessToken = GetValidAccessToken(userId);
        if (string.IsNullOrEmpty(accessToken) || accessToken.Length < 40)
            throw new InvalidOperationException("NO_CALENDAR_TOKEN");

        DataTable all = GetUserPersonalEvents(userId);
        ExportResult result = new ExportResult { Total = all != null ? all.Rows.Count : 0 };
        if (all == null || all.Rows.Count == 0)
            return result;

        foreach (DataRow row in all.Rows)
        {
            try
            {
                if (CreateGoogleEvent(accessToken, row))
                    result.Exported++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.LastError = ex.Message;
                LoggingService.Log("GOOGLE_CALENDAR_EXPORT_ITEM_ERROR", "Failed to export one event: " + ex.Message, ex);

                string body = Convert.ToString(ex.Message);
                if (body.Contains("(401)") || body.IndexOf("UNAUTHENTICATED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    body.IndexOf("Invalid Credentials", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ClearTokens(userId);
                    throw new InvalidOperationException("NO_CALENDAR_TOKEN");
                }
                if (body.IndexOf("Calendar API", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    body.IndexOf("accessNotConfigured", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    body.IndexOf("Cloud Console", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw; // fail fast — API disabled for the whole export
                }
            }
        }

        return result;
    }

    // ביצוע פעולה: ExportSingleEventToGoogle
    public static bool ExportSingleEventToGoogle(int userId, int eventId)
    {
        string accessToken = GetValidAccessToken(userId);
        if (string.IsNullOrEmpty(accessToken) || accessToken.Length < 40)
            throw new InvalidOperationException("NO_CALENDAR_TOKEN");

        DataTable all = GetUserPersonalEvents(userId);
        if (all == null)
            return false;

        foreach (DataRow row in all.Rows)
        {
            if (!row.Table.Columns.Contains("Id") || row["Id"] == DBNull.Value)
                continue;

            int id;
            if (!int.TryParse(Convert.ToString(row["Id"]), out id) || id != eventId)
                continue;

            try
            {
                return CreateGoogleEvent(accessToken, row);
            }
            catch (Exception ex)
            {
                if ((ex.Message ?? "").Contains("(401)"))
                {
                    ClearTokens(userId);
                    throw new InvalidOperationException("NO_CALENDAR_TOKEN");
                }
                throw;
            }
        }

        return false;
    }

    // שליפת נתונים: GetCalendarAuthorizationUrl
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

    // החלפת קוד OAuth בטוקנים
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

    // שמירת טוקני Google למשתמש
    public static void SaveTokens(int userId, GoogleTokenSet tokens)
    {
        if (tokens == null || userId < 0)
            return;

        EnsureTokenColumns();
        string refreshToSave = tokens.RefreshToken ?? "";
        string connectionString = Connect.GetConnectionString();
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string existingRefresh;
            string existingAccess;
            DateTime? existingExpiry;
            LoadTokens(userId, out existingAccess, out existingRefresh, out existingExpiry);

            if (string.IsNullOrEmpty(refreshToSave))
                refreshToSave = existingRefresh ?? "";
            // Access rejects UTC DateTime from AddWithValue — store as local OleDb Date.
            DateTime expiryLocal = tokens.ExpiryUtc.Kind == DateTimeKind.Utc
                ? tokens.ExpiryUtc.ToLocalTime()
                : DateTime.SpecifyKind(tokens.ExpiryUtc, DateTimeKind.Local);

            string sql = "UPDATE [Users] SET [GoogleAccessToken]=?, [GoogleRefreshToken]=?, [GoogleTokenExpiry]=? WHERE [Id]=?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter accessParam = cmd.Parameters.Add("?", OleDbType.LongVarWChar);
                accessParam.Value = tokens.AccessToken ?? "";

                OleDbParameter refreshParam = cmd.Parameters.Add("?", OleDbType.LongVarWChar);
                refreshParam.Value = refreshToSave ?? "";

                OleDbParameter expiryParam = cmd.Parameters.Add("?", OleDbType.Date);
                expiryParam.Value = expiryLocal;

                // Users.Id is text in this Access DB.
                OleDbParameter idParam = cmd.Parameters.Add("?", OleDbType.VarWChar);
                idParam.Value = userId.ToString();

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    // Fallback if Id was stored as numeric matching.
                    cmd.Parameters.Clear();
                    accessParam = cmd.Parameters.Add("?", OleDbType.LongVarWChar);
                    accessParam.Value = tokens.AccessToken ?? "";
                    refreshParam = cmd.Parameters.Add("?", OleDbType.LongVarWChar);
                    refreshParam.Value = refreshToSave ?? "";
                    expiryParam = cmd.Parameters.Add("?", OleDbType.Date);
                    expiryParam.Value = expiryLocal;
                    idParam = cmd.Parameters.Add("?", OleDbType.Integer);
                    idParam.Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Session backup for immediate export after OAuth.
        if (HttpContext.Current != null && HttpContext.Current.Session != null)
        {
            HttpContext.Current.Session["GoogleAccessToken"] = tokens.AccessToken;
            HttpContext.Current.Session["GoogleRefreshToken"] = refreshToSave;
            HttpContext.Current.Session["GoogleTokenExpiryUtc"] = tokens.ExpiryUtc;
        }
    }

    // שליפת נתונים: GetValidAccessToken
    public static string GetValidAccessToken(int userId)
    {
        EnsureTokenColumns();
        string access;
        string refresh;
        DateTime? expiry;
        LoadTokens(userId, out access, out refresh, out expiry);

        if (string.IsNullOrEmpty(access) && HttpContext.Current != null && HttpContext.Current.Session != null)
        {
            access = HttpContext.Current.Session["GoogleAccessToken"] as string;
            refresh = HttpContext.Current.Session["GoogleRefreshToken"] as string ?? refresh;
            object expObj = HttpContext.Current.Session["GoogleTokenExpiryUtc"];
            if (expObj is DateTime)
                expiry = (DateTime)expObj;
        }

        if (!string.IsNullOrEmpty(access) && expiry.HasValue && expiry.Value > DateTime.UtcNow.AddMinutes(1))
            return access;

        // Access token still usable without known expiry for a short window.
        if (!string.IsNullOrEmpty(access) && !expiry.HasValue)
            return access;

        if (string.IsNullOrEmpty(refresh))
            return null;

        GoogleTokenSet refreshed = RefreshAccessToken(refresh);
        if (string.IsNullOrEmpty(refreshed.RefreshToken))
            refreshed.RefreshToken = refresh;
        SaveTokens(userId, refreshed);
        return refreshed.AccessToken;
    }

    // פונקציה: RefreshAccessToken
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

    // טעינה וקישור נתונים: LoadTokens
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
                OleDbParameter idParam = cmd.Parameters.Add("?", OleDbType.VarWChar);
                idParam.Value = userId.ToString();
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        ReadTokenRow(dr, out accessToken, out refreshToken, out expiryUtc);
                        return;
                    }
                }
            }

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                OleDbParameter idParam = cmd.Parameters.Add("?", OleDbType.Integer);
                idParam.Value = userId;
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        ReadTokenRow(dr, out accessToken, out refreshToken, out expiryUtc);
                }
            }
        }
    }

    // פונקציה: ReadTokenRow
    private static void ReadTokenRow(OleDbDataReader dr, out string accessToken, out string refreshToken, out DateTime? expiryUtc)
    {
        accessToken = null;
        refreshToken = null;
        expiryUtc = null;
        if (dr["GoogleAccessToken"] != DBNull.Value)
            accessToken = Convert.ToString(dr["GoogleAccessToken"]);
        if (dr["GoogleRefreshToken"] != DBNull.Value)
            refreshToken = Convert.ToString(dr["GoogleRefreshToken"]);
        if (dr["GoogleTokenExpiry"] != DBNull.Value)
            expiryUtc = Convert.ToDateTime(dr["GoogleTokenExpiry"]).ToUniversalTime();
    }

    // ביצוע פעולה: CreateGoogleEvent
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
            description = "\u05E7\u05D8\u05D2\u05D5\u05E8\u05D9\u05D4: " + category + (string.IsNullOrWhiteSpace(description) ? "" : "\n" + description);

        description += "\n\n\u05D9\u05D5\u05E6\u05D0 \u05DE-OptiSched";

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

    // פונקציה: ParseEventTimes
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

    // שליפת נתונים: GetString
    private static string GetString(DataRow row, params string[] names)
    {
        foreach (string name in names)
        {
            if (row.Table.Columns.Contains(name) && row[name] != null && row[name] != DBNull.Value)
                return Convert.ToString(row[name]);
        }
        return "";
    }

    // פונקציה: ReadWebExceptionBody
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

public class ExportResult
{
    public int Total { get; set; }
    public int Exported { get; set; }
    public int Failed { get; set; }
    public string LastError { get; set; }
}

