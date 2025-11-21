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
        if (!string.IsNullOrEmpty(dateParam) && DateTime.TryParse(dateParam, out DateTime parsed))
            date = parsed;

        try
        {
            var info = home.FetchHebrewInfo(date);
            var serializer = new JavaScriptSerializer();
            var payload = new
            {
                date = date.ToString("yyyy-MM-dd"),
                hebrewDate = info?.HebrewDate ?? string.Empty,
                parasha = info?.Parasha ?? string.Empty,
                holiday = info?.Holiday ?? string.Empty,
                converterUrl = info?.ConverterUrl ?? string.Empty,
                eventsUrl = info?.EventsUrl ?? string.Empty
            };

            context.Response.Write(serializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.Write("{\"error\":\"" + HttpUtility.JavaScriptStringEncode(ex.Message) + "\"}");
        }
    }

    public bool IsReusable => false;
}

