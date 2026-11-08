using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

public static class JewishCalendarService
{
    public enum ShabbatZone
    {
        TelAviv,
        Jerusalem,
        Haifa,
        BeerSheva
    }

    public class Place
    {
        public ShabbatZone Zone { get; set; }
        public string Key { get; set; }
        public string ZoneName { get; set; }
        public string CityName { get; set; }
        public string Display { get; set; }
    }

    public class DayInfo
    {
        public string HebrewDate { get; set; }
        public string Parasha { get; set; }
        public string Holiday { get; set; }
        public string CandleLighting { get; set; }
        public string Havdalah { get; set; }
        public string ZoneKey { get; set; }
        public string ZoneName { get; set; }
        public string Place { get; set; }
    }

    private static readonly HebrewCalendar HebrewCal = new HebrewCalendar();
    private static readonly string[] HebrewOnes = { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
    private static readonly string[] HebrewTens = { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };

    private static readonly int[] JerusalemCityIds = { 10, 13 };
    private static readonly int[] HaifaCityIds = { 6, 7, 8, 9, 16, 18 };
    private static readonly int[] BeerShevaCityIds = { 1, 4, 15, 19 };

    public static DayInfo GetDay(DateTime date)
    {
        return GetDay(date, ShabbatZone.TelAviv, null);
    }

    public static DayInfo GetDay(DateTime date, ShabbatZone zone)
    {
        return GetDay(date, zone, null);
    }

    public static DayInfo GetDay(DateTime date, Place place)
    {
        if (place == null)
            place = DefaultPlace();
        return GetDay(date, place.Zone, place);
    }

    public static Place ResolveForUser(object userIdValue)
    {
        int userId;
        if (!TryInt(userIdValue, out userId))
            return DefaultPlace();

        try
        {
            using (var connection = new OleDbConnection(Connect.GetConnectionString()))
            {
                connection.Open();
                int? cityId = null;
                string storedCity = null;
                using (var cmd = new OleDbCommand("SELECT [city], [CityId] FROM [Users] WHERE [id]=?", connection))
                {
                    cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = userId });
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader != null && reader.Read())
                        {
                            storedCity = Convert.ToString(reader["city"]);
                            cityId = ToNullableInt(reader["CityId"]);
                            if (cityId == null)
                                cityId = ToNullableInt(reader["city"]);
                        }
                    }
                }

                string cityName = null;
                if (cityId != null)
                {
                    using (var cmd = new OleDbCommand("SELECT [cityname] FROM [Citys] WHERE [id]=?", connection))
                    {
                        cmd.Parameters.Add(new OleDbParameter("?", OleDbType.Integer) { Value = cityId.Value });
                        object name = cmd.ExecuteScalar();
                        if (name != null && name != DBNull.Value)
                            cityName = Convert.ToString(name);
                    }
                }

                if (string.IsNullOrWhiteSpace(cityName))
                    cityName = storedCity;

                return ResolvePlace(cityId, cityName);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("JEWISH_CALENDAR", "Error resolving user city", ex);
            return DefaultPlace();
        }
    }

    public static Place ResolvePlace(int? cityId, string cityName)
    {
        string cleanName = NormalizeCity(cityName);
        ShabbatZone zone = ShabbatZone.TelAviv;

        if (InZone(cityId, JerusalemCityIds, cleanName, "ירושלים", "מעלה אדומים", "בית שמש", "גבעת זאב", "מעלה עפריים", "מעלה אפרים"))
            zone = ShabbatZone.Jerusalem;
        else if (InZone(cityId, HaifaCityIds, cleanName, "חיפה", "קריות", "קריית ים", "קריית ביאליק", "קריית מוצקין", "קריית אתא", "עכו", "נהריה", "זכרון", "טבריה", "צפת", "עוספ", "כרמיאל", "עפולה", "נצרת", "חדרה", "ורד הגליל", "קריית שמונה"))
            zone = ShabbatZone.Haifa;
        else if (InZone(cityId, BeerShevaCityIds, cleanName, "באר שבע", "אשדוד", "אשקלון", "אילת", "דימונה", "נתיבות", "אופקים", "שדרות", "קריית גת", "קרית גת", "קרית מלאכי", "קריית מלאכי", "ערד", "ירוחם", "מצפה רמון", "ספיר"))
            zone = ShabbatZone.BeerSheva;

        return CreatePlace(zone, cleanName);
    }

    public static Place ParseZone(string zoneKey, int? cityId, string cityName)
    {
        if (!string.IsNullOrWhiteSpace(cityName) || cityId != null)
        {
            Place byCity = ResolvePlace(cityId, cityName);
            if (!string.IsNullOrWhiteSpace(zoneKey))
            {
                ShabbatZone requested;
                if (TryParseZone(zoneKey, out requested))
                    return CreatePlace(requested, byCity.CityName);
            }
            return byCity;
        }

        ShabbatZone zone;
        if (!TryParseZone(zoneKey, out zone))
            zone = ShabbatZone.TelAviv;
        return CreatePlace(zone, null);
    }

    private static DayInfo GetDay(DateTime date, ShabbatZone zone, Place place)
    {
        if (place == null)
            place = CreatePlace(zone, null);

        DateTime day = date.Date;
        var info = new DayInfo
        {
            HebrewDate = FormatLocalHebrewDate(day),
            Parasha = string.Empty,
            Holiday = string.Empty,
            CandleLighting = string.Empty,
            Havdalah = string.Empty,
            ZoneKey = place.Key,
            ZoneName = place.ZoneName,
            Place = place.Display
        };

        try
        {
            EnsureTls();
            ApplyShabbat(info, day, zone);
        }
        catch (Exception ex)
        {
            LoggingService.Log("JEWISH_CALENDAR", "Error loading Jewish calendar data for " + day.ToString("yyyy-MM-dd"), ex);
        }

        return info;
    }

    private static void ApplyShabbat(DayInfo info, DateTime day, ShabbatZone zone)
    {
        int minutes = zone == ShabbatZone.Jerusalem ? 40 : 22;
        string url = string.Format(
            CultureInfo.InvariantCulture,
            "https://www.hebcal.com/shabbat?cfg=json&geonameid={0}&b={1}&M=on&lg=he&date={2:yyyy-MM-dd}",
            GeonameId(zone),
            minutes,
            day);
        Dictionary<string, object> data = DownloadJson(url);
        if (data == null)
            return;

        object itemsObj;
        if (!data.TryGetValue("items", out itemsObj))
            return;

        string selected = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var holidays = new List<string>();

        foreach (object itemObj in Enumerate(itemsObj))
        {
            Dictionary<string, object> item = itemObj as Dictionary<string, object>;
            if (item == null)
                continue;

            string category = item.ContainsKey("category") ? Convert.ToString(item["category"]) : string.Empty;
            string itemDate = item.ContainsKey("date") ? Convert.ToString(item["date"]) : string.Empty;
            string hebrew = item.ContainsKey("hebrew") ? StripNikud(Convert.ToString(item["hebrew"])) : string.Empty;
            string title = item.ContainsKey("title") ? StripNikud(Convert.ToString(item["title"])) : string.Empty;
            string label = !string.IsNullOrWhiteSpace(hebrew) ? hebrew : title;

            if (string.Equals(category, "parashat", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(info.Parasha))
                info.Parasha = CleanParasha(label);
            else if (string.Equals(category, "candles", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(info.CandleLighting))
                info.CandleLighting = FormatClock(itemDate);
            else if (string.Equals(category, "havdalah", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(info.Havdalah))
                info.Havdalah = FormatClock(itemDate);
            else if (IsHoliday(category) && itemDate.StartsWith(selected, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(label))
            {
                if (!holidays.Contains(label))
                    holidays.Add(label);
            }
        }

        if (holidays.Count > 0)
            info.Holiday = string.Join(", ", holidays);
    }

    private static IEnumerable Enumerate(object value)
    {
        if (value == null || value is string)
            yield break;

        IEnumerable enumerable = value as IEnumerable;
        if (enumerable == null)
            yield break;

        foreach (object item in enumerable)
            yield return item;
    }

    private static int GeonameId(ShabbatZone zone)
    {
        switch (zone)
        {
            case ShabbatZone.Jerusalem: return 281184;
            case ShabbatZone.Haifa: return 294801;
            case ShabbatZone.BeerSheva: return 295530;
            default: return 293397;
        }
    }

    private static Place CreatePlace(ShabbatZone zone, string cityName)
    {
        string zoneName = ZoneDisplayName(zone);
        string key = ZoneKey(zone);
        string cleanCity = NormalizeCity(cityName);
        string display = string.IsNullOrEmpty(cleanCity) || string.Equals(cleanCity, zoneName, StringComparison.Ordinal)
            ? zoneName
            : cleanCity + " · " + zoneName;

        return new Place
        {
            Zone = zone,
            Key = key,
            ZoneName = zoneName,
            CityName = cleanCity,
            Display = display
        };
    }

    private static Place DefaultPlace()
    {
        return CreatePlace(ShabbatZone.TelAviv, null);
    }

    private static string ZoneKey(ShabbatZone zone)
    {
        switch (zone)
        {
            case ShabbatZone.Jerusalem: return "jerusalem";
            case ShabbatZone.Haifa: return "haifa";
            case ShabbatZone.BeerSheva: return "beersheva";
            default: return "telaviv";
        }
    }

    private static string ZoneDisplayName(ShabbatZone zone)
    {
        switch (zone)
        {
            case ShabbatZone.Jerusalem: return "ירושלים";
            case ShabbatZone.Haifa: return "חיפה";
            case ShabbatZone.BeerSheva: return "באר שבע";
            default: return "תל אביב";
        }
    }

    public static bool TryParseZone(string zoneKey, out ShabbatZone zone)
    {
        switch ((zoneKey ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "jerusalem":
            case "ירושלים":
                zone = ShabbatZone.Jerusalem;
                return true;
            case "haifa":
            case "חיפה":
                zone = ShabbatZone.Haifa;
                return true;
            case "beersheva":
            case "beer-sheva":
            case "beer sheva":
            case "באר שבע":
                zone = ShabbatZone.BeerSheva;
                return true;
            case "telaviv":
            case "tel-aviv":
            case "tel aviv":
            case "תל אביב":
                zone = ShabbatZone.TelAviv;
                return true;
            default:
                zone = ShabbatZone.TelAviv;
                return false;
        }
    }

    private static bool InZone(int? cityId, int[] ids, string cityName, params string[] tokens)
    {
        if (cityId != null && Contains(ids, cityId.Value))
            return true;
        return Matches(cityName, tokens);
    }

    private static bool Contains(int[] ids, int id)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == id)
                return true;
        }
        return false;
    }

    private static bool Matches(string cityName, params string[] tokens)
    {
        if (string.IsNullOrEmpty(cityName))
            return false;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (cityName.IndexOf(tokens[i], StringComparison.Ordinal) >= 0)
                return true;
        }
        return false;
    }

    private static string NormalizeCity(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            return string.Empty;
        return cityName.Trim().Replace("  ", " ");
    }

    private static bool TryInt(object value, out int number)
    {
        number = 0;
        if (value == null || value == DBNull.Value)
            return false;
        return int.TryParse(Convert.ToString(value), out number);
    }

    private static int? ToNullableInt(object value)
    {
        int number;
        if (TryInt(value, out number))
            return number;
        return null;
    }

    private static bool IsHoliday(string category)
    {
        switch ((category ?? string.Empty).ToLowerInvariant())
        {
            case "holiday":
            case "yomtov":
            case "roshchodesh":
            case "modern":
            case "fast":
            case "special":
            case "lagbaomer":
                return true;
            default:
                return false;
        }
    }

    private static string CleanParasha(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string text = value.Trim();
        if (text.StartsWith("פרשת "))
            text = text.Substring("פרשת ".Length).Trim();
        return text;
    }

    private static string FormatClock(string iso)
    {
        DateTimeOffset dto;
        if (DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out dto))
            return dto.ToString("HH:mm", CultureInfo.InvariantCulture);
        return string.Empty;
    }

    private static string FormatLocalHebrewDate(DateTime day)
    {
        int year = HebrewCal.GetYear(day);
        int month = HebrewCal.GetMonth(day);
        int dayOfMonth = HebrewCal.GetDayOfMonth(day);
        bool leap = HebrewCal.IsLeapYear(year);
        return ToHebrewNumber(dayOfMonth) + " \u05D1" + GetHebrewMonthName(month, leap) + " " + ToHebrewNumber(year % 1000);
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

    private static string ToHebrewNumber(int number)
    {
        if (number <= 0)
            return number.ToString(CultureInfo.InvariantCulture);

        var text = new StringBuilder();
        int n = number;
        while (n >= 400)
        {
            text.Append('\u05EA');
            n -= 400;
        }

        if (n >= 100)
        {
            text.Append("\u05E7\u05E8\u05E9\u05EA"[n / 100 - 1]);
            n %= 100;
        }

        if (n == 15)
            text.Append("\u05D8\u05D5");
        else if (n == 16)
            text.Append("\u05D8\u05D6");
        else
        {
            if (n >= 10)
            {
                text.Append(HebrewTens[n / 10]);
                n %= 10;
            }
            if (n > 0)
                text.Append(HebrewOnes[n]);
        }

        string value = text.ToString();
        if (value.Length <= 1)
            return value + "\u05F3";
        return value.Insert(value.Length - 1, "\u05F4");
    }

    private static string StripNikud(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var text = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (c >= '\u0591' && c <= '\u05C7')
                continue;
            text.Append(c);
        }
        return text.ToString().Trim();
    }

    private static Dictionary<string, object> DownloadJson(string url)
    {
        using (var client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.Accept] = "application/json";
            client.Headers[HttpRequestHeader.UserAgent] = "OptiSched";
            string json = client.DownloadString(url);
            if (string.IsNullOrWhiteSpace(json))
                return null;
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, object>>(json);
        }
    }

    private static void EnsureTls()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        catch
        {
        }
    }
}
