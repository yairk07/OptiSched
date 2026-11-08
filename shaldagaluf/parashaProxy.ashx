<%@ WebHandler Language="C#" Class="ParashaProxy" %>

using System;
using System.Web;
using System.Web.Script.Serialization;

public class ParashaProxy : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        DateTime date = DateTime.Today;
        string dateParam = context.Request["date"];
        DateTime parsed;
            if (!string.IsNullOrEmpty(dateParam) && DateTime.TryParse(dateParam, out parsed))
                date = parsed.Date;

        try
        {
            int cityId;
            int? city = int.TryParse(context.Request["cityId"], out cityId) ? (int?)cityId : null;
            JewishCalendarService.Place place = JewishCalendarService.ParseZone(context.Request["zone"], city, context.Request["city"]);
            JewishCalendarService.DayInfo info = JewishCalendarService.GetDay(date, place);
            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(new
            {
                date = date.ToString("yyyy-MM-dd"),
                hebrewDate = info != null ? (info.HebrewDate ?? string.Empty) : string.Empty,
                parasha = info != null ? (info.Parasha ?? string.Empty) : string.Empty,
                holiday = info != null ? (info.Holiday ?? string.Empty) : string.Empty,
                candleLighting = info != null ? (info.CandleLighting ?? string.Empty) : string.Empty,
                havdalah = info != null ? (info.Havdalah ?? string.Empty) : string.Empty,
                zone = info != null ? (info.ZoneKey ?? string.Empty) : string.Empty,
                place = info != null ? (info.Place ?? string.Empty) : string.Empty
            }));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.Write("{\"error\":\"" + HttpUtility.JavaScriptStringEncode(ex.Message) + "\"}");
        }
    }

    public bool IsReusable
    {
        get { return false; }
    }
}
