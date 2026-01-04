using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class home : System.Web.UI.Page
{
    private enum CalendarDisplayMode { Gregorian, Hebrew }

    calnderservice calendarService = new calnderservice();
    private DataSet allEvents;
    private static readonly CultureInfo heCulture = new CultureInfo("he-IL");
    private static readonly HebrewCalendar HebrewCal = new HebrewCalendar();
    private static readonly TimeZoneInfo IsraelTimeZone = ResolveIsraelTimeZone();
    private static readonly Dictionary<string, string> ParashaHebrewMap = CreateParashaMap();
    private static readonly Dictionary<string, string> HolidayHebrewMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Chanukah", "חנוכה" }, { "Hanukkah", "חנוכה" }, { "Purim", "פורים" }, { "Passover", "פסח" },
        { "Pesach", "פסח" }, { "Shavuot", "שבועות" }, { "Sukkot", "סוכות" }, { "Rosh Hashana", "ראש השנה" },
        { "Yom Kippur", "יום כיפור" }, { "Lag BaOmer", "ל\"ג בעומר" }, { "Tu BiShvat", "ט\"ו בשבט" },
        { "Tu B'Shvat", "ט\"ו בשבט" }
    };
    private static readonly string[] HebrewOnes = { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
    private static readonly string[] HebrewTens = { "", "י", "כ", "ל" };

    private CalendarDisplayMode CurrentMode
    {
        get
        {
            if (ViewState["CalendarMode"] is string value && Enum.TryParse(value, out CalendarDisplayMode mode))
                return mode;
            return CalendarDisplayMode.Gregorian;
        }
        set
        {
            ViewState["CalendarMode"] = value.ToString();
        }
    }

    private void RenderDayNumber(DayRenderEventArgs e, DateTime date)
    {
        string text = CurrentMode == CalendarDisplayMode.Hebrew
            ? GetHebrewDayLabel(date)
            : date.Day.ToString();

        bool updated = false;
        foreach (Control control in e.Cell.Controls)
        {
            if (control is LinkButton link)
            {
                link.Text = string.Empty;
                link.Controls.Clear();
                link.Controls.Add(new LiteralControl(text));
                updated = true;
                break;
            }

            if (control is LiteralControl literal && !string.IsNullOrWhiteSpace(literal.Text))
            {
                literal.Text = text;
                updated = true;
                break;
            }
        }

        if (!updated)
            e.Cell.Text = text;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        if (!IsPostBack)
        {
            // בדיקת התחברות
            if (Session["username"] != null)
            {
                string name = Session["username"].ToString();
                lblWelcome.Text = $"<h3>ברוך הבא, {name}!</h3>";
                
                string googleEmail = Session["GoogleEmailToStore"]?.ToString();
                if (!string.IsNullOrEmpty(googleEmail))
                {
                    Session.Remove("GoogleEmailToStore");
                    string script = $@"
                        <script type='text/javascript'>
                            try {{
                                localStorage.setItem('googleLoginEmail', '{googleEmail.Replace("'", "\\'")}');
                            }} catch(e) {{
                                console.error('Failed to store email:', e);
                            }}
                        </script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "StoreGoogleEmail", script);
                }
            }
            else
            {
                Response.Redirect("login.aspx");
                return; 
            }

            int? userId = null;
            string role = Session["Role"]?.ToString();
            
            if (role != "owner" && Session["userId"] != null)
            {
                userId = Convert.ToInt32(Session["userId"]);
            }

            allEvents = calendarService.GetAllEvents(userId);
            ViewState["AllEvents"] = allEvents;

            calendar.SelectedDate = DateTime.Today;
            calendar.VisibleDate = calendar.SelectedDate;
            CurrentMode = CalendarDisplayMode.Gregorian;
            rblCalendarMode.SelectedValue = "G";
            ShowEvents(calendar.SelectedDate);
        }
        else
        {
            allEvents = (DataSet)ViewState["AllEvents"];
            if (!string.IsNullOrEmpty(rblCalendarMode.SelectedValue))
                CurrentMode = rblCalendarMode.SelectedValue == "H" ? CalendarDisplayMode.Hebrew : CalendarDisplayMode.Gregorian;
        }

        rblCalendarMode.SelectedValue = CurrentMode == CalendarDisplayMode.Hebrew ? "H" : "G";
        calendar.VisibleDate = calendar.SelectedDate;
        UpdateCalendarMeta(calendar.SelectedDate);
        BindMonthNavigation(calendar.SelectedDate);
    }
    
    protected void calendar_SelectionChanged(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate;
        calendar.VisibleDate = selectedDate;
        ShowEvents(selectedDate);
        UpdateCalendarMeta(selectedDate);
        BindMonthNavigation(selectedDate);
    }

    protected void calendar_VisibleMonthChanged(object sender, MonthChangedEventArgs e)
    {
        calendar.SelectedDate = e.NewDate;
        calendar.VisibleDate = e.NewDate;
        ShowEvents(e.NewDate);
        UpdateCalendarMeta(e.NewDate);
        BindMonthNavigation(e.NewDate);
    }

    protected void Month_Command(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "ChangeMonth")
            return;

        var parts = Convert.ToString(e.CommandArgument)?.Split('|');
        if (parts == null || parts.Length != 2)
            return;

        string modeToken = parts[0];
        if (!int.TryParse(parts[1], out int month))
            return;

        DateTime baseDate = calendar.SelectedDate == DateTime.MinValue ? DateTime.Today : calendar.SelectedDate;
        DateTime newDate;

        if (string.Equals(modeToken, "H", StringComparison.OrdinalIgnoreCase))
        {
            var hebCal = new HebrewCalendar();
            int hebYear = hebCal.GetYear(baseDate);
            int hebDay = hebCal.GetDayOfMonth(baseDate);
            int daysInMonth = hebCal.GetDaysInMonth(hebYear, month);
            int targetDay = Math.Min(hebDay, daysInMonth);
            newDate = hebCal.ToDateTime(hebYear, month, targetDay, baseDate.Hour, baseDate.Minute, baseDate.Second, baseDate.Millisecond);
        }
        else
        {
            int day = Math.Min(DateTime.DaysInMonth(baseDate.Year, month), baseDate.Day);
            newDate = new DateTime(baseDate.Year, month, day);
        }

        calendar.SelectedDate = newDate;
        calendar.VisibleDate = newDate;
        ShowEvents(newDate);
        UpdateCalendarMeta(newDate);
        BindMonthNavigation(newDate);
    }

    protected void ChangeYear_Click(object sender, EventArgs e)
    {
        if (sender is LinkButton btn && int.TryParse(btn.CommandArgument, out int delta))
        {
            DateTime baseDate = calendar.SelectedDate == DateTime.MinValue ? DateTime.Today : calendar.SelectedDate;
            DateTime newDate;

            if (CurrentMode == CalendarDisplayMode.Hebrew)
            {
                var hebCal = new HebrewCalendar();
                int hebYear = hebCal.GetYear(baseDate) + delta;
                int hebMonth = hebCal.GetMonth(baseDate);
                int hebDay = hebCal.GetDayOfMonth(baseDate);

                int monthsInYear = hebCal.GetMonthsInYear(hebYear);
                if (hebMonth > monthsInYear)
                    hebMonth = monthsInYear;

                int daysInMonth = hebCal.GetDaysInMonth(hebYear, hebMonth);
                int targetDay = Math.Min(hebDay, daysInMonth);
                newDate = hebCal.ToDateTime(hebYear, hebMonth, targetDay, baseDate.Hour, baseDate.Minute, baseDate.Second, baseDate.Millisecond);
            }
            else
            {
                newDate = baseDate.AddYears(delta);
            }

            calendar.SelectedDate = newDate;
            calendar.VisibleDate = newDate;
            ShowEvents(newDate);
            UpdateCalendarMeta(newDate);
            BindMonthNavigation(newDate);
        }
    }

    protected void rblCalendarMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentMode = rblCalendarMode.SelectedValue == "H" ? CalendarDisplayMode.Hebrew : CalendarDisplayMode.Gregorian;
        DateTime reference = calendar.SelectedDate == DateTime.MinValue ? DateTime.Today : calendar.SelectedDate;
        calendar.VisibleDate = reference;
        ShowEvents(reference);
        BindMonthNavigation(reference);
        UpdateCalendarMeta(reference);
    }

    private void ShowEvents(DateTime date)
    {
        var sb = new StringBuilder();
        int count = 0;

        foreach (DataTable table in allEvents.Tables)
        {
            foreach (DataRow row in table.Rows)
            {
                DateTime eventDate;

                string dateColumn = row.Table.Columns.Contains("EventDate") ? "EventDate" : (row.Table.Columns.Contains("date") ? "date" : "EventDate");
                if (!row.Table.Columns.Contains(dateColumn) || row[dateColumn] == DBNull.Value || row[dateColumn] == null)
                    continue;
                if (!DateTime.TryParse(row[dateColumn].ToString(), out eventDate))
                {
                    continue;
                }

                if (eventDate.Date == date.Date)
                {
                    string titleColumn = row.Table.Columns.Contains("Title") ? "Title" : (row.Table.Columns.Contains("title") ? "title" : "Title");
                    string timeColumn = row.Table.Columns.Contains("EventTime") ? "EventTime" : (row.Table.Columns.Contains("time") ? "time" : "EventTime");
                    string notesColumn = row.Table.Columns.Contains("Notes") ? "Notes" : (row.Table.Columns.Contains("notes") ? "notes" : "Notes");
                    
                    string title = row.Table.Columns.Contains(titleColumn) && row[titleColumn] != DBNull.Value && row[titleColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[titleColumn].ToString())) : "";
                    string time = row.Table.Columns.Contains(timeColumn) && row[timeColumn] != DBNull.Value && row[timeColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[timeColumn].ToString())) : "";
                    string note = row.Table.Columns.Contains(notesColumn) && row[notesColumn] != DBNull.Value && row[notesColumn] != null 
                        ? HttpUtility.HtmlEncode(Connect.FixEncoding(row[notesColumn].ToString())) : "";
                    string eventType = table.TableName == "SharedEvents" ? " (טבלה משותפת)" : "";

                    sb.Append("<div class='calendar-event'>");
                    sb.Append("<div class='calendar-event-title'>").Append(title).Append(eventType).Append("</div>");

                    if (!string.IsNullOrEmpty(time))
                        sb.Append("<div class='calendar-event-meta'>⏰ ").Append(time).Append("</div>");

                    if (!string.IsNullOrEmpty(note))
                        sb.Append("<div class='calendar-event-note'>📝 ").Append(note).Append("</div>");

                    sb.Append("</div>");
                    count++;
                }
            }
        }

        if (count == 0)
        {
            sb.Append("<div class='calendar-event empty'>אין אירועים לתאריך הזה.</div>");
        }

        lblEvents.Text = sb.ToString();
    }

    protected void calendar_DayRender(object sender, DayRenderEventArgs e)
    {
        DateTime currentDay = e.Day.Date;
        RenderDayNumber(e, currentDay);
        var summaries = new List<string>();

        foreach (DataTable table in allEvents.Tables)
        {
            foreach (DataRow row in table.Rows)
            {
                DateTime eventDate;

                string dateColumn = row.Table.Columns.Contains("EventDate") ? "EventDate" : (row.Table.Columns.Contains("date") ? "date" : "EventDate");
                if (!row.Table.Columns.Contains(dateColumn) || row[dateColumn] == DBNull.Value || row[dateColumn] == null)
                    continue;
                if (!DateTime.TryParse(row[dateColumn].ToString(), out eventDate))
                    continue;

                if (eventDate.Date == currentDay)
                {
                    string titleColumn = row.Table.Columns.Contains("Title") ? "Title" : (row.Table.Columns.Contains("title") ? "title" : "Title");
                    string title = row.Table.Columns.Contains(titleColumn) && row[titleColumn] != DBNull.Value && row[titleColumn] != null 
                        ? Connect.FixEncoding(row[titleColumn].ToString()) : "";
                    if (title.Length > 18)
                        title = title.Substring(0, 18) + "...";

                    summaries.Add(HttpUtility.HtmlEncode(title));
                }
            }
        }

        if (summaries.Count > 0)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='calendar-cell-events'>");
            foreach (string summary in summaries)
                sb.Append("<span class='calendar-cell-event'>").Append(summary).Append("</span>");
            sb.Append("</div>");

            e.Cell.Controls.Add(new Literal { Text = sb.ToString() });
        }
    }

    private void UpdateCalendarMeta(DateTime date)
    {
        lblMetaDay.Text = date.ToString("dd");
        lblMetaWeekday.Text = date.ToString("dddd", heCulture);
        lblSelectedDate.Text = $"בחרת את התאריך: {date:dd/MM/yyyy}";
        hfSelectedDate.Value = date.ToString("yyyy-MM-dd");

        var hebrewInfo = FetchHebrewInfo(date);
        string hebrewDateText = string.IsNullOrEmpty(hebrewInfo.HebrewDate) ? string.Empty : hebrewInfo.HebrewDate;

        lblMetaFullDate.Text = string.IsNullOrEmpty(hebrewDateText)
            ? date.ToString("dd MMM yyyy", heCulture)
            : $"{date:dd MMM yyyy} / {hebrewDateText}";

        lblHebrewDate.Text = string.IsNullOrEmpty(hebrewDateText) ? "—" : hebrewDateText;
        lblHoliday.Text = string.IsNullOrEmpty(hebrewInfo.Holiday) ? "—" : hebrewInfo.Holiday;

        lblYear.Text = CurrentMode == CalendarDisplayMode.Gregorian
            ? date.Year.ToString()
            : ExtractHebrewYear(hebrewDateText);

        if (string.IsNullOrEmpty(lblYear.Text))
            lblYear.Text = date.Year.ToString();

        var shabbatReference = date.DayOfWeek == DayOfWeek.Saturday ? date.AddDays(-1) : date;
        var shabbatTimes = FetchShabbatTimes(shabbatReference);
        string parashaText = !string.IsNullOrEmpty(shabbatTimes.Parasha) ? shabbatTimes.Parasha : hebrewInfo.Parasha;
        lblParsha.Text = string.IsNullOrEmpty(parashaText) ? "—" : parashaText;

        string candleText = "—";
        string havdalahText = "—";
        if (date.DayOfWeek == DayOfWeek.Friday && !string.IsNullOrEmpty(shabbatTimes.CandleLighting))
            candleText = shabbatTimes.CandleLighting;
        if (date.DayOfWeek == DayOfWeek.Saturday && !string.IsNullOrEmpty(shabbatTimes.Havdalah))
            havdalahText = shabbatTimes.Havdalah;
        lblCandleLighting.Text = candleText;
        lblHavdalah.Text = havdalahText;
    }

    private void BindMonthNavigation(DateTime referenceDate)
    {
        var items = new List<MonthNavItem>();

        if (CurrentMode == CalendarDisplayMode.Gregorian)
        {
            var monthNames = heCulture.DateTimeFormat.MonthNames;
            for (int i = 1; i <= 12; i++)
            {
                string label = monthNames[i - 1];
                if (string.IsNullOrEmpty(label))
                    label = heCulture.DateTimeFormat.AbbreviatedMonthNames[i - 1];

                items.Add(new MonthNavItem
                {
                    Value = $"G|{i}",
                    Label = label,
                    CssClass = referenceDate.Month == i ? "calendar-month-link active" : "calendar-month-link"
                });
            }
        }
        else
        {
            var hebCal = new HebrewCalendar();
            int hebYear = hebCal.GetYear(referenceDate);
            bool isLeapYear = hebCal.IsLeapYear(hebYear);
            int monthsInYear = hebCal.GetMonthsInYear(hebYear);
            int currentHebMonth = hebCal.GetMonth(referenceDate);

            for (int m = 1; m <= monthsInYear; m++)
            {
                items.Add(new MonthNavItem
                {
                    Value = $"H|{m}",
                    Label = GetHebrewMonthName(m, isLeapYear),
                    CssClass = currentHebMonth == m ? "calendar-month-link active" : "calendar-month-link"
                });
            }
        }

        rptMonths.DataSource = items;
        rptMonths.DataBind();
    }

    private static string GetHebrewDayLabel(DateTime date)
    {
        int day = HebrewCal.GetDayOfMonth(date);
        return ToHebrewNumeral(day);
    }

    private static string ToHebrewNumeral(int number)
    {
        if (number <= 0 || number > 30)
            return number.ToString();

        if (number == 15)
            return "ט״ו";
        if (number == 16)
            return "ט״ז";

        int tens = number / 10;
        int ones = number % 10;
        var parts = new List<string>();

        if (tens > 0)
            parts.Add(HebrewTens[tens]);
        if (ones > 0)
            parts.Add(HebrewOnes[ones]);

        if (parts.Count == 0)
            parts.Add(HebrewOnes[0]);

        var text = string.Join(string.Empty, parts);
        if (text.Length == 1)
            return text + "׳";
        return text.Insert(text.Length - 1, "״");
    }

    private static string NormalizeParshaTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        if (raw.StartsWith("Parashat ", StringComparison.OrdinalIgnoreCase))
            raw = raw.Substring("Parashat ".Length);
        else if (raw.StartsWith("Parshat ", StringComparison.OrdinalIgnoreCase))
            raw = raw.Substring("Parshat ".Length);
        raw = raw.Trim();

        var parts = raw.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var translated = new List<string>();

        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.Length == 0)
                continue;

            if (ParashaHebrewMap.TryGetValue(trimmed, out var heb))
                translated.Add(heb);
            else
                translated.Add(trimmed);
        }

        if (translated.Count == 0)
            return raw;

        return string.Join(" - ", translated);
    }

    private static string NormalizeHolidayTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        foreach (var kv in HolidayHebrewMap)
        {
            if (raw.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                return kv.Value;
        }

        return raw;
    }

    private static string GetHebrewMonthName(int month, bool isLeapYear)
    {
        switch (month)
        {
            case 1: return "תשרי";
            case 2: return "חשוון";
            case 3: return "כסלו";
            case 4: return "טבת";
            case 5: return "שבט";
            case 6: return isLeapYear ? "אדר א" : "אדר";
            case 7: return isLeapYear ? "אדר ב" : "ניסן";
            case 8: return isLeapYear ? "ניסן" : "אייר";
            case 9: return isLeapYear ? "אייר" : "סיון";
            case 10: return isLeapYear ? "סיון" : "תמוז";
            case 11: return isLeapYear ? "תמוז" : "אב";
            case 12: return isLeapYear ? "אב" : "אלול";
            case 13: return "אלול";
            default: return string.Empty;
        }
    }

    private static string ExtractHebrewYear(string hebrewDate)
    {
        if (string.IsNullOrWhiteSpace(hebrewDate))
            return string.Empty;

        var parts = hebrewDate.Trim().Split(' ');
        return parts.Length == 0 ? string.Empty : parts[parts.Length - 1];
    }

    private static bool IsHolidayCandidate(string category, string title)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        string normalizedCategory = category?.ToLowerInvariant() ?? string.Empty;
        string normalizedTitle = title.ToLowerInvariant();

        bool isChanukah = normalizedTitle.Contains("חנוכה") || normalizedTitle.Contains("chanuk") || normalizedTitle.Contains("hanukk");

        if (isChanukah)
            return true;

        switch (normalizedCategory)
        {
            case "holiday":
            case "yomtov":
            case "roshchodesh":
            case "special":
            case "modern":
            case "fast":
            case "lagbaomer":
            case "yahrtzeit":
            case "candles":
                return true;
            default:
                return false;
        }
    }

    internal static HebrewInfo FetchHebrewInfo(DateTime date)
    {
        var info = new HebrewInfo();
        var serializer = new JavaScriptSerializer();

        try
        {
            string converterUrl = $"https://www.hebcal.com/converter?cfg=json&gy={date.Year}&gm={date.Month}&gd={date.Day}&g2h=1";
            info.ConverterUrl = converterUrl;
            var converterJson = GetJson(converterUrl);
            if (!string.IsNullOrEmpty(converterJson))
            {
                var data = serializer.Deserialize<Dictionary<string, object>>(converterJson);
                if (data != null)
                {
                    if (data.ContainsKey("hebrew"))
                        info.HebrewDate = Convert.ToString(data["hebrew"]);

                    if (string.IsNullOrEmpty(info.Parasha) && data.TryGetValue("events", out object eventsObj) && eventsObj is object[] converterEvents)
                    {
                        foreach (var evt in converterEvents)
                        {
                            string evtText = Convert.ToString(evt);
                            if (string.IsNullOrWhiteSpace(evtText))
                                continue;

                            if (evtText.StartsWith("Parashat", StringComparison.OrdinalIgnoreCase) || evtText.StartsWith("Parshat", StringComparison.OrdinalIgnoreCase))
                            {
                                info.Parasha = NormalizeParshaTitle(evtText);
                                break;
                            }
                        }
                    }
                }
            }

            string eventsUrl = $"https://www.hebcal.com/hebcal?v=1&cfg=json&start={date:yyyy-MM-dd}&end={date:yyyy-MM-dd}&maj=on&min=on&mod=on&nx=on&mf=on&c=on&geo=pos&latitude=32.0853&longitude=34.7818&tzid=Asia/Jerusalem";
            info.EventsUrl = eventsUrl;
            var eventsJson = GetJson(eventsUrl);
            if (!string.IsNullOrEmpty(eventsJson))
            {
                var data = serializer.Deserialize<Dictionary<string, object>>(eventsJson);
                if (data != null && data.TryGetValue("items", out object itemsObj) && itemsObj is object[] items)
                {
                    foreach (var itemObj in items)
                    {
                        if (itemObj is Dictionary<string, object> item)
                        {
                            string category = item.ContainsKey("category") ? Convert.ToString(item["category"]) : string.Empty;
                            string title = item.ContainsKey("title") ? Convert.ToString(item["title"]) : string.Empty;
                            if (string.IsNullOrEmpty(title))
                                continue;

                            if (string.Equals(category, "parashat", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(info.Parasha))
                                info.Parasha = NormalizeParshaTitle(title);

                            if (string.IsNullOrEmpty(info.Holiday) && IsHolidayCandidate(category, title))
                                info.Holiday = NormalizeHolidayTitle(title);
                        }
                    }
                }
            }
        }
        catch
        {
            // swallow - UI will show defaults
        }

        return info;
    }

    private ShabbatTimes FetchShabbatTimes(DateTime date)
    {
        var times = new ShabbatTimes();
        var serializer = new JavaScriptSerializer();

        try
        {
            var payload = serializer.Serialize(new
            {
                latitude = 31.7683,
                longitude = -35.2137,
                date = date.ToString("yyyy-MM-dd")
            });

            var json = PostJson("zmanimProxy.ashx", payload);
            if (!string.IsNullOrEmpty(json))
            {
                var data = serializer.Deserialize<Dictionary<string, object>>(json);
                if (data != null)
                {
                    times.CandleLighting = FormatToIsraelTime(data.ContainsKey("kenisatShabbat22") ? data["kenisatShabbat22"] : null);
                    times.Havdalah = FormatToIsraelTime(data.ContainsKey("yetziatShabbat") ? data["yetziatShabbat"] : null);
                    times.Parasha = data.ContainsKey("parasha") ? Convert.ToString(data["parasha"]) : string.Empty;
                }
            }
        }
        catch
        {
        }

        return times;
    }

    private static string FormatToIsraelTime(object isoValue)
    {
        if (isoValue == null)
            return string.Empty;

        if (DateTime.TryParse(Convert.ToString(isoValue), null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
        {
            var tz = IsraelTimeZone ?? TimeZoneInfo.Utc;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz).AddHours(-2);
            return local.ToString("HH:mm");
        }

        return string.Empty;
    }

    private static TimeZoneInfo ResolveIsraelTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private class MonthNavItem
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public string CssClass { get; set; }
    }

    internal class HebrewInfo
    {
        public string HebrewDate { get; set; }
        public string Parasha { get; set; }
        public string Holiday { get; set; }
        public string ConverterUrl { get; set; }
        public string EventsUrl { get; set; }
    }

    private class ShabbatTimes
    {
        public string CandleLighting { get; set; }
        public string Havdalah { get; set; }
        public string Parasha { get; set; }
    }

    private static string GetJson(string url)
    {
        try
        {
            using (var client = CreateWebClient())
            {
                return client.DownloadString(url);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string PostJson(string url, string payload)
    {
        try
        {
            using (var client = CreateWebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                return client.UploadString(url, "POST", payload);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static WebClient CreateWebClient()
    {
        var client = new WebClient
        {
            Encoding = Encoding.UTF8
        };
        client.Headers[HttpRequestHeader.Accept] = "application/json";
        client.Headers[HttpRequestHeader.AcceptCharset] = "utf-8";
        return client;
    }

    private static Dictionary<string, string> CreateParashaMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddParasha(map, "Bereshit", "בראשית"); AddParasha(map, "Noach", "נח"); AddParasha(map, "Lech-Lecha", "לך לך"); AddParasha(map, "Vayeira", "וירא");
        AddParasha(map, "Chayei Sara", "חיי שרה"); AddParasha(map, "Toldot", "תולדות"); AddParasha(map, "Vayetzei", "ויצא"); AddParasha(map, "Vayishlach", "וישלח");
        AddParasha(map, "Vayeshev", "וישב"); AddParasha(map, "Miketz", "מקץ"); AddParasha(map, "Vayigash", "ויגש"); AddParasha(map, "Vayechi", "ויחי");
        AddParasha(map, "Shemot", "שמות"); AddParasha(map, "Vaera", "וארא"); AddParasha(map, "Bo", "בא"); AddParasha(map, "Beshalach", "בשלח");
        AddParasha(map, "Yitro", "יתרו"); AddParasha(map, "Mishpatim", "משפטים"); AddParasha(map, "Terumah", "תרומה"); AddParasha(map, "Tetzaveh", "תצוה");
        AddParasha(map, "Ki Tisa", "כי תשא"); AddParasha(map, "Vayakhel", "ויקהל"); AddParasha(map, "Pekudei", "פקודי"); AddParasha(map, "Vayikra", "ויקרא");
        AddParasha(map, "Tzav", "צו"); AddParasha(map, "Shemini", "שמיני"); AddParasha(map, "Tazria", "תזריע"); AddParasha(map, "Metzora", "מצורע");
        AddParasha(map, "Achrei Mot", "אחרי מות"); AddParasha(map, "Kedoshim", "קדושים"); AddParasha(map, "Emor", "אמור"); AddParasha(map, "Behar", "בהר");
        AddParasha(map, "Bechukotai", "בחוקותי"); AddParasha(map, "Bamidbar", "במדבר"); AddParasha(map, "Naso", "נשא"); AddParasha(map, "Beha'alotcha", "בהעלותך");
        AddParasha(map, "Sh'lach", "שלח"); AddParasha(map, "Korach", "קרח"); AddParasha(map, "Chukat", "חוקת"); AddParasha(map, "Balak", "בלק");
        AddParasha(map, "Pinchas", "פנחס"); AddParasha(map, "Matot", "מטות"); AddParasha(map, "Masei", "מסעי"); AddParasha(map, "Devarim", "דברים");
        AddParasha(map, "Vaetchanan", "ואתחנן"); AddParasha(map, "Eikev", "עקב"); AddParasha(map, "Re'eh", "ראה"); AddParasha(map, "Shoftim", "שופטים");
        AddParasha(map, "Ki Teitzei", "כי תצא"); AddParasha(map, "Ki Tavo", "כי תבוא"); AddParasha(map, "Nitzavim", "נצבים"); AddParasha(map, "Vayelech", "וילך");
        AddParasha(map, "Ha'Azinu", "האזינו"); AddParasha(map, "Haazinu", "האזינו"); AddParasha(map, "V'Zot HaBerachah", "וזאת הברכה");
        AddParasha(map, "Vayakhel-Pekudei", "ויקהל - פקודי"); AddParasha(map, "Tazria-Metzora", "תזריע - מצורע"); AddParasha(map, "Achrei Mot-Kedoshim", "אחרי מות - קדושים");
        AddParasha(map, "Behar-Bechukotai", "בהר - בחוקותי"); AddParasha(map, "Chukat-Balak", "חוקת - בלק"); AddParasha(map, "Matot-Masei", "מטות - מסעי");
        AddParasha(map, "Nitzavim-Vayelech", "נצבים - וילך");

        return map;
    }

    private static void AddParasha(Dictionary<string, string> map, string key, string value)
    {
        map[key] = value;
    }
}
