<%@ WebHandler Language="C#" Class="ZmanimProxy" %>

using System;
using System.Web;
using System.Web.Script.Serialization;

public class ZmanimProxy : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        try
        {
            DateTime date = DateTime.Today;
            string dateParam = context.Request["date"];
            DateTime parsed;
            if (!string.IsNullOrWhiteSpace(dateParam) && DateTime.TryParse(dateParam, out parsed))
                date = parsed.Date;

            JewishCalendarService.Place place = ResolvePlace(context);
            JewishCalendarService.DayInfo day = JewishCalendarService.GetDay(date, place);
            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(new
            {
                date = date.ToString("yyyy-MM-dd"),
                hebrewDate = day.HebrewDate ?? string.Empty,
                parasha = day.Parasha ?? string.Empty,
                holiday = day.Holiday ?? string.Empty,
                candleLighting = day.CandleLighting ?? string.Empty,
                havdalah = day.Havdalah ?? string.Empty,
                zone = day.ZoneKey ?? string.Empty,
                place = day.Place ?? string.Empty,
                kenosatShabbat22 = day.CandleLighting ?? string.Empty,
                yetziatShabbat = day.Havdalah ?? string.Empty
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

    private static JewishCalendarService.Place ResolvePlace(HttpContext context)
    {
        int cityId;
        int? city = int.TryParse(context.Request["cityId"], out cityId) ? (int?)cityId : null;
        return JewishCalendarService.ParseZone(context.Request["zone"], city, context.Request["city"]);
    }
}
